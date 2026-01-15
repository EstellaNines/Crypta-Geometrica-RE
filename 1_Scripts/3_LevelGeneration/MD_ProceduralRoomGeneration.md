# 程序化随机房间生成系统技术文档

## 1. 系统概述

本系统实现了一套基于 **Spelunky 风格** 的程序化关卡生成方案，采用 **4×4 宏观网格 + 微观房间填充** 的双层架构，支持多种关卡形状、洞穴生成、平台布局和多网格关卡管理。

### 1.1 设计目标

- 生成具有探索性和挑战性的随机关卡
- 保证关卡可通行性（从入口到出口的关键路径）
- 支持多种预设形状和自定义形状
- 提供灵活的难度配置系统
- 支持多网格关卡布局

### 1.2 核心特性

| 特性 | 说明 |
|------|------|
| 醉汉游走算法 | 生成自然的关键路径 |
| 细胞自动机 | 生成有机的洞穴填充 |
| 多层 Tilemap | 分离墙壁、填充、平台、入口、出口、特殊区域 |
| 软边界系统 | 房间活跃区域随机偏移，增加变化性 |
| 物理验证 | 基于跳跃力计算平台高度差 |

---

## 2. 系统架构

### 2.1 文件结构

```
1_Scripts/3_LevelGeneration/
├── Config/
│   ├── DifficultyConfig.cs       # 难度配置 ScriptableObject
│   └── GenerationSettings.cs     # 生成参数配置 ScriptableObject
├── Data/
│   ├── LevelShape.cs             # 关卡形状定义 (4×4位掩码)
│   ├── RoomNode.cs               # 房间节点数据结构
│   └── RoomType.cs               # 房间类型枚举 + 方向枚举
├── Graybox/
│   ├── GrayboxGridPreview.cs     # 网格预览器
│   ├── GrayboxLevelGenerator.cs  # 核心关卡生成器
│   ├── GrayboxRoomTemplates.cs   # 房间模板生成器
│   ├── GrayboxTilemapLayers.cs   # Tilemap层配置
│   └── MultiGridLevelManager.cs  # 多网格关卡管理器
└── MD_ProceduralRoomGeneration.md # 本文档

Editor/3_LevelGeneration/
├── GrayboxLevelGeneratorEditor.cs  # 生成器自定义Inspector
└── GrayboxPreviewEditor.cs         # 预览编辑器窗口
```

### 2.2 核心类关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                    MultiGridLevelManager                        │
│  (多网格管理器 - 生成多个独立4×4网格)                            │
└─────────────────────────┬───────────────────────────────────────┘
                          │ 调用
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GrayboxLevelGenerator                        │
│  (核心生成器 - 单个4×4网格的完整生成逻辑)                        │
├─────────────────────────────────────────────────────────────────┤
│  - GenerateCriticalPath()    # 醉汉游走生成关键路径              │
│  - DrawOuterWalls()          # 绘制外围墙壁                      │
│  - DrawCaveFill()            # 洞穴填充 (细胞自动机)             │
│  - DrawRoomConnections()     # 房间连接通道                      │
│  - DrawEntranceAndExit()     # 入口出口                          │
│  - DrawPlatforms()           # 平台生成                          │
│  - DrawSpecialAreas()        # 特殊区域 (Boss/Shop)              │
└─────────────────────────┬───────────────────────────────────────┘
                          │ 依赖
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
│ GrayboxTilemap  │ │ GrayboxTile │ │ LevelShape      │
│ Layers          │ │ Set         │ │ + RoomNode      │
│ (6层Tilemap)    │ │ (7色瓦片)   │ │ (数据结构)      │
└─────────────────┘ └─────────────┘ └─────────────────┘
```

---

## 3. 数据结构详解

### 3.1 LevelShape - 关卡形状

使用 4×4 位掩码矩阵定义关卡的有效区域：

```csharp
public class LevelShape
{
    public int[,] OccupancyMask = new int[4, 4];  // 1=有效, 0=无效
    public const int GridWidth = 4;
    public const int GridHeight = 4;
    
    // 从字符串创建: "1111,1111,1111,1111"
    public static LevelShape FromString(string pattern);
    
    // 获取有效格子列表
    public List<Vector2Int> GetValidCells();
    
    // 获取相邻有效格子
    public List<Vector2Int> GetValidNeighbors(int x, int y);
}
```

**预设形状库 (LevelShapePresets)**：

| 形状名称 | 可视化 | 有效格子数 |
|----------|--------|------------|
| FullSquare | ■■■■<br>■■■■<br>■■■■<br>■■■■ | 16 |
| LShape | ■□□□<br>■□□□<br>■■■■<br>■■■■ | 10 |
| TShape | ■■■■<br>□■■□<br>□■■□<br>□■■□ | 10 |
| CrossShape | □■■□<br>■■■■<br>■■■■<br>□■■□ | 12 |
| ZShape | ■■■□<br>□■■□<br>□■■□<br>□■■■ | 10 |

### 3.2 RoomNode - 房间节点

每个 RoomNode 代表 4×4 网格中的一个单元格：

```csharp
public class RoomNode
{
    // 位置
    public Vector2Int GridCoordinates;
    
    // 类型与状态
    public RoomType Type;
    public bool IsCriticalPath;
    public bool IsGenerated;
    
    // 连通性 (4-bit掩码: N=1, E=2, S=4, W=8)
    public int ConnectionMask;
    
    // 软边界核心 - 活跃区域
    public RectInt ActiveZone;
    
    // 游戏性数据
    public float DifficultyRating;
    public int EnemyCount;
    public List<Vector2Int> EnemySpawnPoints;
    
    // 连通性方法
    public bool HasConnection(Direction direction);
    public void AddConnection(Direction direction);
    public List<Direction> GetConnections();
}
```

### 3.3 RoomType - 房间类型

```csharp
public enum RoomType
{
    None = 0,      // 无效房间
    Start = 1,     // 起点房间
    Exit = 2,      // 终点房间
    LR = 3,        // 左右贯通
    Drop = 4,      // 下落房间 (底部开口)
    Landing = 5,   // 着陆房间 (顶部开口)
    Side = 6,      // 侧室 (非关键路径)
    Shop = 7,      // 商店房间
    Abyss = 8,     // 深渊竖井
    Boss = 9       // Boss房间 (1.3倍尺寸)
}
```

### 3.4 Direction - 方向枚举

```csharp
public enum Direction
{
    North = 0,  // 掩码: 1
    East = 1,   // 掩码: 2
    South = 2,  // 掩码: 4
    West = 3    // 掩码: 8
}

// 扩展方法
public static int ToMask(this Direction direction);
public static Direction Opposite(this Direction direction);
```

---

## 4. 核心算法

### 4.1 关键路径生成 - 醉汉游走算法

```
算法流程:
1. 从顶排(y=0)随机选择入口位置
2. 每层执行:
   a. 随机选择水平方向 (-1=左, 1=右)
   b. 随机水平移动 1-3 步
   c. 撞墙后反向继续
   d. 向下移动到下一层
3. 标记路径上的房间为关键路径
4. 设置入口(Start)、出口(Exit)、Boss房间
```

```csharp
private void GenerateCriticalPath()
{
    // 从顶排随机选择入口
    int startX = _rng.Next(LevelShape.GridWidth);
    Vector2Int current = new Vector2Int(startX, 0);
    
    List<Vector2Int> path = new List<Vector2Int>();
    path.Add(current);
    
    // 每层水平游走后向下
    for (int row = 0; row < LevelShape.GridHeight; row++)
    {
        int horizontalDirection = _rng.Next(2) == 0 ? -1 : 1;
        int horizontalSteps = _rng.Next(1, 4);
        
        // 水平游走
        for (int step = 0; step < horizontalSteps; step++)
        {
            int nextX = current.x + horizontalDirection;
            
            // 边界检查和反向
            if (nextX < 0 || nextX >= LevelShape.GridWidth)
            {
                horizontalDirection = -horizontalDirection;
                nextX = current.x + horizontalDirection;
                if (nextX < 0 || nextX >= LevelShape.GridWidth) break;
            }
            
            // 添加连接
            Vector2Int next = new Vector2Int(nextX, current.y);
            Direction dir = GetDirection(current, next);
            _roomGrid[current.x, current.y].AddConnection(dir);
            _roomGrid[next.x, next.y].AddConnection(dir.Opposite());
            
            current = next;
            path.Add(current);
        }
        
        // 向下移动
        if (row < LevelShape.GridHeight - 1)
        {
            Vector2Int next = new Vector2Int(current.x, current.y + 1);
            // 添加垂直连接...
            current = next;
            path.Add(current);
        }
    }
    
    // 设置房间类型
    _roomGrid[path[0].x, path[0].y].Type = RoomType.Start;
    _roomGrid[path[^1].x, path[^1].y].Type = RoomType.Exit;
    _roomGrid[path[^2].x, path[^2].y].Type = RoomType.Boss;
}
```

### 4.2 洞穴填充 - 细胞自动机

```
算法流程:
1. 初始化: 根据填充密度随机填充
2. 边缘加权: 边缘和底部区域更容易填充
3. 平滑迭代: 多次细胞自动机平滑
4. 雕刻通道: 在填充中雕刻曲折的行走通道
```

```csharp
private void GenerateConnectedCaveFill()
{
    // 初始化随机填充
    bool[,] cave = new bool[fillWidth, fillHeight];
    
    for (int y = 0; y < fillHeight; y++)
    {
        for (int x = 0; x < fillWidth; x++)
        {
            // 边缘加权
            int distToEdge = Mathf.Min(x, y, fillWidth - 1 - x, fillHeight - 1 - y);
            float edgeFactor = (distToEdge < 6) ? 2.0f : 1.0f;
            
            // 底部加权 (形成地面)
            if (y < fillHeight / 4) edgeFactor *= 1.8f;
            
            cave[x, y] = _rng.NextDouble() < FillDensity * edgeFactor;
        }
    }
    
    // 细胞自动机平滑
    for (int i = 0; i < SmoothIterations + 3; i++)
    {
        cave = SmoothCave(cave, fillWidth, fillHeight);
    }
}

private bool[,] SmoothCave(bool[,] cave, int width, int height)
{
    bool[,] newCave = new bool[width, height];
    
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

### 4.3 平台生成 - 基于跳跃力

```csharp
private void DrawPlatforms()
{
    // 基于跳跃力计算最大可达高度
    int maxJumpHeight = Mathf.Min(MaxPlatformHeightDiff, 
        Mathf.FloorToInt(PlayerJumpForce * 0.5f));
    
    for (each valid room)
    {
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
}
```

---

## 5. Tilemap 层级系统

### 5.1 六层 Tilemap 结构

| 层级 | 名称 | 瓦片颜色 | 功能 | Sorting Order |
|------|------|----------|------|---------------|
| WallLayer | 墙壁层 | 红色 | 4×4网格外围边界 | 0 |
| FillLayer | 填充层 | 橙色 | 洞穴内部随机填充 | 1 |
| PlatformLayer | 平台层 | 蓝色 | 可站立平台 | 2 |
| EntranceLayer | 入口层 | 绿色 | 房间入口标记 | 3 |
| ExitLayer | 出口层 | 黑色 | 房间出口标记 | 4 |
| SpecialLayer | 特殊层 | 黄色 | Boss/Shop区域 | 5 |

### 5.2 GrayboxTileSet - 瓦片配置

```csharp
[System.Serializable]
public class GrayboxTileSet
{
    public TileBase RedTile;     // 外围墙壁
    public TileBase BlueTile;    // 平台
    public TileBase GreenTile;   // 入口
    public TileBase YellowTile;  // 特殊区域
    public TileBase BlackTile;   // 出口
    public TileBase OrangeTile;  // 填充
    public TileBase WhiteTile;   // 预留
    public TileBase PurpleTile;  // 预留
    public TileBase PinkTile;    // 预留
}
```

---

## 6. 配置系统

### 6.1 GenerationSettings - 生成参数

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

### 6.2 DifficultyConfig - 难度配置

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
    public float CalculateLevelDifficulty(int levelIndex);
    public float CalculateRoomDifficulty(float levelDifficulty, RoomType type, bool isCriticalPath);
    public int CalculateEnemyCount(float roomDifficulty, RoomType type, bool isCriticalPath);
}
```

---

## 7. 多网格关卡管理

### 7.1 MultiGridLevelManager

支持在场景中生成多个独立的 4×4 网格关卡区域：

```csharp
public class MultiGridLevelManager : MonoBehaviour
{
    // 配置
    public int GridCount = 4;              // 网格数量
    public int LayoutAreaWidth = 200;      // 布局区域宽度
    public int LayoutAreaHeight = 200;     // 布局区域高度
    public int MinGridSpacing = 16;        // 最小间距
    public int PositionRandomOffset = 16;  // 位置随机偏移
    
    // 随机性控制
    public int BaseSeed = 0;               // 基础种子 (0=随机)
    public bool UseUniqueSeedPerGrid = true;
    
    // 特殊区域概率
    public float MedianGridSpecialChance = 0.8f;
    public float OtherGridSpecialChance = 0.15f;
    
    // 公共方法
    public void GenerateMultiGridLevel();
    public void ClearAllGrids();
    public List<Vector2Int> GetGridPositions();
    public List<Vector3> GetEntrancePositions();
    public List<Vector3> GetExitPositions();
}
```

### 7.2 位置生成算法

```
1. 初始化随机数生成器
2. 对于每个网格:
   a. 随机生成位置 (x, y)
   b. 添加随机偏移
   c. 检查与已放置网格的重叠
   d. 如果不重叠，记录位置
   e. 如果重叠，重试 (最多100次)
3. 按顺序生成每个网格内容
```

---

## 8. 编辑器工具

### 8.1 GrayboxLevelGeneratorEditor

自定义 Inspector，提供：
- 4×4 形状可视化编辑器
- 预设形状快速加载
- 一键生成/清除关卡
- 瓦片颜色图例
- 自动创建 Tilemap 层级结构

### 8.2 GrayboxPreviewEditor

独立编辑器窗口 (`Crypta Geometrica: RE/CryptaGeometricaMapEditor/灰盒预览工具`)：
- 单房间类型预览
- 网格预览
- 房间尺寸实时调整
- 生成状态监控
- 房间类型说明

---

## 9. 使用指南

### 9.1 快速开始

1. **创建 Tilemap 层级**
   - 在 GrayboxLevelGenerator Inspector 中点击 "创建Tilemap层级结构"
   - 自动创建 Grid 和 6 层 Tilemap

2. **配置瓦片**
   - 创建 7 种颜色的 Tile 资源
   - 拖入 GrayboxTileSet 对应字段

3. **选择形状**
   - 使用预设形状或自定义编辑
   - 点击格子切换启用/禁用

4. **生成关卡**
   - 点击 "生成关卡" 按钮
   - 或调用 `GenerateLevel(LevelShape shape)`

### 9.2 代码调用示例

```csharp
// 获取生成器引用
var generator = GetComponent<GrayboxLevelGenerator>();

// 使用预设形状生成
generator.GenerateLevel(LevelShapePresets.CrossShape);

// 使用自定义形状生成
var customShape = LevelShape.FromString("1100,1110,0111,0011");
generator.GenerateLevel(customShape);

// 多网格生成
var multiGrid = GetComponent<MultiGridLevelManager>();
multiGrid.GridCount = 4;
multiGrid.GenerateMultiGridLevel();

// 获取入口出口位置
var entrances = multiGrid.GetEntrancePositions();
var exits = multiGrid.GetExitPositions();
```

---

## 10. 扩展建议

### 10.1 待实现功能

- [ ] WFC (波函数坍缩) 微观瓦片生成
- [ ] 物理可达性验证 (A* 路径检测)
- [ ] 敌人生成点自动计算
- [ ] 宝箱/道具放置逻辑
- [ ] 关卡主题切换 (不同瓦片集)

### 10.2 性能优化方向

- Tilemap 批处理 (超过阈值时使用 SetTilesBlock)
- 异步生成 (协程分帧)
- 对象池复用 RoomNode

---

## 11. 版本记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-01-15 | 初始文档，整理现有代码结构 |

---

## 12. 参考资料

- Spelunky 关卡生成分析
- 细胞自动机洞穴生成
- Unity Tilemap 最佳实践
