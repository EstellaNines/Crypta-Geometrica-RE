# V4 多房间PCG生成系统 - 方案对齐文档 (ALIGNMENT)

> **文档版本**: 1.0  
> **创建日期**: 2026-01-17  


---

## 1. 项目现状分析

### 1.1 V3版本问题总结

| 问题类型 | 具体问题 | 根本原因 |
|----------|----------|----------|
| **架构问题** | 单体脚本（GrayboxLevelGenerator.cs ~96KB） | 逻辑高度耦合，无法独立测试与扩展 |
| **架构问题** | MultiGridLevelManager.cs ~118KB | 职责不清晰，宏观/微观混杂 |
| **功能问题** | 多平台生成不合理 | 光栅化函数与拓扑分析未解耦 |
| **性能问题** | 主线程同步执行 | 无异步支持，大地图生成卡顿 |
| **扩展问题** | 新增地形算法需修改核心代码 | 缺乏策略模式支持 |

### 1.2 现有依赖环境

| 依赖项 | 状态 | 备注 |
|--------|------|------|
| **Odin Inspector** | ✅ 已安装 | Sirenix目录存在，支持多态序列化UI |
| **UniTask** | ✅ 已安装 (v2.5.10) | 异步计算框架，支持线程池切换 |
| **Tilemap** | ✅ Unity内置 | 渲染层基础 |

### 1.3 可复用的V3资产

| 资产 | 复用价值 | 备注 |
|------|----------|------|
| `LevelShape.cs` | 高 | 关卡形状定义可直接迁移 |
| `RoomNode.cs` | 中 | 需重构为纯数据结构 |
| `RoomType.cs` | 高 | 枚举定义可直接使用 |
| `GrayboxTilemapLayers.cs` | 高 | Tilemap层配置可复用 |
| `TerrainArchetype` 枚举 | 中 | 可作为地形原语参考 |

---

## 2. V4方案可行性评估

### 2.1 总体可行性评分：**高 (8.5/10)**

| 评估维度 | 评分 | 说明 |
|----------|------|------|
| **技术可行性** | 9/10 | 所有技术方案均有成熟实现 |
| **依赖兼容性** | 8/10 | Odin已有，需引入UniTask |
| **工作量评估** | 7/10 | 重构工作量大，但架构清晰 |
| **风险可控性** | 9/10 | 可渐进式迁移，V3保留作为对照 |
| **性能收益** | 9/10 | 异步+时间切片=零卡顿 |

### 2.2 核心技术方案评估

#### 2.2.1 SerializeReference + 策略模式

**可行性：高**
- Odin Inspector 已安装，完美支持 `[SerializeReference]` 的多态UI
- 无需为每个算法创建独立SO文件
- 设计师可在Inspector中拖拽组合生成流程

**风险点：**
- 深拷贝需要自定义实现
- 版本迭代时需注意序列化兼容性

#### 2.2.2 UniTask 异步管线

**可行性：高**
- Unity Package Manager 一键安装
- 与现有代码零冲突
- 支持 `SwitchToThreadPool()` / `SwitchToMainThread()`

**风险点：**
- 团队成员需要学习异步编程范式
- 调试相对复杂（需要异步堆栈追踪）

#### 2.2.3 时间切片物理烘焙

**可行性：高**
- `CompositeCollider2D.GenerateGeometry()` API 已验证可用
- 分帧策略简单有效

**风险点：**
- 需要将 Generation Type 设为 Manual
- 需要精细控制每帧处理的Chunk数量

---

## 3. V4架构设计方案

### 3.1 核心架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                     DungeonPipelineData (SO)                     │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ [SerializeReference] List<IGeneratorRule> GenerationRules   │ │
│  │   ├── MacroLayoutRule (醉汉游走)                             │ │
│  │   ├── ConnectivityValidationRule (BFS校验)                  │ │
│  │   ├── CellularAutomataRule (元胞自动机)                      │ │
│  │   ├── PoissonScatterRule (泊松盘采样)                        │ │
│  │   └── TilemapRenderRule (渲染输出)                          │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     DungeonContext (黑板数据)                    │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │ RoomNodes    │ │ Adjacency    │ │ TileMapData              │ │
│  │ List<Room>   │ │ int[,]       │ │ Dictionary<V2Int,TileId> │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │ RNG (Seed)   │ │ PhysicsChunks│ │ PendingSpawns            │ │
│  │ System.Random│ │ HashSet<Rect>│ │ List<SpawnCommand>       │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  DungeonGenerator (执行器)                       │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ async UniTask GenerateDungeonAsync(CancellationToken)       │ │
│  │   1. 主线程：读取配置，初始化Context                          │ │
│  │   2. 线程池：执行所有Rules的计算逻辑                          │ │
│  │   3. 主线程：时间切片渲染Tilemap                             │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 目录结构设计

```
Assets/1_Scripts/3_LevelGeneration/
├── LevelGenerationV3/           # 保留，作为参考
│   └── ...
│
└── LevelGenerationV4/           # 新版本
    ├── Core/
    │   ├── DungeonContext.cs           # 黑板数据
    │   ├── DungeonGenerator.cs         # 异步执行器
    │   └── DungeonPipelineData.cs      # SO配置容器
    │
    ├── Rules/
    │   ├── Abstractions/
    │   │   └── IGeneratorRule.cs       # 规则接口
    │   ├── Macro/
    │   │   ├── DrunkardWalkRule.cs     # 醉汉游走
    │   │   └── BFSValidationRule.cs    # 连通性校验
    │   ├── Micro/
    │   │   ├── CellularAutomataRule.cs # 元胞自动机
    │   │   ├── PerlinNoiseRule.cs      # 柏林噪声
    │   │   └── PoissonScatterRule.cs   # 泊松盘采样
    │   └── Rendering/
    │       ├── TilemapRenderRule.cs    # Tilemap渲染
    │       └── PhysicsSlicingRule.cs   # 物理时间切片
    │
    ├── Data/
    │   ├── RoomNode.cs                 # 房间节点（纯数据）
    │   ├── TileId.cs                   # 瓦片ID枚举
    │   └── SpawnCommand.cs             # 生成指令
    │
    ├── Utilities/
    │   ├── ArrayPoolHelper.cs          # 数组池工具
    │   ├── SpatialHash.cs              # 空间哈希
    │   └── FastRandom.cs               # 轻量RNG
    │
    └── Editor/
        └── DungeonPipelineEditor.cs    # 可视化编辑器
```

### 3.3 核心接口定义

```csharp
/// <summary>
/// 生成规则通用接口
/// </summary>
public interface IGeneratorRule
{
    string RuleName { get; }
    bool Enabled { get; set; }
    
    /// <summary>
    /// 异步执行生成逻辑
    /// </summary>
    UniTask ExecuteAsync(DungeonContext context, CancellationToken token);
}

/// <summary>
/// 黑板数据容器
/// </summary>
public class DungeonContext
{
    // 宏观数据
    public List<RoomNode> RoomNodes { get; set; }
    public int[,] AdjacencyMatrix { get; set; }
    public Vector2Int StartRoom { get; set; }
    public Vector2Int EndRoom { get; set; }
    
    // 微观数据
    public int[] TileMapData { get; set; }  // 一维扁平化
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    
    // 共享资源
    public System.Random RNG { get; set; }
    public CancellationToken Token { get; set; }
    
    // 渲染队列
    public HashSet<BoundsInt> DirtyChunks { get; set; }
    public List<SpawnCommand> PendingSpawns { get; set; }
}
```

---

## 4. 实施路线图

### 4.1 阶段划分

| 阶段 | 目标 | 预估工时 | 优先级 |
|------|------|----------|--------|
| **Phase 0** | ~~环境准备：安装UniTask~~ | ~~0.5h~~ | ✅ 已完成 |
| **Phase 1** | 核心框架：接口+Context+Pipeline | 4h | P0 |
| **Phase 2** | 宏观规则：醉汉游走+BFS校验 | 6h | P0 |
| **Phase 3** | 微观规则：元胞自动机+噪声 | 8h | P1 |
| **Phase 4** | 渲染管线：批量Tile+时间切片物理 | 4h | P0 |
| **Phase 5** | 内容规则：泊松采样+实体生成 | 4h | P2 |
| **Phase 6** | 编辑器工具：可视化调试 | 4h | P2 |

**总计：约 30 小时**（Phase 0已完成）

### 4.2 Phase 1 详细任务

| 序号 | 任务 | 输入 | 输出 | 验收标准 |
|------|------|------|------|----------|
| 1.1 | 创建V4目录结构 | 无 | 目录树 | 结构与设计一致 |
| 1.2 | 实现 `IGeneratorRule` 接口 | 无 | .cs文件 | 编译通过 |
| 1.3 | 实现 `DungeonContext` | 无 | .cs文件 | 包含所有必要字段 |
| 1.4 | 实现 `DungeonPipelineData` SO | Odin | .cs文件 | Inspector可添加规则 |
| 1.5 | 实现 `DungeonGenerator` 执行器 | UniTask | .cs文件 | 异步执行空管线 |

---

## 5. 技术决策点

### 5.1 需用户确认的决策

| 决策项 | 选项A | 选项B | 建议 |
|--------|-------|-------|------|
| ~~**UniTask安装方式**~~ | ~~Package Manager~~ | ~~手动导入~~ | ✅ 已手动安装 v2.5.10 |
| **V3代码处理** | 删除 | 保留为参考 | 保留（便于对照） |
| **网格尺寸** | 固定4x4 | 可配置NxM | 可配置（更灵活） |
| **异步粒度** | 每个Rule异步 | 整体异步 | 每个Rule异步（更可控） |

### 5.2 已确定的技术选型

| 技术点 | 选型 | 理由 |
|--------|------|------|
| **多态序列化** | `[SerializeReference]` + Odin | 项目已有Odin，UI友好 |
| **异步框架** | UniTask | 零GC，线程切换API清晰 |
| **随机数** | `System.Random` | 足够，后续可升级为PCG-Random |
| **地图存储** | 一维int[] | 缓存友好，索引公式简单 |
| **物理烘焙** | Manual + 时间切片 | 避免帧率尖峰 |

---

## 6. 风险与缓解

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| ~~UniTask引入冲突~~ | ~~低~~ | ~~高~~ | ✅ 已安装，无冲突 |
| 序列化版本兼容 | 中 | 中 | 添加版本号字段 |
| 异步调试困难 | 中 | 低 | 使用UniTask的调试工具 |
| 性能不达预期 | 低 | 中 | 预留Burst优化接口 |

---

## 7. 验收标准

### 7.1 Phase 1 验收

- [x] UniTask 安装成功，无编译错误 (v2.5.10)
- [ ] `DungeonPipelineData` 可在Inspector中添加/删除规则
- [ ] `DungeonGenerator.GenerateDungeonAsync()` 可异步执行空管线
- [ ] 日志输出确认线程切换正常

### 7.2 完整验收

- [ ] 生成1000x1000地图无卡顿
- [ ] 物理烘焙分帧执行，单帧耗时<16ms
- [ ] 设计师可无代码组合新地形配方
- [ ] 种子确定性：相同种子=相同地图

---

## 8. 待确认问题

**请用户审核以下问题：**

1. ~~**UniTask安装**~~：✅ 已手动安装 v2.5.10
2. **V3代码**：是否保留V3代码作为参考，还是删除？
3. **优先级**：是否同意Phase 1-4为P0优先实施？
4. **网格尺寸**：是否需要支持可配置的NxM网格，还是固定4x4？

---

**文档结束 - 等待用户审核**
