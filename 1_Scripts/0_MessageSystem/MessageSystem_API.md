# 全局消息系统 - 技术文档与API参考

## 概述

本消息系统基于**发布-订阅模式（Pub/Sub）**实现，用于Unity项目中模块间的低耦合通信。

### 核心特性

- 泛型支持：支持0-3个参数的消息传递
- 类型安全：编译期检查参数类型
- 自动清理：场景切换时自动清理非永久事件
- 调试友好：可选日志输出，支持事件表打印

---

## 架构图

```
┌─────────────────────────────────────────────────────────┐
│                    EventManager (静态类)                 │
├─────────────────────────────────────────────────────────┤
│  Dictionary<EventType, Delegate> eventTable             │
│  List<EventType> permanentEvents                        │
├─────────────────────────────────────────────────────────┤
│  + AddListener<T>(eventType, callback)    // 注册监听   │
│  + RemoveListener<T>(eventType, callback) // 移除监听   │
│  + Broadcast<T>(eventType, data)          // 广播消息   │
│  + MarkAsPermanent(eventType)             // 标记永久   │
│  + Cleanup()                              // 清理事件   │
└─────────────────────────────────────────────────────────┘
           ↑                              ↓
    [订阅者注册监听]                [广播消息给订阅者]
```

---

## 文件结构

| 文件 | 说明 |
|------|------|
| `Callback.cs` | 委托定义，支持0-3个泛型参数 |
| `EventType.cs` | 事件类型枚举，按模块分类管理 |
| `EventData.cs` | 事件数据封装类（可选使用） |
| `EventManager.cs` | 核心消息管理器 |
| `EventManagerHelper.cs` | 场景切换自动清理组件 |

---

## API 参考

### 1. 注册监听 - AddListener

```csharp
// 无参数
EventManager.AddListener(EventType eventType, Callback handler);

// 1个参数
EventManager.AddListener<T>(EventType eventType, Callback<T> handler);

// 2个参数
EventManager.AddListener<T, U>(EventType eventType, Callback<T, U> handler);

// 3个参数
EventManager.AddListener<T, U, V>(EventType eventType, Callback<T, U, V> handler);
```

**参数说明：**
- `eventType`: 事件类型枚举值
- `handler`: 回调函数

**示例：**
```csharp
EventManager.AddListener(EventType.GAME_START, OnGameStart);
EventManager.AddListener<int>(EventType.PLAYER_HURT, OnPlayerHurt);
EventManager.AddListener<string, int>(EventType.ITEM_PICKUP, OnItemPickup);
```

---

### 2. 移除监听 - RemoveListener

```csharp
// 无参数
EventManager.RemoveListener(EventType eventType, Callback handler);

// 1个参数
EventManager.RemoveListener<T>(EventType eventType, Callback<T> handler);

// 2个参数
EventManager.RemoveListener<T, U>(EventType eventType, Callback<T, U> handler);

// 3个参数
EventManager.RemoveListener<T, U, V>(EventType eventType, Callback<T, U, V> handler);
```

**⚠️ 重要：** 必须与AddListener成对使用，参数签名必须完全一致。

---

### 3. 广播消息 - Broadcast

```csharp
// 无参数
EventManager.Broadcast(EventType eventType);

// 1个参数
EventManager.Broadcast<T>(EventType eventType, T arg1);

// 2个参数
EventManager.Broadcast<T, U>(EventType eventType, T arg1, U arg2);

// 3个参数
EventManager.Broadcast<T, U, V>(EventType eventType, T arg1, U arg2, V arg3);
```

**示例：**
```csharp
EventManager.Broadcast(EventType.GAME_START);
EventManager.Broadcast<int>(EventType.DAMAGE_DEALT, 50);
EventManager.Broadcast<string, int>(EventType.ITEM_PICKUP, "金币", 100);
```

---

### 4. 永久事件 - MarkAsPermanent

```csharp
EventManager.MarkAsPermanent(EventType eventType);
```

标记为永久事件后，场景切换时不会被清理。适用于：
- 全局音频管理
- 成就系统
- 存档系统

---

### 5. 调试方法

```csharp
// 启用/禁用日志
EventManager.SetLogEnabled(bool enabled);

// 打印当前事件表
EventManager.PrintEventTable();

// 检查是否有监听器
bool hasListener = EventManager.HasListener(EventType eventType);
```

---

## 使用规范

### 标准使用模板

```csharp
public class ExampleComponent : MonoBehaviour
{
    void OnEnable()
    {
        // 注册监听
        EventManager.AddListener(EventType.GAME_START, OnGameStart);
        EventManager.AddListener<int>(EventType.PLAYER_HURT, OnPlayerHurt);
    }

    void OnDisable()
    {
        // 移除监听（必须！）
        EventManager.RemoveListener(EventType.GAME_START, OnGameStart);
        EventManager.RemoveListener<int>(EventType.PLAYER_HURT, OnPlayerHurt);
    }

    // 回调函数
    void OnGameStart()
    {
        Debug.Log("游戏开始");
    }

    void OnPlayerHurt(int damage)
    {
        Debug.Log($"玩家受到 {damage} 点伤害");
    }
}
```

### 生命周期建议

| 时机 | 操作 |
|------|------|
| `OnEnable` / `Start` | 注册监听 |
| `OnDisable` / `OnDestroy` | 移除监听 |

---

## 添加新事件类型

在 `EventType.cs` 中添加：

```csharp
public enum EventType
{
    // ... 现有事件 ...

    // ========== 新模块事件 ==========
    NEW_MODULE_EVENT_1,
    NEW_MODULE_EVENT_2,
}
```

---

## 常见问题

### Q1: MissingReferenceException 空引用错误
**原因：** 对象销毁后未移除监听  
**解决：** 确保在 `OnDisable` 或 `OnDestroy` 中移除所有监听

### Q2: 签名不一致错误
**原因：** AddListener 和 Broadcast 的参数类型不匹配  
**解决：** 检查泛型参数类型是否一致

### Q3: 场景切换后事件丢失
**原因：** 事件被自动清理  
**解决：** 使用 `MarkAsPermanent()` 标记为永久事件

---

## 性能建议

1. 避免在 `Update` 中频繁广播
2. 复杂数据使用类/结构体封装，而非多参数
3. 及时移除不需要的监听器
4. 生产环境关闭日志：`EventManager.SetLogEnabled(false)`

---

## 监控器使用

### 打开监控器

菜单栏：`Tools > Event Monitor` 或快捷键 `Ctrl+Shift+E`

### 监控器功能

| 功能 | 说明 |
|------|------|
| 动态/静态模式 | 动态模式实时刷新，静态模式手动刷新 |
| 暂停 | 暂停消息记录（编辑/运行模式均可） |
| 过滤 | 按文本、操作类型过滤消息 |
| 颜色标签 | 不同事件类型显示不同颜色 |

### 颜色说明

| 颜色 | 事件前缀 |
|------|----------|
| 🟢 绿色 | GAME_ (系统事件) |
| 🔵 蓝色 | PLAYER_ (玩家事件) |
| 🟡 黄色 | UI_ (界面事件) |
| 🔴 红色 | ENEMY_ (敌人事件) |
| 🟣 紫色 | ITEM_ (道具事件) |
| 🟠 橙色 | AUDIO_ (音频事件) |
| 🩷 粉色 | DAMAGE_ (伤害事件) |
| 🩵 青色 | SCENE_ (场景事件) |

### 监控器API

```csharp
// 启用/禁用监控
EventMonitor.IsEnabled = true;

// 暂停/恢复记录
EventMonitor.IsPaused = false;

// 清空记录
EventMonitor.ClearRecords();

// 设置最大记录数
EventMonitor.MaxRecords = 500;
```

---

## 版本记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.1 | 2026-01-14 | 添加 Event Monitor 监控器窗口 |
| 1.0 | 2026-01-14 | 初始版本，基础消息系统实现 |
