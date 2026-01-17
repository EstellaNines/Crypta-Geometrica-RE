using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// BFS验证规则
    /// 验证起点到终点的连通性，标记关键路径
    /// </summary>
    [Serializable]
    public class BFSValidationRule : GeneratorRuleBase
    {
        #region 配置参数

        [TitleGroup("验证参数")]
        [LabelText("启用环路创建")]
        [Tooltip("在关键路径之外创建额外连接增加多样性")]
        [SerializeField]
        private bool _enableLoopCreation = false;

        [TitleGroup("验证参数")]
        [LabelText("最大额外连接数")]
        [Range(0, 4)]
        [ShowIf("_enableLoopCreation")]
        [SerializeField]
        private int _maxExtraConnections = 2;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public BFSValidationRule()
        {
            _ruleName = "BFSValidationRule";
            _executionOrder = 20; // 在ConstrainedLayoutRule之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始BFS验证...");

            if (context.RoomNodes == null || context.RoomNodes.Count == 0)
            {
                LogError("没有房间节点可验证");
                return false;
            }

            // 1. BFS验证连通性并标记关键路径
            bool isConnected = ValidateAndMarkCriticalPath(context);

            if (!isConnected)
            {
                LogError($"起点({context.StartRoom})到终点({context.EndRoom})不连通");
                return false;
            }

            LogInfo($"连通性验证通过，关键路径包含 {context.CriticalPath.Count} 个房间");

            // 2. 可选：创建环路
            if (_enableLoopCreation)
            {
                CreateExtraConnections(context);
            }

            await UniTask.Yield(token);

            LogInfo("BFS验证完成");
            return true;
        }

        /// <summary>
        /// BFS验证连通性并标记关键路径
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns>是否连通</returns>
        private bool ValidateAndMarkCriticalPath(DungeonContext context)
        {
            Vector2Int start = context.StartRoom;
            Vector2Int end = context.EndRoom;

            // BFS查找路径
            Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);
            parent[start] = start; // 起点的父节点是自己

            bool found = false;

            while (queue.Count > 0 && !found)
            {
                Vector2Int current = queue.Dequeue();

                if (current == end)
                {
                    found = true;
                    break;
                }

                // 获取当前房间的邻居
                RoomNode? currentNode = FindRoomNode(context, current);
                if (currentNode == null || currentNode.Value.ConnectedNeighbors == null)
                    continue;

                foreach (var neighbor in currentNode.Value.ConnectedNeighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (!found)
                return false;

            // 回溯标记关键路径
            context.CriticalPath = new HashSet<Vector2Int>();
            Vector2Int trace = end;

            while (true)
            {
                context.CriticalPath.Add(trace);

                // 在RoomNodes中标记IsCritical
                MarkRoomAsCritical(context, trace);

                if (trace == start)
                    break;

                trace = parent[trace];
            }

            return true;
        }

        /// <summary>
        /// 查找房间节点
        /// </summary>
        private RoomNode? FindRoomNode(DungeonContext context, Vector2Int pos)
        {
            foreach (var node in context.RoomNodes)
            {
                if (node.GridPosition == pos)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// 标记房间为关键路径
        /// </summary>
        private void MarkRoomAsCritical(DungeonContext context, Vector2Int pos)
        {
            for (int i = 0; i < context.RoomNodes.Count; i++)
            {
                var node = context.RoomNodes[i];
                if (node.GridPosition == pos)
                {
                    node.IsCritical = true;
                    context.RoomNodes[i] = node;
                    break;
                }
            }
        }

        /// <summary>
        /// 创建额外连接（环路）增加多样性
        /// </summary>
        private void CreateExtraConnections(DungeonContext context)
        {
            int connectionsAdded = 0;
            int cols = context.GridColumns;
            int rows = context.GridRows;

            // 创建房间位置到索引的映射
            Dictionary<Vector2Int, int> posToIndex = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < context.RoomNodes.Count; i++)
            {
                posToIndex[context.RoomNodes[i].GridPosition] = i;
            }

            // 遍历所有房间，检查可以创建的额外连接
            foreach (var node in context.RoomNodes)
            {
                if (connectionsAdded >= _maxExtraConnections)
                    break;

                Vector2Int pos = node.GridPosition;
                Vector2Int[] potentialNeighbors = new Vector2Int[]
                {
                    pos + new Vector2Int(0, 1),
                    pos + new Vector2Int(0, -1),
                    pos + new Vector2Int(1, 0),
                    pos + new Vector2Int(-1, 0)
                };

                foreach (var neighbor in potentialNeighbors)
                {
                    if (connectionsAdded >= _maxExtraConnections)
                        break;

                    // 检查邻居是否存在且未连接
                    if (posToIndex.ContainsKey(neighbor) && 
                        (node.ConnectedNeighbors == null || !node.ConnectedNeighbors.Contains(neighbor)))
                    {
                        // 添加连接
                        int nodeIdx = posToIndex[pos];
                        int neighborIdx = posToIndex[neighbor];

                        var nodeRef = context.RoomNodes[nodeIdx];
                        var neighborRef = context.RoomNodes[neighborIdx];

                        nodeRef.AddNeighbor(neighbor);
                        neighborRef.AddNeighbor(pos);

                        context.RoomNodes[nodeIdx] = nodeRef;
                        context.RoomNodes[neighborIdx] = neighborRef;

                        // 更新邻接矩阵
                        int fromMatrixIdx = pos.y * cols + pos.x;
                        int toMatrixIdx = neighbor.y * cols + neighbor.x;
                        context.AdjacencyMatrix[fromMatrixIdx, toMatrixIdx] = 1;
                        context.AdjacencyMatrix[toMatrixIdx, fromMatrixIdx] = 1;

                        connectionsAdded++;
                        LogInfo($"创建额外连接: ({pos.x},{pos.y}) <-> ({neighbor.x},{neighbor.y})");
                    }
                }
            }

            if (connectionsAdded > 0)
            {
                LogInfo($"共创建 {connectionsAdded} 个额外连接");
            }
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }
    }
}
