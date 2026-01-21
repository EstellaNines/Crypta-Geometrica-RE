# 高斯堆积造山法 (Gaussian Pile-Up Strategy) 设计方案

## 1. 问题分析

当前地形生成的核心问题：**"瑞士奶酪"拓扑**
- 泥土均匀分布，没有重心
- 中央空旷，缺乏垂直支撑结构
- 底部地基不够厚实

## 2. 核心理念转变

**从"随机撒点"变为"重力堆积"**

| 旧逻辑 | 新逻辑 |
|--------|--------|
| 每格50%概率是墙 | 越低越密，越中间越密 |
| 均匀分布 | 重力堆积 |
| 瑞士奶酪 | 蚁穴/山脉 |

## 3. 三步实施方案

### 3.1 第一步：概率梯度场 (Gradient Probability Field)

**修改位置**: `GenerateConnectedCaveFill()` 中的概率计算

**公式**:
```
最终概率 = 基础密度 
         + (1.0 - 高度百分比) * 重力系数 
         + (1.0 - 离中心距离) * 聚拢系数
```

**参数建议**:
- 重力系数 (GravityFactor): 0.3
- 聚拢系数 (CenterFactor): 0.2

**代码逻辑**:
```csharp
// 垂直梯度：底部概率高，顶部概率低
float heightRatio = (float)y / fillHeight;
float gravityBonus = (1.0f - heightRatio) * 0.3f;

// 水平中心梯度：中间概率高，边缘概率低
float centerX = fillWidth / 2f;
float distToCenter = Mathf.Abs(x - centerX) / centerX;
float centerBonus = (1.0f - distToCenter) * 0.2f;

// 最终概率
float finalProbability = FillDensity * edgeFactor + gravityBonus + centerBonus;
cave[x, y] = _rng.NextDouble() < finalProbability;
```

### 3.2 第二步：石笋注入 (Stalagmite Injection)

**时机**: 在 `SmoothCave` 之前

**逻辑**:
1. 在房间宽度 20%~80% 范围内随机选 2-3 个 X 坐标
2. 从地面向上填充泥土，高度为房间高度的 40%~70%
3. 左右各扩展 1-2 格形成粗柱

**代码逻辑**:
```csharp
// 石笋注入
int stalagmiteCount = 2 + _rng.Next(2); // 2-3根
for (int i = 0; i < stalagmiteCount; i++)
{
    int sx = fillWidth / 5 + _rng.Next(fillWidth * 3 / 5);
    int maxHeight = fillHeight * 2 / 5 + _rng.Next(fillHeight / 3);
    int thickness = 1 + _rng.Next(2);
    
    for (int dy = 0; dy < maxHeight; dy++)
    {
        for (int dx = -thickness; dx <= thickness; dx++)
        {
            int px = sx + dx;
            if (px >= 0 && px < fillWidth)
                cave[px, dy] = true;
        }
    }
}
```

### 3.3 第三步：虫洞挖掘优化 (可选)

**修改位置**: `CarveWindingPath` 或相关雕刻方法

**原则**:
- 挖掘区域限制为 3x3 或 4x4
- 保留路径上方的天花板
- 增加路径的垂直波动

## 4. 可行性评估：**高**

| 改动点 | 复杂度 | 风险 |
|--------|--------|------|
| 概率梯度场 | 低 | 低 |
| 石笋注入 | 低 | 低 |
| 虫洞挖掘 | 中 | 中 |

## 5. 实施顺序

1. **概率梯度场** - 修改 `GenerateConnectedCaveFill` 中的概率计算
2. **石笋注入** - 在 `SmoothCave` 调用前添加石笋生成
3. **测试验证** - 确保地形有明显的中央隆起和底部堆积

## 6. 预期效果

- 底部形成厚实的地基
- 中央自然隆起成"小山"
- 石笋形成垂直支撑结构
- 彻底解决中央空旷问题
- 垂锚修复有处可依

## 7. 验收标准

- [ ] 底部地基明显比顶部厚
- [ ] 房间中央有明显的地形隆起
- [ ] 存在2-3个垂直支撑结构（石笋）
- [ ] 关卡仍然可通关
