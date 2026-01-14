# Unity 2022.3 官方 API 参考文档 - 概览

## 目录
1. [UnityEngine 核心命名空间](#unityengine-核心)
2. [UnityEditor 编辑器命名空间](#unityeditor-编辑器)
3. [常用类快速索引](#常用类快速索引)

---

## 文档说明

Unity 官方 API 文档非常庞大,包含数千个类和方法。本文档将分为多个部分:

- **Part 1 (本文档)**: 概览和常用类索引
- **Part 2**: UnityEngine 核心类详解
- **Part 3**: UnityEditor 编辑器类详解
- **Part 4**: 特定功能模块详解

---

## UnityEngine 核心

### 核心命名空间结构

```
UnityEngine
├── Core (核心类)
│   ├── GameObject
│   ├── Component
│   ├── Transform
│   ├── MonoBehaviour
│   └── ScriptableObject
│
├── Physics (物理)
│   ├── Rigidbody
│   ├── Collider
│   └── Physics
│
├── Rendering (渲染)
│   ├── Camera
│   ├── Light
│   ├── Renderer
│   └── Material
│
├── UI (用户界面)
│   ├── Canvas
│   ├── RectTransform
│   └── UI Components
│
├── Animation (动画)
│   ├── Animator
│   ├── Animation
│   └── AnimationClip
│
└── Input (输入)
    ├── Input
    └── InputSystem
```

---

## 常用类快速索引

### 核心类 (Core Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `GameObject` | 场景中的基本对象 | `Find()`, `Instantiate()`, `Destroy()`, `SetActive()` |
| `Transform` | 位置、旋转、缩放 | `position`, `rotation`, `localScale`, `parent`, `GetChild()` |
| `Component` | 所有组件的基类 | `gameObject`, `transform`, `GetComponent<T>()` |
| `MonoBehaviour` | 脚本组件基类 | `Start()`, `Update()`, `Awake()`, `OnEnable()` |
| `Object` | Unity 对象基类 | `Instantiate()`, `Destroy()`, `DontDestroyOnLoad()` |
| `ScriptableObject` | 数据容器 | `CreateInstance<T>()` |
| `Time` | 时间相关 | `deltaTime`, `time`, `timeScale`, `fixedDeltaTime` |
| `Debug` | 调试工具 | `Log()`, `LogWarning()`, `LogError()`, `DrawLine()` |
| `Application` | 应用程序信息 | `isPlaying`, `dataPath`, `Quit()`, `OpenURL()` |
| `Resources` | 资源加载 | `Load<T>()`, `LoadAll<T>()`, `UnloadAsset()` |

### 物理类 (Physics Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `Rigidbody` | 刚体组件 | `velocity`, `AddForce()`, `AddTorque()`, `MovePosition()` |
| `Rigidbody2D` | 2D刚体 | `velocity`, `AddForce()`, `gravityScale` |
| `Collider` | 碰撞器基类 | `bounds`, `isTrigger`, `enabled` |
| `Collider2D` | 2D碰撞器 | `bounds`, `isTrigger`, `offset` |
| `Physics` | 物理系统 | `Raycast()`, `OverlapSphere()`, `gravity` |
| `Physics2D` | 2D物理系统 | `Raycast()`, `OverlapCircle()`, `gravity` |
| `RaycastHit` | 射线检测结果 | `point`, `normal`, `distance`, `collider` |
| `Collision` | 碰撞信息 | `contacts`, `relativeVelocity`, `gameObject` |

### 渲染类 (Rendering Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `Camera` | 摄像机 | `main`, `fieldOfView`, `orthographic`, `Render()` |
| `Light` | 光源 | `type`, `color`, `intensity`, `range` |
| `Renderer` | 渲染器基类 | `material`, `materials`, `bounds`, `enabled` |
| `MeshRenderer` | 网格渲染器 | `material`, `sharedMaterial` |
| `SpriteRenderer` | 精灵渲染器 | `sprite`, `color`, `flipX`, `flipY` |
| `Material` | 材质 | `color`, `SetColor()`, `SetFloat()`, `SetTexture()` |
| `Shader` | 着色器 | `Find()`, `PropertyToID()` |
| `Texture` | 纹理基类 | `width`, `height`, `filterMode` |
| `Texture2D` | 2D纹理 | `GetPixel()`, `SetPixel()`, `Apply()` |
| `RenderTexture` | 渲染纹理 | `Create()`, `Release()` |

### UI 类 (UI Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `Canvas` | 画布 | `renderMode`, `worldCamera`, `sortingOrder` |
| `CanvasGroup` | 画布组 | `alpha`, `interactable`, `blocksRaycasts` |
| `RectTransform` | UI矩形变换 | `anchoredPosition`, `sizeDelta`, `pivot`, `anchorMin` |
| `Image` | 图片 | `sprite`, `color`, `fillAmount`, `type` |
| `Text` | 文本 | `text`, `font`, `fontSize`, `color` |
| `Button` | 按钮 | `onClick`, `interactable` |
| `Slider` | 滑块 | `value`, `minValue`, `maxValue`, `onValueChanged` |
| `Toggle` | 切换按钮 | `isOn`, `onValueChanged` |
| `InputField` | 输入框 | `text`, `onValueChanged`, `onEndEdit` |
| `ScrollRect` | 滚动视图 | `content`, `horizontal`, `vertical`, `velocity` |

### 动画类 (Animation Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `Animator` | 动画控制器 | `SetTrigger()`, `SetBool()`, `SetFloat()`, `Play()` |
| `Animation` | 动画组件 | `Play()`, `Stop()`, `clip` |
| `AnimationClip` | 动画片段 | `length`, `frameRate`, `legacy` |
| `AnimatorController` | 动画控制器资源 | 编辑器中使用 |

### 音频类 (Audio Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `AudioSource` | 音频源 | `Play()`, `Stop()`, `Pause()`, `clip`, `volume` |
| `AudioClip` | 音频片段 | `length`, `samples`, `frequency` |
| `AudioListener` | 音频监听器 | `volume`, `pause` |
| `AudioMixer` | 音频混合器 | `SetFloat()`, `GetFloat()` |

### 输入类 (Input Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `Input` | 输入系统(旧) | `GetKey()`, `GetButton()`, `GetAxis()`, `mousePosition` |
| `KeyCode` | 键盘代码枚举 | `Space`, `W`, `A`, `S`, `D`, `Escape` |
| `Touch` | 触摸信息 | `position`, `deltaPosition`, `phase`, `fingerId` |

### 数学类 (Math Classes)

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `Vector2` | 2D向量 | `x`, `y`, `magnitude`, `normalized`, `Distance()` |
| `Vector3` | 3D向量 | `x`, `y`, `z`, `forward`, `up`, `right`, `Dot()`, `Cross()` |
| `Vector4` | 4D向量 | `x`, `y`, `z`, `w` |
| `Quaternion` | 四元数(旋转) | `identity`, `Euler()`, `Slerp()`, `LookRotation()` |
| `Matrix4x4` | 4x4矩阵 | `identity`, `TRS()`, `MultiplyPoint()` |
| `Mathf` | 数学函数 | `Clamp()`, `Lerp()`, `Sin()`, `Cos()`, `Sqrt()`, `PI` |
| `Random` | 随机数 | `Range()`, `value`, `insideUnitSphere` |
| `Color` | 颜色 | `r`, `g`, `b`, `a`, `red`, `green`, `blue`, `Lerp()` |
| `Rect` | 矩形 | `x`, `y`, `width`, `height`, `Contains()` |
| `Bounds` | 边界框 | `center`, `size`, `min`, `max`, `Contains()` |

---

## UnityEditor 编辑器

### 编辑器核心类

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `Editor` | 自定义编辑器基类 | `OnInspectorGUI()`, `target`, `serializedObject` |
| `EditorWindow` | 编辑器窗口基类 | `GetWindow<T>()`, `OnGUI()`, `Show()` |
| `EditorGUILayout` | 编辑器GUI布局 | `TextField()`, `IntField()`, `Button()`, `Foldout()` |
| `EditorGUI` | 编辑器GUI | `BeginChangeCheck()`, `EndChangeCheck()`, `PropertyField()` |
| `SerializedObject` | 序列化对象 | `FindProperty()`, `ApplyModifiedProperties()`, `Update()` |
| `SerializedProperty` | 序列化属性 | `stringValue`, `intValue`, `objectReferenceValue` |
| `Selection` | 选择管理 | `activeGameObject`, `objects`, `activeTransform` |
| `AssetDatabase` | 资源数据库 | `CreateAsset()`, `SaveAssets()`, `Refresh()`, `LoadAssetAtPath()` |
| `PrefabUtility` | 预制体工具 | `InstantiatePrefab()`, `SaveAsPrefabAsset()`, `UnpackPrefabInstance()` |
| `Undo` | 撤销系统 | `RecordObject()`, `RegisterCreatedObjectUndo()` |

### 编辑器特性 (Attributes)

| Attribute | 描述 | 用途 |
|-----------|------|------|
| `[MenuItem]` | 菜单项 | 创建菜单命令 |
| `[CustomEditor]` | 自定义编辑器 | 为类型创建自定义Inspector |
| `[CanEditMultipleObjects]` | 多对象编辑 | 允许同时编辑多个对象 |
| `[InitializeOnLoad]` | 加载时初始化 | 编辑器启动时执行 |
| `[ExecuteInEditMode]` | 编辑模式执行 | 在编辑模式下执行脚本 |
| `[ExecuteAlways]` | 总是执行 | 编辑和运行时都执行 |

---

## 特殊命名空间

### UnityEngine.SceneManagement

| 类名 | 描述 | 常用方法 |
|------|------|----------|
| `SceneManager` | 场景管理 | `LoadScene()`, `LoadSceneAsync()`, `GetActiveScene()` |
| `Scene` | 场景 | `name`, `buildIndex`, `isLoaded`, `GetRootGameObjects()` |

### UnityEngine.Events

| 类名 | 描述 | 用途 |
|------|------|------|
| `UnityEvent` | Unity事件 | 可序列化的事件系统 |
| `UnityAction` | Unity动作 | 事件回调委托 |

### UnityEngine.Networking (已弃用)

| 类名 | 描述 | 状态 |
|------|------|------|
| `NetworkManager` | 网络管理器 | 已弃用,使用Netcode替代 |

### UnityEngine.AI

| 类名 | 描述 | 常用方法/属性 |
|------|------|---------------|
| `NavMeshAgent` | 导航代理 | `SetDestination()`, `velocity`, `remainingDistance` |
| `NavMesh` | 导航网格 | `SamplePosition()`, `CalculatePath()` |

### UnityEngine.Tilemaps

| 类名 | 描述 | 常用方法 |
|------|------|----------|
| `Tilemap` | 瓦片地图 | `SetTile()`, `GetTile()`, `ClearAllTiles()` |
| `Tile` | 瓦片 | `sprite`, `color` |

---

## MonoBehaviour 生命周期

### 执行顺序

```
初始化阶段:
1. Awake()          - 脚本实例被加载时调用
2. OnEnable()       - 对象启用时调用
3. Start()          - 第一次Update前调用

物理更新阶段:
4. FixedUpdate()    - 固定时间间隔调用(物理更新)

输入事件:
5. OnMouseXXX()     - 鼠标事件

游戏逻辑阶段:
6. Update()         - 每帧调用
7. LateUpdate()     - Update后调用

渲染阶段:
8. OnPreRender()    - 渲染前
9. OnRenderObject() - 渲染时
10. OnPostRender()  - 渲染后

GUI阶段:
11. OnGUI()         - GUI事件

销毁阶段:
12. OnDisable()     - 对象禁用时
13. OnDestroy()     - 对象销毁时
```

### 常用生命周期方法

```csharp
// 初始化
void Awake() { }           // 最早调用,用于初始化
void OnEnable() { }        // 启用时调用
void Start() { }           // 第一帧前调用

// 更新
void Update() { }          // 每帧调用
void FixedUpdate() { }     // 固定时间调用(物理)
void LateUpdate() { }      // Update后调用

// 碰撞(需要Collider)
void OnCollisionEnter(Collision col) { }
void OnCollisionStay(Collision col) { }
void OnCollisionExit(Collision col) { }

// 触发器(需要Collider + isTrigger)
void OnTriggerEnter(Collider other) { }
void OnTriggerStay(Collider other) { }
void OnTriggerExit(Collider other) { }

// 2D版本
void OnCollisionEnter2D(Collision2D col) { }
void OnTriggerEnter2D(Collider2D other) { }

// 鼠标事件
void OnMouseEnter() { }
void OnMouseOver() { }
void OnMouseExit() { }
void OnMouseDown() { }
void OnMouseUp() { }
void OnMouseDrag() { }

// 渲染
void OnBecameVisible() { }
void OnBecameInvisible() { }
void OnPreRender() { }
void OnRenderObject() { }
void OnPostRender() { }

// GUI
void OnGUI() { }

// 应用程序
void OnApplicationFocus(bool focus) { }
void OnApplicationPause(bool pause) { }
void OnApplicationQuit() { }

// 销毁
void OnDisable() { }
void OnDestroy() { }

// 绘制Gizmos
void OnDrawGizmos() { }
void OnDrawGizmosSelected() { }
```

---

## 常用设计模式

### 单例模式

```csharp
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}
```

### 对象池模式

```csharp
public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int poolSize = 10;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(prefab);
    }
    
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

---

## 性能优化建议

### 1. 避免在Update中频繁操作

```csharp
// 不好
void Update()
{
    GameObject player = GameObject.Find("Player"); // 每帧查找
}

// 好
GameObject player;
void Start()
{
    player = GameObject.Find("Player"); // 只查找一次
}
```

### 2. 使用对象池

```csharp
// 不好
void SpawnBullet()
{
    Instantiate(bulletPrefab);
}

// 好
void SpawnBullet()
{
    bulletPool.Get();
}
```

### 3. 缓存组件引用

```csharp
// 不好
void Update()
{
    GetComponent<Rigidbody>().velocity = Vector3.forward;
}

// 好
Rigidbody rb;
void Start()
{
    rb = GetComponent<Rigidbody>();
}

void Update()
{
    rb.velocity = Vector3.forward;
}
```

### 4. 使用CompareTag

```csharp
// 不好
if (other.gameObject.tag == "Player")

// 好
if (other.CompareTag("Player"))
```

### 5. 避免空引用检查

```csharp
// 不好
if (obj != null)

// 好(如果确定不是Unity对象)
if (obj is not null)

// 最好(Unity对象)
if (obj) // Unity重载了bool运算符
```

---

**文档来源**: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/
**版本**: Unity 2022.3 LTS
**最后更新**: 2026-01-14

**注**: 这是概览文档。详细的类方法和参数说明将在后续 Part 2-4 中提供。
