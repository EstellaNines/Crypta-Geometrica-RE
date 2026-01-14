# GameManager 容器化服务架构

## 设计理念

将 GameManager 设计为**容器化服务**，避免项目中大量单例导致的初始化顺序混乱。

### 核心特性

- **唯一性**：整个游戏只有一个 GameManager 挂载 `DontDestroyOnLoad`
- **模块化**：所有子系统作为"模块"挂载在其子节点下
- **统一接口**：所有模块遵循 `IGameModule` 接口
- **有序生命周期**：Init → Update → Dispose 统一管理
- **自动初始化**：开发阶段任何场景运行时自动创建 GameManager

## 架构层级

```
[GameManager] (Script: GameManager.cs, DontDestroyOnLoad)
  ├── [AsyncSceneManager] (Script: AsyncSceneManager.cs, implements IGameModule)
  ├── [SaveManager] (Script: SaveManager.cs, implements IGameModule)
  └── [FutureSystem...] (e.g., AudioManager)
```

## IGameModule 接口

```csharp
public interface IGameModule
{
    void OnInit();                  // 初始化
    void OnUpdate(float deltaTime); // 轮询（可选）
    void OnDispose();               // 销毁清理
}
```

## 使用方式

### 1. 场景设置

在启动场景创建 GameManager 结构：

```
Hierarchy:
└── [GameManager]           <- 挂载 GameManager.cs
    └── [AsyncSceneManager] <- 挂载 AsyncSceneManager.cs
```

### 2. 获取模块

```csharp
// 方式1：通过GameManager获取（推荐）
var sceneManager = GameManager.Get<AsyncSceneManager>();
sceneManager.LoadScene("GameScene");

// 方式2：通过Instance访问（兼容按钮事件）
AsyncSceneManager.Instance.LoadScene("GameScene");
```

### 3. 按钮事件绑定

在 Inspector 中绑定按钮事件时，仍可使用 `AsyncSceneManager.Instance.LoadScene`，
Instance 会自动从 GameManager 获取模块引用。

### 4. 创建新模块

```csharp
public class SaveManager : MonoBehaviour, IGameModule
{
    public void OnInit()
    {
        // 初始化逻辑
    }

    public void OnUpdate(float deltaTime)
    {
        // 轮询逻辑（可选）
    }

    public void OnDispose()
    {
        // 清理逻辑
    }
}
```

## 兼容性说明

### AsyncSceneManager 兼容性

- **新代码**：推荐使用 `GameManager.Get<AsyncSceneManager>()`
- **旧代码/按钮事件**：`AsyncSceneManager.Instance` 仍然可用
- Instance 属性会优先从 GameManager 获取，回退到自动创建

### MessageManager

`MessageManager` 保持为静态类，无需修改，可直接使用。

## 初始化顺序

1. `GameManager.Awake()` 调用 `DontDestroyOnLoad`
2. 收集所有子节点上的 `IGameModule` 组件
3. 按子节点顺序调用各模块的 `OnInit()`
4. 场景切换时 GameManager 及其子节点保持存在

## 调试

在 GameManager 组件上右键选择 **Print Modules** 可查看已注册的模块列表。

## 自动初始化 (开发阶段)

`GameManagerAutoInitializer` 使用 `[RuntimeInitializeOnLoadMethod]` 特性，
在任何场景进入运行模式时自动检测并创建 GameManager 结构。

### 工作原理

1. 在 `BeforeSceneLoad` 时机检查是否存在 GameManager
2. 如果不存在，自动创建完整的模块结构
3. 包含：AsyncSceneManager、SaveManager

### 注意事项

- **开发阶段**：任何场景都可直接运行，无需手动创建 GameManager
- **发布阶段**：建议在入口场景预先放置 GameManager 预制体
- 自动创建的 GameManager 会在控制台输出日志

### ⚠️ 重要：移除其他场景的 GameManager

**只有入口场景可以放置 GameManager**，其他场景必须移除预先放置的 GameManager。

原因：
1. GameManager 使用 `DontDestroyOnLoad`，跨场景持久存在
2. 如果目标场景也有 GameManager，会触发重复检测和销毁
3. 这可能导致模块状态丢失和功能异常

解决方案：
- 删除非入口场景中的 `[GameManager]` 对象
- 依赖 `GameManagerAutoInitializer` 自动创建（开发阶段）
- 或只在入口场景放置 GameManager 预制体（发布阶段）
- **UI 按钮事件**：使用 `SceneLoaderProxy` 组件代替直接引用 AsyncSceneManager

### SceneLoaderProxy 使用方法

对于需要在 UI 按钮上绑定场景加载的场景：

1. 在场景中创建空对象，添加 `SceneLoaderProxy` 组件
2. 按钮 OnClick 事件绑定到 `SceneLoaderProxy.LoadScene(string)`
3. 填入目标场景名称

```
Hierarchy:
└── Canvas
    └── Button
         └── OnClick: SceneLoaderProxy.LoadScene("TargetScene")

└── [SceneLoaderProxy]  <- 添加 SceneLoaderProxy 组件
```

SceneLoaderProxy 内部会调用 `AsyncSceneManager.Instance`，无需场景中有 GameManager。

## 已集成模块

| 模块 | 文件 | 职责 |
|------|------|------|
| AsyncSceneManager | `0_AsySceneManager/AsyncSceneManager.cs` | 异步场景加载 |
| SaveManager | `0_SaveSystem/Core/SaveManager.cs` | 游戏存档管理 |
