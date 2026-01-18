namespace CryptaGeometrica.LevelGeneration.V4
{
    /// <summary>
    /// 房间类型枚举
    /// </summary>
    public enum RoomType
    {
        /// <summary>空房间（未生成）</summary>
        Empty = 0,

        /// <summary>普通房间</summary>
        Normal = 1,

        /// <summary>起始房间（生成出生点）</summary>
        Start = 2,

        /// <summary>终点房间（生成通关点）</summary>
        End = 3
    }

    /// <summary>
    /// 关卡门类型（区别于房间间连接的普通门）
    /// </summary>
    public enum LevelDoorType
    {
        /// <summary>无特殊门</summary>
        None = 0,

        /// <summary>关卡入口（玩家进入关卡的门）</summary>
        LevelEntrance = 1,

        /// <summary>关卡出口（玩家离开关卡的门）</summary>
        LevelExit = 2
    }

    /// <summary>
    /// 墙壁方向（用于指定出入口位置）
    /// </summary>
    public enum WallDirection
    {
        /// <summary>无</summary>
        None = 0,

        /// <summary>左侧</summary>
        Left = 1,

        /// <summary>右侧</summary>
        Right = 2,

        /// <summary>顶部</summary>
        Top = 3,

        /// <summary>底部</summary>
        Bottom = 4
    }
}
