using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 坐标计算规则
    /// 为每个WorldNode计算世界像素坐标偏移
    /// 公式: WorldPixelOffset = GridPosition × RoomPixelSize
    /// </summary>
    [Serializable]
    public class CoordinateCalcRule : WorldRuleBase
    {
        #region 构造函数

        public CoordinateCalcRule()
        {
            _ruleName = "坐标计算规则";
            _executionOrder = 20;
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
                LogWarning("No nodes to calculate coordinates for");
                return true; // 没有节点不算失败
            }

            LogInfo($"开始计算坐标 ({context.Nodes.Count} 个节点)");

            var roomPixelSize = context.RoomPixelSize;

            foreach (var node in context.Nodes)
            {
                if (CheckCancellation(token))
                {
                    return false;
                }

                // 计算世界像素偏移
                // WorldPixelOffset = GridPosition × RoomPixelSize
                node.WorldPixelOffset = new Vector2Int(
                    node.GridPosition.x * roomPixelSize.x,
                    node.GridPosition.y * roomPixelSize.y
                );

                LogInfo($"节点 {node.GridPosition} -> 世界坐标 {node.WorldPixelOffset}");
            }

            LogInfo($"坐标计算完成");

            await UniTask.CompletedTask;
            return true;
        }

        #endregion

        #region 验证

        /// <inheritdoc/>
        public override bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }

        #endregion
    }
}
