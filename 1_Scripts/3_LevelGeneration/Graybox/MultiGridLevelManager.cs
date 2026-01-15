using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace CryptaGeometrica.LevelGeneration.Graybox
{
    /// <summary>
    /// 多网格关卡管理器
    /// 在场景中生成多个独立的4x4网格关卡区域
    /// </summary>
    public class MultiGridLevelManager : MonoBehaviour
    {
        [Header("生成器引用")]
        [Tooltip("灰盒关卡生成器组件")]
        public GrayboxLevelGenerator LevelGenerator;
        
        [Header("多网格布局")]
        [Tooltip("网格总数量")]
        [Range(1, 8)]
        public int GridCount = 4;
        
        [Tooltip("布局区域宽度（瓦片数）")]
        [Range(100, 500)]
        public int LayoutAreaWidth = 200;
        
        [Tooltip("布局区域高度（瓦片数）")]
        [Range(100, 500)]
        public int LayoutAreaHeight = 200;
        
        [Tooltip("网格之间的最小间距（瓦片数）")]
        [Range(8, 64)]
        public int MinGridSpacing = 16;
        
        [Tooltip("位置随机偏移范围（瓦片数）")]
        [Range(0, 32)]
        public int PositionRandomOffset = 16;
        
        [Header("随机性控制")]
        [Tooltip("基础随机种子 (0=完全随机)")]
        public int BaseSeed = 0;
        
        [Tooltip("每个网格使用独立的随机种子")]
        public bool UseUniqueSeedPerGrid = true;
        
        [Tooltip("位置生成最大尝试次数")]
        [Range(50, 500)]
        public int MaxPlacementAttempts = 100;
        
        [Header("特殊区域设置")]
        [Tooltip("中位数网格特殊区域生成概率")]
        [Range(0f, 1f)]
        public float MedianGridSpecialChance = 0.8f;
        
        [Tooltip("其他网格特殊区域生成概率")]
        [Range(0f, 1f)]
        public float OtherGridSpecialChance = 0.15f;
        
        [Header("调试显示")]
        [Tooltip("在Scene视图中显示网格边界")]
        public bool ShowGridBounds = true;
        
        [Tooltip("网格边界颜色")]
        public Color GridBoundsColor = new Color(0f, 1f, 0f, 0.5f);
        
        [Header("出入口标记显示")]
        [Tooltip("显示出入口标记")]
        public bool ShowEntranceExitMarkers = true;
        
        [Tooltip("入口标记颜色")]
        public Color EntranceMarkerColor = new Color(0f, 1f, 0f, 1f);
        
        [Tooltip("出口标记颜色")]
        public Color ExitMarkerColor = new Color(0f, 0f, 0f, 1f);
        
        [Tooltip("标记大小")]
        [Range(1f, 10f)]
        public float MarkerSize = 4f;
        
        // 内部状态
        private System.Random _rng;
        private List<Rect> _placedGridBounds = new List<Rect>();
        private List<Vector2Int> _gridPositions = new List<Vector2Int>();
        
        // 存储每个网格的入口和出口位置及方向
        private List<Vector3> _entrancePositions = new List<Vector3>();
        private List<Vector3> _exitPositions = new List<Vector3>();
        private List<Direction> _entranceDirections = new List<Direction>();
        private List<Direction> _exitDirections = new List<Direction>();
        
        // 计算属性
        private int SingleGridWidth => LevelShape.GridWidth * LevelGenerator.RoomWidth;
        private int SingleGridHeight => LevelShape.GridHeight * LevelGenerator.RoomHeight;
        
        /// <summary>
        /// 生成多网格关卡
        /// </summary>
        [ContextMenu("生成多网格关卡")]
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
                
                // 生成单个网格
                GenerateSingleGridAtOffset(pos.x, pos.y, gridSeed, generateSpecialArea);
                
                string specialInfo = generateSpecialArea ? ", 包含特殊区域" : "";
                Debug.Log($"  网格[{i}] 生成完成 (位置: {pos.x},{pos.y}, 种子: {gridSeed}{specialInfo})");
            }
            
            Debug.Log($"多网格关卡生成完成! 共{_gridPositions.Count}个独立网格");
        }
        
        /// <summary>
        /// 生成随机位置（确保不重叠）
        /// </summary>
        private bool GenerateRandomPositions()
        {
            int gridWidth = SingleGridWidth;
            int gridHeight = SingleGridHeight;
            int spacing = MinGridSpacing;
            
            // 计算可用区域
            int maxX = LayoutAreaWidth - gridWidth;
            int maxY = LayoutAreaHeight - gridHeight;
            
            if (maxX < 0 || maxY < 0)
            {
                Debug.LogError($"布局区域太小! 需要至少 {gridWidth}x{gridHeight} 的空间");
                return false;
            }
            
            for (int gridIndex = 0; gridIndex < GridCount; gridIndex++)
            {
                bool placed = false;
                
                for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
                {
                    // 随机生成位置
                    int x = _rng.Next(0, maxX + 1);
                    int y = _rng.Next(0, maxY + 1);
                    
                    // 添加随机偏移
                    if (PositionRandomOffset > 0)
                    {
                        x += _rng.Next(-PositionRandomOffset, PositionRandomOffset + 1);
                        y += _rng.Next(-PositionRandomOffset, PositionRandomOffset + 1);
                        
                        // 确保在有效范围内
                        x = Mathf.Clamp(x, 0, maxX);
                        y = Mathf.Clamp(y, 0, maxY);
                    }
                    
                    // 检查是否与已放置的网格重叠
                    Rect newBounds = new Rect(x - spacing, y - spacing, 
                        gridWidth + spacing * 2, gridHeight + spacing * 2);
                    
                    bool overlaps = false;
                    foreach (var existingBounds in _placedGridBounds)
                    {
                        if (newBounds.Overlaps(existingBounds))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    
                    if (!overlaps)
                    {
                        _gridPositions.Add(new Vector2Int(x, y));
                        _placedGridBounds.Add(new Rect(x, y, gridWidth, gridHeight));
                        placed = true;
                        break;
                    }
                }
                
                if (!placed)
                {
                    Debug.LogWarning($"无法放置第{gridIndex + 1}个网格，已尝试{MaxPlacementAttempts}次");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 清除所有网格
        /// </summary>
        [ContextMenu("清除所有网格")]
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
        private void GenerateSingleGridAtOffset(int offsetX, int offsetY, int seed, bool generateSpecialArea = true)
        {
            // 设置生成器的随机种子
            LevelGenerator.RandomSeed = seed;
            
            // 获取Tilemap引用
            var tilemapLayers = LevelGenerator.TilemapLayers;
            var tileSet = LevelGenerator.TileSet;
            
            // 调用生成器的内部方法（通过反射或直接调用公开方法）
            // 由于现有Generator不支持偏移，我们需要直接操作Tilemap
            GenerateGridContent(offsetX, offsetY, seed, generateSpecialArea);
        }
        
        /// <summary>
        /// 生成网格内容（带偏移）
        /// </summary>
        private void GenerateGridContent(int offsetX, int offsetY, int seed, bool generateSpecialArea = true)
        {
            var rng = new System.Random(seed);
            var tilemapLayers = LevelGenerator.TilemapLayers;
            var tileSet = LevelGenerator.TileSet;
            
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
            
            // 绘制外围墙壁
            DrawOuterWalls(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制洞穴填充
            DrawCaveFill(offsetX, offsetY, rng, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制房间连接
            DrawRoomConnections(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
            // 绘制入口和出口
            DrawEntranceAndExit(offsetX, offsetY, currentShape, roomGrid, roomWidth, roomHeight, wallThickness);
            
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
        /// 绘制外围墙壁
        /// </summary>
        private void DrawOuterWalls(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var tilemap = LevelGenerator.TilemapLayers.WallLayer;
            var tile = LevelGenerator.TileSet.RedTile;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 北边
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                    {
                        FillRect(tilemap, tile, worldX, worldY + roomHeight - wallThickness, roomWidth, wallThickness);
                    }
                    // 南边
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                    {
                        FillRect(tilemap, tile, worldX, worldY, roomWidth, wallThickness);
                    }
                    // 西边
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                    {
                        FillRect(tilemap, tile, worldX, worldY, wallThickness, roomHeight);
                    }
                    // 东边
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
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            var tile = LevelGenerator.TileSet.OrangeTile;
            float fillDensity = LevelGenerator.FillDensity;
            int smoothIterations = LevelGenerator.SmoothIterations;
            
            // 添加边界墙壁
            int baseBoundaryWidth = 12;
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                        FillRect(tilemap, tile, worldX, worldY + roomHeight - baseBoundaryWidth, roomWidth, baseBoundaryWidth);
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                        FillRect(tilemap, tile, worldX, worldY, roomWidth, baseBoundaryWidth);
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                        FillRect(tilemap, tile, worldX, worldY, baseBoundaryWidth, roomHeight);
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                        FillRect(tilemap, tile, worldX + roomWidth - baseBoundaryWidth, worldY, baseBoundaryWidth, roomHeight);
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
        }
        
        /// <summary>
        /// 生成连贯洞穴填充
        /// </summary>
        private void GenerateConnectedCaveFill(int offsetX, int offsetY, System.Random rng, LevelShape shape, int roomWidth, int roomHeight, float fillDensity, int smoothIterations)
        {
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            var tile = LevelGenerator.TileSet.OrangeTile;
            
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
                            tilemap.SetTile(new Vector3Int(offsetX + worldX, offsetY + worldY, 0), tile);
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
        /// 雕刻边界边缘
        /// </summary>
        private void CarveBoundaryEdges(int offsetX, int offsetY, System.Random rng, LevelShape shape, int roomWidth, int roomHeight)
        {
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
            int carveDepth = 6;
            int carveVariation = 4;
            
            for (int gy = 0; gy < LevelShape.GridHeight; gy++)
            {
                for (int gx = 0; gx < LevelShape.GridWidth; gx++)
                {
                    if (!shape.IsValidCell(gx, gy)) continue;
                    
                    int worldX = offsetX + gx * roomWidth;
                    int worldY = offsetY + (LevelShape.GridHeight - 1 - gy) * roomHeight;
                    
                    // 北边
                    if (gy == 0 || !shape.IsValidCell(gx, gy - 1))
                    {
                        int baseY = worldY + roomHeight - 12;
                        for (int x = 0; x < roomWidth; x++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                tilemap.SetTile(new Vector3Int(worldX + x, baseY + y, 0), null);
                            }
                        }
                    }
                    
                    // 南边
                    if (gy == LevelShape.GridHeight - 1 || !shape.IsValidCell(gx, gy + 1))
                    {
                        int baseY = worldY + 12;
                        for (int x = 0; x < roomWidth; x++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(x * 0.3f) * 3);
                            for (int y = 0; y < depth; y++)
                            {
                                tilemap.SetTile(new Vector3Int(worldX + x, baseY - y - 1, 0), null);
                            }
                        }
                    }
                    
                    // 西边
                    if (gx == 0 || !shape.IsValidCell(gx - 1, gy))
                    {
                        int baseX = worldX + 12;
                        for (int y = 0; y < roomHeight; y++)
                        {
                            int depth = carveDepth + rng.Next(-carveVariation, carveVariation + 1);
                            depth += (int)(Mathf.Sin(y * 0.3f) * 3);
                            for (int x = 0; x < depth; x++)
                            {
                                tilemap.SetTile(new Vector3Int(baseX - x - 1, worldY + y, 0), null);
                            }
                        }
                    }
                    
                    // 东边
                    if (gx == LevelShape.GridWidth - 1 || !shape.IsValidCell(gx + 1, gy))
                    {
                        int baseX = worldX + roomWidth - 12;
                        for (int y = 0; y < roomHeight; y++)
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
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
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
            var tilemap = LevelGenerator.TilemapLayers.FillLayer;
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
            var wallTilemap = LevelGenerator.TilemapLayers.WallLayer;
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
                        ClearRect(wallTilemap, centerX - passageWidth / 2, worldY + roomHeight - wallThickness, passageWidth, wallThickness);
                    if (room.HasConnection(Direction.South))
                        ClearRect(wallTilemap, centerX - passageWidth / 2, worldY, passageWidth, wallThickness);
                    if (room.HasConnection(Direction.East))
                        ClearRect(wallTilemap, worldX + roomWidth - wallThickness, centerY - passageWidth / 2, wallThickness, passageWidth);
                    if (room.HasConnection(Direction.West))
                        ClearRect(wallTilemap, worldX, centerY - passageWidth / 2, wallThickness, passageWidth);
                }
            }
        }
        
        /// <summary>
        /// 绘制入口和出口（根据房间位置智能选择方向）
        /// </summary>
        private void DrawEntranceAndExit(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)
        {
            var entranceTilemap = LevelGenerator.TilemapLayers.EntranceLayer;
            var exitTilemap = LevelGenerator.TilemapLayers.ExitLayer;
            var wallTilemap = LevelGenerator.TilemapLayers.WallLayer;
            var fillTilemap = LevelGenerator.TilemapLayers.FillLayer;
            var greenTile = LevelGenerator.TileSet.GreenTile;
            var blackTile = LevelGenerator.TileSet.BlackTile;
            
            int entranceWidth = LevelGenerator.EntranceWidth;
            int entranceHeight = LevelGenerator.EntranceHeight;
            
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
                    
                    if (room.Type == RoomType.Start)
                    {
                        // 根据房间在网格中的位置选择入口方向
                        Direction entranceDir = GetBestEntranceDirection(gx, gy, shape);
                        DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness, 
                            entranceWidth, entranceHeight, entranceDir, true,
                            wallTilemap, fillTilemap, entranceTilemap, greenTile);
                        
                        // 记录入口位置和方向
                        Vector3 entrancePos = GetPortalPosition(worldX, worldY, roomWidth, roomHeight, 
                            wallThickness, entranceWidth, entranceDir);
                        _entrancePositions.Add(entrancePos);
                        _entranceDirections.Add(entranceDir);
                    }
                    
                    if (room.Type == RoomType.Exit)
                    {
                        // 根据房间在网格中的位置选择出口方向
                        Direction exitDir = GetBestExitDirection(gx, gy, shape);
                        DrawPortal(worldX, worldY, roomWidth, roomHeight, wallThickness,
                            entranceWidth, entranceHeight, exitDir, false,
                            wallTilemap, fillTilemap, exitTilemap, blackTile);
                        
                        // 记录出口位置和方向
                        Vector3 exitPos = GetPortalPosition(worldX, worldY, roomWidth, roomHeight,
                            wallThickness, entranceWidth, exitDir);
                        _exitPositions.Add(exitPos);
                        _exitDirections.Add(exitDir);
                    }
                }
            }
        }
        
        /// <summary>
        /// 根据房间位置获取最佳入口方向
        /// </summary>
        private Direction GetBestEntranceDirection(int gx, int gy, LevelShape shape)
        {
            // 入口方向选择策略：优先选择外围方向，避开有效相邻房间
            // 收集所有可用的外围方向
            List<Direction> availableDirections = new List<Direction>();
            
            // 检查各个方向是否可用（在边缘或没有有效相邻房间）
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            // 优先级：左侧 > 顶部 > 右侧（避免与出口方向冲突）
            if (canWest) availableDirections.Add(Direction.West);
            if (canNorth) availableDirections.Add(Direction.North);
            if (canEast) availableDirections.Add(Direction.East);
            
            // 如果有可用方向，随机选择一个
            if (availableDirections.Count > 0)
            {
                return availableDirections[_rng.Next(availableDirections.Count)];
            }
            
            // 默认顶部
            return Direction.North;
        }
        
        /// <summary>
        /// 根据房间位置获取最佳出口方向
        /// </summary>
        private Direction GetBestExitDirection(int gx, int gy, LevelShape shape)
        {
            // 出口方向选择策略：优先选择外围方向，避开有效相邻房间
            // 收集所有可用的外围方向
            List<Direction> availableDirections = new List<Direction>();
            
            // 检查各个方向是否可用（在边缘或没有有效相邻房间）
            bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
            bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
            bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
            bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
            
            // 优先级：右侧 > 底部 > 左侧（避免与入口方向冲突）
            if (canEast) availableDirections.Add(Direction.East);
            if (canSouth) availableDirections.Add(Direction.South);
            if (canWest) availableDirections.Add(Direction.West);
            
            // 如果有可用方向，随机选择一个
            if (availableDirections.Count > 0)
            {
                return availableDirections[_rng.Next(availableDirections.Count)];
            }
            
            // 默认底部
            return Direction.South;
        }
        
        /// <summary>
        /// 绘制入口/出口门户
        /// </summary>
        private void DrawPortal(int worldX, int worldY, int roomWidth, int roomHeight, int wallThickness,
            int portalWidth, int portalHeight, Direction direction, bool isEntrance,
            Tilemap wallTilemap, Tilemap fillTilemap, Tilemap portalTilemap, TileBase portalTile)
        {
            int centerX = worldX + roomWidth / 2;
            int centerY = worldY + roomHeight / 2;
            
            int portalX, portalY, clearWidth, clearHeight;
            
            switch (direction)
            {
                case Direction.North:
                    portalX = centerX - portalWidth / 2;
                    portalY = worldY + roomHeight - wallThickness;
                    ClearRect(wallTilemap, portalX, portalY, portalWidth, wallThickness);
                    ClearRect(fillTilemap, portalX, portalY - portalHeight, portalWidth, portalHeight + wallThickness);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, portalWidth, wallThickness);
                    break;
                    
                case Direction.South:
                    portalX = centerX - portalWidth / 2;
                    portalY = worldY;
                    ClearRect(wallTilemap, portalX, portalY, portalWidth, wallThickness);
                    ClearRect(fillTilemap, portalX, portalY, portalWidth, portalHeight + wallThickness);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, portalWidth, wallThickness);
                    break;
                    
                case Direction.West:
                    portalX = worldX;
                    portalY = centerY - portalWidth / 2;
                    ClearRect(wallTilemap, portalX, portalY, wallThickness, portalWidth);
                    ClearRect(fillTilemap, portalX, portalY, portalHeight + wallThickness, portalWidth);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, wallThickness, portalWidth);
                    break;
                    
                case Direction.East:
                    portalX = worldX + roomWidth - wallThickness;
                    portalY = centerY - portalWidth / 2;
                    ClearRect(wallTilemap, portalX, portalY, wallThickness, portalWidth);
                    ClearRect(fillTilemap, portalX - portalHeight, portalY, portalHeight + wallThickness, portalWidth);
                    if (portalTilemap != null && portalTile != null)
                        FillRect(portalTilemap, portalTile, portalX, portalY, wallThickness, portalWidth);
                    break;
            }
        }
        
        /// <summary>
        /// 获取门户中心位置
        /// </summary>
        private Vector3 GetPortalPosition(int worldX, int worldY, int roomWidth, int roomHeight,
            int wallThickness, int portalWidth, Direction direction)
        {
            int centerX = worldX + roomWidth / 2;
            int centerY = worldY + roomHeight / 2;
            
            switch (direction)
            {
                case Direction.North:
                    return new Vector3(centerX, worldY + roomHeight - wallThickness / 2f, 0);
                case Direction.South:
                    return new Vector3(centerX, worldY + wallThickness / 2f, 0);
                case Direction.West:
                    return new Vector3(worldX + wallThickness / 2f, centerY, 0);
                case Direction.East:
                    return new Vector3(worldX + roomWidth - wallThickness / 2f, centerY, 0);
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
            var fillTilemap = LevelGenerator.TilemapLayers.FillLayer;
            var tile = LevelGenerator.TileSet.BlueTile;
            
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
                    
                    int platformLayers = roomHeight / (maxJumpHeight + minGap);
                    platformLayers = Mathf.Clamp(platformLayers, 1, 3);
                    
                    int lastY = worldY + wallThickness + 1;
                    
                    for (int layer = 0; layer < platformLayers; layer++)
                    {
                        int layerHeight = lastY + minGap + rng.Next(maxJumpHeight - minGap + 1);
                        if (layerHeight > worldY + roomHeight - 4) break;
                        
                        int pw = 4 + rng.Next(roomWidth / 3);
                        int px = worldX + 2 + rng.Next(roomWidth - pw - 4);
                        
                        FillRect(tilemap, tile, px, layerHeight, pw, 1);
                        ClearRect(fillTilemap, px - 1, layerHeight, pw + 2, 4);
                        
                        lastY = layerHeight;
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制特殊区域
        /// </summary>
        private void DrawSpecialAreas(int offsetX, int offsetY, LevelShape shape, RoomNode[,] roomGrid, int roomWidth, int roomHeight)
        {
            var specialTilemap = LevelGenerator.TilemapLayers.SpecialLayer;
            var platformTilemap = LevelGenerator.TilemapLayers.PlatformLayer;
            var fillTilemap = LevelGenerator.TilemapLayers.FillLayer;
            var yellowTile = LevelGenerator.TileSet.YellowTile;
            var blueTile = LevelGenerator.TileSet.BlueTile;
            
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
                    
                    if (room.Type == RoomType.Boss)
                    {
                        int bossAreaSize = 10;
                        FillRect(specialTilemap, yellowTile, centerX - bossAreaSize / 2, centerY - bossAreaSize / 2, bossAreaSize, bossAreaSize);
                        ClearRect(fillTilemap, worldX + 3, worldY + 3, roomWidth - 6, roomHeight - 6);
                        FillRect(platformTilemap, blueTile, worldX + 3, worldY + 3, roomWidth - 6, 1);
                    }
                }
            }
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
            
            // 绘制入口和出口标记
            if (ShowEntranceExitMarkers)
            {
                // 绘制入口标记（根据方向绘制箭头）
                Gizmos.color = EntranceMarkerColor;
                for (int i = 0; i < _entrancePositions.Count; i++)
                {
                    Vector3 pos = _entrancePositions[i];
                    Direction dir = i < _entranceDirections.Count ? _entranceDirections[i] : Direction.North;
                    
                    // 绘制实心圆形
                    Gizmos.DrawSphere(pos, MarkerSize);
                    
                    // 根据方向绘制箭头（指向房间内部）
                    DrawDirectionalArrow(pos, dir, true);
                }
                
                // 绘制出口标记（根据方向绘制箭头）
                Gizmos.color = ExitMarkerColor;
                for (int i = 0; i < _exitPositions.Count; i++)
                {
                    Vector3 pos = _exitPositions[i];
                    Direction dir = i < _exitDirections.Count ? _exitDirections[i] : Direction.South;
                    
                    // 绘制实心圆形
                    Gizmos.DrawSphere(pos, MarkerSize);
                    
                    // 根据方向绘制箭头（指向房间外部）
                    DrawDirectionalArrow(pos, dir, false);
                }
            }
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
    }
}
