# 多房间关卡生成 API 文档

> **项目**: Crypta Geometrica: RE  
> **版本**: 1.0  
> **更新日期**: 2026-01-16  
> **关联文档**: [MD_ProceduralRoomGeneration.md](./MD_ProceduralRoomGeneration.md)

---

## 1. MultiGridLevelManager API

多网格关卡管理器，负责生成和管理多个网格的布局。

### 1.1 公共属性

```csharp
// 网格配置
public int GridCount { get; set; }              // 网格总数量 (1-8)
public int LayoutAreaWidth { get; set; }        // 布局区域宽度 (瓦片)
public int LayoutAreaHeight { get; set; }       // 布局区域高度 (瓦片)
public int MinGridSpacing { get; set; }         // 网格最小间距 (瓦片)
public int PositionRandomOffset { get; set; }   // 位置随机偏移 (瓦片)
public int BaseSeed { get; set; }               // 随机种子 (0=随机)
public float MedianGridSpecialChance { get; set; } // 中位数网格特殊区域概率
```

### 1.2 核心方法

#### GenerateMultiGridLevel()

生成完整的多网格关卡。

```csharp
/// <summary>
/// 生成多网格关卡
/// </summary>
/// <returns>生成是否成功</returns>
public bool GenerateMultiGridLevel()
```

**使用示例**:

```csharp
var manager = GetComponent<MultiGridLevelManager>();
manager.GridCount = 4;
manager.BaseSeed = 12345;  // 固定种子可复现结果

if (manager.GenerateMultiGridLevel())
{
    Debug.Log("关卡生成成功");
}
```

#### ClearAllGrids()

清除所有已生成的网格。

```csharp
/// <summary>
/// 清除所有已生成的网格和Tilemap内容
/// </summary>
public void ClearAllGrids()
```

### 1.3 入口/出口查询

#### GetEntrancePositions() / GetExitPositions()

获取所有网格的入口/出口世界坐标。

```csharp
/// <summary>
/// 获取所有入口位置
/// </summary>
/// <returns>入口世界坐标列表</returns>
public List<Vector3> GetEntrancePositions()

/// <summary>
/// 获取所有出口位置
/// </summary>
/// <returns>出口世界坐标列表</returns>
public List<Vector3> GetExitPositions()
```

#### GetEntranceDirections() / GetExitDirections()

获取入口/出口的朝向。

```csharp
/// <summary>
/// 获取所有入口朝向
/// </summary>
/// <returns>入口方向列表</returns>
public List<Direction> GetEntranceDirections()

/// <summary>
/// 获取所有出口朝向
/// </summary>
/// <returns>出口方向列表</returns>
public List<Direction> GetExitDirections()
```

**使用示例**:

```csharp
// 获取第一个网格的入口位置，用于放置玩家
var entrances = manager.GetEntrancePositions();
var entranceDirs = manager.GetEntranceDirections();

if (entrances.Count > 0)
{
    player.transform.position = entrances[0];
    
    // 根据入口方向设置玩家朝向
    if (entranceDirs[0] == Direction.East)
        player.FaceRight();
}

// 获取出口位置，用于设置关卡结束触发器
var exits = manager.GetExitPositions();
foreach (var exitPos in exits)
{
    Instantiate(exitTriggerPrefab, exitPos, Quaternion.identity);
}
```

### 1.4 网格边界查询

```csharp
/// <summary>
/// 获取所有已放置网格的边界
/// </summary>
/// <returns>网格边界Rect列表</returns>
public List<Rect> GetPlacedGridBounds()

/// <summary>
/// 检查指定位置是否在任意网格内
/// </summary>
public bool IsPositionInAnyGrid(Vector2 position)
```

---

## 2. GrayboxLevelGenerator API

单网格关卡生成器，负责生成单个 4×4 宏观网格的内容。

### 2.1 公共属性

```csharp
// 房间尺寸
public int RoomWidth { get; set; }      // 单个房间宽度 (瓦片)
public int RoomHeight { get; set; }     // 单个房间高度 (瓦片)
public int WallThickness { get; set; }  // 墙壁厚度 (瓦片)

// 洞穴生成
public float FillDensity { get; set; }      // 填充密度 (0.0-1.0)
public int SmoothIterations { get; set; }   // 平滑迭代次数

// 平台生成
public int MinPlatformGap { get; set; }         // 平台最小间距
public int MaxPlatformHeightDiff { get; set; }  // 平台最大高度差
public float PlayerJumpForce { get; set; }      // 玩家跳跃力 (用于计算可达性)
```

### 2.2 核心方法

#### GenerateLevel()

使用指定形状生成关卡。

```csharp
/// <summary>
/// 使用指定形状生成关卡
/// </summary>
/// <param name="shape">关卡形状定义</param>
/// <param name="worldOffset">世界坐标偏移</param>
public void GenerateLevel(LevelShape shape, Vector2Int worldOffset = default)
```

**使用示例**:

```csharp
var generator = GetComponent<GrayboxLevelGenerator>();

// 使用预设形状
generator.GenerateLevel(LevelShapePresets.CrossShape);

// 使用自定义形状
var customShape = LevelShape.FromString("1100,1110,0111,0011");
generator.GenerateLevel(customShape, new Vector2Int(100, 0));
```

#### ClearLevel()

清除当前生成的关卡。

```csharp
/// <summary>
/// 清除所有Tilemap内容
/// </summary>
public void ClearLevel()
```

### 2.3 房间查询

```csharp
/// <summary>
/// 获取指定网格坐标的房间节点
/// </summary>
/// <param name="x">网格X坐标 (0-3)</param>
/// <param name="y">网格Y坐标 (0-3)</param>
/// <returns>房间节点，无效坐标返回null</returns>
public RoomNode GetRoomAt(int x, int y)

/// <summary>
/// 获取所有关键路径上的房间
/// </summary>
/// <returns>关键路径房间列表</returns>
public List<RoomNode> GetCriticalPathRooms()

/// <summary>
/// 获取指定类型的所有房间
/// </summary>
/// <param name="type">房间类型</param>
/// <returns>匹配的房间列表</returns>
public List<RoomNode> GetRoomsByType(RoomType type)
```

**使用示例**:

```csharp
// 获取Boss房间位置
var bossRooms = generator.GetRoomsByType(RoomType.Boss);
if (bossRooms.Count > 0)
{
    var bossRoom = bossRooms[0];
    SpawnBoss(bossRoom.ActiveZone.center);
}

// 遍历关键路径放置敌人
foreach (var room in generator.GetCriticalPathRooms())
{
    if (room.Type != RoomType.Start && room.Type != RoomType.Exit)
    {
        SpawnEnemies(room, room.EnemyCount);
    }
}
```

---

## 3. LevelShape API

关卡形状定义类，使用 4×4 位掩码表示有效区域。

### 3.1 静态工厂方法

```csharp
/// <summary>
/// 从字符串创建形状
/// </summary>
/// <param name="pattern">形状字符串，格式: "1111,1111,1111,1111"</param>
/// <returns>LevelShape实例</returns>
public static LevelShape FromString(string pattern)

/// <summary>
/// 创建完整的4×4方形
/// </summary>
public static LevelShape FullSquare()
```

**使用示例**:

```csharp
// 从字符串创建 L 形
var lShape = LevelShape.FromString("1000,1000,1111,1111");

// 从字符串创建 Z 形
var zShape = LevelShape.FromString("1110,0110,0110,0111");

// 使用预设
var cross = LevelShapePresets.CrossShape;
```

### 3.2 查询方法

```csharp
/// <summary>
/// 检查指定格子是否有效
/// </summary>
public bool IsValidCell(int x, int y)

/// <summary>
/// 获取所有有效格子坐标
/// </summary>
public List<Vector2Int> GetValidCells()

/// <summary>
/// 获取指定格子的有效相邻格子
/// </summary>
public List<Vector2Int> GetValidNeighbors(int x, int y)

/// <summary>
/// 获取顶部行的有效格子 (用于确定入口)
/// </summary>
public List<Vector2Int> GetTopRowCells()

/// <summary>
/// 获取底部行的有效格子 (用于确定出口)
/// </summary>
public List<Vector2Int> GetBottomRowCells()
```

### 3.3 预设形状库 (LevelShapePresets)

```csharp
public static class LevelShapePresets
{
    public static LevelShape FullSquare;   // ■■■■ / ■■■■ / ■■■■ / ■■■■
    public static LevelShape LShape;       // ■□□□ / ■□□□ / ■■■■ / ■■■■
    public static LevelShape TShape;       // ■■■■ / □■■□ / □■■□ / □■■□
    public static LevelShape CrossShape;   // □■■□ / ■■■■ / ■■■■ / □■■□
    public static LevelShape ZShape;       // ■■■□ / □■■□ / □■■□ / □■■■
}
```

---

## 4. RoomNode API

房间节点数据结构。

### 4.1 属性

```csharp
// 位置
public Vector2Int GridCoordinates { get; }  // 宏观网格坐标 (0-3, 0-3)

// 类型与状态
public RoomType Type { get; set; }          // 房间类型
public bool IsCriticalPath { get; }         // 是否在关键路径上
public bool IsGenerated { get; }            // 是否已生成

// 连通性
public int ConnectionMask { get; }          // 连接掩码 (4-bit)

// 活跃区域
public RectInt ActiveZone { get; }          // 软边界活跃区域

// 游戏性数据
public float DifficultyRating { get; }      // 难度系数 (0.0-1.0)
public int EnemyCount { get; }              // 敌人数量
public List<Vector2Int> EnemySpawnPoints { get; } // 敌人生成点
```

### 4.2 连通性方法

```csharp
/// <summary>
/// 检查是否有指定方向的连接
/// </summary>
public bool HasConnection(Direction direction)

/// <summary>
/// 获取所有连接方向
/// </summary>
public List<Direction> GetConnections()
```

**使用示例**:

```csharp
var room = generator.GetRoomAt(1, 2);

// 检查房间连通性
if (room.HasConnection(Direction.East))
{
    Debug.Log("房间向东有通道");
}

// 获取所有连接方向
foreach (var dir in room.GetConnections())
{
    Debug.Log($"连接方向: {dir}");
}

// 使用活跃区域放置物品
var center = room.ActiveZone.center;
Instantiate(treasurePrefab, new Vector3(center.x, center.y, 0), Quaternion.identity);
```

---

## 5. Direction 枚举与扩展

### 5.1 枚举定义

```csharp
public enum Direction
{
    North = 0,  // 上
    East = 1,   // 右
    South = 2,  // 下
    West = 3    // 左
}
```

### 5.2 扩展方法

```csharp
public static class DirectionExtensions
{
    /// <summary>
    /// 转换为位掩码
    /// </summary>
    public static int ToMask(this Direction direction)
    
    /// <summary>
    /// 获取相反方向
    /// </summary>
    public static Direction Opposite(this Direction direction)
    
    /// <summary>
    /// 转换为单位向量
    /// </summary>
    public static Vector2Int ToVector(this Direction direction)
}
```

**使用示例**:

```csharp
Direction dir = Direction.East;

// 获取相反方向
Direction opposite = dir.Opposite();  // West

// 转换为向量
Vector2Int vec = dir.ToVector();  // (1, 0)

// 用于位掩码操作
int mask = dir.ToMask();  // 2 (0b0010)
```

---

## 6. RoomType 枚举

```csharp
public enum RoomType
{
    None = 0,      // 无效/未分配
    Start = 1,     // 起点房间
    Exit = 2,      // 终点房间
    LR = 3,        // 左右贯通
    Drop = 4,      // 下落房间
    Landing = 5,   // 着陆房间
    Side = 6,      // 侧室
    Shop = 7,      // 商店房间
    Abyss = 8,     // 深渊竖井
    Boss = 9       // Boss房间
}
```

---

## 7. 完整使用示例

### 7.1 基础关卡生成

```csharp
public class LevelManager : MonoBehaviour
{
    [SerializeField] private MultiGridLevelManager _multiGrid;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _exitTriggerPrefab;
    
    public void GenerateNewLevel(int seed = 0)
    {
        // 配置参数
        _multiGrid.GridCount = 4;
        _multiGrid.BaseSeed = seed;
        _multiGrid.LayoutAreaWidth = 200;
        _multiGrid.LayoutAreaHeight = 200;
        
        // 生成关卡
        if (!_multiGrid.GenerateMultiGridLevel())
        {
            Debug.LogError("关卡生成失败");
            return;
        }
        
        // 放置玩家
        var entrances = _multiGrid.GetEntrancePositions();
        if (entrances.Count > 0)
        {
            Instantiate(_playerPrefab, entrances[0], Quaternion.identity);
        }
        
        // 放置出口触发器
        var exits = _multiGrid.GetExitPositions();
        foreach (var exitPos in exits)
        {
            Instantiate(_exitTriggerPrefab, exitPos, Quaternion.identity);
        }
    }
}
```

### 7.2 自定义形状生成

```csharp
public class CustomLevelGenerator : MonoBehaviour
{
    [SerializeField] private GrayboxLevelGenerator _generator;
    
    public void GenerateCustomLevel()
    {
        // 创建自定义 T 形关卡
        var tShape = LevelShape.FromString("1111,0110,0110,0110");
        
        _generator.RoomWidth = 32;
        _generator.RoomHeight = 24;
        _generator.FillDensity = 0.45f;
        
        _generator.GenerateLevel(tShape);
        
        // 在Boss房间生成Boss
        var bossRooms = _generator.GetRoomsByType(RoomType.Boss);
        foreach (var room in bossRooms)
        {
            SpawnBoss(room.ActiveZone.center);
        }
    }
}
```

### 7.3 敌人生成集成

```csharp
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GrayboxLevelGenerator _generator;
    [SerializeField] private GameObject[] _enemyPrefabs;
    
    public void SpawnEnemiesInLevel()
    {
        var criticalPath = _generator.GetCriticalPathRooms();
        
        foreach (var room in criticalPath)
        {
            // 跳过起点和终点
            if (room.Type == RoomType.Start || room.Type == RoomType.Exit)
                continue;
            
            // 根据房间难度选择敌人
            int enemyTier = Mathf.FloorToInt(room.DifficultyRating * _enemyPrefabs.Length);
            enemyTier = Mathf.Clamp(enemyTier, 0, _enemyPrefabs.Length - 1);
            
            // 在生成点放置敌人
            foreach (var spawnPoint in room.EnemySpawnPoints)
            {
                var pos = new Vector3(spawnPoint.x, spawnPoint.y, 0);
                Instantiate(_enemyPrefabs[enemyTier], pos, Quaternion.identity);
            }
        }
    }
}
```

---

## 8. 版本记录

| 版本 | 日期 | 说明 |
| ---- | ---- | ---- |
| 1.0 | 2026-01-16 | 从主文档拆分，独立API文档 |
