using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.Enemy.States
{
    /// <summary>
    /// 待机状态
    /// 敌人静止不动，播放待机动画
    /// 可配置自动切换到巡逻状态
    /// </summary>
    [System.Serializable]
    public class IdleState : EnemyStateBase
    {
        #region Configuration

        [TitleGroup("待机设置")]
        [LabelText("待机动画名称")]
        [SerializeField]
        private string _idleAnimationName = "Idle";

        [TitleGroup("待机设置")]
        [LabelText("自动切换到巡逻")]
        [SerializeField]
        private bool _autoTransitionToPatrol = true;

        #endregion

        #region Runtime

        private float _timer;
        private float _idleDuration;

        #endregion

        #region Constructor

        public IdleState()
        {
            _stateName = EnemyController.STATE_IDLE;
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            LogInfo("Entered Idle State");

            // 播放待机动画
            PlayAnimation(_idleAnimationName);

            // 停止移动
            if (enemy != null)
            {
                enemy.SetVelocityX(0f);
            }

            // 计算待机时间
            if (_autoTransitionToPatrol && enemy?.Config != null)
            {
                _idleDuration = enemy.Config.GetRandomIdleDuration();
                _timer = 0f;
            }
        }

        public override void Update()
        {
            if (!_autoTransitionToPatrol) return;

            _timer += Time.deltaTime;

            // 待机时间到，切换到巡逻状态
            if (_timer >= _idleDuration)
            {
                if (stateMachine.HasState(EnemyController.STATE_PATROL))
                {
                    ChangeState(EnemyController.STATE_PATROL);
                }
            }
        }

        public override void Exit()
        {
            LogInfo("Exited Idle State");
        }

        #endregion
    }
}
