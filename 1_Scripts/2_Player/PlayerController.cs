using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 玩家主控制器
/// 负责输入处理、组件引用、状态机驱动、地面检测
/// </summary>
public class PlayerController : MonoBehaviour, PlayerInputSystem.IGamePlayActions
{
    #region 组件引用

    [Header("组件引用")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D capsuleCollider;

    public Animator Animator => animator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public Rigidbody2D Rb => rb;

    #endregion

    #region 玩家属性

    [Header("移动属性")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("地面检测")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("生命值")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("受伤属性")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private float invincibleDuration = 1f;

    public float MoveSpeed => moveSpeed;
    public float JumpForce => jumpForce;
    public float KnockbackForce => knockbackForce;
    public float FlashDuration => flashDuration;
    public float InvincibleDuration => invincibleDuration;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    #endregion

    #region 输入状态

    public Vector2 MoveInput { get; private set; }
    public bool JumpInputPressed { get; private set; }
    public bool AttackInputPressed { get; private set; }

    #endregion

    #region 玩家状态

    public bool IsGrounded { get; private set; }
    public bool IsFacingRight { get; private set; } = true;
    public bool IsInvincible { get; set; }
    public int JumpCount { get; set; }
    public int MaxJumpCount => 2;

    #endregion

    #region 状态机

    public PlayerStateMachine StateMachine { get; private set; }

    // 所有状态实例
    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerHurtState HurtState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }

    #endregion

    #region 输入系统

    private PlayerInputSystem inputSystem;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 自动获取组件
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider2D>();

        // 初始化输入系统
        inputSystem = new PlayerInputSystem();
        inputSystem.GamePlay.AddCallbacks(this);

        // 初始化状态机
        StateMachine = new PlayerStateMachine();

        // 创建状态实例
        IdleState = new PlayerIdleState(this, StateMachine);
        WalkState = new PlayerWalkState(this, StateMachine);
        JumpState = new PlayerJumpState(this, StateMachine);
        AttackState = new PlayerAttackState(this, StateMachine);
        HurtState = new PlayerHurtState(this, StateMachine);
        DeadState = new PlayerDeadState(this, StateMachine);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        StateMachine.Initialize(IdleState);
    }

    private void OnEnable()
    {
        inputSystem?.Enable();
    }

    private void OnDisable()
    {
        inputSystem?.Disable();
    }

    private void Update()
    {
        CheckGround();
        StateMachine.Update();

        // 消费一次性输入
        JumpInputPressed = false;
        AttackInputPressed = false;
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    #endregion

    #region 输入回调

    public void OnMovement(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            AttackInputPressed = true;
        }
    }

    public void OnOperate(InputAction.CallbackContext context)
    {
        // 预留交互功能
    }

    #endregion

    #region 地面检测

    /// <summary>
    /// 使用胶囊碰撞体进行地面检测
    /// </summary>
    private void CheckGround()
    {
        if (capsuleCollider == null) return;

        Vector2 origin = (Vector2)transform.position + capsuleCollider.offset;
        Vector2 size = capsuleCollider.size * 0.9f;
        float angle = 0f;
        Vector2 direction = Vector2.down;

        RaycastHit2D hit = Physics2D.CapsuleCast(
            origin, size, capsuleCollider.direction,
            angle, direction, groundCheckDistance, groundLayer
        );

        bool wasGrounded = IsGrounded;
        IsGrounded = hit.collider != null;

        // 落地时重置跳跃次数
        if (!wasGrounded && IsGrounded)
        {
            JumpCount = 0;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置水平移动
    /// </summary>
    public void SetVelocityX(float velocityX)
    {
        rb.velocity = new Vector2(velocityX, rb.velocity.y);
    }

    /// <summary>
    /// 设置垂直速度（跳跃）
    /// </summary>
    public void SetVelocityY(float velocityY)
    {
        rb.velocity = new Vector2(rb.velocity.x, velocityY);
    }

    /// <summary>
    /// 翻转角色朝向
    /// </summary>
    public void Flip(float direction)
    {
        if (direction > 0 && !IsFacingRight)
        {
            IsFacingRight = true;
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (direction < 0 && IsFacingRight)
        {
            IsFacingRight = false;
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    /// <summary>
    /// 检查跳跃输入（W键或上箭头）
    /// </summary>
    public bool CheckJumpInput()
    {
        return MoveInput.y > 0.5f;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(Vector2 damageSource)
    {
        if (IsInvincible || currentHealth <= 0) return;

        // 计算击退方向
        Vector2 knockbackDir = ((Vector2)transform.position - damageSource).normalized;
        if (knockbackDir.x == 0) knockbackDir.x = IsFacingRight ? -1 : 1;

        // 切换到受伤状态
        if (StateMachine.CurrentState.CanBeHurt())
        {
            HurtState.SetKnockbackDirection(knockbackDir);
            StateMachine.ChangeState(HurtState);
        }
    }

    /// <summary>
    /// 扣除生命值
    /// </summary>
    public void ReduceHealth()
    {
        currentHealth--;
        Debug.Log($"[Player] 生命值: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            StateMachine.ChangeState(DeadState);
        }
    }

    /// <summary>
    /// 检查是否死亡
    /// </summary>
    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    #endregion
}
