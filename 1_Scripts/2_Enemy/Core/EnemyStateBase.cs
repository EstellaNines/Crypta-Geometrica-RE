using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.Enemy
{
    /// <summary>
    /// 敌人状态基类
    /// 提供通用功能实现和辅助方法
    /// </summary>
    [System.Serializable]
    public abstract class EnemyStateBase : IEnemyState
    {
        #region Configuration

        [TitleGroup("状态设置")]
        [LabelText("状态名称")]
        [SerializeField]
        protected string _stateName = "Unnamed State";

        [TitleGroup("调试")]
        [LabelText("启用日志")]
        [SerializeField]
        protected bool _enableLogging = false;

        #endregion

        #region References

        /// <summary>
        /// 敌人控制器引用
        /// </summary>
        protected EnemyController enemy;

        /// <summary>
        /// 状态机引用
        /// </summary>
        protected EnemyStateMachine stateMachine;

        #endregion

        #region IEnemyState Implementation

        /// <inheritdoc/>
        public virtual string StateName => _stateName;

        /// <inheritdoc/>
        public virtual void Initialize(EnemyController enemy, EnemyStateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        /// <inheritdoc/>
        public virtual void Enter() { }

        /// <inheritdoc/>
        public virtual void Update() { }

        /// <inheritdoc/>
        public virtual void FixedUpdate() { }

        /// <inheritdoc/>
        public virtual void Exit() { }

        /// <inheritdoc/>
        public virtual bool CanBeInterrupted() => true;

        #endregion

        #region Helper Methods

        /// <summary>
        /// 播放动画（直接调用 Animator.Play）
        /// </summary>
        /// <param name="animName">动画名称</param>
        protected void PlayAnimation(string animName)
        {
            if (enemy == null || enemy.Animator == null)
            {
                LogWarning($"Cannot play animation '{animName}': Animator is null");
                return;
            }
            enemy.Animator.Play(animName);
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        /// <param name="stateName">目标状态名称</param>
        protected void ChangeState(string stateName)
        {
            stateMachine?.ChangeState(stateName);
        }

        /// <summary>
        /// 输出信息日志
        /// </summary>
        /// <param name="message">日志内容</param>
        protected void LogInfo(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[{StateName}] {message}");
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
                Debug.LogWarning($"[{StateName}] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">日志内容</param>
        protected void LogError(string message)
        {
            Debug.LogError($"[{StateName}] {message}");
        }

        #endregion
    }
}
