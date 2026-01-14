using UnityEngine;

/// <summary>
/// 场景加载代理组件
/// 用于 UI 按钮事件绑定，无需在场景中放置完整的 GameManager
/// 内部调用 AsyncSceneManager.Instance 执行实际加载
/// </summary>
public class SceneLoaderProxy : MonoBehaviour
{
    /// <summary>
    /// 加载场景（通过过渡场景）
    /// 可直接绑定到 UI 按钮的 OnClick 事件
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public void LoadScene(string sceneName)
    {
        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("[SceneLoaderProxy] AsyncSceneManager 不可用！");
        }
    }

    /// <summary>
    /// 直接加载场景（不经过过渡场景）
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public void LoadSceneDirect(string sceneName)
    {
        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadSceneDirect(sceneName);
        }
        else
        {
            Debug.LogError("[SceneLoaderProxy] AsyncSceneManager 不可用！");
        }
    }

    /// <summary>
    /// 返回上一个场景（如果有记录）
    /// </summary>
    public void LoadPreviousScene()
    {
        // 此功能需要 AsyncSceneManager 支持场景历史记录
        // 当前简单实现：加载指定的默认场景
        Debug.LogWarning("[SceneLoaderProxy] LoadPreviousScene 需要场景历史支持，请使用 LoadScene 指定目标场景");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
