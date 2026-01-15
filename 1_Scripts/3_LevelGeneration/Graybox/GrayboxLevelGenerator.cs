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
        [Tooltip("填充密度 (0-1)")]
        [Range(0f, 0.6f)]
        public float FillDensity = 0.35f;
        
        [Tooltip("平滑迭代次数")]
        [Range(0, 5)]
        public int SmoothIterations = 3;
        
        [Tooltip("随机种子 (0=随机)")]
        public int RandomSeed = 0;
        
        [Header("出入口设置")]
        [Tooltip("出入口通道宽度")]
        [Range(4, 8)]
        public int EntranceWidth = 6;
        
        [Tooltip("出入口通道高度")]
        [Range(4, 8)]
        public int EntranceHeight = 5;
        
        [Header("平台设置")]
        [Tooltip("玩家跳跃力")]
        public float PlayerJumpForce = 8f;
        
        [Tooltip("平台最大高度差(基于跳跃力计算)")]
        [Range(2, 6)]
        public int MaxPlatformHeightDiff = 4;
        
        [Tooltip("平台最小间距")]
        [Range(2, 6)]
        public int MinPlatformGap = 3;
        
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
                        FillRect(TilemapLayers.WallLayer, TileSet.RedTile,
                            worldX, worldY + RoomHeight - WallThickness, RoomWidth, WallThickness);
                    }
                    
                    // 南边（下方）
                    if (gy == LevelShape.GridHeight - 1 || !_currentShape.IsValidCell(gx, gy + 1))
                    {
                        FillRect(TilemapLayers.WallLayer, TileSet.RedTile,
                            worldX, worldY, RoomWidth, WallThickness);
                    }
                    
                    // 西边（左边）
                    if (gx == 0 || !_currentShape.IsValidCell(gx - 1, gy))
                    {
                        FillRect(TilemapLayers.WallLayer, TileSet.RedTile,
                            worldX, worldY, WallThickness, RoomHeight);
                    }
                    
                    // 东边（右边）
                    if (gx == LevelShape.GridWidth - 1 || !_currentShape.IsValidCell(gx + 1, gy))
                    {
                        FillRect(TilemapLayers.WallLayer, TileSet.RedTile,
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
                        FillRect(TilemapLayers.FillLayer, TileSet.OrangeTile,
                            worldX, worldY + RoomHeight - baseBoundaryWidth, RoomWidth, baseBoundaryWidth);
                    }
                    // 南边
                    if (gy == LevelShape.GridHeight - 1 || !_currentShape.IsValidCell(gx, gy + 1))
                    {
                        FillRect(TilemapLayers.FillLayer, TileSet.OrangeTile,
                            worldX, worldY, RoomWidth, baseBoundaryWidth);
                    }
                    // 西边
                    if (gx == 0 || !_currentShape.IsValidCell(gx - 1, gy))
                    {
                        FillRect(TilemapLayers.FillLayer, TileSet.OrangeTile,
                            worldX, worldY, baseBoundaryWidth, RoomHeight);
                    }
                    // 东边
                    if (gx == LevelShape.GridWidth - 1 || !_currentShape.IsValidCell(gx + 1, gy))
                    {
                        FillRect(TilemapLayers.FillLayer, TileSet.OrangeTile,
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
                    
                    // 边缘更容易填充
                    int distToEdge = Mathf.Min(x, y, fillWidth - 1 - x, fillHeight - 1 - y);
                    float edgeFactor = (distToEdge < 6) ? 2.0f : 1.0f;
                    
                    // 底部更容易填充（形成地面）
                    if (y < fillHeight / 4)
                    {
                        edgeFactor *= 1.8f;
                    }
                    
                    cave[x, y] = _rng.NextDouble() < FillDensity * edgeFactor;
                }
            }
            
            // 细胞自动机平滑 - 多次迭代确保连贯
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
                                TileSet.OrangeTile);
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
                        // 入口：选择靠近外墙的最佳方向
                        EntranceDirection dir = GetBestEntranceDirection(gx, gy);
                        DrawEntrance(worldX, worldY, centerX, centerY, dir);
                    }
                    else if (room.Type == RoomType.Exit)
                    {
                        // 出口：选择靠近外墙的最佳方向
                        EntranceDirection dir = GetBestExitDirection(gx, gy);
                        DrawExit(worldX, worldY, centerX, centerY, dir);
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
            ClearRect(TilemapLayers.WallLayer, entranceX, entranceY, EntranceWidth, WallThickness + 1);
            FillRect(TilemapLayers.EntranceLayer, TileSet.GreenTile, entranceX, entranceY, EntranceWidth, WallThickness);
            
            // 清除入口下方区域的填充
            ClearRect(TilemapLayers.FillLayer, entranceX - 2, entranceY - EntranceHeight - 2, EntranceWidth + 4, EntranceHeight + 4);
            
            // 入口下方的落脚平台
            FillRect(TilemapLayers.PlatformLayer, TileSet.BlueTile, entranceX - 1, entranceY - EntranceHeight - 1, EntranceWidth + 2, 1);
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
            ClearRect(TilemapLayers.WallLayer, exitX, exitY, EntranceWidth, WallThickness + 1);
            FillRect(TilemapLayers.ExitLayer, TileSet.BlackTile, exitX, exitY, EntranceWidth, WallThickness);
            
            // 清除出口上方区域的填充
            ClearRect(TilemapLayers.FillLayer, exitX - 2, exitY + WallThickness, EntranceWidth + 4, EntranceHeight + 2);
            
            // 出口上方的平台
            FillRect(TilemapLayers.PlatformLayer, TileSet.BlueTile, exitX - 1, exitY + WallThickness, EntranceWidth + 2, 1);
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
                        FillRect(TilemapLayers.PlatformLayer, TileSet.BlueTile,
                            px, layerHeight, pw, 1);
                        
                        // 清除平台上方的填充（给玩家站立空间）
                        ClearRect(TilemapLayers.FillLayer,
                            px - 1, layerHeight, pw + 2, 4);
                        
                        lastY = layerHeight;
                    }
                }
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
                        
                        // 特殊区域标记(商店区域)
                        FillRect(TilemapLayers.SpecialLayer, TileSet.YellowTile,
                            centerX - shopAreaSize / 2, centerY - shopAreaSize / 2, 
                            shopAreaSize, shopAreaSize);
                        
                        // 清除商店中央区域的填充
                        ClearRect(TilemapLayers.FillLayer,
                            centerX - shopAreaSize / 2 - 1, centerY - shopAreaSize / 2 - 1, 
                            shopAreaSize + 2, shopAreaSize + 2);
                        
                        // 商店地面平台
                        FillRect(TilemapLayers.PlatformLayer, TileSet.BlueTile,
                            centerX - shopAreaSize / 2 - 1, centerY - shopAreaSize / 2 - 1, 
                            shopAreaSize + 2, 1);
                        
                        // 保持商店与主路径的通道
                        ClearRoomConnectionPassages(worldX, worldY, room);
                    }
                    else if (room.Type == RoomType.Boss)
                    {
                        // Boss房间 - 更大的空间
                        int bossAreaSize = 10;
                        
                        // 特殊区域标记
                        FillRect(TilemapLayers.SpecialLayer, TileSet.YellowTile,
                            centerX - bossAreaSize / 2, centerY - bossAreaSize / 2, 
                            bossAreaSize, bossAreaSize);
                        
                        // 清除Boss房间中央区域的填充
                        ClearRect(TilemapLayers.FillLayer,
                            worldX + 3, worldY + 3, RoomWidth - 6, RoomHeight - 6);
                        
                        // Boss房间地面
                        FillRect(TilemapLayers.PlatformLayer, TileSet.BlueTile,
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
