using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 约束布局规则（醉汉游走+出入口约束）
    /// 生成4x4网格的房间拓扑结构
    /// </summary>
    [Serializable]
    public class ConstrainedLayoutRule : GeneratorRuleBase
    {
        #region 算法参数

        [TitleGroup("算法参数")]
        [LabelText("最大游走步数")]
        [Range(5, 30)]
        [SerializeField]
        private int _maxSteps = 20;

        [TitleGroup("算法参数")]
        [LabelText("向下偏移权重")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _downwardBias = 0.4f;

        [TitleGroup("算法参数")]
        [LabelText("侧向偏移权重")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _sidewaysBias = 0.3f;

        [TitleGroup("算法参数")]
        [LabelText("最少房间数")]
        [Range(4, 16)]
        [SerializeField]
        private int _minRooms = 8;

        [TitleGroup("算法参数")]
        [LabelText("最大重试次数")]
        [Range(1, 30)]
        [SerializeField]
        private int _maxRetries = 20;

        #endregion

        #region 内部状态

        // 房间占用网格 [x, y]
        private bool[,] _occupiedGrid;
        
        // 当前游走位置
        private Vector2Int _currentPos;
        
        // 已访问房间列表
        private List<Vector2Int> _visitedRooms;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public ConstrainedLayoutRule()
        {
            _ruleName = "ConstrainedLayoutRule";
            _executionOrder = 10; // 宏观规则最先执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始生成约束布局...");

            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < _maxRetries)
            {
                if (token.IsCancellationRequested)
                {
                    LogWarning("生成被取消");
                    return false;
                }

                retryCount++;
                LogInfo($"尝试生成 (第{retryCount}次)...");

                // 1. 生成布局
                if (!GenerateLayout(context))
                {
                    LogWarning($"生成失败，房间数不足。重试中...");
                    await UniTask.Yield(token);
                    continue;
                }

                // 2. 设置起点和终点
                SetStartAndEndRooms(context);

                // 3. 验证布局（验证失败也重试）
                if (!ValidateLayout(context))
                {
                    LogWarning($"布局验证失败，重试中...");
                    await UniTask.Yield(token);
                    continue;
                }

                success = true;
            }

            if (!success)
            {
                LogError($"生成失败，已达最大重试次数 {_maxRetries}");
                return false;
            }

            // 初始化邻接矩阵
            InitializeAdjacencyMatrix(context);

            LogInfo($"布局生成完成: {context.RoomNodes.Count} 个房间");
            return true;
        }

        /// <summary>
        /// 生成布局核心逻辑
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        private bool GenerateLayout(DungeonContext context)
        {
            int cols = context.GridColumns;
            int rows = context.GridRows;

            // 初始化
            _occupiedGrid = new bool[cols, rows];
            _visitedRooms = new List<Vector2Int>();
            context.RoomNodes = new List<RoomNode>();

            // 1. 起始房间：顶行随机X坐标
            // Unity Tilemap: Y=0是底行，Y=rows-1是顶行
            int startX = context.RNG.Next(0, cols);
            _currentPos = new Vector2Int(startX, rows - 1); // 顶行

            AddRoom(context, _currentPos);

            // 2. 醉汉游走
            for (int step = 0; step < _maxSteps; step++)
            {
                Vector2Int nextPos = GetNextPosition(context);
                
                if (nextPos == _currentPos)
                {
                    // 无法移动，尝试跳跃到未访问的邻居
                    nextPos = FindUnvisitedNeighbor(context);
                    if (nextPos == _currentPos)
                    {
                        // 真的无路可走了
                        break;
                    }
                }

                // 添加新房间并建立连接
                if (!_occupiedGrid[nextPos.x, nextPos.y])
                {
                    AddRoom(context, nextPos);
                }

                // 建立当前房间与新房间的连接
                ConnectRooms(context, _currentPos, nextPos);

                _currentPos = nextPos;
            }

            // 检查房间数量
            if (_visitedRooms.Count < _minRooms)
                return false;

            // 检查是否有底行房间，如果没有则强制扩展
            if (!HasBottomRowRoom())
            {
                ForcePathToBottom(context);
            }

            return true;
        }

        /// <summary>
        /// 检查是否有底行房间
        /// </summary>
        private bool HasBottomRowRoom()
        {
            foreach (var room in _visitedRooms)
            {
                if (room.y == 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 强制扩展路径到底行
        /// </summary>
        private void ForcePathToBottom(DungeonContext context)
        {
            int cols = context.GridColumns;

            // 找到Y坐标最小的房间
            Vector2Int lowestRoom = _visitedRooms[0];
            foreach (var room in _visitedRooms)
            {
                if (room.y < lowestRoom.y)
                    lowestRoom = room;
            }

            // 从该房间向下扩展直到底行
            Vector2Int current = lowestRoom;
            while (current.y > 0)
            {
                Vector2Int next = new Vector2Int(current.x, current.y - 1);
                
                if (!_occupiedGrid[next.x, next.y])
                {
                    AddRoom(context, next);
                }
                ConnectRooms(context, current, next);
                current = next;
            }

            LogInfo($"强制扩展路径到底行: ({current.x}, 0)");
        }

        /// <summary>
        /// 添加房间到布局
        /// </summary>
        private void AddRoom(DungeonContext context, Vector2Int gridPos)
        {
            if (_occupiedGrid[gridPos.x, gridPos.y])
                return;

            _occupiedGrid[gridPos.x, gridPos.y] = true;
            _visitedRooms.Add(gridPos);

            // 计算世界坐标边界
            BoundsInt worldBounds = new BoundsInt(
                gridPos.x * context.RoomSize.x,
                gridPos.y * context.RoomSize.y,
                0,
                context.RoomSize.x,
                context.RoomSize.y,
                1
            );

            RoomNode node = new RoomNode(gridPos, RoomType.Normal, worldBounds);
            context.RoomNodes.Add(node);

            LogInfo($"添加房间: ({gridPos.x}, {gridPos.y})");
        }

        /// <summary>
        /// 获取下一个游走位置（带权重）
        /// </summary>
        private Vector2Int GetNextPosition(DungeonContext context)
        {
            int cols = context.GridColumns;
            int rows = context.GridRows;

            // 可能的方向: 下、左、右、上
            List<(Vector2Int dir, float weight)> directions = new List<(Vector2Int, float)>();

            // 向下 (y-1，因为顶行是rows-1，底行是0)
            if (_currentPos.y - 1 >= 0)
            {
                directions.Add((new Vector2Int(0, -1), _downwardBias));
            }

            // 左
            if (_currentPos.x - 1 >= 0)
            {
                directions.Add((new Vector2Int(-1, 0), _sidewaysBias));
            }

            // 右
            if (_currentPos.x + 1 < cols)
            {
                directions.Add((new Vector2Int(1, 0), _sidewaysBias));
            }

            // 向上 (y+1) - 较低权重
            if (_currentPos.y + 1 < rows)
            {
                directions.Add((new Vector2Int(0, 1), 0.1f));
            }

            if (directions.Count == 0)
                return _currentPos;

            // 归一化权重并随机选择
            float totalWeight = 0f;
            foreach (var d in directions)
            {
                totalWeight += d.weight;
            }

            float random = (float)context.RNG.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (var d in directions)
            {
                cumulative += d.weight;
                if (random <= cumulative)
                {
                    return _currentPos + d.dir;
                }
            }

            return _currentPos + directions[0].dir;
        }

        /// <summary>
        /// 查找未访问的邻居房间
        /// </summary>
        private Vector2Int FindUnvisitedNeighbor(DungeonContext context)
        {
            int cols = context.GridColumns;
            int rows = context.GridRows;

            // 从已访问房间中随机选择一个，查找其未访问的邻居
            List<Vector2Int> shuffledRooms = new List<Vector2Int>(_visitedRooms);
            ShuffleList(shuffledRooms, context.RNG);

            foreach (var room in shuffledRooms)
            {
                Vector2Int[] neighbors = new Vector2Int[]
                {
                    room + new Vector2Int(0, -1), // 下（向底行方向）
                    room + new Vector2Int(0, 1),  // 上
                    room + new Vector2Int(1, 0),  // 右
                    room + new Vector2Int(-1, 0)  // 左
                };

                foreach (var neighbor in neighbors)
                {
                    if (neighbor.x >= 0 && neighbor.x < cols &&
                        neighbor.y >= 0 && neighbor.y < rows &&
                        !_occupiedGrid[neighbor.x, neighbor.y])
                    {
                        // 建立从room到neighbor的连接
                        _currentPos = room;
                        return neighbor;
                    }
                }
            }

            return _currentPos;
        }

        /// <summary>
        /// 连接两个房间
        /// </summary>
        private void ConnectRooms(DungeonContext context, Vector2Int from, Vector2Int to)
        {
            // 在RoomNodes中找到对应房间并添加邻居
            for (int i = 0; i < context.RoomNodes.Count; i++)
            {
                var node = context.RoomNodes[i];
                if (node.GridPosition == from)
                {
                    node.AddNeighbor(to);
                    context.RoomNodes[i] = node; // struct需要重新赋值
                }
                else if (node.GridPosition == to)
                {
                    node.AddNeighbor(from);
                    context.RoomNodes[i] = node;
                }
            }
        }

        /// <summary>
        /// 设置起点和终点房间
        /// </summary>
        private void SetStartAndEndRooms(DungeonContext context)
        {
            if (context.RoomNodes.Count == 0)
                return;

            int cols = context.GridColumns;

            // 起点：第一个添加的房间（顶行）
            Vector2Int startPos = _visitedRooms[0];
            
            // 终点：优先选择底行(Y=0)的房间，否则使用BFS最远房间
            Vector2Int endPos = FindEndRoom(context, startPos);

            // 智能分配侧向门方向（避免被邻居房间阻挡）
            WallDirection startDoorSide = GetValidDoorSide(context, startPos, cols);
            WallDirection endDoorSide = GetValidDoorSide(context, endPos, cols);

            // 更新RoomNodes
            for (int i = 0; i < context.RoomNodes.Count; i++)
            {
                var node = context.RoomNodes[i];
                if (node.GridPosition == startPos)
                {
                    node.SetAsStart(startDoorSide);
                    context.RoomNodes[i] = node;
                    LogInfo($"起点房间: ({startPos.x}, {startPos.y}), 入口方向: {startDoorSide}");
                }
                else if (node.GridPosition == endPos)
                {
                    node.SetAsEnd(endDoorSide);
                    context.RoomNodes[i] = node;
                    LogInfo($"终点房间: ({endPos.x}, {endPos.y}), 出口方向: {endDoorSide}");
                }
            }

            context.StartRoom = startPos;
            context.EndRoom = endPos;
        }

        /// <summary>
        /// 查找终点房间（优先底行）
        /// </summary>
        private Vector2Int FindEndRoom(DungeonContext context, Vector2Int start)
        {
            // 优先选择底行(Y=0)的房间
            Vector2Int? bottomRowRoom = null;
            int maxDistInBottomRow = -1;

            // 计算所有房间到起点的距离
            var distances = CalculateDistances(context, start);

            foreach (var node in context.RoomNodes)
            {
                if (node.GridPosition.y == 0) // 底行
                {
                    int dist = distances.ContainsKey(node.GridPosition) ? distances[node.GridPosition] : 0;
                    if (dist > maxDistInBottomRow)
                    {
                        maxDistInBottomRow = dist;
                        bottomRowRoom = node.GridPosition;
                    }
                }
            }

            // 如果底行有房间，返回距离最远的那个
            if (bottomRowRoom.HasValue)
            {
                return bottomRowRoom.Value;
            }

            // 否则返回全局最远房间
            return FindFarthestRoom(context, start);
        }

        /// <summary>
        /// 计算从起点到所有房间的距离
        /// </summary>
        private Dictionary<Vector2Int, int> CalculateDistances(DungeonContext context, Vector2Int start)
        {
            Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            distances[start] = 0;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentDist = distances[current];

                RoomNode? currentNode = null;
                foreach (var node in context.RoomNodes)
                {
                    if (node.GridPosition == current)
                    {
                        currentNode = node;
                        break;
                    }
                }

                if (currentNode == null || currentNode.Value.ConnectedNeighbors == null)
                    continue;

                foreach (var neighbor in currentNode.Value.ConnectedNeighbors)
                {
                    if (!distances.ContainsKey(neighbor))
                    {
                        distances[neighbor] = currentDist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return distances;
        }

        /// <summary>
        /// 获取有效的侧向门方向（避免被邻居房间阻挡）
        /// </summary>
        private WallDirection GetValidDoorSide(DungeonContext context, Vector2Int pos, int cols)
        {
            bool hasLeftNeighbor = false;
            bool hasRightNeighbor = false;

            // 检查左右是否有邻居房间
            foreach (var node in context.RoomNodes)
            {
                if (node.GridPosition == pos + new Vector2Int(-1, 0))
                    hasLeftNeighbor = true;
                if (node.GridPosition == pos + new Vector2Int(1, 0))
                    hasRightNeighbor = true;
            }

            // 边界检查
            bool canLeft = pos.x > 0 && !hasLeftNeighbor;
            bool canRight = pos.x < cols - 1 && !hasRightNeighbor;

            // 如果在最左边，只能Right；如果在最右边，只能Left
            if (pos.x == 0)
                return WallDirection.Right;
            if (pos.x == cols - 1)
                return WallDirection.Left;

            // 优先选择没有邻居的方向
            if (canLeft && !canRight)
                return WallDirection.Left;
            if (canRight && !canLeft)
                return WallDirection.Right;

            // 两边都可以或都不可以，随机选择
            return context.RNG.Next(2) == 0 ? WallDirection.Left : WallDirection.Right;
        }

        /// <summary>
        /// 使用BFS查找距离起点最远的房间
        /// </summary>
        private Vector2Int FindFarthestRoom(DungeonContext context, Vector2Int start)
        {
            Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            distances[start] = 0;
            queue.Enqueue(start);

            Vector2Int farthest = start;
            int maxDistance = 0;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentDist = distances[current];

                // 找到当前房间的邻居
                RoomNode? currentNode = null;
                foreach (var node in context.RoomNodes)
                {
                    if (node.GridPosition == current)
                    {
                        currentNode = node;
                        break;
                    }
                }

                if (currentNode == null || currentNode.Value.ConnectedNeighbors == null)
                    continue;

                foreach (var neighbor in currentNode.Value.ConnectedNeighbors)
                {
                    if (!distances.ContainsKey(neighbor))
                    {
                        int newDist = currentDist + 1;
                        distances[neighbor] = newDist;
                        queue.Enqueue(neighbor);

                        if (newDist > maxDistance)
                        {
                            maxDistance = newDist;
                            farthest = neighbor;
                        }
                    }
                }
            }

            return farthest;
        }

        /// <summary>
        /// 初始化邻接矩阵
        /// </summary>
        private void InitializeAdjacencyMatrix(DungeonContext context)
        {
            int size = context.GridColumns * context.GridRows;
            context.AdjacencyMatrix = new int[size, size];

            foreach (var node in context.RoomNodes)
            {
                if (node.ConnectedNeighbors == null)
                    continue;

                int fromIdx = node.GetIndex(context.GridColumns);
                foreach (var neighbor in node.ConnectedNeighbors)
                {
                    int toIdx = neighbor.y * context.GridColumns + neighbor.x;
                    context.AdjacencyMatrix[fromIdx, toIdx] = 1;
                    context.AdjacencyMatrix[toIdx, fromIdx] = 1; // 双向
                }
            }
        }

        /// <summary>
        /// 洗牌列表
        /// </summary>
        private void ShuffleList<T>(List<T> list, System.Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// 验证布局是否符合约束
        /// </summary>
        private bool ValidateLayout(DungeonContext context)
        {
            int rows = context.GridRows;
            int cols = context.GridColumns;

            // 1. 验证起点在顶行
            if (context.StartRoom.y != rows - 1)
            {
                LogWarning($"起点不在顶行: Y={context.StartRoom.y}, 期望Y={rows - 1}");
                return false;
            }

            // 2. 验证终点在底行
            if (context.EndRoom.y != 0)
            {
                LogWarning($"终点不在底行: Y={context.EndRoom.y}, 期望Y=0");
                return false;
            }

            // 3. 验证起点侧向门不被阻挡
            RoomNode? startNode = null;
            RoomNode? endNode = null;
            foreach (var node in context.RoomNodes)
            {
                if (node.GridPosition == context.StartRoom)
                    startNode = node;
                if (node.GridPosition == context.EndRoom)
                    endNode = node;
            }

            if (startNode.HasValue && IsDoorBlocked(context, startNode.Value))
            {
                LogWarning($"起点侧向门被阻挡");
                return false;
            }

            if (endNode.HasValue && IsDoorBlocked(context, endNode.Value))
            {
                LogWarning($"终点侧向门被阻挡");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查侧向门是否被邻居房间阻挡
        /// </summary>
        private bool IsDoorBlocked(DungeonContext context, RoomNode room)
        {
            if (room.RestrictedDoorSide == WallDirection.None)
                return false;

            Vector2Int checkDir = room.RestrictedDoorSide == WallDirection.Left 
                ? new Vector2Int(-1, 0) 
                : new Vector2Int(1, 0);

            Vector2Int neighborPos = room.GridPosition + checkDir;

            // 检查是否有邻居房间
            foreach (var node in context.RoomNodes)
            {
                if (node.GridPosition == neighborPos)
                    return true; // 被阻挡
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_maxSteps < _minRooms)
            {
                errorMessage = $"最大步数({_maxSteps})不能小于最少房间数({_minRooms})";
                return false;
            }

            if (_downwardBias + _sidewaysBias > 1f)
            {
                errorMessage = "向下偏移权重 + 侧向偏移权重不应超过1";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
