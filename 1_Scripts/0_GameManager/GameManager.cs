using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏管理器 - 容器化服务架构
/// 作为游戏唯一的根节点和持久化对象，统一管理所有游戏模块
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例

    private static GameManager instance;

    /// <summary>
    /// GameManager单例实例
    /// </summary>
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("[GameManager] 实例不存在！请确保场景中有GameManager对象。");
            }
            return instance;
        }
    }

    /// <summary>
    /// 检查GameManager是否已初始化
    /// </summary>
    public static bool IsInitialized => instance != null;

    #endregion

    #region 模块管理

    // 模块字典：按类型存储模块引用
    private Dictionary<Type, IGameModule> modules = new Dictionary<Type, IGameModule>();

    // 模块列表：按注册顺序存储，用于有序更新
    private List<IGameModule> moduleList = new List<IGameModule>();

    // 是否已完成初始化
    private bool isInitialized = false;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 单例检查
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[GameManager] 检测到重复实例，销毁当前对象。");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[GameManager] 初始化开始...");

        // 收集并初始化所有子模块
        CollectModules();
        InitializeModules();

        isInitialized = true;
        Debug.Log($"[GameManager] 初始化完成，已注册 {modules.Count} 个模块。");
    }

    private void Update()
    {
        if (!isInitialized) return;

        float deltaTime = Time.deltaTime;
        foreach (var module in moduleList)
        {
            module.OnUpdate(deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        Debug.Log("[GameManager] 开始销毁模块...");

        // 逆序销毁模块
        for (int i = moduleList.Count - 1; i >= 0; i--)
        {
            try
            {
                moduleList[i].OnDispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] 模块销毁异常: {e.Message}");
            }
        }

        modules.Clear();
        moduleList.Clear();
        instance = null;

        Debug.Log("[GameManager] 销毁完成。");
    }

    #endregion

    #region 模块收集与初始化

    /// <summary>
    /// 收集子节点上的所有模块
    /// </summary>
    private void CollectModules()
    {
        // 获取所有子节点上实现IGameModule的组件
        var childModules = GetComponentsInChildren<IGameModule>(true);

        foreach (var module in childModules)
        {
            // 跳过GameManager自身（如果它实现了IGameModule）
            if (module is GameManager) continue;

            Type moduleType = module.GetType();

            if (modules.ContainsKey(moduleType))
            {
                Debug.LogWarning($"[GameManager] 模块类型重复: {moduleType.Name}，跳过注册。");
                continue;
            }

            modules[moduleType] = module;
            moduleList.Add(module);

            Debug.Log($"[GameManager] 收集模块: {moduleType.Name}");
        }
    }

    /// <summary>
    /// 按顺序初始化所有模块
    /// </summary>
    private void InitializeModules()
    {
        foreach (var module in moduleList)
        {
            try
            {
                module.OnInit();
                Debug.Log($"[GameManager] 模块初始化完成: {module.GetType().Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] 模块初始化失败 {module.GetType().Name}: {e.Message}");
            }
        }
    }

    #endregion

    #region 公共API

    /// <summary>
    /// 获取指定类型的模块
    /// </summary>
    /// <typeparam name="T">模块类型</typeparam>
    /// <returns>模块实例，不存在返回null</returns>
    public static T Get<T>() where T : class, IGameModule
    {
        if (instance == null)
        {
            Debug.LogError("[GameManager] 实例不存在，无法获取模块。");
            return null;
        }

        Type type = typeof(T);
        if (instance.modules.TryGetValue(type, out IGameModule module))
        {
            return module as T;
        }

        Debug.LogWarning($"[GameManager] 模块不存在: {type.Name}");
        return null;
    }

    /// <summary>
    /// 尝试获取指定类型的模块
    /// </summary>
    /// <typeparam name="T">模块类型</typeparam>
    /// <param name="module">输出模块实例</param>
    /// <returns>是否成功获取</returns>
    public static bool TryGet<T>(out T module) where T : class, IGameModule
    {
        module = Get<T>();
        return module != null;
    }

    /// <summary>
    /// 检查是否存在指定类型的模块
    /// </summary>
    /// <typeparam name="T">模块类型</typeparam>
    /// <returns>是否存在</returns>
    public static bool Has<T>() where T : class, IGameModule
    {
        if (instance == null) return false;
        return instance.modules.ContainsKey(typeof(T));
    }

    /// <summary>
    /// 运行时注册模块（用于动态添加模块）
    /// </summary>
    /// <typeparam name="T">模块类型</typeparam>
    /// <param name="module">模块实例</param>
    public static void Register<T>(T module) where T : class, IGameModule
    {
        if (instance == null)
        {
            Debug.LogError("[GameManager] 实例不存在，无法注册模块。");
            return;
        }

        Type type = typeof(T);
        if (instance.modules.ContainsKey(type))
        {
            Debug.LogWarning($"[GameManager] 模块已存在: {type.Name}，跳过注册。");
            return;
        }

        instance.modules[type] = module;
        instance.moduleList.Add(module);

        // 如果GameManager已初始化，立即初始化新模块
        if (instance.isInitialized)
        {
            try
            {
                module.OnInit();
                Debug.Log($"[GameManager] 动态注册并初始化模块: {type.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] 动态模块初始化失败 {type.Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 运行时注销模块
    /// </summary>
    /// <typeparam name="T">模块类型</typeparam>
    public static void Unregister<T>() where T : class, IGameModule
    {
        if (instance == null) return;

        Type type = typeof(T);
        if (instance.modules.TryGetValue(type, out IGameModule module))
        {
            try
            {
                module.OnDispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] 模块注销异常 {type.Name}: {e.Message}");
            }

            instance.modules.Remove(type);
            instance.moduleList.Remove(module);

            Debug.Log($"[GameManager] 注销模块: {type.Name}");
        }
    }

    #endregion

    #region 调试

    /// <summary>
    /// 打印所有已注册的模块
    /// </summary>
    [ContextMenu("Print Modules")]
    public void PrintModules()
    {
        Debug.Log("========== GameManager 模块列表 ==========");
        for (int i = 0; i < moduleList.Count; i++)
        {
            Debug.Log($"  [{i}] {moduleList[i].GetType().Name}");
        }
        Debug.Log($"总计: {moduleList.Count} 个模块");
        Debug.Log("==========================================");
    }

    #endregion
}
