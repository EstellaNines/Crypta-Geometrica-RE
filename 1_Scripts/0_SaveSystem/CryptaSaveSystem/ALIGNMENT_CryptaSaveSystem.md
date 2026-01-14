# ALIGNMENT - Crypta Save System (专属加密存档系统)

## 1. 项目上下文分析

### 1.1 现有技术栈
- **游戏引擎**: Unity (2D Roguelite)
- **架构模式**: 容器化服务架构 (GameManager 统一管理模块)
- **通信机制**: 发布-订阅模式 (MessageManager)
- **场景管理**: 异步场景加载 (AsyncSceneManager)
- **接口规范**: IGameModule 接口标准

### 1.2 现有模块
| 模块 | 职责 | 状态 |
|------|------|------|
| GameManager | 游戏根节点，统一管理所有模块 | ✅ 已实现 |
| AsyncSceneManager | 异步场景加载管理 | ✅ 已实现 |
| MessageManager | 全局消息发布订阅 | ✅ 已实现 |
| DataExportManager | 数据导出管理 | ✅ 已实现 |
| SaveManager | 保存系统管理 | ⏳ 待实现 |

### 1.3 PCG 集成需求
根据 `0_HybridPCGRandomizedLevels` 文档，保存系统需要支持：
- **种子机制序列化**: 仅序列化种子+元数据，而非整个关卡
- **RoomNode 数据结构**: 宏观网格坐标、房间类型、连通性掩码等
- **动态物体处理**: 对象池管理的 Lease/Return 机制
- **可重现的随机性**: 基于 Unity.Mathematics.Random

---

## 2. 原始需求理解

### 2.1 核心需求
1. **开发环境**: 使用 JSON 明文保存，便于调试
2. **发布环境**: 使用 .crypta 加密格式，保护玩家数据
3. **可扩展性**: 预留 PCG 程序化关卡数据保存接口
4. **架构集成**: 作为 GameManager 子模块运行

### 2.2 用户提供的架构方案

#### 三层汉堡结构
```
表现层 (Presentation Layer): 游戏对象通过 ISaveable 接口交互
逻辑层 (Logic Layer): SaveManager 负责数据收集、分发、缓存
持久层 (Persistence Layer): SaveUtility 负责加密、二进制转换、磁盘读写
```

#### 数据协议
- **文件头 (Header)**: SaveVersion, Timestamp, ScreenshotPath, PlayTime
- **数据体 (Body)**: 全局数据 + 模块化字典 (Dictionary<string, string>)

#### 文件格式 (.crypta)
```
|-- Header (校验区) --|-- Body (数据区) -----------------------|
| "CG2026" (6 bytes) | [加密后的二进制数据流 ...................] |
```

---

## 3. 边界确认

### 3.1 本次实现范围 (IN SCOPE)
- ✅ SaveManager 模块实现
- ✅ SaveUtility 工具类实现
- ✅ ISaveable 接口定义
- ✅ SaveableEntity 组件 (GUID 系统)
- ✅ SaveData 数据结构
- ✅ JSON/Crypta 双模式切换
- ✅ MessageManager 集成
- ✅ AsyncSceneManager 集成
- ✅ PCG 扩展接口预留

### 3.2 本次不实现 (OUT OF SCOPE)
- ❌ 存档 UI 界面
- ❌ 截图功能
- ❌ 云存档同步
- ❌ 具体的 PCG 数据序列化实现
- ❌ 存档槽位管理 UI

---

## 4. 疑问澄清

### 4.1 关键决策点

| # | 问题 | 推荐方案 | 依据 |
|---|------|----------|------|
| 1 | 加密算法选择 | XOR 对称加密 | 用户方案中已指定，简单高效 |
| 2 | 存档位置 | `Application.persistentDataPath` | Unity 跨平台标准 |
| 3 | 版本兼容策略 | 版本号+向后兼容解析 | 行业最佳实践 |
| 4 | GUID 生成时机 | 编辑器模式下生成 | 用户方案中已指定 |
| 5 | 动态物体标识 | PrefabPath + RuntimeID | 用户方案中已指定 |

### 4.2 待确认问题 (需用户回答)

**Q1: 加密密钥管理**
- 方案A: 硬编码密钥 (简单但安全性较低)
- 方案B: 基于设备ID生成密钥 (更安全但可能影响存档迁移)
- **推荐**: 方案A，配合混淆和魔数校验

**Q2: 多存档槽位数量**
- 默认实现几个存档槽位？
- **推荐**: 3个槽位 (行业标准)

**Q3: 自动保存策略**
- 是否需要自动保存功能？
- **推荐**: 预留接口，但默认关闭

**Q4: PCG 种子保存策略**
- 是仅保存主种子还是保存完整的 RoomNode 矩阵？
- **推荐**: 根据文档建议，仅保存种子+元数据，加载时重新生成

---

## 5. 技术约束

### 5.1 必须遵循
- 实现 `IGameModule` 接口
- 通过 `MessageManager` 进行事件通信
- 与 `AsyncSceneManager` 协同工作
- 遵循现有代码命名规范

### 5.2 性能要求
- 保存操作应异步执行，避免卡顿
- 加载操作应支持进度回调
- 避免频繁的 GC 分配

---

## 6. 验收标准

### 6.1 功能验收
- [ ] 可以保存游戏状态到 JSON/Crypta 文件
- [ ] 可以从 JSON/Crypta 文件加载游戏状态
- [ ] 场景切换时数据正确恢复
- [ ] 支持多存档槽位
- [ ] 损坏文件能正确检测并报错

### 6.2 集成验收
- [ ] 作为 GameManager 子模块正常运行
- [ ] MessageManager 消息正确收发
- [ ] AsyncSceneManager 场景跳转正常
- [ ] PCG 扩展接口可用

### 6.3 代码质量
- [ ] 遵循项目现有代码规范
- [ ] 有完整的 XML 文档注释
- [ ] 关键流程有 Debug.Log 输出

---

## 7. 下一步行动

1. 等待用户确认疑问点 (Q1-Q4)
2. 创建 CONSENSUS 文档
3. 进入架构设计阶段
