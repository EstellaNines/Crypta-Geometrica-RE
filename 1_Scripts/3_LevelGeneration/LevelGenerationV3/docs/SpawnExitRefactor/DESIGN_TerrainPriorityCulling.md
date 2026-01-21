# 地形优先剔除法 (Terrain Priority Culling) 设计方案

## 1. 问题描述

当前平台生成逻辑是"先生成洞穴，然后无视洞穴形态，强行在空中铺满平台"，导致：
- 平台过多，掩盖了洞穴地形（Fill Layer）的探索感
- 玩家无需利用自然地形，只需跳平台
- 降低了关卡的视觉层次和可玩性

## 2. 解决方案：地形邻近剔除法

### 2.1 核心逻辑

在生成每一个平台之前，先向左、向右进行"雷达扫描"：
- **扫描范围**：左右各 3-4 格
- **检测目标**：GroundLayer（包含 FillLayer/WallLayer）

### 2.2 剔除规则

```
如果检测到地形 → 玩家可跳到洞穴壁上 → 取消生成平台
如果全是空气   → 巨大空洞无法跳跃     → 保留平台
```

## 3. 技术实现

### 3.1 架构分析

当前架构：
- `GrayboxTilemapLayers.FillLayer` → 指向 `GroundLayer`
- `GrayboxTilemapLayers.WallLayer` → 指向 `GroundLayer`
- 平台生成位置：`MultiGridLevelManager.DrawPlatforms()`

### 3.2 实现位置

修改 `DrawPlatforms()` 方法，在 `FillRect(tilemap, tile, px, layerHeight, pw, 1)` 之前添加地形邻近检测。

### 3.3 核心方法

```csharp
/// <summary>
/// 检查平台位置左右是否有可利用的地形
/// </summary>
/// <param name="x">平台X坐标</param>
/// <param name="y">平台Y坐标</param>
/// <param name="width">平台宽度</param>
/// <param name="scanRange">扫描范围（左右各多少格）</param>
/// <returns>true=附近有地形可用，应跳过平台生成</returns>
private bool HasNearbyTerrain(int x, int y, int width, int scanRange)
{
    var groundTilemap = LevelGenerator.TilemapLayers.GroundLayer;
    
    // 检查平台左侧
    for (int dx = 1; dx <= scanRange; dx++)
    {
        // 检查平台高度及上下1格范围（玩家可跳达）
        for (int dy = -1; dy <= 1; dy++)
        {
            var pos = new Vector3Int(x - dx, y + dy, 0);
            if (groundTilemap.GetTile(pos) != null)
            {
                // 检查该地形上方是否可站立
                var above = new Vector3Int(x - dx, y + dy + 1, 0);
                if (groundTilemap.GetTile(above) == null)
                    return true; // 左侧有可利用地形
            }
        }
    }
    
    // 检查平台右侧
    for (int dx = 1; dx <= scanRange; dx++)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            var pos = new Vector3Int(x + width + dx, y + dy, 0);
            if (groundTilemap.GetTile(pos) != null)
            {
                var above = new Vector3Int(x + width + dx, y + dy + 1, 0);
                if (groundTilemap.GetTile(above) == null)
                    return true; // 右侧有可利用地形
            }
        }
    }
    
    return false; // 两侧都是空气
}
```

### 3.4 调用位置

```csharp
// DrawPlatforms() 方法中
int px = worldX + 2 + rng.Next(roomWidth - pw - 4);

// 新增：地形邻近剔除
int terrainScanRange = 4; // 可配置参数
if (HasNearbyTerrain(px, layerHeight, pw, terrainScanRange))
{
    continue; // 跳过此平台，附近有可用地形
}

FillRect(tilemap, tile, px, layerHeight, pw, 1);
```

## 4. 配置参数

| 参数名 | 默认值 | 说明 |
|--------|--------|------|
| TerrainScanRange | 4 | 地形扫描范围（格） |
| EnableTerrainCulling | true | 是否启用地形剔除 |

建议在 `GrayboxLevelGenerator` 中添加 Inspector 可配置参数。

## 5. 效果预期

- **靠近墙壁的平台消失**：迫使玩家利用洞穴自然凹凸
- **只有中央空旷区有平台**：平台成为真正的"桥梁"
- **强化填充雕刻存在感**：自然地形主导关卡探索

## 6. 补充优化建议

### 6.1 增加 Fill Density
- 将 `Fill Density` 从 0.35 提高到 0.46-0.48
- 生成更厚实、连接更紧密的洞穴块

### 6.2 调整雕刻宽度
- 减少 `CarveWindingPath` 中的 `pathWidth`
- 让通道更狭窄、更崎岖

## 7. 验收标准

- [ ] 靠近地形的平台被正确剔除
- [ ] 中央空旷区保留必要平台
- [ ] 不影响出口阶梯和垂锚连接功能
- [ ] 关卡仍然可通关（可达性保证）
