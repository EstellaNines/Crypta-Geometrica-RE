# V4 多房间PCG生成系统 - API参考文档

> **版本**: 1.0  
> **更新日期**: 2026-01-18

---

## 目录

1. [Core - 核心类](#1-core---核心类)
2. [Rules - 规则类](#2-rules---规则类)
3. [Data - 数据结构](#3-data---数据结构)
4. [Enums - 枚举类型](#4-enums---枚举类型)

---

## 1. Core - 核心类

### 1.1 DungeonGenerator

生成器主类，负责执行生成管线。

**命名空间**: `CryptaGeometrica.LevelGeneration.V4`

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `PipelineData` | `DungeonPipelineData` | 管线配置数据 |
| `Context` | `DungeonContext` | 当前生成上下文 |
| `IsGenerating` | `bool` | 是否正在生成 |

#### 方法

```csharp
/// <summary>
/// 异步生成地牢
/// </summary>
/// <param name="seed">随机种子，-1表示随机</param>
/// <returns>是否成功</returns>
public async UniTask<bool> GenerateDungeonAsync(int seed = -1)

/// <summary>
/// 取消当前生成
/// </summary>
public void CancelGeneration()

/// <summary>
/// 清除所有Tilemap
/// </summary>
public void ClearAllTiles()
```

#### 使用示例

```csharp
// 获取生成器引用
var generator = GetComponent<DungeonGenerator>();

// 异步生成
bool success = await generator.GenerateDungeonAsync(12345);

// 取消生成
generator.CancelGeneration();
```

---

### 1.2 DungeonContext

数据黑板，存储生成过程中的所有数据。

#### 属性 - 宏观层

| 属性 | 类型 | 说明 |
|------|------|------|
| `RoomNodes` | `List<RoomNode>` | 所有房间节点 |
| `StartRoom` | `RoomNode?` | 起始房间 |
| `EndRoom` | `RoomNode?` | 终点房间 |
| `GridColumns` | `int` | 网格列数 |
| `GridRows` | `int` | 网格行数 |
| `RoomSize` | `Vector2Int` | 房间尺寸 |

#### 属性 - 微观层

| 属性 | 类型 | 说明 |
|------|------|------|
| `BackgroundTileData` | `int[]` | 背景层数据 |
| `GroundTileData` | `int[]` | 地面层数据 |
| `PlatformTileData` | `int[]` | 平台层数据 |
| `MapWidth` | `int` | 地图宽度（瓦片） |
| `MapHeight` | `int` | 地图高度（瓦片） |

#### 方法

```csharp
/// <summary>
/// 获取指定层的瓦片值
/// </summary>
/// <param name="layer">Tilemap层</param>
/// <param name="x">X坐标</param>
/// <param name="y">Y坐标</param>
/// <returns>瓦片值（0=空，1=实心）</returns>
public int GetTile(TilemapLayer layer, int x, int y)

/// <summary>
/// 设置指定层的瓦片值
/// </summary>
/// <param name="layer">Tilemap层</param>
/// <param name="x">X坐标</param>
/// <param name="y">Y坐标</param>
/// <param name="value">瓦片值</param>
public void SetTile(TilemapLayer layer, int x, int y, int value)

/// <summary>
/// 坐标转换：世界坐标 → 数组索引
/// </summary>
public int CoordToIndex(int x, int y)

/// <summary>
/// 释放资源
/// </summary>
public void Dispose()
```

#### 使用示例

```csharp
// 遍历所有房间
foreach (var room in context.RoomNodes)
{
    Debug.Log($"房间位置: {room.GridPosition}");
}

// 读写瓦片数据
int tile = context.GetTile(TilemapLayer.Ground, 10, 20);
context.SetTile(TilemapLayer.Platform, 10, 20, 1);
```

---

### 1.3 DungeonPipelineData

管线配置ScriptableObject。

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Rules` | `List<IGeneratorRule>` | 规则列表 |
| `GridColumns` | `int` | 网格列数 |
| `GridRows` | `int` | 网格行数 |
| `RoomSize` | `Vector2Int` | 房间尺寸 |

#### 方法

```csharp
/// <summary>
/// 验证所有规则配置
/// </summary>
/// <returns>是否全部有效</returns>
public bool ValidateAll()

/// <summary>
/// 按执行顺序获取启用的规则
/// </summary>
public IEnumerable<IGeneratorRule> GetOrderedRules()
```

---

## 2. Rules - 规则类

### 2.1 IGeneratorRule (接口)

所有规则必须实现的接口。

```csharp
public interface IGeneratorRule
{
    /// <summary>规则名称</summary>
    string RuleName { get; }
    
    /// <summary>是否启用</summary>
    bool Enabled { get; set; }
    
    /// <summary>执行顺序（小的先执行）</summary>
    int ExecutionOrder { get; }
    
    /// <summary>异步执行规则</summary>
    UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token);
    
    /// <summary>验证配置有效性</summary>
    bool Validate(out string errorMessage);
}
```

---

### 2.2 GeneratorRuleBase (基类)

规则基类，提供通用功能。

#### 受保护属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `_ruleName` | `string` | 规则名称 |
| `_enabled` | `bool` | 是否启用 |
| `_executionOrder` | `int` | 执行顺序 |

#### 受保护方法

```csharp
/// <summary>输出信息日志</summary>
protected void LogInfo(string message)

/// <summary>输出警告日志</summary>
protected void LogWarning(string message)

/// <summary>输出错误日志</summary>
protected void LogError(string message)
```

---

### 2.3 ConstrainedLayoutRule

醉汉游走房间布局规则。

**执行顺序**: 10

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_maxSteps` | `int` | 25 | 最大游走步数 |
| `_minRooms` | `int` | 8 | 最少房间数 |
| `_downwardBias` | `float` | 0.4 | 向下偏好 |
| `_sidewaysBias` | `float` | 0.3 | 横向偏好 |
| `_maxRetries` | `int` | 25 | 最大重试次数 |

---

### 2.4 BFSValidationRule

BFS连通性验证规则。

**执行顺序**: 20

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_markCriticalPath` | `bool` | true | 标记关键路径 |

---

### 2.5 CellularAutomataRule

细胞自动机地形生成规则。

**执行顺序**: 30

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_iterations` | `int` | 8 | CA迭代次数 |
| `_fillProbability` | `float` | 0.45 | 初始填充率 |
| `_birthLimit` | `int` | 4 | 出生阈值 |
| `_deathLimit` | `int` | 3 | 死亡阈值 |
| `_borderThickness` | `int` | 2 | 边界厚度 |
| `_removeIsolatedTiles` | `bool` | true | 移除孤立格 |

---

### 2.6 EntranceExitRule

入口出口挖掘规则。

**执行顺序**: 35

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_carveRadius` | `int` | 4 | 挖掘半径 |
| `_ensureFloor` | `bool` | true | 确保有地板 |
| `_floorThickness` | `int` | 2 | 地板厚度 |

---

### 2.7 PathValidationRule

2x2玩家路径验证规则。

**执行顺序**: 36

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_playerSize` | `int` | 2 | 玩家尺寸 |
| `_autoFix` | `bool` | true | 自动修复 |
| `_fixPathWidth` | `int` | 3 | 修复通道宽度 |

---

### 2.8 PlatformRule

空气柱步进采样平台生成规则。

**执行顺序**: 40

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_jumpHeight` | `int` | 8 | 单跳高度 |
| `_doubleJump` | `bool` | true | 支持二段跳 |
| `_safetyMargin` | `int` | 2 | 安全余量 |
| `_minPlatformWidth` | `int` | 2 | 最小平台宽度 |
| `_maxPlatformWidth` | `int` | 5 | 最大平台宽度 |
| `_platformThickness` | `int` | 1 | 平台厚度 |
| `_minHorizontalSpacing` | `int` | 4 | 最小水平间距 |
| `_debugLog` | `bool` | false | 显示调试日志 |

---

### 2.9 渲染规则

#### RoomRenderRule
**执行顺序**: 100  
渲染房间背景层。

#### WallRenderRule
**执行顺序**: 105  
渲染墙壁边缘装饰。

#### GroundRenderRule
**执行顺序**: 110  
渲染地面层（使用Rule Tile）。

#### PlatformRenderRule
**执行顺序**: 120  
渲染平台层（使用Rule Tile）。

---

## 3. Data - 数据结构

### 3.1 RoomNode

房间节点结构体。

```csharp
public struct RoomNode
{
    /// <summary>网格坐标</summary>
    public Vector2Int GridPosition;
    
    /// <summary>房间类型</summary>
    public RoomType Type;
    
    /// <summary>门类型</summary>
    public LevelDoorType DoorType;
    
    /// <summary>侧向门位置</summary>
    public WallDirection RestrictedDoorSide;
    
    /// <summary>是否关键路径</summary>
    public bool IsCritical;
    
    /// <summary>世界边界</summary>
    public BoundsInt WorldBounds;
    
    /// <summary>连接的邻居</summary>
    public List<Vector2Int> ConnectedNeighbors;
    
    /// <summary>设置为起始房间</summary>
    public void SetAsStart(WallDirection doorSide);
    
    /// <summary>设置为终点房间</summary>
    public void SetAsEnd(WallDirection doorSide);
}
```

---

### 3.2 TileConfigData

瓦片配置ScriptableObject。

```csharp
[CreateAssetMenu(menuName = "Dungeon/Tile Config")]
public class TileConfigData : ScriptableObject
{
    /// <summary>获取指定主题的配置</summary>
    public ThemeTileConfig GetConfig(TileTheme theme);
}

[Serializable]
public class ThemeTileConfig
{
    /// <summary>背景瓦片数组</summary>
    public TileBase[] BackgroundTiles;
    
    /// <summary>地面Rule Tile</summary>
    public RuleTile GroundRuleTile;
    
    /// <summary>平台Rule Tile</summary>
    public RuleTile PlatformRuleTile;
}
```

---

## 4. Enums - 枚举类型

### 4.1 RoomType

```csharp
public enum RoomType
{
    Empty = 0,   // 空房间
    Normal = 1,  // 普通房间
    Start = 2,   // 起始房间
    End = 3      // 终点房间
}
```

### 4.2 LevelDoorType

```csharp
public enum LevelDoorType
{
    None = 0,          // 无特殊门
    LevelEntrance = 1, // 关卡入口
    LevelExit = 2      // 关卡出口
}
```

### 4.3 WallDirection

```csharp
public enum WallDirection
{
    None = 0,
    Left = 1,
    Right = 2,
    Top = 3,
    Bottom = 4
}
```

### 4.4 TilemapLayer

```csharp
public enum TilemapLayer
{
    Background = 0, // 背景层
    Ground = 1,     // 地面层
    Platform = 2,   // 平台层
    Decoration = 3  // 装饰层
}
```

### 4.5 TileTheme

```csharp
public enum TileTheme
{
    Blue = 0,   // 蓝色主题
    Green = 1,  // 绿色主题
    Red = 2     // 红色主题
}
```

---

## 5. 玩家组件

### 5.1 PlatformDropthrough

单向平台穿透组件，挂载到玩家对象。

**命名空间**: 无（全局）

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `platformLayer` | `LayerMask` | - | 平台Layer |
| `dropDuration` | `float` | 0.3 | 下落禁用碰撞时长 |
| `platformCheckDistance` | `float` | 0.2 | 平台检测距离 |

#### 使用方式

1. 将组件挂载到Player对象
2. 设置`platformLayer`为平台所在Layer
3. 玩家按S键/下箭头时自动穿透平台

---
