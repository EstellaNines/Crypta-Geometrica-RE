# Odin Inspector API 参考文档 - Part 1: Attributes

## 目录
1. [Odin Inspector 简介](#odin-inspector-简介)
2. [Type Specifics (类型特定)](#type-specifics)
3. [Essentials (核心特性)](#essentials)
4. [Validation (验证)](#validation)
5. [Groups (分组)](#groups)
6. [Buttons (按钮)](#buttons)
7. [Collections (集合)](#collections)
8. [Conditionals (条件显示)](#conditionals)
9. [Numbers (数值)](#numbers)
10. [Misc (杂项)](#misc)

---

## Odin Inspector 简介

**Odin Inspector** 是 Unity 最强大的 Inspector 扩展插件,提供 100+ 个 Attributes 用于增强编辑器体验。

### 命名空间
```csharp
using Sirenix.OdinInspector;
```

---

## Type Specifics (类型特定)

这些 Attributes 用于特定类型的字段显示和交互。

| Attribute | 描述 | 适用类型 |
|-----------|------|----------|
| `[AssetList]` | 显示项目中所有指定类型的资源列表 | Object引用 |
| `[AssetSelector]` | 自定义资源选择器 | Object引用 |
| `[ChildGameObjectsOnly]` | 只允许选择子物体 | GameObject |
| `[ColorPalette]` | 显示颜色调色板 | Color |
| `[DisplayAsString]` | 将值显示为字符串 | 任意类型 |
| `[EnumPaging]` | 枚举分页显示 | Enum |
| `[EnumToggleButtons]` | 枚举显示为切换按钮 | Enum |
| `[FilePath]` | 文件路径选择器 | string |
| `[FolderPath]` | 文件夹路径选择器 | string |
| `[HideInInlineEditors]` | 在内联编辑器中隐藏 | 任意 |
| `[HideInTables]` | 在表格中隐藏 | 任意 |
| `[HideMonoScript]` | 隐藏脚本引用 | MonoBehaviour |
| `[HideNetworkBehaviourFields]` | 隐藏网络行为字段 | NetworkBehaviour |
| `[HideReferenceObjectPicker]` | 隐藏引用对象选择器 | 引用类型 |
| `[InlineEditor]` | 内联编辑器 | Object引用 |
| `[MultiLineProperty]` | 多行属性 | string |
| `[PreviewField]` | 显示预览 | Texture/Sprite |
| `[PolymorphicDrawerSettings]` | 多态绘制设置 | 基类/接口 |
| `[TypeDrawerSettings]` | 类型绘制设置 | 任意 |
| `[SceneObjectsOnly]` | 只允许场景对象 | Object引用 |
| `[TableList]` | 表格列表显示 | List/Array |
| `[TableMatrix]` | 表格矩阵显示 | 2D数组 |
| `[Toggle]` | 切换按钮 | bool |
| `[ToggleLeft]` | 左侧切换按钮 | bool |

### 示例
```csharp
// 资源列表
[AssetList(Path = "Assets/Prefabs")]
public GameObject prefab;

// 颜色调色板
[ColorPalette("MyPalette")]
public Color color;

// 文件路径
[FilePath(Extensions = "txt,json")]
public string configPath;

// 内联编辑器
[InlineEditor]
public ScriptableObject data;

// 表格列表
[TableList]
public List<ItemData> items;
```

---

## Essentials (核心特性)

最常用的核心 Attributes。

| Attribute | 描述 | 用途 |
|-----------|------|------|
| `[AssetsOnly]` | 只允许项目资源 | 验证 |
| `[CustomValueDrawer]` | 自定义值绘制器 | 自定义显示 |
| `[DelayedProperty]` | 延迟应用更改 | 性能优化 |
| `[DetailedInfoBox]` | 详细信息框 | 提示信息 |
| `[EnableGUI]` | 启用GUI | 控制交互 |
| `[GUIColor]` | GUI颜色 | 视觉标识 |
| `[HideLabel]` | 隐藏标签 | 布局优化 |
| `[PropertyOrder]` | 属性排序 | 布局控制 |
| `[PropertySpace]` | 属性间距 | 布局控制 |
| `[ReadOnly]` | 只读显示 | 防止修改 |
| `[Required]` | 必填字段 | 验证 |
| `[RequiredIn]` | 特定模式必填 | 验证 |
| `[Searchable]` | 可搜索 | 大型列表 |
| `[ShowInInspector]` | 显示非序列化成员 | 调试/显示 |
| `[Title]` | 标题 | 分组标识 |
| `[TypeFilter]` | 类型过滤 | 类型选择 |
| `[TypeInfoBox]` | 类型信息框 | 类级别提示 |
| `[ValidateInput]` | 输入验证 | 自定义验证 |
| `[ValueDropdown]` | 值下拉列表 | 选项选择 |

### 示例
```csharp
// 只读
[ReadOnly]
public int score;

// 必填
[Required]
public GameObject player;

// GUI颜色
[GUIColor(0, 1, 0)]
public string successMessage;

// 隐藏标签
[HideLabel]
public string title;

// 属性排序
[PropertyOrder(1)]
public int health;

[PropertyOrder(2)]
public int maxHealth;

// 显示属性
[ShowInInspector]
public int ComputedValue => health * 2;

// 标题
[Title("Player Stats")]
public int strength;

// 值下拉
[ValueDropdown("GetDifficulties")]
public string difficulty;

private IEnumerable GetDifficulties() {
    return new[] { "Easy", "Normal", "Hard" };
}

// 输入验证
[ValidateInput("IsPositive", "Value must be positive!")]
public int value;

private bool IsPositive(int val) => val > 0;
```

---

## Validation (验证)

用于字段验证的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[AssetsOnly]` | 只允许项目资源,不允许场景对象 |
| `[ChildGameObjectsOnly]` | 只允许子物体 |
| `[DisallowModificationsIn]` | 禁止在特定模式下修改 |
| `[FilePath]` | 验证文件路径 |
| `[FolderPath]` | 验证文件夹路径 |
| `[MaxValue]` | 最大值限制 |
| `[MinMaxSlider]` | 最小最大值滑块 |
| `[MinValue]` | 最小值限制 |
| `[PropertyRange]` | 属性范围 |
| `[Range]` | Unity标准范围 |
| `[Required]` | 必填字段 |
| `[RequiredIn]` | 特定模式必填 |
| `[RequiredListLength]` | 列表长度要求 |
| `[SceneObjectsOnly]` | 只允许场景对象 |
| `[ValidateInput]` | 自定义输入验证 |

### 示例
```csharp
// 最小最大值
[MinValue(0)]
public int health;

[MaxValue(100)]
public int maxHealth;

// 范围滑块
[MinMaxSlider(0, 100)]
public Vector2 damageRange;

// 必填
[Required("Player reference is required!")]
public GameObject player;

// 特定模式必填
[RequiredIn(PrefabKind.PrefabAsset)]
public Sprite icon;

// 列表长度
[RequiredListLength(1, 10)]
public List<string> tags;

// 自定义验证
[ValidateInput("ValidateHealth")]
public int currentHealth;

private bool ValidateHealth(int value) {
    return value >= 0 && value <= maxHealth;
}
```

---

## Groups (分组)

用于组织和分组字段的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[BoxGroup]` | 盒子分组 |
| `[Button]` | 按钮(也可用于分组) |
| `[ButtonGroup]` | 按钮组 |
| `[FoldoutGroup]` | 折叠组 |
| `[HideIfGroup]` | 条件隐藏组 |
| `[HorizontalGroup]` | 水平分组 |
| `[ResponsiveButtonGroup]` | 响应式按钮组 |
| `[ShowIfGroup]` | 条件显示组 |
| `[TabGroup]` | 标签页分组 |
| `[TitleGroup]` | 标题分组 |
| `[ToggleGroup]` | 切换分组 |
| `[VerticalGroup]` | 垂直分组 |

### 示例
```csharp
// 盒子分组
[BoxGroup("Stats")]
public int health;

[BoxGroup("Stats")]
public int mana;

// 折叠组
[FoldoutGroup("Advanced Settings")]
public bool enableDebug;

[FoldoutGroup("Advanced Settings")]
public float debugInterval;

// 水平分组
[HorizontalGroup("Split")]
public int left;

[HorizontalGroup("Split")]
public int right;

// 标签页分组
[TabGroup("Settings", "General")]
public string playerName;

[TabGroup("Settings", "Graphics")]
public int quality;

[TabGroup("Settings", "Audio")]
public float volume;

// 切换分组
[ToggleGroup("EnableFeature")]
public bool EnableFeature;

[ToggleGroup("EnableFeature")]
public int featureValue;

// 标题分组
[TitleGroup("Player Data")]
public string name;

[TitleGroup("Player Data")]
public int level;
```

---

## Buttons (按钮)

用于在 Inspector 中添加按钮的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[Button]` | 方法按钮 |
| `[ButtonGroup]` | 按钮组 |
| `[EnumPaging]` | 枚举分页按钮 |
| `[EnumToggleButtons]` | 枚举切换按钮 |
| `[InlineButton]` | 内联按钮 |
| `[ResponsiveButtonGroup]` | 响应式按钮组 |

### 示例
```csharp
// 基本按钮
[Button]
public void ResetHealth() {
    health = maxHealth;
}

// 自定义按钮文本和大小
[Button("Heal Player", ButtonSizes.Large)]
public void Heal() {
    health = maxHealth;
}

// 按钮组
[ButtonGroup]
public void Save() { }

[ButtonGroup]
public void Load() { }

// 内联按钮
[InlineButton("Reset")]
public int value;

private void Reset() {
    value = 0;
}

// 枚举切换按钮
[EnumToggleButtons]
public GameState state;
```

---

## Collections (集合)

用于集合类型(List, Array, Dictionary)的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[DictionaryDrawerSettings]` | 字典绘制设置 |
| `[ListDrawerSettings]` | 列表绘制设置 |
| `[TableColumnWidth]` | 表格列宽 |
| `[TableList]` | 表格列表 |
| `[TableMatrix]` | 表格矩阵 |
| `[ValueDropdown]` | 值下拉列表 |

### 示例
```csharp
// 列表设置
[ListDrawerSettings(
    NumberOfItemsPerPage = 5,
    ShowIndexLabels = true,
    ShowPaging = true,
    Expanded = true
)]
public List<string> items;

// 自定义添加/删除
[ListDrawerSettings(
    CustomAddFunction = "AddItem",
    CustomRemoveIndexFunction = "RemoveItem"
)]
public List<ItemData> inventory;

private ItemData AddItem() {
    return new ItemData();
}

private void RemoveItem(int index) {
    inventory.RemoveAt(index);
}

// 表格列表
[TableList(ShowIndexLabels = true)]
public List<Enemy> enemies;

[System.Serializable]
public class Enemy {
    public string name;
    public int health;
    public float speed;
}

// 字典设置
[DictionaryDrawerSettings(
    KeyLabel = "Item ID",
    ValueLabel = "Count"
)]
public Dictionary<string, int> itemCounts;

// 表格矩阵
[TableMatrix(
    HorizontalTitle = "X",
    VerticalTitle = "Y",
    SquareCells = true
)]
public int[,] grid;
```

---

## Conditionals (条件显示)

根据条件控制字段显示/隐藏/启用/禁用的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[DisableIf]` | 条件禁用 |
| `[DisableIn]` | 特定模式禁用 |
| `[DisableInEditorMode]` | 编辑模式禁用 |
| `[DisableInInlineEditors]` | 内联编辑器禁用 |
| `[DisableInPlayMode]` | 播放模式禁用 |
| `[EnableIf]` | 条件启用 |
| `[EnableIn]` | 特定模式启用 |
| `[HideIf]` | 条件隐藏 |
| `[HideIfGroup]` | 条件隐藏组 |
| `[HideIn]` | 特定模式隐藏 |
| `[HideInEditorMode]` | 编辑模式隐藏 |
| `[HideInPlayMode]` | 播放模式隐藏 |
| `[ShowIf]` | 条件显示 |
| `[ShowIfGroup]` | 条件显示组 |
| `[ShowIn]` | 特定模式显示 |
| `[ShowInInlineEditors]` | 内联编辑器显示 |

### 示例
```csharp
public bool enableFeature;

// 条件显示
[ShowIf("enableFeature")]
public int featureValue;

// 条件隐藏
[HideIf("enableFeature")]
public string disabledMessage;

// 条件启用
[EnableIf("IsPlaying")]
public float playSpeed;

private bool IsPlaying() {
    return Application.isPlaying;
}

// 条件禁用
[DisableIf("@health <= 0")]
public bool canMove;

// 模式控制
[DisableInPlayMode]
public GameObject prefab;

[ShowInInlineEditors]
public string inlineInfo;

// 多条件
[ShowIf("@enableFeature && health > 0")]
public string statusMessage;
```

---

## Numbers (数值)

用于数值类型的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[MaxValue]` | 最大值 |
| `[MinMaxSlider]` | 最小最大滑块 |
| `[MinValue]` | 最小值 |
| `[ProgressBar]` | 进度条 |
| `[PropertyRange]` | 属性范围 |
| `[Unit]` | 单位显示 |
| `[Wrap]` | 值循环 |

### 示例
```csharp
// 最小最大值
[MinValue(0), MaxValue(100)]
public int health;

// 范围滑块
[MinMaxSlider(0, 100, true)]
public Vector2 damageRange;

// 进度条
[ProgressBar(0, 100, ColorGetter = "GetHealthColor")]
public float currentHealth;

private Color GetHealthColor(float value) {
    return Color.Lerp(Color.red, Color.green, value / 100f);
}

// 单位显示
[Unit(Units.Meter)]
public float distance;

[Unit(Units.Second)]
public float time;

// 属性范围
[PropertyRange(0, "maxHealth")]
public int health;
public int maxHealth = 100;

// 值循环
[Wrap(0, 360)]
public float angle;
```

---

## Misc (杂项)

其他实用的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[CustomContextMenu]` | 自定义右键菜单 |
| `[DisableContextMenu]` | 禁用右键菜单 |
| `[DrawWithUnity]` | 使用Unity默认绘制 |
| `[HideDuplicateReferenceBox]` | 隐藏重复引用框 |
| `[Indent]` | 缩进 |
| `[InfoBox]` | 信息框 |
| `[InlineProperty]` | 内联属性 |
| `[LabelText]` | 标签文本 |
| `[LabelWidth]` | 标签宽度 |
| `[OnCollectionChanged]` | 集合改变回调 |
| `[OnInspectorDispose]` | Inspector销毁回调 |
| `[OnInspectorGUI]` | Inspector GUI回调 |
| `[OnInspectorInit]` | Inspector初始化回调 |
| `[OnStateUpdate]` | 状态更新回调 |
| `[OnValueChanged]` | 值改变回调 |
| `[TypeSelectorSettings]` | 类型选择器设置 |
| `[TypeRegistryItem]` | 类型注册项 |
| `[PropertyTooltip]` | 属性提示 |
| `[SuffixLabel]` | 后缀标签 |

### 示例
```csharp
// 信息框
[InfoBox("This is important information!")]
public int value;

// 条件信息框
[InfoBox("Health is low!", InfoMessageType.Warning, "IsLowHealth")]
public int health;

private bool IsLowHealth() => health < 20;

// 标签文本
[LabelText("Player HP")]
public int health;

// 动态标签
[LabelText("$GetLabel")]
public string data;

private string GetLabel() => "Dynamic: " + data;

// 后缀标签
[SuffixLabel("kg")]
public float weight;

[SuffixLabel("m/s", Overlay = true)]
public float speed;

// 值改变回调
[OnValueChanged("OnHealthChanged")]
public int health;

private void OnHealthChanged() {
    Debug.Log("Health changed to: " + health);
}

// 自定义右键菜单
[CustomContextMenu("Reset to Default", "ResetValue")]
public int value;

private void ResetValue() {
    value = 0;
}

// 缩进
[Indent]
public int indentedValue;

[Indent(2)]
public int doubleIndented;

// 提示
[PropertyTooltip("This is a helpful tooltip")]
public string tooltipExample;
```

---

## Unity 标准 Attributes

Odin 也支持 Unity 的标准 Attributes:

| Attribute | 描述 |
|-----------|------|
| `[Multiline]` | 多行文本 |
| `[Range]` | 范围滑块 |
| `[Space]` | 间距 |
| `[TextArea]` | 文本区域 |

### 示例
```csharp
[Multiline(3)]
public string description;

[Range(0, 100)]
public int percentage;

[Space(20)]
public int spacedValue;

[TextArea(3, 10)]
public string longText;
```

---

## Debug Attributes

用于调试的 Attributes。

| Attribute | 描述 |
|-----------|------|
| `[ShowDrawerChain]` | 显示绘制链 |
| `[ShowPropertyResolver]` | 显示属性解析器 |

---

## 常用组合模式

### 1. 配置面板
```csharp
[TabGroup("Settings", "General")]
[BoxGroup("Settings/General/Player")]
[Required]
public GameObject playerPrefab;

[TabGroup("Settings", "Graphics")]
[Range(0, 10)]
public int qualityLevel;
```

### 2. 条件验证
```csharp
[Required]
[ShowIf("enableFeature")]
[ValidateInput("ValidateValue")]
public int featureValue;
```

### 3. 列表管理
```csharp
[TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
[ListDrawerSettings(
    CustomAddFunction = "AddItem",
    OnTitleBarGUI = "DrawRefreshButton"
)]
public List<ItemData> items;
```

### 4. 调试信息
```csharp
[ShowInInspector, ReadOnly]
[ProgressBar(0, 100)]
public float HealthPercentage => (float)health / maxHealth * 100;

[ShowInInspector, DisplayAsString]
public string Status => isAlive ? "Alive" : "Dead";
```

---

**文档来源**: https://odininspector.com/attributes
**最后更新**: 2026-01-14
**注**: 这是 Part 1,包含所有 Attributes。Part 2 将包含其他核心功能。
