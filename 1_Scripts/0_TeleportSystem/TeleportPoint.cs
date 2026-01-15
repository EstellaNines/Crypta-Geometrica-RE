using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 传送点组件
/// 玩家进入后按键传送到目标场景
/// 使用新输入系统 (PlayerInputSystem.GamePlay.Operate)
/// </summary>
public class TeleportPoint : MonoBehaviour
{
    #region 序列化字段

    [Header("传送配置")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private bool updateLevelProgress = true;

    [Header("提示 UI")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string promptMessage = "Press E to continue";

    [Header("触发配置")]
    [SerializeField] private string playerTag = "Player";

    [Header("Gizmo 显示")]
    [SerializeField] private Color gizmoColor = Color.cyan;

    #endregion

    #region 私有变量

    private bool playerInRange = false;
    private bool isTeleporting = false;
    private PlayerInputSystem inputSystem;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 初始化输入系统
        inputSystem = new PlayerInputSystem();
    }

    private void Start()
    {
        // 自动注册到 TeleportManager
        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.RegisterTeleportPoint(this);
        }

        // 初始化提示 UI
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }

        // 设置提示文本
        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
    }

    private void OnEnable()
    {
        if (inputSystem != null)
        {
            inputSystem.GamePlay.Enable();
            inputSystem.GamePlay.Operate.performed += OnOperatePerformed;
        }
    }

    private void OnDisable()
    {
        if (inputSystem != null)
        {
            inputSystem.GamePlay.Operate.performed -= OnOperatePerformed;
            inputSystem.GamePlay.Disable();
        }
    }

    private void OnDestroy()
    {
        // 取消注册
        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.UnregisterTeleportPoint(this);
        }

        // 释放输入系统
        inputSystem?.Dispose();
    }

    #endregion

    #region 输入处理

    private void OnOperatePerformed(InputAction.CallbackContext context)
    {
        if (playerInRange && !isTeleporting)
        {
            Teleport();
        }
    }

    #endregion

    #region 触发器

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerEnter();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerExit();
        }
    }

    // 3D 触发器支持
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerExit();
        }
    }

    #endregion

    #region 私有方法

    private void OnPlayerEnter()
    {
        playerInRange = true;
        ShowPrompt();
        Debug.Log($"[TeleportPoint] 玩家进入传送区域: {name}");
    }

    private void OnPlayerExit()
    {
        playerInRange = false;
        HidePrompt();
        Debug.Log($"[TeleportPoint] 玩家离开传送区域: {name}");
    }

    private void ShowPrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    private void Teleport()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"[TeleportPoint] 目标场景未设置: {name}");
            return;
        }

        isTeleporting = true;
        HidePrompt();

        Debug.Log($"[TeleportPoint] 开始传送到: {targetSceneName}");

        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.TeleportToScene(targetSceneName, updateLevelProgress);
        }
        else if (AsyncSceneManager.Instance != null)
        {
            // 回退方案：直接使用 AsyncSceneManager
            AsyncSceneManager.Instance.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("[TeleportPoint] 无法传送：TeleportManager 和 AsyncSceneManager 都不存在");
            isTeleporting = false;
        }
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 目标场景名
    /// </summary>
    public string TargetSceneName
    {
        get => targetSceneName;
        set => targetSceneName = value;
    }

    #endregion

    #region Editor Gizmo

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // 绘制触发区域
        Collider2D col2D = GetComponent<Collider2D>();
        Collider col3D = GetComponent<Collider>();

        if (col2D != null)
        {
            Gizmos.DrawWireCube(col2D.bounds.center, col2D.bounds.size);
        }
        else if (col3D != null)
        {
            Gizmos.DrawWireCube(col3D.bounds.center, col3D.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);

        Collider2D col2D = GetComponent<Collider2D>();
        Collider col3D = GetComponent<Collider>();

        if (col2D != null)
        {
            Gizmos.DrawCube(col2D.bounds.center, col2D.bounds.size);
        }
        else if (col3D != null)
        {
            Gizmos.DrawCube(col3D.bounds.center, col3D.bounds.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, Vector3.one);
        }
    }

    #endregion
}
