// V3版本已废弃，抑制未使用变量警告
#pragma warning disable CS0219

using UnityEngine;
using UnityEngine.Tilemaps;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 灰盒房间模板生成器
    /// 使用四色瓦片（红/蓝/绿/黄）生成不同类型房间的视觉原型
    /// </summary>
    public class GrayboxRoomTemplates : MonoBehaviour
    {
        [Header("瓦片引用")]
        [Tooltip("红色瓦片 - 地面/墙壁")]
        public TileBase RedTile;
        
        [Tooltip("蓝色瓦片 - 平台")]
        public TileBase BlueTile;
        
        [Tooltip("绿色瓦片 - 出入口/通道")]
        public TileBase GreenTile;
        
        [Tooltip("黄色瓦片 - 特殊区域/陷阱")]
        public TileBase YellowTile;
        
        [Header("Tilemap引用")]
        public Tilemap TargetTilemap;
        
        [Header("房间尺寸")]
        [Tooltip("房间宽度（瓦片数）")]
        [Range(16, 64)]
        public int RoomWidth = 32;
        
        [Tooltip("房间高度（瓦片数）")]
        [Range(16, 64)]
        public int RoomHeight = 32;
        
        [Tooltip("墙壁厚度（瓦片数）")]
        [Range(1, 4)]
        public int WallThickness = 2;
        
        [Tooltip("通道宽度（瓦片数）")]
        [Range(4, 12)]
        public int PassageWidth = 6;
        
        /// <summary>
        /// 生成指定类型的房间模板
        /// </summary>
        /// <param name="roomType">房间类型</param>
        /// <param name="origin">生成原点</param>
        public void GenerateRoomTemplate(RoomType roomType, Vector2Int origin)
        {
            if (TargetTilemap == null)
            {
                Debug.LogError("GrayboxRoomTemplates: TargetTilemap未设置!");
                return;
            }
            
            switch (roomType)
            {
                case RoomType.Start:
                    GenerateStartRoom(origin);
                    break;
                case RoomType.Exit:
                    GenerateExitRoom(origin);
                    break;
                case RoomType.LR:
                    GenerateLRRoom(origin);
                    break;
                case RoomType.Drop:
                    GenerateDropRoom(origin);
                    break;
                case RoomType.Landing:
                    GenerateLandingRoom(origin);
                    break;
                case RoomType.Side:
                    GenerateSideRoom(origin);
                    break;
                case RoomType.Shop:
                    GenerateShopRoom(origin);
                    break;
                case RoomType.Abyss:
                    GenerateAbyssRoom(origin);
                    break;
                case RoomType.Boss:
                    GenerateBossRoom(origin);
                    break;
                default:
                    GenerateEmptyRoom(origin);
                    break;
            }
        }
        
        /// <summary>
        /// 清除Tilemap
        /// </summary>
        public void ClearTilemap()
        {
            if (TargetTilemap != null)
            {
                TargetTilemap.ClearAllTiles();
            }
        }
        
        /// <summary>
        /// 生成基础房间框架（四周墙壁）
        /// </summary>
        /// <param name="origin">原点</param>
        private void GenerateBaseFrame(Vector2Int origin)
        {
            // 底部墙壁
            FillRect(origin.x, origin.y, RoomWidth, WallThickness, RedTile);
            
            // 顶部墙壁
            FillRect(origin.x, origin.y + RoomHeight - WallThickness, RoomWidth, WallThickness, RedTile);
            
            // 左侧墙壁
            FillRect(origin.x, origin.y, WallThickness, RoomHeight, RedTile);
            
            // 右侧墙壁
            FillRect(origin.x + RoomWidth - WallThickness, origin.y, WallThickness, RoomHeight, RedTile);
        }
        
        /// <summary>
        /// 生成起点房间
        /// </summary>
        private void GenerateStartRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
            
            // 中央绿色起点标记
            int centerX = origin.x + RoomWidth / 2 - 2;
            int centerY = origin.y + WallThickness;
            FillRect(centerX, centerY, 4, 4, GreenTile);
            
            // 右侧通道
            int passageY = origin.y + RoomHeight / 2 - PassageWidth / 2;
            FillRect(origin.x + RoomWidth - WallThickness, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 内部平台
            GenerateInternalPlatforms(origin, 2);
        }
        
        /// <summary>
        /// 生成终点房间
        /// </summary>
        private void GenerateExitRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
            
            // 中央绿色出口标记
            int centerX = origin.x + RoomWidth / 2 - 2;
            int centerY = origin.y + RoomHeight / 2 - 2;
            FillRect(centerX, centerY, 4, 4, GreenTile);
            
            // 左侧通道
            int passageY = origin.y + RoomHeight / 2 - PassageWidth / 2;
            FillRect(origin.x, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 内部平台
            GenerateInternalPlatforms(origin, 2);
        }
        
        /// <summary>
        /// 生成左右贯通房间
        /// </summary>
        private void GenerateLRRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
            
            int passageY = origin.y + RoomHeight / 2 - PassageWidth / 2;
            
            // 左侧通道
            FillRect(origin.x, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 右侧通道
            FillRect(origin.x + RoomWidth - WallThickness, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 内部平台
            GenerateInternalPlatforms(origin, 3);
        }
        
        /// <summary>
        /// 生成下落房间（底部开口）
        /// </summary>
        private void GenerateDropRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
            
            // 底部中央开口
            int passageX = origin.x + RoomWidth / 2 - PassageWidth / 2;
            FillRect(passageX, origin.y, PassageWidth, WallThickness, GreenTile);
            
            // 左右通道
            int passageY = origin.y + RoomHeight / 2 - PassageWidth / 2;
            FillRect(origin.x, passageY, WallThickness, PassageWidth, GreenTile);
            FillRect(origin.x + RoomWidth - WallThickness, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 内部平台
            GenerateInternalPlatforms(origin, 2);
        }
        
        /// <summary>
        /// 生成着陆房间（顶部开口）
        /// </summary>
        private void GenerateLandingRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
            
            // 顶部中央开口
            int passageX = origin.x + RoomWidth / 2 - PassageWidth / 2;
            FillRect(passageX, origin.y + RoomHeight - WallThickness, PassageWidth, WallThickness, GreenTile);
            
            // 左右通道
            int passageY = origin.y + RoomHeight / 2 - PassageWidth / 2;
            FillRect(origin.x, passageY, WallThickness, PassageWidth, GreenTile);
            FillRect(origin.x + RoomWidth - WallThickness, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 内部平台
            GenerateInternalPlatforms(origin, 2);
        }
        
        /// <summary>
        /// 生成侧室（单侧通道）
        /// </summary>
        private void GenerateSideRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
            
            // 仅左侧通道
            int passageY = origin.y + RoomHeight / 2 - PassageWidth / 2;
            FillRect(origin.x, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 内部平台
            GenerateInternalPlatforms(origin, 2);
        }
        
        /// <summary>
        /// 生成商店房间（黄色特殊区域）
        /// </summary>
        private void GenerateShopRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
            
            // 中央黄色商店区域
            int shopX = origin.x + RoomWidth / 4;
            int shopY = origin.y + WallThickness;
            int shopWidth = RoomWidth / 2;
            int shopHeight = RoomHeight / 3;
            FillRect(shopX, shopY, shopWidth, shopHeight, YellowTile);
            
            // 左侧通道
            int passageY = origin.y + RoomHeight / 2 - PassageWidth / 2;
            FillRect(origin.x, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 蓝色平台
            FillRect(origin.x + WallThickness + 4, origin.y + RoomHeight / 2, RoomWidth - WallThickness * 2 - 8, 1, BlueTile);
        }
        
        /// <summary>
        /// 生成深渊房间（垂直贯通）
        /// </summary>
        private void GenerateAbyssRoom(Vector2Int origin)
        {
            // 仅左右墙壁
            FillRect(origin.x, origin.y, WallThickness, RoomHeight, RedTile);
            FillRect(origin.x + RoomWidth - WallThickness, origin.y, WallThickness, RoomHeight, RedTile);
            
            // 顶部和底部绿色通道
            int passageX = origin.x + RoomWidth / 2 - PassageWidth / 2;
            FillRect(passageX, origin.y, PassageWidth, 2, GreenTile);
            FillRect(passageX, origin.y + RoomHeight - 2, PassageWidth, 2, GreenTile);
            
            // 一些蓝色平台（可选的落脚点）
            FillRect(origin.x + WallThickness + 2, origin.y + RoomHeight / 3, 4, 1, BlueTile);
            FillRect(origin.x + RoomWidth - WallThickness - 6, origin.y + RoomHeight * 2 / 3, 4, 1, BlueTile);
        }
        
        /// <summary>
        /// 生成Boss房间（扩大区域+黄色标记）
        /// </summary>
        private void GenerateBossRoom(Vector2Int origin)
        {
            // Boss房间使用1.3倍尺寸
            int bossWidth = Mathf.RoundToInt(RoomWidth * 1.3f);
            int bossHeight = Mathf.RoundToInt(RoomHeight * 1.3f);
            
            // 调整原点使房间居中
            int offsetX = (bossWidth - RoomWidth) / 2;
            int offsetY = (bossHeight - RoomHeight) / 2;
            Vector2Int bossOrigin = new Vector2Int(origin.x - offsetX, origin.y - offsetY);
            
            // 底部墙壁
            FillRect(bossOrigin.x, bossOrigin.y, bossWidth, WallThickness, RedTile);
            
            // 顶部墙壁
            FillRect(bossOrigin.x, bossOrigin.y + bossHeight - WallThickness, bossWidth, WallThickness, RedTile);
            
            // 左侧墙壁
            FillRect(bossOrigin.x, bossOrigin.y, WallThickness, bossHeight, RedTile);
            
            // 右侧墙壁
            FillRect(bossOrigin.x + bossWidth - WallThickness, bossOrigin.y, WallThickness, bossHeight, RedTile);
            
            // 左侧入口通道
            int passageY = bossOrigin.y + bossHeight / 2 - PassageWidth / 2;
            FillRect(bossOrigin.x, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 右侧出口通道
            FillRect(bossOrigin.x + bossWidth - WallThickness, passageY, WallThickness, PassageWidth, GreenTile);
            
            // 中央黄色Boss区域标记
            int bossAreaX = bossOrigin.x + bossWidth / 2 - 4;
            int bossAreaY = bossOrigin.y + WallThickness + 2;
            FillRect(bossAreaX, bossAreaY, 8, 8, YellowTile);
            
            // 战斗平台
            FillRect(bossOrigin.x + WallThickness + 4, bossOrigin.y + bossHeight / 3, bossWidth - WallThickness * 2 - 8, 1, BlueTile);
            FillRect(bossOrigin.x + WallThickness + 8, bossOrigin.y + bossHeight * 2 / 3, bossWidth - WallThickness * 2 - 16, 1, BlueTile);
        }
        
        /// <summary>
        /// 生成空房间
        /// </summary>
        private void GenerateEmptyRoom(Vector2Int origin)
        {
            GenerateBaseFrame(origin);
        }
        
        /// <summary>
        /// 生成内部平台
        /// </summary>
        /// <param name="origin">房间原点</param>
        /// <param name="platformCount">平台数量</param>
        private void GenerateInternalPlatforms(Vector2Int origin, int platformCount)
        {
            int innerWidth = RoomWidth - WallThickness * 2 - 8;
            int innerStartX = origin.x + WallThickness + 4;
            
            for (int i = 0; i < platformCount; i++)
            {
                int platformY = origin.y + WallThickness + (RoomHeight - WallThickness * 2) * (i + 1) / (platformCount + 1);
                int platformWidth = innerWidth * 2 / 3;
                int platformX = innerStartX + (i % 2 == 0 ? 0 : innerWidth - platformWidth);
                
                FillRect(platformX, platformY, platformWidth, 1, BlueTile);
            }
        }
        
        /// <summary>
        /// 填充矩形区域
        /// </summary>
        private void FillRect(int x, int y, int width, int height, TileBase tile)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    TargetTilemap.SetTile(new Vector3Int(x + dx, y + dy, 0), tile);
                }
            }
        }
    }
}
