using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 房间生成规则
    /// 串行调用DungeonGenerator为每个WorldNode生成房间
    /// </summary>
    [Serializable]
    public class RoomGenerationRule : WorldRuleBase
    {
        #region 配置

        [TitleGroup("生成配置")]
        [LabelText("生成失败时重试")]
        [Tooltip("单个房间生成失败时是否重试")]
        [SerializeField]
        private bool _retryOnFailure = true;

        [TitleGroup("生成配置")]
        [LabelText("最大重试次数")]
        [Tooltip("单个房间的最大重试次数")]
        [MinValue(1)]
        [ShowIf("_retryOnFailure")]
        [SerializeField]
        private int _maxRetries = 3;

        [TitleGroup("生成配置")]
        [LabelText("生成间隔帧数")]
        [Tooltip("每个房间生成后等待的帧数（0=不等待）")]
        [MinValue(0)]
        [SerializeField]
        private int _framesBetweenRooms = 1;

        #endregion

        #region 构造函数

        public RoomGenerationRule()
        {
            _ruleName = "房间生成规则";
            _executionOrder = 100;
        }

        #endregion

        #region 执行逻辑

        /// <inheritdoc/>
        public override async UniTask<bool> ExecuteAsync(WorldContext context, CancellationToken token)
        {
            if (context == null)
            {
                LogError("WorldContext is null");
                return false;
            }

            if (context.Nodes == null || context.Nodes.Count == 0)
            {
                LogWarning("No nodes to generate rooms for");
                return true;
            }

            if (context.DungeonGenerator == null)
            {
                LogError("DungeonGenerator reference is null");
                return false;
            }

            LogInfo($"开始生成房间 ({context.Nodes.Count} 个节点)");

            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < context.Nodes.Count; i++)
            {
                if (CheckCancellation(token))
                {
                    return false;
                }

                var node = context.Nodes[i];
                LogInfo($"生成房间 [{i + 1}/{context.Nodes.Count}] @ {node.GridPosition} (WorldOffset: {node.WorldPixelOffset})");

                // 调用房间生成（WorldOffset通过重载方法传递）
                bool success = await GenerateRoomWithRetry(
                    context.DungeonGenerator,
                    node,
                    token
                );

                if (success)
                {
                    node.IsGenerated = true;
                    successCount++;
                    
                    // 获取并存储出入口位置
                    ExtractEntranceExitPositions(context.DungeonGenerator, node);
                    
                    LogInfo($"房间生成成功 @ {node.GridPosition}");
                }
                else
                {
                    failCount++;
                    LogWarning($"房间生成失败 @ {node.GridPosition}");
                }

                // 帧间隔
                if (_framesBetweenRooms > 0 && i < context.Nodes.Count - 1)
                {
                    for (int f = 0; f < _framesBetweenRooms; f++)
                    {
                        await YieldFrame(token);
                    }
                }
            }

            LogInfo($"房间生成完成: 成功 {successCount}, 失败 {failCount}");

            return failCount == 0;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 带重试的房间生成
        /// 使用DungeonGenerator的带WorldOffset重载方法
        /// </summary>
        /// <param name="generator">房间生成器</param>
        /// <param name="node">世界节点</param>
        /// <param name="token">取消令牌</param>
        /// <returns>是否成功</returns>
        private async UniTask<bool> GenerateRoomWithRetry(
            DungeonGenerator generator,
            WorldNode node,
            CancellationToken token)
        {
            int attempts = 0;
            int maxAttempts = _retryOnFailure ? _maxRetries : 1;

            while (attempts < maxAttempts)
            {
                attempts++;

                if (CheckCancellation(token))
                {
                    return false;
                }

                try
                {
                    // 调用DungeonGenerator生成房间（带WorldOffset）
                    bool success = await generator.GenerateDungeonAsync(node.RoomSeed, node.WorldPixelOffset);

                    if (success)
                    {
                        return true;
                    }

                    if (attempts < maxAttempts)
                    {
                        LogWarning($"房间生成失败，重试 ({attempts}/{maxAttempts})");
                        // 更换种子重试
                        node.RoomSeed = Environment.TickCount + attempts;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    LogError($"房间生成异常: {e.Message}");

                    if (attempts >= maxAttempts)
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 从DungeonGenerator的Context中提取出入口位置
        /// </summary>
        /// <param name="generator">房间生成器</param>
        /// <param name="node">世界节点</param>
        private void ExtractEntranceExitPositions(DungeonGenerator generator, WorldNode node)
        {
            var context = generator.Context;
            if (context == null || context.RoomNodes == null || context.RoomNodes.Count == 0)
            {
                LogWarning($"无法提取出入口位置 @ {node.GridPosition}: Context或RoomNodes为空");
                return;
            }

            // 查找起点和终点房间
            RoomNode startRoom = null;
            RoomNode endRoom = null;

            foreach (var room in context.RoomNodes)
            {
                if (room.Type == RoomType.Start)
                {
                    startRoom = room;
                }
                else if (room.Type == RoomType.End)
                {
                    endRoom = room;
                }
            }

            // 计算入口位置（起点房间的门位置 + WorldOffset）
            if (startRoom != null)
            {
                Vector2Int doorPos = CalculateDoorPosition(startRoom);
                node.EntrancePosition = doorPos + node.WorldPixelOffset;
                LogInfo($"入口位置 @ {node.GridPosition}: {node.EntrancePosition}");
            }
            else
            {
                LogWarning($"未找到起点房间 @ {node.GridPosition}");
            }

            // 计算出口位置（终点房间的门位置 + WorldOffset）
            if (endRoom != null)
            {
                Vector2Int doorPos = CalculateDoorPosition(endRoom);
                node.ExitPosition = doorPos + node.WorldPixelOffset;
                LogInfo($"出口位置 @ {node.GridPosition}: {node.ExitPosition}");
            }
            else
            {
                LogWarning($"未找到终点房间 @ {node.GridPosition}");
            }

            node.HasEntranceExitData = startRoom != null && endRoom != null;
        }

        /// <summary>
        /// 计算门位置（与EntranceExitRule一致）
        /// </summary>
        /// <param name="room">房间节点</param>
        /// <returns>门的像素坐标</returns>
        private Vector2Int CalculateDoorPosition(RoomNode room)
        {
            BoundsInt bounds = room.WorldBounds;
            
            // 房间中心
            int centerX = bounds.xMin + bounds.size.x / 2;
            int centerY = bounds.yMin + bounds.size.y / 2;
            
            // 根据门方向计算X偏移（与Gizmos一致：±0.45 * roomSize.x）
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

        #endregion

        #region 验证

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_retryOnFailure && _maxRetries < 1)
            {
                errorMessage = "重试次数至少为 1";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        #endregion
    }
}
