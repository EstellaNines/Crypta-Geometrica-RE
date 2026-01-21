using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 随机放置规则
    /// 在世界网格中随机放置房间节点
    /// 约束：房间之间必须间隔1格（不能正交相邻），禁止对角放置
    /// </summary>
    [Serializable]
    public class RandomPlacementRule : WorldRuleBase
    {
        #region 配置

        [TitleGroup("放置配置")]
        [LabelText("生成阈值")]
        [Tooltip("随机值超过此阈值才生成房间 (0-1)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _spawnThreshold = 0.5f;

        [TitleGroup("放置配置")]
        [LabelText("最大尝试次数")]
        [Tooltip("单次放置的最大尝试次数")]
        [MinValue(10)]
        [SerializeField]
        private int _maxAttempts = 100;

        [TitleGroup("放置配置")]
        [LabelText("最大轮次")]
        [Tooltip("整体放置的最大轮次")]
        [MinValue(1)]
        [SerializeField]
        private int _maxRounds = 50;

        #endregion

        #region 正交方向定义

        /// <summary>
        /// 正交方向（上下左右）
        /// </summary>
        private static readonly Vector2Int[] OrthogonalDirections = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // 上
            new Vector2Int(0, -1),  // 下
            new Vector2Int(-1, 0),  // 左
            new Vector2Int(1, 0)    // 右
        };

        #endregion

        #region 构造函数

        public RandomPlacementRule()
        {
            _ruleName = "随机放置规则";
            _executionOrder = 10;
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

            LogInfo($"开始随机放置 (目标: {context.RoomCount} 个房间, 网格: {context.GridSize}×{context.GridSize})");

            // 收集所有可用位置
            var availablePositions = new List<Vector2Int>();
            for (int x = 0; x < context.GridSize; x++)
            {
                for (int y = 0; y < context.GridSize; y++)
                {
                    availablePositions.Add(new Vector2Int(x, y));
                }
            }

            int placedCount = 0;
            int round = 0;

            while (placedCount < context.RoomCount && round < _maxRounds)
            {
                round++;

                // 检查取消
                if (CheckCancellation(token))
                {
                    return false;
                }

                // 打乱可用位置顺序
                ShuffleList(availablePositions, context.RNG);

                // 尝试放置
                int attemptsThisRound = 0;
                var positionsToRemove = new List<Vector2Int>();

                foreach (var pos in availablePositions)
                {
                    if (placedCount >= context.RoomCount)
                        break;

                    if (attemptsThisRound >= _maxAttempts)
                        break;

                    attemptsThisRound++;

                    // 检查是否已被占用
                    if (context.IsOccupied(pos))
                    {
                        positionsToRemove.Add(pos);
                        continue;
                    }

                    // 随机阈值判断
                    float randomValue = context.NextRandomFloat();
                    if (randomValue <= _spawnThreshold)
                    {
                        continue;
                    }

                    // 检查间隔约束（不能正交相邻，也不能对角相邻）
                    if (!CheckSpacingConstraint(context, pos))
                    {
                        continue;
                    }

                    // 放置成功
                    var node = new WorldNode(pos, context.RNG.Next());
                    context.AddNode(node);
                    positionsToRemove.Add(pos);
                    placedCount++;

                    LogInfo($"放置房间 [{placedCount}/{context.RoomCount}] @ {pos}");
                }

                // 移除已使用的位置
                foreach (var pos in positionsToRemove)
                {
                    availablePositions.Remove(pos);
                }

                // 每轮结束后让出一帧
                await YieldFrame(token);
            }

            // 检查是否达到目标
            if (placedCount < context.RoomCount)
            {
                LogWarning($"放置未完成: 只放置了 {placedCount}/{context.RoomCount} 个房间 (轮次: {round})");
                
                // 强制放置剩余房间（忽略阈值，仅检查间隔约束）
                await ForcePlaceRemaining(context, token, placedCount);
            }

            LogInfo($"放置完成: {context.Nodes.Count} 个房间");
            return context.Nodes.Count >= context.RoomCount;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 对角方向（4个对角）
        /// </summary>
        private static readonly Vector2Int[] DiagonalDirections = new Vector2Int[]
        {
            new Vector2Int(1, 1),   // 右上
            new Vector2Int(1, -1),  // 右下
            new Vector2Int(-1, 1),  // 左上
            new Vector2Int(-1, -1)  // 左下
        };

        /// <summary>
        /// 检查间隔约束
        /// 房间之间必须间隔1格（不能正交相邻，也不能对角相邻）
        /// </summary>
        /// <param name="context">世界上下文</param>
        /// <param name="position">检查位置</param>
        /// <returns>是否满足约束</returns>
        private bool CheckSpacingConstraint(WorldContext context, Vector2Int position)
        {
            // 检查正交方向（不能有相邻房间）
            foreach (var dir in OrthogonalDirections)
            {
                var neighbor = position + dir;
                if (context.IsOccupied(neighbor))
                {
                    return false; // 正交方向有相邻房间，不满足约束
                }
            }

            // 检查对角方向（不能有相邻房间）
            foreach (var dir in DiagonalDirections)
            {
                var neighbor = position + dir;
                if (context.IsOccupied(neighbor))
                {
                    return false; // 对角方向有相邻房间，不满足约束
                }
            }

            return true;
        }

        /// <summary>
        /// 强制放置剩余房间
        /// 当随机阈值无法满足目标数量时使用
        /// </summary>
        private async UniTask ForcePlaceRemaining(WorldContext context, CancellationToken token, int currentCount)
        {
            LogInfo($"强制放置剩余 {context.RoomCount - currentCount} 个房间...");

            for (int x = 0; x < context.GridSize && currentCount < context.RoomCount; x++)
            {
                for (int y = 0; y < context.GridSize && currentCount < context.RoomCount; y++)
                {
                    if (CheckCancellation(token))
                        return;

                    var pos = new Vector2Int(x, y);

                    if (context.IsOccupied(pos))
                        continue;

                    if (!CheckSpacingConstraint(context, pos))
                        continue;

                    var node = new WorldNode(pos, context.RNG.Next());
                    context.AddNode(node);
                    currentCount++;

                    LogInfo($"强制放置房间 [{currentCount}/{context.RoomCount}] @ {pos}");
                }
            }

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 打乱列表顺序
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

        #endregion

        #region 验证

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            if (_spawnThreshold < 0 || _spawnThreshold > 1)
            {
                errorMessage = "生成阈值必须在 0-1 之间";
                return false;
            }

            if (_maxAttempts < 10)
            {
                errorMessage = "最大尝试次数至少为 10";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        #endregion
    }
}
