namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// Tilemap层级枚举
    /// </summary>
    public enum TilemapLayer
    {
        /// <summary>背景层（使用Tileable普通瓦片平铺）</summary>
        Background = 0,

        /// <summary>地面层（使用Rule Tile，有碰撞）</summary>
        Ground = 1,

        /// <summary>平台层（使用Rule Tile，有碰撞）</summary>
        Platform = 2
    }

    /// <summary>
    /// 瓦片颜色主题
    /// Background层：对应Tileable普通瓦片
    /// Ground/Platform层：对应Rule Tile规则瓦片
    /// </summary>
    public enum TileTheme
    {
        /// <summary>无瓦片</summary>
        Empty = 0,

        /// <summary>蓝色主题</summary>
        Blue = 1,

        /// <summary>红色主题</summary>
        Red = 2,

        /// <summary>黄色主题</summary>
        Yellow = 3
    }
}
