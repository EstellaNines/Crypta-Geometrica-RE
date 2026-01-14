# UnityEditor 和 Tilemaps 完整 API 参考

## 目录
1. [UnityEditor 核心类](#unityeditor-核心类)
2. [自定义编辑器](#自定义编辑器)
3. [编辑器窗口](#编辑器窗口)
4. [资源管理](#资源管理)
5. [Tilemaps 完整 API](#tilemaps-完整-api)
6. [Tilemap Collider](#tilemap-collider)
7. [Rule Tiles](#rule-tiles)
8. [实用工具](#实用工具)

---

## UnityEditor 核心类

### Editor 自定义编辑器基类

```csharp
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MyScript))]
public class MyScriptEditor : Editor
{
    SerializedProperty healthProp;
    SerializedProperty speedProp;
    
    void OnEnable()
    {
        // 获取序列化属性
        healthProp = serializedObject.FindProperty("health");
        speedProp = serializedObject.FindProperty("speed");
    }
    
    public override void OnInspectorGUI()
    {
        // 更新序列化对象
        serializedObject.Update();
        
        // 绘制默认Inspector
        // DrawDefaultInspector();
        
        // 自定义绘制
        EditorGUILayout.LabelField("Player Stats", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(healthProp);
        EditorGUILayout.PropertyField(speedProp);
        
        // 自定义控件
        EditorGUILayout.Space();
        if (GUILayout.Button("Reset Values"))
        {
            healthProp.intValue = 100;
            speedProp.floatValue = 5f;
        }
        
        // 应用修改
        serializedObject.ApplyModifiedProperties();
    }
    
    // Scene视图绘制
    void OnSceneGUI()
    {
        MyScript script = (MyScript)target;
        
        // 绘制手柄
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(script.transform.position, Vector3.forward, 2f);
        
        // 位置手柄
        Vector3 newPos = Handles.PositionHandle(
            script.transform.position, 
            script.transform.rotation
        );
        
        if (newPos != script.transform.position)
        {
            Undo.RecordObject(script.transform, "Move Object");
            script.transform.position = newPos;
        }
    }
}
```

### EditorWindow 编辑器窗口

```csharp
using UnityEngine;
using UnityEditor;

public class MyEditorWindow : EditorWindow
{
    string myString = "Hello World";
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;
    
    [MenuItem("Window/My Window")]
    public static void ShowWindow()
    {
        GetWindow<MyEditorWindow>("My Window");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        myString = EditorGUILayout.TextField("Text Field", myString);
        
        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
        myBool = EditorGUILayout.Toggle("Toggle", myBool);
        myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();
        
        if (GUILayout.Button("Execute"))
        {
            Debug.Log($"Executing with: {myString}");
        }
    }
    
    void OnEnable()
    {
        // 窗口启用时
    }
    
    void OnDisable()
    {
        // 窗口禁用时
    }
    
    void OnDestroy()
    {
        // 窗口销毁时
    }
}
```

### EditorGUILayout 常用控件

```csharp
using UnityEditor;
using UnityEngine;

public class EditorGUIExample : EditorWindow
{
    void OnGUI()
    {
        // 标签
        EditorGUILayout.LabelField("Label");
        EditorGUILayout.LabelField("Label", "Value");
        
        // 文本框
        string text = EditorGUILayout.TextField("Text", "default");
        string textArea = EditorGUILayout.TextArea("Multi-line text");
        
        // 数值
        int intValue = EditorGUILayout.IntField("Int", 0);
        float floatValue = EditorGUILayout.FloatField("Float", 0f);
        
        // 滑块
        float slider = EditorGUILayout.Slider("Slider", 0.5f, 0f, 1f);
        int intSlider = EditorGUILayout.IntSlider("Int Slider", 5, 0, 10);
        
        // 范围滑块
        float minVal = 0f, maxVal = 10f;
        EditorGUILayout.MinMaxSlider("Range", ref minVal, ref maxVal, 0f, 100f);
        
        // 切换
        bool toggle = EditorGUILayout.Toggle("Toggle", false);
        bool toggleLeft = EditorGUILayout.ToggleLeft("Toggle Left", false);
        
        // 枚举
        MyEnum enumValue = (MyEnum)EditorGUILayout.EnumPopup("Enum", MyEnum.Option1);
        
        // 对象字段
        GameObject obj = EditorGUILayout.ObjectField(
            "GameObject", 
            null, 
            typeof(GameObject), 
            true
        ) as GameObject;
        
        // 颜色
        Color color = EditorGUILayout.ColorField("Color", Color.white);
        
        // 曲线
        AnimationCurve curve = EditorGUILayout.CurveField("Curve", AnimationCurve.Linear(0, 0, 1, 1));
        
        // 向量
        Vector2 vec2 = EditorGUILayout.Vector2Field("Vector2", Vector2.zero);
        Vector3 vec3 = EditorGUILayout.Vector3Field("Vector3", Vector3.zero);
        Vector4 vec4 = EditorGUILayout.Vector4Field("Vector4", Vector4.zero);
        
        // 按钮
        if (GUILayout.Button("Button"))
        {
            Debug.Log("Button clicked");
        }
        
        // 折叠组
        bool foldout = EditorGUILayout.Foldout(true, "Foldout");
        if (foldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Content");
            EditorGUI.indentLevel--;
        }
        
        // 水平/垂直布局
        EditorGUILayout.BeginHorizontal();
        GUILayout.Button("Left");
        GUILayout.Button("Right");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Top");
        GUILayout.Label("Bottom");
        EditorGUILayout.EndVertical();
        
        // 空间
        EditorGUILayout.Space();
        EditorGUILayout.Space(20);
        
        // 分隔线
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // 帮助框
        EditorGUILayout.HelpBox("This is a help message", MessageType.Info);
        EditorGUILayout.HelpBox("Warning!", MessageType.Warning);
        EditorGUILayout.HelpBox("Error!", MessageType.Error);
    }
}

enum MyEnum { Option1, Option2, Option3 }
```

### SerializedObject 和 SerializedProperty

```csharp
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MyScript))]
public class SerializedExample : Editor
{
    SerializedProperty nameProp;
    SerializedProperty healthProp;
    SerializedProperty itemsProp;
    
    void OnEnable()
    {
        nameProp = serializedObject.FindProperty("playerName");
        healthProp = serializedObject.FindProperty("health");
        itemsProp = serializedObject.FindProperty("items");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 直接绘制属性
        EditorGUILayout.PropertyField(nameProp);
        EditorGUILayout.PropertyField(healthProp);
        
        // 数组/列表
        EditorGUILayout.PropertyField(itemsProp, true); // true = 包含子元素
        
        // 手动处理数组
        EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);
        int arraySize = itemsProp.arraySize;
        arraySize = EditorGUILayout.IntField("Size", arraySize);
        
        if (arraySize != itemsProp.arraySize)
        {
            itemsProp.arraySize = arraySize;
        }
        
        for (int i = 0; i < itemsProp.arraySize; i++)
        {
            SerializedProperty element = itemsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(element);
        }
        
        // 获取/设置值
        string name = nameProp.stringValue;
        int health = healthProp.intValue;
        
        nameProp.stringValue = "New Name";
        healthProp.intValue = 100;
        
        serializedObject.ApplyModifiedProperties();
    }
}
```

### Selection 选择管理

```csharp
using UnityEditor;
using UnityEngine;

public class SelectionExample : EditorWindow
{
    [MenuItem("Tools/Selection Info")]
    static void ShowSelectionInfo()
    {
        // 当前选中的GameObject
        GameObject selected = Selection.activeGameObject;
        
        // 当前选中的Transform
        Transform selectedTransform = Selection.activeTransform;
        
        // 当前选中的Object
        Object selectedObject = Selection.activeObject;
        
        // 所有选中的对象
        Object[] selectedObjects = Selection.objects;
        GameObject[] selectedGameObjects = Selection.gameObjects;
        Transform[] selectedTransforms = Selection.transforms;
        
        // 选择对象
        Selection.activeGameObject = GameObject.Find("Player");
        Selection.objects = new Object[] { obj1, obj2, obj3 };
        
        // 选择变化事件
        Selection.selectionChanged += OnSelectionChanged;
    }
    
    static void OnSelectionChanged()
    {
        Debug.Log("Selection changed!");
    }
}
```

### AssetDatabase 资源数据库

```csharp
using UnityEditor;
using UnityEngine;

public class AssetDatabaseExample
{
    [MenuItem("Assets/Create My Asset")]
    static void CreateAsset()
    {
        // 创建ScriptableObject
        MyData asset = ScriptableObject.CreateInstance<MyData>();
        
        // 保存为资源
        AssetDatabase.CreateAsset(asset, "Assets/MyData.asset");
        AssetDatabase.SaveAssets();
        
        // 刷新
        AssetDatabase.Refresh();
        
        // 加载资源
        MyData loaded = AssetDatabase.LoadAssetAtPath<MyData>("Assets/MyData.asset");
        
        // 查找资源
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        
        // 获取资源路径
        string path = AssetDatabase.GetAssetPath(asset);
        
        // 删除资源
        AssetDatabase.DeleteAsset("Assets/MyData.asset");
        
        // 移动资源
        AssetDatabase.MoveAsset("Assets/Old.asset", "Assets/New.asset");
        
        // 复制资源
        AssetDatabase.CopyAsset("Assets/Original.asset", "Assets/Copy.asset");
        
        // 导入资源
        AssetDatabase.ImportAsset("Assets/NewTexture.png");
    }
}
```

### PrefabUtility 预制体工具

```csharp
using UnityEditor;
using UnityEngine;

public class PrefabExample
{
    [MenuItem("Tools/Prefab Operations")]
    static void PrefabOperations()
    {
        GameObject obj = Selection.activeGameObject;
        
        // 检查是否是预制体
        bool isPrefab = PrefabUtility.IsPartOfPrefabAsset(obj);
        bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(obj);
        
        // 获取预制体资源
        GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        
        // 创建预制体
        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(obj, "Assets/MyPrefab.prefab");
        
        // 实例化预制体
        GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
        
        // 应用修改到预制体
        PrefabUtility.ApplyPrefabInstance(obj, InteractionMode.UserAction);
        
        // 还原预制体实例
        PrefabUtility.RevertPrefabInstance(obj, InteractionMode.UserAction);
        
        // 解包预制体
        PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.UserAction);
    }
}
```

### Undo 撤销系统

```csharp
using UnityEditor;
using UnityEngine;

public class UndoExample : EditorWindow
{
    GameObject target;
    
    void OnGUI()
    {
        target = EditorGUILayout.ObjectField("Target", target, typeof(GameObject), true) as GameObject;
        
        if (GUILayout.Button("Move Object"))
        {
            // 记录对象状态
            Undo.RecordObject(target.transform, "Move Object");
            target.transform.position += Vector3.right;
        }
        
        if (GUILayout.Button("Create Object"))
        {
            GameObject newObj = new GameObject("New Object");
            // 注册创建操作
            Undo.RegisterCreatedObjectUndo(newObj, "Create Object");
        }
        
        if (GUILayout.Button("Destroy Object"))
        {
            // 注册销毁操作
            Undo.DestroyObjectImmediate(target);
        }
        
        if (GUILayout.Button("Change Parent"))
        {
            Transform newParent = GameObject.Find("Parent").transform;
            // 注册父级变化
            Undo.SetTransformParent(target.transform, newParent, "Change Parent");
        }
        
        // 组合多个操作
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        
        Undo.RecordObject(target.transform, "Multiple Changes");
        target.transform.position = Vector3.zero;
        target.transform.rotation = Quaternion.identity;
        
        Undo.CollapseUndoOperations(group);
    }
}
```

### Handles Scene视图手柄

```csharp
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MyScript))]
public class HandlesExample : Editor
{
    void OnSceneGUI()
    {
        MyScript script = (MyScript)target;
        
        // 位置手柄
        Vector3 newPos = Handles.PositionHandle(
            script.transform.position,
            script.transform.rotation
        );
        
        // 旋转手柄
        Quaternion newRot = Handles.RotationHandle(
            script.transform.rotation,
            script.transform.position
        );
        
        // 缩放手柄
        Vector3 newScale = Handles.ScaleHandle(
            script.transform.localScale,
            script.transform.position,
            script.transform.rotation,
            1f
        );
        
        // 自由移动手柄
        Vector3 freeMove = Handles.FreeMoveHandle(
            script.transform.position,
            Quaternion.identity,
            0.5f,
            Vector3.one * 0.1f,
            Handles.SphereHandleCap
        );
        
        // 绘制形状
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(script.transform.position, Vector3.forward, 2f);
        Handles.DrawWireCube(script.transform.position, Vector3.one);
        Handles.DrawLine(Vector3.zero, script.transform.position);
        
        // 绘制标签
        Handles.Label(script.transform.position, "My Object");
        
        // 应用修改
        if (GUI.changed)
        {
            Undo.RecordObject(script.transform, "Handle Change");
            script.transform.position = newPos;
            script.transform.rotation = newRot;
            script.transform.localScale = newScale;
        }
    }
}
```

### MenuItem 菜单项

```csharp
using UnityEditor;
using UnityEngine;

public class MenuItemExample
{
    // 基本菜单项
    [MenuItem("Tools/My Tool")]
    static void MyTool()
    {
        Debug.Log("Tool executed");
    }
    
    // 带快捷键
    [MenuItem("Tools/Quick Tool %q")] // Ctrl+Q (Mac: Cmd+Q)
    static void QuickTool()
    {
        Debug.Log("Quick tool");
    }
    
    // 快捷键说明:
    // % = Ctrl (Mac: Cmd)
    // # = Shift
    // & = Alt
    // _ = 无修饰键
    
    // 右键菜单
    [MenuItem("GameObject/My Custom Object", false, 10)]
    static void CreateCustomObject()
    {
        GameObject obj = new GameObject("Custom Object");
        Undo.RegisterCreatedObjectUndo(obj, "Create Custom Object");
    }
    
    // Assets菜单
    [MenuItem("Assets/Process Selected")]
    static void ProcessSelected()
    {
        foreach (Object obj in Selection.objects)
        {
            Debug.Log(obj.name);
        }
    }
    
    // 验证菜单项(灰色/可用)
    [MenuItem("Tools/Conditional Tool")]
    static void ConditionalTool()
    {
        Debug.Log("Conditional tool");
    }
    
    [MenuItem("Tools/Conditional Tool", true)]
    static bool ValidateConditionalTool()
    {
        return Selection.activeGameObject != null;
    }
    
    // 优先级(数字越小越靠前)
    [MenuItem("Tools/First", false, 1)]
    static void First() { }
    
    [MenuItem("Tools/Second", false, 2)]
    static void Second() { }
}
```

---

## Tilemaps 完整 API

### Tilemap 核心类

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapAPI : MonoBehaviour
{
    [SerializeField] Tilemap tilemap;
    [SerializeField] Tile tile;
    
    void Start()
    {
        // 设置瓦片
        Vector3Int position = new Vector3Int(0, 0, 0);
        tilemap.SetTile(position, tile);
        
        // 获取瓦片
        TileBase getTile = tilemap.GetTile(position);
        Tile specificTile = tilemap.GetTile<Tile>(position);
        
        // 检查是否有瓦片
        bool hasTile = tilemap.HasTile(position);
        
        // 删除瓦片
        tilemap.SetTile(position, null);
        
        // 批量设置
        Vector3Int[] positions = new Vector3Int[] {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(2, 0, 0)
        };
        TileBase[] tiles = new TileBase[] { tile, tile, tile };
        tilemap.SetTiles(positions, tiles);
        
        // 使用BoundsInt批量设置
        BoundsInt bounds = new BoundsInt(-10, -10, 0, 20, 20, 1);
        TileBase[] tileArray = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
        for (int i = 0; i < tileArray.Length; i++)
        {
            tileArray[i] = tile;
        }
        tilemap.SetTilesBlock(bounds, tileArray);
        
        // 清空
        tilemap.ClearAllTiles();
        
        // 刷新瓦片
        tilemap.RefreshTile(position);
        tilemap.RefreshAllTiles();
        
        // 坐标转换
        Vector3 worldPos = new Vector3(5f, 5f, 0f);
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        Vector3 worldPosFromCell = tilemap.CellToWorld(cellPos);
        Vector3 centerPos = tilemap.GetCellCenterWorld(cellPos);
        
        // 获取边界
        BoundsInt cellBounds = tilemap.cellBounds;
        Bounds localBounds = tilemap.localBounds;
        
        // 遍历所有瓦片
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                TileBase t = tilemap.GetTile(pos);
                // 处理瓦片
            }
        }
        
        // 获取使用的瓦片
        TileBase[] usedTiles = tilemap.GetTilesBlock(tilemap.cellBounds);
        
        // 压缩边界
        tilemap.CompressBounds();
        
        // 调整大小
        tilemap.ResizeBounds();
    }
    
    // 获取瓦片Sprite
    Sprite GetTileSprite(Vector3Int position)
    {
        return tilemap.GetSprite(position);
    }
    
    // 获取瓦片颜色
    Color GetTileColor(Vector3Int position)
    {
        return tilemap.GetColor(position);
    }
    
    // 设置瓦片颜色
    void SetTileColor(Vector3Int position, Color color)
    {
        tilemap.SetColor(position, color);
    }
    
    // 获取瓦片变换矩阵
    Matrix4x4 GetTileTransform(Vector3Int position)
    {
        return tilemap.GetTransformMatrix(position);
    }
    
    // 设置瓦片变换
    void SetTileTransform(Vector3Int position, Matrix4x4 transform)
    {
        tilemap.SetTransformMatrix(position, transform);
    }
}
```

### Tile 瓦片资源

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

// 创建自定义Tile
[CreateAssetMenu(fileName = "New Custom Tile", menuName = "Tiles/Custom Tile")]
public class CustomTile : Tile
{
    public int damage;
    public bool isWalkable = true;
    
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);
        
        // 自定义瓦片数据
        tileData.sprite = sprite;
        tileData.color = color;
        tileData.transform = transform;
        tileData.gameObject = gameObject;
        tileData.flags = flags;
        tileData.colliderType = colliderType;
    }
    
    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        // 瓦片放置时调用
        return base.StartUp(position, tilemap, go);
    }
}
```

### Animated Tile 动画瓦片

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Animated Tile", menuName = "Tiles/Animated Tile")]
public class AnimatedTile : TileBase
{
    public Sprite[] animationSprites;
    public float animationSpeed = 1f;
    public Tile.ColliderType colliderType = Tile.ColliderType.Sprite;
    
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = animationSprites[0];
        tileData.colliderType = colliderType;
    }
    
    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (animationSprites != null && animationSprites.Length > 0)
        {
            tileAnimationData.animatedSprites = animationSprites;
            tileAnimationData.animationSpeed = animationSpeed;
            return true;
        }
        return false;
    }
}
```

### TilemapCollider2D

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapColliderExample : MonoBehaviour
{
    void Start()
    {
        // 添加碰撞器
        TilemapCollider2D collider = gameObject.AddComponent<TilemapCollider2D>();
        
        // 设置为触发器
        collider.isTrigger = false;
        
        // 使用复合碰撞器(优化性能)
        CompositeCollider2D composite = gameObject.AddComponent<CompositeCollider2D>();
        collider.usedByComposite = true;
        
        // Rigidbody2D设置
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }
}
```

### Grid 网格系统

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridExample : MonoBehaviour
{
    Grid grid;
    
    void Start()
    {
        grid = GetComponent<Grid>();
        
        // 网格类型
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        // GridLayout.CellLayout.Hexagon
        // GridLayout.CellLayout.Isometric
        
        // 单元格大小
        grid.cellSize = new Vector3(1f, 1f, 0f);
        
        // 单元格间隙
        grid.cellGap = Vector3.zero;
        
        // 坐标转换
        Vector3 worldPos = new Vector3(5f, 5f, 0f);
        Vector3Int cellPos = grid.WorldToCell(worldPos);
        Vector3 cellWorldPos = grid.CellToWorld(cellPos);
        Vector3 centerPos = grid.GetCellCenterWorld(cellPos);
        
        // 本地坐标转换
        Vector3 localPos = grid.WorldToLocal(worldPos);
        Vector3 worldFromLocal = grid.LocalToWorld(localPos);
    }
}
```

---

## 实用工具

### Gizmos 可视化调试

```csharp
using UnityEngine;

public class GizmosExample : MonoBehaviour
{
    [SerializeField] float radius = 2f;
    [SerializeField] Vector3 targetPosition;
    
    void OnDrawGizmos()
    {
        // 设置颜色
        Gizmos.color = Color.yellow;
        
        // 绘制线
        Gizmos.DrawLine(transform.position, targetPosition);
        
        // 绘制射线
        Gizmos.DrawRay(transform.position, transform.right * 5f);
        
        // 绘制球体
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawSphere(transform.position, radius * 0.5f);
        
        // 绘制立方体
        Gizmos.DrawWireCube(transform.position, Vector3.one);
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
        
        // 绘制图标
        Gizmos.DrawIcon(transform.position, "MyIcon.png");
        
        // 绘制网格
        Gizmos.DrawGUITexture(new Rect(0, 0, 100, 100), myTexture);
    }
    
    void OnDrawGizmosSelected()
    {
        // 仅在选中时绘制
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius * 1.5f);
    }
}
```

### Debug 调试工具

```csharp
using UnityEngine;

public class DebugExample : MonoBehaviour
{
    void Update()
    {
        // 日志
        Debug.Log("Normal message");
        Debug.LogWarning("Warning message");
        Debug.LogError("Error message");
        
        // 条件日志
        Debug.Assert(health > 0, "Health must be positive!");
        
        // 绘制调试线
        Debug.DrawLine(transform.position, targetPosition, Color.red, 1f);
        Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue, 1f);
        
        // 暂停编辑器
        if (criticalError)
        {
            Debug.Break();
        }
    }
}
```

---

**文档来源**: Unity 2022.3 官方文档
**最后更新**: 2026-01-14
