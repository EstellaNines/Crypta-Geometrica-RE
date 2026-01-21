// V3版本已废弃，抑制未使用变量警告
#pragma warning disable CS0219

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 地形原语枚举：定义每个4x4子格子的地形类型
    /// 用于基于拓扑原语的多地形分块生成系统
    /// </summary>
    public enum TerrainArchetype
    {
        /// <summary>实心岩石（非路径区域）</summary>
        Solid,
        /// <summary>完全空旷（高空区域）</summary>
        Open,
        /// <summary>水平直通隧道</summary>
        Corridor,
        /// <summary>垂直竖井</summary>
        Shaft,
        /// <summary>拐角：左通 & 下通</summary>
        Corner_BL,
        /// <summary>拐角：左通 & 上通</summary>
        Corner_TL,
        /// <summary>拐角：右通 & 下通</summary>
        Corner_BR,
        /// <summary>拐角：右通 & 上通</summary>
        Corner_TR,
        /// <summary>正向阶梯 (/)</summary>
        Stairs_Pos,
        /// <summary>负向阶梯 (\)</summary>
        Stairs_Neg,
        /// <summary>山体基座 (金字塔底)</summary>
        Mountain_Base,
        /// <summary>山峰 (金字塔尖)</summary>
        Mountain_Peak,
        /// <summary>稀疏平台 (跳跳乐)</summary>
        Platforms_Sparse,
        /// <summary>T型交叉 - 左右下</summary>
        T_Junction_LRD,
        /// <summary>T型交叉 - 左右上</summary>
        T_Junction_LRU,
        /// <summary>十字交叉</summary>
        Cross_Junction,
        /// <summary>着陆区（带平台的垂直入口）</summary>
        Landing_Zone
    }

    /// <summary>
    /// 灰盒关卡生成器 - Spelunky风格
    /// 生成4×4整体网格，无房间间隙，只有外围墙壁
    /// 使用基于拓扑原语的多地形分块生成系统
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
        
        // === 基于拓扑原语的地形生成系统 ===
        /// <summary>关键路径节点列表</summary>
        private List<Vector2Int> _criticalPath = new List<Vector2Int>();
        /// <summary>地形数据数组：1=墙壁, 0=空气</summary>
        private int[,] _map;
        /// <summary>每个网格单元的地形原语类型</summary>
        private TerrainArchetype[,] _archetypeGrid;
        
        // === 掩码保护特征注入：地形模板 ===
        private static readonly int[][,] FeaturePatterns = new int[][,]
        {
            // 形状A: 3x3 实心块（大岛屿核心）
            new int[,] { {1,1,1}, {1,1,1}, {1,1,1} },
            // 形状B: 十字型（连接点）
            new int[,] { {0,1,0}, {1,1,1}, {0,1,0} },
            // 形状C: U型（口袋地形）
            new int[,] { {1,0,1}, {1,0,1}, {1,1,1} },
            // 形状D: L型（拐角）
            new int[,] { {1,1,0}, {1,0,0}, {1,1,1} },
            // 形状E: T型（分叉）
            new int[,] { {1,1,1}, {0,1,0}, {0,1,0} }
        };
        
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
            
            // 注意：平台生成已由拓扑原语系统（TerrainArchetype）内置处理
            // 如需额外平台，可取消下面注释
            // DrawPlatforms();
            
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
            
            // 第三步：设置入口和出口，保存关键路径
            _criticalPath = new List<Vector2Int>(path);
            
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
        /// 绘制洞穴填充 - 基于拓扑原语的多地形分块生成系统
        /// 4步流程：拓扑分析 → 原语映射 → 确定性光栅化 → 安全修正
        /// </summary>
        private void DrawCaveFill()
        {
            // === 基于拓扑原语的多地形分块生成系统 ===
            
            // 第一步：初始化地形数据
            InitializeTerrainMap();
            
            // 第二步：分析网格拓扑并分配原语类型
            AnalyzeGridTopology();
            
            // 第三步：确定性光栅化 - 填充每个Chunk
            RasterizeAllChunks();
            
            // 第四步：安全修正 - 确保路径连通性
            CarvePathConnections();
            
            // 第五步：将地形数据渲染到Tilemap
            RenderTerrainToTilemap();
            
            // 第六步：将表层地板替换为白色瓦片
            ApplySurfaceTiles();
        }
        
        #region 基于拓扑原语的地形生成系统
        
        /// <summary>
        /// 初始化地形数据数组
        /// </summary>
        private void InitializeTerrainMap()
        {
            _map = new int[TotalWidth, TotalHeight];
            _archetypeGrid = new TerrainArchetype[LevelShape.GridWidth, LevelShape.GridHeight];
            
            // 初始化所有位置为实心墙壁
            for (int y = 0; y < TotalHeight; y++)
            {
                for (int x = 0; x < TotalWidth; x++)
                {
                    _map[x, y] = 1; // 1 = 墙壁
                }
            }
            
            // 初始化所有网格单元为实心
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    _archetypeGrid[gx, gy] = TerrainArchetype.Solid;
                }
            }
        }
        
        /// <summary>
        /// 分析网格拓扑并为每个格子分配TerrainArchetype
        /// </summary>
        private void AnalyzeGridTopology()
        {
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    Vector2Int gridPos = new Vector2Int(gx, gy);
                    _archetypeGrid[gx, gy] = DetermineArchetype(gridPos);
                }
            }
        }
        
        /// <summary>
        /// 根据网格位置的连接关系确定地形原语类型
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        /// <returns>地形原语类型</returns>
        private TerrainArchetype DetermineArchetype(Vector2Int gridPos)
        {
            int gx = gridPos.x;
            int gy = gridPos.y;
            
            // 检查是否在关键路径上
            bool isOnPath = _criticalPath.Contains(gridPos);
            
            if (!_currentShape.IsValidCell(gx, gy))
            {
                return TerrainArchetype.Solid; // 无效区域为实心
            }
            
            if (!isOnPath)
            {
                // 非路径区域：根据与路径的相对位置决定类型
                return DetermineOffPathArchetype(gridPos);
            }
            
            // 路径区域：分析连接方向
            var room = _roomGrid[gx, gy];
            bool hasNorth = room.HasConnection(Direction.North);
            bool hasSouth = room.HasConnection(Direction.South);
            bool hasEast = room.HasConnection(Direction.East);
            bool hasWest = room.HasConnection(Direction.West);
            
            int horizontalCount = (hasEast ? 1 : 0) + (hasWest ? 1 : 0);
            int verticalCount = (hasNorth ? 1 : 0) + (hasSouth ? 1 : 0);
            int totalConnections = horizontalCount + verticalCount;
            
            // 十字交叉
            if (totalConnections == 4)
            {
                return TerrainArchetype.Cross_Junction;
            }
            
            // T型交叉
            if (totalConnections == 3)
            {
                if (!hasNorth) return TerrainArchetype.T_Junction_LRD;
                if (!hasSouth) return TerrainArchetype.T_Junction_LRU;
                // 其他T型情况使用走廊
                return TerrainArchetype.Corridor;
            }
            
            // 纯水平连接
            if (horizontalCount == 2 && verticalCount == 0)
            {
                return TerrainArchetype.Corridor;
            }
            
            // 纯垂直连接
            if (verticalCount == 2 && horizontalCount == 0)
            {
                return TerrainArchetype.Shaft;
            }
            
            // 垂直+水平混合（拐角或着陆区）
            if (verticalCount >= 1 && horizontalCount >= 1)
            {
                // 如果有向下的连接，需要着陆区
                if (hasSouth && (hasEast || hasWest))
                {
                    return TerrainArchetype.Landing_Zone;
                }
                
                // 拐角类型
                if (hasNorth && hasEast) return TerrainArchetype.Corner_TR;
                if (hasNorth && hasWest) return TerrainArchetype.Corner_TL;
                if (hasSouth && hasEast) return TerrainArchetype.Corner_BR;
                if (hasSouth && hasWest) return TerrainArchetype.Corner_BL;
                
                // 默认使用着陆区
                return TerrainArchetype.Landing_Zone;
            }
            
            // 只有单向连接（起点或终点）
            if (totalConnections == 1)
            {
                if (hasNorth || hasSouth)
                {
                    return TerrainArchetype.Shaft;
                }
                return TerrainArchetype.Corridor;
            }
            
            // 默认使用走廊
            return TerrainArchetype.Corridor;
        }
        
        /// <summary>
        /// 确定非路径区域的地形类型
        /// </summary>
        private TerrainArchetype DetermineOffPathArchetype(Vector2Int gridPos)
        {
            int gx = gridPos.x;
            int gy = gridPos.y;
            
            // 查找最近的路径节点
            int minPathY = int.MaxValue;
            int maxPathY = int.MinValue;
            
            foreach (var pathNode in _criticalPath)
            {
                if (pathNode.y < minPathY) minPathY = pathNode.y;
                if (pathNode.y > maxPathY) maxPathY = pathNode.y;
            }
            
            // 如果在路径上方（y值更小），使用山峰或开放空间
            if (gy < minPathY)
            {
                return TerrainArchetype.Open;
            }
            
            // 如果在路径下方（y值更大），使用山体基座支撑
            if (gy > maxPathY)
            {
                return TerrainArchetype.Mountain_Base;
            }
            
            // 如果与路径同层但不在路径上
            // 检查是否有相邻的路径节点
            bool hasAdjacentPath = false;
            foreach (var pathNode in _criticalPath)
            {
                if (Mathf.Abs(pathNode.x - gx) <= 1 && pathNode.y == gy)
                {
                    hasAdjacentPath = true;
                    break;
                }
            }
            
            if (hasAdjacentPath)
            {
                // 相邻路径的侧室可以使用稀疏平台
                return _rng.NextDouble() < 0.3 ? TerrainArchetype.Platforms_Sparse : TerrainArchetype.Mountain_Base;
            }
            
            // 远离路径的区域使用实心或山体
            return TerrainArchetype.Mountain_Base;
        }
        
        /// <summary>
        /// 确定性光栅化 - 遍历所有Chunk并填充
        /// </summary>
        private void RasterizeAllChunks()
        {
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    TerrainArchetype archetype = _archetypeGrid[gx, gy];
                    FillChunk(gx, gy, archetype);
                }
            }
        }
        
        /// <summary>
        /// 填充单个Chunk的像素数据
        /// 使用数学函数（线性、正弦、矩形）确定性填充
        /// </summary>
        /// <param name="gx">网格X坐标</param>
        /// <param name="gy">网格Y坐标</param>
        /// <param name="archetype">地形原语类型</param>
        private void FillChunk(int gx, int gy, TerrainArchetype archetype)
        {
            // 计算Chunk在世界坐标中的范围
            int worldX = gx * RoomWidth;
            int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
            
            int floorHeight = 4;    // 地板厚度
            int ceilingHeight = 4;  // 天花板厚度
            int wallWidth = 3;      // 墙壁宽度
            int passageWidth = 6;   // 通道宽度（玩家约1.5格，需要至少3格）
            
            switch (archetype)
            {
                case TerrainArchetype.Solid:
                    // 全部填充为墙壁（已在初始化时完成）
                    break;
                    
                case TerrainArchetype.Open:
                    // 完全清空（高空区域）
                    FillChunkRect(worldX, worldY, RoomWidth, RoomHeight, 0);
                    break;
                    
                case TerrainArchetype.Corridor:
                    // 水平走廊：底部和顶部是墙，中间是空
                    FillChunkCorridor(worldX, worldY, floorHeight, ceilingHeight);
                    break;
                    
                case TerrainArchetype.Shaft:
                    // 垂直竖井：左右是墙，中间是空
                    FillChunkShaft(worldX, worldY, wallWidth);
                    break;
                    
                case TerrainArchetype.Corner_BL:
                    FillChunkCorner(worldX, worldY, false, true, floorHeight, wallWidth);
                    break;
                    
                case TerrainArchetype.Corner_TL:
                    FillChunkCorner(worldX, worldY, false, false, floorHeight, wallWidth);
                    break;
                    
                case TerrainArchetype.Corner_BR:
                    FillChunkCorner(worldX, worldY, true, true, floorHeight, wallWidth);
                    break;
                    
                case TerrainArchetype.Corner_TR:
                    FillChunkCorner(worldX, worldY, true, false, floorHeight, wallWidth);
                    break;
                    
                case TerrainArchetype.Stairs_Pos:
                    // 正向阶梯 (/)
                    FillChunkStairs(worldX, worldY, true);
                    break;
                    
                case TerrainArchetype.Stairs_Neg:
                    // 负向阶梯 (\)
                    FillChunkStairs(worldX, worldY, false);
                    break;
                    
                case TerrainArchetype.Mountain_Base:
                    // 山体基座（金字塔底部）
                    FillChunkMountainBase(worldX, worldY);
                    break;
                    
                case TerrainArchetype.Mountain_Peak:
                    // 山峰
                    FillChunkMountainPeak(worldX, worldY);
                    break;
                    
                case TerrainArchetype.Platforms_Sparse:
                    // 稀疏平台
                    FillChunkPlatformsSparse(worldX, worldY);
                    break;
                    
                case TerrainArchetype.T_Junction_LRD:
                    // T型交叉 - 左右下通
                    FillChunkTJunction(worldX, worldY, true, true, false, true, floorHeight, wallWidth);
                    break;
                    
                case TerrainArchetype.T_Junction_LRU:
                    // T型交叉 - 左右上通
                    FillChunkTJunction(worldX, worldY, true, true, true, false, floorHeight, wallWidth);
                    break;
                    
                case TerrainArchetype.Cross_Junction:
                    // 十字交叉
                    FillChunkCrossJunction(worldX, worldY, floorHeight, wallWidth);
                    break;
                    
                case TerrainArchetype.Landing_Zone:
                    // 着陆区
                    FillChunkLandingZone(worldX, worldY, floorHeight, wallWidth);
                    break;
            }
        }
        
        /// <summary>
        /// 填充Chunk矩形区域
        /// </summary>
        private void FillChunkRect(int x, int y, int width, int height, int value)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        _map[px, py] = value;
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充水平走廊
        /// </summary>
        private void FillChunkCorridor(int worldX, int worldY, int floorHeight, int ceilingHeight)
        {
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        // 地板区域
                        if (dy < floorHeight)
                        {
                            _map[px, py] = 1;
                        }
                        // 天花板区域
                        else if (dy >= RoomHeight - ceilingHeight)
                        {
                            _map[px, py] = 1;
                        }
                        // 中间空区域
                        else
                        {
                            _map[px, py] = 0;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充垂直竖井
        /// </summary>
        private void FillChunkShaft(int worldX, int worldY, int wallWidth)
        {
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        // 左墙
                        if (dx < wallWidth)
                        {
                            _map[px, py] = 1;
                        }
                        // 右墙
                        else if (dx >= RoomWidth - wallWidth)
                        {
                            _map[px, py] = 1;
                        }
                        // 中间空区域
                        else
                        {
                            _map[px, py] = 0;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充拐角
        /// </summary>
        private void FillChunkCorner(int worldX, int worldY, bool rightSide, bool bottomOpen, int floorHeight, int wallWidth)
        {
            int centerX = RoomWidth / 2;
            int centerY = RoomHeight / 2;
            
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        bool isWall = true;
                        
                        // 水平通道部分
                        bool inHorizontalPassage = dy >= floorHeight && dy < RoomHeight - floorHeight;
                        // 垂直通道部分
                        bool inVerticalPassage = dx >= wallWidth && dx < RoomWidth - wallWidth;
                        
                        if (rightSide)
                        {
                            // 右侧开口
                            if (inHorizontalPassage && dx >= centerX - wallWidth)
                                isWall = false;
                        }
                        else
                        {
                            // 左侧开口
                            if (inHorizontalPassage && dx < centerX + wallWidth)
                                isWall = false;
                        }
                        
                        if (bottomOpen)
                        {
                            // 下方开口
                            if (inVerticalPassage && dy < centerY + floorHeight)
                                isWall = false;
                        }
                        else
                        {
                            // 上方开口
                            if (inVerticalPassage && dy >= centerY - floorHeight)
                                isWall = false;
                        }
                        
                        _map[px, py] = isWall ? 1 : 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充阶梯
        /// </summary>
        private void FillChunkStairs(int worldX, int worldY, bool positive)
        {
            int stepHeight = 2;
            int stepWidth = RoomWidth / 4;
            
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        float slope = positive 
                            ? (float)dy / RoomHeight 
                            : 1.0f - (float)dy / RoomHeight;
                        
                        float xRatio = (float)dx / RoomWidth;
                        
                        // 阶梯线
                        bool isFloor = xRatio < slope + 0.15f && xRatio > slope - 0.15f && dy < RoomHeight - 3;
                        // 阶梯下方填充
                        bool isBelowStairs = positive 
                            ? (dy < dx * RoomHeight / RoomWidth)
                            : (dy < (RoomWidth - dx) * RoomHeight / RoomWidth);
                        
                        _map[px, py] = (isFloor || isBelowStairs) ? 1 : 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充山体基座
        /// </summary>
        private void FillChunkMountainBase(int worldX, int worldY)
        {
            int centerX = RoomWidth / 2;
            
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        // 金字塔形状：距离中心越远，高度越低
                        int distFromCenter = Mathf.Abs(dx - centerX);
                        int pyramidHeight = RoomHeight - distFromCenter;
                        
                        // 底部实心，顶部逐渐变窄
                        bool isWall = dy < pyramidHeight;
                        _map[px, py] = isWall ? 1 : 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充山峰
        /// </summary>
        private void FillChunkMountainPeak(int worldX, int worldY)
        {
            int centerX = RoomWidth / 2;
            int peakHeight = RoomHeight * 2 / 3;
            
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        // 倒置金字塔（尖朝上）
                        int distFromCenter = Mathf.Abs(dx - centerX);
                        int threshold = (RoomHeight - dy) * RoomWidth / (2 * peakHeight);
                        
                        bool isWall = distFromCenter < threshold && dy < peakHeight;
                        _map[px, py] = isWall ? 1 : 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充稀疏平台
        /// </summary>
        private void FillChunkPlatformsSparse(int worldX, int worldY)
        {
            // 清空区域
            FillChunkRect(worldX, worldY, RoomWidth, RoomHeight, 0);
            
            // 添加边框
            int borderWidth = 2;
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    if (dx < borderWidth || dx >= RoomWidth - borderWidth ||
                        dy < borderWidth || dy >= RoomHeight - borderWidth)
                    {
                        int px = worldX + dx;
                        int py = worldY + dy;
                        if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                        {
                            _map[px, py] = 1;
                        }
                    }
                }
            }
            
            // 添加稀疏平台（确保间距小于4格）
            int[] platformYs = { 4, 8, 12 };
            int platformWidth = 4;
            
            foreach (int relY in platformYs)
            {
                if (relY >= RoomHeight - 3) continue;
                
                // 随机水平位置
                int relX = 3 + _rng.Next(RoomWidth - platformWidth - 6);
                
                for (int dx = 0; dx < platformWidth; dx++)
                {
                    int px = worldX + relX + dx;
                    int py = worldY + relY;
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        _map[px, py] = 1;
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充T型交叉
        /// </summary>
        private void FillChunkTJunction(int worldX, int worldY, bool left, bool right, bool up, bool down, int floorHeight, int wallWidth)
        {
            int centerX = RoomWidth / 2;
            int centerY = RoomHeight / 2;
            int passageHalfWidth = (RoomWidth - 2 * wallWidth) / 2;
            int passageHalfHeight = (RoomHeight - 2 * floorHeight) / 2;
            
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        bool isWall = true;
                        
                        // 水平通道
                        bool inHorizontalPassage = dy >= floorHeight && dy < RoomHeight - floorHeight;
                        // 垂直通道
                        bool inVerticalPassage = dx >= wallWidth && dx < RoomWidth - wallWidth;
                        
                        // 中心区域
                        if (inHorizontalPassage && inVerticalPassage)
                        {
                            isWall = false;
                        }
                        
                        // 左侧通道
                        if (left && inHorizontalPassage && dx < centerX)
                        {
                            isWall = false;
                        }
                        
                        // 右侧通道
                        if (right && inHorizontalPassage && dx >= centerX)
                        {
                            isWall = false;
                        }
                        
                        // 上方通道
                        if (up && inVerticalPassage && dy >= centerY)
                        {
                            isWall = false;
                        }
                        
                        // 下方通道
                        if (down && inVerticalPassage && dy < centerY)
                        {
                            isWall = false;
                        }
                        
                        _map[px, py] = isWall ? 1 : 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// 填充十字交叉
        /// </summary>
        private void FillChunkCrossJunction(int worldX, int worldY, int floorHeight, int wallWidth)
        {
            FillChunkTJunction(worldX, worldY, true, true, true, true, floorHeight, wallWidth);
        }
        
        /// <summary>
        /// 填充着陆区
        /// </summary>
        private void FillChunkLandingZone(int worldX, int worldY, int floorHeight, int wallWidth)
        {
            int centerX = RoomWidth / 2;
            int platformY = floorHeight + 2;
            int platformWidth = RoomWidth - 2 * wallWidth - 4;
            
            // 先清空区域
            FillChunkRect(worldX, worldY, RoomWidth, RoomHeight, 0);
            
            // 添加边框墙壁
            for (int dy = 0; dy < RoomHeight; dy++)
            {
                for (int dx = 0; dx < RoomWidth; dx++)
                {
                    int px = worldX + dx;
                    int py = worldY + dy;
                    
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        // 左右墙壁（只在非通道区域）
                        if (dx < wallWidth || dx >= RoomWidth - wallWidth)
                        {
                            // 保留水平通道入口
                            if (dy < floorHeight || dy >= RoomHeight - floorHeight)
                            {
                                _map[px, py] = 1;
                            }
                        }
                        
                        // 底部地板
                        if (dy < floorHeight)
                        {
                            _map[px, py] = 1;
                        }
                    }
                }
            }
            
            // 添加着陆平台（在垂直通道中间）
            for (int dx = 0; dx < platformWidth; dx++)
            {
                int px = worldX + wallWidth + 2 + dx;
                int py = worldY + RoomHeight / 2;
                if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                {
                    _map[px, py] = 1;
                }
            }
        }
        
        /// <summary>
        /// 安全修正 - 在关键路径连接处强制清除通道
        /// 确保玩家（1.5x1.5尺寸）可以通过
        /// </summary>
        private void CarvePathConnections()
        {
            int carveWidth = 4; // 雕刻宽度，确保玩家可通过
            
            // 遍历关键路径，在相邻节点之间雕刻连接通道
            for (int i = 0; i < _criticalPath.Count; i++)
            {
                Vector2Int current = _criticalPath[i];
                var room = _roomGrid[current.x, current.y];
                
                int worldX = current.x * RoomWidth;
                int worldY = (LevelShape.GridHeight - 1 - current.y) * RoomHeight;
                int centerX = worldX + RoomWidth / 2;
                int centerY = worldY + RoomHeight / 2;
                
                // 清除房间中心区域
                CarveRect(centerX - carveWidth, centerY - carveWidth, carveWidth * 2, carveWidth * 2);
                
                // 处理每个方向的连接
                if (room.HasConnection(Direction.East))
                {
                    // 向东的连接
                    int exitX = worldX + RoomWidth - carveWidth;
                    CarveRect(exitX, centerY - carveWidth / 2, carveWidth * 2, carveWidth);
                }
                
                if (room.HasConnection(Direction.West))
                {
                    // 向西的连接
                    int exitX = worldX - carveWidth;
                    CarveRect(exitX, centerY - carveWidth / 2, carveWidth * 2, carveWidth);
                }
                
                if (room.HasConnection(Direction.South))
                {
                    // 向南的连接（Unity中Y向上，所以South是Y减小）
                    int exitY = worldY - carveWidth;
                    CarveRect(centerX - carveWidth / 2, exitY, carveWidth, carveWidth * 2);
                }
                
                if (room.HasConnection(Direction.North))
                {
                    // 向北的连接
                    int exitY = worldY + RoomHeight - carveWidth;
                    CarveRect(centerX - carveWidth / 2, exitY, carveWidth, carveWidth * 2);
                }
            }
        }
        
        /// <summary>
        /// 在地形数据中雕刻矩形空洞
        /// </summary>
        private void CarveRect(int x, int y, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && px < TotalWidth && py >= 0 && py < TotalHeight)
                    {
                        _map[px, py] = 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// 将地形数据渲染到Tilemap
        /// </summary>
        private void RenderTerrainToTilemap()
        {
            for (int y = 0; y < TotalHeight; y++)
            {
                for (int x = 0; x < TotalWidth; x++)
                {
                    // 检查是否在有效房间区域内
                    int gx = x / RoomWidth;
                    int gy = LevelShape.GridHeight - 1 - y / RoomHeight;
                    
                    if (gx >= 0 && gx < LevelShape.GridWidth && 
                        gy >= 0 && gy < LevelShape.GridHeight &&
                        _currentShape.IsValidCell(gx, gy))
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        
                        if (_map[x, y] == 1)
                        {
                            TilemapLayers.FillLayer.SetTile(pos, TileSet.GrayTile);
                        }
                        else
                        {
                            TilemapLayers.FillLayer.SetTile(pos, null);
                        }
                    }
                }
            }
        }
        
        #endregion
        
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
        
        #region 旧版洞穴生成方法（已废弃，保留供参考）
        // 以下方法已被基于拓扑原语的多地形分块生成系统替代
        // 如需恢复旧版行为，可取消注释并修改 DrawCaveFill() 方法
        
        /*
        /// <summary>
        /// [已废弃] 生成整体连贯的洞穴填充
        /// </summary>
        private void GenerateConnectedCaveFill_Legacy()
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
            
            // === 掩码保护特征注入 ===
            InjectTerrainFeatures(cave, fillWidth, fillHeight, minX, minY);
            
            // 细胞自动机平滑 - 多次迭代确保连贯（石笋和特征会自然融合）
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
        /// 掩码保护特征注入：在非路径区域注入地形特征，减少中央空洞
        /// </summary>
        /// <param name="cave">洞穴数组</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="offsetX">世界坐标偏移X</param>
        /// <param name="offsetY">世界坐标偏移Y</param>
        private void InjectTerrainFeatures(bool[,] cave, int width, int height, int offsetX, int offsetY)
        {
            // 1. 构建预测安全区（房间中心 + 连接通道）
            HashSet<Vector2Int> safeZone = new HashSet<Vector2Int>();
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!_currentShape.IsValidCell(gx, gy)) continue;
                    
                    var room = _roomGrid[gx, gy];
                    int roomWorldX = gx * RoomWidth;
                    int roomWorldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
                    
                    // 转换为cave数组坐标
                    int roomLocalX = roomWorldX - offsetX;
                    int roomLocalY = roomWorldY - offsetY;
                    int centerX = roomLocalX + RoomWidth / 2;
                    int centerY = roomLocalY + RoomHeight / 2;
                    
                    // 房间中心安全区（6x6范围）
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        for (int dy = -3; dy <= 3; dy++)
                        {
                            safeZone.Add(new Vector2Int(centerX + dx, centerY + dy));
                        }
                    }
                    
                    // 连接方向通道安全区
                    int pathHalfWidth = 3;
                    if (room.HasConnection(Direction.North))
                    {
                        for (int dy = 0; dy < RoomHeight / 2; dy++)
                            for (int dx = -pathHalfWidth; dx <= pathHalfWidth; dx++)
                                safeZone.Add(new Vector2Int(centerX + dx, centerY + dy));
                    }
                    if (room.HasConnection(Direction.South))
                    {
                        for (int dy = 0; dy < RoomHeight / 2; dy++)
                            for (int dx = -pathHalfWidth; dx <= pathHalfWidth; dx++)
                                safeZone.Add(new Vector2Int(centerX + dx, centerY - dy));
                    }
                    if (room.HasConnection(Direction.East))
                    {
                        for (int dx = 0; dx < RoomWidth / 2; dx++)
                            for (int dy = -pathHalfWidth; dy <= pathHalfWidth; dy++)
                                safeZone.Add(new Vector2Int(centerX + dx, centerY + dy));
                    }
                    if (room.HasConnection(Direction.West))
                    {
                        for (int dx = 0; dx < RoomWidth / 2; dx++)
                            for (int dy = -pathHalfWidth; dy <= pathHalfWidth; dy++)
                                safeZone.Add(new Vector2Int(centerX - dx, centerY + dy));
                    }
                }
            }
            
            // 2. 在非安全区注入特征（步长4，避免过密）
            for (int y = 4; y < height - 4; y += 4)
            {
                for (int x = 4; x < width - 4; x += 4)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    // 如果在安全区内，跳过
                    if (safeZone.Contains(pos)) continue;
                    
                    // 40%概率注入特征
                    if (_rng.NextDouble() < 0.4)
                    {
                        StampPattern(cave, x, y, width, height);
                    }
                }
            }
        }
        
        /// <summary>
        /// 将地形模板印章到洞穴数组
        /// </summary>
        private void StampPattern(bool[,] cave, int cx, int cy, int width, int height)
        {
            // 随机选择一个模板
            int patternIndex = _rng.Next(FeaturePatterns.Length);
            int[,] pattern = FeaturePatterns[patternIndex];
            
            int size = 3; // 模板大小3x3
            
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    if (pattern[py, px] == 1)
                    {
                        int tx = cx + px - 1; // 居中放置
                        int ty = cy + py - 1;
                        
                        // 边界检查
                        if (tx >= 1 && tx < width - 1 && ty >= 1 && ty < height - 1)
                        {
                            cave[tx, ty] = true;
                        }
                    }
                }
            }
        }
        */
        #endregion
        
        /// <summary>
        /// 清除单个房间连接处的填充（保留供特殊区域使用）
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
