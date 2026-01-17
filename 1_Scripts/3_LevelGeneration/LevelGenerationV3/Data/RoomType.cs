namespace CryptaGeometrica.LevelGeneration
{
    /// <summary>
    /// 房间类型枚举 - 定义宏观层中的房间功能类型
    /// </summary>
    public enum RoomType
    {
        /// <summary>
        /// 无效/未分配房间
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 起点房间 - 玩家初始位置
        /// </summary>
        Start = 1,
        
        /// <summary>
        /// 终点房间 - 关卡出口
        /// </summary>
        Exit = 2,
        
        /// <summary>
        /// 左右贯通房间 - 水平移动通道
        /// </summary>
        LR = 3,
        
        /// <summary>
        /// 下落房间 - 底部开口，允许向下移动
        /// </summary>
        Drop = 4,
        
        /// <summary>
        /// 着陆房间 - 顶部开口，接收上层下落
        /// </summary>
        Landing = 5,
        
        /// <summary>
        /// 侧室 - 非关键路径的可选房间
        /// </summary>
        Side = 6,
        
        /// <summary>
        /// 商店房间 - 特殊功能房间
        /// </summary>
        Shop = 7,
        
        /// <summary>
        /// 深渊竖井 - 连续垂直下落区域
        /// </summary>
        Abyss = 8,
        
        /// <summary>
        /// Boss房间 - 位于Exit前，扩大区域，最高难度
        /// </summary>
        Boss = 9
    }
    
    /// <summary>
    /// 方向枚举 - 用于连通性掩码
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// 北方 (上) - 掩码值: 1
        /// </summary>
        North = 0,
        
        /// <summary>
        /// 东方 (右) - 掩码值: 2
        /// </summary>
        East = 1,
        
        /// <summary>
        /// 南方 (下) - 掩码值: 4
        /// </summary>
        South = 2,
        
        /// <summary>
        /// 西方 (左) - 掩码值: 8
        /// </summary>
        West = 3
    }
    
    /// <summary>
    /// 方向扩展方法
    /// </summary>
    public static class DirectionExtensions
    {
        /// <summary>
        /// 获取方向对应的掩码位值
        /// </summary>
        /// <param name="direction">方向</param>
        /// <returns>掩码位值 (1, 2, 4, 或 8)</returns>
        public static int ToMask(this Direction direction)
        {
            return 1 << (int)direction;
        }
        
        /// <summary>
        /// 获取相反方向
        /// </summary>
        /// <param name="direction">当前方向</param>
        /// <returns>相反方向</returns>
        public static Direction Opposite(this Direction direction)
        {
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => direction
            };
        }
    }
}
