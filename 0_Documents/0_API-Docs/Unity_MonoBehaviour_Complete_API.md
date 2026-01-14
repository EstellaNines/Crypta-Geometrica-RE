# MonoBehaviour 完整 API 参考文档

## 目录
1. [MonoBehaviour 简介](#monobehaviour-简介)
2. [生命周期方法](#生命周期方法)
3. [协程方法](#协程方法)
4. [组件访问方法](#组件访问方法)
5. [GameObject 操作方法](#gameobject-操作方法)
6. [消息方法](#消息方法)
7. [属性](#属性)
8. [最佳实践](#最佳实践)

---

## MonoBehaviour 简介

**命名空间**: `UnityEngine`

**继承链**: `Object` → `Component` → `Behaviour` → `MonoBehaviour`

**描述**: MonoBehaviour 是所有 Unity 脚本的基类,提供了生命周期方法、协程、消息系统等核心功能。

---

## 生命周期方法

### 初始化阶段

#### Awake()
```csharp
protected void Awake()
```
**调用时机**: 脚本实例被加载时,在所有对象初始化后,Start() 之前调用

**用途**: 
- 初始化变量
- 设置对象引用
- 在 Start() 之前需要完成的初始化

**特点**:
- 即使脚本未启用也会调用
- 在场景加载时只调用一次
- 在所有 Start() 之前执行

**示例**:
```csharp
void Awake()
{
    // 单例模式
    if (instance == null)
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else
    {
        Destroy(gameObject);
    }
    
    // 获取组件引用
    rb = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
}
```

---

#### OnEnable()
```csharp
protected void OnEnable()
```
**调用时机**: 对象启用时调用

**用途**:
- 注册事件监听
- 重置状态
- 启用时的初始化

**特点**:
- 每次启用都会调用
- 在 Awake() 之后,Start() 之前

**示例**:
```csharp
void OnEnable()
{
    // 注册事件
    EventManager.OnPlayerDeath += HandlePlayerDeath;
    
    // 重置状态
    health = maxHealth;
    isAlive = true;
}
```

---

#### Start()
```csharp
protected void Start()
```
**调用时机**: 第一次 Update() 之前调用

**用途**:
- 初始化需要其他对象已完成 Awake() 的逻辑
- 设置初始状态

**特点**:
- 只在脚本启用时调用一次
- 在所有 Awake() 之后执行
- 可以使用协程

**示例**:
```csharp
void Start()
{
    // 查找其他对象(此时所有对象已 Awake)
    player = GameObject.FindGameObjectWithTag("Player");
    
    // 启动协程
    StartCoroutine(SpawnEnemies());
    
    // 初始化UI
    UpdateHealthUI();
}
```

---

### 更新阶段

#### Update()
```csharp
protected void Update()
```
**调用时机**: 每帧调用一次

**用途**:
- 处理输入
- 游戏逻辑更新
- 非物理相关的移动

**特点**:
- 帧率不固定,使用 `Time.deltaTime` 保证平滑
- 不适合物理计算

**示例**:
```csharp
void Update()
{
    // 输入处理
    float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");
    
    // 移动(非物理)
    transform.position += new Vector3(horizontal, vertical, 0) * speed * Time.deltaTime;
    
    // 检测按键
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Jump();
    }
    
    // 更新计时器
    timer += Time.deltaTime;
}
```

---

#### FixedUpdate()
```csharp
protected void FixedUpdate()
```
**调用时机**: 固定时间间隔调用(默认 0.02 秒)

**用途**:
- 物理计算
- Rigidbody 操作
- 需要固定时间步长的逻辑

**特点**:
- 与帧率无关
- 适合物理模拟
- 使用 `Time.fixedDeltaTime`

**示例**:
```csharp
void FixedUpdate()
{
    // 物理移动
    rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
    
    // 施加力
    rb.AddForce(Vector2.up * jumpForce);
    
    // 物理检测
    isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
}
```

---

#### LateUpdate()
```csharp
protected void LateUpdate()
```
**调用时机**: 所有 Update() 之后调用

**用途**:
- 相机跟随
- 需要在其他对象更新后执行的逻辑
- 最终位置调整

**特点**:
- 在所有 Update() 完成后执行
- 适合相机和跟随逻辑

**示例**:
```csharp
void LateUpdate()
{
    // 相机跟随玩家
    Vector3 targetPos = player.position + offset;
    transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    
    // 角色朝向鼠标
    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    transform.right = mousePos - transform.position;
}
```

---

### 物理碰撞方法

#### OnCollisionEnter(Collision collision)
```csharp
protected void OnCollisionEnter(Collision collision)
```
**调用时机**: 碰撞开始时

**参数**: `Collision collision` - 碰撞信息

**用途**: 处理碰撞开始事件

**示例**:
```csharp
void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Enemy"))
    {
        TakeDamage(10);
    }
    
    // 获取碰撞点
    ContactPoint contact = collision.contacts[0];
    Vector3 hitPoint = contact.point;
    Vector3 hitNormal = contact.normal;
    
    // 播放音效
    AudioSource.PlayClipAtPoint(hitSound, hitPoint);
}
```

---

#### OnCollisionStay(Collision collision)
```csharp
protected void OnCollisionStay(Collision collision)
```
**调用时机**: 碰撞持续时,每帧调用

**用途**: 持续碰撞处理

**示例**:
```csharp
void OnCollisionStay(Collision collision)
{
    if (collision.gameObject.CompareTag("Fire"))
    {
        TakeDamage(burnDamage * Time.deltaTime);
    }
}
```

---

#### OnCollisionExit(Collision collision)
```csharp
protected void OnCollisionExit(Collision collision)
```
**调用时机**: 碰撞结束时

**用途**: 处理碰撞结束事件

**示例**:
```csharp
void OnCollisionExit(Collision collision)
{
    if (collision.gameObject.CompareTag("Platform"))
    {
        isOnPlatform = false;
    }
}
```

---

### 2D 碰撞方法

#### OnCollisionEnter2D(Collision2D collision)
```csharp
protected void OnCollisionEnter2D(Collision2D collision)
```
**2D 版本的碰撞方法**

**示例**:
```csharp
void OnCollisionEnter2D(Collision2D collision)
{
    if (collision.gameObject.CompareTag("Ground"))
    {
        isGrounded = true;
        canJump = true;
    }
    
    // 获取相对速度
    float impactForce = collision.relativeVelocity.magnitude;
    if (impactForce > damageThreshold)
    {
        TakeDamage(impactForce);
    }
}
```

---

### 触发器方法

#### OnTriggerEnter(Collider other)
```csharp
protected void OnTriggerEnter(Collider other)
```
**调用时机**: 进入触发器时

**要求**: Collider 组件的 `isTrigger = true`

**用途**: 检测进入区域

**示例**:
```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Coin"))
    {
        CollectCoin(other.gameObject);
        Destroy(other.gameObject);
    }
    
    if (other.CompareTag("Checkpoint"))
    {
        SaveCheckpoint(other.transform.position);
    }
}
```

---

#### OnTriggerStay(Collider other)
```csharp
protected void OnTriggerStay(Collider other)
```
**调用时机**: 停留在触发器内,每帧调用

**示例**:
```csharp
void OnTriggerStay(Collider other)
{
    if (other.CompareTag("Water"))
    {
        ApplyWaterPhysics();
    }
}
```

---

#### OnTriggerExit(Collider other)
```csharp
protected void OnTriggerExit(Collider other)
```
**调用时机**: 离开触发器时

**示例**:
```csharp
void OnTriggerExit(Collider other)
{
    if (other.CompareTag("SafeZone"))
    {
        canTakeDamage = true;
    }
}
```

---

### 2D 触发器方法

#### OnTriggerEnter2D(Collider2D other)
```csharp
protected void OnTriggerEnter2D(Collider2D other)
```
**2D 版本的触发器方法**

**示例**:
```csharp
void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("PowerUp"))
    {
        PowerUp powerUp = other.GetComponent<PowerUp>();
        ApplyPowerUp(powerUp.type, powerUp.duration);
        Destroy(other.gameObject);
    }
}
```

---

### 鼠标事件方法

#### OnMouseEnter()
```csharp
protected void OnMouseEnter()
```
**调用时机**: 鼠标进入对象时

**要求**: 对象需要 Collider

**示例**:
```csharp
void OnMouseEnter()
{
    // 高亮显示
    GetComponent<Renderer>().material.color = highlightColor;
    
    // 显示提示
    tooltipUI.SetActive(true);
}
```

---

#### OnMouseOver()
```csharp
protected void OnMouseOver()
```
**调用时机**: 鼠标停留在对象上,每帧调用

**示例**:
```csharp
void OnMouseOver()
{
    if (Input.GetMouseButtonDown(0))
    {
        OnClicked();
    }
}
```

---

#### OnMouseExit()
```csharp
protected void OnMouseExit()
```
**调用时机**: 鼠标离开对象时

**示例**:
```csharp
void OnMouseExit()
{
    GetComponent<Renderer>().material.color = normalColor;
    tooltipUI.SetActive(false);
}
```

---

#### OnMouseDown()
```csharp
protected void OnMouseDown()
```
**调用时机**: 鼠标在对象上按下时

**示例**:
```csharp
void OnMouseDown()
{
    isDragging = true;
    dragOffset = transform.position - GetMouseWorldPosition();
}
```

---

#### OnMouseUp()
```csharp
protected void OnMouseUp()
```
**调用时机**: 鼠标在对象上释放时

**示例**:
```csharp
void OnMouseUp()
{
    isDragging = false;
    SnapToGrid();
}
```

---

#### OnMouseDrag()
```csharp
protected void OnMouseDrag()
```
**调用时机**: 鼠标拖拽对象时,每帧调用

**示例**:
```csharp
void OnMouseDrag()
{
    Vector3 mousePos = GetMouseWorldPosition();
    transform.position = mousePos + dragOffset;
}
```

---

### 渲染事件方法

#### OnBecameVisible()
```csharp
protected void OnBecameVisible()
```
**调用时机**: 对象对任意相机可见时

**用途**: 优化,启用渲染相关逻辑

**示例**:
```csharp
void OnBecameVisible()
{
    isVisible = true;
    animator.enabled = true;
}
```

---

#### OnBecameInvisible()
```csharp
protected void OnBecameInvisible()
```
**调用时机**: 对象对所有相机不可见时

**示例**:
```csharp
void OnBecameInvisible()
{
    isVisible = false;
    animator.enabled = false; // 优化性能
}
```

---

#### OnPreRender()
```csharp
protected void OnPreRender()
```
**调用时机**: 相机渲染场景前

**要求**: 附加到 Camera 对象

---

#### OnRenderObject()
```csharp
protected void OnRenderObject()
```
**调用时机**: 所有常规场景渲染完成后

**用途**: 自定义渲染

---

#### OnPostRender()
```csharp
protected void OnPostRender()
```
**调用时机**: 相机完成场景渲染后

**要求**: 附加到 Camera 对象

---

### GUI 方法

#### OnGUI()
```csharp
protected void OnGUI()
```
**调用时机**: 渲染和处理 GUI 事件时

**用途**: 绘制 GUI(不推荐,建议使用 UI 系统)

**示例**:
```csharp
void OnGUI()
{
    if (GUI.Button(new Rect(10, 10, 100, 30), "Click Me"))
    {
        Debug.Log("Button clicked!");
    }
    
    GUI.Label(new Rect(10, 50, 200, 30), $"Score: {score}");
}
```

---

### 应用程序事件方法

#### OnApplicationFocus(bool hasFocus)
```csharp
protected void OnApplicationFocus(bool hasFocus)
```
**调用时机**: 应用程序获得/失去焦点时

**参数**: `bool hasFocus` - 是否获得焦点

**示例**:
```csharp
void OnApplicationFocus(bool hasFocus)
{
    if (!hasFocus)
    {
        PauseGame();
    }
    else
    {
        ResumeGame();
    }
}
```

---

#### OnApplicationPause(bool pauseStatus)
```csharp
protected void OnApplicationPause(bool pauseStatus)
```
**调用时机**: 应用程序暂停/恢复时

**参数**: `bool pauseStatus` - 是否暂停

**示例**:
```csharp
void OnApplicationPause(bool pauseStatus)
{
    if (pauseStatus)
    {
        SaveGame();
    }
}
```

---

#### OnApplicationQuit()
```csharp
protected void OnApplicationQuit()
```
**调用时机**: 应用程序退出前

**用途**: 清理资源,保存数据

**示例**:
```csharp
void OnApplicationQuit()
{
    SavePlayerData();
    CloseConnections();
}
```

---

### 销毁阶段方法

#### OnDisable()
```csharp
protected void OnDisable()
```
**调用时机**: 对象禁用时

**用途**: 
- 注销事件监听
- 清理资源

**示例**:
```csharp
void OnDisable()
{
    EventManager.OnPlayerDeath -= HandlePlayerDeath;
    StopAllCoroutines();
}
```

---

#### OnDestroy()
```csharp
protected void OnDestroy()
```
**调用时机**: 对象销毁时

**用途**: 最终清理

**示例**:
```csharp
void OnDestroy()
{
    if (particleEffect != null)
    {
        Destroy(particleEffect);
    }
    
    SaveFinalData();
}
```

---

### Gizmos 绘制方法

#### OnDrawGizmos()
```csharp
protected void OnDrawGizmos()
```
**调用时机**: 场景视图绘制 Gizmos 时

**用途**: 可视化调试

**示例**:
```csharp
void OnDrawGizmos()
{
    // 绘制检测范围
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
    
    // 绘制路径
    Gizmos.color = Color.green;
    for (int i = 0; i < waypoints.Length - 1; i++)
    {
        Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
    }
}
```

---

#### OnDrawGizmosSelected()
```csharp
protected void OnDrawGizmosSelected()
```
**调用时机**: 对象被选中时绘制 Gizmos

**示例**:
```csharp
void OnDrawGizmosSelected()
{
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(transform.position, attackRange);
}
```

---

## 协程方法

### StartCoroutine()
```csharp
public Coroutine StartCoroutine(IEnumerator routine)
public Coroutine StartCoroutine(string methodName)
```
**用途**: 启动协程

**返回**: `Coroutine` 对象,可用于停止协程

**示例**:
```csharp
// 方式1: 使用 IEnumerator
Coroutine myCoroutine = StartCoroutine(MyCoroutine());

// 方式2: 使用方法名(不推荐)
StartCoroutine("MyCoroutine");

IEnumerator MyCoroutine()
{
    Debug.Log("Start");
    yield return new WaitForSeconds(2f);
    Debug.Log("After 2 seconds");
    
    yield return StartCoroutine(AnotherCoroutine());
    Debug.Log("After another coroutine");
}
```

---

### StopCoroutine()
```csharp
public void StopCoroutine(Coroutine routine)
public void StopCoroutine(IEnumerator routine)
public void StopCoroutine(string methodName)
```
**用途**: 停止协程

**示例**:
```csharp
Coroutine myCoroutine;

void Start()
{
    myCoroutine = StartCoroutine(RepeatAction());
}

void OnDisable()
{
    if (myCoroutine != null)
    {
        StopCoroutine(myCoroutine);
    }
}

IEnumerator RepeatAction()
{
    while (true)
    {
        DoSomething();
        yield return new WaitForSeconds(1f);
    }
}
```

---

### StopAllCoroutines()
```csharp
public void StopAllCoroutines()
```
**用途**: 停止所有协程

**示例**:
```csharp
void OnDisable()
{
    StopAllCoroutines();
}
```

---

### 协程 Yield 指令

| 指令 | 描述 | 示例 |
|------|------|------|
| `yield return null` | 等待下一帧 | `yield return null;` |
| `yield return new WaitForSeconds(float)` | 等待指定秒数 | `yield return new WaitForSeconds(2f);` |
| `yield return new WaitForSecondsRealtime(float)` | 等待真实时间(不受 timeScale 影响) | `yield return new WaitForSecondsRealtime(1f);` |
| `yield return new WaitForFixedUpdate()` | 等待下一次 FixedUpdate | `yield return new WaitForFixedUpdate();` |
| `yield return new WaitForEndOfFrame()` | 等待帧结束 | `yield return new WaitForEndOfFrame();` |
| `yield return new WaitUntil(Func<bool>)` | 等待条件为真 | `yield return new WaitUntil(() => isReady);` |
| `yield return new WaitWhile(Func<bool>)` | 等待条件为假 | `yield return new WaitWhile(() => isLoading);` |
| `yield return StartCoroutine(IEnumerator)` | 等待另一个协程完成 | `yield return StartCoroutine(LoadAssets());` |
| `yield return new WWW(string)` | 等待 Web 请求(已弃用) | 使用 UnityWebRequest |
| `yield return AsyncOperation` | 等待异步操作 | `yield return SceneManager.LoadSceneAsync("Level1");` |

---

## 组件访问方法

### GetComponent<T>()
```csharp
public T GetComponent<T>()
```
**用途**: 获取组件

**返回**: 组件实例,如果不存在返回 null

**示例**:
```csharp
Rigidbody2D rb = GetComponent<Rigidbody2D>();
Animator animator = GetComponent<Animator>();

// 检查是否存在
if (TryGetComponent<Health>(out Health health))
{
    health.TakeDamage(10);
}
```

---

### GetComponents<T>()
```csharp
public T[] GetComponents<T>()
```
**用途**: 获取所有指定类型的组件

**示例**:
```csharp
Collider2D[] colliders = GetComponents<Collider2D>();
foreach (var collider in colliders)
{
    collider.enabled = false;
}
```

---

### GetComponentInChildren<T>()
```csharp
public T GetComponentInChildren<T>(bool includeInactive = false)
```
**用途**: 获取子对象中的组件

**参数**: `includeInactive` - 是否包含未激活的对象

**示例**:
```csharp
// 获取子对象的 SpriteRenderer
SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();

// 包含未激活的对象
Text text = GetComponentInChildren<Text>(true);
```

---

### GetComponentsInChildren<T>()
```csharp
public T[] GetComponentsInChildren<T>(bool includeInactive = false)
```
**用途**: 获取所有子对象中的组件

**示例**:
```csharp
// 获取所有子对象的 Renderer
Renderer[] renderers = GetComponentsInChildren<Renderer>();
foreach (var renderer in renderers)
{
    renderer.enabled = false;
}
```

---

### GetComponentInParent<T>()
```csharp
public T GetComponentInParent<T>()
```
**用途**: 获取父对象中的组件

**示例**:
```csharp
Canvas canvas = GetComponentInParent<Canvas>();
```

---

### GetComponentsInParent<T>()
```csharp
public T[] GetComponentsInParent<T>(bool includeInactive = false)
```
**用途**: 获取所有父对象中的组件

---

### TryGetComponent<T>()
```csharp
public bool TryGetComponent<T>(out T component)
```
**用途**: 尝试获取组件(推荐方式)

**返回**: 是否成功获取

**示例**:
```csharp
if (TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
{
    rb.velocity = Vector2.zero;
}
```

---

## GameObject 操作方法

### Invoke()
```csharp
public void Invoke(string methodName, float time)
```
**用途**: 延迟调用方法

**参数**:
- `methodName` - 方法名(字符串)
- `time` - 延迟时间(秒)

**示例**:
```csharp
void Start()
{
    Invoke("DestroyObject", 3f);
}

void DestroyObject()
{
    Destroy(gameObject);
}
```

---

### InvokeRepeating()
```csharp
public void InvokeRepeating(string methodName, float time, float repeatRate)
```
**用途**: 重复调用方法

**参数**:
- `methodName` - 方法名
- `time` - 首次调用延迟
- `repeatRate` - 重复间隔

**示例**:
```csharp
void Start()
{
    InvokeRepeating("SpawnEnemy", 2f, 5f);
}

void SpawnEnemy()
{
    Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
}
```

---

### CancelInvoke()
```csharp
public void CancelInvoke()
public void CancelInvoke(string methodName)
```
**用途**: 取消 Invoke

**示例**:
```csharp
void OnDisable()
{
    CancelInvoke("SpawnEnemy");
    // 或取消所有
    CancelInvoke();
}
```

---

### IsInvoking()
```csharp
public bool IsInvoking()
public bool IsInvoking(string methodName)
```
**用途**: 检查是否有 Invoke 在执行

**示例**:
```csharp
if (IsInvoking("SpawnEnemy"))
{
    CancelInvoke("SpawnEnemy");
}
```

---

## 属性

### enabled
```csharp
public bool enabled { get; set; }
```
**描述**: 启用/禁用脚本

**示例**:
```csharp
// 禁用脚本
enabled = false;

// 启用脚本
enabled = true;
```

---

### isActiveAndEnabled
```csharp
public bool isActiveAndEnabled { get; }
```
**描述**: 脚本是否激活且启用(只读)

---

### gameObject
```csharp
public GameObject gameObject { get; }
```
**描述**: 脚本附加的 GameObject(只读)

---

### transform
```csharp
public Transform transform { get; }
```
**描述**: 脚本附加的 Transform(只读)

---

### tag
```csharp
public string tag { get; set; }
```
**描述**: GameObject 的标签

**示例**:
```csharp
if (gameObject.tag == "Player")
{
    // 或使用 CompareTag(推荐)
    if (CompareTag("Player"))
    {
        // ...
    }
}
```

---

### name
```csharp
public string name { get; set; }
```
**描述**: GameObject 的名称

---

### useGUILayout
```csharp
public bool useGUILayout { get; set; }
```
**描述**: 是否禁用自动 GUI 布局(优化 OnGUI 性能)

---

## 最佳实践

### 1. 缓存组件引用

```csharp
// 不好
void Update()
{
    GetComponent<Rigidbody2D>().velocity = Vector2.zero;
}

// 好
Rigidbody2D rb;

void Awake()
{
    rb = GetComponent<Rigidbody2D>();
}

void Update()
{
    rb.velocity = Vector2.zero;
}
```

---

### 2. 使用 TryGetComponent

```csharp
// 不好
Health health = GetComponent<Health>();
if (health != null)
{
    health.TakeDamage(10);
}

// 好
if (TryGetComponent<Health>(out Health health))
{
    health.TakeDamage(10);
}
```

---

### 3. 协程优于 Invoke

```csharp
// 不推荐
Invoke("DoSomething", 2f);

// 推荐
StartCoroutine(DoSomethingAfterDelay(2f));

IEnumerator DoSomethingAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    DoSomething();
}
```

---

### 4. 正确使用生命周期

```csharp
// Awake: 初始化自身
void Awake()
{
    rb = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
}

// Start: 访问其他对象
void Start()
{
    player = GameObject.FindGameObjectWithTag("Player");
    gameManager = GameManager.Instance;
}

// Update: 输入和逻辑
void Update()
{
    HandleInput();
    UpdateLogic();
}

// FixedUpdate: 物理
void FixedUpdate()
{
    HandlePhysics();
}

// LateUpdate: 相机和跟随
void LateUpdate()
{
    UpdateCamera();
}
```

---

### 5. 事件注册和注销

```csharp
void OnEnable()
{
    EventManager.OnGameOver += HandleGameOver;
}

void OnDisable()
{
    EventManager.OnGameOver -= HandleGameOver;
}

void HandleGameOver()
{
    // 处理游戏结束
}
```

---

**文档来源**: Unity 2022.3 官方文档
**最后更新**: 2026-01-14
