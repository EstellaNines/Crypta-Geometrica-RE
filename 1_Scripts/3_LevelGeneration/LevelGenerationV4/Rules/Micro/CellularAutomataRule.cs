using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 元胞自动机规则
    /// 为每个房间生成洞穴形态的地形
    /// </summary>
    [Serializable]
    public class CellularAutomataRule : GeneratorRuleBase
    {
        #region CA参数

        [TitleGroup("CA参数")]
        [LabelText("迭代次数")]
        [Range(1, 15)]
        [SerializeField]
        private int _iterations = 8;

        [TitleGroup("CA参数")]
        [LabelText("移除孤立格")]
        [Tooltip("移除孤立的凸起和凹陷，使地形更平滑")]
        [SerializeField]
        private bool _removeIsolatedTiles = true;

        [TitleGroup("CA参数")]
        [LabelText("初始填充率")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _fillProbability = 0.45f;

        [TitleGroup("CA参数")]
        [LabelText("出生阈值")]
        [Tooltip("邻居数量>=此值时，空格变为实心")]
        [Range(1, 8)]
        [SerializeField]
        private int _birthLimit = 4;

        [TitleGroup("CA参数")]
        [LabelText("死亡阈值")]
        [Tooltip("邻居数量<此值时，实心变为空格")]
        [Range(1, 8)]
        [SerializeField]
        private int _deathLimit = 3;

        [TitleGroup("边界设置")]
        [LabelText("边界厚度")]
        [Tooltip("房间边缘保持实心的厚度")]
        [Range(1, 5)]
        [SerializeField]
        private int _borderThickness = 2;

        [TitleGroup("连通性")]
        [LabelText("确保连通性")]
        [SerializeField]
        private bool _ensureConnectivity = true;

        [TitleGroup("连通性")]
        [LabelText("最小通道宽度")]
        [ShowIf("_ensureConnectivity")]
        [Range(2, 6)]
        [SerializeField]
        private int _minPathWidth = 3;

        [TitleGroup("连通性")]
        [LabelText("玩家尺寸（格）")]
        [Tooltip("玩家碰撞体尺寸，向上取整。用于移除无法通过的狭窄区域")]
        [Range(1, 4)]
        [SerializeField]
        private int _playerSize = 2;

        #endregion

        #region 内部状态

        // 双缓冲数组（避免每次迭代分配）
        private int[] _bufferA;
        private int[] _bufferB;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public CellularAutomataRule()
        {
            _ruleName = "CellularAutomataRule";
            _executionOrder = 30; // 在宏观规则之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始元胞自动机地形生成...");

            if (context.RoomNodes == null || context.RoomNodes.Count == 0)
            {
                LogError("房间节点列表为空");
                return false;
            }

            // 确保GroundTileData已初始化
            if (context.GroundTileData == null)
            {
                LogError("GroundTileData未初始化");
                return false;
            }

            int processedRooms = 0;

            // 遍历每个有效房间
            foreach (var room in context.RoomNodes)
            {
                if (token.IsCancellationRequested)
                {
                    LogWarning("生成被取消");
                    return false;
                }

                // 为单个房间生成CA地形
                ProcessRoom(context, room);
                processedRooms++;

                // 每处理几个房间让出一帧，避免卡顿
                if (processedRooms % 4 == 0)
                {
                    await UniTask.Yield(token);
                }
            }

            // 方案B：门位置已在CA初始化时标记为空，无需后处理挖掘

            // 膨胀-腐蚀处理（移除狭窄通道）
            LogInfo("执行膨胀-腐蚀处理（移除狭窄通道）...");
            DilateErodeProcess(context);

            // 房间内部连通性后处理
            if (_ensureConnectivity)
            {
                LogInfo("执行房间内部连通性后处理...");
                EnsureRoomConnectivity(context);
            }

            LogInfo($"CA地形生成完成，处理了 {processedRooms} 个房间");
            return true;
        }

        /// <summary>
        /// 处理单个房间的CA生成
        /// </summary>
        private void ProcessRoom(DungeonContext context, RoomNode room)
        {
            BoundsInt bounds = room.WorldBounds;
            int width = bounds.size.x;
            int height = bounds.size.y;
            int startX = bounds.xMin;
            int startY = bounds.yMin;

            // 确保缓冲区大小足够
            int bufferSize = width * height;
            if (_bufferA == null || _bufferA.Length < bufferSize)
            {
                _bufferA = new int[bufferSize];
                _bufferB = new int[bufferSize];
            }

            // 计算该房间的门位置（相邻房间的连接点）
            HashSet<Vector2Int> doorPositions = CalculateDoorPositions(context, room, width, height);

            // 1. 初始化随机填充（门位置保持空）
            InitializeGridWithDoors(_bufferA, width, height, context.RNG, doorPositions);

            // 2. 迭代CA规则（门位置保持空）
            int[] current = _bufferA;
            int[] next = _bufferB;

            for (int iter = 0; iter < _iterations; iter++)
            {
                ApplyCARuleWithDoors(current, next, width, height, doorPositions);
                // 交换缓冲区
                (current, next) = (next, current);
            }

            // 3. 强制边界为实心（门位置除外）
            ApplyBorderWithDoors(current, width, height, doorPositions);

            // 4. 移除孤立格（平滑处理）
            if (_removeIsolatedTiles)
            {
                RemoveIsolatedTiles(current, width, height, doorPositions);
            }

            // 5. 写入context.GroundTileData
            WriteToContext(context, current, startX, startY, width, height);
        }

        /// <summary>
        /// 计算房间的门位置（相邻房间的连接点）
        /// </summary>
        private HashSet<Vector2Int> CalculateDoorPositions(DungeonContext context, RoomNode room, int width, int height)
        {
            HashSet<Vector2Int> doors = new HashSet<Vector2Int>();
            Vector2Int gridPos = room.GridPosition;

            // 创建房间位置查找表
            HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
            foreach (var r in context.RoomNodes)
            {
                roomPositions.Add(r.GridPosition);
            }

            int halfPath = _minPathWidth / 2 + 1;
            int centerX = width / 2;
            int centerY = height / 2;

            // 右邻居 -> 右边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(1, 0)))
            {
                for (int dy = -halfPath; dy <= halfPath; dy++)
                {
                    for (int dx = 0; dx < _borderThickness + 1; dx++)
                    {
                        doors.Add(new Vector2Int(width - 1 - dx, centerY + dy));
                    }
                }
            }

            // 左邻居 -> 左边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(-1, 0)))
            {
                for (int dy = -halfPath; dy <= halfPath; dy++)
                {
                    for (int dx = 0; dx < _borderThickness + 1; dx++)
                    {
                        doors.Add(new Vector2Int(dx, centerY + dy));
                    }
                }
            }

            // 上邻居 -> 上边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(0, 1)))
            {
                for (int dx = -halfPath; dx <= halfPath; dx++)
                {
                    for (int dy = 0; dy < _borderThickness + 1; dy++)
                    {
                        doors.Add(new Vector2Int(centerX + dx, height - 1 - dy));
                    }
                }
            }

            // 下邻居 -> 下边界中央开门
            if (roomPositions.Contains(gridPos + new Vector2Int(0, -1)))
            {
                for (int dx = -halfPath; dx <= halfPath; dx++)
                {
                    for (int dy = 0; dy < _borderThickness + 1; dy++)
                    {
                        doors.Add(new Vector2Int(centerX + dx, dy));
                    }
                }
            }

            return doors;
        }

        /// <summary>
        /// 初始化网格（带门位置，门位置保持空）
        /// </summary>
        private void InitializeGridWithDoors(int[] grid, int width, int height, System.Random rng, HashSet<Vector2Int> doors)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Vector2Int pos = new Vector2Int(x, y);

                    // 门位置强制为空
                    if (doors.Contains(pos))
                    {
                        grid[index] = 0;
                        continue;
                    }

                    // 边界强制为实心
                    if (x < _borderThickness || x >= width - _borderThickness ||
                        y < _borderThickness || y >= height - _borderThickness)
                    {
                        grid[index] = 1;
                    }
                    else
                    {
                        grid[index] = rng.NextDouble() < _fillProbability ? 1 : 0;
                    }
                }
            }
        }

        /// <summary>
        /// 应用CA规则（带门位置，门位置保持空）
        /// </summary>
        private void ApplyCARuleWithDoors(int[] current, int[] next, int width, int height, HashSet<Vector2Int> doors)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Vector2Int pos = new Vector2Int(x, y);

                    // 门位置保持空
                    if (doors.Contains(pos))
                    {
                        next[index] = 0;
                        continue;
                    }

                    // 边界保持实心
                    if (x < _borderThickness || x >= width - _borderThickness ||
                        y < _borderThickness || y >= height - _borderThickness)
                    {
                        next[index] = 1;
                        continue;
                    }

                    int neighbors = CountNeighbors(current, x, y, width, height);
                    int currentValue = current[index];

                    if (currentValue == 1)
                    {
                        next[index] = neighbors < _deathLimit ? 0 : 1;
                    }
                    else
                    {
                        next[index] = neighbors >= _birthLimit ? 1 : 0;
                    }
                }
            }
        }

        /// <summary>
        /// 强制边界为实心（门位置除外）
        /// </summary>
        private void ApplyBorderWithDoors(int[] grid, int width, int height, HashSet<Vector2Int> doors)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    // 门位置保持空
                    if (doors.Contains(pos))
                    {
                        grid[y * width + x] = 0;
                        continue;
                    }

                    if (x < _borderThickness || x >= width - _borderThickness ||
                        y < _borderThickness || y >= height - _borderThickness)
                    {
                        grid[y * width + x] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 计算8邻居中实心格子的数量
        /// </summary>
        private int CountNeighbors(int[] grid, int x, int y, int width, int height)
        {
            int count = 0;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    // 边界外视为实心
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        count++;
                    }
                    else
                    {
                        count += grid[ny * width + nx];
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 强制边界为实心
        /// </summary>
        private void ApplyBorder(int[] grid, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < _borderThickness || x >= width - _borderThickness ||
                        y < _borderThickness || y >= height - _borderThickness)
                    {
                        grid[y * width + x] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 移除孤立的凸起和凹陷格子，使地形更平滑
        /// 孤立凸起：实心格周围≤2个实心邻居 → 设为空
        /// 孤立凹陷：空格周围≤2个空邻居 → 设为实心
        /// </summary>
        private void RemoveIsolatedTiles(int[] grid, int width, int height, HashSet<Vector2Int> doors)
        {
            // 创建临时副本
            int[] temp = new int[grid.Length];
            Array.Copy(grid, temp, grid.Length);

            for (int y = _borderThickness; y < height - _borderThickness; y++)
            {
                for (int x = _borderThickness; x < width - _borderThickness; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    // 门位置不处理
                    if (doors.Contains(pos))
                        continue;

                    int index = y * width + x;
                    int neighbors = CountNeighbors(grid, x, y, width, height);
                    int currentValue = grid[index];

                    if (currentValue == 1)
                    {
                        // 孤立凸起：实心格周围≤2个实心邻居 → 设为空
                        if (neighbors <= 2)
                        {
                            temp[index] = 0;
                        }
                    }
                    else
                    {
                        // 孤立凹陷：空格周围≥6个实心邻居 → 设为实心
                        if (neighbors >= 6)
                        {
                            temp[index] = 1;
                        }
                    }
                }
            }

            // 复制结果回原数组
            Array.Copy(temp, grid, grid.Length);
        }

        /// <summary>
        /// 将CA结果写入上下文的GroundTileData
        /// </summary>
        private void WriteToContext(DungeonContext context, int[] grid, int startX, int startY, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int worldX = startX + x;
                    int worldY = startY + y;
                    int value = grid[y * width + x];

                    context.SetTile(TilemapLayer.Ground, worldX, worldY, value);
                }
            }
        }

        /// <summary>
        /// 膨胀-腐蚀处理（移除无法通过的狭窄区域）
        /// </summary>
        private void DilateErodeProcess(DungeonContext context)
        {
            int mapWidth = context.MapWidth;
            int mapHeight = context.MapHeight;
            int size = mapWidth * mapHeight;

            // 临时缓冲区
            int[] temp = new int[size];

            // 复制当前数据
            Array.Copy(context.GroundTileData, temp, size);

            // 第一步：膨胀（扩展实心区域，填充小缝隙）
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int index = y * mapWidth + x;
                    // 如果当前是空洞，检查周围是否有实心
                    if (context.GroundTileData[index] == 0)
                    {
                        // 检查_playerSize范围内是否有实心邻居
                        bool hasNearbyWall = false;
                        for (int dy = -_playerSize + 1; dy < _playerSize && !hasNearbyWall; dy++)
                        {
                            for (int dx = -_playerSize + 1; dx < _playerSize && !hasNearbyWall; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
                                {
                                    if (context.GroundTileData[ny * mapWidth + nx] == 1)
                                    {
                                        // 计算到最近实心的距离
                                        int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                                        // 如果距离小于玩家尺寸的一半，标记为需要填充
                                        if (dist < _playerSize / 2 + 1)
                                        {
                                            hasNearbyWall = true;
                                        }
                                    }
                                }
                            }
                        }

                        // 如果空洞太窄（两侧都有墙），填充它
                        if (hasNearbyWall && IsNarrowGap(context, x, y, mapWidth, mapHeight))
                        {
                            temp[index] = 1;
                        }
                    }
                }
            }

            // 写回结果
            Array.Copy(temp, context.GroundTileData, size);
        }

        /// <summary>
        /// 检查指定位置是否为狭窄缝隙（两侧都有墙）
        /// </summary>
        private bool IsNarrowGap(DungeonContext context, int x, int y, int mapWidth, int mapHeight)
        {
            // 检查水平方向是否狭窄
            bool leftWall = false;
            bool rightWall = false;
            for (int dx = 1; dx <= _playerSize; dx++)
            {
                if (x - dx >= 0 && context.GroundTileData[y * mapWidth + (x - dx)] == 1)
                    leftWall = true;
                if (x + dx < mapWidth && context.GroundTileData[y * mapWidth + (x + dx)] == 1)
                    rightWall = true;
            }
            if (leftWall && rightWall) return true;

            // 检查垂直方向是否狭窄
            bool topWall = false;
            bool bottomWall = false;
            for (int dy = 1; dy <= _playerSize; dy++)
            {
                if (y - dy >= 0 && context.GroundTileData[(y - dy) * mapWidth + x] == 1)
                    bottomWall = true;
                if (y + dy < mapHeight && context.GroundTileData[(y + dy) * mapWidth + x] == 1)
                    topWall = true;
            }
            if (topWall && bottomWall) return true;

            return false;
        }

        /// <summary>
        /// 确保房间内部连通性（移除孤岛）
        /// </summary>
        private void EnsureRoomConnectivity(DungeonContext context)
        {
            foreach (var room in context.RoomNodes)
            {
                EnsureSingleRoomConnectivity(context, room);
            }
        }

        /// <summary>
        /// 确保单个房间的连通性
        /// </summary>
        private void EnsureSingleRoomConnectivity(DungeonContext context, RoomNode room)
        {
            BoundsInt bounds = room.WorldBounds;
            int width = bounds.size.x;
            int height = bounds.size.y;
            int startX = bounds.xMin;
            int startY = bounds.yMin;

            // 找到所有空洞区域
            bool[,] visited = new bool[width, height];
            List<List<Vector2Int>> regions = new List<List<Vector2Int>>();

            for (int y = _borderThickness; y < height - _borderThickness; y++)
            {
                for (int x = _borderThickness; x < width - _borderThickness; x++)
                {
                    if (visited[x, y])
                        continue;

                    int worldX = startX + x;
                    int worldY = startY + y;

                    // 只处理空洞（值为0）
                    if (context.GetTile(TilemapLayer.Ground, worldX, worldY) == 0)
                    {
                        var region = FloodFill(context, startX, startY, x, y, width, height, visited);
                        if (region.Count > 0)
                        {
                            regions.Add(region);
                        }
                    }
                    else
                    {
                        visited[x, y] = true;
                    }
                }
            }

            if (regions.Count <= 1)
                return; // 无需处理

            // 找到最大区域
            int maxIndex = 0;
            int maxSize = regions[0].Count;
            for (int i = 1; i < regions.Count; i++)
            {
                if (regions[i].Count > maxSize)
                {
                    maxSize = regions[i].Count;
                    maxIndex = i;
                }
            }

            // 连接其他区域到最大区域
            for (int i = 0; i < regions.Count; i++)
            {
                if (i == maxIndex)
                    continue;

                ConnectRegions(context, regions[maxIndex], regions[i], startX, startY);
            }
        }

        /// <summary>
        /// 洪水填充找到连通区域
        /// </summary>
        private List<Vector2Int> FloodFill(DungeonContext context, int startX, int startY, 
            int localX, int localY, int width, int height, bool[,] visited)
        {
            List<Vector2Int> region = new List<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            queue.Enqueue(new Vector2Int(localX, localY));
            visited[localX, localY] = true;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                region.Add(current);

                // 4方向扩展
                int[] dx = { 0, 1, 0, -1 };
                int[] dy = { 1, 0, -1, 0 };

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.x + dx[i];
                    int ny = current.y + dy[i];

                    if (nx < _borderThickness || nx >= width - _borderThickness ||
                        ny < _borderThickness || ny >= height - _borderThickness)
                        continue;

                    if (visited[nx, ny])
                        continue;

                    int worldX = startX + nx;
                    int worldY = startY + ny;

                    if (context.GetTile(TilemapLayer.Ground, worldX, worldY) == 0)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }

            return region;
        }

        /// <summary>
        /// 连接两个区域（挖掘通道）
        /// </summary>
        private void ConnectRegions(DungeonContext context, List<Vector2Int> regionA, 
            List<Vector2Int> regionB, int startX, int startY)
        {
            // 找到两个区域最近的点对
            Vector2Int pointA = regionA[0];
            Vector2Int pointB = regionB[0];
            int minDist = int.MaxValue;

            foreach (var a in regionA)
            {
                foreach (var b in regionB)
                {
                    int dist = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        pointA = a;
                        pointB = b;
                    }
                }
            }

            // 挖掘通道（L型路径）
            CreatePassage(context, pointA, pointB, startX, startY);
        }

        /// <summary>
        /// 创建通道（L型路径）
        /// </summary>
        private void CreatePassage(DungeonContext context, Vector2Int from, Vector2Int to, int startX, int startY)
        {
            int x = from.x;
            int y = from.y;

            // 先水平移动
            while (x != to.x)
            {
                CarvePoint(context, startX + x, startY + y);
                x += x < to.x ? 1 : -1;
            }

            // 再垂直移动
            while (y != to.y)
            {
                CarvePoint(context, startX + x, startY + y);
                y += y < to.y ? 1 : -1;
            }

            // 终点
            CarvePoint(context, startX + x, startY + y);
        }

        /// <summary>
        /// 在指定位置挖掘（设为空洞）
        /// </summary>
        private void CarvePoint(DungeonContext context, int worldX, int worldY)
        {
            // 挖掘指定宽度的通道
            int halfWidth = _minPathWidth / 2;
            for (int dy = -halfWidth; dy <= halfWidth; dy++)
            {
                for (int dx = -halfWidth; dx <= halfWidth; dx++)
                {
                    context.SetTile(TilemapLayer.Ground, worldX + dx, worldY + dy, 0);
                }
            }
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_birthLimit <= _deathLimit)
            {
                errorMessage = "出生阈值应大于死亡阈值";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
