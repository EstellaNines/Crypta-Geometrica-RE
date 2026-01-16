# 程序化多房间+走廊随机生成系统技术文档

> **项目**: Crypta Geometrica: RE  
> **版本**: 2.0  
> **更新日期**: 2026-01-16  
> **风格参考**: Spelunky (醉汉游走) + Dead Cells (走廊连接)

---

## 1. 系统概述

本系统实现了一套完整的程序化关卡生成方案，采用 **多网格蛇形布局 + A*走廊寻路 + 单网格醉汉游走** 的三层架构。

### 1.1 核心特性

| 特性 | 实现方式 | 参考游戏 |
| ---- | -------- | -------- |
| 多网格布局 | 随机化蛇形排列 | Dead Cells |
| 走廊连接 | A*寻路 + 曼哈顿路径 | Dead Cells |
| 单网格路径 | 醉汉游走算法 | Spelunky |
| 洞穴填充 | 细胞自动机 | Spelunky |
| 双层走廊 | 水平/垂直偏移避免重叠 | 原创 |

### 1.2 系统架构图

```text
┌─────────────────────────────────────────────────────────────────┐
│                    MultiGridLevelManager                        │
│  (多网格管理器 - 蛇形布局 + 走廊生成)                            │
├─────────────────────────────────────────────────────────────────┤
│  - GenerateRandomPositions()    # 随机化蛇形布局                 │
│  - GenerateCorridorPaths()      # A*走廊寻路                     │
│  - GenerateSingleGridAtOffset() # 调用单网格生成                 │
└─────────────────────────┬───────────────────────────────────────┘
                          │ 调用
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CorridorPathfinder                           │
│  (走廊寻路器 - A*算法 + 曼哈顿路径)                              │
├─────────────────────────────────────────────────────────────────┤
│  - FindPath()                   # 基础A*寻路                     │
│  - GenerateDeadCellsStylePath() # Dead Cells风格路径             │
│  - AStarManhattanSearch()       # 4方向曼哈顿A*                  │
│  - GenerateStairSegment()       # L/Z形阶梯生成                  │
└─────────────────────────────────────────────────────────────────┘
                          │ 依赖
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GrayboxLevelGenerator                        │
│  (单网格生成器 - 4×4宏观网格)                                    │
├─────────────────────────────────────────────────────────────────┤
│  - GenerateCriticalPath()    # 醉汉游走生成关键路径              │
│  - DrawOuterWalls()          # 绘制外围墙壁                      │
│  - DrawCaveFill()            # 洞穴填充 (细胞自动机)             │
│  - DrawPlatforms()           # 平台生成 (基于跳跃力)             │
└─────────────────────────────────────────────────────────────────┘
```


---

## 2. 文件结构

```text
1_Scripts/3_LevelGeneration/
├── Config/
│   ├── DifficultyConfig.cs       # 难度配置 ScriptableObject
│   └── GenerationSettings.cs     # 生成参数配置 ScriptableObject
├── Data/
│   ├── LevelShape.cs             # 关卡形状定义 (4×4位掩码)
│   ├── RoomNode.cs               # 房间节点数据结构
│   └── RoomType.cs               # 房间类型 + 方向枚举
├── Graybox/
│   ├── CorridorPathfinder.cs     # 走廊A*寻路器 ★核心
│   ├── GrayboxLevelGenerator.cs  # 单网格关卡生成器 ★核心
│   ├── GrayboxTilemapLayers.cs   # 6层Tilemap配置
│   ├── GrayboxTileSet.cs         # 瓦片集配置
│   └── MultiGridLevelManager.cs  # 多网格管理器 ★核心
└── MD_ProceduralRoomGeneration.md

Editor/3_LevelGeneration/
├── GrayboxLevelGeneratorEditor.cs  # 生成器Inspector
├── GrayboxPreviewEditor.cs         # 预览编辑器窗口
└── MultiGridMapEditorWindow.cs     # 多网格地图编辑器
```

---

## 3. 多网格布局系统 (MultiGridLevelManager)

### 3.1 蛇形布局算法

系统采用 **随机化蛇形布局** 确保走廊不交叉：

```text
布局示例 (4个网格):
┌─────┐     ┌─────┐
│  0  │────▶│  1  │
└─────┘     └──┬──┘
               │
┌─────┐     ┌──▼──┐
│  3  │◀────│  2  │
└─────┘     └─────┘
```

**算法流程**:

```csharp
private bool GenerateRandomPositions()
{
    // 1. 计算每行可放置的房间数量
    int maxRoomsPerRow = (LayoutAreaWidth - corridorSpace) / (gridWidth + corridorSpace);
    
    // 2. 随机决定每行实际房间数量
    List<int> rowRoomCounts = new List<int>();
    while (remainingRooms > 0)
    {
        int roomsInRow = _rng.Next(minInThisRow, maxInThisRow + 1);
        rowRoomCounts.Add(roomsInRow);
        remainingRooms -= roomsInRow;
    }
    
    // 3. 蛇形排列：偶数行从左到右，奇数行从右到左
    for (int row = 0; row < rowsNeeded; row++)
    {
        bool leftToRight = (row % 2 == 0);
        
        for (int col = 0; col < roomsInThisRow; col++)
        {
            int actualCol = leftToRight ? col : (roomsInThisRow - 1 - col);
            // 计算位置并添加随机偏移...
        }
    }
}
```

### 3.2 配置参数

| 参数 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| GridCount | int | 4 | 网格总数量 (1-8) |
| LayoutAreaWidth | int | 200 | 布局区域宽度 (瓦片) |
| LayoutAreaHeight | int | 200 | 布局区域高度 (瓦片) |
| MinGridSpacing | int | 16 | 网格最小间距 (瓦片) |
| PositionRandomOffset | int | 16 | 位置随机偏移 (瓦片) |
| BaseSeed | int | 0 | 随机种子 (0=随机) |
| MedianGridSpecialChance | float | 0.8 | 中位数网格特殊区域概率 |


---

## 4. 走廊寻路系统 (CorridorPathfinder)

### 4.1 Dead Cells 风格走廊

走廊采用 **纯曼哈顿路径**（只有水平和垂直线段），形成清晰的 L形/Z形 拐角：

```text
L形走廊:          Z形走廊:
┌───┐             ┌───┐
│   │             │   │
│   └────────     │   └────┐
│                 │        │
└─────────────    └────────┘
```

### 4.2 核心算法: GenerateDeadCellsStylePath

```csharp
private List<Vector2> GenerateDeadCellsStylePath(Vector2 start, Vector2 end)
{
    // 1. 检查起点/终点是否在安全区域
    Vector2 safeStart = startSafe ? start : ExtendPointOutsideRoom(start);
    Vector2 safeEnd = endSafe ? end : ExtendPointOutsideRoom(end);
    
    // 2. 尝试生成阶梯状路径 (优先)
    List<Vector2> middlePath = GenerateExternalStairPath(safeStart, safeEnd);
    
    // 3. 如果阶梯路径无效，使用A*寻路
    if (middlePath.Count == 0 || !ValidatePathSegments(...))
    {
        middlePath = GenerateAStarExternalPath(safeStart, safeEnd);
    }
    
    // 4. 如果A*也失败，尝试简单L形路径
    if (middlePath.Count == 0)
    {
        middlePath = GenerateSimpleLPath(safeStart, safeEnd);
    }
    
    // 5. 强制转换为曼哈顿路径 (消除所有斜线)
    path = EnforceManhattanPathPreserveEndpoints(path, originalStart, originalEnd);
    
    return path;
}
```

### 4.3 阶梯生成策略 (GenerateStairSegment)

```csharp
private List<Vector2> GenerateStairSegment(Vector2 start, Vector2 end)
{
    // 方案1：L形 - 先水平后垂直
    Vector2 corner1 = new Vector2(end.x, start.y);
    if (IsPointInSafeZone(corner1) && IsLineInSafeZone(start, corner1) && IsLineInSafeZone(corner1, end))
    {
        return new List<Vector2> { corner1 };
    }
    
    // 方案2：L形 - 先垂直后水平
    Vector2 corner2 = new Vector2(start.x, end.y);
    if (IsPointInSafeZone(corner2) && ...)
    {
        return new List<Vector2> { corner2 };
    }
    
    // 方案3：Z形 - 水平-垂直-水平
    float midX = (start.x + end.x) / 2f;
    Vector2 z1 = new Vector2(midX, start.y);
    Vector2 z2 = new Vector2(midX, end.y);
    if (IsPointInSafeZone(z1) && IsPointInSafeZone(z2) && ...)
    {
        return new List<Vector2> { z1, z2 };
    }
    
    // 方案4：Z形 - 垂直-水平-垂直
    // ...
    
    // 方案5：偏移绕行 (向外20-60单位)
    // ...
}
```

### 4.4 双层走廊避免重叠

```csharp
private float CalculateCorridorLayerOffset(Vector2 start, Vector2 end, 
    List<Rect> existingBounds, bool isMainlyHorizontal)
{
    // 检查与已有走廊的重叠数量
    int overlapCount = 0;
    foreach (Rect existing in existingBounds)
    {
        if (currentBound.Overlaps(existing))
            overlapCount++;
    }
    
    if (overlapCount == 0) return 0f;
    
    float baseOffset = 8f;
    if (isMainlyHorizontal)
    {
        // 水平走廊：交替上下偏移
        return (overlapCount % 2 == 0) ? baseOffset : -baseOffset;
    }
    else
    {
        // 垂直走廊：交替左右偏移
        return (overlapCount % 2 == 0) ? baseOffset : -baseOffset;
    }
}
```

### 4.5 A* 曼哈顿寻路

```csharp
private List<Vector2Int> AStarManhattanSearch(Vector2Int start, Vector2Int goal)
{
    // 只使用4方向邻居（无对角线）
    foreach (Vector2Int neighborPos in Get4DirectionNeighbors(current.Position))
    {
        // 曼哈顿距离作为启发函数
        neighbor.HCost = ManhattanDistance(neighborPos, goal);
        // ...
    }
}

private List<Vector2Int> Get4DirectionNeighbors(Vector2Int pos)
{
    return new List<Vector2Int>
    {
        new Vector2Int(pos.x, pos.y + 1),  // 上
        new Vector2Int(pos.x, pos.y - 1),  // 下
        new Vector2Int(pos.x - 1, pos.y),  // 左
        new Vector2Int(pos.x + 1, pos.y)   // 右
    };
}
```


---

## 5. 单网格生成系统 (GrayboxLevelGenerator)

### 5.1 醉汉游走算法 (Spelunky风格)

从顶排随机入口开始，每层水平游走后向下：

```text
生成示例:
┌───┬───┬───┬───┐
│ S │ → │   │   │  S=Start (入口)
├───┼───┼───┼───┤
│   │ ↓ │ ← │   │  →/←/↓ = 路径方向
├───┼───┼───┼───┤
│   │   │ ↓ │   │
├───┼───┼───┼───┤
│   │   │ E │   │  E=Exit (出口)
└───┴───┴───┴───┘
```

**算法实现**:

```csharp
private void GenerateCriticalPath()
{
    // 1. 从顶排随机选择入口
    int startX = _rng.Next(LevelShape.GridWidth);  // 0-3
    Vector2Int current = new Vector2Int(startX, 0);
    
    // 2. 每层水平游走后向下
    for (int row = 0; row < LevelShape.GridHeight; row++)
    {
        // 随机水平方向和步数
        int horizontalDirection = _rng.Next(2) == 0 ? -1 : 1;
        int horizontalSteps = _rng.Next(1, 4);  // 1-3步
        
        // 水平游走
        for (int step = 0; step < horizontalSteps; step++)
        {
            int nextX = current.x + horizontalDirection;
            
            // 撞墙反向
            if (nextX < 0 || nextX >= LevelShape.GridWidth)
            {
                horizontalDirection = -horizontalDirection;
                nextX = current.x + horizontalDirection;
            }
            
            // 添加连接
            Direction dir = GetDirection(current, next);
            _roomGrid[current.x, current.y].AddConnection(dir);
            _roomGrid[next.x, next.y].AddConnection(dir.Opposite());
            
            current = next;
        }
        
        // 向下移动
        if (row < LevelShape.GridHeight - 1)
        {
            Vector2Int next = new Vector2Int(current.x, current.y + 1);
            // 添加垂直连接...
            current = next;
        }
    }
    
    // 3. 设置房间类型
    _roomGrid[entrance.x, entrance.y].Type = RoomType.Start;
    _roomGrid[exit.x, exit.y].Type = RoomType.Exit;
    _roomGrid[path[^2].x, path[^2].y].Type = RoomType.Boss;  // 出口前一个
}
```

### 5.2 洞穴填充 (细胞自动机)

```csharp
private void DrawCaveFill()
{
    // 1. 添加边界墙壁 (12瓦片厚)
    AddBoundaryWalls();
    
    // 2. 生成整体连贯填充
    GenerateConnectedCaveFill();
    
    // 3. 清除房间连接通道
    ClearAllConnectionPassages();
    
    // 4. 雕刻曲折行走通道
    CarveWindingPath();
}

private void GenerateConnectedCaveFill()
{
    // 初始化随机填充 (边缘加权)
    for (int y = 0; y < fillHeight; y++)
    {
        for (int x = 0; x < fillWidth; x++)
        {
            int distToEdge = Mathf.Min(x, y, fillWidth - 1 - x, fillHeight - 1 - y);
            float edgeFactor = (distToEdge < 6) ? 2.0f : 1.0f;
            
            // 底部更容易填充 (形成地面)
            if (y < fillHeight / 4) edgeFactor *= 1.8f;
            
            cave[x, y] = _rng.NextDouble() < FillDensity * edgeFactor;
        }
    }
    
    // 细胞自动机平滑 (4-5规则)
    for (int i = 0; i < SmoothIterations + 3; i++)
    {
        cave = SmoothCave(cave, fillWidth, fillHeight);
    }
}

private bool[,] SmoothCave(bool[,] cave, int width, int height)
{
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int neighbors = CountNeighbors(cave, x, y, width, height);
            
            // 4-5规则: >4邻居填充, <4邻居清空
            if (neighbors > 4) newCave[x, y] = true;
            else if (neighbors < 4) newCave[x, y] = false;
            else newCave[x, y] = cave[x, y];
        }
    }
    return newCave;
}
```

### 5.3 平台生成 (基于跳跃力)

```csharp
private void DrawPlatforms()
{
    // 基于跳跃力计算最大可达高度
    int maxJumpHeight = Mathf.Min(MaxPlatformHeightDiff, 
        Mathf.FloorToInt(PlayerJumpForce * 0.5f));
    
    // 计算平台层数
    int platformLayers = RoomHeight / (maxJumpHeight + MinPlatformGap);
    platformLayers = Mathf.Clamp(platformLayers, 1, 3);
    
    int lastY = worldY + WallThickness + 1;
    
    for (int layer = 0; layer < platformLayers; layer++)
    {
        // 确保玩家能跳上去
        int layerHeight = lastY + MinPlatformGap + 
            _rng.Next(maxJumpHeight - MinPlatformGap + 1);
        
        // 随机平台位置和宽度
        int pw = 4 + _rng.Next(RoomWidth / 3);
        int px = worldX + 2 + _rng.Next(RoomWidth - pw - 4);
        
        // 绘制平台并清除上方填充
        FillRect(PlatformLayer, BlueTile, px, layerHeight, pw, 1);
        ClearRect(FillLayer, px - 1, layerHeight, pw + 2, 4);
        
        lastY = layerHeight;
    }
}
```


---

## 6. 数据结构详解

### 6.1 LevelShape - 关卡形状 (4×4位掩码)

```csharp
public class LevelShape
{
    public int[,] OccupancyMask = new int[4, 4];  // 1=有效, 0=无效
    public const int GridWidth = 4;
    public const int GridHeight = 4;
    
    // 从字符串创建
    public static LevelShape FromString(string pattern)
    {
        // 格式: "1111,1111,1111,1111" (逗号分隔行)
        var rows = pattern.Replace(" ", "").Split(',');
        for (int y = 0; y < GridHeight && y < rows.Length; y++)
        {
            for (int x = 0; x < GridWidth && x < rows[y].Length; x++)
            {
                shape.OccupancyMask[x, y] = rows[y][x] == '1' ? 1 : 0;
            }
        }
    }
    
    // 核心方法
    public bool IsValidCell(int x, int y);           // 检查格子是否有效
    public List<Vector2Int> GetValidCells();         // 获取所有有效格子
    public List<Vector2Int> GetValidNeighbors(x, y); // 获取相邻有效格子
    public List<Vector2Int> GetTopRowCells();        // 获取顶部行有效格子
    public List<Vector2Int> GetBottomRowCells();     // 获取底部行有效格子
}
```

**预设形状库**:

| 名称 | 模式字符串 | 可视化 |
| ---- | ---------- | ------ |
| FullSquare | 1111,1111,1111,1111 | ■■■■ / ■■■■ / ■■■■ / ■■■■ |
| LShape | 1000,1000,1111,1111 | ■□□□ / ■□□□ / ■■■■ / ■■■■ |
| TShape | 1111,0110,0110,0110 | ■■■■ / □■■□ / □■■□ / □■■□ |
| CrossShape | 0110,1111,1111,0110 | □■■□ / ■■■■ / ■■■■ / □■■□ |
| ZShape | 1110,0110,0110,0111 | ■■■□ / □■■□ / □■■□ / □■■■ |

### 6.2 RoomNode - 房间节点

```csharp
public class RoomNode
{
    // 位置标识
    public Vector2Int GridCoordinates;  // 宏观网格坐标 (0-3, 0-3)
    
    // 类型与状态
    public RoomType Type = RoomType.None;
    public bool IsCriticalPath;
    public bool IsGenerated;
    
    // 连通性 (4-bit掩码: N=1, E=2, S=4, W=8)
    public int ConnectionMask;
    
    // 软边界核心 - 活跃区域
    public RectInt ActiveZone;
    
    // 游戏性数据
    public float DifficultyRating;      // 难度系数 0.0-1.0
    public int EnemyCount;
    public List<Vector2Int> EnemySpawnPoints;
    
    // 连通性方法
    public bool HasConnection(Direction direction)
    {
        int mask = direction.ToMask();
        return (ConnectionMask & mask) != 0;
    }
    
    public void AddConnection(Direction direction)
    {
        ConnectionMask |= direction.ToMask();
    }
}
```

### 6.3 RoomType - 房间类型枚举

```csharp
public enum RoomType
{
    None = 0,      // 无效/未分配
    Start = 1,     // 起点房间 (玩家初始位置)
    Exit = 2,      // 终点房间 (关卡出口)
    LR = 3,        // 左右贯通 (水平移动通道)
    Drop = 4,      // 下落房间 (底部开口)
    Landing = 5,   // 着陆房间 (顶部开口)
    Side = 6,      // 侧室 (非关键路径)
    Shop = 7,      // 商店房间
    Abyss = 8,     // 深渊竖井 (连续垂直下落)
    Boss = 9       // Boss房间 (1.3倍尺寸, 最高难度)
}
```

### 6.4 Direction - 方向枚举

```csharp
public enum Direction
{
    North = 0,  // 上, 掩码: 1 (0b0001)
    East = 1,   // 右, 掩码: 2 (0b0010)
    South = 2,  // 下, 掩码: 4 (0b0100)
    West = 3    // 左, 掩码: 8 (0b1000)
}

public static class DirectionExtensions
{
    public static int ToMask(this Direction direction)
    {
        return 1 << (int)direction;
    }
    
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
```


---

## 7. Tilemap 层级系统

### 7.1 六层 Tilemap 结构

```csharp
public class GrayboxTilemapLayers : MonoBehaviour
{
    public Tilemap WallLayer;      // 外围墙壁 (红色)
    public Tilemap FillLayer;      // 洞穴填充 (橙色)
    public Tilemap PlatformLayer;  // 可站立平台 (蓝色)
    public Tilemap EntranceLayer;  // 入口标记 (绿色)
    public Tilemap ExitLayer;      // 出口标记 (黑色)
    public Tilemap SpecialLayer;   // Boss/Shop区域 (黄色)
}
```

| 层级 | 瓦片颜色 | 功能 | Sorting Order |
| ---- | -------- | ---- | ------------- |
| WallLayer | 红色 | 4×4网格外围边界，不可穿越 | 0 |
| FillLayer | 橙色 | 洞穴内部随机填充，细胞自动机生成 | 1 |
| PlatformLayer | 蓝色 | 可站立平台，基于跳跃力计算高度 | 2 |
| EntranceLayer | 绿色 | 房间入口标记，玩家进入点 | 3 |
| ExitLayer | 黑色 | 房间出口标记，关卡结束点 | 4 |
| SpecialLayer | 黄色 | Boss/Shop特殊区域标记 | 5 |

### 7.2 GrayboxTileSet - 瓦片配置

```csharp
public class GrayboxTileSet : MonoBehaviour
{
    public TileBase RedTile;     // 外围墙壁
    public TileBase OrangeTile;  // 洞穴填充
    public TileBase BlueTile;    // 平台
    public TileBase GreenTile;   // 入口
    public TileBase BlackTile;   // 出口
    public TileBase YellowTile;  // 特殊区域
    public TileBase WhiteTile;   // 预留
    public TileBase PurpleTile;  // 预留
    public TileBase PinkTile;    // 预留
}
```

---

## 8. 入口/出口智能方向选择

### 8.1 方向选择算法

系统根据房间在网格中的位置自动选择最佳入口/出口方向：

```csharp
private Direction GetBestEntranceDirection(int gx, int gy, LevelShape shape)
{
    // 收集所有可用的外围方向（没有相邻房间的方向）
    bool canNorth = (gy == 0) || !shape.IsValidCell(gx, gy - 1);
    bool canSouth = (gy == LevelShape.GridHeight - 1) || !shape.IsValidCell(gx, gy + 1);
    bool canWest = (gx == 0) || !shape.IsValidCell(gx - 1, gy);
    bool canEast = (gx == LevelShape.GridWidth - 1) || !shape.IsValidCell(gx + 1, gy);
    
    // 入口优先级：左 > 上 > 下 > 右
    if (canWest) return Direction.West;
    if (canNorth) return Direction.North;
    if (canSouth) return Direction.South;
    if (canEast) return Direction.East;
    
    return Direction.North;  // 默认
}

private Direction GetBestExitDirection(int gx, int gy, LevelShape shape)
{
    // 出口优先级：右 > 下 > 上 > 左 (与入口相反)
    if (canEast) return Direction.East;
    if (canSouth) return Direction.South;
    if (canNorth) return Direction.North;
    if (canWest) return Direction.West;
    
    return Direction.South;  // 默认
}
```

### 8.2 门户位置计算 (向外延伸)

```csharp
private Vector3 GetPortalPosition(int worldX, int worldY, int roomWidth, int roomHeight,
    int wallThickness, int portalWidth, Direction direction)
{
    int centerX = worldX + roomWidth / 2;
    int centerY = worldY + roomHeight / 2;
    
    // 向外延伸的距离（确保走廊起点在房间外部）
    float extendDistance = wallThickness + 3f;
    
    switch (direction)
    {
        case Direction.North:
            return new Vector3(centerX, worldY + roomHeight + extendDistance, 0);
        case Direction.South:
            return new Vector3(centerX, worldY - extendDistance, 0);
        case Direction.West:
            return new Vector3(worldX - extendDistance, centerY, 0);
        case Direction.East:
            return new Vector3(worldX + roomWidth + extendDistance, centerY, 0);
    }
}
```


---

## 9. 配置系统

### 9.1 GenerationSettings (生成参数)

```csharp
[CreateAssetMenu(menuName = "CryptaGeometrica/PCG/GenerationSettings")]
public class GenerationSettings : ScriptableObject
{
    // 网格单元格尺寸
    public int CellWidth = 32;
    public int CellHeight = 32;
    
    // 软边界设置
    public float ShrinkRatio = 0.7f;        // 活跃区域缩放比例
    public int MaxActiveZoneOffset = 4;     // 最大随机偏移
    
    // 走廊设置
    public int CorridorMinWidth = 4;
    public int CorridorMaxWidth = 6;
    
    // 平台生成
    public int PlatformMinGap = 3;
    public int PlatformMaxGap = 6;
    public int MaxJumpHeight = 4;
    public int MaxJumpDistance = 6;
    
    // Boss房间
    public float BossRoomScale = 1.3f;
}
```

### 9.2 DifficultyConfig (难度配置)

```csharp
[CreateAssetMenu(menuName = "CryptaGeometrica/PCG/DifficultyConfig")]
public class DifficultyConfig : ScriptableObject
{
    // 难度递增
    public float BaseDifficulty = 0.2f;
    public float DifficultyIncrement = 0.1f;
    public float MaxDifficulty = 1.0f;
    
    // 敌人数量
    public int BaseEnemyCount = 2;
    public int EnemyPerDifficultyStep = 1;
    public int MaxEnemiesPerRoom = 8;
    
    // 房间类型修正
    public int CriticalPathEnemyBonus = 1;
    public int SideRoomEnemyReduction = 1;
    
    // 计算方法
    public float CalculateLevelDifficulty(int levelIndex)
    {
        return Mathf.Min(BaseDifficulty + levelIndex * DifficultyIncrement, MaxDifficulty);
    }
    
    public int CalculateEnemyCount(float roomDifficulty, RoomType type, bool isCriticalPath)
    {
        int baseCount = BaseEnemyCount + Mathf.FloorToInt(roomDifficulty * EnemyPerDifficultyStep);
        
        if (isCriticalPath) baseCount += CriticalPathEnemyBonus;
        if (type == RoomType.Side) baseCount -= SideRoomEnemyReduction;
        if (type == RoomType.Boss) baseCount *= 2;
        
        return Mathf.Clamp(baseCount, 0, MaxEnemiesPerRoom);
    }
}
```

---

## 10. 编辑器工具

### 10.1 GrayboxLevelGeneratorEditor

自定义 Inspector 功能：
- 4×4 形状可视化编辑器（点击切换格子）
- 预设形状快速加载按钮
- 一键生成/清除关卡
- 瓦片颜色图例显示
- 自动创建 Tilemap 层级结构

### 10.2 MultiGridMapEditorWindow

独立编辑器窗口 (`Window/Crypta Geometrica/Multi-Grid Map Editor`)：
- 多网格布局可视化
- 走廊路径预览
- 入口/出口标记显示
- 实时参数调整

### 10.3 Gizmos 调试显示

```csharp
private void OnDrawGizmos()
{
    // 绘制布局区域边界 (黄色)
    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
    Gizmos.DrawWireCube(layoutCenter, layoutSize);
    
    // 绘制已生成的网格边界 (绿色)
    Gizmos.color = GridBoundsColor;
    foreach (var bounds in _placedGridBounds)
    {
        Gizmos.DrawWireCube(center, size);
    }
    
    // 绘制入口标记 (绿色球体 + 箭头)
    Gizmos.color = EntranceMarkerColor;
    for (int i = 0; i < _entrancePositions.Count; i++)
    {
        Gizmos.DrawSphere(pos, MarkerSize);
        DrawDirectionalArrow(pos, dir, true);  // 指向内部
    }
    
    // 绘制出口标记 (黑色球体 + 箭头)
    Gizmos.color = ExitMarkerColor;
    for (int i = 0; i < _exitPositions.Count; i++)
    {
        Gizmos.DrawSphere(pos, MarkerSize);
        DrawDirectionalArrow(pos, dir, false);  // 指向外部
    }
    
    // 绘制走廊路径 (橙色线段)
    Gizmos.color = CorridorPathColor;
    foreach (List<Vector2> path in _corridorPaths)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(start, end);
        }
    }
}
```


---

## 11. 使用指南

### 11.1 快速开始

```csharp
// 1. 获取管理器引用
var multiGrid = GetComponent<MultiGridLevelManager>();

// 2. 配置参数
multiGrid.GridCount = 4;
multiGrid.LayoutAreaWidth = 200;
multiGrid.LayoutAreaHeight = 200;
multiGrid.BaseSeed = 12345;  // 固定种子可复现

// 3. 生成多网格关卡
multiGrid.GenerateMultiGridLevel();

// 4. 获取入口出口位置 (用于玩家传送)
List<Vector3> entrances = multiGrid.GetEntrancePositions();
List<Vector3> exits = multiGrid.GetExitPositions();
List<Direction> entranceDirs = multiGrid.GetEntranceDirections();
List<Direction> exitDirs = multiGrid.GetExitDirections();

// 5. 获取走廊路径 (用于敌人巡逻等)
List<List<Vector2>> corridors = multiGrid.GetCorridorPaths();
```

### 11.2 单网格生成

```csharp
var generator = GetComponent<GrayboxLevelGenerator>();

// 使用预设形状
generator.GenerateLevel(LevelShapePresets.CrossShape);

// 使用自定义形状
var customShape = LevelShape.FromString("1100,1110,0111,0011");
generator.GenerateLevel(customShape);
```

---

## 12. 扩展建议

### 12.1 待实现功能

- [ ] WFC (波函数坍缩) 微观瓦片生成
- [ ] 物理可达性验证 (A* 路径检测)
- [ ] 敌人生成点自动计算
- [ ] 宝箱/道具放置逻辑
- [ ] 关卡主题切换 (不同瓦片集)
- [ ] 走廊内部装饰生成

### 12.2 性能优化方向

- Tilemap 批处理 (SetTilesBlock)
- 异步生成 (协程分帧)
- 对象池复用 RoomNode
- 走廊路径缓存

---

## 13. 版本记录

| 版本 | 日期 | 说明 |
| ---- | ---- | ---- |
| 1.0 | 2026-01-15 | 初始文档 |
| 2.0 | 2026-01-16 | 完整重写，新增走廊寻路、多网格布局详解 |

---

## 14. 参考资料

- Spelunky 关卡生成分析 (醉汉游走)
- Dead Cells 走廊连接系统 (曼哈顿路径)
- 细胞自动机洞穴生成
- Unity Tilemap 最佳实践
- A* 寻路算法
