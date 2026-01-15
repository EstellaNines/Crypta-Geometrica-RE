# CONSENSUS - 混合式程序化随机关卡系统 (Hybrid PCG)

## 一、最终需求共识

### 1.1 系统目标
构建一套**运行时毫秒级生成**的混合式程序化关卡系统，实现:
- Spelunky式确定性骨架 (保证可通关)
- Dead Cells式有机视觉 (打破网格刚性)
- 60FPS流畅运行

### 1.2 核心功能清单
| 功能模块 | 优先级 | 状态 |
|----------|--------|------|
| LevelShape不规则形状定义 | P0 | 待实现 |
| 网格连通性验证 | P0 | 待实现 |
| 关键路径生成算法 | P0 | 待实现 |
| Boss房间插入 | P0 | 待实现 |
| 软边界活跃区域 | P1 | 待实现 |
| WFC内部填充 | P1 | 待实现 |
| Voronoi走廊生成 | P1 | 待实现 |
| 难度系统集成 | P1 | 待实现 |
| Tilemap批处理 | P2 | 待实现 |
| 物理可达性验证 | P2 | 待实现 |
| 对象池管理 | P2 | 待实现 |

---

## 二、技术实现方案

### 2.1 数据结构

#### RoomNode (房间节点)
```csharp
public class RoomNode
{
    public Vector2Int GridCoordinates;  // 网格坐标
    public RoomType Type;               // 房间类型
    public int ConnectionMask;          // 4-bit连通性 (NESW)
    public RectInt ActiveZone;          // 软边界活跃区域
    public float DifficultyRating;      // 难度系数
    public bool IsCriticalPath;         // 关键路径标记
    public List<Vector2Int> EnemySpawnPoints; // 敌人生成点
}
```

#### RoomType (房间类型)
```csharp
public enum RoomType
{
    None, Start, Exit, LR, Drop, Landing, Side, Shop, Abyss, Boss
}
```

#### LevelShape (关卡形状)
```csharp
public class LevelShape
{
    public int[,] OccupancyMask;  // 4×4位掩码
    // 方法: FromString(), GetValidCells(), IsValidCell()
}
```

### 2.2 算法选型

| 算法 | 用途 | 复杂度 |
|------|------|--------|
| BFS | 网格连通性验证 | O(n) |
| 醉汉行走变体 | 关键路径生成 | O(n) |
| WFC (简化版) | 房间内部填充 | O(n²) |
| Voronoi + A* | 走廊生成 | O(n log n) |
| 抛物线射线检测 | 物理可达性 | O(n²) |

### 2.3 Unity技术栈

| 模块 | Unity技术 |
|------|-----------|
| 瓦片渲染 | Tilemap.SetTilesBlock |
| 自动边缘 | Rule Tile |
| 碰撞优化 | CompositeCollider2D |
| 多线程计算 | C# Job System |
| SIMD加速 | Burst Compiler |
| 随机数 | Unity.Mathematics.Random |

---

## 三、技术约束

### 3.1 性能约束
- 单次生成时间: < 100ms
- 运行帧率: >= 60 FPS
- GC Alloc: < 1KB/帧 (运行时)

### 3.2 兼容性约束
- 使用项目现有Tilemap瓦片资源
- 使用项目现有敌人预制件
- 兼容现有关卡加载流程

### 3.3 扩展性约束
- 支持新增房间类型
- 支持自定义关卡形状
- 支持难度参数调整

---

## 四、集成方案

### 4.1 入口接口
```csharp
public interface ILevelGenerator
{
    /// <summary>
    /// 生成完整关卡
    /// </summary>
    /// <param name="shape">关卡形状定义</param>
    /// <param name="seed">随机种子</param>
    /// <param name="levelIndex">关卡序号(影响难度)</param>
    /// <returns>生成结果</returns>
    LevelGenerationResult Generate(LevelShape shape, int seed, int levelIndex);
}
```

### 4.2 输出数据
```csharp
public class LevelGenerationResult
{
    public bool Success;
    public RoomNode[,] RoomGrid;
    public List<Vector2Int> CriticalPath;
    public int BossRoomIndex;
    public float TotalDifficulty;
}
```

### 4.3 Tilemap集成
```csharp
// 生成后调用
TilemapGenerator.GenerateTiles(result.RoomGrid, tilemap);
```

---

## 五、验收标准

### 5.1 功能验收
| 验收项 | 标准 | 验证方法 |
|--------|------|----------|
| 关卡连通 | 100%存在可行路径 | 自动化测试1000次 |
| 形状支持 | 支持任意连通形状 | 测试L/T/十字/Z形 |
| Boss房间 | 正确位于Exit前 | 单元测试 |
| 难度递增 | 敌人数量正确增加 | 参数验证 |
| 物理可达 | 跳跃参数验证通过 | Job验证器 |

### 5.2 性能验收
| 验收项 | 标准 | 验证方法 |
|--------|------|----------|
| 生成时间 | < 100ms | Profiler测量 |
| 运行帧率 | >= 60 FPS | 帧率监控 |
| 内存分配 | 无运行时GC | Memory Profiler |

### 5.3 代码质量
| 验收项 | 标准 |
|--------|------|
| 编译通过 | 0 Error, 0 Warning |
| 单元测试 | 覆盖核心算法 |
| 代码注释 | 函数级注释完整 |

---

## 六、里程碑定义

### Phase 1: 核心骨架 (预计3天)
- [x] 数据结构定义
- [ ] LevelShape系统
- [ ] 关键路径算法
- [ ] Boss房间插入

### Phase 2: 有机血肉 (预计4天)
- [ ] 软边界系统
- [ ] WFC填充算法
- [ ] 难度系统集成

### Phase 3: 走廊连接 (预计3天)
- [ ] Voronoi图构建
- [ ] A*寻路集成
- [ ] 形态学平滑

### Phase 4: Unity集成 (预计3天)
- [ ] Tilemap批处理
- [ ] 碰撞体优化
- [ ] 物理验证系统

### Phase 5: 优化收尾 (预计2天)
- [ ] 对象池系统
- [ ] 性能优化
- [ ] 测试验收

---

## 七、风险缓解确认

| 风险 | 缓解措施 | 责任人 |
|------|----------|--------|
| WFC死锁 | 迭代上限500次+局部重置 | 开发者 |
| 路径断裂 | 物理验证+补救模块 | 开发者 |
| 性能问题 | SetTilesBlock+Job/Burst | 开发者 |

---

## 八、文档状态

- **创建时间**: 2026-01-15
- **状态**: 已完成
- **下一步**: 创建DESIGN系统架构文档
- **所有不确定性**: 已解决
