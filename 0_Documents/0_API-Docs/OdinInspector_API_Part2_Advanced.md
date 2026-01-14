# Odin Inspector API 参考文档 - Part 2: 高级功能

## 目录
1. [Odin Serializer (序列化系统)](#odin-serializer)
2. [Editor Windows (编辑器窗口)](#editor-windows)
3. [OdinMenuTree (菜单树)](#odinmenutree)
4. [Validators (验证器)](#validators)
5. [Custom Drawers (自定义绘制器)](#custom-drawers)
6. [Property Resolvers (属性解析器)](#property-resolvers)
7. [实用工具类](#实用工具类)

---

## Odin Serializer (序列化系统)

Odin Serializer 是一个强大、灵活、可扩展的开源序列化系统。

### 核心特性

- 序列化几乎任何类型(泛型、接口、抽象类、多态等)
- 支持循环引用
- 支持外部引用
- 高性能
- 可扩展

### 快速开始

#### 1. 启用 Odin Serializer

```csharp
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class MyComponent : SerializedMonoBehaviour
{
    // 现在可以序列化任何类型
    public Dictionary<string, List<int>> myDictionary;
    public IMyInterface myInterface;
    public System.Type myType;
}
```

#### 2. 使用 OdinSerialize Attribute

```csharp
using UnityEngine;
using Sirenix.OdinInspector;

public class MyComponent : MonoBehaviour
{
    [OdinSerialize]
    private Dictionary<string, GameObject> privateDictionary;
    
    [OdinSerialize]
    public IWeapon weapon;
}
```

### SerializedMonoBehaviour

替代 `MonoBehaviour`,启用 Odin 序列化。

```csharp
using Sirenix.OdinInspector;

public class Player : SerializedMonoBehaviour
{
    // 支持字典
    public Dictionary<string, int> stats = new Dictionary<string, int>();
    
    // 支持接口
    public IInventory inventory;
    
    // 支持多维数组
    public int[,] grid;
    
    // 支持元组
    public (string name, int value) data;
}
```

### SerializedScriptableObject

替代 `ScriptableObject`,启用 Odin 序列化。

```csharp
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu]
public class GameConfig : SerializedScriptableObject
{
    public Dictionary<string, ItemData> items;
    public List<IAbility> abilities;
}
```

### 序列化格式

Odin 支持多种序列化格式:

| 格式 | 描述 | 用途 |
|------|------|------|
| `Binary` | 二进制格式 | 最快,最小 |
| `JSON` | JSON格式 | 可读,调试友好 |
| `Nodes` | 节点格式 | Unity内部使用 |

```csharp
// 设置序列化格式
[SerializeField, HideInInspector]
private SerializationData serializationData;

// 在 Inspector 中设置
// Tools > Odin Inspector > Preferences > Editor Types
```

### ShowInInspector

显示非序列化成员(属性、方法返回值等)。

```csharp
public class Character : MonoBehaviour
{
    private int health = 100;
    
    // 显示私有字段
    [ShowInInspector]
    private int Health
    {
        get => health;
        set => health = Mathf.Clamp(value, 0, maxHealth);
    }
    
    // 显示只读属性
    [ShowInInspector]
    public int MaxHealth => 100;
    
    // 显示计算属性
    [ShowInInspector]
    public float HealthPercentage => (float)health / maxHealth;
    
    // 显示方法返回值
    [ShowInInspector]
    public string Status => GetStatus();
    
    private string GetStatus()
    {
        return health > 50 ? "Healthy" : "Injured";
    }
}
```

### 序列化限制和注意事项

**支持的类型**:
- 所有基本类型
- Unity 类型 (Vector3, Color, GameObject 等)
- 泛型 (List<T>, Dictionary<K,V> 等)
- 接口和抽象类
- 多态类型
- 循环引用

**不支持的类型**:
- 委托 (Delegate)
- 事件 (Event)
- 某些特殊 Unity 类型

---

## Editor Windows (编辑器窗口)

Odin 提供强大的编辑器窗口创建工具。

### OdinEditorWindow

创建自定义编辑器窗口的基类。

```csharp
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class MyEditorWindow : OdinEditorWindow
{
    [MenuItem("Tools/My Window")]
    private static void OpenWindow()
    {
        GetWindow<MyEditorWindow>().Show();
    }
    
    [Title("Settings")]
    public string playerName;
    
    [Range(0, 100)]
    public int level;
    
    [Button(ButtonSizes.Large)]
    public void SaveSettings()
    {
        Debug.Log("Settings saved!");
    }
}
```

### OdinMenuEditorWindow

带菜单树的编辑器窗口。

```csharp
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class GameDataWindow : OdinMenuEditorWindow
{
    [MenuItem("Tools/Game Data")]
    private static void OpenWindow()
    {
        GetWindow<GameDataWindow>().Show();
    }
    
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        
        // 添加所有 ScriptableObject
        tree.AddAllAssetsAtPath("Items", "Assets/Data/Items", typeof(ItemData));
        tree.AddAllAssetsAtPath("Enemies", "Assets/Data/Enemies", typeof(EnemyData));
        
        // 添加自定义对象
        tree.Add("Settings", new GameSettings());
        
        // 添加工具
        tree.Add("Tools/Create Item", new ItemCreator());
        
        return tree;
    }
}
```

### OdinMenuTree

菜单树系统,用于组织编辑器内容。

```csharp
protected override OdinMenuTree BuildMenuTree()
{
    var tree = new OdinMenuTree(supportsMultiSelect: true)
    {
        { "Home", this, EditorIcons.House },
        { "Settings", mySettings, EditorIcons.SettingsCog }
    };
    
    // 添加资源
    tree.AddAllAssetsAtPath("Items", "Assets/Items", typeof(ItemData), true);
    
    // 添加对象
    tree.Add("Player/Stats", playerStats);
    tree.Add("Player/Inventory", inventory);
    
    // 自定义图标
    tree.MenuItems.AddIcons<ItemData>(x => x.icon);
    
    // 排序
    tree.EnumerateTree().SortMenuItemsByName();
    
    return tree;
}
```

### 窗口配置

```csharp
public class ConfigWindow : OdinMenuEditorWindow
{
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // 窗口大小
        minSize = new Vector2(800, 600);
        
        // 窗口标题
        titleContent = new GUIContent("Game Config", EditorIcons.SettingsCog.Active);
    }
    
    protected override void OnBeginDrawEditors()
    {
        // 在编辑器绘制前
        var selected = MenuTree.Selection.FirstOrDefault();
        var toolbarHeight = MenuTree.Config.SearchToolbarHeight;
        
        // 绘制工具栏
        SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);
        {
            if (selected != null)
            {
                GUILayout.Label(selected.Name);
            }
            
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus))
            {
                // 添加新项
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }
}
```

---

## OdinMenuTree (菜单树)

### 创建菜单树

```csharp
var tree = new OdinMenuTree();

// 基本添加
tree.Add("Path/To/Item", myObject);

// 带图标
tree.Add("Path/To/Item", myObject, EditorIcons.Star);

// 添加所有资源
tree.AddAllAssetsAtPath("Items", "Assets/Items", typeof(ItemData));

// 添加对象的所有成员
tree.AddObjectAtPath("Player", player);
```

### 菜单配置

```csharp
var tree = new OdinMenuTree()
{
    Config = 
    {
        DrawSearchToolbar = true,
        AutoHandleKeyboardNavigation = true,
        UseCachedExpandedStates = true,
        DefaultMenuStyle = OdinMenuStyle.TreeViewStyle
    }
};
```

### 自定义菜单项

```csharp
tree.MenuItems.AddIcons<ItemData>(x => x.icon);
tree.MenuItems.AddThumbnailIcons();

// 自定义绘制
tree.DrawMenuSearchBar = () =>
{
    // 自定义搜索栏
};

tree.DrawInMenuTree = () =>
{
    // 自定义菜单树绘制
};
```

---

## Validators (验证器)

### 内置验证器

Odin 提供多个内置验证器:

```csharp
using Sirenix.OdinInspector;

public class ValidationExample : MonoBehaviour
{
    // 必填验证
    [Required]
    public GameObject player;
    
    // 资源验证
    [AssetsOnly]
    public GameObject prefab;
    
    // 场景对象验证
    [SceneObjectsOnly]
    public Transform target;
    
    // 范围验证
    [MinValue(0), MaxValue(100)]
    public int health;
    
    // 列表长度验证
    [RequiredListLength(1, 10)]
    public List<string> tags;
    
    // 自定义验证
    [ValidateInput("ValidateHealth", "Health must be positive!")]
    public int currentHealth;
    
    private bool ValidateHealth(int value)
    {
        return value >= 0;
    }
}
```

### 自定义验证器

创建自定义验证器:

```csharp
using Sirenix.OdinInspector.Editor.Validation;
using System.Collections.Generic;

[assembly: RegisterValidator(typeof(MyCustomValidator))]

public class MyCustomValidator : AttributeValidator<MyValidateAttribute>
{
    protected override void Validate(ValidationResult result)
    {
        var value = ValueEntry.SmartValue;
        
        if (!IsValid(value))
        {
            result.ResultType = ValidationResultType.Error;
            result.Message = "Validation failed!";
        }
    }
    
    private bool IsValid(object value)
    {
        // 自定义验证逻辑
        return true;
    }
}
```

### 验证扫描

```csharp
// 在编辑器中
// Tools > Odin Inspector > Validator > Scan All

// 代码中触发
using Sirenix.OdinInspector.Editor.Validation;

ValidationRunner.RunValidation(target);
```

---

## Custom Drawers (自定义绘制器)

### OdinAttributeDrawer

创建基于 Attribute 的自定义绘制器。

```csharp
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

// 自定义 Attribute
public class MyCustomAttribute : Attribute { }

// 自定义绘制器
public class MyCustomDrawer : OdinAttributeDrawer<MyCustomAttribute>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        // 绘制前
        EditorGUILayout.BeginVertical("box");
        
        // 调用默认绘制
        CallNextDrawer(label);
        
        // 绘制后
        if (GUILayout.Button("Custom Button"))
        {
            Debug.Log("Button clicked!");
        }
        
        EditorGUILayout.EndVertical();
    }
}
```

### OdinValueDrawer

创建基于类型的自定义绘制器。

```csharp
public class Vector3Drawer : OdinValueDrawer<Vector3>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var value = ValueEntry.SmartValue;
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        
        value.x = EditorGUILayout.FloatField(value.x);
        value.y = EditorGUILayout.FloatField(value.y);
        value.z = EditorGUILayout.FloatField(value.z);
        
        EditorGUILayout.EndHorizontal();
        
        ValueEntry.SmartValue = value;
    }
}
```

### 绘制器优先级

```csharp
[DrawerPriority(0, 0, 1)] // DrawerPriorityLevel, SuperPriority, Priority
public class MyDrawer : OdinAttributeDrawer<MyAttribute>
{
    // ...
}
```

---

## Property Resolvers (属性解析器)

属性解析器用于解析和处理属性。

### 自定义属性解析器

```csharp
using Sirenix.OdinInspector.Editor;

public class MyPropertyResolver<T> : OdinPropertyResolver<T>
{
    protected override InspectorPropertyInfo[] GetPropertyInfos()
    {
        // 返回要显示的属性信息
        return new InspectorPropertyInfo[]
        {
            InspectorPropertyInfo.CreateValue(
                "MyProperty",
                0,
                SerializationBackend.None,
                new GetterSetter<T, int>(
                    (ref T owner) => GetValue(owner),
                    (ref T owner, int value) => SetValue(owner, value)
                )
            )
        };
    }
    
    private int GetValue(T owner)
    {
        // 获取值逻辑
        return 0;
    }
    
    private void SetValue(T owner, int value)
    {
        // 设置值逻辑
    }
}
```

---

## 实用工具类

### SirenixEditorGUI

Odin 提供的编辑器 GUI 工具类。

```csharp
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class MyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 标题
        SirenixEditorGUI.Title("My Title", "Subtitle", TextAlignment.Center, true);
        
        // 信息框
        SirenixEditorGUI.InfoMessageBox("This is an info message");
        SirenixEditorGUI.WarningMessageBox("This is a warning");
        SirenixEditorGUI.ErrorMessageBox("This is an error");
        
        // 水平线
        SirenixEditorGUI.HorizontalLineSeparator();
        
        // 工具栏
        SirenixEditorGUI.BeginHorizontalToolbar();
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Plus))
            {
                // 添加
            }
            
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Minus))
            {
                // 删除
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
        
        // 折叠组
        SirenixEditorGUI.BeginBox("My Box");
        {
            EditorGUILayout.LabelField("Content");
        }
        SirenixEditorGUI.EndBox();
    }
}
```

### EditorIcons

Odin 提供的图标库。

```csharp
using Sirenix.Utilities.Editor;

// 常用图标
EditorIcons.Plus
EditorIcons.Minus
EditorIcons.Refresh
EditorIcons.SettingsCog
EditorIcons.Star
EditorIcons.House
EditorIcons.File
EditorIcons.Folder
EditorIcons.Eye
EditorIcons.EyeDropper
EditorIcons.Pen
EditorIcons.Trash
EditorIcons.ArrowUp
EditorIcons.ArrowDown
EditorIcons.ArrowLeft
EditorIcons.ArrowRight

// 使用图标
GUILayout.Button(EditorIcons.Plus.Active);
```

### GUIHelper

GUI 辅助工具。

```csharp
using Sirenix.Utilities.Editor;

// 推入GUI颜色
GUIHelper.PushColor(Color.red);
// ... 绘制内容
GUIHelper.PopColor();

// 推入GUI启用状态
GUIHelper.PushGUIEnabled(false);
// ... 绘制禁用内容
GUIHelper.PopGUIEnabled();

// 获取控件矩形
var rect = GUIHelper.GetCurrentLayoutRect();

// 请求重绘
GUIHelper.RequestRepaint();
```

---

## 高级示例

### 完整的编辑器窗口示例

```csharp
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameDatabaseWindow : OdinMenuEditorWindow
{
    [MenuItem("Tools/Game Database")]
    private static void OpenWindow()
    {
        var window = GetWindow<GameDatabaseWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
    }
    
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree(supportsMultiSelect: true)
        {
            { "Home", this, EditorIcons.House },
            { "Create New", new CreateNewData(), EditorIcons.Plus }
        };
        
        tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
        tree.Config.DrawSearchToolbar = true;
        
        // 添加所有数据
        tree.AddAllAssetsAtPath("Items", "Assets/Data/Items", typeof(ItemData), true, true);
        tree.AddAllAssetsAtPath("Enemies", "Assets/Data/Enemies", typeof(EnemyData), true, true);
        
        // 添加图标
        tree.MenuItems.AddIcons<ItemData>(x => x.icon);
        
        return tree;
    }
    
    protected override void OnBeginDrawEditors()
    {
        var selected = MenuTree.Selection.FirstOrDefault();
        var toolbarHeight = MenuTree.Config.SearchToolbarHeight;
        
        SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);
        {
            if (selected != null)
            {
                GUILayout.Label(selected.Name);
            }
            
            GUILayout.FlexibleSpace();
            
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                MenuTree.UpdateMenuTree();
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }
    
    [Title("Home")]
    [ShowInInspector, ReadOnly]
    private string Info => "Welcome to Game Database!";
    
    [Button(ButtonSizes.Large)]
    private void RefreshDatabase()
    {
        MenuTree.UpdateMenuTree();
    }
}

public class CreateNewData
{
    [Title("Create New Data")]
    
    [ValueDropdown("GetDataTypes")]
    public string dataType;
    
    public string dataName;
    
    [Button(ButtonSizes.Large)]
    public void Create()
    {
        // 创建新数据逻辑
        Debug.Log($"Creating {dataType}: {dataName}");
    }
    
    private IEnumerable<string> GetDataTypes()
    {
        return new[] { "Item", "Enemy", "Weapon" };
    }
}
```

### 自定义序列化示例

```csharp
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SerializedMonoBehaviour
{
    [Title("Game Data")]
    
    // 序列化字典
    [DictionaryDrawerSettings(KeyLabel = "ID", ValueLabel = "Player Data")]
    public Dictionary<string, PlayerData> players = new Dictionary<string, PlayerData>();
    
    // 序列化接口
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<IGameSystem> systems = new List<IGameSystem>();
    
    // 序列化多态
    [ShowInInspector]
    public IWeapon currentWeapon;
    
    [Button]
    public void SaveGame()
    {
        // 保存逻辑
    }
    
    [Button]
    public void LoadGame()
    {
        // 加载逻辑
    }
}

[System.Serializable]
public class PlayerData
{
    public string name;
    public int level;
    public Dictionary<string, int> stats;
}

public interface IGameSystem
{
    void Initialize();
    void Update();
}

public interface IWeapon
{
    void Attack();
}
```

---

## 最佳实践

### 1. 性能优化

```csharp
// 使用 HideInInspector 隐藏不需要的字段
[SerializeField, HideInInspector]
private SerializationData serializationData;

// 使用 DelayedProperty 减少更新频率
[DelayedProperty]
public string heavyComputationField;

// 大型列表使用分页
[ListDrawerSettings(NumberOfItemsPerPage = 10, ShowPaging = true)]
public List<ItemData> items;
```

### 2. 组织结构

```csharp
[TabGroup("Settings", "General")]
[BoxGroup("Settings/General/Player")]
public string playerName;

[TabGroup("Settings", "Graphics")]
[FoldoutGroup("Settings/Graphics/Quality")]
public int qualityLevel;
```

### 3. 验证和调试

```csharp
[Required]
[ValidateInput("ValidateValue")]
[InfoBox("@GetInfoMessage()")]
public int value;

private bool ValidateValue(int val) => val > 0;
private string GetInfoMessage() => $"Current value: {value}";
```

---

**文档来源**: https://odininspector.com/
**最后更新**: 2026-01-14
**注**: 这是 Part 2,包含高级功能。结合 Part 1 使用可获得完整的 Odin Inspector 参考。
