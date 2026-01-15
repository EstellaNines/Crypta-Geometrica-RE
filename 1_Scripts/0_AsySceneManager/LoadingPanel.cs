using UnityEngine;
using TMPro;

/// <summary>
/// 加载界面UI控制器
/// 监听 MessageManager 的加载进度消息并更新UI
/// 挂载于 LoadingScene 的 Canvas 下
/// </summary>
public class LoadingPanel : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("行走小人动画")]
    [SerializeField] private RectTransform characterContainer;  // 小人容器
    [SerializeField] private Animator characterAnimator;        // 小人动画控制器
    [SerializeField] private string walkAnimationName = "Walk"; // 行走动画状态名

    [Header("移动范围设置")]
    [SerializeField] private float startPosX = -800f;  // 起始X坐标（屏幕左侧）
    [SerializeField] private float endPosX = 800f;     // 结束X坐标（屏幕右侧）
    [SerializeField] private float characterY = -200f; // 小人Y坐标（固定高度）

    [Header("平滑移动")]
    [SerializeField] private bool useSmoothMove = true;  // 是否使用平滑移动
    [SerializeField] private float moveSmoothSpeed = 5f; // 移动平滑速度

    private float currentPosX;  // 当前X坐标（用于平滑插值）
    private float targetPosX;   // 目标X坐标

    [Header("显示设置")]
    [SerializeField] private string[] loadingTips = new string[]
    {
        "Loading game resources...",
        "Initializing scene...",
        "Loading character data...",
        "Loading level configuration...",
        "Preparing game environment...",
        "Loading audio resources...",
        "Loading UI interface...",
        "Building world..."
    };

    #region 生命周期

    private void OnEnable()
    {
        // 注册消息监听
        MessageManager.AddListener<float>(MessageType.SCENE_LOADING_PROGRESS, OnLoadingProgress);
        MessageManager.AddListener<string>(MessageType.SCENE_LOADING_COMPLETED, OnLoadingCompleted);

        // 初始化UI
        InitializeUI();
    }

    private void OnDisable()
    {
        // 移除消息监听（必须！防止空引用）
        MessageManager.RemoveListener<float>(MessageType.SCENE_LOADING_PROGRESS, OnLoadingProgress);
        MessageManager.RemoveListener<string>(MessageType.SCENE_LOADING_COMPLETED, OnLoadingCompleted);
    }

    #endregion

    #region 初始化

    private void InitializeUI()
    {
        // 设置小人初始位置（屏幕左侧）
        currentPosX = startPosX;
        targetPosX = startPosX;
        if (characterContainer != null)
        {
            characterContainer.anchoredPosition = new Vector2(startPosX, characterY);
        }

        // 播放行走动画
        if (characterAnimator != null)
        {
            if (characterAnimator.runtimeAnimatorController != null)
            {
                characterAnimator.Play(walkAnimationName, 0, 0f);
            }
            else
            {
                Debug.LogWarning("[LoadingPanel] Animator does not have an AnimatorController assigned. Please assign the ProgressCharacter controller in the Inspector.");
            }
        }
        else
        {
            Debug.LogWarning("[LoadingPanel] Character Animator is not assigned. Please assign the Animator component in the Inspector.");
        }

        // 重置进度文本
        if (progressText != null)
        {
            progressText.text = "0%";
        }

        // 显示随机提示
        ShowRandomTip();
    }

    private void ShowRandomTip()
    {
        if (tipText != null && loadingTips != null && loadingTips.Length > 0)
        {
            int index = Random.Range(0, loadingTips.Length);
            tipText.text = loadingTips[index];
        }
    }

    #endregion

    #region 消息回调

    /// <summary>
    /// 进度更新回调
    /// </summary>
    private void OnLoadingProgress(float progress)
    {
        // 直接更新位置，确保实际加载时也能正常工作
        UpdateCharacterPosition(progress);
    }

    /// <summary>
    /// 更新小人位置
    /// </summary>
    private void UpdateCharacterPosition(float progress)
    {
        // 计算目标X坐标
        targetPosX = Mathf.Lerp(startPosX, endPosX, progress);

        // 移动小人
        if (characterContainer != null)
        {
            if (useSmoothMove)
            {
                // 平滑移动
                currentPosX = Mathf.Lerp(currentPosX, targetPosX, Time.deltaTime * moveSmoothSpeed);
                characterContainer.anchoredPosition = new Vector2(currentPosX, characterY);
            }
            else
            {
                // 直接移动
                characterContainer.anchoredPosition = new Vector2(targetPosX, characterY);
            }
        }

        // 更新进度文本
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }

    /// <summary>
    /// 加载完成回调
    /// </summary>
    private void OnLoadingCompleted(string sceneName)
    {
        Debug.Log($"[LoadingPanel] 场景加载完成: {sceneName}");
        // 可在此处添加完成音效或动画
    }

    #endregion
}
