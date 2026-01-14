# CONSENSUS - Crypta Save System (专属加密存档系统)

> **文档状态**: ✅ 已确认
> **确认日期**: 2026-01-14
> **版本**: v1.0

---

## 1. 需求描述

### 1.1 核心功能
为 CRYPTA GEOMETRICA 游戏实现一套专属的加密存档系统，支持：
- 开发环境下的 JSON 明文存档（便于调试）
- 发布环境下的 .crypta 加密存档（保护玩家数据）
- 程序化关卡（PCG）的种子数据持久化
- 与现有 GameManager 架构无缝集成

### 1.2 确认的决策点

| # | 决策项 | 最终方案 |
|---|--------|----------|
| Q1 | 加密密钥管理 | **硬编码密钥 + 混淆** |
| Q2 | 存档槽位数量 | **3个正式槽位 + 1个测试槽位** (共4个) |
| Q3 | 自动保存策略 | **进入关卡时自动保存一次，通关时再次保存**，中途不保存 |
| Q4 | PCG种子保存 | **仅保存种子+元数据**，加载时重新生成关卡 |

---

## 2. 技术实现方案

### 2.1 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                      GameManager                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                    SaveManager                          │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │ │
│  │  │ ISaveable    │  │ SaveData     │  │ SaveSlot     │  │ │
│  │  │ Registry     │  │ Cache        │  │ Manager      │  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  │ │
│  │                         │                               │ │
│  │                         ▼                               │ │
│  │              ┌──────────────────────┐                   │ │
│  │              │    SaveUtility       │                   │ │
│  │              │  ┌────────────────┐  │                   │ │
│  │              │  │ JSON Serialize │  │                   │ │
│  │              │  │ XOR Encrypt    │  │                   │ │
│  │              │  │ File I/O       │  │                   │ │
│  │              │  └────────────────┘  │                   │ │
│  │              └──────────────────────┘                   │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 核心组件

#### 2.2.1 SaveManager (MonoBehaviour, IGameModule)
- **职责**: 数据收集、分发、缓存，协调保存/加载流程
- **依赖**: MessageManager, AsyncSceneManager

#### 2.2.2 SaveUtility (Static Class)
- **职责**: JSON序列化、XOR加密、文件读写
- **特性**: 纯静态工具类，无状态

#### 2.2.3 ISaveable (Interface)
- **职责**: 定义可保存对象的标准接口
- **方法**: `SaveID`, `CaptureState()`, `RestoreState()`

#### 2.2.4 SaveableEntity (MonoBehaviour)
- **职责**: 为游戏对象提供唯一GUID标识
- **特性**: 编辑器模式下自动生成GUID

#### 2.2.5 SaveData (Data Class)
- **职责**: 存档数据的内存表示
- **结构**: Header + Body (模块化字典)

### 2.3 文件格式

#### JSON 模式 (开发环境)
```json
{
  "header": {
    "version": "1.0.0",
    "timestamp": "2026-01-14T22:48:00",
    "playTime": 3600,
    "sceneName": "Level_1"
  },
  "globalData": {
    "difficulty": 1,
    "currentLevel": 1
  },
  "pcgData": {
    "masterSeed": 12345678,
    "levelSeeds": [111, 222, 333]
  },
  "entityData": {
    "guid-001": "{\"health\":100,\"position\":{\"x\":0,\"y\":0}}",
    "guid-002": "{\"isOpened\":true}"
  },
  "dynamicObjects": []
}
```

#### Crypta 模式 (发布环境)
```
[Magic: "CG2026" (6 bytes)] + [XOR Encrypted JSON Bytes]
```

### 2.4 存档槽位

| 槽位 | ID | 用途 |
|------|-----|------|
| 槽位1 | slot_0 | 正式存档 |
| 槽位2 | slot_1 | 正式存档 |
| 槽位3 | slot_2 | 正式存档 |
| 测试槽 | slot_debug | 开发测试用 |

### 2.5 自动保存时机

```
游戏流程:
  [主菜单] → [选择存档槽] → [进入关卡] → [游戏中] → [Boss战] → [通关]
                              │                              │
                        ✅ 自动保存                      ✅ 自动保存
```

---

## 3. 消息通信协议

### 3.1 新增消息类型

```csharp
// MessageType.cs 新增
SAVE_GAME_REQUEST,      // 参数: int slotIndex
LOAD_GAME_REQUEST,      // 参数: int slotIndex
SAVE_OPERATION_DONE,    // 参数: bool success, string message
LOAD_OPERATION_DONE,    // 参数: bool success, string message
AUTO_SAVE_TRIGGERED,    // 参数: int slotIndex
```

### 3.2 与 AsyncSceneManager 集成

加载存档流程:
1. SaveManager 预读取存档 Header
2. 确认目标场景名
3. AsyncSceneManager 执行场景跳转
4. 场景加载完成后，SaveManager 恢复实体数据

---

## 4. PCG 扩展接口

### 4.1 IPCGSaveable 接口 (预留)
```csharp
public interface IPCGSaveable
{
    int MasterSeed { get; }
    object CapturePCGState();
    void RestorePCGState(object state);
}
```

### 4.2 PCG 数据结构 (预留)
```csharp
[Serializable]
public class PCGSaveData
{
    public int masterSeed;
    public List<int> levelSeeds;
    public Dictionary<string, object> metadata;
}
```

---

## 5. 技术约束

### 5.1 必须遵循
- ✅ 实现 `IGameModule` 接口
- ✅ 通过 `MessageManager` 进行事件通信
- ✅ 保存操作异步执行
- ✅ 遵循现有代码命名规范
- ✅ 完整的 XML 文档注释

### 5.2 文件路径
```csharp
// 存档目录
Application.persistentDataPath + "/Saves/"

// 文件命名
slot_0.json / slot_0.crypta
slot_1.json / slot_1.crypta
slot_2.json / slot_2.crypta
slot_debug.json / slot_debug.crypta
```

---

## 6. 验收标准

### 6.1 功能验收
- [ ] 可以保存游戏状态到 4 个存档槽位
- [ ] 可以从存档加载并恢复游戏状态
- [ ] 进入关卡时自动保存一次
- [ ] 通关时自动保存一次
- [ ] 开发模式使用 JSON，发布模式使用 Crypta
- [ ] 损坏文件能正确检测（Magic Number 校验）

### 6.2 集成验收
- [ ] 作为 GameManager 子模块正常运行
- [ ] MessageManager 消息正确收发
- [ ] AsyncSceneManager 场景跳转后数据正确恢复
- [ ] PCG 扩展接口可用

### 6.3 代码质量
- [ ] 遵循项目现有代码规范
- [ ] 有完整的 XML 文档注释
- [ ] 关键流程有 Debug.Log 输出

---

## 7. 所有不确定性已解决

| 原始疑问 | 解决方案 |
|----------|----------|
| 加密密钥管理 | 硬编码 + 混淆 ✅ |
| 存档槽位数量 | 3正式 + 1测试 ✅ |
| 自动保存策略 | 进入关卡 + 通关 ✅ |
| PCG保存策略 | 种子+元数据 ✅ |

---

**下一步**: 进入 Architect 阶段，创建 DESIGN 文档
