using UnityEngine;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace CryptaGeometrica.Enemy.States
{
    /// <summary>
    /// 受伤状态
    /// 敌人受伤后进入此状态，执行红白闪烁效果
    /// 闪烁结束后根据生命值决定回到待机或进入死亡
    /// </summary>
    [System.Serializable]
    public class HurtState : EnemyStateBase
    {
        #region Configuration

        [TitleGroup("受伤设置")]
        [LabelText("应用击退效果")]
        [SerializeField]
        private bool _applyKnockback = true;

        [TitleGroup("受伤设置")]
        [LabelText("击退力度倍率")]
        [MinValue(0.1f)]
        [SerializeField]
        private float _knockbackMultiplier = 1f;

        #endregion

        #region Runtime

        private CancellationTokenSource _cts;

        #endregion

        #region Constructor

        public HurtState()
        {
            _stateName = EnemyController.STATE_HURT;
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            LogInfo("Entered Hurt State");

            // 取消之前的闪烁任务（如果有）
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // 停止移动
            if (enemy != null)
            {
                enemy.SetVelocityX(0f);
            }

            // 应用击退效果
            if (_applyKnockback && enemy != null)
            {
                float knockbackForce = enemy.Config != null
                    ? enemy.Config.KnockbackForce * _knockbackMultiplier
                    : 3f;

                Vector2 knockbackDir = enemy.LastDamageDirection;
                knockbackDir.y = 0.3f; // 轻微向上击退
                enemy.ApplyKnockback(knockbackDir.normalized, knockbackForce);
            }

            // 开始闪烁效果
            FlashEffectAsync(_cts.Token).Forget();
        }

        public override void Exit()
        {
            LogInfo("Exited Hurt State");

            // 取消闪烁任务
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // 恢复正常颜色
            if (enemy?.SpriteRenderer != null)
            {
                enemy.SpriteRenderer.color = Color.white;
            }
        }

        /// <summary>
        /// 受伤状态不可被打断（除非死亡）
        /// </summary>
        public override bool CanBeInterrupted() => false;

        #endregion

        #region Flash Effect

        /// <summary>
        /// 红白闪烁效果（异步）
        /// </summary>
        private async UniTaskVoid FlashEffectAsync(CancellationToken token)
        {
            if (enemy?.SpriteRenderer == null || enemy?.Config == null)
            {
                TransitionAfterHurt();
                return;
            }

            float flashDuration = enemy.Config.HurtFlashDuration;
            float flashInterval = enemy.Config.FlashInterval;
            float elapsed = 0f;
            bool isRed = false;

            try
            {
                // 红白闪烁
                while (elapsed < flashDuration)
                {
                    token.ThrowIfCancellationRequested();

                    enemy.SpriteRenderer.color = isRed ? Color.white : Color.red;
                    isRed = !isRed;

                    await UniTask.Delay(
                        (int)(flashInterval * 1000),
                        cancellationToken: token
                    );

                    elapsed += flashInterval;
                }

                // 恢复正常颜色
                enemy.SpriteRenderer.color = Color.white;

                // 切换状态
                TransitionAfterHurt();
            }
            catch (System.OperationCanceledException)
            {
                // 任务被取消，不做处理
            }
        }

        /// <summary>
        /// 受伤结束后的状态切换
        /// </summary>
        private void TransitionAfterHurt()
        {
            if (enemy == null || stateMachine == null) return;

            // 检查是否死亡
            if (enemy.IsDead)
            {
                if (stateMachine.HasState(EnemyController.STATE_DEATH))
                {
                    ChangeState(EnemyController.STATE_DEATH);
                }
            }
            else
            {
                // 返回待机状态
                if (stateMachine.HasState(EnemyController.STATE_IDLE))
                {
                    ChangeState(EnemyController.STATE_IDLE);
                }
            }
        }

        #endregion
    }
}
