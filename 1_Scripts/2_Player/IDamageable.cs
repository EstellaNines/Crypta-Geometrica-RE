using UnityEngine;

/// <summary>
/// 可受伤接口
/// 所有可被攻击的对象需实现此接口
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="damageSource">伤害来源位置（用于计算击退方向）</param>
    void TakeDamage(int damage, Vector2 damageSource);
}
