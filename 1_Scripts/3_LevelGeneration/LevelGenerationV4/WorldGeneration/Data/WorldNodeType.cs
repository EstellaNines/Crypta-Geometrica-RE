namespace CryptaGeometrica.LevelGeneration.V4.World
{
    /// <summary>
    /// 世界节点类型枚举
    /// 决定使用哪个 DungeonPipelineData 配置
    /// </summary>
    public enum WorldNodeType
    {
        /// <summary>
        /// 走廊风格（脊柱专用）
        /// </summary>
        Corridor,
        
        /// <summary>
        /// 大厅风格（随机区域）
        /// </summary>
        Hall,
        
        /// <summary>
        /// 宝藏房
        /// </summary>
        Treasure,
        
        /// <summary>
        /// Boss房
        /// </summary>
        Boss,
        
        /// <summary>
        /// 世界入口
        /// </summary>
        Entrance,
        
        /// <summary>
        /// 世界出口
        /// </summary>
        Exit
    }
}
