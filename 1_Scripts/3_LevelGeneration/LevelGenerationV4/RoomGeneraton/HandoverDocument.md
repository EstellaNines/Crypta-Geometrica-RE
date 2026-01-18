# V4 房间生成器 - 交接文档

> **版本**: 2.0  
> **更新日期**: 2026-01-19  
> **状态**: 最新版本

---

## 📋 文档目的

本文档用于交接 Level Generation V4 房间生成系统的最新状态，包含所有近期重构和优化内容。

---

## 🔄 版本更新摘要

### v2.0 重大更新 (2026-01-19)

| 更新项 | 类型 | 说明 |
|--------|------|------|
| RoomNode struct→class | 架构重构 | 解决值拷贝陷阱问题 |
| BorderEnforcementRule | 新增规则 | 替代 WallRenderRule，数据驱动边界 |
| Bottom-Up 平台算法 | 算法重构 | 模拟攀爬 + 包围盒碰撞检测 |
| 生成重试机制 | 功能增强 | 失败自动刷新种子重试 |

---

## 🏗️ 系统架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                      DungeonGenerator                            │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │   【重试机制】失败时自动刷新种子重新生成 (最大3次)          │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              ↓                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                DungeonPipelineData (SO)                    │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐         │  │
│  │  │ Macro   │→│ Micro   │→│ Border  │→│ Render  │         │  │
│  │  │ Rules   │ │ Rules   │ │ Rules   │ │ Rules   │         │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘         │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              ↓                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                   DungeonContext (黑板)                    │  │
│  │  ┌──────────────────┐  ┌──────────────────┐               │  │
│  │  │    宏观层数据      │  │    微观层数据      │               │  │
│  │  │  List<RoomNode>  │  │  int[] TileData  │               │  │
│  │  │   (引用类型!)     │  │  (三层分离)       │               │  │
│  │  └──────────────────┘  └──────────────────┘               │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 数据流向

```
Macro层 (房间拓扑)
    ↓
Micro层 (地形细节)
    ↓
Border层 (边界保障)  ← 【新增】
    ↓
Render层 (可视化)
```

---

## 📁 项目结构

```
LevelGenerationV4/RoomGeneraton/
├── Core/                              # 核心框架
│   ├── DungeonGenerator.cs            # 生成器主控制器 【含重试机制】
│   ├── DungeonContext.cs              # 数据黑板
│   └── DungeonPipelineData.cs         # 管线配置SO
│
├── Data/                              # 数据结构
│   ├── RoomNode.cs                    # 房间节点 【已改为class】
│   ├── RoomType.cs                    # 房间类型枚举
│   ├── TileConfig.cs                  # 瓦片配置
│   └── SpawnCommand.cs                # 生成指令
│
├── Rules/
│   ├── Abstractions/                  # 抽象层
│   │   ├── IGeneratorRule.cs          # 规则接口
│   │   └── GeneratorRuleBase.cs       # 规则基类
│   │
│   ├── Macro/                         # 宏观层规则
│   │   ├── ConstrainedLayoutRule.cs   # 醉汉游走布局
│   │   └── BFSValidationRule.cs       # BFS连通性验证
│   │
│   ├── Micro/                         # 微观层规则
│   │   ├── CellularAutomataRule.cs    # CA地形生成
│   │   ├── EntranceExitRule.cs        # 入口出口挖掘
│   │   ├── PathValidationRule.cs      # 2x2路径验证
│   │   ├── PlatformRule.cs            # 平台生成 【已重构算法】
│   │   └── BorderEnforcementRule.cs   # 边界保障 【新增】
│   │
│   └── Rendering/                     # 渲染层规则
│       ├── RoomRenderRule.cs          # 房间背景渲染
│       ├── GroundRenderRule.cs        # 地面渲染
│       ├── PlatformRenderRule.cs      # 平台渲染
│       └── WallRenderRule.cs          # 【已废弃，请删除】
│
└── Documentation/
    ├── README.md                      # 快速入门
    ├── TechnicalDoc.md                # 技术文档
    ├── APIReference.md                # API参考
    └── HandoverDocument.md            # 本文档
```

---

## 🔧 规则执行顺序 (最新)

| Order | 规则名称 | 类型 | 说明 | 状态 |
|-------|----------|------|------|------|
| 10 | ConstrainedLayoutRule | Macro | 醉汉游走生成房间拓扑 | ✅ 稳定 |
| 20 | BFSValidationRule | Macro | BFS连通性验证，标记关键路径 | ✅ 已修复 |
| 30 | CellularAutomataRule | Micro | CA算法生成自然洞穴地形 | ✅ 稳定 |
| 35 | EntranceExitRule | Micro | 挖掘起点入口和终点出口 | ✅ 已修复 |
| 36 | PathValidationRule | Micro | 2x2玩家可达性验证 | ✅ 已修复 |
| **38** | **BorderEnforcementRule** | **Micro** | **边界保障，保护门位置** | **🆕 新增** |
| 40 | PlatformRule | Micro | Bottom-Up平台生成 | ✅ 已重构 |
| 100 | RoomRenderRule | Render | 房间背景层渲染 | ✅ 稳定 |
| ~~105~~ | ~~WallRenderRule~~ | ~~Render~~ | ~~墙壁渲染~~ | ❌ **已废弃** |
| 110 | GroundRenderRule | Render | 地面层渲染 | ✅ 稳定 |
| 120 | PlatformRenderRule | Render | 平台层渲染 | ✅ 稳定 |

---

## 🔴 重要变更详解

### 1. RoomNode: struct → class

**问题背景**:
```csharp
// 旧代码 - struct 值拷贝陷阱
var node = context.RoomNodes[i];
node.IsCritical = true;
// node 是副本，原数据未被修改！
context.RoomNodes[i] = node; // 必须手动回填
```

**解决方案**: 将 `RoomNode` 从 `struct` 改为 `class`

```csharp
// 新代码 - class 引用语义
var node = context.RoomNodes[i];
node.IsCritical = true;
// 直接修改原数据，无需回填
```

**受影响文件**:
- `RoomNode.cs` - 定义修改
- `ConstrainedLayoutRule.cs` - 移除回填代码
- `BFSValidationRule.cs` - 移除回填代码
- `EntranceExitRule.cs` - `RoomNode?` → `RoomNode`
- `PathValidationRule.cs` - `RoomNode?` → `RoomNode`

---

### 2. BorderEnforcementRule (新增)

**问题背景**: 
原 `WallRenderRule` 直接操作 Tilemap，与 `GroundRenderRule` 冲突导致墙壁被清除。

**解决方案**: 
数据驱动架构 - 在 `GroundTileData` 层面强制边界为实心。

**关键特性**:
- 在 Micro 层最后执行 (Order=38)
- 保护入口/出口门位置不被封死
- 计算相邻房间门位置

**代码位置**: `Rules/Micro/BorderEnforcementRule.cs`

---

### 3. PlatformRule 算法重构

**旧算法问题**:
- Top-Down 扫描导致平台集中在高处
- 种子点距离检查无法防止膨胀后碰撞
- 缺乏梯子效应防护

**新算法特性**:

| 改动点 | 旧逻辑 | 新逻辑 |
|--------|--------|--------|
| 扫描方向 | Top-Down (从上往下) | Bottom-Up (从下往上) |
| 距离检测 | 种子点距离 | **包围盒碰撞检测 (AABB)** |
| 计数器重置 | 只在遇到实心时重置 | 放置平台后也重置 |
| 大空洞 | 无处理 | FillBigGaps 按层扫描 |

**新增方法**:
```csharp
// 预计算平台包围盒（不实际放置）
BoundsInt? CalculatePlatformBounds(context, x, y, roomBounds)

// AABB 矩形碰撞检测
bool CheckBoundsCollision(newBounds, existingBounds, hMargin, vMargin)

// 检查点是否在任何包围盒附近
bool IsPointInAnyBounds(x, y, boundsList, hMargin, vMargin)

// 根据包围盒实际放置平台
void PlacePlatformFromBounds(context, platformBounds)

// 按层扫描填充大空洞
int FillBigGaps(context, bounds, safeHeight, placedPlatformBounds)
```

---

### 4. 生成重试机制

**问题背景**: 
路径检查规则可能失败，但需要保证房间生成成功。

**解决方案**:
```csharp
// DungeonGenerator.cs
[SerializeField] private int _maxRetryCount = 3;

while (currentAttempt < _maxRetryCount && !success)
{
    int actualSeed = baseSeed + (currentAttempt - 1);
    // 清空 Tilemap
    // 重建 Context
    // 执行管线
    success = await ExecuteGenerationPipeline();
}
```

**日志输出**:
- 🟡 重试: `[DungeonGenerator] 重试生成 (2/3)，新种子=xxx`
- 🟢 成功: `[DungeonGenerator] 生成成功（尝试次数: x）`
- 🔴 失败: `[DungeonGenerator] 生成失败（已达到最大重试次数 x）`

---

## 📊 Inspector 配置参数

### DungeonGenerator

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| Pipeline | SO | - | 管线配置资产 |
| Seed | int | -1 | 随机种子，-1=系统时间 |
| **Max Retry Count** | int | 3 | 失败重试次数 (1-10) |
| Background Tilemap | Tilemap | - | 背景层引用 |
| Ground Tilemap | Tilemap | - | 地面层引用 |
| Platform Tilemap | Tilemap | - | 平台层引用 |

### PlatformRule

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| Single Jump Height | int | 8 | 单跳高度(格) |
| Enable Double Jump | bool | true | 是否支持二段跳 |
| Safety Margin | int | 2 | 安全余量 |
| Min Platform Width | int | 3 | 最小平台宽度 |
| Max Platform Width | int | 8 | 最大平台宽度 |
| Min Horizontal Spacing | int | 5 | 最小水平间距 |
| Platform Thickness | int | 1 | 平台厚度 |

### BorderEnforcementRule

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| Door Width | int | 4 | 门宽度(格) |
| Door Height | int | 4 | 门高度(格) |
| Debug Log | bool | false | 调试日志 |

---

## ⚠️ 已知问题与待办

### 待删除文件
- [ ] `WallRenderRule.cs` - 已废弃，手动删除

### 参数调优建议
- 平台过于稀疏: 降低 `Min Horizontal Spacing` 到 3-4
- 平台过于密集: 增加 `Safety Margin` 到 3-4
- 重试次数不足: 增加 `Max Retry Count` 到 5

---

## 🚀 快速入门

### 1. 场景配置
```
1. 创建 DungeonGenerator GameObject
2. 添加 DungeonGenerator 组件
3. 指定 PipelineData 资产
4. 指定 3 个 Tilemap 引用
5. 配置 TileConfig 资产
```

### 2. 代码调用
```csharp
var generator = GetComponent<DungeonGenerator>();

// 异步生成（带重试机制）
bool success = await generator.GenerateDungeonAsync(seed);

// 取消生成
generator.CancelGeneration();

// 清理数据
generator.ClearGeneration();
```

### 3. 自定义规则
```csharp
[Serializable]
public class MyRule : GeneratorRuleBase
{
    public MyRule()
    {
        _ruleName = "MyRule";
        _executionOrder = 50; // Micro层范围: 30-99
    }

    public override async UniTask<bool> ExecuteAsync(
        DungeonContext context, 
        CancellationToken token)
    {
        // 访问房间数据 (现在是引用类型!)
        foreach (var room in context.RoomNodes)
        {
            room.IsCritical = true; // 直接修改
        }
        
        // 访问瓦片数据
        int tile = context.GetTile(TilemapLayer.Ground, x, y);
        context.SetTile(TilemapLayer.Ground, x, y, 1);
        
        return true;
    }
}
```

---

## 📞 联系方式

如有问题，请联系项目负责人或查阅以下文档:
- `README.md` - 快速入门
- `TechnicalDoc.md` - 技术细节
- `APIReference.md` - API参考

---

> **最后更新**: 2026-01-19  
> **文档版本**: 2.0
