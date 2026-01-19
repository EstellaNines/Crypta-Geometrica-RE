using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 世界生成管线配置
    /// 存储世界生成参数和规则列表
    /// 仿照房间生成器V4的DungeonPipelineData设计
    /// </summary>
    [CreateAssetMenu(
        fileName = "WorldPipeline",
        menuName = "Crypta Geometrica:RE/PCG程序化关卡/V4/World Pipeline")]
    public class WorldPipelineData : ScriptableObject
    {
        #region 基础配置

        [TitleGroup("世界配置")]
        [LabelText("目标房间数量")]
        [Tooltip("决定网格大小为 X×X")]
        [MinValue(1)]
        [MaxValue(20)]
        [SerializeField]
        private int _roomCount = 6;

        [TitleGroup("世界配置")]
        [LabelText("单房间像素尺寸")]
        [Tooltip("每个房间的像素大小")]
        [SerializeField]
        private Vector2Int _roomPixelSize = new Vector2Int(64, 64);

        [TitleGroup("世界配置")]
        [LabelText("房间生成管线")]
        [Tooltip("引用房间生成器V4的管线配置")]
        [Required("必须指定房间生成管线")]
        [SerializeField]
        private DungeonPipelineData _dungeonPipeline;

        #endregion

        #region 调试配置

        [TitleGroup("调试")]
        [LabelText("启用日志")]
        [SerializeField]
        private bool _enableLogging = true;

        #endregion

        #region 规则列表

        [TitleGroup("规则管线")]
        [LabelText("世界生成规则")]
        [Tooltip("按ExecutionOrder顺序执行")]
        [ListDrawerSettings(
            ShowIndexLabels = true,
            ListElementLabelName = "RuleName",
            DraggableItems = true,
            ShowItemCount = true)]
        [SerializeReference]
        private List<IWorldRule> _rules = new List<IWorldRule>();

        #endregion

        #region 公共属性

        /// <summary>
        /// 目标房间数量（决定网格大小 X×X）
        /// </summary>
        public int RoomCount => _roomCount;

        /// <summary>
        /// 单房间像素尺寸
        /// </summary>
        public Vector2Int RoomPixelSize => _roomPixelSize;

        /// <summary>
        /// 房间生成管线引用
        /// </summary>
        public DungeonPipelineData DungeonPipeline => _dungeonPipeline;

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool EnableLogging => _enableLogging;

        /// <summary>
        /// 规则列表
        /// </summary>
        public List<IWorldRule> Rules => _rules;

        /// <summary>
        /// 网格尺寸（房间数量 - 1）
        /// </summary>
        public int GridSize => _roomCount - 1;

        #endregion

        #region 规则管理

        /// <summary>
        /// 获取已启用的规则列表（按ExecutionOrder排序）
        /// </summary>
        /// <returns>排序后的启用规则列表</returns>
        public List<IWorldRule> GetEnabledRules()
        {
            if (_rules == null || _rules.Count == 0)
            {
                return new List<IWorldRule>();
            }

            return _rules
                .Where(r => r != null && r.Enabled)
                .OrderBy(r => r.ExecutionOrder)
                .ToList();
        }

        /// <summary>
        /// 验证所有规则配置
        /// </summary>
        /// <param name="errors">错误信息列表</param>
        /// <returns>是否全部验证通过</returns>
        public bool ValidateAll(out List<string> errors)
        {
            errors = new List<string>();

            // 验证基础配置
            if (_roomCount <= 0)
            {
                errors.Add("房间数量必须大于0");
            }

            if (_roomPixelSize.x <= 0 || _roomPixelSize.y <= 0)
            {
                errors.Add("房间像素尺寸必须大于0");
            }

            if (_dungeonPipeline == null)
            {
                errors.Add("必须指定房间生成管线");
            }

            // 验证规则列表
            if (_rules == null || _rules.Count == 0)
            {
                errors.Add("规则列表不能为空");
            }
            else
            {
                for (int i = 0; i < _rules.Count; i++)
                {
                    var rule = _rules[i];
                    if (rule == null)
                    {
                        errors.Add($"规则[{i}]为空");
                        continue;
                    }

                    if (rule.Validate(out string ruleError) == false)
                    {
                        errors.Add($"规则[{i}] {rule.RuleName}: {ruleError}");
                    }
                }
            }

            return errors.Count == 0;
        }

        #endregion

        #region 编辑器辅助

        [TitleGroup("规则管线")]
        [Button("验证配置", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void ValidateConfiguration()
        {
            if (ValidateAll(out var errors))
            {
                Debug.Log($"[WorldPipeline] ✓ 配置验证通过 ({_rules?.Count ?? 0} 条规则)");
            }
            else
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[WorldPipeline] ✗ {error}");
                }
            }
        }

        [TitleGroup("规则管线")]
        [Button("按顺序排序规则", ButtonSizes.Medium)]
        private void SortRulesByOrder()
        {
            if (_rules == null || _rules.Count == 0) return;

            _rules = _rules
                .Where(r => r != null)
                .OrderBy(r => r.ExecutionOrder)
                .ToList();

            Debug.Log($"[WorldPipeline] 规则已按ExecutionOrder排序");
        }

        #endregion

        #region Unity生命周期

        private void OnValidate()
        {
            // 确保房间数量在合理范围内
            _roomCount = Mathf.Clamp(_roomCount, 1, 20);

            // 确保像素尺寸为正
            _roomPixelSize = new Vector2Int(
                Mathf.Max(1, _roomPixelSize.x),
                Mathf.Max(1, _roomPixelSize.y)
            );
        }

        #endregion
    }
}
