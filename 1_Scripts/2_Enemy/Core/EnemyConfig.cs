using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.Enemy
{
    /// <summary>
    /// 敌人配置 ScriptableObject
    /// 定义敌人的基础属性和行为参数
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Crypta Geometrica:RE/Enemy/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        #region Basic Settings

        [BoxGroup("基础设置")]
        [LabelText("敌人类型")]
        [SerializeField]
        private EnemyType _enemyType = EnemyType.Ground;

        [BoxGroup("基础设置")]
        [LabelText("最大生命值")]
        [MinValue(1)]
        [SerializeField]
        private int _maxHealth = 2;

        [BoxGroup("基础设置")]
        [LabelText("移动速度")]
        [MinValue(0)]
        [SerializeField]
        private float _moveSpeed = 2f;

        [BoxGroup("基础设置")]
        [LabelText("击退力度")]
        [MinValue(0)]
        [SerializeField]
        private float _knockbackForce = 3f;

        #endregion

        #region Patrol Settings

        [BoxGroup("巡逻设置")]
        [LabelText("巡逻时间范围")]
        [MinMaxSlider(0.5f, 10f, true)]
        [SerializeField]
        private Vector2 _patrolDurationRange = new Vector2(2f, 5f);

        [BoxGroup("巡逻设置")]
        [LabelText("待机时间范围")]
        [MinMaxSlider(0.5f, 5f, true)]
        [SerializeField]
        private Vector2 _idleDurationRange = new Vector2(1f, 3f);

        #endregion

        #region Hurt Settings

        [BoxGroup("受伤设置")]
        [LabelText("闪烁持续时间")]
        [MinValue(0.1f)]
        [SerializeField]
        private float _hurtFlashDuration = 0.5f;

        [BoxGroup("受伤设置")]
        [LabelText("闪烁间隔")]
        [MinValue(0.05f)]
        [SerializeField]
        private float _flashInterval = 0.1f;

        #endregion

        #region Death Settings

        [BoxGroup("死亡设置")]
        [LabelText("死亡延迟时间")]
        [MinValue(0)]
        [SerializeField]
        private float _deathDelay = 2f;

        [BoxGroup("死亡设置")]
        [LabelText("淡出持续时间")]
        [MinValue(0.1f)]
        [SerializeField]
        private float _fadeDuration = 0.5f;

        #endregion

        #region Public Properties

        /// <summary>
        /// 敌人类型
        /// </summary>
        public EnemyType EnemyType => _enemyType;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth => _maxHealth;

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>
        /// 击退力度
        /// </summary>
        public float KnockbackForce => _knockbackForce;

        /// <summary>
        /// 巡逻时间范围（X=最小，Y=最大）
        /// </summary>
        public Vector2 PatrolDurationRange => _patrolDurationRange;

        /// <summary>
        /// 待机时间范围（X=最小，Y=最大）
        /// </summary>
        public Vector2 IdleDurationRange => _idleDurationRange;

        /// <summary>
        /// 受伤闪烁持续时间
        /// </summary>
        public float HurtFlashDuration => _hurtFlashDuration;

        /// <summary>
        /// 闪烁间隔
        /// </summary>
        public float FlashInterval => _flashInterval;

        /// <summary>
        /// 死亡延迟时间
        /// </summary>
        public float DeathDelay => _deathDelay;

        /// <summary>
        /// 淡出持续时间
        /// </summary>
        public float FadeDuration => _fadeDuration;

        #endregion

        #region Helper Methods

        /// <summary>
        /// 获取随机巡逻时间
        /// </summary>
        public float GetRandomPatrolDuration()
        {
            return Random.Range(_patrolDurationRange.x, _patrolDurationRange.y);
        }

        /// <summary>
        /// 获取随机待机时间
        /// </summary>
        public float GetRandomIdleDuration()
        {
            return Random.Range(_idleDurationRange.x, _idleDurationRange.y);
        }

        #endregion
    }
}
