using UnityEngine;

/// <summary>
/// GameManager 自动初始化器
/// 在任何场景进入运行模式时自动创建 GameManager 及其所有子模块
/// 仅在开发阶段使用，发布时应确保入口场景已有 GameManager
/// </summary>
public static class GameManagerAutoInitializer
{
    /// <summary>
    /// 在场景加载完成后自动执行
    /// 使用 AfterSceneLoad 确保能正确检测场景中已有的 GameManager
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        // 检查是否已存在 GameManager（通过静态实例）
        if (GameManager.IsInitialized)
        {
            Debug.Log("[GameManagerAutoInitializer] GameManager 已初始化，跳过自动创建。");
            return;
        }

        // 检查场景中是否有 GameManager（可能还未执行 Awake）
        GameManager existingGM = Object.FindObjectOfType<GameManager>();
        if (existingGM != null)
        {
            Debug.Log("[GameManagerAutoInitializer] 场景中已有 GameManager，无需自动创建。");
            return;
        }

        // 自动创建 GameManager 结构
        Debug.Log("[GameManagerAutoInitializer] 未检测到 GameManager，自动创建中...");
        CreateGameManagerStructure();
    }

    /// <summary>
    /// 创建完整的 GameManager 层级结构
    /// 注意：必须先创建所有子对象和组件，最后再添加 GameManager 组件
    /// 因为 GameManager.Awake() 会立即收集子模块
    /// </summary>
    private static void CreateGameManagerStructure()
    {
        // 1. 创建根节点（先不添加 GameManager 组件）
        GameObject gmRoot = new GameObject("[GameManager]");
        Object.DontDestroyOnLoad(gmRoot);

        // 2. 先创建所有子模块（在添加 GameManager 之前）
        GameObject asyncSceneMgr = new GameObject("[AsyncSceneManager]");
        asyncSceneMgr.transform.SetParent(gmRoot.transform);
        asyncSceneMgr.AddComponent<AsyncSceneManager>();

        GameObject saveMgr = new GameObject("[SaveManager]");
        saveMgr.transform.SetParent(gmRoot.transform);
        saveMgr.AddComponent<SaveManager>();

        // 3. 最后添加 GameManager 组件（此时子模块已存在，可以正确收集）
        gmRoot.AddComponent<GameManager>();

        Debug.Log("[GameManagerAutoInitializer] GameManager 结构自动创建完成！");
        Debug.Log("  - [GameManager]");
        Debug.Log("    - [AsyncSceneManager]");
        Debug.Log("    - [SaveManager]");
    }
}
