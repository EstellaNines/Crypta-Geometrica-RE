using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 灰盒关卡生成器 - Spelunky风格
    /// 生成4×4整体网格，无房间间隙，只有外围墙壁
    /// </summary>
    public class GrayboxLevelGenerator : MonoBehaviour
    {
        [Header("Tilemap层配置")]
        public GrayboxTilemapLayers TilemapLayers;
        
        [Header("瓦片配置")]
        public GrayboxTileSet TileSet;
        
        [Header("网格尺寸")]
        [Tooltip("单个房间宽度（瓦片数）")]
        [Range(8, 32)]
        public int RoomWidth = 16;
        
        [Tooltip("单个房间高度（瓦片数）")]
        [Range(8, 32)]
        public int RoomHeight = 16;
        
        [Tooltip("外围墙壁厚度")]
        [Range(1, 4)]
        public int WallThickness = 2;
        
        [Header("洞穴生成参数")]
        [Tooltip("填充密度 (0-1) - 推荐0.5获得更实的洞穴感")]
        [Range(0f, 0.6f)]
        public float FillDensity = 0.50f;
        
        [Tooltip("平滑迭代次数")]
        [Range(0, 5)]
        public int SmoothIterations = 3;
        
        [Tooltip("随机种子 (0=随机)")]
        public int RandomSeed = 0;
        
        [Header("出入口设置")]
        [Tooltip("出入口通道宽度（玩家宽度约1.5瓦片）")]
        [Range(2, 6)]
        public int EntranceWidth = 3;
        
        [Tooltip("出入口通道高度（玩家高度约1.5瓦片）")]
        [Range(2, 6)]
        public int EntranceHeight = 3;
        
        [Header("平台设置")]
        [Tooltip("玩家跳跃力")]
        public float PlayerJumpForce = 8f;
        
        [Tooltip("平台最大高度差(基于跳跃力计算)")]
        [Range(2, 6)]
        public int MaxPlatformHeightDiff = 4;
        
        [Tooltip("平台最小间距")]
        [Range(2, 6)]
        public int MinPlatformGap = 3;
        
        [Header("逆向阶梯设置")]
        [Tooltip("阶梯平台间的安全跳跃高度")]
        [Range(3, 6)]
        public int StaircaseSafeHeight = 4;
        
        [Tooltip("阶梯平台宽度")]
        [Range(3, 6)]
        public int StaircasePlatformWidth = 4;
        
        [Tooltip("阶梯水平偏移量")]
        [Range(3, 8)]
        public int StaircaseHorizontalOffset = 4;
        
        [Header("主题设置")]
        [Tooltip("是否启用主题系统（使用规则瓦片替换灰盒瓦片）")]
        public bool UseTheme = false;
        
        [Tooltip("主题配置数据")]
        public RoomTheme ThemeConfig;
        
        // 当前使用的颜色主题（运行时设置）
        [HideInInspector]
        public ThemeColorData CurrentColorTheme;
        
        [Header("规则瓦片替换（无主题时使用）")]
        [Tooltip("是否启用规则瓦片替换（将墙壁、填充、表层替换为规则瓦片）")]
        public bool UseRuleTile = false;
        
        [Tooltip("地面规则瓦片")]
        public RuleTile GroundRuleTile;
        
        [Tooltip("平台规则瓦片")]
        public RuleTile PlatformRuleTile;
        
        private LevelShape _currentShape;
        private RoomNode[,] _roomGrid;
        private System.Random _rng;
        
        // 计算属性
        private int TotalWidth => LevelShape.GridWidth * RoomWidth;
        private int TotalHeight => LevelShape.GridHeight * RoomHeight;
        
        /// <summary>
        /// 生成完整关卡
        /// </summary>
        [ContextMenu("生成关卡")]
        public void GenerateLevel()
        {
            GenerateLevel(LevelShapePresets.FullSquare);
        }
        
        /// <summary>
        /// 生成指定形状的关卡
        /// </summary>
        public void GenerateLevel(LevelShape shape)
        {
            if (!ValidateSetup()) return;
            
            _currentShape = shape;
            _rng = RandomSeed == 0 ? new System.Random() : new System.Random(RandomSeed);
            
            // 清除所有层
            TilemapLayers.ClearAll();
            
            // 初始化房间网格
            InitializeRoomGrid();
            
            // 生成关键路径
            GenerateCriticalPath();
            
            // 绘制外围墙壁
            DrawOuterWalls();
            
            // 绘制洞穴填充
            DrawCaveFill();
            
            // 绘制房间连接通道
            DrawRoomConnections();
            
            // 绘制入口和出口
            DrawEntranceAndExit();
            
            // 绘制平台
            DrawPlatforms();
            
            // 绘制特殊区域
            DrawSpecialAreas();
            
            Debug.Log($"关卡生成完成! 形状: {shape}, 有效房间: {shape.GetValidCellCount()}");
        }
        
        /// <summary>
        /// 清除关卡
        /// </summary>
        [ContextMenu("清除关卡")]
        public void ClearLevel()
        {
            if (TilemapLayers != null)
            {
                TilemapLayers.ClearAll();
            }
        }
        
        /// <summary>
        /// 验证设置
        /// </summary>
        private bool ValidateSetup()
        {
            if (TilemapLayers == null || !TilemapLayers.IsValid())
            {
                Debug.LogError("GrayboxLevelGenerator: Tilemap层未正确配置!");
                return false;
            }
            
            if (TileSet == null || !TileSet.IsValid())
            {
                Debug.LogError("GrayboxLevelGenerator: 瓦片未正确配置!");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 初始化房间网格
        /// </summary>
        private void InitializeRoomGrid()
        {
            _roomGrid = new RoomNode[LevelShape.GridWidth, LevelShape.GridHeight];
            
            for (int y = 0; y < LevelShape.GridHeight; y++)
            {
                for (int x = 0; x < LevelShape.GridWidth; x++)
                {
                    _roomGrid[x, y] = new RoomNode(x, y);
                    
                    if (_currentShape.IsValidCell(x, y))
                    {
                        _roomGrid[x, y].Type = RoomType.Side;
                    }
                }
            }
        }
        
        /// <summary>
        /// 生成关键路径 - Spelunky风格醉汉游走算法
        /// 从顶排随机选择入口，每层水平游走后向下
        /// </summary>
        private void GenerateCriticalPath()
        {
            // 重置形状为全部无效，由路径决定哪些房间有效
            _currentShape = new LevelShape();
            
            // 第一步：从顶排(y=0)随机选择入口
            int startX = _rng.Next(LevelShape.GridWidth);
            Vector2Int current = new Vector2Int(startX, 0);
            
            // 路径列表
            List<Vector2Int> path = new List<Vector2Int>();
            path.Add(current);
            _currentShape.SetCell(current.x, current.y, true);
            
            // 第二步：每层先水平游走，再向下
            for (int row = 0; row < LevelShape.GridHeight; row++)
            {
                // 在当前行随机选择水平移动方向和步数
                int horizontalDirection = _rng.Next(2) == 0 ? -1 : 1; // -1=左, 1=右
                int horizontalSteps = _rng.Next(1, 4); // 1-3步
                
                // 水平游走
                for (int step = 0; step < horizontalSteps; step++)
                {
                    int nextX = current.x + horizontalDirection;
                    
                    // 检查边界
                    if (nextX < 0 || nextX >= LevelShape.GridWidth)
                    {
                        // 撞墙后反向
                        horizontalDirection = -horizontalDirection;
                        nextX = current.x + horizontalDirection;
                        
                        if (nextX < 0 || nextX >= LevelShape.GridWidth)
                            break;
                    }
                    
                    Vector2Int next = new Vector2Int(nextX, current.y);
                    
                    // 添加到路径
                    if (!path.Contains(next))
                    {
                        path.Add(next);
                    }
                    _currentShape.SetCell(next.x, next.y, true);
                    
                    // 添加水平连接
                    Direction dir = GetDirection(current, next);
                    _roomGrid[current.x, current.y].AddConnection(dir);
                    _roomGrid[next.x, next.y].AddConnection(dir.Opposite());
                    
                    current = next;
                }
                
                // 向下移动到下一层（如果不是最后一层）
                if (row < LevelShape.GridHeight - 1)
                {
                    Vector2Int next = new Vector2Int(current.x, current.y + 1);
                    
                    if (!path.Contains(next))
                    {
                        path.Add(next);
                    }
                    _currentShape.SetCell(next.x, next.y, true);
                    
                    // 添加垂直连接
                    Direction dir = GetDirection(current, next);
                    _roomGrid[current.x, current.y].AddConnection(dir);
                    _roomGrid[next.x, next.y].AddConnection(dir.Opposite());
                    
                    current = next;
                }
            }
            
            // 第三步：设置入口和出口
            Vector2Int entrance = path[0];
            Vector2Int exit = path[path.Count - 1];
            
            _roomGrid[entrance.x, entrance.y].Type = RoomType.Start;
            _roomGrid[entrance.x, entrance.y].IsCriticalPath = true;
            
            _roomGrid[exit.x, exit.y].Type = RoomType.Exit;
            _roomGrid[exit.x, exit.y].IsCriticalPath = true;
            
            // 标记路径上的房间
            foreach (var cell in path)
            {
                _roomGrid[cell.x, cell.y].IsCriticalPath = true;
                if (_roomGrid[cell.x, cell.y].Type == RoomType.Side)
                {
                    // 根据连接方向设置房间类型
                    var room = _roomGrid[cell.x, cell.y];
                    bool hasVertical = room.HasConnection(Direction.North) || room.HasConnection(Direction.South);
                    bool hasHorizontal = room.HasConnection(Direction.East) || room.HasConnection(Direction.West);
                    
                    if (hasVertical && !hasHorizontal)
                    {
                        room.Type = RoomType.Drop; // 只有垂直连接
                    }
                    else if (hasHorizontal && !hasVertical)
                    {
                        room.Type = RoomType.LR; // 只有水平连接
                    }
                    else
                    {
                        room.Type = RoomType.Landing; // 混合连接
                    }
                }
            }
            
            // 在出口前设置Boss房间（如果路径够长）
            if (path.Count >= 3)
            {
                var bossCell = path[path.Count - 2];
                if (_roomGrid[bossCell.x, bossCell.y].Type != RoomType.Start &&
                    _roomGrid[bossCell.x, bossCell.y].Type != RoomType.Exit)
                {
                    _roomGrid[bossCell.x, bossCell.y].Type = RoomType.Boss;
                }
            }
        }
        
        /// <summary>
        /// 生成商店支路（可选，添加到主路径旁边）
        /// </summary>
        private void GenerateShopBranch(List<Vector2Int> mainPath)
        {
            // 找到非关键路径的有效房间作为商店
            var validCells = _currentShape.GetValidCells();
            List<Vector2Int> nonPathCells = new List<Vector2Int>();
            
            foreach (var cell in validCells)
            {
                if (!_roomGrid[cell.x, cell.y].IsCriticalPath)
                {
                    // 检查是否与主路径相邻
                    foreach (var pathCell in mainPath)
                    {
                        if (AreNeighbors(cell, pathCell))
                        {
                            nonPathCells.Add(cell);
                            break;
                        }
                    }
                }
            }
            
            // 如果有可用的支路房间，选择一个作为商店
            if (nonPathCells.Count > 0)
            {
                var shopCell = nonPathCells[_rng.Next(nonPathCells.Count)];
                _roomGrid[shopCell.x, shopCell.y].Type = RoomType.Shop;
                
                // 连接商店到主路径
                foreach (var pathCell in mainPath)
                {
                    if (AreNeighbors(shopCell, pathCell))
                    {
                        Direction dir = GetDirection(shopCell, pathCell);
                        _roomGrid[shopCell.x, shopCell.y].AddConnection(dir);
                        _roomGrid[pathCell.x, pathCell.y].AddConnection(dir.Opposite());
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查两个房间是否相邻
        /// </summary>
        private bool AreNeighbors(Vector2Int a, Vector2Int b)
        {
            return (Mathf.Abs(a.x - b.x) == 1 && a.y == b.y) ||
                   (Mathf.Abs(a.y - b.y) == 1 && a.x == b.x);
        }
        
        /// <summary>
        /// 简单路径查找
        /// </summary>
        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = new List<Vector2Int> { start };
            Vector2Int current = start;
            
            int maxIterations = 50;
            while (current != end && maxIterations-- > 0)
            {
                int dx = end.x - current.x;
                int dy = end.y - current.y;
                
                List<Vector2Int> candidates = new List<Vector2Int>();
                
                // 优先向目标方向移动
                if (dy > 0)
                {
                    Vector2Int down = new Vector2Int(current.x, current.y + 1);
                    if (_currentShape.IsValidCell(down) && !path.Contains(down))
                        candidates.Add(down);
                }
                if (dy < 0)
                {
                    Vector2Int up = new Vector2Int(current.x, current.y - 1);
                    if (_currentShape.IsValidCell(up) && !path.Contains(up))
                        candidates.Add(up);
                }
                if (dx > 0)
                {
                    Vector2Int right = new Vector2Int(current.x + 1, current.y);
                    if (_currentShape.IsValidCell(right) && !path.Contains(right))
                        candidates.Add(right);
                }
                if (dx < 0)
                {
                    Vector2Int left = new Vector2Int(current.x - 1, current.y);
                    if (_currentShape.IsValidCell(left) && !path.Contains(left))
                        candidates.Add(left);
                }
                
                // 如果没有候选，尝试任意相邻格子
                if (candidates.Count == 0)
                {
                    var neighbors = _currentShape.GetValidNeighbors(current.x, current.y);
                    foreach (var n in neighbors)
                    {
                        if (!path.Contains(n)) candidates.Add(n);
                    }
                }
                
                if (candidates.Count == 0) break;
                
                current = candidates[_rng.Next(candidates.Count)];
                path.Add(current);
            }
            
            return path;
        }
        
        /// <summary>
        /// 获取方向
        /// </summary>
        private Direction GetDirection(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            
            if (dy < 0) return Direction.North;
            if (dy > 0) return Direction.South;
            if (dx > 0) return Direction.East;
            if (dx < 0) return Direction.West;
            
            return Direction.North;
        }
        
        /// <summary>
        /// 绘制外围墙壁 - 只围绕有效房间
        /// </summary>
        private void DrawOuterWalls()
        {
            // 只在有效房间的外围绘制红色墙壁
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    
                    // 检查四个方向，在边界处绘制墙壁
                    
                    // 北边（上方）- 如果是边缘或相邻无效房间
                    if (gy == 0 || !_currentShape.IsValidCell(gx, gy - 1))
                    {
                        FillRect(TilemapLayers.WallLayer, TileSet.BlackTile,
                            worldX, worldY + RoomHeight - WallThickness, RoomWidth, WallThickness);
                    }
                    
                    // 南边（下方）
                    if (gy == LevelShape.GridHeight - 1 || !_currentShape.IsValidCell(gx, gy + 1))
                    {
                        FillRect(TilemapLayers.WallLayer, TileSet.BlackTile,
                            worldX, worldY, RoomWidth, WallThickness);
                    }
                    
                    // 西边（左边）
                    if (gx == 0 || !_currentShape.IsValidCell(gx - 1, gy))
                    {
                        FillRect(TilemapLayers.WallLayer, TileSet.BlackTile,
                            worldX, worldY, WallThickness, RoomHeight);
                    }
                    
                    // 东边（右边）
                    if (gx == LevelShape.GridWidth - 1 || !_currentShape.IsValidCell(gx + 1, gy))
                    {
                        FillRect(TilemapLayers.WallLayer, TileSet.BlackTile,
                            worldX + RoomWidth - WallThickness, worldY, WallThickness, RoomHeight);
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制洞穴填充 - 整体连贯填充，然后在其中雕刻曲折通道
        /// </summary>
        private void DrawCaveFill()
        {
            // 第一步：在有效房间与无效房间的边界处添加墙壁过渡
            AddBoundaryWalls();
            
            // 第二步：有效区域整体生成连贯填充
            GenerateConnectedCaveFill();
            
            // 第三步：清除房间连接通道
            ClearAllConnectionPassages();
            
            // 第四步：在填充中雕刻曲折的行走通道
            CarveWindingPath();
            
            // 第五步：将表层地板替换为白色瓦片
            ApplySurfaceTiles();
        }
        
        /// <summary>
        /// 将表层地板（上方是空气，下方是填充物）替换为白色瓦片
        /// 排除墙壁层已有瓦片的位置
        /// </summary>
        private void ApplySurfaceTiles()
        {
            var fillTilemap = TilemapLayers.FillLayer;
            var wallTilemap = TilemapLayers.WallLayer;
            var whiteTile = TileSet.WhiteTile;
            
            if (whiteTile == null) return;
            
            // 遍历所有有效房间区域
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    
                    // 扫描房间内的每个瓦片
                    for (int y = 0; y < RoomHeight; y++)
                    {
                        for (int x = 0; x < RoomWidth; x++)
                        {
                            int tileX = worldX + x;
                            int tileY = worldY + y;
                            Vector3Int pos = new Vector3Int(tileX, tileY, 0);
                            
                            // 跳过墙壁层已有瓦片的位置（保持墙壁边框为黑色）
                            if (wallTilemap.GetTile(pos) != null) continue;
                            
                            // 检查当前位置是否有填充瓦片
                            var currentTile = fillTilemap.GetTile(pos);
                            if (currentTile == null) continue;
                            
                            // 检查上方是否为空（表层条件）
                            var aboveTile = fillTilemap.GetTile(new Vector3Int(tileX, tileY + 1, 0));
                            if (aboveTile == null)
                            {
                                // 这是表层地板，替换为白色
                                fillTilemap.SetTile(pos, whiteTile);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 在有效房间边界处添加自然洞穴墙壁
        /// </summary>
        private void AddBoundaryWalls()
        {
            // 先为所有有效房间填充完整的边界基础层
            int baseBoundaryWidth = 12; // 基础边界厚度
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    
                    // 为所有边界方向填充基础层
                    // 北边
                    if (gy == 0 || !_currentShape.IsValidCell(gx, gy - 1))
                    {
                        FillRect(TilemapLayers.FillLayer, TileSet.GrayTile,
                            worldX, worldY + RoomHeight - baseBoundaryWidth, RoomWidth, baseBoundaryWidth);
                    }
                    // 南边
                    if (gy == LevelShape.GridHeight - 1 || !_currentShape.IsValidCell(gx, gy + 1))
                    {
                        FillRect(TilemapLayers.FillLayer, TileSet.GrayTile,
                            worldX, worldY, RoomWidth, baseBoundaryWidth);
                    }
                    // 西边
                    if (gx == 0 || !_currentShape.IsValidCell(gx - 1, gy))
                    {
                        FillRect(TilemapLayers.FillLayer, TileSet.GrayTile,
                            worldX, worldY, baseBoundaryWidth, RoomHeight);
                    }
                    // 东边
                    if (gx == LevelShape.GridWidth - 1 || !_currentShape.IsValidCell(gx + 1, gy))
                    {
                        FillRect(TilemapLayers.FillLayer, TileSet.GrayTile,
                            worldX + RoomWidth - baseBoundaryWidth, worldY, baseBoundaryWidth, RoomHeight);
                    }
                }
            }
            
            // 然后在边界上雕刻不规则的洞穴边缘
            CarveBoundaryEdges();
        }
        
        /// <summary>
        /// 在边界上雕刻不规则的洞穴边缘
        /// </summary>
        private void CarveBoundaryEdges()
        {
            int carveDepth = 6; // 雕刻深度
            int carveVariation = 4; // 变化范围
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    int centerX = worldX + RoomWidth / 2;
                    int centerY = worldY + RoomHeight / 2;
                    
                    // 在边界内侧雕刻不规则边缘
                    // 北边
                    if (gy == 0 || !_currentShape.IsValidCell(gx, gy - 1))
                    {
                        int baseY = worldY + RoomHeight - 12;
                        for (int x = 0; x < RoomWidth; x++)
                        {
                            int depth = carveDepth + _rng.Next(-carveVariation, carveVariation + 1);
                            // 使用正弦波形创建更自然的边缘
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                ClearTile(TilemapLayers.FillLayer, worldX + x, baseY + y);
                            }
                        }
                    }
                    
                    // 南边
                    if (gy == LevelShape.GridHeight - 1 || !_currentShape.IsValidCell(gx, gy + 1))
                    {
                        int baseY = worldY + 12;
                        for (int x = 0; x < RoomWidth; x++)
                        {
                            int depth = carveDepth + _rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                ClearTile(TilemapLayers.FillLayer, worldX + x, baseY - y - 1);
                            }
                        }
                    }
                    
                    // 西边
                    if (gx == 0 || !_currentShape.IsValidCell(gx - 1, gy))
                    {
                        int baseX = worldX + 12;
                        for (int y = 0; y < RoomHeight; y++)
                        {
                            int depth = carveDepth + _rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(y * 0.3f) * 3);
                            for (int x = 0; x < depth; x++)
                            {
                                ClearTile(TilemapLayers.FillLayer, baseX - x - 1, worldY + y);
                            }
                        }
                    }
                    
                    // 东边
                    if (gx == LevelShape.GridWidth - 1 || !_currentShape.IsValidCell(gx + 1, gy))
                    {
                        int baseX = worldX + RoomWidth - 12;
                        for (int y = 0; y < RoomHeight; y++)
                        {
                            int depth = carveDepth + _rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(y * 0.3f) * 3);
                            for (int x = 0; x < depth; x++)
                            {
                                ClearTile(TilemapLayers.FillLayer, baseX + x, worldY + y);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除单个瓦片
        /// </summary>
        private void ClearTile(Tilemap tilemap, int x, int y)
        {
            tilemap.SetTile(new Vector3Int(x, y, 0), null);
        }
        
        /// <summary>
        /// 生成整体连贯的洞穴填充
        /// </summary>
        private void GenerateConnectedCaveFill()
        {
            // 计算有效区域的边界
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (_currentShape.IsValidCell(gx, gy))
                    {
                        int worldX = gx * RoomWidth;
                        int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                        
                        minX = Mathf.Min(minX, worldX);
                        maxX = Mathf.Max(maxX, worldX + RoomWidth);
                        minY = Mathf.Min(minY, worldY);
                        maxY = Mathf.Max(maxY, worldY + RoomHeight);
                    }
                }
            }
            
            int fillWidth = maxX - minX;
            int fillHeight = maxY - minY;
            
            if (fillWidth <= 0 || fillHeight <= 0) return;
            
            // 生成整体洞穴
            bool[,] cave = new bool[fillWidth, fillHeight];
            
            for (int y = 0; y < fillHeight; y++)
            {
                for (int x = 0; x < fillWidth; x++)
                {
                    int worldX = minX + x;
                    int worldY = minY + y;
                    
                    // 检查这个位置是否在有效房间内
                    int gx = worldX / RoomWidth;
                    int gy = LevelShape.GridHeight - 1 - worldY / RoomHeight;
                    
                    if (gx < 0 || gx >= LevelShape.GridWidth || 
                        gy < 0 || gy >= LevelShape.GridHeight ||
                        !_currentShape.IsValidCell(gx, gy))
                    {
                        continue; // 跳过无效区域
                    }
                    
                    // === 高斯堆积造山法：概率梯度场 ===
                    
                    // 1. 边缘因子
                    int distToEdge = Mathf.Min(x, y, fillWidth - 1 - x, fillHeight - 1 - y);
                    float edgeFactor = (distToEdge < 6) ? 1.5f : 1.0f;
                    
                    // 2. 垂直梯度（重力堆积）：底部概率高，顶部概率低
                    float heightRatio = (float)y / fillHeight;
                    float gravityBonus = (1.0f - heightRatio) * 0.35f; // 重力系数0.35
                    
                    // 3. 水平中心梯度：中间概率高，边缘概率低
                    float centerX = fillWidth / 2f;
                    float distToCenter = Mathf.Abs(x - centerX) / centerX;
                    float centerBonus = (1.0f - distToCenter) * 0.25f; // 聚拢系数0.25
                    
                    // 4. 最终概率计算
                    float finalProbability = FillDensity * edgeFactor + gravityBonus + centerBonus;
                    cave[x, y] = _rng.NextDouble() < finalProbability;
                }
            }
            
            // === 高斯堆积造山法：石笋注入 ===
            int stalagmiteCount = 2 + _rng.Next(2); // 2-3根石笋
            for (int i = 0; i < stalagmiteCount; i++)
            {
                // 在房间宽度20%~80%范围内随机选X坐标
                int sx = fillWidth / 5 + _rng.Next(fillWidth * 3 / 5);
                // 高度为房间高度的40%~70%
                int maxHeight = fillHeight * 2 / 5 + _rng.Next(fillHeight / 3);
                // 粗细1-2格
                int thickness = 1 + _rng.Next(2);
                
                // 从底部向上生长
                for (int dy = 0; dy < maxHeight; dy++)
                {
                    for (int dx = -thickness; dx <= thickness; dx++)
                    {
                        int px = sx + dx;
                        if (px >= 0 && px < fillWidth && dy < fillHeight)
                            cave[px, dy] = true;
                    }
                }
            }
            
            // 细胞自动机平滑 - 多次迭代确保连贯（石笋会自然融合）
            for (int i = 0; i < SmoothIterations + 3; i++)
            {
                cave = SmoothCave(cave, fillWidth, fillHeight);
            }
            
            // 绘制到填充层
            for (int y = 0; y < fillHeight; y++)
            {
                for (int x = 0; x < fillWidth; x++)
                {
                    if (cave[x, y])
                    {
                        int worldX = minX + x;
                        int worldY = minY + y;
                        
                        // 确保在有效房间内
                        int gx = worldX / RoomWidth;
                        int gy = LevelShape.GridHeight - 1 - worldY / RoomHeight;
                        
                        if (gx >= 0 && gx < LevelShape.GridWidth && 
                            gy >= 0 && gy < LevelShape.GridHeight &&
                            _currentShape.IsValidCell(gx, gy))
                        {
                            TilemapLayers.FillLayer.SetTile(
                                new Vector3Int(worldX, worldY, 0),
                                TileSet.GrayTile);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 在填充中雕刻曲折的行走通道
        /// </summary>
        private void CarveWindingPath()
        {
            int pathWidth = 6; // 通道宽度
            
            // 遍历所有有效房间，在房间内雕刻曲折通道
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    var room = _roomGrid[gx, gy];
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    int centerX = worldX + RoomWidth / 2;
                    int centerY = worldY + RoomHeight / 2;
                    
                    // 在房间内生成曲折通道
                    CarveRoomPath(worldX, worldY, room);
                    
                    // 清除房间中心区域确保可通行
                    int clearSize = 8;
                    ClearRect(TilemapLayers.FillLayer,
                        centerX - clearSize / 2, centerY - clearSize / 2,
                        clearSize, clearSize);
                }
            }
        }
        
        /// <summary>
        /// 在单个房间内雕刻曲折通道
        /// </summary>
        private void CarveRoomPath(int worldX, int worldY, RoomNode room)
        {
            int pathWidth = 5;
            int margin = WallThickness + 2;
            int innerWidth = RoomWidth - margin * 2;
            int innerHeight = RoomHeight - margin * 2;
            
            // 根据房间连接方向雕刻通道
            int centerX = worldX + RoomWidth / 2;
            int centerY = worldY + RoomHeight / 2;
            
            // 生成曲折的内部路径
            int currentX = centerX;
            int currentY = centerY;
            
            // 随机游走创建曲折通道
            for (int step = 0; step < 15; step++)
            {
                // 随机方向
                int dx = _rng.Next(-3, 4);
                int dy = _rng.Next(-3, 4);
                
                int nextX = Mathf.Clamp(currentX + dx, worldX + margin, worldX + RoomWidth - margin);
                int nextY = Mathf.Clamp(currentY + dy, worldY + margin, worldY + RoomHeight - margin);
                
                // 雕刻通道
                ClearLine(TilemapLayers.FillLayer, currentX, currentY, nextX, nextY, pathWidth);
                
                currentX = nextX;
                currentY = nextY;
            }
            
            // 确保与连接方向的通道畅通
            if (room.HasConnection(Direction.North))
            {
                ClearRect(TilemapLayers.FillLayer, centerX - pathWidth / 2, centerY, pathWidth, RoomHeight / 2);
            }
            if (room.HasConnection(Direction.South))
            {
                ClearRect(TilemapLayers.FillLayer, centerX - pathWidth / 2, worldY + margin, pathWidth, RoomHeight / 2);
            }
            if (room.HasConnection(Direction.East))
            {
                ClearRect(TilemapLayers.FillLayer, centerX, centerY - pathWidth / 2, RoomWidth / 2, pathWidth);
            }
            if (room.HasConnection(Direction.West))
            {
                ClearRect(TilemapLayers.FillLayer, worldX + margin, centerY - pathWidth / 2, RoomWidth / 2, pathWidth);
            }
        }
        
        /// <summary>
        /// 清除两点之间的线段区域
        /// </summary>
        private void ClearLine(Tilemap tilemap, int x1, int y1, int x2, int y2, int width)
        {
            int steps = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1)) + 1;
            
            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? (float)i / steps : 0;
                int x = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y1, y2, t));
                
                ClearRect(tilemap, x - width / 2, y - width / 2, width, width);
            }
        }
        
        /// <summary>
        /// 清除单个房间连接处的填充
        /// </summary>
        private void ClearRoomConnectionPassages(int worldX, int worldY, RoomNode room)
        {
            int passageWidth = 8;
            int centerX = worldX + RoomWidth / 2;
            int centerY = worldY + RoomHeight / 2;
            
            if (room.HasConnection(Direction.East))
            {
                ClearRect(TilemapLayers.FillLayer,
                    worldX + RoomWidth - passageWidth, centerY - passageWidth / 2,
                    passageWidth * 2, passageWidth);
            }
            if (room.HasConnection(Direction.West))
            {
                ClearRect(TilemapLayers.FillLayer,
                    worldX - passageWidth, centerY - passageWidth / 2,
                    passageWidth * 2, passageWidth);
            }
            if (room.HasConnection(Direction.South))
            {
                ClearRect(TilemapLayers.FillLayer,
                    centerX - passageWidth / 2, worldY - passageWidth,
                    passageWidth, passageWidth * 2);
            }
            if (room.HasConnection(Direction.North))
            {
                ClearRect(TilemapLayers.FillLayer,
                    centerX - passageWidth / 2, worldY + RoomHeight - passageWidth,
                    passageWidth, passageWidth * 2);
            }
        }
        
        /// <summary>
        /// 清除所有房间连接处的填充
        /// </summary>
        private void ClearAllConnectionPassages()
        {
            int passageWidth = 8; // 加宽通道
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    var room = _roomGrid[gx, gy];
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    int centerX = worldX + RoomWidth / 2;
                    int centerY = worldY + RoomHeight / 2;
                    
                    // 东向连接
                    if (room.HasConnection(Direction.East))
                    {
                        ClearRect(TilemapLayers.FillLayer,
                            worldX + RoomWidth - passageWidth, centerY - passageWidth / 2,
                            passageWidth * 2, passageWidth);
                    }
                    
                    // 南向连接
                    if (room.HasConnection(Direction.South))
                    {
                        ClearRect(TilemapLayers.FillLayer,
                            centerX - passageWidth / 2, worldY - passageWidth,
                            passageWidth, passageWidth * 2);
                    }
                    
                    // 为每个有效房间清除中心区域，确保可通行
                    int clearSize = 6;
                    ClearRect(TilemapLayers.FillLayer,
                        centerX - clearSize / 2, centerY - clearSize / 2,
                        clearSize, clearSize);
                }
            }
        }
        
        /// <summary>
        /// 细胞自动机平滑
        /// </summary>
        private bool[,] SmoothCave(bool[,] cave, int width, int height)
        {
            bool[,] newCave = new bool[width, height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int neighbors = CountNeighbors(cave, x, y, width, height);
                    
                    if (neighbors > 4)
                        newCave[x, y] = true;
                    else if (neighbors < 4)
                        newCave[x, y] = false;
                    else
                        newCave[x, y] = cave[x, y];
                }
            }
            
            return newCave;
        }
        
        /// <summary>
        /// 计算邻居数量
        /// </summary>
        private int CountNeighbors(bool[,] cave, int x, int y, int width, int height)
        {
            int count = 0;
            
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        count++; // 边界算作墙
                    else if (cave[nx, ny])
                        count++;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// 绘制房间连接通道
        /// </summary>
        private void DrawRoomConnections()
        {
            int passageWidth = 4;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    var room = _roomGrid[gx, gy];
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    int centerX = worldX + RoomWidth / 2;
                    int centerY = worldY + RoomHeight / 2;
                    
                    // 清除连接区域的填充（创建通道）
                    // 东向连接
                    if (room.HasConnection(Direction.East) && gx < LevelShape.GridWidth - 1)
                    {
                        ClearRect(TilemapLayers.FillLayer,
                            worldX + RoomWidth - passageWidth, centerY - passageWidth / 2,
                            passageWidth * 2, passageWidth);
                    }
                    
                    // 南向连接
                    if (room.HasConnection(Direction.South) && gy < LevelShape.GridHeight - 1)
                    {
                        ClearRect(TilemapLayers.FillLayer,
                            centerX - passageWidth / 2, worldY - passageWidth,
                            passageWidth, passageWidth * 2);
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制入口和出口 - 智能选择最佳墙壁方向
        /// </summary>
        private void DrawEntranceAndExit()
        {
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    var room = _roomGrid[gx, gy];
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    int centerX = worldX + RoomWidth / 2;
                    int centerY = worldY + RoomHeight / 2;
                    
                    if (room.Type == RoomType.Start)
                    {
                        // 0号房间（起始房间）只有出口 - 玩家从这里出发
                        EntranceDirection dir = GetBestExitDirection(gx, gy);
                        DrawExit(worldX, worldY, centerX, centerY, dir);
                    }
                    else if (room.Type == RoomType.Exit)
                    {
                        // 最后一个房间只有入口 - 玩家到达这里结束
                        EntranceDirection dir = GetBestEntranceDirection(gx, gy);
                        DrawEntrance(worldX, worldY, centerX, centerY, dir);
                    }
                }
            }
        }
        
        /// <summary>
        /// 出入口方向枚举
        /// </summary>
        private enum EntranceDirection { Top, Bottom, Left, Right }
        
        /// <summary>
        /// 获取最佳入口方向 - 入口始终从顶部进入
        /// </summary>
        private EntranceDirection GetBestEntranceDirection(int gx, int gy)
        {
            // 入口房间位于顶排，始终从顶部进入
            return EntranceDirection.Top;
        }
        
        /// <summary>
        /// 获取最佳出口方向 - 出口始终从底部离开
        /// </summary>
        private EntranceDirection GetBestExitDirection(int gx, int gy)
        {
            // 出口房间位于底排，始终从底部离开
            return EntranceDirection.Bottom;
        }
        
        /// <summary>
        /// 绘制入口 - 在入口房间的顶部墙壁上
        /// </summary>
        private void DrawEntrance(int worldX, int worldY, int centerX, int centerY, EntranceDirection dir)
        {
            // 入口在房间顶部墙壁位置
            int roomTopY = worldY + RoomHeight;
            int entranceX = centerX - EntranceWidth / 2;
            int entranceY = roomTopY - WallThickness;
            
            // 清除墙壁并绘制入口
            ClearRect(TilemapLayers.GroundLayer, entranceX, entranceY, EntranceWidth, WallThickness + 1);
            
            // 清除入口下方区域的填充
            ClearRect(TilemapLayers.GroundLayer, entranceX - 2, entranceY - EntranceHeight - 2, EntranceWidth + 4, EntranceHeight + 4);
            
            // 入口下方的落脚平台
            FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile, entranceX - 1, entranceY - EntranceHeight - 1, EntranceWidth + 2, 1);
        }
        
        /// <summary>
        /// 绘制出口 - 在出口房间的底部墙壁上
        /// </summary>
        private void DrawExit(int worldX, int worldY, int centerX, int centerY, EntranceDirection dir)
        {
            // 出口在房间底部墙壁位置
            int exitX = centerX - EntranceWidth / 2;
            int exitY = worldY;
            
            // 清除墙壁并绘制出口
            ClearRect(TilemapLayers.GroundLayer, exitX, exitY, EntranceWidth, WallThickness + 1);
            
            // 清除出口上方区域的填充
            ClearRect(TilemapLayers.GroundLayer, exitX - 2, exitY + WallThickness, EntranceWidth + 4, EntranceHeight + 2);
            
            // 出口上方的平台
            FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile, exitX - 1, exitY + WallThickness, EntranceWidth + 2, 1);
        }
        
        /// <summary>
        /// 绘制平台 - 基于玩家跳跃力计算合理高度
        /// </summary>
        private void DrawPlatforms()
        {
            // 基于跳跃力计算最大可达高度 (物理公式简化)
            int maxJumpHeight = Mathf.Min(MaxPlatformHeightDiff, Mathf.FloorToInt(PlayerJumpForce * 0.5f));
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    // 跳过入口和出口房间的平台生成
                    var room = _roomGrid[gx, gy];
                    if (room.Type == RoomType.Start || room.Type == RoomType.Exit) continue;
                    
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    
                    // 生成阶梯式平台系统
                    int platformLayers = RoomHeight / (maxJumpHeight + MinPlatformGap);
                    platformLayers = Mathf.Clamp(platformLayers, 1, 3);
                    
                    int lastY = worldY + WallThickness + 1; // 从底部开始
                    
                    for (int layer = 0; layer < platformLayers; layer++)
                    {
                        // 计算当前层高度（确保玩家能跳上去）
                        int layerHeight = lastY + MinPlatformGap + _rng.Next(maxJumpHeight - MinPlatformGap + 1);
                        
                        // 确保不超出房间顶部
                        if (layerHeight > worldY + RoomHeight - 4) break;
                        
                        // 随机平台位置和宽度
                        int pw = 4 + _rng.Next(RoomWidth / 3);
                        int px = worldX + 2 + _rng.Next(RoomWidth - pw - 4);
                        
                        // 绘制平台
                        FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile,
                            px, layerHeight, pw, 1);
                        
                        // 清除平台上方的填充（给玩家站立空间）
                        ClearRect(TilemapLayers.GroundLayer,
                            px - 1, layerHeight, pw + 2, 4);
                        
                        lastY = layerHeight;
                    }
                }
            }
            
            // 为所有垂直出口生成阶梯
            GenerateVerticalExitStaircases();
        }
        
        /// <summary>
        /// 为所有房间的垂直方向出口生成阶梯，确保玩家能够到达
        /// </summary>
        private void GenerateVerticalExitStaircases()
        {
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    var room = _roomGrid[gx, gy];
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    
                    // 检查北向出口（需要向上到达）
                    if (room.HasConnection(Direction.North))
                    {
                        GenerateUpwardStaircase(worldX, worldY);
                    }
                    
                    // 检查南向入口（需要确保有安全着陆区域）
                    if (room.HasConnection(Direction.South))
                    {
                        GenerateDownwardPath(worldX, worldY);
                    }
                }
            }
        }
        
        /// <summary>
        /// 生成通往北向出口的向上阶梯
        /// </summary>
        private void GenerateUpwardStaircase(int worldX, int worldY)
        {
            // 计算出口踏板Y坐标（北向出口在房间顶部）
            int exitY = worldY + RoomHeight - WallThickness - 2;
            int groundY = worldY + WallThickness + 1;
            
            // 检查是否需要阶梯
            int heightDiff = exitY - groundY;
            if (heightDiff <= StaircaseSafeHeight) return;
            
            // 房间边界
            int roomLeft = worldX + WallThickness + 2;
            int roomRight = worldX + RoomWidth - WallThickness - StaircasePlatformWidth - 2;
            int roomCenterX = worldX + RoomWidth / 2;
            
            // 当前锚点（从出口开始向下）
            int currentY = exitY;
            bool placeLeft = true;
            
            // 循环生成阶梯
            while (currentY - groundY > StaircaseSafeHeight)
            {
                int newY = currentY - StaircaseSafeHeight;
                
                // 计算X位置（交替偏移，防撞头）
                int newX = placeLeft 
                    ? Mathf.Clamp(roomCenterX - StaircaseHorizontalOffset, roomLeft, roomRight)
                    : Mathf.Clamp(roomCenterX + StaircaseHorizontalOffset, roomLeft, roomRight);
                
                // 生成平台
                FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile,
                    newX, newY, StaircasePlatformWidth, 1);
                
                // 清除平台上方空间（防止卡头）
                ClearRect(TilemapLayers.GroundLayer,
                    newX - 1, newY + 1, StaircasePlatformWidth + 2, 3);
                
                currentY = newY;
                placeLeft = !placeLeft;
            }
        }
        
        /// <summary>
        /// 确保南向入口下方有安全着陆区域
        /// </summary>
        private void GenerateDownwardPath(int worldX, int worldY)
        {
            // 南向入口：玩家从上方掉落进入
            // 确保入口下方有安全着陆平台
            
            int entranceX = worldX + RoomWidth / 2;
            int entranceY = worldY + WallThickness + 2;
            
            // 检查入口下方是否有地面
            Vector3Int belowPos = new Vector3Int(entranceX, entranceY - 1, 0);
            if (TilemapLayers.GroundLayer.GetTile(belowPos) == null &&
                TilemapLayers.PlatformLayer.GetTile(belowPos) == null)
            {
                // 生成着陆平台
                FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile,
                    entranceX - 2, entranceY - 1, 5, 1);
            }
        }
        
        /// <summary>
        /// 绘制特殊区域 - 商店占用整个房间，保持通道
        /// </summary>
        private void DrawSpecialAreas()
        {
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    var room = _roomGrid[gx, gy];
                    int worldX = gx * RoomWidth;
                    int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    int centerX = worldX + RoomWidth / 2;
                    int centerY = worldY + RoomHeight / 2;
                    
                    if (room.Type == RoomType.Shop)
                    {
                        // 商店占用整个房间，中央区域为商店空间
                        int shopAreaSize = 8;
                        
                        // 清除商店中央区域的填充
                        ClearRect(TilemapLayers.GroundLayer,
                            centerX - shopAreaSize / 2 - 1, centerY - shopAreaSize / 2 - 1, 
                            shopAreaSize + 2, shopAreaSize + 2);
                        
                        // 商店地面平台
                        FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile,
                            centerX - shopAreaSize / 2 - 1, centerY - shopAreaSize / 2 - 1, 
                            shopAreaSize + 2, 1);
                        
                        // 保持商店与主路径的通道
                        ClearRoomConnectionPassages(worldX, worldY, room);
                    }
                    else if (room.Type == RoomType.Boss)
                    {
                        // Boss房间 - 更大的空间
                        int bossAreaSize = 10;
                        
                        // 清除Boss房间中央区域的填充
                        ClearRect(TilemapLayers.GroundLayer,
                            worldX + 3, worldY + 3, RoomWidth - 6, RoomHeight - 6);
                        
                        // Boss房间地面
                        FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile,
                            worldX + 3, worldY + 3, RoomWidth - 6, 1);
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充矩形区域
        /// </summary>
        private void FillRect(Tilemap tilemap, TileBase tile, int x, int y, int width, int height)
        {
            if (tilemap == null || tile == null) return;
            
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    tilemap.SetTile(new Vector3Int(x + dx, y + dy, 0), tile);
                }
            }
        }
        
        /// <summary>
        /// 清除矩形区域
        /// </summary>
        private void ClearRect(Tilemap tilemap, int x, int y, int width, int height)
        {
            if (tilemap == null) return;
            
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    tilemap.SetTile(new Vector3Int(x + dx, y + dy, 0), null);
                }
            }
        }
    }
}
