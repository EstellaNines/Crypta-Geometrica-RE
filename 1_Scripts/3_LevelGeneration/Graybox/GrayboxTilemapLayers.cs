using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 灰盒多层Tilemap配置
    /// 管理不同功能层的Tilemap引用
    /// </summary>
    [System.Serializable]
    public class GrayboxTilemapLayers
    {
        [Header("Tilemap层引用")]
        
        [Tooltip("墙壁层 - 红色瓦片，4×4网格最外围边界")]
        public Tilemap WallLayer;
        
        [Tooltip("填充层 - 橙色瓦片，洞穴内部随机填充")]
        public Tilemap FillLayer;
        
        [Tooltip("平台层 - 蓝色瓦片，可站立平台")]
        public Tilemap PlatformLayer;
        
        [Tooltip("入口层 - 绿色瓦片，房间入口标记")]
        public Tilemap EntranceLayer;
        
        [Tooltip("出口层 - 黑色瓦片，房间出口标记")]
        public Tilemap ExitLayer;
        
        [Tooltip("特殊层 - 黄色瓦片，商店/Boss等特殊区域")]
        public Tilemap SpecialLayer;
        
        /// <summary>
        /// 检查所有必需层是否已配置
        /// </summary>
        public bool IsValid()
        {
            return WallLayer != null && FillLayer != null && PlatformLayer != null;
        }
        
        /// <summary>
        /// 清除所有层
        /// </summary>
        public void ClearAll()
        {
            if (WallLayer != null) WallLayer.ClearAllTiles();
            if (FillLayer != null) FillLayer.ClearAllTiles();
            if (PlatformLayer != null) PlatformLayer.ClearAllTiles();
            if (EntranceLayer != null) EntranceLayer.ClearAllTiles();
            if (ExitLayer != null) ExitLayer.ClearAllTiles();
            if (SpecialLayer != null) SpecialLayer.ClearAllTiles();
        }
    }
    
    /// <summary>
    /// 灰盒瓦片配置
    /// 7种颜色瓦片定义
    /// </summary>
    [System.Serializable]
    public class GrayboxTileSet
    {
        [Header("瓦片引用 (7色)")]
        
        [Tooltip("红色 - 外围墙壁")]
        public TileBase RedTile;
        
        [Tooltip("蓝色 - 平台")]
        public TileBase BlueTile;
        
        [Tooltip("绿色 - 入口")]
        public TileBase GreenTile;
        
        [Tooltip("黄色 - 特殊区域")]
        public TileBase YellowTile;
        
        [Tooltip("黑色 - 出口")]
        public TileBase BlackTile;
        
        [Tooltip("橙色 - 填充瓦片")]
        public TileBase OrangeTile;
        
        [Tooltip("白色 - 预留")]
        public TileBase WhiteTile;
        
        [Tooltip("紫色 - 预留")]
        public TileBase PurpleTile;
        
        [Tooltip("粉色 - 预留")]
        public TileBase PinkTile;
        
        /// <summary>
        /// 检查必需瓦片是否已配置
        /// </summary>
        public bool IsValid()
        {
            return RedTile != null && BlueTile != null && 
                   GreenTile != null && OrangeTile != null && BlackTile != null;
        }
    }
}
