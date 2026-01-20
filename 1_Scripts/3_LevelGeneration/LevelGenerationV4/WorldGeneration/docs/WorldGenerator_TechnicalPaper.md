# 世界生成器 V4 技术文档

## 目录

1. [系统概述](#系统概述)
2. [架构设计](#架构设计)
3. [核心组件](#核心组件)
4. [规则管线](#规则管线)
5. [算法详解](#算法详解)
6. [数据流](#数据流)
7. [使用指南](#使用指南)

---

## 系统概述

世界生成器V4是一个基于规则管线的程序化世界生成系统，用于在2D网格中随机布局房间并调用房间生成器V4生成具体房间内容。

### 设计理念

- **规则管线（Rule Pipeline）**：将生成过程分解为独立的规则，按顺序串行执行
- **黑板模式（Blackboard Pattern）**：通过 `WorldContext` 共享数据，解耦规则之间的依赖
- **异步执行**：基于 UniTask 的异步架构，支持取消和中断

### 技术栈

| 技术           | 用途       |
| -------------- | ---------- |
| UniTask        | 异步编程   |
| Odin Inspector | 编辑器扩展 |
| Unity Tilemap  | 地图渲染   |

---

## 架构设计

### 系统架构图

```mermaid
graph TB
    subgraph 世界生成器
        WG[WorldGenerator] --> WP[WorldPipelineData]
        WG --> WC[WorldContext]

        WP --> R1[RandomPlacementRule]
        WP --> R2[CoordinateCalcRule]
        WP --> R3[RoomGenerationRule]
    end

    subgraph 房间生成器V4
        R3 --> DG[DungeonGenerator]
        DG --> DP[DungeonPipelineData]
    end

    subgraph 数据层
        WC --> WN[WorldNode]
        WC --> OG[OccupancyGrid]
    end
```

### 类关系图

```mermaid
classDiagram
    class WorldGenerator {
        -WorldPipelineData _pipelineData
        -WorldContext _context
        +GenerateWorldAsync()
        +CancelGeneration()
        +ClearGeneration()
    }

    class WorldPipelineData {
        -int _roomCount
        -Vector2Int _roomPixelSize
        -List~IWorldRule~ _rules
        +GetEnabledRules()
        +ValidateAll()
    }

    class WorldContext {
        +int GridSize
        +int RoomCount
        +bool[,] OccupancyGrid
        +List~WorldNode~ Nodes
        +System.Random RNG
        +IsInBounds()
        +IsOccupied()
        +AddNode()
    }

    class WorldNode {
        +Vector2Int GridPosition
        +Vector2Int WorldPixelOffset
        +int RoomSeed
        +bool IsGenerated
        +CalculateWorldOffset()
    }

    class IWorldRule {
        <<interface>>
        +string RuleName
        +bool Enabled
        +int ExecutionOrder
        +ExecuteAsync()
        +Validate()
    }

    class WorldRuleBase {
        <<abstract>>
        #string _ruleName
        #bool _enabled
        #int _executionOrder
        +LogInfo()
        +LogWarning()
        +LogError()
    }

    WorldGenerator --> WorldPipelineData
    WorldGenerator --> WorldContext
    WorldContext --> WorldNode
    WorldPipelineData --> IWorldRule
    WorldRuleBase ..|> IWorldRule
    RandomPlacementRule --|> WorldRuleBase
    CoordinateCalcRule --|> WorldRuleBase
    RoomGenerationRule --|> WorldRuleBase
```

---

## 核心组件

### 1. WorldGenerator（世界生成器）

**位置**: `Core/WorldGenerator.cs`

世界生成的主控制器，负责：

- 初始化生成上下文
- 按顺序执行规则管线
- 管理生成生命周期（开始/取消/清除）

**关键字段**:

| 字段                | 类型              | 描述           |
| ------------------- | ----------------- | -------------- |
| `_pipelineData`     | WorldPipelineData | 管线配置资产   |
| `_dungeonGenerator` | DungeonGenerator  | 房间生成器引用 |
| `_context`          | WorldContext      | 运行时上下文   |
| `_seed`             | int               | 随机种子       |

### 2. WorldContext（世界上下文）

**位置**: `Core/WorldContext.cs`

基于黑板模式的共享数据容器，在规则执行过程中传递数据。

**核心属性**:

| 属性            | 类型              | 描述                      |
| --------------- | ----------------- | ------------------------- |
| `GridSize`      | int               | 网格尺寸（RoomCount - 1） |
| `RoomCount`     | int               | 目标房间数量              |
| `RoomPixelSize` | Vector2Int        | 单房间像素尺寸            |
| `OccupancyGrid` | bool[,]           | 占用网格                  |
| `Nodes`         | List\<WorldNode\> | 世界节点列表              |
| `RNG`           | System.Random     | 随机数生成器              |
| `Seed`          | int               | 随机种子                  |

---

## 规则管线

### 规则执行顺序

```mermaid
graph LR
    A[RandomPlacementRule<br/>Order: 10] --> B[CoordinateCalcRule<br/>Order: 20]
    B --> C[RoomGenerationRule<br/>Order: 100]

    style A fill:#e1f5fe
    style B fill:#fff3e0
    style C fill:#e8f5e9
```

---

## 算法详解

### 1. 稀疏随机放置算法 (Sparse Random Placement)

**规则类**: `RandomPlacementRule`  
**位置**: `Rules/Layout/RandomPlacementRule.cs`

该算法通过多轮随机尝试和严格的邻居约束，在网格中生成分布稀疏、互不相邻的房间布局。

#### 核心逻辑

1.  **初始化可用池**: 收集网格内所有坐标 `(x, y)`。
2.  **多轮尝试**:
    - 每轮打乱可用池顺序 (`ShuffleList`)。
    - 遍历每个位置，尝试放置房间。
3.  **约束检查**:
    - **随机阈值**: `Random.value > _spawnThreshold` 才允许放置（增加随机稀疏性）。
    - **隔离约束**: 目标位置周围 **8个方向**（正交+对角）必须全部为空。

#### 隔离约束图解

```mermaid
graph TD
    subgraph 约束检查
    C[Center<br/>目标位置]

    NT[Top<br/>正交] --- C
    NB[Bottom<br/>正交] --- C
    NL[Left<br/>正交] --- C
    NR[Right<br/>正交] --- C

    ND1[Top-Left<br/>对角] --- C
    ND2[Top-Right<br/>对角] --- C
    ND3[Bottom-Left<br/>对角] --- C
    ND4[Bottom-Right<br/>对角] --- C

    style C fill:#4CAF50,stroke:#333,stroke-width:2px
    style NT fill:#FF5252,stroke:#333
    style NB fill:#FF5252,stroke:#333
    style NL fill:#FF5252,stroke:#333
    style NR fill:#FF5252,stroke:#333
    style ND1 fill:#FF5252,stroke:#333
    style ND2 fill:#FF5252,stroke:#333
    style ND3 fill:#FF5252,stroke:#333
    style ND4 fill:#FF5252,stroke:#333
    end
```

> **注意**: 红色区域必须全部为空，中心位置才能放置房间。

#### 强制填充机制

如果经过 `_maxRounds` 轮次后仍未达到目标 `RoomCount`，算法进入**强制模式**：忽略随机阈值，仅保留隔离约束，按顺序遍历网格填补空位，确保总是尽可能生成房间。

---

### 2. 坐标映射计算

**规则类**: `CoordinateCalcRule`

将抽象的网格坐标 `GridPosition` 转换为实际的像素坐标 `WorldPixelOffset`。此偏移量将传递给房间生成器，用于决定Tilemap绘制的基准点。

$$
\begin{cases}
WorldX = GridX \times RoomPixelWidth \\
WorldY = GridY \times RoomPixelHeight
\end{cases}
$$

---

### 3. 带重试的房间生成管线

**规则类**: `RoomGenerationRule`

负责协调从抽象节点到具体Tilemap内容的生成。

#### 算法流程

```mermaid
sequenceDiagram
    participant Rule as RoomGenerationRule
    participant Node as WorldNode
    participant DG as DungeonGenerator

    loop 对每个 WorldNode
        Note over Rule, DG: 尝试生成最多 MaxRetryCount 次
        Rule->>DG: GenerateDungeonAsync(seed, offset)

        alt 生成成功
            DG-->>Rule: Success
            Rule->>Node: 标记 IsGenerated = true
            Rule->>Node: 提取 Entrance/Exit 坐标
        else 生成失败
            DG-->>Rule: Fail
            Rule->>DG: 使用新 Seed 重试
        end

        Rule->>Rule: 记录最终状态
    end
```

**关键点**:

- **串行执行**: 目前版本为了稳定性，采用逐个房间生成的方式。
- **独立上下文**: 每个房间有自己的 `DungeonContext`，互不干扰。
- **偏移绘制**: `DungeonGenerator` 接收 `worldOffset` 参数，直接在正确的Tilemap位置绘制瓦片。

---

## 配置指南

### 1. 创建管线配置

1. 右键 Project 窗口
2. Create → **Crypta Geometrica:RE/PCG程序化关卡/V4/World Pipeline**
3. 配置参数：
   - Room Count: 目标房间数量
   - Room Pixel Size: 单房间像素尺寸
   - Dungeon Pipeline: 引用房间生成管线

### 2. 添加生成规则

在规则列表中按顺序添加：

| 顺序 | 规则类型            | 作用     |
| ---- | ------------------- | -------- |
| 1    | RandomPlacementRule | 随机布局 |
| 2    | CoordinateCalcRule  | 坐标计算 |
| 3    | RoomGenerationRule  | 房间生成 |

### 3. 场景设置

1. 创建空对象，添加 `WorldGenerator` 组件
2. 拖拽管线配置到 Pipeline Data 字段
3. 拖拽 DungeonGenerator 引用
4. 点击 "生成世界" 按钮

---

## 附录

### 文件结构

```
WorldGeneration/
├── Core/
│   ├── WorldGenerator.cs      # 主控制器
│   ├── WorldContext.cs        # 上下文（黑板）
│   └── WorldPipelineData.cs   # 管线配置
├── Data/
│   └── WorldNode.cs           # 节点数据
└── Rules/
    ├── Abstractions/
    │   ├── IWorldRule.cs      # 规则接口
    │   └── WorldRuleBase.cs   # 规则基类
    ├── Layout/
    │   ├── RandomPlacementRule.cs  # 随机放置
    │   └── CoordinateCalcRule.cs   # 坐标计算
    └── Generation/
        └── RoomGenerationRule.cs   # 房间生成
```
