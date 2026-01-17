# 全局垂锚连接法 (Global Vertical Anchoring) 设计方案

## 1. 问题背景

在已实施"逆向阶梯"后，仍存在以下问题：
- 场景内零散的不可到达区域（孤岛平台）
- 随机生成的平台可能形成断层
- 玩家无法通过跳跃到达某些高处平台

## 2. 核心概念

**全局垂锚连接**：每个悬空平台组向下"抛锚"，如果锚链过长（超过玩家跳跃高度），在中间插入踏板。

```
步骤 1: 发现高空孤岛
       ┌───────┐ (平台组 A)
       └───────┘
           ↓
           ↓  (高度差 = 10，太高了！)
           ↓
─────────────────────── (地面)

步骤 2: 插入中继平台
       ┌───────┐ (平台组 A)
       └───────┘
           ↓ (高度 = 5)
       ┌───┴───┐ (自动生成的中继平台 B)
           ↓ (高度 = 5)
─────────────────────── (地面)
```

## 3. 算法流程

### 3.1 第一步：平台聚类 (Platform Clustering)

```csharp
/// <summary>
/// 平台聚类数据结构
/// </summary>
public class PlatformCluster
{
    public List<Vector3Int> Tiles;      // 组内所有瓦片
    public int MinX, MaxX;              // X范围
    public int Y;                       // Y坐标（同一组Y相同）
    public Vector3Int LeftPoint;        // 最左端检查点
    public Vector3Int RightPoint;       // 最右端检查点
    public Vector3Int CenterPoint;      // 中心检查点
}
```

**扫描逻辑**：
1. 遍历 `PlatformLayer` 所有瓦片
2. 使用 Union-Find 或 Flood-Fill 将水平相邻瓦片分组
3. 忽略 Y 坐标在房间底部边缘的瓦片（GroundLayer）

### 3.2 第二步：寻找落脚点 (Find Drop Point)

对每个平台组：
1. 计算3个检查点：`LeftPoint`, `CenterPoint`, `RightPoint`
2. 从每个检查点向下发射逻辑射线
3. 检测下方最近实体（PlatformLayer 或 GroundLayer）
4. 记录最小垂直距离

```csharp
int FindDropDistance(Vector3Int checkPoint, Tilemap platformLayer, Tilemap groundLayer)
{
    for (int dy = 1; dy < maxCheckDistance; dy++)
    {
        Vector3Int below = new Vector3Int(checkPoint.x, checkPoint.y - dy, 0);
        if (platformLayer.GetTile(below) != null || groundLayer.GetTile(below) != null)
            return dy;
    }
    return maxCheckDistance; // 未找到着陆点
}
```

### 3.3 第三步：判定与修复 (Validate & Repair)

```
情况 A：距离 <= SafeHeight (4格)
  → 安全，跳过

情况 B：距离 > SafeHeight
  → 断层，插入中继平台
  → 在 SafeHeight 距离处生成新平台
  → 新平台带随机X偏移（防止垂直梯子）
```

## 4. 实施方案

### 4.1 在 MultiGridLevelManager 中添加方法

位置：`DrawPlatforms()` 方法末尾，在 `GenerateVerticalExitStaircases()` 之后调用

```csharp
// 在 DrawPlatforms 末尾添加：
EnsurePlatformAccessibility(offsetX, offsetY, shape, roomGrid, roomWidth, roomHeight, wallThickness);
```

### 4.2 核心方法签名

```csharp
/// <summary>
/// 确保所有平台可达 - 全局垂锚连接法
/// </summary>
private void EnsurePlatformAccessibility(int offsetX, int offsetY, LevelShape shape, 
    RoomNode[,] roomGrid, int roomWidth, int roomHeight, int wallThickness)

/// <summary>
/// 获取所有独立平台组
/// </summary>
private List<PlatformCluster> GetPlatformClusters(int offsetX, int offsetY, 
    LevelShape shape, int roomWidth, int roomHeight)

/// <summary>
/// 向下射线检测，返回到最近实体的距离
/// </summary>
private int RaycastDown(Vector3Int from, int maxDistance)

/// <summary>
/// 在指定位置生成中继平台
/// </summary>
private void PlaceAnchorPlatform(int x, int y, int width, int horizontalOffset)
```

### 4.3 参数配置

| 参数名 | 默认值 | 说明 |
|--------|--------|------|
| SafeJumpHeight | 4 | 安全跳跃高度 |
| AnchorPlatformWidth | 3 | 中继平台宽度 |
| MaxHorizontalOffset | 4 | 最大水平偏移 |
| MaxRaycastDistance | 50 | 最大射线检测距离 |

### 4.4 左右摆动规则

为避免生成垂直梯子：
1. 新平台随机向左或向右偏移 `HorizontalOffset` 格
2. 偏移前检查目标位置是否有墙壁阻挡
3. 如果偏移后水平距离 > 玩家跳跃距离，拉回一点

```csharp
int CalculateOffsetX(int parentX, int minX, int maxX, bool placeLeft)
{
    int offset = placeLeft ? -HorizontalOffset : HorizontalOffset;
    int newX = parentX + offset;
    
    // 边界检查
    newX = Mathf.Clamp(newX, minX, maxX);
    
    // 水平可达性检查（不超过跳跃距离）
    if (Mathf.Abs(newX - parentX) > MaxHorizontalJump)
        newX = parentX + (placeLeft ? -MaxHorizontalJump : MaxHorizontalJump);
    
    return newX;
}
```

## 5. 执行顺序

```
DrawPlatforms()
    │
    ├── 1. 常规平台生成（随机）
    │
    ├── 2. GenerateVerticalExitStaircases() [已实现]
    │      └── 为出口方向生成阶梯
    │
    └── 3. EnsurePlatformAccessibility() [新增]
           ├── GetPlatformClusters() - 聚类
           ├── Sort by Y (高→低)
           └── Loop: 检测+修复
```

## 6. 迭代修复机制

由于新生成的中继平台可能仍需要更多中继，采用**迭代修复**：

```csharp
int maxIterations = 10;
int iteration = 0;

while (iteration < maxIterations)
{
    var clusters = GetPlatformClusters(...);
    bool anyFixed = false;
    
    foreach (var cluster in clusters.OrderByDescending(c => c.Y))
    {
        int gap = FindMinDropDistance(cluster);
        if (gap > SafeJumpHeight)
        {
            PlaceAnchorPlatform(...);
            anyFixed = true;
        }
    }
    
    if (!anyFixed) break; // 全部修复完成
    iteration++;
}
```

## 7. 边界情况处理

1. **墙壁阻挡**：偏移前检查目标位置左右是否有墙
2. **出口区域**：不在出口/入口门框附近生成中继平台
3. **已有平台**：避免与已有平台重叠
4. **房间边界**：中继平台不能超出房间边界

## 8. 验收标准

- [ ] 所有平台组到地面的垂直链路 <= SafeJumpHeight
- [ ] 中继平台带有左右摆动，不形成垂直梯子
- [ ] 不影响现有出口阶梯逻辑
- [ ] 性能：聚类+修复在 100ms 内完成

## 9. 任务拆分

| ID | 任务 | 依赖 | 预估 |
|----|------|------|------|
| 1 | 定义 PlatformCluster 数据结构 | - | 10min |
| 2 | 实现 GetPlatformClusters() | 1 | 30min |
| 3 | 实现 RaycastDown() | - | 15min |
| 4 | 实现 PlaceAnchorPlatform() | - | 15min |
| 5 | 实现 EnsurePlatformAccessibility() | 2,3,4 | 30min |
| 6 | 集成到 DrawPlatforms() | 5 | 10min |
| 7 | 测试验证 | 6 | 20min |

**总计：约 2 小时**
