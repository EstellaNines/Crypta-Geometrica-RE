# 房间生成器 V4 技术文档

## 目录

1. [系统概述](#系统概述)
2. [架构设计](#架构设计)
3. [核心组件](#核心组件)
4. [规则管线](#规则管线)
5. [核心算法详解](#核心算法详解)
   - [约束醉汉游走](#1-约束醉汉游走-constrained-drunkard-walk)
   - [细胞自动机](#2-细胞自动机-cellular-automata)
   - [柏林噪声](#3-柏林噪声-perlin-noise)
   - [泊松盘采样](#4-泊松盘采样-poisson-disk-sampling)
   - [空气柱步进采样](#5-空气柱步进采样-air-column-interval-sampling)
   - [边界强制与门保护](#6-边界强制与门保护-border-enforcement)
6. [数据结构](#数据结构)
7. [配置指南](#配置指南)

---

## 系统概述

房间生成器V4（Dungeon Generator V4）是一个基于规则管线的多房间程序化内容生成（PCG）系统。它采用"数据-表现"分离架构，支持异步生成、模块化扩展和可视化配置。

### 核心特性

- **规则管线（Rule Pipeline）**：将生成过程分解为独立的原子规则，支持自定义执行顺序和热插拔。
- **数据与表现分离**：
  - **宏观层**：处理房间拓扑、连接关系和关键路径 (`RoomNode`)。
  - **微观层**：处理具体的瓦片数据 (`int[]`)，与Unity Tilemap解耦。
  - **渲染层**：独立的渲染规则负责将数据转换为 `Tile`。
- **异步架构**：基于 `UniTask` 的全异步流程，支持取消操作 (`CancellationToken`)，避免主线程卡顿。
- **可视化调试**：深度集成 `Odin Inspector`，支持并在Inspector中进行管线配置和单步调试。

---

## 架构设计

### 系统架构图

```mermaid
graph TB
    subgraph 核心层
        DG[DungeonGenerator] --> DP[DungeonPipelineData]
        DG --> DC[DungeonContext]
    end

    subgraph 规则层
        DP --> R_Macro[宏观规则]
        DP --> R_Micro[微观规则]
        DP --> R_Render[渲染规则]
    end

    subgraph 数据层
        DC --> MacroData[RoomNodes / Adjacency]
        DC --> MicroData[TileData Arrays]
    end

    subgraph 表现层
        R_Render --> Tilemaps[Unity Tilemaps]
    end
```

### 数据流向

1. **宏观阶段**：生成房间布局，确定房间位置、类型和连接关系。
2. **微观阶段**：在内存中生成瓦片数据（0=空，1=实心），执行CA、柏林噪声、泊松采样、挖掘、平台生成等算法。
3. **渲染阶段**：读取内存数据，批量设置到 Unity Tilemap。

---

## 核心算法详解

### 1. 约束醉汉游走 (Constrained Drunkard Walk)

用于生成房间的拓扑布局。此算法在经典随机游走的基础上增加了方向权重和约束，以生成更适合横版平台游戏（Platformer）的结构。

**规则类**: `ConstrainedLayoutRule` (`Rules/Macro`)

#### 权重逻辑

算法在选择下一步移动方向时，并非均匀随机，而是根据预设偏好进行加权：

- **向下偏好 (`_downwardBias`, 0.4)**: 鼓励地牢向深处延伸。
- **横向偏好 (`_sidewaysBias`, 0.3)**: 鼓励产生分支。
- **向上惩罚 (0.1)**: 极低概率向上回退，避免死循环和无意义堆叠。

```mermaid
pie title 移动方向概率分布示例
    "向下 (Down)" : 40
    "向左 (Left)" : 30
    "向右 (Right)" : 30
    "向上 (Up)" : 10
```

#### 智能回溯 (Smart Backtracking)

当游走陷入死胡同（四周均有房间或越界）时，算法不会立即终止，而是触发 **FindUnvisitedNeighbor** 策略：

1. 从已访问的房间列表中随机选择一个房间。
2. 检查其四周是否有未被占用的空位。
3. 如果有，瞬移到该房间并向空位方向移动，开辟新路径。
4. 这保证了即使如果不小心走进死胡同，也能继续生成直到满足 `MinRooms` 数量。

---

### 2. 细胞自动机 (Cellular Automata)

用于生成自然的有机洞穴形状。

**规则类**: `CellularAutomataRule` (`Rules/Micro`)

#### 演化规则

采用经典的 **4-5 规则** (B45/S4)：

- **诞生 (Birth)**: 如果死细胞周围有 **>= 4** 个活墙壁，则复活变成墙。
- **存活 (Survival)**: 如果活细胞周围有 **>= 4** 个活墙壁，则保持存活；否则死亡变成空地（Death Limit = 3）。

$$
State_{t+1}(x,y) = \begin{cases}
1 & \text{if } Neighbors(x,y) \ge 4 \\
0 & \text{if } Neighbors(x,y) < 4
\end{cases}
$$

#### 流程图

```mermaid
graph TD
    Start[初始化网格] --> Noise[填充随机噪点 FillProbability]
    Noise --> IterLoop{当前迭代 < MaxIterations?}
    IterLoop -->|是| Calc[计算每个细胞邻居]
    Calc --> Apply[应用 B4/S4 规则]
    Apply --> IterLoop
    IterLoop -->|否| Border[强制边界实心化]
    Border --> Smooth[移除孤立死角]
    Smooth --> End
```

---

### 3. 柏林噪声 (Perlin Noise)

用于生成地形的宏观起伏、生物群落分布以及背景墙壁的纹理变化。与完全随机的白噪声不同，柏林噪声具有 **平滑的梯度**，能够模拟自然界的山脉、云层和洞穴走势。

**规则类**: `PerlinNoiseRule` (`Rules/Advanced`)

#### 算法原理

柏林噪声通过在晶格顶点定义随机梯度向量，并对网格内的点进行插值计算，生成连续的伪随机数值。

- **输入**: 坐标 `(x, y)`，缩放因子 `Scale`，偏移量 `Offset`。
- **输出**: `[0, 1]` 范围内的浮点数。

#### 地形生成应用

我们使用 **分形布朗运动 (fBm)**，即叠加多个不同频率和振幅的噪声层（Octaves），来生成复杂的地形边缘。

$$
Noise(x, y) = \sum_{i=0}^{Octaves} \frac{1}{2^i} \cdot Perlin(2^i \cdot x, 2^i \cdot y)
$$

- **Persistence (持续度)**: 控制振幅衰减，决定地形的"粗糙度"。
- **Lacunarity (隙度)**: 控制频率增加，决定地形的"细节度"。

#### 噪声阈值映射图

```mermaid
graph LR
    Input["坐标 (x,y)"] --> NoiseFunc["Perlin Noise 计算"]
    NoiseFunc --> Value{"Noise Value v"}

    Value -->|v < 0.3| DeepWater["深水区/空洞"]
    Value -->|0.3 < v < 0.45| Floor["地面/平台"]
    Value -->|0.45 < v < 0.7| Wall["墙壁"]
    Value -->|v > 0.7| Ore["矿石/特殊资源"]

    style DeepWater fill:#e3f2fd
    style Floor fill:#fff3e0
    style Wall fill:#e8f5e9
    style Ore fill:#fce4ec
```

---

### 4. 泊松盘采样 (Poisson Disk Sampling)

用于在生成的房间中布置 **敌人、宝箱和陷阱**。它可以生成 **紧密排列但互并不重叠** 的点集，比纯随机生成（Uniform Random）更自然，避免了对象堆叠或过度聚集的问题。

**规则类**: `PoissonDiskScatterRule` (`Rules/Content`)

#### 算法核心：Bridson 算法

该算法维护两个列表：

1.  **Grid**:用于快速查询邻居，网格大小为 $\frac{r}{\sqrt{2}}$。
2.  **Active List**: 当前待处理的活跃点列表。

#### 步骤流程

1.  **初始化**: 随机选择一个初始点 $P_0$，放入 Active List。
2.  **采样**: 从 Active List 中随机选取一点 $P$。
3.  **生成候选点**: 在 $P$ 周围的圆环区域 $[r, 2r]$ 内随机生成 $k$ 个候选点。
4.  **验证**: 检查每个候选点是否与已存在的点距离 $< r$。
    - 如果距离合法，将该点加入 Active List 和 Grid。
5.  **移除**: 如果 $k$ 次尝试都失败，将 $P$ 从 Active List 移除。
6.  **重复**: 直到 Active List 为空。

#### 分布对比图 (Mermaid Scatter Simulation)

```mermaid
graph TD
    Center{采样算法对比}

    Center --> Q1["纯随机 (Uniform Random)"]
    Q1 -->|缺点| D1["团聚现象严重 / 甚至重叠"]

    Center --> Q2["高斯分布 (Gaussian)"]
    Q2 -->|缺点| D2["中心过于密集 / 边缘稀疏"]

    Center --> Q3["网格抖动 (Jittered Grid)"]
    Q3 -->|缺点| D3["人工痕迹明显 / 过于整齐"]

    Center --> Q4["泊松盘采样 (Poisson Disk)"]
    Q4 -->|优点| D4["自然且分布均匀 / 互不重叠"]

    style Q1 fill:#fce4ec
    style Q4 fill:#e8f5e9
```

> **注**: 泊松盘采样能有效平衡随机性与均匀性，是生成游戏内容的理想选择。

---

### 5. 空气柱步进采样 (Air Column Interval Sampling)

用于在垂直空旷区域智能生成跳跃平台。

**规则类**: `PlatformRule` (`Rules/Micro`)

#### 算法原理

传统的概率生成会导致平台无法跳跃或过于密集。本算法模拟重力感知，通过扫描垂直空间的"空气柱"长度来决定放置时机。

1.  **Bottom-Up 扫描**: 对每一列 x，从下往上扫描 y。
2.  **空气计数**: 遇到空瓦片时 `cnt++`，遇到实心瓦片时 `cnt = 0`。
3.  **触发阈值**: 当 `cnt >= SafeHeight` 时，尝试生成平台。
    - `SafeHeight = JumpHeight * (DoubleJump ? 2 : 1) - SafetyMargin`

#### 自适应宽度与包围盒检测

在放置平台前，算法会进行自适应探测：

1.  **向左/右延伸**: 从中心点向两边探测，直到遇到墙壁或达到 `MaxWidth`。
2.  **边界避让**: 强制平台两端与墙壁保持 **1格间距**，防止生成贴墙的尴尬平台。
3.  **AABB 碰撞**: 将拟生成的平台包围盒与已放置平台列表进行比较，如果有交集则放弃，防止平台重叠。

```mermaid
sequenceDiagram
    participant Scanner as 扫描器
    participant Logic as 判定逻辑
    participant Grid as 瓦片网格

    Scanner->>Grid: 获取列 Tile(x, y)
    alt 是空气
        Scanner->>Logic: 计数器 +1
        Logic->>Logic: Check >= SafeHeight?
        alt 达到高度
            Logic->>Grid: 探测左右空间 (Adaptive Width)
            Logic->>Logic: 物理包围盒检测 (AABB)
            Logic->>Grid: 放置平台 SetTile(Platform)
            Logic->>Scanner: 重置计数器
        end
    else 是墙壁
        Scanner->>Logic: 重置计数器 = 0
    end
```

---

### 6. 边界强制与门保护 (Border Enforcement)

**规则类**: `BorderEnforcementRule` (`Rules/Micro`)

为了防止玩家掉出地图世界，必须强制房间边缘为实心墙壁，但又不能封死房间的入口和出口。

#### 逻辑流程

1.  **计算门位置**: 读取宏观层的 `RoomNode` 连接信息，计算出所有门在微观网格中的坐标。
2.  **遍历边界**: 扫描房间矩形的四条边。
3.  **选择性填充**:
    - 如果当前边界点在 **门列表** 中 -> 跳过（保留通道）。
    - 否则 -> 设置为实心墙壁。

---

## 数据结构

### RoomNode

```csharp
public struct RoomNode
{
    public Vector2Int GridPosition;      // 网格坐标
    public RoomType Type;                // Normal, Start, End
    public LevelDoorType DoorType;       // None, Entrance, Exit
    public WallDirection RestrictedDoorSide; // 限制门的方向
    public bool IsCritical;              // 是否在关键路径上
    public BoundsInt WorldBounds;        // 世界空间边界
    public List<Vector2Int> ConnectedNeighbors; // 连接的邻居坐标
}
```

### TilemapLayer 枚举

```csharp
public enum TilemapLayer
{
    Background = 0,
    Ground = 1,
    Platform = 2,
    Decoration = 3
}
```

---

## 扩展性

### 自定义规则

只需继承 `GeneratorRuleBase` 并实现 `ExecuteAsync` 方法：

```csharp
[Serializable]
public class MyCustomRule : GeneratorRuleBase
{
    public MyCustomRule()
    {
        _ruleName = "My Custom Rule";
        _executionOrder = 55; // 插入到平台生成之后
    }

    public override async UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token)
    {
        // 实现自定义逻辑
        // context.SetTile(...)
        return true;
    }
}
```

---

## 核心组件

### 1. DungeonGenerator（地牢生成器）

**位置**: `Core/DungeonGenerator.cs`

地牢生成的主控制器，负责：

- 初始化生成上下文 (`DungeonContext`)
- 按顺序执行管线中的所有规则
- 管理生成生命周期（开始/取消/清理）
- 支持自动重试机制

**关键字段**:

| 字段                | 类型              | 描述             |
| ------------------- | ----------------- | ---------------- |
| `_pipeline`         | DungeonPipelineData | 管线配置资产     |
| `_seed`             | int               | 随机种子（-1表示系统时间） |
| `_maxRetryCount`    | int               | 生成失败时最大重试次数 |
| `_backgroundTilemap`| Tilemap           | 背景层 Tilemap   |
| `_groundTilemap`    | Tilemap           | 地面层 Tilemap   |
| `_platformTilemap`  | Tilemap           | 平台层 Tilemap   |
| `_tileConfig`       | TileConfigData    | 瓦片配置数据     |

**关键事件**:

| 事件                  | 签名                    | 描述             |
| --------------------- | ----------------------- | ---------------- |
| `OnGenerationStarted` | `Action<int>`           | 生成开始（参数为种子） |
| `OnGenerationCompleted`| `Action<bool>`         | 生成完成（参数为是否成功） |
| `OnRuleExecuted`      | `Action<string, bool>`  | 规则执行完毕（规则名，是否成功） |

### 2. DungeonContext（地牢上下文）

**位置**: `Core/DungeonContext.cs`

基于 **黑板模式 (Blackboard Pattern)** 的共享数据容器。所有规则通过此对象共享和传递数据，实现规则之间的解耦。

**配置数据**:

| 属性            | 类型              | 描述                      |
| --------------- | ----------------- | ------------------------- |
| `RNG`           | System.Random     | 随机数生成器              |
| `Seed`          | int               | 随机种子                  |
| `Token`         | CancellationToken | 取消令牌                  |
| `GridColumns`   | int               | 网格列数                  |
| `GridRows`      | int               | 网格行数                  |
| `RoomSize`      | Vector2Int        | 单房间像素尺寸            |
| `WorldOffset`   | Vector2Int        | 世界坐标偏移（多房间渲染） |

**宏观层数据**:

| 属性              | 类型                  | 描述                      |
| ----------------- | --------------------- | ------------------------- |
| `RoomNodes`       | List\<RoomNode\>      | 房间节点列表              |
| `AdjacencyMatrix` | int[,]                | 邻接矩阵                  |
| `StartRoom`       | Vector2Int            | 起始房间坐标              |
| `EndRoom`         | Vector2Int            | 终点房间坐标              |
| `CriticalPath`    | HashSet\<Vector2Int\> | 关键路径集合              |

**微观层数据**:

| 属性               | 类型   | 描述                                |
| ------------------ | ------ | ----------------------------------- |
| `BackgroundTileData`| int[] | 背景层瓦片数据（一维扁平化）        |
| `GroundTileData`   | int[]  | 地面层瓦片数据（一维扁平化）        |
| `PlatformTileData` | int[]  | 平台层瓦片数据（一维扁平化）        |
| `MapWidth`         | int    | 地图总宽度（像素）                  |
| `MapHeight`        | int    | 地图总高度（像素）                  |

**内存布局优化**:

使用一维 `int[]` 代替二维数组，索引公式为：
$$index = y \times MapWidth + x$$

这种设计带来的优势：
- **零 GC Alloc**: 避免对象数组的装箱开销
- **缓存友好**: 连续内存布局提高 CPU 缓存命中率

### 3. DungeonPipelineData（管线配置）

**位置**: `Core/DungeonPipelineData.cs`

`ScriptableObject` 资产，存储管线的所有配置和规则列表。

**配置项**:

| 属性            | 类型                    | 描述                  |
| --------------- | ----------------------- | --------------------- |
| `GridColumns`   | int                     | 网格列数 (2-10)       |
| `GridRows`      | int                     | 网格行数 (2-10)       |
| `RoomSize`      | Vector2Int              | 单房间像素尺寸        |
| `Rules`         | List\<IGeneratorRule\>  | 规则列表              |
| `EnableLogging` | bool                    | 是否启用日志          |
| `EnableVisualization` | bool              | 是否启用可视化调试    |

**计算属性**:

| 属性          | 公式                          | 描述             |
| ------------- | ----------------------------- | ---------------- |
| `TotalWidth`  | GridColumns × RoomSize.x      | 地图总宽度       |
| `TotalHeight` | GridRows × RoomSize.y         | 地图总高度       |
| `TotalRooms`  | GridColumns × GridRows        | 总房间格子数     |

---

## 规则管线

### 规则接口与基类

**IGeneratorRule 接口**:

```csharp
public interface IGeneratorRule
{
    string RuleName { get; }           // 规则名称
    bool Enabled { get; set; }         // 是否启用
    int ExecutionOrder { get; }        // 执行顺序（越小越先）
    
    UniTask<bool> ExecuteAsync(DungeonContext context, CancellationToken token);
    bool Validate(out string errorMessage);
}
```

**GeneratorRuleBase 基类**:

提供通用功能：
- 序列化字段（规则名、启用状态、执行顺序）
- 日志辅助方法（`LogInfo`, `LogWarning`, `LogError`）
- 默认的 `Validate` 实现

### 规则执行流程

```mermaid
sequenceDiagram
    participant DG as DungeonGenerator
    participant PD as PipelineData
    participant CTX as DungeonContext
    participant Rule as IGeneratorRule

    DG->>PD: GetEnabledRules()
    PD-->>DG: 按 ExecutionOrder 排序的规则列表
    DG->>CTX: 创建上下文（种子、尺寸）

    loop 遍历每个规则
        DG->>Rule: Validate()
        alt 验证失败
            Rule-->>DG: false + errorMessage
            DG->>DG: 终止生成
        else 验证通过
            DG->>Rule: ExecuteAsync(context, token)
            alt 执行成功
                Rule-->>DG: true
            else 执行失败
                Rule-->>DG: false
                DG->>DG: 触发重试机制
            end
        end
    end
```

### 完整规则执行顺序

```mermaid
graph LR
    subgraph 宏观规则 Macro
        M1[ConstrainedLayoutRule<br/>Order: 10] --> M2[BFSValidationRule<br/>Order: 20]
    end

    subgraph 微观规则 Micro
        M2 --> C1[CellularAutomataRule<br/>Order: 30]
        C1 --> C2[PathValidationRule<br/>Order: 35]
        C2 --> C3[PlatformRule<br/>Order: 40]
        C3 --> C4[BorderEnforcementRule<br/>Order: 50]
        C4 --> C5[EntranceExitRule<br/>Order: 60]
    end

    subgraph 渲染规则 Rendering
        C5 --> R1[RoomRenderRule<br/>Order: 100]
        R1 --> R2[GroundRenderRule<br/>Order: 110]
        R2 --> R3[PlatformRenderRule<br/>Order: 120]
    end

    style M1 fill:#e3f2fd
    style M2 fill:#e3f2fd
    style C1 fill:#fff3e0
    style C2 fill:#fff3e0
    style C3 fill:#fff3e0
    style C4 fill:#fff3e0
    style C5 fill:#fff3e0
    style R1 fill:#e8f5e9
    style R2 fill:#e8f5e9
    style R3 fill:#e8f5e9
```

---

## 其他核心算法详解

### 7. BFS连通性验证 (BFS Validation)

**规则类**: `BFSValidationRule` (`Rules/Macro`)

验证起点到终点的连通性，并标记关键路径。

#### 算法步骤

1. **BFS遍历**: 从起点开始广度优先搜索
2. **路径记录**: 使用 `parent` 字典记录每个节点的前驱
3. **连通性判断**: 检查终点是否被访问
4. **关键路径标记**: 从终点回溯到起点，标记所有经过的房间为 `IsCritical = true`

#### 可选功能：环路创建

通过 `_enableLoopCreation` 参数可以在关键路径之外创建额外连接，增加地图多样性。

```mermaid
graph TD
    Start[起点房间] --> BFS[BFS遍历]
    BFS --> Found{找到终点?}
    Found -->|是| Trace[回溯标记关键路径]
    Found -->|否| Fail[返回失败]
    Trace --> Loop{启用环路?}
    Loop -->|是| Extra[创建额外连接]
    Loop -->|否| Done[完成]
    Extra --> Done
```

---

### 8. 路径验证规则 (Path Validation)

**规则类**: `PathValidationRule` (`Rules/Micro`)

确保生成的地形中存在从入口到出口的可通行路径。

#### 验证逻辑

1. 对每个房间执行**洪水填充 (Flood Fill)**
2. 检查房间的所有门位置是否在同一个连通区域内
3. 如果不连通，执行 **通道挖掘** 算法连接孤立区域

---

### 9. 入口出口规则 (Entrance/Exit Rule)

**规则类**: `EntranceExitRule` (`Rules/Micro`)

处理关卡入口和出口的特殊生成逻辑。

#### 功能

- 在起始房间的指定侧面（Left/Right）挖掘入口通道
- 在终点房间的指定侧面挖掘出口通道
- 确保通道宽度足够玩家通过
- 在入口/出口位置放置特殊标记瓦片

---

## 渲染规则详解

渲染规则负责将内存中的瓦片数据 (`int[]`) 转换为 Unity Tilemap 中的实际 `Tile` 对象。

### RoomRenderRule

**执行顺序**: 100

渲染背景层（BackgroundTileData）。

### GroundRenderRule

**执行顺序**: 110

渲染地面层（GroundTileData）。

**关键特性**:
- **批量渲染**: 使用 `_batchSize` 参数控制每次批量设置的瓦片数量
- **主题支持**: 根据 `context.Theme` 选择对应的规则瓦片
- **世界偏移**: 应用 `context.WorldOffset` 支持多房间渲染

### PlatformRenderRule

**执行顺序**: 120

渲染平台层（PlatformTileData）。

---

## 配置指南

### 1. 创建管线配置

1. 右键 Project 窗口
2. Create → **Crypta Geometrica:RE/PCG程序化关卡/V4/Dungeon Pipeline**
3. 配置基础参数：
   - Grid Columns: 水平房间数 (推荐 4)
   - Grid Rows: 垂直房间数 (推荐 4)
   - Room Size: 单房间像素尺寸 (推荐 64×64)

### 2. 添加生成规则

按顺序添加以下规则：

| 顺序 | 规则类型              | 类别 | 作用                   |
| ---- | --------------------- | ---- | ---------------------- |
| 10   | ConstrainedLayoutRule | 宏观 | 约束醉汉游走生成布局   |
| 20   | BFSValidationRule     | 宏观 | 验证连通性，标记关键路径 |
| 30   | CellularAutomataRule  | 微观 | 细胞自动机生成地形     |
| 35   | PathValidationRule    | 微观 | 验证路径可通行性       |
| 40   | PlatformRule          | 微观 | 智能平台生成           |
| 50   | BorderEnforcementRule | 微观 | 边界强制与门保护       |
| 60   | EntranceExitRule      | 微观 | 入口出口处理           |
| 100  | RoomRenderRule        | 渲染 | 背景层渲染             |
| 110  | GroundRenderRule      | 渲染 | 地面层渲染             |
| 120  | PlatformRenderRule    | 渲染 | 平台层渲染             |

### 3. 场景设置

1. 创建空 GameObject，添加 `DungeonGenerator` 组件
2. 拖拽管线配置到 **Pipeline Data** 字段
3. 设置 Tilemap 引用（可使用"自动查找所有引用"按钮）
4. 设置瓦片配置数据 (`TileConfigData`)
5. 点击 **生成地牢** 按钮测试

### 4. 参数调优建议

| 参数               | 推荐值      | 说明                           |
| ------------------ | ----------- | ------------------------------ |
| 最大游走步数       | 15-20       | 越大房间数越多                 |
| 向下偏移权重       | 0.4         | 控制地牢垂直延展倾向           |
| CA迭代次数         | 6-8         | 越多地形越平滑                 |
| 初始填充率         | 0.42-0.48   | 控制洞穴密度                   |
| 玩家跳跃高度       | 8           | 需与游戏实际跳跃能力匹配       |
| 平台最小水平间距   | 4-5         | 防止平台过于密集               |

---

## 附录

### 文件结构

```
RoomGeneraton/
├── Core/
│   ├── DungeonGenerator.cs      # 主控制器
│   ├── DungeonContext.cs        # 上下文（黑板）
│   └── DungeonPipelineData.cs   # 管线配置
├── Data/
│   ├── RoomNode.cs              # 房间节点数据
│   ├── RoomType.cs              # 房间类型枚举
│   ├── SpawnCommand.cs          # 生成指令
│   ├── TileConfigData.cs        # 瓦片配置数据
│   └── TileId.cs                # 瓦片ID枚举
├── Rules/
│   ├── Abstractions/
│   │   ├── IGeneratorRule.cs    # 规则接口
│   │   └── GeneratorRuleBase.cs # 规则基类
│   ├── Macro/
│   │   ├── ConstrainedLayoutRule.cs  # 约束布局
│   │   └── BFSValidationRule.cs      # BFS验证
│   ├── Micro/
│   │   ├── CellularAutomataRule.cs   # 细胞自动机
│   │   ├── PlatformRule.cs           # 平台生成
│   │   ├── BorderEnforcementRule.cs  # 边界强制
│   │   ├── EntranceExitRule.cs       # 入口出口
│   │   └── PathValidationRule.cs     # 路径验证
│   ├── Rendering/
│   │   ├── RoomRenderRule.cs         # 背景渲染
│   │   ├── GroundRenderRule.cs       # 地面渲染
│   │   └── PlatformRenderRule.cs     # 平台渲染
│   └── Content/
│       └── (敌人/道具生成规则)
├── Utilities/
│   └── TilemapFinder.cs         # Tilemap自动查找工具
├── Editor/
│   └── (编辑器扩展)
└── docs/
    └── RoomGenerator_TechnicalPaper.md  # 本文档
```

### 枚举定义

**RoomType**:
```csharp
public enum RoomType
{
    Normal,   // 普通房间
    Start,    // 起始房间（关卡入口）
    End       // 终点房间（关卡出口）
}
```

**WallDirection**:
```csharp
public enum WallDirection
{
    None,
    Left,
    Right,
    Top,
    Bottom
}
```

**LevelDoorType**:
```csharp
public enum LevelDoorType
{
    None,          // 无特殊门
    LevelEntrance, // 关卡入口
    LevelExit      // 关卡出口
}
```

**TilemapLayer**:
```csharp
public enum TilemapLayer
{
    Background = 0,  // 背景层
    Ground = 1,      // 地面层
    Platform = 2,    // 平台层
    Decoration = 3   // 装饰层
}
```

**TileTheme**:
```csharp
public enum TileTheme
{
    Blue,    // 蓝色主题
    Red,     // 红色主题
    Yellow   // 黄色主题
}
```

---

## 与世界生成器的集成

房间生成器V4设计为可独立使用，也可被 **世界生成器V4** 调用进行多房间世界生成。

### 集成要点

1. **WorldOffset 参数**: 世界生成器通过 `context.WorldOffset` 传递像素偏移，使房间内容渲染到正确位置
2. **独立上下文**: 每个房间使用独立的 `DungeonContext`，互不干扰
3. **串行执行**: 当前版本为稳定性考虑，多房间按顺序生成

### 调用流程

```mermaid
sequenceDiagram
    participant WG as WorldGenerator
    participant WN as WorldNode
    participant DG as DungeonGenerator

    loop 对每个 WorldNode
        WG->>WN: 获取 WorldPixelOffset
        WG->>DG: GenerateDungeonAsync(seed, offset)
        DG->>DG: 创建 Context (WorldOffset = offset)
        DG->>DG: 执行管线规则
        DG-->>WG: 返回成功/失败
    end
```
