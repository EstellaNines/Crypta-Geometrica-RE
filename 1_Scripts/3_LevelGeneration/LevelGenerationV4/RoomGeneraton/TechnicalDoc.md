# V4 多房间PCG生成系统 - 技术文档

> **版本**: 1.0  
> **更新日期**: 2026-01-18  
> **状态**: 已完成

---

## 1. 系统概述

V4 是一个基于规则管线的多房间程序化内容生成（PCG）系统，采用"数据-表现"分离架构，支持异步生成和模块化扩展。

### 1.1 核心特性

| 特性 | 说明 |
|------|------|
| **规则管线** | 可配置的规则执行顺序，支持热插拔 |
| **异步生成** | 基于UniTask的异步架构，支持取消令牌 |
| **数据分离** | 宏观层/微观层数据独立，渲染层解耦 |
| **可视化配置** | Odin Inspector支持，Inspector可视化编辑 |

### 1.2 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    DungeonGenerator                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              DungeonPipelineData (SO)                │   │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐   │   │
│  │  │ Rule 1  │→│ Rule 2  │→│ Rule 3  │→│ Rule N  │   │   │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘   │   │
│  └─────────────────────────────────────────────────────┘   │
│                           ↓                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                 DungeonContext (黑板)                │   │
│  │  ┌──────────────┐  ┌──────────────┐                 │   │
│  │  │   宏观层数据   │  │   微观层数据   │                 │   │
│  │  │  RoomNodes   │  │  TileData    │                 │   │
│  │  │  Adjacency   │  │  MapSize     │                 │   │
│  │  └──────────────┘  └──────────────┘                 │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 目录结构

```
LevelGenerationV4/
├── Core/                          # 核心框架
│   ├── DungeonGenerator.cs        # 生成器执行器
│   ├── DungeonContext.cs          # 数据黑板
│   └── DungeonPipelineData.cs     # 管线配置SO
├── Rules/
│   ├── Abstractions/              # 抽象层
│   │   ├── IGeneratorRule.cs      # 规则接口
│   │   └── GeneratorRuleBase.cs   # 规则基类
│   ├── Macro/                     # 宏观层规则
│   │   ├── ConstrainedLayoutRule.cs   # 醉汉游走布局
│   │   └── BFSValidationRule.cs       # BFS连通性验证
│   ├── Micro/                     # 微观层规则
│   │   ├── CellularAutomataRule.cs    # CA地形生成
│   │   ├── EntranceExitRule.cs        # 入口出口挖掘
│   │   ├── PathValidationRule.cs      # 路径验证
│   │   └── PlatformRule.cs            # 平台生成
│   └── Rendering/                 # 渲染层规则
│       ├── RoomRenderRule.cs          # 房间背景渲染
│       ├── GroundRenderRule.cs        # 地面渲染
│       ├── WallRenderRule.cs          # 墙壁渲染
│       └── PlatformRenderRule.cs      # 平台渲染
├── Data/                          # 数据结构
│   ├── RoomNode.cs                # 房间节点
│   ├── TileConfig.cs              # 瓦片配置
│   └── TilemapLayer.cs            # Tilemap层枚举
└── Editor/                        # 编辑器扩展
    └── DungeonGeneratorEditor.cs  # 自定义Inspector
```

---

## 3. 规则执行顺序

| 顺序 | 规则名称 | 类型 | 说明 |
|------|----------|------|------|
| 10 | ConstrainedLayoutRule | 宏观 | 醉汉游走生成房间拓扑 |
| 20 | BFSValidationRule | 宏观 | 验证连通性，标记关键路径 |
| 30 | CellularAutomataRule | 微观 | CA算法生成地形 |
| 35 | EntranceExitRule | 微观 | 挖掘入口出口区域 |
| 36 | PathValidationRule | 微观 | 2x2玩家路径验证 |
| 40 | PlatformRule | 微观 | 空气柱步进采样生成平台 |
| 100 | RoomRenderRule | 渲染 | 渲染房间背景 |
| 105 | WallRenderRule | 渲染 | 渲染墙壁边缘 |
| 110 | GroundRenderRule | 渲染 | 渲染地面 |
| 120 | PlatformRenderRule | 渲染 | 渲染平台 |

---

## 4. 核心算法

### 4.1 醉汉游走 (Drunkard Walk)

**用途**: 生成房间拓扑结构

**算法流程**:
1. 从顶行随机位置开始
2. 带权重随机游走（向下偏好）
3. 记录访问的格子作为房间
4. 选择最远节点作为终点

**关键参数**:
| 参数 | 默认值 | 说明 |
|------|--------|------|
| MaxSteps | 20-30 | 最大游走步数 |
| DownwardBias | 0.4 | 向下偏好权重 |
| SidewaysBias | 0.3 | 横向偏好权重 |
| MinRooms | 8 | 最少房间数 |

### 4.2 细胞自动机 (Cellular Automata)

**用途**: 生成自然洞穴地形

**算法流程**:
1. 随机初始化网格（保留门位置为空）
2. 迭代应用CA规则
3. 强制边界为实心（门除外）
4. 移除孤立格平滑地形

**CA规则**:
- 邻居 >= BirthLimit → 变为实心
- 邻居 < DeathLimit → 变为空

**关键参数**:
| 参数 | 默认值 | 说明 |
|------|--------|------|
| Iterations | 8 | 迭代次数 |
| FillProbability | 0.45 | 初始填充率 |
| BirthLimit | 4 | 出生阈值 |
| DeathLimit | 3 | 死亡阈值 |

### 4.3 空气柱步进采样 (Air Column Interval Sampling)

**用途**: 在垂直空旷区域生成平台

**算法流程**:
1. 从上往下垂直扫描每列
2. 统计连续空气格数量
3. 当空气格 >= 安全高度 且 空气格 % 间隔 == 0 时
4. 尝试放置自适应宽度平台

**关键参数**:
| 参数 | 默认值 | 说明 |
|------|--------|------|
| JumpHeight | 8 | 单跳高度 |
| DoubleJump | true | 支持二段跳 |
| SafetyMargin | 2 | 安全余量 |
| PlatformInterval | 10 | 平台间隔 |

---

## 5. 数据结构

### 5.1 RoomNode

```csharp
public struct RoomNode
{
    public Vector2Int GridPosition;      // 网格坐标
    public RoomType Type;                 // Normal/Start/End
    public LevelDoorType DoorType;        // None/Entrance/Exit
    public WallDirection RestrictedDoorSide; // 侧向门位置
    public bool IsCritical;               // 是否关键路径
    public BoundsInt WorldBounds;         // 世界边界
    public List<Vector2Int> ConnectedNeighbors; // 连接邻居
}
```

### 5.2 DungeonContext

```csharp
public class DungeonContext : IDisposable
{
    // 宏观层数据
    public List<RoomNode> RoomNodes;
    public RoomNode? StartRoom;
    public RoomNode? EndRoom;
    
    // 微观层数据
    public int[] BackgroundTileData;
    public int[] GroundTileData;
    public int[] PlatformTileData;
    
    // 辅助方法
    public int GetTile(TilemapLayer layer, int x, int y);
    public void SetTile(TilemapLayer layer, int x, int y, int value);
}
```

---

## 6. 配置指南

### 6.1 Unity编辑器配置

1. **创建管线资产**
   - 右键 → Create → Dungeon → Pipeline Data

2. **配置规则列表**
   - 在Inspector中添加规则
   - 调整规则参数
   - 规则按ExecutionOrder自动排序

3. **配置Tilemap**
   - Ground层: 实心地面
   - Platform层: 单向平台
   - Background层: 房间背景

### 6.2 Layer配置

| Layer | 用途 |
|-------|------|
| Ground | 实心地面碰撞 |
| Platform | 单向平台（需PlatformEffector2D） |
| Player | 玩家角色 |
| Enemy | 敌人角色 |

### 6.3 单向平台配置

**Platform Tilemap需要**:
- `Tilemap Collider 2D` (Used By Composite: ✓)
- `Composite Collider 2D` (Used By Effector: ✓)
- `Platform Effector 2D`:
  - Use One Way: ✓
  - Surface Arc: 180
  - Rotational Offset: 180
  - Collider Mask: Player层

---

## 7. 扩展指南

### 7.1 创建新规则

```csharp
[Serializable]
public class MyCustomRule : GeneratorRuleBase
{
    public MyCustomRule()
    {
        _ruleName = "MyCustomRule";
        _executionOrder = 50; // 设置执行顺序
    }

    public override async UniTask<bool> ExecuteAsync(
        DungeonContext context, 
        CancellationToken token)
    {
        // 实现规则逻辑
        LogInfo("执行自定义规则...");
        
        // 访问/修改上下文数据
        foreach (var room in context.RoomNodes)
        {
            // 处理每个房间
        }
        
        return true;
    }

    public override bool Validate(out string errorMessage)
    {
        errorMessage = string.Empty;
        return true;
    }
}
```

### 7.2 规则最佳实践

1. **执行顺序**: 宏观(10-29) → 微观(30-99) → 渲染(100+)
2. **数据访问**: 使用 `context.GetTile()`/`SetTile()` 访问瓦片数据
3. **异步支持**: 长时间操作使用 `await UniTask.Yield(token)`
4. **日志输出**: 使用 `LogInfo()`/`LogWarning()`/`LogError()`

---

## 8. 性能优化

### 8.1 内存优化

- 使用一维数组存储瓦片数据
- 双缓冲避免频繁分配
- 实现 `IDisposable` 清理资源

### 8.2 CPU优化

- CA使用双缓冲避免额外分配
- 批量设置Tilemap (`SetTilesBlock`)
- 异步生成避免主线程阻塞

### 8.3 推荐配置

| 地图尺寸 | CA迭代 | 预期耗时 |
|----------|--------|----------|
| 64x64 | 5 | <100ms |
| 128x128 | 8 | <500ms |
| 256x256 | 10 | <2s |

---

## 9. 故障排除

### 9.1 常见问题

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 房间不连通 | CA覆盖了门位置 | 检查门位置处理逻辑 |
| 玩家卡在平台里 | 碰撞恢复时机 | 使用离开检测而非固定时间 |
| 平台无法穿透 | Effector配置错误 | 检查Rotational Offset |
| 生成失败 | 布局验证不通过 | 增加MaxRetries |

### 9.2 调试技巧

1. 启用规则的调试日志
2. 使用Gizmos可视化房间布局
3. 检查Console输出的规则执行顺序

---

