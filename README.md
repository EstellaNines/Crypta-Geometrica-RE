# Level Generation V3 - Topology-Based Terrain System

## 概述

Level Generation V3 是基于**拓扑原语的多地形分块生成系统**，采用结构化的4x4网格分块方法，替代了原有的高斯堆积造山法和细胞自动机平滑算法。

---

## 核心架构

```
GrayboxLevelGenerator (房间生成器)
        ↓
MultiGridLevelManager (多房间生成器)
```

---

## GrayboxLevelGenerator 功能列表

### ✅ 已实现功能

| 功能模块 | 描述 | 状态 |
|---------|------|------|
| **4x4网格系统** | 将关卡划分为4x4的Chunk网格 | ✅ |
| **关键路径生成** | 使用醉汉游走算法生成玩家必经路径 | ✅ |
| **TerrainArchetype 枚举** | 17种地形原语类型定义 | ✅ |
| **拓扑分析** | 分析每个Chunk的连接关系 | ✅ |
| **确定性光栅化** | 使用数学函数填充地形 | ✅ |
| **安全修正** | 确保路径连通性 | ✅ |
| **出入口系统** | Start/Exit 房间标记和渲染 | ✅ |
| **特殊区域** | Shop/Boss 房间支持 | ✅ |
| **Tilemap渲染** | 多层Tilemap输出 | ✅ |

### TerrainArchetype 类型

```csharp
public enum TerrainArchetype
{
    Solid,           // 实心岩石（非路径区域）
    Open,            // 完全空旷（高空区域）
    Corridor,        // 水平直通隧道
    Shaft,           // 垂直竖井
    Corner_BL,       // 拐角：左通 & 下通
    Corner_TL,       // 拐角：左通 & 上通
    Corner_BR,       // 拐角：右通 & 下通
    Corner_TR,       // 拐角：右通 & 上通
    Stairs_Pos,      // 正向阶梯 (/)
    Stairs_Neg,      // 负向阶梯 (\)
    Mountain_Base,   // 山体基座
    Mountain_Peak,   // 山峰
    Platforms_Sparse,// 稀疏平台
    T_Junction_LRD,  // T型交叉 - 左右下
    T_Junction_LRU,  // T型交叉 - 左右上
    Cross_Junction,  // 十字交叉
    Landing_Zone     // 着陆区
}
```

### 4步生成流程

1. **InitializeTerrainMap()** - 初始化地形数据数组
2. **AnalyzeGridTopology()** - 拓扑分析并分配原语类型
3. **RasterizeAllChunks()** - 确定性光栅化填充
4. **CarvePathConnections()** - 安全修正确保连通

---

## MultiGridLevelManager 功能列表

### ✅ 已实现功能

| 功能模块 | 描述 | 状态 |
|---------|------|------|
| **多网格布局** | 支持1-8个独立关卡区域 | ✅ |
| **随机位置分布** | 在指定区域内随机放置网格 | ✅ |
| **碰撞检测** | 防止网格重叠 | ✅ |
| **独立种子** | 每个网格使用独立随机种子 | ✅ |
| **特殊区域概率** | 中位数网格和其他网格不同概率 | ✅ |
| **调试显示** | 网格边界和出入口标记可视化 | ✅ |
| **形状预设** | 支持多种LevelShape预设 | ✅ |

---

## ⚠️ 已知问题

### 1. 多平台问题 (未解决)

**问题描述：**
新的拓扑原语系统生成的地形中，某些区域仍然出现过多的平台或平台分布不合理的情况。

**可能原因：**
- `Platforms_Sparse` 原语的光栅化函数需要调优
- 部分 Archetype 的平台生成逻辑可能与预期不符
- 需要更精细的拓扑分析来决定哪些区域需要平台

**临时解决方案：**
已禁用旧版 `DrawPlatforms()` 调用，但新系统内置的平台生成可能仍需调整。

**相关代码位置：**
- `GrayboxLevelGenerator.cs` 第583-850行 (`FillChunk*` 方法)
- `DrawCaveFill()` 方法

### 2. TileSet 配置问题

**问题描述：**
新场景或重新配置时可能出现 "瓦片未正确配置" 错误。

**解决方案：**
需要在Unity Inspector中手动配置：
- `TilemapLayers` - 四层Tilemap引用
- `TileSet` - 四种瓦片资源引用

---

## 文件结构

```
3_LevelGeneration/
├── Graybox/
│   ├── GrayboxLevelGenerator.cs      # 房间生成器（V3核心）
│   ├── MultiGridLevelManager.cs      # 多房间生成器
│   ├── GrayboxTilemapLayers.cs       # Tilemap层配置
│   ├── GrayboxGridPreview.cs         # 网格预览
│   └── ...
├── Data/
│   ├── LevelShape.cs                 # 关卡形状定义
│   ├── RoomNode.cs                   # 房间节点数据
│   └── RoomType.cs                   # 房间类型枚举
├── docs/
│   ├── DESIGN_TopologyBasedTerrainGeneration.md  # 拓扑原语系统设计文档
│   └── ...
└── LevelGenerationV3/
    └── README.md                     # 本文档
```

---

## 版本信息

- **版本**: V3.0
- **更新日期**: 2026-01-17
- **状态**: 开发中
- **分支**: feature/topology-terrain-system

---

## 下一步计划

1. [ ] 解决多平台问题 - 调优 `Platforms_Sparse` 光栅化函数
2. [ ] 添加更多 TerrainArchetype 类型
3. [ ] 实现房间间的过渡地形
4. [ ] 性能优化
5. [ ] 单元测试覆盖
