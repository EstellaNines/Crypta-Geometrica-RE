# 极简主义平台生成 (Minimalist Platforming) 设计方案

## 1. 核心理念转变

**从"加法思维"转向"减法思维"**

| 旧逻辑 | 新逻辑 |
|--------|--------|
| 先生成空房间，再填平台 | 先生成实心土块，再挖路 |
| 随机撒平台 | 只在必要处补平台 |
| 平台是主角 | 地形是主角 |

## 2. 三步改造方案

### 2.1 第一步：地形致密化 (Solid Block Strategy)

**修改文件**: `GrayboxLevelGenerator.cs`

**改动1**: 提高 FillDensity
```csharp
// 原值
public float FillDensity = 0.35f;
// 新值
public float FillDensity = 0.50f;
```

**改动2**: 强制中央噪点 (GenerateConnectedCaveFill)
```csharp
// 在 cave[x, y] = _rng.NextDouble() < FillDensity * edgeFactor; 前添加
// 计算到中心的距离
float centerX = fillWidth / 2f;
float centerY = fillHeight / 2f;
float distToCenter = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
float maxDist = Mathf.Min(fillWidth, fillHeight) / 2f;

// 中央区域（距离中心 < 25% 范围）强制增加填充概率
if (distToCenter < maxDist * 0.4f)
{
    edgeFactor *= 1.5f; // 中央区域填充概率提升 50%
}
```

### 2.2 第二步：雕刻优先 (Carve First)

**已有机制**: `CarveWindingPath` 已存在

**建议调整**: 可在 Inspector 中调整雕刻宽度参数（如果有）

### 2.3 第三步：极简平台生成 (Minimalist Platforming)

**修改文件**: `MultiGridLevelManager.cs`

**改动**: 禁用随机平台层生成，只保留垂锚修复
```csharp
// DrawPlatforms() 方法中
// 注释或删除原有的随机平台生成循环
// for (int layer = 0; layer < platformLayers; layer++) { ... }

// 只保留：
GenerateVerticalExitStaircases(...);
EnsurePlatformAccessibility(...);
```

## 3. 可行性评估：**高**

| 改动点 | 复杂度 | 风险 |
|--------|--------|------|
| FillDensity 调整 | 低 | 低 |
| 中央噪点注入 | 低 | 低 |
| 禁用随机平台 | 低 | 中（需测试可达性） |

**关键保障**：
- `EnsurePlatformAccessibility` 会在断层过高处自动生成必要平台
- `GenerateVerticalExitStaircases` 保证出口可达
- 三步过滤法防止过度修复

## 4. 实施顺序

1. **GrayboxLevelGenerator.cs**:
   - 修改 `FillDensity` 默认值为 0.50f
   - 在 `GenerateConnectedCaveFill` 中添加中央噪点逻辑

2. **MultiGridLevelManager.cs**:
   - 禁用 `DrawPlatforms` 中的随机平台生成循环
   - 保留出口阶梯和垂锚修复调用

## 5. 预期效果

- 房间大部分区域被泥土填满
- 玩家需要利用自然地形移动
- 平台只在"断层过高"处出现
- 洞穴探索感大幅增强
- 平台数量减少 70%+

## 6. 验收标准

- [ ] 房间中央不再是巨大空腔
- [ ] 随机平台被禁用
- [ ] 垂锚修复正常工作
- [ ] 关卡仍然可通关
- [ ] 视觉上更像洞穴而非跳台
