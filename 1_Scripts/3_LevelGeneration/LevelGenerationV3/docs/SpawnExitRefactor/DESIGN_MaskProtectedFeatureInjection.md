# 基于掩码保护的程序化特征注入 (Mask-Protected Procedural Feature Injection)

## 1. 可行性评估：**高**

| 评估维度 | 结论 | 说明 |
|----------|------|------|
| 性能影响 | 极低 | O(W*H) 遍历 + HashSet O(1) 查找 |
| 架构兼容性 | 高 | 与现有 CA 流程完美配合 |
| 实现复杂度 | 低 | 约 80 行新增代码 |
| 效果可控性 | 高 | 通过概率阈值调节密度 |

## 2. 当前架构分析

**现有生成流程**（`GenerateConnectedCaveFill`）：
```
1. 基础噪声生成（概率梯度场）
2. 石笋注入（高斯堆积造山法）
3. CA 平滑
4. 绘制到 Tilemap
```

**问题**：`CarveWindingPath()` 在 `GenerateConnectedCaveFill()` 之后执行，无法提前获取路径点。

## 3. 适配方案

### 3.1 安全区预测策略

由于路径雕刻在填充生成之后，采用**预测安全区**替代实际路径掩码：

```csharp
// 预测安全区 = 房间中心区域 + 连接方向通道
HashSet<Vector2Int> safeZone = new HashSet<Vector2Int>();

// 1. 房间中心区域（8x8范围）
int centerX = roomWidth / 2;
int centerY = roomHeight / 2;
for (int dx = -4; dx <= 4; dx++)
    for (int dy = -4; dy <= 4; dy++)
        safeZone.Add(new Vector2Int(centerX + dx, centerY + dy));

// 2. 连接方向通道（宽度6格）
if (room.HasConnection(Direction.North))
    // 向上延伸...
```

### 3.2 注入时机

在 `GenerateConnectedCaveFill()` 中：
```
1. 基础噪声生成
2. 石笋注入
3. 【新增】特征模板注入 (InjectTerrainFeatures)
4. CA 平滑（将硬切形状融合为自然地形）
```

## 4. 实现设计

### 4.1 特征模板定义

```csharp
private static readonly int[][,] FeaturePatterns = new int[][,]
{
    // 形状A: 3x3 实心块（大岛屿核心）
    new int[,] { {1,1,1}, {1,1,1}, {1,1,1} },
    // 形状B: 十字型（连接点）
    new int[,] { {0,1,0}, {1,1,1}, {0,1,0} },
    // 形状C: U型（口袋地形）
    new int[,] { {1,0,1}, {1,0,1}, {1,1,1} },
    // 形状D: L型（拐角）
    new int[,] { {1,1,0}, {1,0,0}, {1,1,1} },
    // 形状E: T型（分叉）
    new int[,] { {1,1,1}, {0,1,0}, {0,1,0} }
};
```

### 4.2 核心算法

```csharp
private void InjectTerrainFeatures(bool[,] cave, int width, int height)
{
    // 1. 构建预测安全区（房间中心 + 连接通道）
    HashSet<Vector2Int> safeZone = BuildPredictedSafeZone();
    
    // 2. 在非安全区注入特征
    for (int y = 3; y < height - 3; y += 4)
    {
        for (int x = 3; x < width - 3; x += 4)
        {
            if (safeZone.Contains(new Vector2Int(x, y))) continue;
            if (_rng.NextDouble() < 0.4) // 40%概率
            {
                StampPattern(cave, x, y, width, height);
            }
        }
    }
}
```

### 4.3 模板印章

```csharp
private void StampPattern(bool[,] cave, int cx, int cy, int width, int height)
{
    int patternIndex = _rng.Next(FeaturePatterns.Length);
    int[,] pattern = FeaturePatterns[patternIndex];
    
    for (int py = 0; py < 3; py++)
    {
        for (int px = 0; px < 3; px++)
        {
            if (pattern[py, px] == 1)
            {
                int tx = cx + px;
                int ty = cy + py;
                if (tx >= 1 && tx < width - 1 && ty >= 1 && ty < height - 1)
                    cave[tx, ty] = true;
            }
        }
    }
}
```

## 5. 预期效果

| 改进点 | 预期结果 |
|--------|----------|
| 中央空洞 | 填充悬浮岛/支柱，空洞面积减少 50%+ |
| 平台数量 | 垂锚修复有更多地形挂点，平台减少 |
| 地形多样性 | 5种基础形状 + CA融合 = 无限变化 |
| 可通行性 | 安全区保护确保路径不被阻断 |

## 6. 参数调节

| 参数 | 默认值 | 调节建议 |
|------|--------|----------|
| 注入概率 | 0.4 (40%) | 0.3-0.5 根据密度需求 |
| 步长 | 4 格 | 3-5 根据房间大小 |
| 安全区半径 | 4 格 | 3-5 根据通道宽度 |

## 7. 实施步骤

1. 在 `GrayboxLevelGenerator.cs` 中添加 `FeaturePatterns` 静态数组
2. 实现 `InjectTerrainFeatures()` 方法
3. 实现 `StampPattern()` 辅助方法
4. 在 `GenerateConnectedCaveFill()` 的 CA 平滑前调用 `InjectTerrainFeatures()`
