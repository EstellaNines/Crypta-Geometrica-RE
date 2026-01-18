using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 地牢生成管线配置
    /// 使用SerializeReference支持多态规则列表
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonPipeline", menuName = "Crypta Geometrica:RE/PCG程序化关卡/V4/Dungeon Pipeline")]
    public class DungeonPipelineData : ScriptableObject
    {
        #region 基础配置

        [TitleGroup("网格配置")]
        [LabelText("网格列数")]
        [Range(2, 10)]
        [Tooltip("水平方向的房间数量")]
        public int GridColumns = 4;

        [TitleGroup("网格配置")]
        [LabelText("网格行数")]
        [Range(2, 10)]
        [Tooltip("垂直方向的房间数量")]
        public int GridRows = 4;

        [TitleGroup("网格配置")]
        [LabelText("房间尺寸")]
        [Tooltip("单个房间的像素尺寸")]
        public Vector2Int RoomSize = new Vector2Int(64, 64);

        #endregion

        #region 生成规则

        [TitleGroup("生成规则")]
        [LabelText("规则列表")]
        [SerializeReference]
        [ListDrawerSettings(
            ShowFoldout = true,
            DraggableItems = true,
            ShowItemCount = true,
            HideAddButton = false,
            HideRemoveButton = false
        )]
        [InfoBox("规则按执行顺序排列，可拖拽调整顺序")]
        public List<IGeneratorRule> Rules = new List<IGeneratorRule>();

        #endregion

        #region 调试选项

        [TitleGroup("调试选项")]
        [LabelText("启用日志")]
        [Tooltip("是否在控制台输出生成日志")]
        public bool EnableLogging = true;

        [TitleGroup("调试选项")]
        [LabelText("可视化调试")]
        [Tooltip("是否在Scene视图显示调试信息")]
        public bool EnableVisualization = false;

        #endregion

        #region 计算属性

        /// <summary>
        /// 地图总宽度（像素）
        /// </summary>
        public int TotalWidth => GridColumns * RoomSize.x;

        /// <summary>
        /// 地图总高度（像素）
        /// </summary>
        public int TotalHeight => GridRows * RoomSize.y;

        /// <summary>
        /// 总房间数量
        /// </summary>
        public int TotalRooms => GridColumns * GridRows;

        #endregion

        #region 方法

        /// <summary>
        /// 验证所有规则配置
        /// </summary>
        /// <param name="errors">错误信息列表</param>
        /// <returns>是否全部有效</returns>
        public bool ValidateAll(out List<string> errors)
        {
            errors = new List<string>();

            if (Rules == null || Rules.Count == 0)
            {
                errors.Add("规则列表为空");
                return false;
            }

            foreach (var rule in Rules)
            {
                if (rule == null)
                {
                    errors.Add("存在空规则引用");
                    continue;
                }

                if (!rule.Validate(out string msg))
                {
                    errors.Add($"[{rule.RuleName}] {msg}");
                }
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// 获取已启用的规则（按执行顺序排序）
        /// </summary>
        /// <returns>启用的规则列表</returns>
        public List<IGeneratorRule> GetEnabledRules()
        {
            var result = new List<IGeneratorRule>();

            if (Rules == null) return result;

            foreach (var rule in Rules)
            {
                if (rule != null && rule.Enabled)
                {
                    result.Add(rule);
                }
            }

            result.Sort((a, b) => a.ExecutionOrder.CompareTo(b.ExecutionOrder));
            return result;
        }

        #endregion

        #region 编辑器按钮

#if UNITY_EDITOR
        [TitleGroup("工具")]
        [Button("验证配置", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void ValidateInEditor()
        {
            if (ValidateAll(out var errors))
            {
                Debug.Log("[DungeonPipelineData] 配置验证通过");
            }
            else
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[DungeonPipelineData] {error}");
                }
            }
        }

        [TitleGroup("工具")]
        [Button("清空规则", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        private void ClearRulesInEditor()
        {
            if (UnityEditor.EditorUtility.DisplayDialog(
                "确认清空",
                "确定要清空所有规则吗？此操作不可撤销。",
                "确定",
                "取消"))
            {
                Rules.Clear();
                Debug.Log("[DungeonPipelineData] 规则已清空");
            }
        }
#endif

        #endregion
    }
}
