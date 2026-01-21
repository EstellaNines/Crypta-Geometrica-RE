using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 灰盒Tilemap层配置（简化版）
    /// 只包含地面层和平台层
    /// </summary>
    [System.Serializable]
    public class GrayboxTilemapLayers
    {
        [Header("Tilemap层引用")]
        
        [Tooltip("地面层 - 墙壁和填充合并，包含所有地形瓦片")]
        public Tilemap GroundLayer;
        
        [Tooltip("平台层 - 可站立平台")]
        public Tilemap PlatformLayer;
        
        // 兼容性属性（指向 GroundLayer）
        public Tilemap WallLayer => GroundLayer;
        public Tilemap FillLayer => GroundLayer;
        
        /// <summary>
        /// 检查所有必需层是否已配置
        /// </summary>
        public bool IsValid()
        {
            return GroundLayer != null && PlatformLayer != null;
        }
        
        /// <summary>
        /// 清除所有层
        /// </summary>
        public void ClearAll()
        {
            if (GroundLayer != null) GroundLayer.ClearAllTiles();
            if (PlatformLayer != null) PlatformLayer.ClearAllTiles();
        }
    }
    
    /// <summary>
    /// 灰盒瓦片配置
    /// 11种颜色瓦片定义
    /// </summary>
    [System.Serializable]
    public class GrayboxTileSet
    {
        [Header("瓦片引用 (11色)")]
        
        [Tooltip("红色 - 出口")]
        public TileBase RedTile;
        
        [Tooltip("黄色 - 特殊区域")]
        public TileBase YellowTile;
        
        [Tooltip("蓝色 - 预留")]
        public TileBase BlueTile;
        
        [Tooltip("绿色 - 入口")]
        public TileBase GreenTile;
        
        [Tooltip("青色 - 预留")]
        public TileBase CyanTile;
        
        [Tooltip("紫色 - 预留")]
        public TileBase PurpleTile;
        
        [Tooltip("粉色 - 平台")]
        public TileBase PinkTile;
        
        [Tooltip("橙色 - 预留")]
        public TileBase OrangeTile;
        
        [Tooltip("黑色 - 墙壁")]
        public TileBase BlackTile;
        
        [Tooltip("白色 - 表层瓦片")]
        public TileBase WhiteTile;
        
        [Tooltip("灰色 - 洞穴填充")]
        public TileBase GrayTile;
        
        /// <summary>
        /// 检查必需瓦片是否已配置
        /// </summary>
        public bool IsValid()
        {
            return BlackTile != null && GrayTile != null && 
                   GreenTile != null && RedTile != null && PinkTile != null;
        }
    }
}
