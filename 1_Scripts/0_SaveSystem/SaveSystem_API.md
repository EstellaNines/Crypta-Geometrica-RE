# Crypta Save System - API 文档

> **版本**: 1.0.0
> **最后更新**: 2026-01-14

---

## 1. 快速开始

### 1.1 使游戏对象可保存

1. 添加 `SaveableEntity` 组件（自动生成 GUID）
2. 实现 `ISaveable` 接口

```csharp
using UnityEngine;

public class Chest : MonoBehaviour, ISaveable
{
    [SerializeField] private SaveableEntity saveableEntity;
    [SerializeField] private bool isOpened;

    // 实现 ISaveable 接口
    public string SaveID => saveableEntity.ID;

    public object CaptureState()
    {
        return new ChestState { isOpened = this.isOpened };
    }

    public void RestoreState(object state)
    {
        // state 是 JSON 字符串，需要反序列化
        if (state is string json)
        {
            var data = JsonUtility.FromJson<ChestState>(json);
            this.isOpened = data.isOpened;
            UpdateVisual();
        }
    }

    private void OnEnable()
    {
        SaveManager.Instance?.RegisterSaveable(this);
    }

    private void OnDisable()
    {
        SaveManager.Instance?.UnregisterSaveable(this);
    }

    [System.Serializable]
    private class ChestState
    {
        public bool isOpened;
    }
}
```

### 1.2 保存和加载

```csharp
// 保存到槽位 0
SaveManager.Instance.SaveGame(0);

// 从槽位 0 加载
SaveManager.Instance.LoadGame(0);

// 使用消息系统
MessageManager.Broadcast<int>(MessageType.SAVE_GAME_REQUEST, 0);
MessageManager.Broadcast<int>(MessageType.LOAD_GAME_REQUEST, 0);
```

---

## 2. 核心 API

### 2.1 SaveManager

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | SaveManager | 单例实例 |
| `UseEncryption` | bool | 是否使用加密 |
| `CurrentSlotIndex` | int | 当前槽位索引 |
| `CurrentData` | SaveData | 当前存档数据 |

#### 方法

| 方法 | 参数 | 说明 |
|------|------|------|
| `RegisterSaveable(ISaveable)` | saveable | 注册可保存对象 |
| `UnregisterSaveable(ISaveable)` | saveable | 取消注册 |
| `RegisterPCGSaveable(IPCGSaveable)` | saveable | 注册 PCG 对象 |
| `SaveGame(int)` | slotIndex | 保存到指定槽位 |
| `LoadGame(int)` | slotIndex | 从指定槽位加载 |
| `AutoSave()` | - | 自动保存到当前槽位 |
| `GetSlotHeader(int)` | slotIndex | 获取槽位头信息 |
| `HasSaveData(int)` | slotIndex | 检查槽位是否有存档 |
| `DeleteSave(int)` | slotIndex | 删除指定槽位存档 |
| `SetCurrentSlot(int)` | slotIndex | 设置当前槽位 |
| `SetUseEncryption(bool)` | encrypt | 设置是否加密 |

### 2.2 SaveUtility (静态工具类)

| 方法 | 说明 |
|------|------|
| `GetSaveDirectory()` | 获取存档目录路径 |
| `GetSavePath(int, bool)` | 获取存档文件路径 |
| `ToJson(SaveData)` | 序列化为 JSON |
| `FromJson(string)` | 从 JSON 反序列化 |
| `SaveToFile(string, SaveData, bool)` | 保存到文件 |
| `LoadFromFile(string, bool)` | 从文件加载 |
| `ValidateFile(string)` | 验证文件有效性 |
| `HasSaveData(int)` | 检查槽位是否有数据 |

---

## 3. 消息类型

### 3.1 存档相关消息

| 消息类型 | 参数 | 说明 |
|----------|------|------|
| `SAVE_GAME_REQUEST` | int (slotIndex) | 请求保存游戏 |
| `LOAD_GAME_REQUEST` | int (slotIndex) | 请求加载游戏 |
| `SAVE_OPERATION_DONE` | bool, string | 保存完成 (成功?, 消息) |
| `LOAD_OPERATION_DONE` | bool, string | 加载完成 (成功?, 消息) |
| `AUTO_SAVE_TRIGGERED` | int (slotIndex) | 自动保存触发 |
| `LEVEL_ENTERED` | string (levelName) | 进入关卡 (触发自动保存) |
| `LEVEL_COMPLETED` | string (levelName) | 关卡通关 (触发自动保存) |

### 3.2 监听示例

```csharp
void OnEnable()
{
    MessageManager.AddListener<bool, string>(MessageType.SAVE_OPERATION_DONE, OnSaveDone);
}

void OnDisable()
{
    MessageManager.RemoveListener<bool, string>(MessageType.SAVE_OPERATION_DONE, OnSaveDone);
}

void OnSaveDone(bool success, string message)
{
    if (success)
        ShowNotification("保存成功");
    else
        ShowError(message);
}
```

---

## 4. 数据结构

### 4.1 SaveData

```csharp
[Serializable]
public class SaveData
{
    public SaveHeader header;           // 头信息
    public GlobalSaveData globalData;   // 全局数据
    public PCGSaveData pcgData;         // PCG 数据
    public SerializableDictionary<string, string> entityData;  // 实体数据
    public List<DynamicObjectData> dynamicObjects;  // 动态物体
}
```

### 4.2 SaveHeader

```csharp
[Serializable]
public class SaveHeader
{
    public string version;      // 版本号
    public string timestamp;    // 时间戳
    public float playTime;      // 游玩时间 (秒)
    public string sceneName;    // 场景名
    public int slotIndex;       // 槽位索引
}
```

---

## 5. 存档槽位

| 槽位 | 索引 | 说明 |
|------|------|------|
| 槽位 1 | 0 | 正式存档 |
| 槽位 2 | 1 | 正式存档 |
| 槽位 3 | 2 | 正式存档 |
| 测试槽 | -1 | 开发测试用 |

---

## 6. 文件位置

### 开发模式 (Unity Editor)
```
Assets/Resources/Save/
├── slot_0.json
├── slot_1.json
├── slot_2.json
└── slot_debug.json
```

### 发布模式
```
[persistentDataPath]/Save/
├── slot_0.crypta
├── slot_1.crypta
├── slot_2.crypta
└── slot_debug.crypta
```

---

## 7. PCG 扩展

### 7.1 实现 IPCGSaveable

```csharp
public class LevelGenerator : MonoBehaviour, IPCGSaveable
{
    private int masterSeed;

    public int MasterSeed => masterSeed;

    public PCGSaveData CapturePCGState()
    {
        var data = new PCGSaveData();
        data.Initialize(masterSeed, 10);
        return data;
    }

    public void RestorePCGState(PCGSaveData state)
    {
        masterSeed = state.masterSeed;
        RegenerateLevel(state.GetCurrentLevelSeed());
    }

    void OnEnable()
    {
        SaveManager.Instance?.RegisterPCGSaveable(this);
    }

    void OnDisable()
    {
        SaveManager.Instance?.UnregisterPCGSaveable();
    }
}
```

---

## 8. 自动保存

自动保存在以下时机触发：
1. **进入关卡时** - 广播 `LEVEL_ENTERED` 消息
2. **通关时** - 广播 `LEVEL_COMPLETED` 消息

### 触发自动保存

```csharp
// 关卡入口调用
MessageManager.Broadcast<string>(MessageType.LEVEL_ENTERED, "Level_1");

// 通关时调用
MessageManager.Broadcast<string>(MessageType.LEVEL_COMPLETED, "Level_1");
```

---

## 9. 注意事项

1. **GUID 唯一性**: 确保每个需要保存的对象都有唯一的 SaveID
2. **序列化**: 状态数据必须使用 `[Serializable]` 标记
3. **注册时机**: 在 `OnEnable` 中注册，`OnDisable` 中取消注册
4. **场景切换**: 加载存档会自动切换到保存时的场景
5. **加密模式**: 发布时建议开启 `useEncryption`
