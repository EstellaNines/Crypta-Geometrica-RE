# 多网格关卡生成系统 - API 文档

## 一、MultiGridLevelManager API

### 1.1 公共方法

#### GenerateMultiGridLevel()
```csharp
public void GenerateMultiGridLevel()
```
**功能**: 生成多网格关卡

**流程**:
1. 初始化随机数生成器
2. 清除所有Tilemap层
3. 生成随机位置
4. 遍历生成每个网格
5. 计算出生点和通关点

**示例**:
```csharp
MultiGridLevelManager manager = GetComponent<MultiGridLevelManager>();
manager.GenerateMultiGridLevel();
```

#### ClearAllGrids()
```csharp
public void ClearAllGrids()
```
**功能**: 清除所有网格

**示例**:
```csharp
manager.ClearAllGrids();
```

#### GetGridPositions()
```csharp
public List<Vector2Int> GetGridPositions()
```
**功能**: 获取已生成的网格位置列表

**返回值**: 网格位置列表（左下角坐标）

#### GetGridBounds()
```csharp
public List<Rect> GetGridBounds()
```
**功能**: 获取已生成的网格边界列表

**返回值**: 网格边界矩形列表


#### GetEntrancePositions()
```csharp
public List<Vector3> GetEntrancePositions()
```
**功能**: 获取所有入口位置

**返回值**: 入口世界坐标列表

#### GetExitPositions()
```csharp
public List<Vector3> GetExitPositions()
```
**功能**: 获取所有出口位置

**返回值**: 出口世界坐标列表

#### GetPlayerSpawnPoint()
```csharp
public Vector3 GetPlayerSpawnPoint()
```
**功能**: 获取玩家出生点

**返回值**: 出生点世界坐标

#### GetLevelExitPoint()
```csharp
public Vector3 GetLevelExitPoint()
```
**功能**: 获取关卡通关点

**返回值**: 通关点世界坐标

### 1.2 公共属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| LevelGenerator | GrayboxLevelGenerator | 灰盒关卡生成器引用 |
| GridCount | int | 网格总数量 (1-8) |
| LayoutAreaWidth | int | 布局区域宽度（瓦片） |
| LayoutAreaHeight | int | 布局区域高度（瓦片） |
| MinGridSpacing | int | 网格最小间距（瓦片） |
| BaseSeed | int | 基础随机种子（0=随机） |
| UseUniqueSeedPerGrid | bool | 每网格独立种子 |
| HasSpawnPoint | bool | 是否有有效的出生点 |
| HasExitPoint | bool | 是否有有效的通关点 |


---

## 二、GrayboxLevelGenerator API

### 2.1 公共方法

#### GenerateLevel()
```csharp
public void GenerateLevel()
```
**功能**: 生成完整关卡（使用默认FullSquare形状）

#### GenerateLevel(LevelShape shape)
```csharp
public void GenerateLevel(LevelShape shape)
```
**功能**: 生成指定形状的关卡

**参数**:
- `shape`: 关卡形状配置

**流程**:
1. 验证设置
2. 初始化房间网格
3. 生成关键路径（醉汉游走）
4. 绘制外围墙壁
5. 绘制洞穴填充
6. 绘制房间连接通道
7. 绘制入口和出口
8. 绘制平台
9. 绘制特殊区域

#### ClearLevel()
```csharp
public void ClearLevel()
```
**功能**: 清除关卡所有瓦片

### 2.2 公共属性

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| TilemapLayers | GrayboxTilemapLayers | - | Tilemap层配置 |
| TileSet | GrayboxTileSet | - | 瓦片配置 |
| RoomWidth | int | 16 | 单个房间宽度（瓦片） |
| RoomHeight | int | 16 | 单个房间高度（瓦片） |
| WallThickness | int | 2 | 外围墙壁厚度 |
| FillDensity | float | 0.50 | 填充密度 (0-0.6) |
| SmoothIterations | int | 3 | 平滑迭代次数 (0-5) |
| RandomSeed | int | 0 | 随机种子（0=随机） |
| EntranceWidth | int | 3 | 出入口通道宽度 |
| EntranceHeight | int | 3 | 出入口通道高度 |
| PlayerJumpForce | float | 8f | 玩家跳跃力 |
| MaxPlatformHeightDiff | int | 4 | 平台最大高度差 |
| MinPlatformGap | int | 3 | 平台最小间距 |
| StaircaseSafeHeight | int | 4 | 阶梯安全跳跃高度 |
| StaircasePlatformWidth | int | 4 | 阶梯平台宽度 |
| StaircaseHorizontalOffset | int | 4 | 阶梯水平偏移量 |
| UseTheme | bool | false | 是否启用主题系统 |
| ThemeConfig | RoomTheme | - | 主题配置数据 |
| UseRuleTile | bool | false | 是否启用规则瓦片替换 |
| GroundRuleTile | RuleTile | - | 地面规则瓦片 |
| PlatformRuleTile | RuleTile | - | 平台规则瓦片 |

---

## 三、SpawnExitPointCalculator API

### 3.1 公共方法

#### CalculateFarthestPoint()
```csharp
public Vector3 CalculateFarthestPoint(
    Vector3 referencePoint,
    Rect searchBounds,
    Tilemap groundLayer,
    Tilemap platformLayer)
```
**功能**: 计算距离参考点最远的有效可站立位置

**参数**:
- `referencePoint`: 参考门位置（出口或入口）
- `searchBounds`: 搜索边界（当前网格的Rect）
- `groundLayer`: 地面层Tilemap
- `platformLayer`: 平台层Tilemap

**返回值**: 最远的有效可站立位置（世界坐标）

**算法流程**:
1. 将参考点转换为整数坐标
2. 使用BFS构建距离场
3. 按距离降序排序，获取前10个候选点
4. 物理环境验证（可站立性检查）
5. 返回第一个有效的可站立位置

**示例**:
```csharp
SpawnExitPointCalculator calculator = new SpawnExitPointCalculator();
Vector3 spawnPoint = calculator.CalculateFarthestPoint(
    exitPosition,
    gridBounds,
    groundLayer,
    platformLayer
);
```

---

## 四、数据结构

### 4.1 RoomNode
```csharp
public class RoomNode
{
    public int X { get; set; }
    public int Y { get; set; }
    public RoomType Type { get; set; }
    public bool IsCriticalPath { get; set; }
    
    public void AddConnection(Direction dir);
    public bool HasConnection(Direction dir);
}
```

### 4.2 LevelShape
```csharp
public class LevelShape
{
    public const int GridWidth = 4;
    public const int GridHeight = 4;
    
    public bool IsValidCell(int x, int y);
    public void SetCell(int x, int y, bool valid);
    public List<Vector2Int> GetValidCells();
    public int GetValidCellCount();
}
```

### 4.3 Direction (枚举)
```csharp
public enum Direction
{
    North,  // 上
    South,  // 下
    East,   // 右
    West    // 左
}
```

### 4.4 RoomType (枚举)
```csharp
public enum RoomType
{
    Side,     // 普通房间
    Start,    // 起始房间
    Exit,     // 出口房间
    Boss,     // Boss房间
    Shop,     // 商店房间
    LR,       // 左右连接房间
    Drop,     // 垂直掉落房间
    Landing   // 着陆房间
}
```

---

## 五、使用示例

### 5.1 基础生成
```csharp
// 获取组件
MultiGridLevelManager manager = GetComponent<MultiGridLevelManager>();

// 配置参数
manager.GridCount = 4;
manager.BaseSeed = 12345;
manager.UseUniqueSeedPerGrid = true;

// 生成关卡
manager.GenerateMultiGridLevel();

// 获取出生点
Vector3 spawnPoint = manager.GetPlayerSpawnPoint();
Debug.Log($"玩家出生点: {spawnPoint}");
```

### 5.2 自定义主题
```csharp
// 启用主题系统
GrayboxLevelGenerator generator = GetComponent<GrayboxLevelGenerator>();
generator.UseTheme = true;
generator.ThemeConfig = myThemeConfig;

// 生成关卡
manager.GenerateMultiGridLevel();
```

### 5.3 获取网格信息
```csharp
// 获取所有网格位置
List<Vector2Int> gridPositions = manager.GetGridPositions();
foreach (var pos in gridPositions)
{
    Debug.Log($"网格位置: {pos}");
}

// 获取所有出入口
List<Vector3> entrances = manager.GetEntrancePositions();
List<Vector3> exits = manager.GetExitPositions();
```

---

## 六、事件回调

### 6.1 生成完成回调
```csharp
// 在MultiGridLevelManager中添加事件
public event Action OnLevelGenerated;

// 在GenerateMultiGridLevel()末尾触发
OnLevelGenerated?.Invoke();

// 使用示例
manager.OnLevelGenerated += () => {
    Debug.Log("关卡生成完成！");
    SpawnPlayer(manager.GetPlayerSpawnPoint());
};
```

---

## 七、错误处理

### 7.1 常见错误码

| 错误信息 | 原因 | 解决方案 |
|----------|------|----------|
| "Tilemap层未正确配置" | TilemapLayers为null或无效 | 检查TilemapLayers配置 |
| "瓦片未正确配置" | TileSet为null或无效 | 检查TileSet配置 |
| "无法在指定区域内放置所有网格" | 布局区域不足 | 增加LayoutAreaWidth/Height |
| "BFS未找到任何有效位置" | 搜索边界错误 | 检查searchBounds参数 |

### 7.2 调试日志
```csharp
// 启用详细日志
Debug.Log($"开始生成多网格关卡: {GridCount}个网格");
Debug.Log($"网格[{i}] 生成完成 (位置: {pos.x},{pos.y})");
Debug.Log($"[垂锚连接] 完成, 共{iteration}次迭代");
Debug.Log($"出生点计算完成: {spawnPoint}");
```

---

**文档版本**: v1.0  
**最后更新**: 2026-01-17  
**维护者**: CRYPTA GEOMETRICA RE Team
