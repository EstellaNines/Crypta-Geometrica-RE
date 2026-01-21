using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace CryptaGeometrica.Enemy.States
{
    /// <summary>
    /// 死亡状态
    /// 敌人死亡后执行：变黑 -> 禁用碰撞 -> 延迟 -> 淡出消失
    /// </summary>
    [System.Serializable]
    public class DeathState : EnemyStateBase
    {
        #region Configuration

        [TitleGroup("死亡设置")]
        [LabelText("死亡颜色")]
        [SerializeField]
        private Color _deathColor = Color.black;

        #endregion

        #region Runtime

        private Tween _fadeTween;

        #endregion

        #region Constructor

        public DeathState()
        {
            _stateName = EnemyController.STATE_DEATH;
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            LogInfo("Entered Death State");

            if (enemy == null) return;

            // 1. Sprite 变为黑色
            if (enemy.SpriteRenderer != null)
            {
                enemy.SpriteRenderer.color = _deathColor;
            }

            // 2. 禁用碰撞器
            enemy.DisableCollider();

            // 3. 停止移动
            enemy.SetVelocity(Vector2.zero);

            // 4. 配置延迟和淡出参数
            float deathDelay = enemy.Config != null ? enemy.Config.DeathDelay : 2f;
            float fadeDuration = enemy.Config != null ? enemy.Config.FadeDuration : 0.5f;

            // 5. 延迟后淡出消失
            StartFadeOut(deathDelay, fadeDuration);
        }

        public override void Exit()
        {
            LogInfo("Exited Death State");

            // 清理 Tween
            _fadeTween?.Kill();
            _fadeTween = null;
        }

        /// <summary>
        /// 死亡状态不可被打断
        /// </summary>
        public override bool CanBeInterrupted() => false;

        #endregion

        #region Fade Out

        /// <summary>
        /// 开始淡出效果
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="duration">淡出持续时间</param>
        private void StartFadeOut(float delay, float duration)
        {
            if (enemy?.SpriteRenderer == null) return;

            // 创建淡出序列
            Sequence sequence = DOTween.Sequence();

            // 延迟
            sequence.AppendInterval(delay);

            // 淡出（透明度从当前值到0）
            sequence.Append(
                enemy.SpriteRenderer
                    .DOFade(0f, duration)
                    .SetEase(Ease.OutQuad)
            );

            // 淡出完成后销毁对象
            sequence.OnComplete(() =>
            {
                LogInfo("Death fade complete, destroying enemy");
                enemy.DestroySelf();
            });

            // 绑定到 GameObject 生命周期
            sequence.SetLink(enemy.gameObject);

            _fadeTween = sequence;
        }

        #endregion
    }
}
