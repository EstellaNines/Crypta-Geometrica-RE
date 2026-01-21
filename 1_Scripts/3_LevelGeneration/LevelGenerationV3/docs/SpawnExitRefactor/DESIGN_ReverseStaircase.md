# 逆向阶梯生成算法设计方案

## 问题分析

### 问题现象
**所有房间的垂直方向出口**（北向/南向）位置过高，玩家无法到达（垂直可达性断层）

### 根本原因
1. `GrayboxLevelGenerator.cs` 第1196行跳过了Start/Exit房间的平台生成
2. 其他房间的随机平台生成无法保证通往垂直出口的可达性
3. 北向出口通常在房间顶部，南向出口可能需要向下到达

### 适用范围
**所有房间类型**中存在垂直方向（North/South）出口的情况：
- Exit房间的北向/南向出口
- 普通房间（LR类型）的北向/南向出口
- Drop/Landing房间
- 任何具有垂直门户的房间

### 玩家能力参数
| 参数 | 值 | 说明 |
|------|-----|------|
| Jump Force | 8 | 跳跃力度 |
| 单次跳跃高度 | ~3.2 瓦片 | 物理估算 |
| 二段跳极限 | ~5-6 瓦片 | 物理估算 |
| **安全高度** | **4 瓦片** | 手感流畅的设计值 |

---

## 核心算法：逆向阶梯生成 (Reverse Staircase)

### 设计原则
从出口往下倒推，基于数学计算确保必然能跳上去。

### 算法流程

```
┌─────────────────────────────────────────────────────────────┐
│                    逆向阶梯生成流程                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. 获取锚点                                                │
│     ├── 目标锚点 = 出口底部 Y 坐标                          │
│     └── 地面锚点 = 房间地面 Y 坐标                          │
│                                                             │
│  2. 检测是否需要阶梯                                        │
│     if (目标锚点 - 地面锚点 <= 安全高度)                    │
│         return; // 不需要阶梯                               │
│                                                             │
│  3. 填充循环                                                │
│     while (当前锚点 - 地面锚点 > 安全高度)                   │
│     {                                                       │
│         新平台Y = 当前锚点Y - 安全高度                      │
│         新平台X = 交替偏移（防撞头）                        │
│         生成平台(新平台X, 新平台Y, 长度3-4)                 │
│         当前锚点 = 新平台                                   │
│     }                                                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 视觉示意

```
出口 ─────────────────── [出口踏板] ← 目标锚点
                              │
                              │ 安全高度(4格)
                              ↓
         ┌─────────┐ ← 阶梯平台3 (向右偏移)
         │         │
                   │ 安全高度(4格)
                   ↓
              ┌─────────┐ ← 阶梯平台2 (向左偏移)
              │         │
                        │ 安全高度(4格)
                        ↓
                   ┌─────────┐ ← 阶梯平台1 (向右偏移)
                   │         │
                              │ ≤ 安全高度
                              ↓
═══════════════════════════════════ ← 地面锚点
```

---

## 实施方案

### 修改位置
`GrayboxLevelGenerator.cs`

### 核心改动：为所有垂直出口生成阶梯

在 `GeneratePlatforms()` 方法末尾，添加对所有房间垂直出口的阶梯生成：

```csharp
// 在现有平台生成逻辑之后，添加垂直出口阶梯生成
GenerateVerticalExitStaircases();
```

### 新增方法：GenerateVerticalExitStaircases

```csharp
/// <summary>
/// 为所有房间的垂直方向出口生成阶梯，确保玩家能够到达
/// </summary>
private void GenerateVerticalExitStaircases()
{
    for (int gy = 0; gy < LevelShape.GridHeight; gy++)
    {
        for (int gx = 0; gx < LevelShape.GridWidth; gx++)
        {
            if (!_currentShape.IsValidCell(gx, gy)) continue;
            
            var room = _roomGrid[gx, gy];
            int worldX = gx * RoomWidth;
            int worldY = (LevelShape.GridHeight - 1 - gy) * RoomHeight;
            
            // 检查北向出口（需要向上到达）
            if (room.HasNorthExit)
            {
                GenerateUpwardStaircase(worldX, worldY, Direction.North);
            }
            
            // 检查南向出口（需要向下到达，通常不需要阶梯，但需要确保入口可达）
            if (room.HasSouthEntrance)
            {
                GenerateDownwardPath(worldX, worldY, Direction.South);
            }
        }
    }
}
```

### 新增方法：GenerateUpwardStaircase（向上阶梯）

```csharp
/// <summary>
/// 生成通往北向出口的向上阶梯
/// </summary>
private void GenerateUpwardStaircase(int worldX, int worldY, Direction exitDir)
{
    // 常量定义
    int safeHeight = StaircaseSafeHeight;     // 安全跳跃高度（可配置）
    int platformWidth = StaircasePlatformWidth; // 平台宽度
    int horizontalOffset = StaircaseHorizontalOffset; // 水平偏移量
    
    // 计算出口踏板Y坐标（北向出口在房间顶部）
    int exitY = worldY + RoomHeight - WallThickness - 2;
    int groundY = worldY + WallThickness + 1;
    
    // 检查是否需要阶梯
    int heightDiff = exitY - groundY;
    if (heightDiff <= safeHeight) return;
    
    // 房间边界
    int roomLeft = worldX + WallThickness + 2;
    int roomRight = worldX + RoomWidth - WallThickness - platformWidth - 2;
    int roomCenterX = worldX + RoomWidth / 2;
    
    // 当前锚点（从出口开始向下）
    int currentY = exitY;
    bool placeLeft = true;
    
    // 循环生成阶梯
    while (currentY - groundY > safeHeight)
    {
        int newY = currentY - safeHeight;
        
        // 计算X位置（交替偏移）
        int newX = placeLeft 
            ? Mathf.Clamp(roomCenterX - horizontalOffset, roomLeft, roomRight)
            : Mathf.Clamp(roomCenterX + horizontalOffset, roomLeft, roomRight);
        
        // 生成平台
        FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile,
            newX, newY, platformWidth, 1);
        
        // 清除平台上方空间（防止卡头）
        ClearRect(TilemapLayers.GroundLayer,
            newX - 1, newY + 1, platformWidth + 2, 3);
        
        currentY = newY;
        placeLeft = !placeLeft;
    }
}
```

### 新增方法：GenerateDownwardPath（向下路径）

```csharp
/// <summary>
/// 确保南向入口下方有安全着陆区域
/// </summary>
private void GenerateDownwardPath(int worldX, int worldY, Direction entranceDir)
{
    // 南向入口：玩家从上方掉落进入
    // 确保入口下方有安全着陆平台
    
    int entranceX = worldX + RoomWidth / 2;
    int entranceY = worldY + WallThickness + 2;
    
    // 检查入口下方是否有地面
    Vector3Int belowPos = new Vector3Int(entranceX, entranceY - 1, 0);
    if (TilemapLayers.GroundLayer.GetTile(belowPos) == null &&
        TilemapLayers.PlatformLayer.GetTile(belowPos) == null)
    {
        // 生成着陆平台
        FillRect(TilemapLayers.PlatformLayer, TileSet.PinkTile,
            entranceX - 2, entranceY - 1, 5, 1);
    }
}
```

---

## 参数配置

建议在 `GrayboxLevelGenerator` 中添加可配置参数：

```csharp
[Header("逆向阶梯设置")]
[Tooltip("阶梯平台间的安全跳跃高度")]
[Range(3, 6)]
public int StaircaseSafeHeight = 4;

[Tooltip("阶梯平台宽度")]
[Range(3, 6)]
public int StaircasePlatformWidth = 4;

[Tooltip("阶梯水平偏移量")]
[Range(3, 8)]
public int StaircaseHorizontalOffset = 4;
```

---

## 任务拆分

| 序号 | 任务 | 依赖 | 复杂度 |
|------|------|------|--------|
| 1 | 添加逆向阶梯配置参数 | 无 | 低 |
| 2 | 实现 GetExitPlatformY 方法 | 无 | 低 |
| 3 | 实现 GenerateReverseStaircase 方法 | 1,2 | 中 |
| 4 | 修改 GeneratePlatforms 调用逻辑 | 3 | 低 |
| 5 | 测试验证 | 全部 | 中 |

---

## 验收标准

1. 出口房间有清晰的阶梯平台通往出口
2. 每级平台间距 ≤ 安全高度（4瓦片）
3. 平台交替偏移，玩家不会撞头
4. 平台上方有足够的站立和跳跃空间
5. 不影响其他房间的正常平台生成
