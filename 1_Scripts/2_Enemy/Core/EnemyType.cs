namespace CryptaGeometrica.Enemy
{
    /// <summary>
    /// 敌人类型枚举
    /// 用于配置不同敌人的基础属性
    /// </summary>
    public enum EnemyType
    {
        /// <summary>
        /// 地面敌人 - 2HP，左右巡逻
        /// </summary>
        Ground = 0,

        /// <summary>
        /// 飞行敌人 - 1HP，自由方向巡逻
        /// </summary>
        Flying = 1,

        /// <summary>
        /// 肉盾敌人 - 3HP，左右巡逻
        /// </summary>
        Tank = 2
    }
}
