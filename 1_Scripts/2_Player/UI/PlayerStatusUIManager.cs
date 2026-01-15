using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家状态栏UI管理器
/// 作为GameManager的子模块，管理状态栏UI的生命周期
/// 在有玩家的场景中显示状态栏，无玩家时隐藏
/// </summary>
public class PlayerStatusUIManager : MonoBehaviour, IGameModule
{
    #region 配置

    [Header("状态栏UI预制件")]
    [SerializeField] private GameObject statusUIPrefab;

    [Header("需要显示状态栏的场景")]
    [SerializeField] private string[] gameScenes = { "5_Game1-1", "5_Game1-2", "5_Game1-3", "5_Game1-4", "5_Game1-5" };

    #endregion

    #region 私有字段

    private GameObject statusUIInstance;
    private PlayerStatusUI statusUI;
    private PlayerController currentPlayer;

    #endregion

    #region IGameModule 实现

    public void OnInit()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("[PlayerStatusUIManager] 初始化完成");
    }

    public void OnUpdate(float deltaTime)
    {
        // 检查玩家状态变化
        if (statusUI != null && statusUIInstance != null && statusUIInstance.activeSelf)
        {
            // 如果玩家丢失，尝试重新查找
            if (currentPlayer == null)
            {
                FindPlayer();
            }
        }
    }

    public void OnDispose()
    {
        // 取消订阅
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 销毁UI实例
        if (statusUIInstance != null)
        {
            Destroy(statusUIInstance);
            statusUIInstance = null;
        }

        Debug.Log("[PlayerStatusUIManager] 已销毁");
    }

    #endregion

    #region 场景管理

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldShow = IsGameScene(scene.name);

        if (shouldShow)
        {
            ShowStatusUI();
            FindPlayer();
        }
        else
        {
            HideStatusUI();
        }

        Debug.Log($"[PlayerStatusUIManager] 场景: {scene.name}, 显示状态栏: {shouldShow}");
    }

    /// <summary>
    /// 检查是否为游戏场景
    /// </summary>
    private bool IsGameScene(string sceneName)
    {
        foreach (string gameScene in gameScenes)
        {
            if (sceneName.Contains(gameScene) || sceneName.StartsWith("5_Game"))
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region UI管理

    /// <summary>
    /// 显示状态栏UI
    /// </summary>
    private void ShowStatusUI()
    {
        // 如果实例不存在，创建它
        if (statusUIInstance == null && statusUIPrefab != null)
        {
            statusUIInstance = Instantiate(statusUIPrefab, transform);
            statusUI = statusUIInstance.GetComponent<PlayerStatusUI>();
            Debug.Log("[PlayerStatusUIManager] 创建状态栏UI实例");
        }

        // 显示UI
        if (statusUIInstance != null)
        {
            statusUIInstance.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏状态栏UI
    /// </summary>
    private void HideStatusUI()
    {
        if (statusUIInstance != null)
        {
            statusUIInstance.SetActive(false);
        }

        currentPlayer = null;
    }

    /// <summary>
    /// 查找场景中的玩家
    /// </summary>
    private void FindPlayer()
    {
        currentPlayer = FindObjectOfType<PlayerController>();

        if (currentPlayer != null && statusUI != null)
        {
            statusUI.RefreshUI();
            Debug.Log("[PlayerStatusUIManager] 找到玩家，刷新UI");
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 手动刷新状态栏
    /// </summary>
    public void RefreshStatusUI()
    {
        FindPlayer();
        if (statusUI != null)
        {
            statusUI.RefreshUI();
        }
    }

    /// <summary>
    /// 强制显示状态栏（用于特殊场景）
    /// </summary>
    public void ForceShow()
    {
        ShowStatusUI();
        FindPlayer();
    }

    /// <summary>
    /// 强制隐藏状态栏
    /// </summary>
    public void ForceHide()
    {
        HideStatusUI();
    }

    /// <summary>
    /// 获取状态栏UI实例
    /// </summary>
    public PlayerStatusUI GetStatusUI()
    {
        return statusUI;
    }

    #endregion
}
