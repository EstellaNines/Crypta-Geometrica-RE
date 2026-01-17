using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 路径验证规则
    /// 使用2×2方块模拟玩家，验证从入口到出口是否可通行
    /// 如果存在阻塞，自动修复
    /// </summary>
    [Serializable]
    public class PathValidationRule : GeneratorRuleBase
    {
        #region 参数

        [TitleGroup("玩家模拟")]
        [LabelText("玩家尺寸")]
        [Tooltip("玩家碰撞体尺寸（格）")]
        [Range(1, 4)]
        [SerializeField]
        private int _playerSize = 2;

        [TitleGroup("修复设置")]
        [LabelText("自动修复")]
        [Tooltip("检测到阻塞时自动挖掘通道")]
        [SerializeField]
        private bool _autoFix = true;

        [TitleGroup("修复设置")]
        [LabelText("修复通道宽度")]
        [ShowIf("_autoFix")]
        [Range(2, 6)]
        [SerializeField]
        private int _fixPathWidth = 3;

        [TitleGroup("调试")]
        [LabelText("显示路径日志")]
        [SerializeField]
        private bool _showPathLog = false;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public PathValidationRule()
        {
            _ruleName = "PathValidationRule";
            _executionOrder = 36; // 在EntranceExitRule之后执行
        }

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
        {
            LogInfo("开始路径验证...");

            // 找到起点和终点房间
            RoomNode? startRoom = null;
            RoomNode? endRoom = null;

            foreach (var room in context.RoomNodes)
            {
                if (room.Type == RoomType.Start)
                    startRoom = room;
                else if (room.Type == RoomType.End)
                    endRoom = room;
            }

            if (!startRoom.HasValue || !endRoom.HasValue)
            {
                LogWarning("未找到起点或终点房间，跳过路径验证");
                return true;
            }

            // 计算起点和终点位置
            Vector2Int startPos = CalculateDoorPosition(startRoom.Value);
            Vector2Int endPos = CalculateDoorPosition(endRoom.Value);

            LogInfo($"起点: {startPos}, 终点: {endPos}");

            // 使用BFS寻找路径
            List<Vector2Int> path = FindPath(context, startPos, endPos);

            if (path != null && path.Count > 0)
            {
                LogInfo($"找到有效路径，长度: {path.Count}");
                return true;
            }

            LogWarning("未找到有效路径，尝试自动修复...");

            if (_autoFix)
            {
                // 自动修复：在房间间连接处挖掘更宽的通道
                FixBlockedPaths(context);

                // 重新验证
                path = FindPath(context, startPos, endPos);
                if (path != null && path.Count > 0)
                {
                    LogInfo($"修复成功，找到有效路径，长度: {path.Count}");
                    return true;
                }
                else
                {
                    LogError("自动修复失败，仍无法找到有效路径");
                    return false;
                }
            }

            await UniTask.Yield(token);
            return false;
        }

        /// <summary>
        /// 计算门位置（与EntranceExitRule一致）
        /// </summary>
        private Vector2Int CalculateDoorPosition(RoomNode room)
        {
            BoundsInt bounds = room.WorldBounds;
            int centerX = bounds.xMin + bounds.size.x / 2;
            int centerY = bounds.yMin + bounds.size.y / 2;
            int xOffset = (int)(bounds.size.x * 0.45f);

            int x;
            switch (room.RestrictedDoorSide)
            {
                case WallDirection.Left:
                    x = centerX - xOffset;
                    break;
                case WallDirection.Right:
                    x = centerX + xOffset;
                    break;
                default:
                    x = centerX;
                    break;
            }

            return new Vector2Int(x, centerY);
        }

        /// <summary>
        /// 使用BFS寻找路径（考虑玩家尺寸）
        /// </summary>
        private List<Vector2Int> FindPath(DungeonContext context, Vector2Int start, Vector2Int end)
        {
            int mapWidth = context.MapWidth;
            int mapHeight = context.MapHeight;

            // 检查起点和终点是否可通行
            if (!CanPlayerStand(context, start.x, start.y))
            {
                if (_showPathLog) LogInfo($"起点 {start} 不可通行");
                return null;
            }

            if (!CanPlayerStand(context, end.x, end.y))
            {
                if (_showPathLog) LogInfo($"终点 {end} 不可通行");
                return null;
            }

            // BFS
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);
            cameFrom[start] = start;

            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                // 到达终点（允许一定误差）
                if (Vector2Int.Distance(current, end) < _playerSize + 1)
                {
                    // 重建路径
                    List<Vector2Int> path = new List<Vector2Int>();
                    Vector2Int node = current;
                    while (node != start)
                    {
                        path.Add(node);
                        node = cameFrom[node];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                foreach (var dir in directions)
                {
                    Vector2Int next = current + dir;

                    if (visited.Contains(next))
                        continue;

                    if (next.x < 0 || next.x >= mapWidth || next.y < 0 || next.y >= mapHeight)
                        continue;

                    if (!CanPlayerStand(context, next.x, next.y))
                        continue;

                    visited.Add(next);
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            return null; // 未找到路径
        }

        /// <summary>
        /// 检查玩家是否可以站在指定位置（考虑玩家尺寸）
        /// </summary>
        private bool CanPlayerStand(DungeonContext context, int x, int y)
        {
            // 检查_playerSize × _playerSize区域是否都是空的
            for (int dy = 0; dy < _playerSize; dy++)
            {
                for (int dx = 0; dx < _playerSize; dx++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;

                    if (checkX < 0 || checkX >= context.MapWidth ||
                        checkY < 0 || checkY >= context.MapHeight)
                    {
                        return false;
                    }

                    int tile = context.GetTile(TilemapLayer.Ground, checkX, checkY);
                    if (tile == 1) // 实心
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 修复阻塞的路径
        /// </summary>
        private void FixBlockedPaths(DungeonContext context)
        {
            LogInfo("开始修复阻塞路径...");

            // 在所有房间连接处挖掘更宽的通道
            HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
            foreach (var room in context.RoomNodes)
            {
                roomPositions.Add(room.GridPosition);
            }

            foreach (var room in context.RoomNodes)
            {
                Vector2Int gridPos = room.GridPosition;
                BoundsInt bounds = room.WorldBounds;
                int centerX = bounds.xMin + bounds.size.x / 2;
                int centerY = bounds.yMin + bounds.size.y / 2;

                // 右邻居
                if (roomPositions.Contains(gridPos + new Vector2Int(1, 0)))
                {
                    CarvePassage(context, bounds.xMax - 1, centerY, true);
                }

                // 上邻居
                if (roomPositions.Contains(gridPos + new Vector2Int(0, 1)))
                {
                    CarvePassage(context, centerX, bounds.yMax - 1, false);
                }
            }
        }

        /// <summary>
        /// 挖掘通道
        /// </summary>
        private void CarvePassage(DungeonContext context, int x, int y, bool horizontal)
        {
            int halfWidth = _fixPathWidth / 2 + 1;
            int depth = _playerSize + 2;

            if (horizontal)
            {
                // 水平通道（连接左右房间）
                for (int dx = -depth; dx <= depth; dx++)
                {
                    for (int dy = -halfWidth; dy <= halfWidth; dy++)
                    {
                        context.SetTile(TilemapLayer.Ground, x + dx, y + dy, 0);
                    }
                }
            }
            else
            {
                // 垂直通道（连接上下房间）
                for (int dy = -depth; dy <= depth; dy++)
                {
                    for (int dx = -halfWidth; dx <= halfWidth; dx++)
                    {
                        context.SetTile(TilemapLayer.Ground, x + dx, y + dy, 0);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_playerSize < 1)
            {
                errorMessage = "玩家尺寸必须>=1";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
