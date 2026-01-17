// V3版本已废弃，抑制未使用变量警告
#pragma warning disable CS0219

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 多网格关卡管理器
    /// 在场景中生成多个独立的4x4网格关卡区域
    /// </summary>
    public class MultiGridLevelManager : MonoBehaviour
    {
        [TitleGroup("生成器引用")]
        [LabelText("灰盒关卡生成器")]
        [Required("必须指定灰盒关卡生成器组件")]
        public GrayboxLevelGenerator LevelGenerator;
        
        [TitleGroup("多网格布局")]
        [LabelText("网格总数量")]
        [Range(1, 8)]
        public int GridCount = 4;
        
        [TitleGroup("多网格布局")]
        [LabelText("布局区域宽度")]
        [SuffixLabel("瓦片", true)]
        [Range(100, 500)]
        public int LayoutAreaWidth = 200;
        
        [TitleGroup("多网格布局")]
        [LabelText("布局区域高度")]
        [SuffixLabel("瓦片", true)]
        [Range(100, 500)]
        public int LayoutAreaHeight = 200;
        
        [TitleGroup("多网格布局")]
        [LabelText("网格最小间距")]
        [SuffixLabel("瓦片", true)]
        [Range(8, 64)]
        public int MinGridSpacing = 16;
        
        [TitleGroup("多网格布局")]
        [LabelText("位置随机偏移")]
        [SuffixLabel("瓦片", true)]
        [Range(0, 32)]
        public int PositionRandomOffset = 16;
        
        [TitleGroup("随机性控制")]
        [LabelText("基础随机种子")]
        [InfoBox("设为0时使用完全随机种子")]
        public int BaseSeed = 0;
        
        [TitleGroup("随机性控制")]
        [LabelText("每网格独立种子")]
        public bool UseUniqueSeedPerGrid = true;
        
        [TitleGroup("随机性控制")]
        [LabelText("位置尝试次数")]
        [Range(50, 500)]
        public int MaxPlacementAttempts = 100;
        
        [TitleGroup("特殊区域设置")]
        [LabelText("中位数网格概率")]
        [ProgressBar(0, 1, ColorGetter = "GetSpecialChanceColor")]
        public float MedianGridSpecialChance = 0.8f;
        
        [TitleGroup("特殊区域设置")]
        [LabelText("其他网格概率")]
        [ProgressBar(0, 1, ColorGetter = "GetSpecialChanceColor")]
        public float OtherGridSpecialChance = 0.15f;
        
        [TitleGroup("调试显示")]
        [LabelText("显示网格边界")]
        public bool ShowGridBounds = true;
        
        [TitleGroup("调试显示")]
        [LabelText("网格边界颜色")]
        [ShowIf("ShowGridBounds")]
        public Color GridBoundsColor = new Color(0f, 1f, 0f, 0.5f);
        
        [TitleGroup("出入口标记")]
        [LabelText("显示出入口标记")]
        public bool ShowEntranceExitMarkers = true;
        
        [TitleGroup("出入口标记")]
        [LabelText("入口标记颜色")]
        [ShowIf("ShowEntranceExitMarkers")]
        public Color EntranceMarkerColor = new Color(0f, 1f, 0f, 1f);
        
        [TitleGroup("出入口标记")]
        [LabelText("出口标记颜色")]
        [ShowIf("ShowEntranceExitMarkers")]
        public Color ExitMarkerColor = new Color(0f, 0f, 0f, 1f);
        
        [TitleGroup("出入口标记")]
        [LabelText("标记大小")]
        [ShowIf("ShowEntranceExitMarkers")]
        [Range(1f, 10f)]
        public float MarkerSize = 4f;
        
        // ==================== 生成控制 ====================
        
        [TitleGroup("生成控制", Order = 100)]
        [Button("生成多网格关卡", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void GenerateButton()
        {
            GenerateMultiGridLevel();
        }
        
        [TitleGroup("生成控制")]
        [Button("清除所有网格", ButtonSizes.Medium), GUIColor(0.8f, 0.4f, 0.4f)]
        public void ClearButton()
        {
            ClearAllGrids();
        }
        
        // ==================== 预览信息 ====================
        
        [TitleGroup("预览信息", Order = 101)]
        [ShowInInspector, LabelText("单网格宽度"), ReadOnly, SuffixLabel("瓦片", true)]
        private int PreviewSingleGridWidth => LevelGenerator != null ? LevelShape.GridWidth * LevelGenerator.RoomWidth : 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("单网格高度"), ReadOnly, SuffixLabel("瓦片", true)]
        private int PreviewSingleGridHeight => LevelGenerator != null ? LevelShape.GridHeight * LevelGenerator.RoomHeight : 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("已生成网格数"), ReadOnly]
        private int PreviewGeneratedGridCount => _gridPositions?.Count ?? 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("入口数量"), ReadOnly]
        private int PreviewEntranceCount => _entrancePositions?.Count ?? 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("出口数量"), ReadOnly]
        private int PreviewExitCount => _exitPositions?.Count ?? 0;
        
        [TitleGroup("预览信息")]
        [ShowInInspector, LabelText("网格位置列表"), ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        private List<Vector2Int> PreviewGridPositions => _gridPositions ?? new List<Vector2Int>();
        
        // ==================== 辅助方法 ====================
        
        /// <summary>
        /// 获取特殊区域概率条颜色
        /// </summary>
        private Color GetSpecialChanceColor(float value)
        {
            return Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 0.2f), value);
        }
        
        // 内部状态
        private System.Random _rng;
        private List<Rect> _placedGridBounds = new List<Rect>();
        private List<Vector2Int> _gridPositions = new List<Vector2Int>();
        
        // 存储每个网格的入口和出口位置
        private List<Vector3> _entrancePositions = new List<Vector3>();
        private List<Vector3> _exitPositions = new List<Vector3>();
        private List<Direction> _entranceDirections = new List<Direction>();
        private List<Direction> _exitDirections = new List<Direction>();
        
        // 玩家出生点和通关点
        private Vector3 _playerSpawnPoint;
        private Vector3 _levelExitPoint;
        private bool _hasSpawnPoint = false;
        private bool _hasExitPoint = false;
        
        // 计算属性
        private int SingleGridWidth => LevelShape.GridWidth * LevelGenerator.RoomWidth;
        private int SingleGridHeight => LevelShape.GridHeight * LevelGenerator.RoomHeight;
        
        /// <summary>
        /// 生成多网格关卡
        /// </summary>
        public void GenerateMultiGridLevel()
        {
            if (!ValidateSetup()) return;
            
            // 初始化随机数生成器
            _rng = BaseSeed == 0 ? new System.Random() : new System.Random(BaseSeed);
            _placedGridBounds.Clear();
            _gridPositions.Clear();
            _entrancePositions.Clear();
            _exitPositions.Clear();
            _entranceDirections.Clear();
            _exitDirections.Clear();
            
            // 清除所有Tilemap层
            LevelGenerator.TilemapLayers.ClearAll();
            
            Debug.Log($"开始生成多网格关卡: {GridCount}个网格, 布局区域: {LayoutAreaWidth}x{LayoutAreaHeight}");
            
            // 生成所有网格的随机位置
            if (!GenerateRandomPositions())
            {
                Debug.LogError("无法在指定区域内放置所有网格，请增加布局区域或减少网格数量");
                return;
            }
            
            // 计算中位数网格索引
            int medianIndex = _gridPositions.Count / 2;
            
            // 遍历生成每个网格
            for (int i = 0; i < _gridPositions.Count; i++)
            {
                Vector2Int pos = _gridPositions[i];
                int gridSeed = GetGridSeed(i);
                
                // 判断是否生成特殊区域
                bool isMedianGrid = (i == medianIndex);
                float specialChance = isMedianGrid ? MedianGridSpecialChance : OtherGridSpecialChance;
                bool generateSpecialArea = _rng.NextDouble() < specialChance;
                
                // 生成单个网格（传递网格索引和总数，用于决定出入口）
                GenerateSingleGridAtOffset(pos.x, pos.y, gridSeed, generateSpecialArea, i, _gridPositions.Count);
                
                string specialInfo = generateSpecialArea ? ", 包含特殊区域" : "";
                Debug.Log($"  网格[{i}] 生成完成 (位置: {pos.x},{pos.y}, 种子: {gridSeed}{specialInfo})");
            }
            
            // 计算出生点和通关点
            CalculateSpawnAndExitPoints();
            
            Debug.Log($"多网格关卡生成完成! 共{_gridPositions.Count}个独立网格");
        }
        
        /// <summary>
        /// 生成有序布局位置（随机化蛇形布局）
        /// </summary>
        private bool GenerateRandomPositions()
        {
            int gridWidth = SingleGridWidth;
            int gridHeight = SingleGridHeight;
            int spacing = MinGridSpacing;
            
            // 走廊通道宽度（房间之间的间距，用于放置走廊）
            int corridorSpace = spacing + 15;
            
            // 计算每行可以放置的房间数量（留有余地）
            int maxRoomsPerRow = Mathf.Max(1, (LayoutAreaWidth - corridorSpace) / (gridWidth + corridorSpace));
            
            // 随机决定每行实际放置的房间数量（1到最大值之间）
            List<int> rowRoomCounts = new List<int>();
            int remainingRooms = GridCount;
            
            while (remainingRooms > 0)
            {
                // 随机决定这一行放几个房间（至少1个，最多剩余数量或最大值）
                int maxInThisRow = Mathf.Min(maxRoomsPerRow, remainingRooms);
                int minInThisRow = Mathf.Max(1, maxInThisRow - 1);
                int roomsInRow = _rng.Next(minInThisRow, maxInThisRow + 1);
                
                rowRoomCounts.Add(roomsInRow);
                remainingRooms -= roomsInRow;
            }
            
            int rowsNeeded = rowRoomCounts.Count;
            
            // 检查是否有足够的垂直空间
            int totalHeightNeeded = rowsNeeded * gridHeight + (rowsNeeded - 1) * corridorSpace;
            if (totalHeightNeeded > LayoutAreaHeight)
            {
                Debug.LogError($"布局区域高度不足! 需要 {totalHeightNeeded}, 可用 {LayoutAreaHeight}");
                return false;
            }
            
            // 计算起始Y位置（从顶部开始，带随机偏移）
            int verticalPadding = LayoutAreaHeight - totalHeightNeeded;
            int startY = LayoutAreaHeight - gridHeight - _rng.Next(0, Mathf.Max(1, verticalPadding / 2));
            
            int gridIndex = 0;
            for (int row = 0; row < rowsNeeded && gridIndex < GridCount; row++)
            {
                int roomsInThisRow = rowRoomCounts[row];
                
                // 计算当前行的Y位置（带随机偏移）
                int baseY = startY - row * (gridHeight + corridorSpace);
                int yOffset = _rng.Next(-PositionRandomOffset / 2, PositionRandomOffset / 2 + 1);
                int y = Mathf.Clamp(baseY + yOffset, 0, LayoutAreaHeight - gridHeight);
                
                // 蛇形布局：偶数行从左到右，奇数行从右到左
                bool leftToRight = (row % 2 == 0);
                
                // 计算这一行的水平分布（随机化间距）
                int totalRowWidth = roomsInThisRow * gridWidth + (roomsInThisRow - 1) * corridorSpace;
                int horizontalPadding = LayoutAreaWidth - totalRowWidth;
                int startX = _rng.Next(0, Mathf.Max(1, horizontalPadding));
                
                for (int col = 0; col < roomsInThisRow && gridIndex < GridCount; col++)
                {
                    int actualCol = leftToRight ? col : (roomsInThisRow - 1 - col);
                    
                    // 计算X位置（带随机间距）
                    int randomSpacing = corridorSpace + _rng.Next(-5, 6);
                    int x = startX + actualCol * (gridWidth + randomSpacing);
                    
                    // 添加额外随机偏移
                    if (PositionRandomOffset > 0)
                    {
                        int maxOffset = Mathf.Min(PositionRandomOffset, corridorSpace / 3);
                        x += _rng.Next(-maxOffset, maxOffset + 1);
                    }
                    
                    // 确保在有效范围内且不重叠
                    x = Mathf.Clamp(x, 0, LayoutAreaWidth - gridWidth);
                    int finalY = Mathf.Clamp(y, 0, LayoutAreaHeight - gridHeight);
                    
                    // 检查是否与已放置的网格重叠
                    Rect newBounds = new Rect(x, finalY, gridWidth, gridHeight);
                    bool overlaps = false;
                    foreach (var existingBounds in _placedGridBounds)
                    {
                        Rect expandedExisting = new Rect(
                            existingBounds.x - spacing,
                            existingBounds.y - spacing,
                            existingBounds.width + spacing * 2,
                            existingBounds.height + spacing * 2
                        );
                        if (newBounds.Overlaps(expandedExisting))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    
                    // 如果重叠，尝试调整位置
                    if (overlaps)
                    {
                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            x = startX + actualCol * (gridWidth + corridorSpace) + _rng.Next(-20, 21);
                            x = Mathf.Clamp(x, 0, LayoutAreaWidth - gridWidth);
                            newBounds = new Rect(x, finalY, gridWidth, gridHeight);
                            
                            overlaps = false;
                            foreach (var existingBounds in _placedGridBounds)
                            {
                                Rect expandedExisting = new Rect(
                                    existingBounds.x - spacing,
                                    existingBounds.y - spacing,
                                    existingBounds.width + spacing * 2,
                                    existingBounds.height + spacing * 2
                                );
                                if (newBounds.Overlaps(expandedExisting))
                                {
                                    overlaps = true;
                                    break;
                                }
                            }
                            
                            if (!overlaps) break;
                        }
                    }
                    
                    _gridPositions.Add(new Vector2Int(x, finalY));
                    _placedGridBounds.Add(new Rect(x, finalY, gridWidth, gridHeight));
                    
                    gridIndex++;
                }
            }
            
            Debug.Log($"随机化蛇形布局完成: {rowsNeeded}行, 房间分布: {string.Join(",", rowRoomCounts)}");
            return true;
        }
        
        /// <summary>
        /// 清除所有网格
        /// </summary>
        public void ClearAllGrids()
        {
            if (LevelGenerator != null && LevelGenerator.TilemapLayers != null)
            {
                LevelGenerator.TilemapLayers.ClearAll();
                _placedGridBounds.Clear();
                _gridPositions.Clear();
                _entrancePositions.Clear();
                _exitPositions.Clear();
                _entranceDirections.Clear();
                _exitDirections.Clear();
                Debug.Log("所有网格已清除");
            }
        }
        
        /// <summary>
        /// 获取已生成的网格位置列表（供外部访问）
        /// </summary>
        public List<Vector2Int> GetGridPositions()
        {
            return new List<Vector2Int>(_gridPositions);
        }
        
        /// <summary>
        /// 获取已生成的网格边界列表（供外部访问）
        /// </summary>
        public List<Rect> GetGridBounds()
        {
            return new List<Rect>(_placedGridBounds);
        }
        
        /// <summary>
        /// 在指定偏移位置生成单个网格
        /// </summary>
        private void GenerateSingleGridAtOffset(int offsetX, int offsetY, int seed, bool generateSpecialArea, int gridIndex, int totalGrids)
        {
            // 设置生成器的随机种子
            LevelGenerator.RandomSeed = seed;
            
            // 获取Tilemap引用
            var tilemapLayers = LevelGenerator.TilemapLayers;
            var tileSet = LevelGenerator.TileSet;
            
            // 调用生成器的内部方法（通过反射或直接调用公开方法）
            // 由于现有Generator不支持偏移，我们需要直接操作Tilemap
            GenerateGridContent(offsetX, offsetY, seed, generateSpecialArea, gridIndex, totalGrids);
        }
        
        /// <summary>
        /// 生成网格内容（带偏移）
        /// </summary>
        private void GenerateGridContent(int offsetX, int offsetY, int seed, bool generateSpecialArea, int gridIndex, int totalGrids)
        {
            var rng = new System.Random(seed);
            var tilemapLayers = LevelGenerator.TilemapLayers;
            var tileSet = LevelGenerator.TileSet;
            
            // 随机选择主题
            SelectRandomTheme(rng);
            
            int roomWidth = LevelGenerator.RoomWidth;
            int roomHeight = LevelGenerator.RoomHeight;
            int wallThickness = LevelGenerator.WallThickness;
            
            // 初始化房间网格数据
            LevelShape currentShape = new LevelShape();
            RoomNode[,] roomGrid = new RoomNode[LevelShape.GridWidth, LevelShape.GridHeight];
            
            for (int y = 0; y < LevelShape.GridHeight; y++)
            {
                for (int x = 0; x < LevelShape.GridWidth; x++)
                {
                    roomGrid[x, y] = new RoomNode(x, y);
                    roomGrid[x, y].Type = RoomType.Side;
                }
            }
            
            // 生成关键路径（醉汉游走）
            GenerateCriticalPath(rng, currentShape, roomGrid, roomWidth, roomHeight);
            
            // 绘制洞穴填充（包含边缘填充和内部填充）
            DrawCaveFill(offsetX, offsetY, rng, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制外围墙壁（最后绘制，确保不被覆盖）
            DrawOuterWalls(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制房间连接（在墙壁上打洞）
            DrawRoomConnections(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制入口和出口（根据网格索引决定）
            DrawEntranceAndExit(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness, gridIndex, totalGrids);
            
            // 绘制平台
            DrawPlatforms(offsetX, offsetY, rng, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制特殊区域（根据概率决定）
            if (generateSpecialArea)
            {
                DrawSpecialAreas(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight);
            }
        }
        
        /// <summary>
        /// 生成关键路径
        /// </summary>
        private void GenerateCriticalPath(System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight)
        {
            // 从顶排随机选择入口
            int startX = rng.Next(LevelShape.GridWidth);
            Vector2Int current = new Vector2Int(startX, 0);
            
            List<Vector2Int> path = new List<Vector2Int>();
            path.Add(current);
            shape.SetCell(current.x, current.y, true);
            
            // 每层先水平游走，再向下
            for (int row = 0; row < LevelShape.GridHeight; row++)
            {
                int horizontalDirection = rng.Next(2) == 0 ? -1 : 1;
                int horizontalSteps = rng.Next(1, 4);
                
                for (int step = 0; step < horizontalSteps; step++)
                {
                    int nextX = current.x + horizontalDirection;
                    
                    if (nextX < 0 || nextX >= LevelShape.GridWidth)
                    {
                        horizontalDirection = -horizontalDirection;
                        nextX = current.x + horizontalDirection;
                        if (nextX < 0 || nextX >= LevelShape.GridWidth) break;
                    }
                    
                    Vector2Int next = new Vector2Int(nextX, current.y);
                    if (!path.Contains(next)) path.Add(next);
                    shape.SetCell(next.x, next.y, true);
                    
                    // 添加连接
                    Direction dir = GetDirection(current, next);
                    roomGrid[current.x, current.y].AddConnection(dir);
                    roomGrid[next.x, next.y].AddConnection(dir.Opposite());
                    
                    current = next;
                }
                
                if (row < LevelShape.GridHeight - 1)
                {
                    Vector2Int next = new Vector2Int(current.x, current.y + 1);
                    if (!path.Contains(next)) path.Add(next);
                    shape.SetCell(next.x, next.y, true);
                    
                    Direction dir = GetDirection(current, next);
                    roomGrid[current.x, current.y].AddConnection(dir);
                    roomGrid[next.x, next.y].AddConnection(dir.Opposite());
                    
                    current = next;
                }
            }
            
            // 设置入口和出口
            Vector2Int entrance = path[0];
            Vector2Int exit = path[path.Count - 1];
            
            roomGrid[entrance.x, entrance.y].Type = RoomType.Start;
            roomGrid[entrance.x, entrance.y].IsCriticalPath = true;
            
            roomGrid[exit.x, exit.y].Type = RoomType.Exit;
            roomGrid[exit.x, exit.y].IsCriticalPath = true;
            
            foreach (var cell in path)
            {
                roomGrid[cell.x, cell.y].IsCriticalPath = true;
            }
            
            // 设置Boss房间
            if (path.Count >= 3)
            {
                var bossCell = path[path.Count - 2];
                if (roomGrid[bossCell.x, bossCell.y].Type != RoomType.Start &&
                    roomGrid[bossCell.x, bossCell.y].Type != RoomType.Exit)
                {
                    roomGrid[bossCell.x, bossCell.y].Type = RoomType.Boss;
                }
            }
        }
        
        /// <summary>
        /// 绘制整个形状的外围墙壁（仅在形状边缘绘制）
        /// </summary>
        private void DrawOuterWalls(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.GroundLayer;
            // 优先使用规则瓦片，否则使用黑色瓦片
            var ruleTile = GetCurrentGroundRuleTile();
            TileBase tile = ruleTile != null ? (TileBase)ruleTile : LevelGenerator.TileSet.BlackTile;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 仅在形状外围边缘绘制墙壁
                    // 北边（上方）- 当上方没有有效格子时
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                    {
                        FillRect(tilemap, tile, worldX, worldY + roomHeight - wallThickness, roomWidth, wallThickness);
                    }
                    // 南边（下方）- 当下方没有有效格子时
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                    {
                        FillRect(tilemap, tile, worldX, worldY, roomWidth, wallThickness);
                    }
                    // 西边（左边）- 当左边没有有效格子时
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                    {
                        FillRect(tilemap, tile, worldX, worldY, wallThickness, roomHeight);
                    }
                    // 东边（右边）- 当右边没有有效格子时
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                    {
                        FillRect(tilemap, tile, worldX + roomWidth - wallThickness, worldY, wallThickness, roomHeight);
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制洞穴填充
        /// </summary>
        private void DrawCaveFill(int offsetX, int offsetY, System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var grayTile = LevelGenerator.TileSet.GrayTile;
            var blackTile = LevelGenerator.TileSet.BlackTile;
            float fillDensity = LevelGenerator.FillDensity;
            int smoothIterations = LevelGenerator.SmoothIterations;
            
            // 在形状外围边缘绘制填充（先绘制灰色填充，再绘制最外层黑色边框）
            int baseBoundaryWidth = 12; // 边缘填充宽度
            int blackBorderWidth = 2;   // 黑色边框宽度（最外层）
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 先绘制灰色填充区域
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                        FillRect(tilemap, grayTile, worldX, worldY + roomHeight - baseBoundaryWidth, roomWidth, baseBoundaryWidth);
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                        FillRect(tilemap, grayTile, worldX, worldY, roomWidth, baseBoundaryWidth);
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                        FillRect(tilemap, grayTile, worldX, worldY, baseBoundaryWidth, roomHeight);
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                        FillRect(tilemap, grayTile, worldX + roomWidth - baseBoundaryWidth, worldY, baseBoundaryWidth, roomHeight);
                    
                    // 再绘制最外层黑色边框
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                        FillRect(tilemap, blackTile, worldX, worldY + roomHeight - blackBorderWidth, roomWidth, blackBorderWidth);
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                        FillRect(tilemap, blackTile, worldX, worldY, roomWidth, blackBorderWidth);
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                        FillRect(tilemap, blackTile, worldX, worldY, blackBorderWidth, roomHeight);
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                        FillRect(tilemap, blackTile, worldX + roomWidth - blackBorderWidth, worldY, blackBorderWidth, roomHeight);
                }
            }
            
            // 生成连贯洞穴填充
            GenerateConnectedCaveFill(offsetX, offsetY, rng, shape, roomWidth, roomHeight, fillDensity, smoothIterations);
            
            // 雕刻边界边缘
            CarveBoundaryEdges(offsetX, offsetY, rng, shape, roomWidth, roomHeight);
            
            // 清除房间连接通道
            ClearAllConnectionPassages(offsetX, offsetY, shape, roomGrid, roomWidth, roomHeight);
            
            // 雕刻曲折通道
            CarveWindingPath(offsetX, offsetY, rng, shape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 将表层地板替换为白色瓦片
            ApplySurfaceTiles(offsetX, offsetY, shape, roomWidth, roomHeight);
            
            // 应用规则瓦片替换（可选）
            ApplyRuleTileReplacement(offsetX, offsetY, shape, roomWidth, roomHeight);
        }
        
        /// <summary>
        /// 将表层地板（上方是空气，下方是填充物）替换为白色瓦片
        /// 排除墙壁层已有瓦片的位置
        /// </summary>
        private void ApplySurfaceTiles(int offsetX, int offsetY, LevelShape shape, int roomWidth, int roomHeight)
        {
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var whiteTile = LevelGenerator.TileSet.WhiteTile;
            
            if (whiteTile == null) return;
            
            // 遍历所有有效房间区域
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 扫描房间内的每个瓦片
                    for (int y = 0; y < roomHeight; y++)
                    {
                        for (int x = 0; x < roomWidth; x++)
                        {
                            int tileX = worldX + x;
                            int tileY = worldY + y;
                            Vector3Int pos = new Vector3Int(tileX, tileY, 0);
                            
                            // 检查当前位置是否有地面瓦片
                            var currentTile = groundTilemap.GetTile(pos);
                            if (currentTile == null) continue;
                            
                            // 检查上方是否为空（表层条件）
                            var aboveTile = groundTilemap.GetTile(new Vector3Int(tileX, tileY + 1, 0));
                            if (aboveTile == null)
                            {
                                // 这是表层地板，替换为白色
                                groundTilemap.SetTile(pos, whiteTile);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 将所有地面瓦片替换为规则瓦片
        /// </summary>
        private void ApplyRuleTileReplacement(int offsetX, int offsetY, LevelShape shape, int roomWidth, int roomHeight)
        {
            var ruleTile = GetCurrentGroundRuleTile();
            if (ruleTile == null) return;
            
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            
            // 第一步：收集所有有瓦片的位置
            HashSet<Vector3Int> tilePositions = new HashSet<Vector3Int>();
            
            // 遍历所有有效房间区域
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 遍历房间内每个瓦片位置
                    for (int y = 0; y < roomHeight; y++)
                    {
                        for (int x = 0; x < roomWidth; x++)
                        {
                            Vector3Int pos = new Vector3Int(worldX + x, worldY + y, 0);
                            
                            // 检查地面层是否有瓦片
                            if (groundTilemap.GetTile(pos) != null)
                            {
                                tilePositions.Add(pos);
                                groundTilemap.SetTile(pos, null);
                            }
                        }
                    }
                }
            }
            
            // 第二步：设置规则瓦片
            foreach (var pos in tilePositions)
            {
                groundTilemap.SetTile(pos, ruleTile);
            }
            
            // 刷新瓦片地图以应用规则
            groundTilemap.RefreshAllTiles();
        }
        
        /// <summary>
        /// 随机选择主题
        /// </summary>
        private void SelectRandomTheme(System.Random rng)
        {
            if (!LevelGenerator.UseTheme || LevelGenerator.ThemeConfig == null)
            {
                LevelGenerator.CurrentColorTheme = null;
                return;
            }
            
            LevelGenerator.CurrentColorTheme = LevelGenerator.ThemeConfig.GetRandomTheme(rng);
            
            if (LevelGenerator.CurrentColorTheme != null)
            {
                Debug.Log($"选择主题颜色: {LevelGenerator.CurrentColorTheme.ColorName}");
            }
        }
        
        /// <summary>
        /// 获取当前使用的地面规则瓦片
        /// </summary>
        private RuleTile GetCurrentGroundRuleTile()
        {
            // 优先使用主题中的规则瓦片
            if (LevelGenerator.UseTheme && LevelGenerator.CurrentColorTheme != null && LevelGenerator.CurrentColorTheme.GroundRuleTile != null)
            {
                return LevelGenerator.CurrentColorTheme.GroundRuleTile;
            }
            // 其次使用直接配置的规则瓦片
            if (LevelGenerator.UseRuleTile && LevelGenerator.GroundRuleTile != null)
            {
                return LevelGenerator.GroundRuleTile;
            }
            return null;
        }
        
        /// <summary>
        /// 获取当前使用的平台规则瓦片
        /// </summary>
        private RuleTile GetCurrentPlatformRuleTile()
        {
            // 优先使用主题中的规则瓦片
            if (LevelGenerator.UseTheme && LevelGenerator.CurrentColorTheme != null && LevelGenerator.CurrentColorTheme.PlatformRuleTile != null)
            {
                return LevelGenerator.CurrentColorTheme.PlatformRuleTile;
            }
            // 其次使用直接配置的规则瓦片
            if (LevelGenerator.UseRuleTile && LevelGenerator.PlatformRuleTile != null)
            {
                return LevelGenerator.PlatformRuleTile;
            }
            return null;
        }
        
        /// <summary>
        /// 生成连贯洞穴填充
        /// </summary>
        private void GenerateConnectedCaveFill(int offsetX, int offsetY, System.Random rng, LevelShape shape, int roomWidth, int roomHeight, float fillDensity, int smoothIterations)
        {
            var tilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var tile = LevelGenerator.TileSet.GrayTile;
            
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (shape.IsValidCell(gx, gy))
                    {
                        int worldX = gx * roomWidth;
                        int worldY = (LevelShape.GridHeight - 1 - gy) * roomHeight;
                        minX = Mathf.Min(minX, worldX);
                        maxX = Mathf.Max(maxX, worldX + roomWidth);
                        minY = Mathf.Min(minY, worldY);
                        maxY = Mathf.Max(maxY, worldY + roomHeight);
                    }
                }
            }
            
            int fillWidth = maxX - minX;
            int fillHeight = maxY - minY;
            if (fillWidth <= 0 || fillHeight <= 0) return;
            
            bool[,] cave = new bool[fillWidth, fillHeight];
            
            for (int y = 0; y < fillHeight; y++)
            {
                for (int x = 0; x < fillWidth; x++)
                {
                    int worldX = minX + x;
                    int worldY = minY + y;
                    int gx = worldX / roomWidth;
                    int gy = LevelShape.GridHeight - 1 - worldY / roomHeight;
                    
                    if (gx < 0 || gx >= LevelShape.GridWidth || gy < 0 || gy >= LevelShape.GridHeight || !shape.IsValidCell(gx, gy))
                        continue;
                    
                    int distToEdge = Mathf.Min(x, y, fillWidth - 1 - x, fillHeight - 1 - y);
                    float edgeFactor = (distToEdge < 6) ? 2.0f : 1.0f;
                    if (y < fillHeight / 4) edgeFactor *= 1.8f;
                    
                    cave[x, y] = rng.NextDouble() < fillDensity * edgeFactor;
                }
            }
            
            for (int i = 0; i < smoothIterations + 3; i++)
            {
                cave = SmoothCave(cave, fillWidth, fillHeight);
            }
            
            // 边缘区域宽度（只跳过最外层黑色边框区域）
            int edgeWidth = 2;
            
            for (int y = 0; y < fillHeight; y++)
            {
                for (int x = 0; x < fillWidth; x++)
                {
                    if (cave[x, y])
                    {
                        int worldX = minX + x;
                        int worldY = minY + y;
                        int gx = worldX / roomWidth;
                        int gy = LevelShape.GridHeight - 1 - worldY / roomHeight;
                        
                        if (gx >= 0 && gx < LevelShape.GridWidth && gy >= 0 && gy < LevelShape.GridHeight && shape.IsValidCell(gx, gy))
                        {
                            // 计算在当前房间格子内的局部坐标
                            int localX = worldX - gx * roomWidth;
                            int localY = worldY - (LevelShape.GridHeight - 1 - gy) * roomHeight;
                            
                            // 只跳过最外层黑色边框区域（2单位宽）
                            bool isOnBlackBorder = false;
                            if ((gy == 0 || !shape.IsValidCell(gx, gy - 1)) && localY >= roomHeight - edgeWidth)
                                isOnBlackBorder = true;
                            if ((gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1)) && localY < edgeWidth)
                                isOnBlackBorder = true;
                            if ((gx == 0 || !shape.IsValidCell(gx - 1, gy)) && localX < edgeWidth)
                                isOnBlackBorder = true;
                            if ((gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy)) && localX >= roomWidth - edgeWidth)
                                isOnBlackBorder = true;
                            
                            if (!isOnBlackBorder)
                            {
                                tilemap.SetTile(new Vector3Int(offsetX + worldX, offsetY + worldY, 0), tile);
                            }
                        }
                    }
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
                    newCave[x, y] = neighbors >= 4;
                }
            }
            
            return newCave;
        }
        
        /// <summary>
        /// 统计邻居数量
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
                    {
                        count++;
                    }
                    else if (cave[nx, ny])
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// 雕刻边界边缘（不影响最外层黑色边框）
        /// </summary>
        private void CarveBoundaryEdges(int offsetX, int offsetY, System.Random rng, LevelShape shape, int roomWidth, int roomHeight)
        {
            var tilemap = LevelGenerator.TilemapLayers.GroundLayer;
            int carveDepth = 6;
            int carveVariation = 4;
            int edgeWidth = 12; // 边缘填充宽度（与 DrawCaveFill 中一致）
            int safeMargin = 1; // 安全边距
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 计算各边是否为外围边缘
                    bool isNorthEdge = (gy == 0 || !shape.IsValidCell(gx, gy - 1));
                    bool isSouthEdge = (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1));
                    bool isWestEdge = (gx == 0 || !shape.IsValidCell(gx - 1, gy));
                    bool isEastEdge = (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy));
                    
                    // 北边雕刻（避开角落的黑色边框）
                    if (isNorthEdge)
                    {
                        int baseY = worldY + roomHeight - edgeWidth;
                        int startX = isWestEdge ? edgeWidth + safeMargin : 0;
                        int endX = isEastEdge ? roomWidth - edgeWidth - safeMargin : roomWidth;
                        for (int x = startX; x < endX; x++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                tilemap.SetTile(new Vector3Int(worldX + x, baseY + y, 0), null);
                            }
                        }
                    }
                    
                    // 南边雕刻（避开角落的黑色边框）
                    if (isSouthEdge)
                    {
                        int baseY = worldY + edgeWidth;
                        int startX = isWestEdge ? edgeWidth + safeMargin : 0;
                        int endX = isEastEdge ? roomWidth - edgeWidth - safeMargin : roomWidth;
                        for (int x = startX; x < endX; x++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                tilemap.SetTile(new Vector3Int(worldX + x, baseY - y - 1, 0), null);
                            }
                        }
                    }
                    
                    // 西边雕刻（避开角落的黑色边框）
                    if (isWestEdge)
                    {
                        int baseX = worldX + edgeWidth;
                        int startY = isSouthEdge ? edgeWidth + safeMargin : 0;
                        int endY = isNorthEdge ? roomHeight - edgeWidth - safeMargin : roomHeight;
                        for (int y = startY; y < endY; y++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(y * 0.3f) * 3);
                            for (int x = 0; x < depth; x++)
                            {
                                tilemap.SetTile(new Vector3Int(baseX - x - 1, worldY + y, 0), null);
                            }
                        }
                    }
                    
                    // 东边雕刻（避开角落的黑色边框）
                    if (isEastEdge)
                    {
                        int baseX = worldX + roomWidth - edgeWidth;
                        int startY = isSouthEdge ? edgeWidth + safeMargin : 0;
                        int endY = isNorthEdge ? roomHeight - edgeWidth - safeMargin : roomHeight;
                        for (int y = startY; y < endY; y++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(y * 0.3f) * 3);
                            for (int x = 0; x < depth; x++)
                            {
                                tilemap.SetTile(new Vector3Int(baseX + x, worldY + y, 0), null);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 清除所有连接通道
        /// </summary>
        private void ClearAllConnectionPassages(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight)
        {
            var tilemap = LevelGenerator.TilemapLayers.GroundLayer;
            int passageWidth = 8;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    if (room.HasConnection(Direction.North))
                        ClearRect(tilemap, centerX - passageWidth / 2, worldY + roomHeight - 4, passageWidth, 4);
                    if (room.HasConnection(Direction.South))
                        ClearRect(tilemap, centerX - passageWidth / 2, worldY, passageWidth, 4);
                    if (room.HasConnection(Direction.East))
                        ClearRect(tilemap, worldX + roomWidth - 4, centerY - passageWidth / 2, 4, passageWidth);
                    if (room.HasConnection(Direction.West))
                        ClearRect(tilemap, worldX, centerY - passageWidth / 2, 4, passageWidth);
                }
            }
        }
        
        /// <summary>
        /// 雕刻曲折通道
        /// </summary>
        private void CarveWindingPath(int offsetX, int offsetY, System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.GroundLayer;
            int pathWidth = 5;
            int margin = wallThickness + 2;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    // 清除中心区域
                    ClearRect(tilemap, centerX - 4, centerY - 4, 8, 8);
                    
                    // 随机游走创建曲折通道
                    int currentX = centerX;
                    int currentY = centerY;
                    
                    for (int step = 0; step < 15; step++)
                    {
                        int dx = rng.Next(-3, 4);
                        int dy = rng.Next(-3, 4);
                        
                        int nextX = Mathf.Clamp(currentX + dx, worldX + margin, worldX + roomWidth - margin);
                        int nextY = Mathf.Clamp(currentY + dy, worldY + margin, worldY + roomHeight - margin);
                        
                        ClearLine(tilemap, currentX, currentY, nextX, nextY, pathWidth);
                        
                        currentX = nextX;
                        currentY = nextY;
                    }
                    
                    // 确保连接通道畅通
                    if (room.HasConnection(Direction.North))
                        ClearRect(tilemap, centerX - pathWidth / 2, centerY, pathWidth, roomHeight / 2);
                    if (room.HasConnection(Direction.South))
                        ClearRect(tilemap, centerX - pathWidth / 2, worldY + margin, pathWidth, roomHeight / 2);
                    if (room.HasConnection(Direction.East))
                        ClearRect(tilemap, centerX, centerY - pathWidth / 2, roomWidth / 2, pathWidth);
                    if (room.HasConnection(Direction.West))
                        ClearRect(tilemap, worldX + margin, centerY - pathWidth / 2, roomWidth / 2, pathWidth);
                }
            }
        }
        
        /// <summary>
        /// 绘制房间连接
        /// </summary>
        private void DrawRoomConnections(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            int passageWidth = 8;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    int centerX = worldX + roomWidth / 2;
                    int centerY = worldY + roomHeight / 2;
                    
                    if (room.HasConnection(Direction.North))
                        ClearRect(groundTilemap, centerX - passageWidth / 2, worldY + roomHeight - wallThickness, passageWidth, wallThickness);
                    if (room.HasConnection(Direction.South))
                        ClearRect(groundTilemap, centerX - passageWidth / 2, worldY, passageWidth, wallThickness);
                    if (room.HasConnection(Direction.East))
                        ClearRect(groundTilemap, worldX + roomWidth - wallThickness, centerY - passageWidth / 2, wallThickness, passageWidth);
                    if (room.HasConnection(Direction.West))
                        ClearRect(groundTilemap, worldX, centerY - passageWidth / 2, wallThickness, passageWidth);
                }
            }
        }
        
        /// <summary>
        /// 绘制入口和出口（根据网格索引决定）
        /// 0号网格只有出口，最后网格只有入口，中间网格绘制出入口标记
        /// </summary>
        private void DrawEntranceAndExit(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness, int gridIndex, int totalGrids)
        {
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var greenTile = LevelGenerator.TileSet.GreenTile;
            var redTile = LevelGenerator.TileSet.RedTile;
            
            int entranceWidth = LevelGenerator.EntranceWidth;
            int entranceHeight = LevelGenerator.EntranceHeight;
            
            // 判断当前网格的角色
            bool isFirstGrid = (gridIndex == 0);
            bool isLastGrid = (gridIndex == totalGrids - 1);
            bool isMiddleGrid = !isFirstGrid && !isLastGrid;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 0号网格的Start房间：只绘制出口（玩家出发点）
                    if (room.Type == RoomType.Start && isFirstGrid)
                    {
                        Direction exitDir = GetBestExitDirection(gx, gy, shape);
                        
                        DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness,
                            entranceWidth, entranceHeight, exitDir, false,
                            groundTilemap, groundTilemap, groundTilemap, redTile);
                        
                        Vector3 exitPos = GetPortalPosition(worldX, worldY, roomWidth, roomHeight, wallThickness, entranceWidth, exitDir);
                        _exitPositions.Add(exitPos);
                        _exitDirections.Add(exitDir);
                        
                    }
                    
                    // 最后网格的Exit房间：只绘制入口（终点）
                    if (room.Type == RoomType.Exit && isLastGrid)
                    {
                        Direction entranceDir = GetBestEntranceDirection(gx, gy, shape);
                        
                        DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness, 
                            entranceWidth, entranceHeight, entranceDir, true,
                            groundTilemap, groundTilemap, groundTilemap, greenTile);
                        
                        Vector3 entrancePos = GetPortalPosition(worldX, worldY, roomWidth, roomHeight, wallThickness, entranceWidth, entranceDir);
                        _entrancePositions.Add(entrancePos);
                        _entranceDirections.Add(entranceDir);
                        
                    }
                    
                    // 中间网格：绘制出入口标记（Start房间绘制出口，Exit房间绘制入口）
                    if (isMiddleGrid)
                    {
                        if (room.Type == RoomType.Start)
                        {
                            Direction exitDir = GetBestExitDirection(gx, gy, shape);
                            
                            DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness,
                                entranceWidth, entranceHeight, exitDir, false,
                                groundTilemap, groundTilemap, groundTilemap, redTile);
                            
                            Vector3 exitPos = GetPortalPosition(worldX, worldY, roomWidth, roomHeight, wallThickness, entranceWidth, exitDir);
                            _exitPositions.Add(exitPos);
                            _exitDirections.Add(exitDir);
                        }
                        
                        if (room.Type == RoomType.Exit)
                        {
                            Direction entranceDir = GetBestEntranceDirection(gx, gy, shape);
                            
                            DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness, 
                                entranceWidth, entranceHeight, entranceDir, true,
                                groundTilemap, groundTilemap, groundTilemap, greenTile);
                            
                            Vector3 entrancePos = GetPortalPosition(worldX, worldY, roomWidth, roomHeight, wallThickness, entranceWidth, entranceDir);
                            _entrancePositions.Add(entrancePos);
                            _entranceDirections.Add(entranceDir);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 根据房间位置获取最佳入口方向
        /// 简化逻辑：优先选择可用的外围方向
        /// </summary>
        private Direction GetBestEntranceDirection(int gx, int gy, LevelShape shape)
        {
            // 收集所有可用的外围方向（没有相邻房间的方向）
            List<Direction> availableDirections = new List<Direction>();
            
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            // 入口优先级：左 > 上 > 下 > 右
            if (canWest) availableDirections.Add(Direction.West);
            if (canNorth) availableDirections.Add(Direction.North);
            if (canSouth) availableDirections.Add(Direction.South);
            if (canEast) availableDirections.Add(Direction.East);
            
            if (availableDirections.Count > 0)
            {
                return availableDirections[0]; // 返回最优先的方向
            }
            
            // 默认顶部
            return Direction.North;
        }
        
        /// <summary>
        /// 根据房间位置获取最佳出口方向
        /// 简化逻辑：优先选择可用的外围方向
        /// </summary>
        private Direction GetBestExitDirection(int gx, int gy, LevelShape shape)
        {
            // 收集所有可用的外围方向（没有相邻房间的方向）
            List<Direction> availableDirections = new List<Direction>();
            
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            // 出口优先级：右 > 下 > 上 > 左（与入口相反）
            if (canEast) availableDirections.Add(Direction.East);
            if (canSouth) availableDirections.Add(Direction.South);
            if (canNorth) availableDirections.Add(Direction.North);
            if (canWest) availableDirections.Add(Direction.West);
            
            if (availableDirections.Count > 0)
            {
                return availableDirections[0]; // 返回最优先的方向
            }
            
            // 默认底部
            return Direction.South;
        }
        
        /// <summary>
        /// 获取外围方向（入口或出口）
        /// </summary>
        private Direction GetOuterDirection(int gx, int gy, LevelShape shape, bool isEntrance)
        {
            List<Direction> availableDirections = new List<Direction>();
            
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            if (isEntrance)
            {
                // 入口优先：左 > 上 > 右
                if (canWest) availableDirections.Add(Direction.West);
                if (canNorth) availableDirections.Add(Direction.North);
                if (canEast) availableDirections.Add(Direction.East);
            }
            else
            {
                // 出口优先：右 > 下 > 左
                if (canEast) availableDirections.Add(Direction.East);
                if (canSouth) availableDirections.Add(Direction.South);
                if (canWest) availableDirections.Add(Direction.West);
            }
            
            if (availableDirections.Count > 0)
            {
                return availableDirections[_rng.Next(availableDirections.Count)];
            }
            
            return isEntrance ? Direction.North : Direction.South;
        }
        
        /// <summary>
        /// 获取下一个网格的预期位置（用于方向预测）
        /// </summary>
        private Vector2Int GetNextGridPosition()
        {
            if (_gridPositions.Count == 0)
                return new Vector2Int(0, 0);
            
            Vector2Int lastPos = _gridPositions[_gridPositions.Count - 1];
            int spacing = MinGridSpacing + 15;
            
            // 简单预测：右侧或下方
            return new Vector2Int(lastPos.x + SingleGridWidth + spacing, lastPos.y);
        }
        
        /// <summary>
        /// 绘制入口/出口门户
        /// </summary>
        private void DrawPortal(int worldX, int worldY, int roomWidth, int roomHeight, int wallThickness,
            int portalWidth, int portalHeight, Direction direction, bool isEntrance,
            Tilemap groundTilemap, Tilemap unusedLayer, Tilemap portalTilemap, TileBase portalTile)
        {
            int centerX = worldX + roomWidth / 2;
            int centerY = worldY + roomHeight / 2;
            
            int portalX = 0, portalY = 0;
            
            switch (direction)
            {
                case Direction.North:
                    portalX = centerX - portalWidth / 2;
                    portalY = worldY + roomHeight - wallThickness;
                    ClearRect(groundTilemap, portalX, portalY, portalWidth, wallThickness);
                    ClearRect(groundTilemap, portalX, portalY - portalHeight, portalWidth, portalHeight + wallThickness);
                    break;
                    
                case Direction.South:
                    portalX = centerX - portalWidth / 2;
                    portalY = worldY;
                    ClearRect(groundTilemap, portalX, portalY, portalWidth, wallThickness);
                    ClearRect(groundTilemap, portalX, portalY, portalWidth, portalHeight + wallThickness);
                    break;
                    
                case Direction.West:
                    portalX = worldX;
                    portalY = centerY - portalWidth / 2;
                    ClearRect(groundTilemap, portalX, portalY, wallThickness, portalWidth);
                    ClearRect(groundTilemap, portalX, portalY, portalHeight + wallThickness, portalWidth);
                    break;
                    
                case Direction.East:
                    portalX = worldX + roomWidth - wallThickness;
                    portalY = centerY - portalWidth / 2;
                    ClearRect(groundTilemap, portalX, portalY, wallThickness, portalWidth);
                    ClearRect(groundTilemap, portalX - portalHeight, portalY, portalHeight + wallThickness, portalWidth);
                    break;
            }
        }
        
        /// <summary>
        /// 获取门户位置（用于标记显示）
        /// 返回门瓦片中心位置
        /// </summary>
        private Vector3 GetPortalPosition(int worldX, int worldY, int roomWidth, int roomHeight,
            int wallThickness, int portalWidth, Direction direction)
        {
            int centerX = worldX + roomWidth / 2;
            int centerY = worldY + roomHeight / 2;
            
            // 计算门瓦片中心位置（与 DrawPortal 中的 portalX, portalY 对应）
            switch (direction)
            {
                case Direction.North:
                    // 北向门：portalX = centerX - portalWidth/2, portalY = worldY + roomHeight - wallThickness
                    // 门瓦片中心：(portalX + portalWidth/2, portalY + wallThickness/2)
                    return new Vector3(centerX, worldY + roomHeight - wallThickness + wallThickness / 2f, 0);
                case Direction.South:
                    // 南向门：portalX = centerX - portalWidth/2, portalY = worldY
                    // 门瓦片中心：(portalX + portalWidth/2, portalY + wallThickness/2)
                    return new Vector3(centerX, worldY + wallThickness / 2f, 0);
                case Direction.East:
                    // 东向门：portalX = worldX + roomWidth - wallThickness, portalY = centerY - portalWidth/2
                    // 门瓦片中心：(portalX + wallThickness/2, portalY + portalWidth/2)
                    return new Vector3(worldX + roomWidth - wallThickness + wallThickness / 2f, centerY, 0);
                case Direction.West:
                    // 西向门：portalX = worldX, portalY = centerY - portalWidth/2
                    // 门瓦片中心：(portalX + wallThickness/2, portalY + portalWidth/2)
                    return new Vector3(worldX + wallThickness / 2f, centerY, 0);
                default:
                    return new Vector3(centerX, centerY, 0);
            }
        }
        
        /// <summary>
        /// 绘制平台
        /// </summary>
        private void DrawPlatforms(int offsetX, int offsetY, System.Random rng, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var tile = LevelGenerator.TileSet.PinkTile;
            
            int maxJumpHeight = LevelGenerator.MaxPlatformHeightDiff;
            int minGap = LevelGenerator.MinPlatformGap;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    if (room.Type == RoomType.Start || room.Type == RoomType.Exit) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // [极简主义平台生成] 禁用随机平台层生成
                    // 依赖垂锚修复(EnsurePlatformAccessibility)作为唯一的平台生成器
                    // 只在"断层过高"的地方生成必要的跳板
                    
                    // 原随机平台生成逻辑已禁用：
                    // int platformLayers = roomHeight / (maxJumpHeight + minGap);
                    // for (int layer = 0; layer < platformLayers; layer++) { ... }
                }
            }
            
            // 为所有垂直出口生成阶梯
            GenerateVerticalExitStaircases(offsetX, offsetY, shape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 全局垂锚连接 - 确保所有平台可达
            EnsurePlatformAccessibility(offsetX, offsetY, shape, roomWidth, roomHeight, wallThickness);
            
            // 应用平台规则瓦片替换（可选）
            ApplyPlatformRuleTileReplacement(offsetX, offsetY, shape, roomWidth, roomHeight);
        }
        
        /// <summary>
        /// 为所有房间的出口生成阶梯，确保玩家能够到达所有方向的高处出口
        /// </summary>
        private void GenerateVerticalExitStaircases(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var tile = LevelGenerator.TileSet.PinkTile;
            
            // 使用LevelGenerator的阶梯参数
            int safeHeight = LevelGenerator.StaircaseSafeHeight;
            int platformWidth = LevelGenerator.StaircasePlatformWidth;
            int horizontalOffset = LevelGenerator.StaircaseHorizontalOffset;
            
            Debug.Log($"[阶梯生成] 开始检测所有房间出口, safeHeight={safeHeight}, platformWidth={platformWidth}");
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 检查北向出口（需要向上到达）
                    if (room.HasConnection(Direction.North))
                    {
                        Debug.Log($"[阶梯生成] 房间[{gx},{gy}] 有北向出口，生成阶梯");
                        GenerateStaircaseToExit(worldX, worldY, roomWidth, roomHeight, wallThickness, 
                            safeHeight, platformWidth, horizontalOffset, Direction.North, tilemap, groundTilemap, tile);
                    }
                    
                    // 检查西向出口（在高处，需要向上到达）
                    if (room.HasConnection(Direction.West))
                    {
                        Debug.Log($"[阶梯生成] 房间[{gx},{gy}] 有西向出口，生成阶梯");
                        GenerateStaircaseToExit(worldX, worldY, roomWidth, roomHeight, wallThickness, 
                            safeHeight, platformWidth, horizontalOffset, Direction.West, tilemap, groundTilemap, tile);
                    }
                    
                    // 检查东向出口（在高处，需要向上到达）
                    if (room.HasConnection(Direction.East))
                    {
                        Debug.Log($"[阶梯生成] 房间[{gx},{gy}] 有东向出口，生成阶梯");
                        GenerateStaircaseToExit(worldX, worldY, roomWidth, roomHeight, wallThickness, 
                            safeHeight, platformWidth, horizontalOffset, Direction.East, tilemap, groundTilemap, tile);
                    }
                    
                    // 检查南向入口（需要确保有安全着陆区域）
                    if (room.HasConnection(Direction.South))
                    {
                        GenerateDownwardPath(worldX, worldY, roomWidth, wallThickness, tilemap, tile);
                    }
                }
            }
        }
        
        /// <summary>
        /// 根据出口方向生成通往出口的阶梯
        /// </summary>
        private void GenerateStaircaseToExit(int worldX, int worldY, int roomWidth, int roomHeight, int wallThickness,
            int safeHeight, int platformWidth, int horizontalOffset, Direction exitDir, Tilemap tilemap, Tilemap groundTilemap, TileBase tile)
        {
            // 根据出口方向计算出口踏板位置
            int exitY, exitX;
            int groundY = worldY + wallThickness + 1;
            
            switch (exitDir)
            {
                case Direction.North:
                    // 北向出口在房间顶部中央
                    exitY = worldY + roomHeight - wallThickness - 2;
                    exitX = worldX + roomWidth / 2;
                    break;
                case Direction.West:
                    // 西向出口在房间左侧，Y位置与北向相同（顶部边缘）
                    exitY = worldY + roomHeight - wallThickness - 2;
                    exitX = worldX + wallThickness + 2;
                    break;
                case Direction.East:
                    // 东向出口在房间右侧，Y位置与北向相同（顶部边缘）
                    exitY = worldY + roomHeight - wallThickness - 2;
                    exitX = worldX + roomWidth - wallThickness - 2;
                    break;
                default:
                    return; // 南向不需要向上阶梯
            }
            
            // 检查是否需要阶梯
            int heightDiff = exitY - groundY;
            Debug.Log($"[阶梯生成] 方向={exitDir}, exitY={exitY}, groundY={groundY}, heightDiff={heightDiff}, safeHeight={safeHeight}");
            if (heightDiff <= safeHeight)
            {
                Debug.Log($"[阶梯生成] 高度差{heightDiff}不超过安全高度{safeHeight}，跳过");
                return;
            }
            
            // 房间边界
            int roomLeft = worldX + wallThickness + 2;
            int roomRight = worldX + roomWidth - wallThickness - platformWidth - 2;
            
            // 当前锚点（从出口开始向下）
            int currentY = exitY;
            int currentX = exitX;
            bool placeLeft = (exitDir == Direction.East); // 东向出口先向左偏移，西向先向右
            
            // 循环生成阶梯
            int platformCount = 0;
            while (currentY - groundY > safeHeight)
            {
                int newY = currentY - safeHeight;
                platformCount++;
                
                // 计算X位置（交替偏移，防撞头）
                int newX;
                if (exitDir == Direction.North)
                {
                    // 北向出口：左右交替
                    int roomCenterX = worldX + roomWidth / 2;
                    newX = placeLeft 
                        ? Mathf.Clamp(roomCenterX - horizontalOffset, roomLeft, roomRight)
                        : Mathf.Clamp(roomCenterX + horizontalOffset, roomLeft, roomRight);
                }
                else if (exitDir == Direction.West)
                {
                    // 西向出口：从左向右延伸
                    newX = Mathf.Clamp(currentX + (placeLeft ? -2 : horizontalOffset), roomLeft, roomRight);
                }
                else // East
                {
                    // 东向出口：从右向左延伸
                    newX = Mathf.Clamp(currentX + (placeLeft ? -horizontalOffset : 2), roomLeft, roomRight);
                }
                
                // 生成平台
                Debug.Log($"[阶梯生成] 生成平台#{platformCount} 位置=({newX},{newY}), 宽度={platformWidth}");
                FillRect(tilemap, tile, newX, newY, platformWidth, 1);
                
                // 清除平台上方空间（防止卡头）
                ClearRect(groundTilemap, newX - 1, newY + 1, platformWidth + 2, 3);
                
                currentY = newY;
                currentX = newX;
                placeLeft = !placeLeft;
            }
            
            if (platformCount > 0)
            {
                Debug.Log($"[阶梯生成] 完成，共生成{platformCount}个阶梯平台");
            }
        }
        
        /// <summary>
        /// 确保南向入口下方有安全着陆区域
        /// </summary>
        private void GenerateDownwardPath(int worldX, int worldY, int roomWidth, int wallThickness, Tilemap tilemap, TileBase tile)
        {
            // 南向入口：玩家从上方掉落进入
            // 确保入口下方有安全着陆平台
            
            int entranceX = worldX + roomWidth / 2;
            int entranceY = worldY + wallThickness + 2;
            
            // 生成着陆平台
            FillRect(tilemap, tile, entranceX - 2, entranceY - 1, 5, 1);
        }
        
        #region 全局垂锚连接法 (Global Vertical Anchoring)
        
        /// <summary>
        /// 平台聚类数据结构
        /// </summary>
        private class PlatformCluster
        {
            public List<Vector3Int> Tiles = new List<Vector3Int>();
            public int MinX, MaxX;
            public int Y;
            
            public Vector3Int LeftPoint => new Vector3Int(MinX, Y, 0);
            public Vector3Int RightPoint => new Vector3Int(MaxX, Y, 0);
            public Vector3Int CenterPoint => new Vector3Int((MinX + MaxX) / 2, Y, 0);
        }
        
        /// <summary>
        /// 确保所有平台可达 - 全局垂锚连接法
        /// </summary>
        private void EnsurePlatformAccessibility(int offsetX, int offsetY, LevelShape shape, int roomWidth, int roomHeight, int wallThickness)
        {
            var platformTilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var tile = LevelGenerator.TileSet.PinkTile;
            
            int safeHeight = LevelGenerator.StaircaseSafeHeight;
            int anchorWidth = 3;
            int maxOffset = LevelGenerator.StaircaseHorizontalOffset;
            
            // 计算整个网格区域边界
            int totalWidth = LevelShape.GridWidth * roomWidth;
            int totalHeight = LevelShape.GridHeight * roomHeight;
            int minY = offsetY + wallThickness;
            int maxY = offsetY + totalHeight - wallThickness;
            
            Debug.Log($"[垂锚连接] 开始检测平台可达性, 区域: ({offsetX},{offsetY}) ~ ({offsetX + totalWidth},{maxY})");
            
            // 约束3：收集禁飞区（出口/入口附近10格）
            var noFlyZones = new List<(int x, int y, int radius)>();
            int noFlyRadius = 10;
            
            // 从入口和出口位置列表获取禁飞区
            foreach (var entrance in _entrancePositions)
            {
                noFlyZones.Add(((int)entrance.x, (int)entrance.y, noFlyRadius));
            }
            foreach (var exit in _exitPositions)
            {
                noFlyZones.Add(((int)exit.x, (int)exit.y, noFlyRadius));
            }
            
            Debug.Log($"[垂锚连接] 设置{noFlyZones.Count}个禁飞区");
            
            // 房间平台计数器（每房间最多3个平台）
            int maxPlatformsPerRoom = 3;
            var roomPlatformCount = new Dictionary<(int gx, int gy), int>();
            
            int maxIterations = 10;
            int iteration = 0;
            int totalAnchorsPlaced = 0;
            
            while (iteration < maxIterations)
            {
                // 获取所有平台聚类
                var clusters = GetPlatformClusters(platformTilemap, offsetX, offsetY, totalWidth, totalHeight, wallThickness);
                
                if (clusters.Count == 0) break;
                
                // 按Y从高到低排序
                clusters.Sort((a, b) => b.Y.CompareTo(a.Y));
                
                bool anyFixed = false;
                
                foreach (var cluster in clusters)
                {
                    // 跳过地面附近的平台
                    if (cluster.Y <= minY + 2) continue;
                    
                    // 约束3：禁飞区检查 - 跳过出口/入口附近的平台
                    bool inNoFlyZone = false;
                    foreach (var zone in noFlyZones)
                    {
                        int dx = Mathf.Abs(cluster.CenterPoint.x - zone.x);
                        int dy = Mathf.Abs(cluster.Y - zone.y);
                        if (dx <= zone.radius && dy <= zone.radius)
                        {
                            inNoFlyZone = true;
                            break;
                        }
                    }
                    if (inNoFlyZone) continue;
                    
                    // 从3个检查点向下射线检测
                    int minGap = int.MaxValue;
                    Vector3Int bestCheckPoint = cluster.CenterPoint;
                    
                    foreach (var checkPoint in new[] { cluster.LeftPoint, cluster.CenterPoint, cluster.RightPoint })
                    {
                        int gap = RaycastDown(checkPoint, platformTilemap, groundTilemap, 50);
                        if (gap < minGap)
                        {
                            minGap = gap;
                            bestCheckPoint = checkPoint;
                        }
                    }
                    
                    // 判定是否需要修复
                    if (minGap > safeHeight && minGap < 50)
                    {
                        // 水平可达性检查：如果左右附近有可跳达的平台，跳过
                        bool hasHorizontalPath = CheckHorizontalPath(cluster, platformTilemap, groundTilemap, safeHeight, maxOffset * 2);
                        if (hasHorizontalPath)
                        {
                            continue; // 旁边有路，不需要垂直造梯子
                        }
                        
                        // 计算中继平台位置
                        int anchorY = cluster.Y - safeHeight;
                        
                        // 左右摆动偏移
                        int offsetDir = (iteration + cluster.MinX) % 2 == 0 ? -1 : 1;
                        int anchorX = bestCheckPoint.x + offsetDir * maxOffset;
                        
                        // 边界检查
                        anchorX = Mathf.Clamp(anchorX, offsetX + wallThickness + 1, offsetX + totalWidth - wallThickness - anchorWidth - 1);
                        
                        // 检查目标位置是否已有平台
                        bool hasExisting = false;
                        for (int dx = 0; dx < anchorWidth; dx++)
                        {
                            if (platformTilemap.GetTile(new Vector3Int(anchorX + dx, anchorY, 0)) != null)
                            {
                                hasExisting = true;
                                break;
                            }
                        }
                        
                        if (!hasExisting)
                        {
                            // 计算平台所属房间
                            int roomGx = (anchorX - offsetX) / roomWidth;
                            int roomGy = LevelShape.GridHeight - 1 - (anchorY - offsetY) / roomHeight;
                            var roomKey = (roomGx, roomGy);
                            
                            // 检查房间平台数量限制
                            if (!roomPlatformCount.ContainsKey(roomKey))
                                roomPlatformCount[roomKey] = 0;
                            
                            if (roomPlatformCount[roomKey] >= maxPlatformsPerRoom)
                            {
                                Debug.Log($"[垂锚连接] 房间({roomGx},{roomGy})已达到{maxPlatformsPerRoom}个平台上限，跳过");
                                continue;
                            }
                            
                            // 生成中继平台
                            FillRect(platformTilemap, tile, anchorX, anchorY, anchorWidth, 1);
                            // 清除上方空间
                            ClearRect(groundTilemap, anchorX - 1, anchorY + 1, anchorWidth + 2, 3);
                            
                            roomPlatformCount[roomKey]++;
                            Debug.Log($"[垂锚连接] 迭代{iteration}: 在({anchorX},{anchorY})生成中继平台, 房间({roomGx},{roomGy})已有{roomPlatformCount[roomKey]}个平台");
                            anyFixed = true;
                            totalAnchorsPlaced++;
                        }
                    }
                }
                
                if (!anyFixed) break;
                iteration++;
            }
            
            Debug.Log($"[垂锚连接] 完成, 共{iteration}次迭代, 生成{totalAnchorsPlaced}个中继平台");
        }
        
        /// <summary>
        /// 获取所有独立平台聚类（只包含真正可站立的平台）
        /// </summary>
        private List<PlatformCluster> GetPlatformClusters(Tilemap platformTilemap, int offsetX, int offsetY, int totalWidth, int totalHeight, int wallThickness)
        {
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var clusters = new List<PlatformCluster>();
            var visited = new HashSet<Vector3Int>();
            
            // 扫描整个区域
            for (int y = offsetY; y < offsetY + totalHeight; y++)
            {
                PlatformCluster currentCluster = null;
                
                for (int x = offsetX; x < offsetX + totalWidth; x++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    
                    // 只处理PlatformLayer的瓦片
                    if (platformTilemap.GetTile(pos) != null && !visited.Contains(pos))
                    {
                        // 关键过滤：检查平台上方是否可站立（上方3格无阻挡）
                        bool isStandable = true;
                        for (int dy = 1; dy <= 3; dy++)
                        {
                            var above = new Vector3Int(x, y + dy, 0);
                            if (groundTilemap.GetTile(above) != null || platformTilemap.GetTile(above) != null)
                            {
                                isStandable = false;
                                break;
                            }
                        }
                        
                        // 跳过不可站立的平台（如天花板底部）
                        if (!isStandable)
                        {
                            visited.Add(pos);
                            continue;
                        }
                        
                        visited.Add(pos);
                        
                        if (currentCluster == null || currentCluster.Y != y)
                        {
                            // 开始新聚类
                            currentCluster = new PlatformCluster { Y = y, MinX = x, MaxX = x };
                            currentCluster.Tiles.Add(pos);
                            clusters.Add(currentCluster);
                        }
                        else
                        {
                            // 扩展当前聚类
                            currentCluster.Tiles.Add(pos);
                            currentCluster.MaxX = x;
                        }
                    }
                    else if (currentCluster != null && currentCluster.Y == y)
                    {
                        // 当前行聚类结束，准备下一个
                        currentCluster = null;
                    }
                }
            }
            
            return clusters;
        }
        
        /// <summary>
        /// 向下射线检测，返回到最近实体的距离
        /// </summary>
        private int RaycastDown(Vector3Int from, Tilemap platformTilemap, Tilemap groundTilemap, int maxDistance)
        {
            for (int dy = 1; dy <= maxDistance; dy++)
            {
                var below = new Vector3Int(from.x, from.y - dy, 0);
                if (platformTilemap.GetTile(below) != null || groundTilemap.GetTile(below) != null)
                {
                    return dy;
                }
            }
            return maxDistance;
        }
        
        /// <summary>
        /// 检查平台位置左右是否有可利用的地形（地形优先剔除）
        /// </summary>
        /// <param name="groundTilemap">地面层Tilemap</param>
        /// <param name="x">平台X坐标</param>
        /// <param name="y">平台Y坐标</param>
        /// <param name="width">平台宽度</param>
        /// <param name="scanRange">扫描范围（左右各多少格）</param>
        /// <returns>true=附近有地形可用，应跳过平台生成</returns>
        private bool HasNearbyTerrain(Tilemap groundTilemap, int x, int y, int width, int scanRange)
        {
            // 检查平台左侧
            for (int dx = 1; dx <= scanRange; dx++)
            {
                // 检查平台高度及上下1格范围（玩家可跳达）
                for (int dy = -1; dy <= 1; dy++)
                {
                    var pos = new Vector3Int(x - dx, y + dy, 0);
                    if (groundTilemap.GetTile(pos) != null)
                    {
                        // 检查该地形上方是否可站立
                        var above = new Vector3Int(x - dx, y + dy + 1, 0);
                        if (groundTilemap.GetTile(above) == null)
                            return true; // 左侧有可利用地形
                    }
                }
            }
            
            // 检查平台右侧
            for (int dx = 1; dx <= scanRange; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var pos = new Vector3Int(x + width + dx, y + dy, 0);
                    if (groundTilemap.GetTile(pos) != null)
                    {
                        var above = new Vector3Int(x + width + dx, y + dy + 1, 0);
                        if (groundTilemap.GetTile(above) == null)
                            return true; // 右侧有可利用地形
                    }
                }
            }
            
            return false; // 两侧都是空气，需要平台
        }
        
        /// <summary>
        /// 检查平台左右是否有可跳达的路径（水平可达性）
        /// </summary>
        private bool CheckHorizontalPath(PlatformCluster cluster, Tilemap platformTilemap, Tilemap groundTilemap, int safeHeight, int maxHorizontalCheck)
        {
            // 检查平台左侧
            for (int dx = 1; dx <= maxHorizontalCheck; dx++)
            {
                int checkX = cluster.MinX - dx;
                // 在安全跳跃高度范围内检查是否有平台或地面
                for (int dy = 0; dy <= safeHeight; dy++)
                {
                    var pos = new Vector3Int(checkX, cluster.Y - dy, 0);
                    if (platformTilemap.GetTile(pos) != null || groundTilemap.GetTile(pos) != null)
                    {
                        // 检查该位置上方是否可站立
                        var above = new Vector3Int(checkX, cluster.Y - dy + 1, 0);
                        if (groundTilemap.GetTile(above) == null)
                        {
                            return true; // 左侧有可达路径
                        }
                    }
                }
            }
            
            // 检查平台右侧
            for (int dx = 1; dx <= maxHorizontalCheck; dx++)
            {
                int checkX = cluster.MaxX + dx;
                for (int dy = 0; dy <= safeHeight; dy++)
                {
                    var pos = new Vector3Int(checkX, cluster.Y - dy, 0);
                    if (platformTilemap.GetTile(pos) != null || groundTilemap.GetTile(pos) != null)
                    {
                        var above = new Vector3Int(checkX, cluster.Y - dy + 1, 0);
                        if (groundTilemap.GetTile(above) == null)
                        {
                            return true; // 右侧有可达路径
                        }
                    }
                }
            }
            
            return false; // 左右都没有可达路径
        }
        
        #endregion
        
        /// <summary>
        /// 将平台瓦片替换为规则瓦片
        /// </summary>
        private void ApplyPlatformRuleTileReplacement(int offsetX, int offsetY, LevelShape shape, int roomWidth, int roomHeight)
        {
            var ruleTile = GetCurrentPlatformRuleTile();
            if (ruleTile == null) return;
            
            var platformTilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            
            // 收集所有平台瓦片位置
            HashSet<Vector3Int> platformPositions = new HashSet<Vector3Int>();
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    for (int y = 0; y < roomHeight; y++)
                    {
                        for (int x = 0; x < roomWidth; x++)
                        {
                            Vector3Int pos = new Vector3Int(worldX + x, worldY + y, 0);
                            if (platformTilemap.GetTile(pos) != null)
                            {
                                platformPositions.Add(pos);
                            }
                        }
                    }
                }
            }
            
            // 替换为规则瓦片
            foreach (var pos in platformPositions)
            {
                platformTilemap.SetTile(pos, ruleTile);
            }
            
            // 刷新瓦片地图
            platformTilemap.RefreshAllTiles();
        }
        
        /// <summary>
        /// 绘制特殊区域（简化版，仅清空Boss房间区域并添加平台）
        /// </summary>
        private void DrawSpecialAreas(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight)
        {
            var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
            var platformTilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            
            // 优先使用规则瓦片
            var platformRuleTile = GetCurrentPlatformRuleTile();
            TileBase platformTile = platformRuleTile != null ? (TileBase)platformRuleTile : LevelGenerator.TileSet.PinkTile;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    var room = roomGrid[gx, gy];
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    if (room.Type == RoomType.Boss)
                    {
                        // Boss房间：清空中央区域，添加底部平台
                        ClearRect(groundTilemap, worldX + 3, worldY + 3, roomWidth - 6, roomHeight - 6);
                        FillRect(platformTilemap, platformTile, worldX + 3, worldY + 3, roomWidth - 6, 1);
                    }
                }
            }
            
            // 刷新平台层
            platformTilemap.RefreshAllTiles();
        }
        
        /// <summary>
        /// 获取网格种子
        /// </summary>
        private int GetGridSeed(int gridIndex)
        {
            if (UseUniqueSeedPerGrid)
            {
                return BaseSeed == 0 ? _rng.Next() : BaseSeed + gridIndex * 1000;
            }
            return BaseSeed == 0 ? _rng.Next() : BaseSeed;
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
        /// 验证设置
        /// </summary>
        private bool ValidateSetup()
        {
            if (LevelGenerator == null)
            {
                Debug.LogError("MultiGridLevelManager: 未配置LevelGenerator引用!");
                return false;
            }
            
            if (LevelGenerator.TilemapLayers == null || !LevelGenerator.TilemapLayers.IsValid())
            {
                Debug.LogError("MultiGridLevelManager: LevelGenerator的TilemapLayers未配置!");
                return false;
            }
            
            if (LevelGenerator.TileSet == null || !LevelGenerator.TileSet.IsValid())
            {
                Debug.LogError("MultiGridLevelManager: LevelGenerator的TileSet未配置!");
                return false;
            }
            
            return true;
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
        
        /// <summary>
        /// 清除线段区域
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
        /// 在Scene视图中绘制调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            if (LevelGenerator == null) return;
            
            int gridWidth = SingleGridWidth;
            int gridHeight = SingleGridHeight;
            
            // 绘制网格边界
            if (ShowGridBounds)
            {
                // 绘制布局区域边界
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Vector3 layoutCenter = new Vector3(LayoutAreaWidth / 2f, LayoutAreaHeight / 2f, 0);
                Vector3 layoutSize = new Vector3(LayoutAreaWidth, LayoutAreaHeight, 0);
                Gizmos.DrawWireCube(layoutCenter, layoutSize);
                
                // 绘制已生成的网格边界
                Gizmos.color = GridBoundsColor;
                foreach (var bounds in _placedGridBounds)
                {
                    Vector3 center = new Vector3(
                        bounds.x + bounds.width / 2f,
                        bounds.y + bounds.height / 2f,
                        0);
                    Vector3 size = new Vector3(bounds.width, bounds.height, 0);
                    Gizmos.DrawWireCube(center, size);
                }
                
                // 如果没有已生成的网格，显示预估位置
                if (_placedGridBounds.Count == 0)
                {
                    Gizmos.color = new Color(GridBoundsColor.r, GridBoundsColor.g, GridBoundsColor.b, 0.2f);
                    
                    int cols = Mathf.Max(1, LayoutAreaWidth / (gridWidth + MinGridSpacing));
                    int rows = Mathf.Max(1, LayoutAreaHeight / (gridHeight + MinGridSpacing));
                    int showCount = Mathf.Min(GridCount, cols * rows);
                    
                    for (int i = 0; i < showCount; i++)
                    {
                        int col = i % cols;
                        int row = i / cols;
                        
                        float x = col * (gridWidth + MinGridSpacing) + gridWidth / 2f;
                        float y = row * (gridHeight + MinGridSpacing) + gridHeight / 2f;
                        
                        Vector3 center = new Vector3(x, y, 0);
                        Vector3 size = new Vector3(gridWidth, gridHeight, 0);
                        Gizmos.DrawWireCube(center, size);
                    }
                }
            }
            
            // 绘制入口和出口标记（线框+方向箭头）
            if (ShowEntranceExitMarkers)
            {
                // 绘制入口标记（绿色线框+箭头指向内部）
                Gizmos.color = EntranceMarkerColor;
                for (int i = 0; i < _entrancePositions.Count; i++)
                {
                    Vector3 pos = _entrancePositions[i];
                    Direction dir = i < _entranceDirections.Count ? _entranceDirections[i] : Direction.North;
                    
                    // 绘制线框矩形
                    DrawWireRect(pos, MarkerSize * 2f, dir);
                    
                    // 根据方向绘制箭头（指向房间内部）
                    DrawDirectionalArrow(pos, dir, true);
                }
                
                // 绘制出口标记（红色线框+箭头指向外部）
                Gizmos.color = ExitMarkerColor;
                for (int i = 0; i < _exitPositions.Count; i++)
                {
                    Vector3 pos = _exitPositions[i];
                    Direction dir = i < _exitDirections.Count ? _exitDirections[i] : Direction.South;
                    
                    // 绘制线框矩形
                    DrawWireRect(pos, MarkerSize * 2f, dir);
                    
                    // 根据方向绘制箭头（指向房间外部）
                    DrawDirectionalArrow(pos, dir, false);
                }
                
                // 绘制玩家出生点（蓝色圆形）
                if (_hasSpawnPoint)
                {
                    Gizmos.color = new Color(0f, 0.5f, 1f, 1f); // 蓝色
                    Gizmos.DrawWireSphere(_playerSpawnPoint, MarkerSize * 1.5f);
                    Gizmos.DrawSphere(_playerSpawnPoint, MarkerSize * 0.5f);
                }
                
                // 绘制通关点（金色圆形）
                if (_hasExitPoint)
                {
                    Gizmos.color = new Color(1f, 0.8f, 0f, 1f); // 金色
                    Gizmos.DrawWireSphere(_levelExitPoint, MarkerSize * 1.5f);
                    Gizmos.DrawSphere(_levelExitPoint, MarkerSize * 0.5f);
                }
            }
            
        }
        
        /// <summary>
        /// 绘制线框矩形
        /// </summary>
        private void DrawWireRect(Vector3 center, float size, Direction direction)
        {
            float halfSize = size / 2f;
            Vector3[] corners;
            
            // 根据方向决定矩形的宽高比
            if (direction == Direction.North || direction == Direction.South)
            {
                // 水平方向的门户，宽大于高
                corners = new Vector3[]
                {
                    center + new Vector3(-halfSize * 1.5f, -halfSize * 0.5f, 0),
                    center + new Vector3(halfSize * 1.5f, -halfSize * 0.5f, 0),
                    center + new Vector3(halfSize * 1.5f, halfSize * 0.5f, 0),
                    center + new Vector3(-halfSize * 1.5f, halfSize * 0.5f, 0)
                };
            }
            else
            {
                // 垂直方向的门户，高大于宽
                corners = new Vector3[]
                {
                    center + new Vector3(-halfSize * 0.5f, -halfSize * 1.5f, 0),
                    center + new Vector3(halfSize * 0.5f, -halfSize * 1.5f, 0),
                    center + new Vector3(halfSize * 0.5f, halfSize * 1.5f, 0),
                    center + new Vector3(-halfSize * 0.5f, halfSize * 1.5f, 0)
                };
            }
            
            // 绘制线框
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
        }
        
        /// <summary>
        /// 根据方向绘制箭头
        /// </summary>
        private void DrawDirectionalArrow(Vector3 pos, Direction direction, bool pointInward)
        {
            Vector3 arrowDir = Vector3.zero;
            Vector3 perpendicular = Vector3.zero;
            
            // 确定箭头方向（入口指向内部，出口指向外部）
            switch (direction)
            {
                case Direction.North:
                    arrowDir = pointInward ? Vector3.down : Vector3.up;
                    perpendicular = Vector3.right;
                    break;
                case Direction.South:
                    arrowDir = pointInward ? Vector3.up : Vector3.down;
                    perpendicular = Vector3.right;
                    break;
                case Direction.West:
                    arrowDir = pointInward ? Vector3.right : Vector3.left;
                    perpendicular = Vector3.up;
                    break;
                case Direction.East:
                    arrowDir = pointInward ? Vector3.left : Vector3.right;
                    perpendicular = Vector3.up;
                    break;
            }
            
            // 绘制箭头
            Vector3 arrowTip = pos + arrowDir * (MarkerSize * 2.5f);
            Vector3 arrowLeft = pos + arrowDir * (MarkerSize * 1.5f) + perpendicular * MarkerSize;
            Vector3 arrowRight = pos + arrowDir * (MarkerSize * 1.5f) - perpendicular * MarkerSize;
            
            Gizmos.DrawLine(pos, arrowTip);
            Gizmos.DrawLine(arrowTip, arrowLeft);
            Gizmos.DrawLine(arrowTip, arrowRight);
        }
        
        /// <summary>
        /// 获取所有入口位置（供外部访问）
        /// </summary>
        public List<Vector3> GetEntrancePositions()
        {
            return new List<Vector3>(_entrancePositions);
        }
        
        /// <summary>
        /// 获取所有出口位置（供外部访问）
        /// </summary>
        public List<Vector3> GetExitPositions()
        {
            return new List<Vector3>(_exitPositions);
        }
        
        /// <summary>
        /// 获取所有入口方向（供外部访问）
        /// </summary>
        public List<Direction> GetEntranceDirections()
        {
            return new List<Direction>(_entranceDirections);
        }
        
        /// <summary>
        /// 获取所有出口方向（供外部访问）
        /// </summary>
        public List<Direction> GetExitDirections()
        {
            return new List<Direction>(_exitDirections);
        }
        
        /// <summary>
        /// 获取玩家出生点
        /// </summary>
        public Vector3 GetPlayerSpawnPoint()
        {
            return _playerSpawnPoint;
        }
        
        /// <summary>
        /// 获取通关点
        /// </summary>
        public Vector3 GetLevelExitPoint()
        {
            return _levelExitPoint;
        }
        
        /// <summary>
        /// 是否有有效的出生点
        /// </summary>
        public bool HasSpawnPoint => _hasSpawnPoint;
        
        /// <summary>
        /// 是否有有效的通关点
        /// </summary>
        public bool HasExitPoint => _hasExitPoint;
        
        /// <summary>
        /// 计算出生点和通关点（使用BFS算法寻找最远可站立位置）
        /// </summary>
        private void CalculateSpawnAndExitPoints()
        {
            _hasSpawnPoint = false;
            _hasExitPoint = false;
            
            if (LevelGenerator == null || LevelGenerator.TilemapLayers == null)
            {
                Debug.LogWarning("无法计算出生点/通关点：LevelGenerator或TilemapLayers未配置");
                return;
            }
            
            var calculator = new SpawnExitPointCalculator();
            var ground = LevelGenerator.TilemapLayers.GroundLayer;
            var platform = LevelGenerator.TilemapLayers.PlatformLayer;
            var bounds = _placedGridBounds;
            
            // 计算出生点（第一个网格，参考该网格的出口位置）
            if (_exitPositions.Count > 0 && bounds.Count > 0)
            {
                Vector3 exitRef = _exitPositions[0]; // 第一个网格的出口
                Rect startBounds = bounds[0];        // 第一个网格的边界
                _playerSpawnPoint = calculator.CalculateFarthestPoint(exitRef, startBounds, ground, platform);
                _hasSpawnPoint = true;
                
                // 清理出生点周围的平台，确保玩家有足够活动空间
                ClearSafetyBox(_playerSpawnPoint, platform);
                Debug.Log($"出生点计算完成: {_playerSpawnPoint} (距离出口最远，已清理安全区)");
            }
            
            // 计算通关点（最后一个网格，参考该网格的入口位置）
            int lastIndex = _entrancePositions.Count - 1;
            int lastBoundsIndex = bounds.Count - 1;
            if (lastIndex >= 0 && lastBoundsIndex >= 0)
            {
                Vector3 entranceRef = _entrancePositions[lastIndex]; // 最后一个网格的入口
                Rect endBounds = bounds[lastBoundsIndex];            // 最后一个网格的边界
                _levelExitPoint = calculator.CalculateFarthestPoint(entranceRef, endBounds, ground, platform);
                _hasExitPoint = true;
                
                // 清理通关点周围的平台，确保玩家有足够活动空间
                ClearSafetyBox(_levelExitPoint, platform);
                Debug.Log($"通关点计算完成: {_levelExitPoint} (距离入口最远，已清理安全区)");
            }
        }
        
        /// <summary>
        /// 清理指定点周围的平台瓦片，创建玩家安全活动区域
        /// 安全区尺寸: 宽3格 × 高4格（以点为底边中心）
        /// </summary>
        private void ClearSafetyBox(Vector3 centerPoint, Tilemap platformLayer)
        {
            if (platformLayer == null) return;
            
            // 安全区尺寸常量
            const int safetyWidth = 3;   // 宽度：左1 + 中 + 右1
            const int safetyHeight = 4;  // 高度：玩家位置 + 跳跃空间
            
            // 将世界坐标转换为瓦片坐标
            int centerX = Mathf.RoundToInt(centerPoint.x);
            int centerY = Mathf.RoundToInt(centerPoint.y);
            
            int clearedCount = 0;
            
            // 遍历安全区范围（以centerPoint为底边中心）
            // X范围: centerX-1 到 centerX+1 (共3格)
            // Y范围: centerY 到 centerY+3 (共4格，包含玩家站立位置和头顶空间)
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int y = centerY; y <= centerY + safetyHeight - 1; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    
                    // 只清除平台层，不触碰地面层
                    if (platformLayer.HasTile(tilePos))
                    {
                        platformLayer.SetTile(tilePos, null);
                        clearedCount++;
                    }
                }
            }
            
            if (clearedCount > 0)
            {
                Debug.Log($"安全区清理完成: 移除了 {clearedCount} 个平台瓦片 (位置: {centerX}, {centerY})");
            }
        }
        
    }
}
