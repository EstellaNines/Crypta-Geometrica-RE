# 世界生成器 V4 API 文档

## 概述

世界生成器V4是一个基于规则管线的程序化世界生成系统，提供模块化、可扩展的API接口。本文档列出所有公开API及其参数说明。

---

## 核心类 API

### WorldGenerator

主控制器类，管理世界生成的整体流程。

#### GenerateWorldAsync

异步生成世界。

```csharp
public async UniTask<bool> GenerateWorldAsync(int seed = -1)
```

**描述**: 执行完整的世界生成流程，按顺序调用所有已启用的规则。

| 参数   | 类型 | 描述                                 |
| ------ | ---- | ------------------------------------ |
| `seed` | int  | 随机种子，-1表示使用配置值或系统时间 |

| 返回值 | 类型            | 描述         |
| ------ | --------------- | ------------ |
| -      | UniTask\<bool\> | 生成是否成功 |

---

#### CancelGeneration

取消正在进行的生成。

```csharp
public void CancelGeneration()
```

**描述**: 取消当前正在执行的生成过程，释放相关资源。

| 参数 | 无  |
| ---- | --- |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

#### ClearGeneration

清除生成结果。

```csharp
public void ClearGeneration()
```

**描述**: 清除已生成的世界内容，包括Tilemap和节点数据。

| 参数 | 无  |
| ---- | --- |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

#### ValidateConfiguration

验证配置是否有效。

```csharp
public bool ValidateConfiguration()
```

**描述**: 验证管线配置和所有规则是否正确配置。

| 参数 | 无  |
| ---- | --- |

| 返回值 | 类型 | 描述         |
| ------ | ---- | ------------ |
| -      | bool | 配置是否有效 |

---

### WorldContext

世界生成上下文，基于黑板模式的共享数据容器。

#### 构造函数

```csharp
public WorldContext(int roomCount, Vector2Int roomPixelSize, int seed = -1)
```

**描述**: 创建世界上下文实例。

| 参数            | 类型       | 描述                     |
| --------------- | ---------- | ------------------------ |
| `roomCount`     | int        | 目标房间数量             |
| `roomPixelSize` | Vector2Int | 单房间像素尺寸           |
| `seed`          | int        | 随机种子，-1使用系统时间 |

---

#### Reset

重置上下文状态。

```csharp
public void Reset(int newSeed = -1)
```

**描述**: 清空网格和节点数据，可选更新随机种子。

| 参数      | 类型 | 描述                 |
| --------- | ---- | -------------------- |
| `newSeed` | int  | 新种子，-1保持原种子 |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

#### IsInBounds

检查网格位置是否在边界内。

```csharp
public bool IsInBounds(Vector2Int position)
```

**描述**: 判断给定坐标是否在网格有效范围内。

| 参数       | 类型       | 描述     |
| ---------- | ---------- | -------- |
| `position` | Vector2Int | 网格坐标 |

| 返回值 | 类型 | 描述         |
| ------ | ---- | ------------ |
| -      | bool | 是否在边界内 |

---

#### IsOccupied

检查网格位置是否被占用。

```csharp
public bool IsOccupied(Vector2Int position)
```

**描述**: 判断给定坐标是否已放置房间。

| 参数       | 类型       | 描述     |
| ---------- | ---------- | -------- |
| `position` | Vector2Int | 网格坐标 |

| 返回值 | 类型 | 描述       |
| ------ | ---- | ---------- |
| -      | bool | 是否被占用 |

---

#### SetOccupied

设置网格位置占用状态。

```csharp
public void SetOccupied(Vector2Int position, bool occupied)
```

**描述**: 标记指定坐标的占用状态。

| 参数       | 类型       | 描述     |
| ---------- | ---------- | -------- |
| `position` | Vector2Int | 网格坐标 |
| `occupied` | bool       | 占用状态 |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

#### AddNode

添加世界节点。

```csharp
public void AddNode(WorldNode node)
```

**描述**: 将节点添加到列表并标记网格位置为已占用。

| 参数   | 类型      | 描述         |
| ------ | --------- | ------------ |
| `node` | WorldNode | 世界节点实例 |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

#### NextRandomFloat

获取下一个随机浮点数。

```csharp
public float NextRandomFloat()
```

**描述**: 返回 [0, 1) 范围的随机浮点数。

| 参数 | 无  |
| ---- | --- |

| 返回值 | 类型  | 描述              |
| ------ | ----- | ----------------- |
| -      | float | 随机浮点数 [0, 1) |

---

#### NextRandomInt

获取下一个随机整数。

```csharp
public int NextRandomInt(int maxExclusive)
public int NextRandomInt(int minInclusive, int maxExclusive)
```

**描述**: 返回指定范围内的随机整数。

| 参数           | 类型 | 描述             |
| -------------- | ---- | ---------------- |
| `minInclusive` | int  | 最小值（包含）   |
| `maxExclusive` | int  | 最大值（不包含） |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | int  | 随机整数 |

---

#### Dispose

释放资源。

```csharp
public void Dispose()
```

**描述**: 释放上下文占用的资源。

| 参数 | 无  |
| ---- | --- |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

### WorldPipelineData

管线配置 ScriptableObject。

#### GetEnabledRules

获取已启用的规则列表。

```csharp
public List<IWorldRule> GetEnabledRules()
```

**描述**: 返回按 ExecutionOrder 排序的已启用规则列表。

| 参数 | 无  |
| ---- | --- |

| 返回值 | 类型               | 描述             |
| ------ | ------------------ | ---------------- |
| -      | List\<IWorldRule\> | 排序后的规则列表 |

---

#### ValidateAll

验证所有规则配置。

```csharp
public bool ValidateAll(out List<string> errors)
```

**描述**: 验证管线配置和所有规则是否正确。

| 参数     | 类型               | 描述         |
| -------- | ------------------ | ------------ |
| `errors` | out List\<string\> | 错误信息列表 |

| 返回值 | 类型 | 描述             |
| ------ | ---- | ---------------- |
| -      | bool | 是否全部验证通过 |

---

### WorldNode

世界节点数据类。

#### 构造函数

```csharp
public WorldNode()
public WorldNode(Vector2Int gridPosition, int roomSeed)
public WorldNode(Vector2Int gridPosition, Vector2Int worldPixelOffset, int roomSeed)
```

**描述**: 创建世界节点实例。

| 参数               | 类型       | 描述         |
| ------------------ | ---------- | ------------ |
| `gridPosition`     | Vector2Int | 网格坐标     |
| `worldPixelOffset` | Vector2Int | 世界像素偏移 |
| `roomSeed`         | int        | 房间生成种子 |

---

#### CalculateWorldOffset

计算世界像素偏移。

```csharp
public void CalculateWorldOffset(Vector2Int roomPixelSize)
```

**描述**: 根据网格位置和房间尺寸计算世界坐标。

| 参数            | 类型       | 描述           |
| --------------- | ---------- | -------------- |
| `roomPixelSize` | Vector2Int | 单房间像素尺寸 |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

#### Reset

重置生成状态。

```csharp
public void Reset()
```

**描述**: 将 IsGenerated 标记重置为 false。

| 参数 | 无  |
| ---- | --- |

| 返回值 | 类型 | 描述     |
| ------ | ---- | -------- |
| -      | void | 无返回值 |

---

## 规则接口 API

### IWorldRule

世界生成规则接口。

#### 属性

| 属性             | 类型   | 描述                     |
| ---------------- | ------ | ------------------------ |
| `RuleName`       | string | 规则显示名称             |
| `Enabled`        | bool   | 是否启用此规则           |
| `ExecutionOrder` | int    | 执行顺序（越小越先执行） |

#### ExecuteAsync

异步执行生成逻辑。

```csharp
UniTask<bool> ExecuteAsync(WorldContext context, CancellationToken token)
```

**描述**: 执行规则的核心逻辑。

| 参数      | 类型              | 描述             |
| --------- | ----------------- | ---------------- |
| `context` | WorldContext      | 共享的世界上下文 |
| `token`   | CancellationToken | 取消令牌         |

| 返回值 | 类型            | 描述         |
| ------ | --------------- | ------------ |
| -      | UniTask\<bool\> | 执行是否成功 |

---

#### Validate

验证规则配置。

```csharp
bool Validate(out string errorMessage)
```

**描述**: 验证规则配置是否有效。

| 参数           | 类型       | 描述     |
| -------------- | ---------- | -------- |
| `errorMessage` | out string | 错误信息 |

| 返回值 | 类型 | 描述         |
| ------ | ---- | ------------ |
| -      | bool | 配置是否有效 |

---

### WorldRuleBase

规则基类，提供通用功能。

#### 保护方法

| 方法                                         | 描述         |
| -------------------------------------------- | ------------ |
| `LogInfo(string message)`                    | 输出信息日志 |
| `LogWarning(string message)`                 | 输出警告日志 |
| `LogError(string message)`                   | 输出错误日志 |
| `CheckCancellation(CancellationToken token)` | 检查取消请求 |
| `YieldFrame(CancellationToken token)`        | 安全等待一帧 |

---

## 具体规则 API

### RandomPlacementRule

随机放置规则。

#### ExecuteAsync

```csharp
public override async UniTask<bool> ExecuteAsync(WorldContext context, CancellationToken token)
```

**描述**: 在网格中随机放置房间节点。

| 参数      | 类型              | 描述       |
| --------- | ----------------- | ---------- |
| `context` | WorldContext      | 世界上下文 |
| `token`   | CancellationToken | 取消令牌   |

| 返回值 | 类型            | 描述                       |
| ------ | --------------- | -------------------------- |
| -      | UniTask\<bool\> | 是否成功放置目标数量的房间 |

---

### CoordinateCalcRule

坐标计算规则。

#### ExecuteAsync

```csharp
public override async UniTask<bool> ExecuteAsync(WorldContext context, CancellationToken token)
```

**描述**: 为每个节点计算世界像素坐标。

| 参数      | 类型              | 描述       |
| --------- | ----------------- | ---------- |
| `context` | WorldContext      | 世界上下文 |
| `token`   | CancellationToken | 取消令牌   |

| 返回值 | 类型            | 描述          |
| ------ | --------------- | ------------- |
| -      | UniTask\<bool\> | 始终返回 true |

---

### RoomGenerationRule

房间生成规则。

#### ExecuteAsync

```csharp
public override async UniTask<bool> ExecuteAsync(WorldContext context, CancellationToken token)
```

**描述**: 串行调用 DungeonGenerator 生成每个房间。

| 参数      | 类型              | 描述       |
| --------- | ----------------- | ---------- |
| `context` | WorldContext      | 世界上下文 |
| `token`   | CancellationToken | 取消令牌   |

| 返回值 | 类型            | 描述             |
| ------ | --------------- | ---------------- |
| -      | UniTask\<bool\> | 是否全部生成成功 |

---

## 快速参考

### 类型速查表

| 类                  | 命名空间                                  | 描述     |
| ------------------- | ----------------------------------------- | -------- |
| WorldGenerator      | CryptaGeometrica.LevelGeneration.V4.World | 主控制器 |
| WorldContext        | CryptaGeometrica.LevelGeneration.V4.World | 上下文   |
| WorldPipelineData   | CryptaGeometrica.LevelGeneration.V4.World | 管线配置 |
| WorldNode           | CryptaGeometrica.LevelGeneration.V4.World | 节点数据 |
| IWorldRule          | CryptaGeometrica.LevelGeneration.V4.World | 规则接口 |
| WorldRuleBase       | CryptaGeometrica.LevelGeneration.V4.World | 规则基类 |
| RandomPlacementRule | CryptaGeometrica.LevelGeneration.V4.World | 随机放置 |
| CoordinateCalcRule  | CryptaGeometrica.LevelGeneration.V4.World | 坐标计算 |
| RoomGenerationRule  | CryptaGeometrica.LevelGeneration.V4.World | 房间生成 |

### 依赖项

| 包             | 版本 | 用途       |
| -------------- | ---- | ---------- |
| UniTask        | 2.x  | 异步编程   |
| Odin Inspector | 3.x  | 编辑器扩展 |
