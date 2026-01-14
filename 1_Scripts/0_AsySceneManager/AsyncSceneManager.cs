using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 异步场景加载管理器
/// 基于"三明治"模式：当前场景 -> LoadingScene -> 目标场景
/// 通过 MessageManager 广播加载进度
/// 实现 IGameModule 接口，由 GameManager 统一管理
/// </summary>
public class AsyncSceneManager : MonoBehaviour, IGameModule
{
    #region 单例兼容

    private static AsyncSceneManager instance;

    /// <summary>
    /// 单例访问（兼容按钮事件调用）
    /// 优先从GameManager获取，回退到自动创建
    /// </summary>
    public static AsyncSceneManager Instance
    {
        get
        {
            // 优先从GameManager获取
            if (instance == null && GameManager.IsInitialized)
            {
                instance = GameManager.Get<AsyncSceneManager>();
            }

            // 回退：自动创建（兼容旧代码）
            if (instance == null)
            {
                Debug.LogWarning("[AsyncSceneManager] 未通过GameManager初始化，自动创建实例。建议将其作为GameManager子节点。");
                GameObject go = new GameObject("[AsyncSceneManager]");
                instance = go.AddComponent<AsyncSceneManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    #endregion

    #region 配置

    [Header("加载场景名称")]
    [SerializeField] private string loadingSceneName = "S_LoadingScene";

    [Header("最小加载时间(秒) - 防止进度条闪烁")]
    [SerializeField] private float minLoadTime = 1.0f;

    [Header("强制加载时间(秒) - 0表示使用实际加载时间，大于0表示强制等待指定时间")]
    [SerializeField] private float forceLoadTime = 0f;

    [Header("进度平滑速度")]
    [SerializeField] private float progressSmoothSpeed = 2.0f;

    #endregion

    #region 状态

    private bool isLoading = false;
    private string targetSceneName;

    /// <summary>是否正在加载中</summary>
    public bool IsLoading => isLoading;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 单例检查：如果已存在实例且不是自己，销毁自己
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // 立即标记永久消息，防止被 MessageManagerHelper 在场景切换时清理
        // 必须在 Awake 中执行，因为 OnInit 可能在场景加载后才调用
        MessageManager.MarkAsPermanent(MessageType.SCENE_LOADING_START);
        MessageManager.MarkAsPermanent(MessageType.SCENE_LOADING_PROGRESS);
        MessageManager.MarkAsPermanent(MessageType.SCENE_LOADING_COMPLETED);

        // 注意：不再自行调用DontDestroyOnLoad
        // 如果作为GameManager子节点，由GameManager统一管理
        // 如果独立存在，通过Instance属性的回退逻辑处理
    }

    #endregion

    #region IGameModule 实现

    /// <summary>
    /// 模块初始化（由GameManager调用）
    /// </summary>
    public void OnInit()
    {
        // 永久消息标记已在 Awake 中完成
        Debug.Log("[AsyncSceneManager] 模块初始化完成");
    }

    /// <summary>
    /// 模块轮询（可选，当前无需实现）
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        // 当前无需轮询逻辑
    }

    /// <summary>
    /// 模块销毁清理
    /// </summary>
    public void OnDispose()
    {
        // 停止所有协程
        StopAllCoroutines();
        isLoading = false;

        if (instance == this)
        {
            instance = null;
        }

        Debug.Log("[AsyncSceneManager] 模块已销毁");
    }

    #endregion

    #region 公共API

    /// <summary>
    /// 加载目标场景（通过中间加载场景过渡）
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"[AsyncSceneManager] 正在加载中，忽略请求: {sceneName}");
            return;
        }

        targetSceneName = sceneName;
        StartCoroutine(LoadSceneCoroutine());
    }

    /// <summary>
    /// 直接加载场景（不经过中间场景，用于特殊情况）
    /// </summary>
    public void LoadSceneDirect(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"[AsyncSceneManager] 正在加载中，忽略请求: {sceneName}");
            return;
        }

        StartCoroutine(LoadSceneDirectCoroutine(sceneName));
    }

    /// <summary>
    /// 设置加载场景名称
    /// </summary>
    public void SetLoadingSceneName(string name)
    {
        loadingSceneName = name;
    }

    /// <summary>
    /// 设置最小加载时间
    /// </summary>
    public void SetMinLoadTime(float time)
    {
        minLoadTime = Mathf.Max(0f, time);
    }

    /// <summary>
    /// 设置强制加载时间
    /// </summary>
    /// <param name="time">0表示使用实际加载时间，大于0表示强制等待指定时间</param>
    public void SetForceLoadTime(float time)
    {
        forceLoadTime = Mathf.Max(0f, time);
    }

    #endregion

    #region 加载协程

    private IEnumerator LoadSceneCoroutine()
    {
        isLoading = true;

        // 1. 广播加载开始
        MessageManager.Broadcast<string>(MessageType.SCENE_LOADING_START, targetSceneName);
        Debug.Log($"[AsyncSceneManager] 开始加载: {targetSceneName}");

        // 2. 同步加载中间场景
        SceneManager.LoadScene(loadingSceneName);

        // 3. 等待一帧，确保 LoadingScene 的 UI 完成初始化和事件注册
        yield return null;

        // 4. 异步加载目标场景
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(targetSceneName);
        asyncOp.allowSceneActivation = false;

        float displayProgress = 0f;
        float elapsedTime = 0f;

        // 确定实际使用的加载时间
        bool useForceTime = forceLoadTime > 0f;
        float targetLoadTime = useForceTime ? forceLoadTime : minLoadTime;

        // 5. 进度循环
        while (!asyncOp.isDone)
        {
            elapsedTime += Time.deltaTime;

            // Unity 的 progress 范围是 0~0.9，映射到 0~1
            float realProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);

            // 如果使用强制加载时间，进度由时间控制
            if (useForceTime)
            {
                // 进度由经过时间决定，确保在forceLoadTime时达到100%
                float timeProgress = Mathf.Clamp01(elapsedTime / forceLoadTime);
                // 使用时间进度，但确保实际加载已完成时才能达到100%
                if (realProgress >= 0.99f)
                {
                    // 实际加载已完成，使用时间进度
                    displayProgress = timeProgress;
                }
                else
                {
                    // 实际加载未完成，进度不能超过90%，留10%给实际加载完成
                    displayProgress = Mathf.Min(timeProgress * 0.9f, realProgress);
                }
            }
            else
            {
                // 平滑进度（视觉效果）
                displayProgress = Mathf.MoveTowards(displayProgress, realProgress, progressSmoothSpeed * Time.deltaTime);
            }

            // 广播进度
            MessageManager.Broadcast<float>(MessageType.SCENE_LOADING_PROGRESS, displayProgress);

            // 完成判定
            bool loadingComplete = asyncOp.progress >= 0.9f;
            bool timeComplete = elapsedTime >= targetLoadTime;
            bool progressComplete = displayProgress >= 0.99f;

            if (loadingComplete && timeComplete && progressComplete)
            {
                // 广播进度 1.0
                MessageManager.Broadcast<float>(MessageType.SCENE_LOADING_PROGRESS, 1f);

                // 允许场景激活
                asyncOp.allowSceneActivation = true;
            }

            yield return null;
        }

        // 6. 广播加载完成
        MessageManager.Broadcast<string>(MessageType.SCENE_LOADING_COMPLETED, targetSceneName);
        Debug.Log($"[AsyncSceneManager] 加载完成: {targetSceneName}");

        isLoading = false;
    }

    /// <summary>
    /// 直接异步加载（不经过中间场景）
    /// </summary>
    private IEnumerator LoadSceneDirectCoroutine(string sceneName)
    {
        isLoading = true;

        MessageManager.Broadcast<string>(MessageType.SCENE_LOADING_START, sceneName);

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncOp.isDone)
        {
            float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            MessageManager.Broadcast<float>(MessageType.SCENE_LOADING_PROGRESS, progress);
            yield return null;
        }

        MessageManager.Broadcast<float>(MessageType.SCENE_LOADING_PROGRESS, 1f);
        MessageManager.Broadcast<string>(MessageType.SCENE_LOADING_COMPLETED, sceneName);

        isLoading = false;
    }

    #endregion
}
