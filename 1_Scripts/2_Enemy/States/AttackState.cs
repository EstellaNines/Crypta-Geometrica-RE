using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.Enemy.States
{
    /// <summary>
    /// 攻击状态（骨架）
    /// 预留攻击逻辑框架，暂不实现具体攻击行为
    /// </summary>
    [System.Serializable]
    public class AttackState : EnemyStateBase
    {
        #region Configuration

        [TitleGroup("攻击设置")]
        [LabelText("攻击动画名称")]
        [SerializeField]
        private string _attackAnimationName = "Attack";

        [TitleGroup("攻击设置")]
        [LabelText("攻击持续时间")]
        [MinValue(0.1f)]
        [SerializeField]
        private float _attackDuration = 1f;

        [TitleGroup("攻击设置")]
        [LabelText("攻击冷却时间")]
        [MinValue(0)]
        [SerializeField]
        private float _attackCooldown = 0.5f;

        #endregion

        #region Runtime

        private float _timer;

        #endregion

        #region Constructor

        public AttackState()
        {
            _stateName = EnemyController.STATE_ATTACK;
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            LogInfo("Entered Attack State");

            // 播放攻击动画
            PlayAnimation(_attackAnimationName);

            // 停止移动
            if (enemy != null)
            {
                enemy.SetVelocityX(0f);
            }

            _timer = 0f;

            // TODO: 实现具体攻击逻辑
            // 例如：检测攻击范围内的玩家、造成伤害等
        }

        public override void Update()
        {
            _timer += Time.deltaTime;

            // 攻击动画结束后返回待机状态
            if (_timer >= _attackDuration)
            {
                if (stateMachine.HasState(EnemyController.STATE_IDLE))
                {
                    ChangeState(EnemyController.STATE_IDLE);
                }
            }

            // TODO: 实现攻击判定逻辑
        }

        public override void Exit()
        {
            LogInfo("Exited Attack State");

            // TODO: 清理攻击状态
        }

        /// <summary>
        /// 攻击状态可被打断（受伤/死亡优先级更高）
        /// </summary>
        public override bool CanBeInterrupted() => true;

        #endregion

        #region Attack Logic (TODO)

        // /// <summary>
        // /// 执行攻击判定
        // /// </summary>
        // private void PerformAttack()
        // {
        //     // 检测攻击范围内的玩家
        //     // 对玩家造成伤害
        // }

        #endregion
    }
}
