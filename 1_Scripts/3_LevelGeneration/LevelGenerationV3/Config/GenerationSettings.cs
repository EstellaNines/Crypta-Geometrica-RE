using UnityEngine;

namespace CryptaGeometrica.LevelGeneration
{
    /// <summary>
    /// 生成设置 - 控制关卡生成的各种参数
    /// </summary>
    [CreateAssetMenu(fileName = "GenerationSettings", menuName = "Crypta Geometrica:RE/PCG程序化关卡/V3/GenerationSettings")]
    public class GenerationSettings : ScriptableObject
    {
        [Header("网格单元格尺寸")]
        
        [Tooltip("单元格宽度 (瓦片数)")]
        [Range(16, 64)]
        public int CellWidth = 32;
        
        [Tooltip("单元格高度 (瓦片数)")]
        [Range(16, 64)]
        public int CellHeight = 32;
        
        [Header("软边界设置")]
        
        [Tooltip("活跃区域缩放比例 (相对于单元格)")]
        [Range(0.5f, 0.9f)]
        public float ShrinkRatio = 0.7f;
        
        [Tooltip("活跃区域最大随机偏移 (瓦片数)")]
        [Range(0, 8)]
        public int MaxActiveZoneOffset = 4;
        
        [Header("走廊设置")]
        
        [Tooltip("走廊最小宽度 (瓦片数)")]
        [Range(2, 8)]
        public int CorridorMinWidth = 4;
        
        [Tooltip("走廊最大宽度 (瓦片数)")]
        [Range(4, 12)]
        public int CorridorMaxWidth = 6;
        
        [Header("WFC设置")]
        
        [Tooltip("WFC最大迭代次数")]
        [Range(100, 2000)]
        public int WFCMaxIterations = 500;
        
        [Tooltip("WFC冲突时局部重置半径")]
        [Range(1, 5)]
        public int WFCResetRadius = 2;
        
        [Header("平台生成")]
        
        [Tooltip("平台最小间距 (瓦片数)")]
        [Range(2, 8)]
        public int PlatformMinGap = 3;
        
        [Tooltip("平台最大间距 (瓦片数)")]
        [Range(4, 12)]
        public int PlatformMaxGap = 6;
        
        [Tooltip("玩家最大跳跃高度 (瓦片数)")]
        [Range(2, 8)]
        public int MaxJumpHeight = 4;
        
        [Tooltip("玩家最大跳跃距离 (瓦片数)")]
        [Range(3, 10)]
        public int MaxJumpDistance = 6;
        
        [Header("Boss房间")]
        
        [Tooltip("Boss房间扩大倍数")]
        [Range(1.0f, 2.0f)]
        public float BossRoomScale = 1.3f;
        
        [Header("深渊检测")]
        
        [Tooltip("深渊最小连续垂直房间数")]
        [Range(2, 4)]
        public int AbyssMinVerticalRooms = 3;
        
        [Header("性能设置")]
        
        [Tooltip("物理验证并行批次大小")]
        [Range(16, 128)]
        public int PhysicsValidationBatchSize = 64;
        
        [Tooltip("Tilemap批处理阈值")]
        [Range(100, 1000)]
        public int TilemapBatchThreshold = 500;
        
        /// <summary>
        /// 计算活跃区域尺寸
        /// </summary>
        /// <returns>活跃区域尺寸</returns>
        public Vector2Int GetActiveZoneSize()
        {
            int width = Mathf.RoundToInt(CellWidth * ShrinkRatio);
            int height = Mathf.RoundToInt(CellHeight * ShrinkRatio);
            return new Vector2Int(width, height);
        }
        
        /// <summary>
        /// 计算Boss房间活跃区域尺寸
        /// </summary>
        /// <returns>Boss房间活跃区域尺寸</returns>
        public Vector2Int GetBossRoomActiveZoneSize()
        {
            var normalSize = GetActiveZoneSize();
            int width = Mathf.RoundToInt(normalSize.x * BossRoomScale);
            int height = Mathf.RoundToInt(normalSize.y * BossRoomScale);
            return new Vector2Int(width, height);
        }
        
        /// <summary>
        /// 获取随机走廊宽度
        /// </summary>
        /// <param name="rng">随机数生成器</param>
        /// <returns>走廊宽度</returns>
        public int GetRandomCorridorWidth(System.Random rng)
        {
            return rng.Next(CorridorMinWidth, CorridorMaxWidth + 1);
        }
        
        /// <summary>
        /// 获取随机平台间距
        /// </summary>
        /// <param name="rng">随机数生成器</param>
        /// <returns>平台间距</returns>
        public int GetRandomPlatformGap(System.Random rng)
        {
            return rng.Next(PlatformMinGap, PlatformMaxGap + 1);
        }
        
        /// <summary>
        /// 计算单元格在世界中的原点位置
        /// </summary>
        /// <param name="gridX">网格X坐标</param>
        /// <param name="gridY">网格Y坐标</param>
        /// <returns>世界坐标原点</returns>
        public Vector2Int GetCellWorldOrigin(int gridX, int gridY)
        {
            return new Vector2Int(gridX * CellWidth, gridY * CellHeight);
        }
        
        /// <summary>
        /// 计算随机活跃区域偏移
        /// </summary>
        /// <param name="rng">随机数生成器</param>
        /// <returns>偏移量</returns>
        public Vector2Int GetRandomActiveZoneOffset(System.Random rng)
        {
            int offsetX = rng.Next(-MaxActiveZoneOffset, MaxActiveZoneOffset + 1);
            int offsetY = rng.Next(-MaxActiveZoneOffset, MaxActiveZoneOffset + 1);
            return new Vector2Int(offsetX, offsetY);
        }
    }
}
