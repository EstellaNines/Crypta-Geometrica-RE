using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 世界生成规则基类
    /// 提供通用功能实现和日志辅助方法
    /// 仿照房间生成器V4的GeneratorRuleBase设计
    /// </summary>
    [Serializable]
    public abstract class WorldRuleBase : IWorldRule
    {
        #region 基础配置

        [TitleGroup("基础设置")]
        [LabelText("规则名称")]
        [SerializeField]
        protected string _ruleName = "Unnamed Rule";

        [TitleGroup("基础设置")]
        [LabelText("启用")]
        [SerializeField]
        protected bool _enabled = true;

        [TitleGroup("基础设置")]
        [LabelText("执行顺序")]
        [Tooltip("越小越先执行")]
        [SerializeField]
        protected int _executionOrder = 100;

        [TitleGroup("调试")]
        [LabelText("启用日志")]
        [SerializeField]
        protected bool _enableLogging = true;

        #endregion

        #region IWorldRule 实现

        /// <inheritdoc/>
        public virtual string RuleName => _ruleName;

        /// <inheritdoc/>
        public virtual bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <inheritdoc/>
        public virtual int ExecutionOrder => _executionOrder;

        /// <inheritdoc/>
        public abstract UniTask<bool> ExecuteAsync(WorldContext context, CancellationToken token);

        /// <inheritdoc/>
        public virtual bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }

        #endregion

        #region 日志辅助方法

        /// <summary>
        /// 输出信息日志
        /// </summary>
        /// <param name="message">日志内容</param>
        protected void LogInfo(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[{RuleName}] {message}");
            }
        }

        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="message">日志内容</param>
        protected void LogWarning(string message)
        {
            if (_enableLogging)
            {
                Debug.LogWarning($"[{RuleName}] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">日志内容</param>
        protected void LogError(string message)
        {
            Debug.LogError($"[{RuleName}] {message}");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查取消请求
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>是否已取消</returns>
        protected bool CheckCancellation(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                LogWarning("Generation cancelled.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 安全等待一帧（避免阻塞主线程）
        /// </summary>
        /// <param name="token">取消令牌</param>
        protected async UniTask YieldFrame(CancellationToken token)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        #endregion
    }
}
