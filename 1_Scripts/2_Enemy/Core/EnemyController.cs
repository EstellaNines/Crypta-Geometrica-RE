using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.Enemy
{
    /// <summary>
    /// 敌人控制器
    /// 采用列表装载模式，在 Inspector 中配置状态列表
    /// 实现 IDamageable 接口以响应玩家攻击
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        #region Configuration

        [BoxGroup("配置")]
        [LabelText("敌人配置")]
        [Required("必须指定敌人配置！")]
        [SerializeField]
        private EnemyConfig _config;

        [BoxGroup("配置")]
        [LabelText("状态列表")]
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = true)]
        private List<EnemyStateBase> _loadedStates = new List<EnemyStateBase>();

        #endregion

        #region Debug

        [BoxGroup("调试")]
        [LabelText("启用日志")]
        [SerializeField]
        private bool _enableLogging = false;

        [BoxGroup("调试")]
        [LabelText("当前状态")]
        [ShowInInspector, ReadOnly]
        private string CurrentStateName => _stateMachine?.CurrentStateName ?? "None";

        [BoxGroup("调试")]
        [LabelText("当前生命值")]
        [ShowInInspector, ReadOnly]
        private int _currentHealth;

        #endregion

        #region Cached Components

        /// <summary>
        /// Sprite 渲染器
        /// </summary>
        public SpriteRenderer SpriteRenderer { get; private set; }

        /// <summary>
        /// 动画控制器
        /// </summary>
        public Animator Animator { get; private set; }

        /// <summary>
        /// 刚体组件
        /// </summary>
        public Rigidbody2D Rb { get; private set; }

        /// <summary>
        /// 碰撞器组件
        /// </summary>
        public Collider2D Collider { get; private set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// 敌人配置
        /// </summary>
        public EnemyConfig Config => _config;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>
        /// 是否已死亡
        /// </summary>
        public bool IsDead => _currentHealth <= 0;

        /// <summary>
        /// 状态机引用（用于状态间切换）
        /// </summary>
        public EnemyStateMachine StateMachine => _stateMachine;

        /// <summary>
        /// 最后一次受伤的来源方向
        /// </summary>
        public Vector2 LastDamageDirection { get; private set; }

        #endregion

        #region Private Fields

        private EnemyStateMachine _stateMachine;

        #endregion

        #region Constants - State Names

        /// <summary>
        /// 待机状态名称
        /// </summary>
        public const string STATE_IDLE = "Idle";

        /// <summary>
        /// 巡逻状态名称
        /// </summary>
        public const string STATE_PATROL = "Patrol";

        /// <summary>
        /// 攻击状态名称
        /// </summary>
        public const string STATE_ATTACK = "Attack";

        /// <summary>
        /// 受伤状态名称
        /// </summary>
        public const string STATE_HURT = "Hurt";

        /// <summary>
        /// 死亡状态名称
        /// </summary>
        public const string STATE_DEATH = "Death";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 缓存组件
            CacheComponents();

            // 防御性检查
            ValidateReferences();

            // 初始化生命值
            _currentHealth = _config != null ? _config.MaxHealth : 1;

            // 初始化状态机
            InitializeStateMachine();
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 缓存组件引用
        /// </summary>
        private void CacheComponents()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
            if (SpriteRenderer == null) SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

            Animator = GetComponent<Animator>();
            if (Animator == null) Animator = GetComponentInChildren<Animator>();

            Rb = GetComponent<Rigidbody2D>();
            Collider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// 验证必要引用
        /// </summary>
        private void ValidateReferences()
        {
            if (_config == null)
            {
                Debug.LogError($"[{nameof(EnemyController)}] Critical: EnemyConfig is missing on {gameObject.name}!");
            }

            if (SpriteRenderer == null)
            {
                Debug.LogError($"[{nameof(EnemyController)}] Critical: SpriteRenderer is missing on {gameObject.name}!");
            }

            if (_loadedStates == null || _loadedStates.Count == 0)
            {
                Debug.LogError($"[{nameof(EnemyController)}] Critical: No states loaded on {gameObject.name}!");
            }
        }

        /// <summary>
        /// 初始化状态机
        /// </summary>
        private void InitializeStateMachine()
        {
            _stateMachine = new EnemyStateMachine();
            _stateMachine.Initialize(this, _loadedStates, _enableLogging);
        }

        #endregion

        #region IDamageable Implementation

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源位置</param>
        public void TakeDamage(int damage, Vector2 damageSource)
        {
            if (IsDead) return;

            // 检查当前状态是否可被打断
            if (_stateMachine.CurrentState != null && !_stateMachine.CurrentState.CanBeInterrupted())
            {
                if (_enableLogging)
                {
                    Debug.Log($"[EnemyController] {gameObject.name} cannot be interrupted in state: {CurrentStateName}");
                }
                return;
            }

            // 计算伤害方向
            LastDamageDirection = ((Vector2)transform.position - damageSource).normalized;

            // 扣除生命值
            _currentHealth -= damage;

            if (_enableLogging)
            {
                Debug.Log($"[EnemyController] {gameObject.name} took {damage} damage. Health: {_currentHealth}/{_config.MaxHealth}");
            }

            // 切换到受伤或死亡状态
            if (IsDead)
            {
                if (_stateMachine.HasState(STATE_DEATH))
                {
                    _stateMachine.ChangeState(STATE_DEATH);
                }
            }
            else
            {
                if (_stateMachine.HasState(STATE_HURT))
                {
                    _stateMachine.ChangeState(STATE_HURT);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置水平速度
        /// </summary>
        /// <param name="velocityX">水平速度</param>
        public void SetVelocityX(float velocityX)
        {
            if (Rb != null)
            {
                Rb.velocity = new Vector2(velocityX, Rb.velocity.y);
            }
        }

        /// <summary>
        /// 设置速度
        /// </summary>
        /// <param name="velocity">速度向量</param>
        public void SetVelocity(Vector2 velocity)
        {
            if (Rb != null)
            {
                Rb.velocity = velocity;
            }
        }

        /// <summary>
        /// 应用击退效果
        /// </summary>
        /// <param name="direction">击退方向</param>
        /// <param name="force">击退力度（可选，默认使用配置值）</param>
        public void ApplyKnockback(Vector2 direction, float? force = null)
        {
            if (Rb == null || _config == null) return;

            float knockbackForce = force ?? _config.KnockbackForce;
            Rb.velocity = direction * knockbackForce;
        }

        /// <summary>
        /// 切换朝向（翻转 Sprite）
        /// </summary>
        /// <param name="faceRight">是否朝右</param>
        public void SetFacingDirection(bool faceRight)
        {
            if (SpriteRenderer != null)
            {
                SpriteRenderer.flipX = !faceRight;
            }
        }

        /// <summary>
        /// 禁用碰撞器（用于死亡状态）
        /// </summary>
        public void DisableCollider()
        {
            if (Collider != null)
            {
                Collider.enabled = false;
            }
        }

        /// <summary>
        /// 销毁敌人对象
        /// </summary>
        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Editor Buttons

#if UNITY_EDITOR
        [BoxGroup("调试")]
        [Button("测试受伤")]
        private void TestTakeDamage()
        {
            TakeDamage(1, transform.position + Vector3.left);
        }

        [BoxGroup("调试")]
        [Button("测试死亡")]
        private void TestDeath()
        {
            _currentHealth = 1;
            TakeDamage(1, transform.position + Vector3.left);
        }
#endif

        #endregion
    }
}
