using UnityEngine;

/// <summary>
/// 玩家攻击判定处理器
/// 通过动画事件触发，使用OverlapBox检测攻击范围内的敌人
/// </summary>
public class PlayerAttackHandler : MonoBehaviour
{
    #region 攻击配置

    [Header("攻击范围")]
    [SerializeField] private Vector2 attackOffset = new Vector2(0.5f, 0);
    [SerializeField] private Vector2 attackSize = new Vector2(1f, 1f);

    [Header("攻击属性")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int attackDamage = 1;

    [Header("调试")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.5f);

    #endregion

    #region 组件引用

    private PlayerController playerController;

    #endregion

    #region 生命周期

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    #endregion

    #region 攻击方法

    /// <summary>
    /// 执行攻击判定（由动画事件调用）
    /// </summary>
    public void PerformAttack()
    {
        Vector2 attackCenter = GetAttackCenter();

        // OverlapBox检测敌人
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackCenter,
            attackSize,
            0f,
            enemyLayer
        );

        // 对检测到的敌人造成伤害
        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage, transform.position);
                Debug.Log($"[Attack] 命中: {hit.name}, 伤害: {attackDamage}");
            }
        }

        if (hits.Length == 0)
        {
            Debug.Log("[Attack] 未命中任何目标");
        }
    }

    /// <summary>
    /// 获取攻击范围中心点（考虑朝向）
    /// </summary>
    private Vector2 GetAttackCenter()
    {
        Vector2 offset = attackOffset;

        // 根据朝向翻转X偏移
        if (playerController != null && !playerController.IsFacingRight)
        {
            offset.x = -offset.x;
        }

        return (Vector2)transform.position + offset;
    }

    #endregion

    #region 编辑器可视化

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector2 center = GetAttackCenter();

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, attackSize);

        // 填充半透明
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawCube(center, attackSize);
    }

    #endregion
}
