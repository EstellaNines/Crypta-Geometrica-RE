using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MessageManager辅助组件
/// 负责在场景切换时自动清理非永久消息
/// 游戏启动时自动创建，无需手动挂载
/// </summary>
public class MessageManagerHelper : MonoBehaviour
{
    private static MessageManagerHelper instance;

    /// <summary>
    /// 初始化Helper（在游戏启动时调用一次即可）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("[MessageManagerHelper]");
            instance = go.AddComponent<MessageManagerHelper>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成时清理消息表
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 仅在单场景加载模式下清理（Additive模式不清理）
        if (mode == LoadSceneMode.Single)
        {
            MessageManager.Cleanup();
            Debug.Log($"[MessageManagerHelper] 场景 {scene.name} 加载完成，已清理消息表");
        }
    }
}
