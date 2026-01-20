# 房间生成器 V4 API 参考文档

## 概述

本文档提供了房间生成器 V4 (`RoomGenerator`) 的详细 API 参考，包括核心类、规则接口、数据结构及枚举类型。

**命名空间**: `CryptaGeometrica.LevelGeneration.V4`

---

## 目录

1. [核心类 (Core)](#1-核心类-core)
2. [数据结构 (Data)](#2-数据结构-data)
3. [规则系统 (Rules)](#3-规则系统-rules)
4. [枚举类型 (Enums)](#4-枚举类型-enums)

---

## 1. 核心类 (Core)

### `DungeonGenerator`

**描述**: 地牢生成的主控制器，继承自 `MonoBehaviour`。负责加载管线、管理上下文并调度规则执行。

#### 公共属性

| 属性           | 类型                  | 描述                             |
| :------------- | :-------------------- | :------------------------------- |
| `PipelineData` | `DungeonPipelineData` | 当前使用的生成管线配置数据。     |
| `Context`      | `DungeonContext`      | 当前生成的运行时上下文（只读）。 |
| `IsGenerating` | `bool`                | 当前是否正在执行生成任务。       |

#### 公共方法

```csharp
/// <summary>
/// 异步生成地牢（标准入口）。
/// </summary>
/// <param name="seed">随机种子。-1 表示使用系统时间随机。</param>
/// <returns>UniTask&lt;bool&gt;: 生成是否成功。</returns>
public async UniTask<bool> GenerateDungeonAsync(int seed = -1);

/// <summary>
/// 异步生成地牢（带世界偏移）。
/// 用于 WorldGenerator 调用，支持将房间生成在特定世界坐标。
/// </summary>
/// <param name="seed">随机种子。</param>
/// <param name="worldOffset">世界空间的像素偏移量。</param>
/// <returns>UniTask&lt;bool&gt;: 生成是否成功。</returns>
public async UniTask<bool> GenerateDungeonAsync(int seed, Vector2Int worldOffset);

/// <summary>
/// 取消当前的生成任务。
/// </summary>
public void CancelGeneration();

/// <summary>
/// 清除所有已生成的 Tilemap 内容和上下文数据。
/// </summary>
public void ClearGeneration();
```

---

### `DungeonContext`

**描述**: 生成过程中的共享上下文（黑板），实现了 `IDisposable`。

#### 数据访问方法

```csharp
/// <summary>
/// 获取指定层的瓦片数据。
/// </summary>
/// <param name="layer">目标图层 (Background/Ground/Platform)。</param>
/// <param name="x">本地 Grid X 坐标。</param>
/// <param name="y">本地 Grid Y 坐标。</param>
/// <returns>int: 瓦片 ID (0=空, 1=实心)。</returns>
public int GetTile(TilemapLayer layer, int x, int y);

/// <summary>
/// 设置指定层的瓦片数据。
/// </summary>
public void SetTile(TilemapLayer layer, int x, int y, int value);

/// <summary>
/// 将 2D 坐标转换为一维数组索引。
/// </summary>
public int CoordToIndex(int x, int y);
```

#### 关键属性

- **`RoomNodes`**: `List<RoomNode>` - 生成的所有房间节点。
- **`StartRoom`**: `RoomNode?` - 起点房间。
- **`EndRoom`**: `RoomNode?` - 终点房间。
- **`MapWidth` / `MapHeight`**: `int` - 地图总尺寸（瓦片单位）。

---

### `DungeonPipelineData`

**描述**: `ScriptableObject` 配置文件，用于定义生成参数和规则链。

#### 关键属性

| 属性          | 类型                   | 描述                   |
| :------------ | :--------------------- | :--------------------- |
| `GridColumns` | `int`                  | 宏观网格列数。         |
| `GridRows`    | `int`                  | 宏观网格行数。         |
| `RoomSize`    | `Vector2Int`           | 单个房间的像素尺寸。   |
| `Rules`       | `List<IGeneratorRule>` | 按顺序执行的规则列表。 |

#### 方法

```csharp
/// <summary>
/// 验证所有规则的配置是否有效。
/// </summary>
public bool ValidateAll(out List<string> errors);

/// <summary>
/// 获取按 ExecutionOrder 排序后的启用规则列表。
/// </summary>
public List<IGeneratorRule> GetEnabledRules();
```

---

## 2. 数据结构 (Data)

### `RoomNode` (Struct)

**描述**: 宏观层面的单个房间节点数据。

```csharp
public struct RoomNode
{
    // 房间在宏观网格中的坐标
    public Vector2Int GridPosition;

    // 房间类型 (Normal, Start, End)
    public RoomType Type;

    // 该房间包含的门类型 (Entrance, Exit, None)
    public LevelDoorType DoorType;

    // 世界空间边界
    public BoundsInt WorldBounds;

    // 已连接的邻居坐标列表
    public List<Vector2Int> ConnectedNeighbors;

    // 是否位于关键路径上
    public bool IsCritical;

    // 限制门生成的方向（例如强制只有左侧门）
    public WallDirection RestrictedDoorSide;
}
```

---

## 3. 规则系统 (Rules)

### `IGeneratorRule` (Interface)

**描述**: 所有生成规则必须实现的接口。

```csharp
public interface IGeneratorRule
{
    // 规则名称
    string RuleName { get; }

    // 是否启用
    bool Enabled { get; set; }

    // 执行顺序 (升序执行)
    int ExecutionOrder { get; }

    // 核心执行逻辑
    UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token);

    // 配置验证
    bool Validate(out string errorMessage);
}
```

### 标准规则列表

| 类名                      | 默认 Order | 参数说明                                               |
| :------------------------ | :--------: | :----------------------------------------------------- |
| **ConstrainedLayoutRule** |     10     | `MaxSteps` (游走步数), `MinRooms` (最小房间数)         |
| **BFSValidationRule**     |     20     | `MarkCriticalPath` (是否标记关键路径)                  |
| **CellularAutomataRule**  |     30     | `Iterations` (迭代次数), `FillProbability` (填充率)    |
| **BorderEnforcementRule** |     32     | 无参数。强制边界实心化。                               |
| **EntranceExitRule**      |     35     | `CarveRadius` (挖掘半径)                               |
| **PathValidationRule**    |     36     | `PlayerSize` (玩家尺寸), `AutoFix` (自动修复路径)      |
| **PlatformRule**          |     40     | `JumpHeight` (跳跃高度), `PlatformInterval` (平台间隔) |
| **RenderRules**           |    100+    | 各类渲染规则 (Room, Ground, Wall, Platform)            |

---

## 4. 枚举类型 (Enums)

### `TilemapLayer`

用于区分不同的数据层：

- `Background` = 0
- `Ground` = 1
- `Platform` = 2
- `Decoration` = 3

### `RoomType`

- `Empty` = 0
- `Normal` = 1
- `Start` = 2
- `End` = 3

### `LevelDoorType`

- `None` = 0
- `LevelEntrance` = 1
- `LevelExit` = 2

---
