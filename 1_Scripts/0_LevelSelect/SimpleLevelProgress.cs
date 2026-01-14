using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 简化的关卡进度显示
/// 显示 5 个关卡（1-1 到 1-5），高亮当前关卡
/// </summary>
public class SimpleLevelProgress : MonoBehaviour
{
    #region 序列化字段

    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("关卡图标 (按顺序 1-1 到 1-5)")]
    [SerializeField] private Image[] levelIcons;
    [SerializeField] private TextMeshProUGUI[] levelNumberTexts;

    [Header("高亮样式")]
    [SerializeField] private Color normalIconColor = Color.white;
    [SerializeField] private Color currentIconColor = Color.red;
    [SerializeField] private Color normalTextColor = Color.gray;
    [SerializeField] private Color currentTextColor = Color.white;

    [Header("关卡场景名 (按顺序)")]
    [SerializeField] private string[] levelSceneNames = new string[]
    {
        "Level_1_1",
        "Level_1_2",
        "Level_1_3",
        "Level_1_4",
        "Level_1_5"
    };

    [Header("配置")]
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private bool autoStartLevel = true;

    #endregion

    #region 私有变量

    private int currentLevelIndex = 0;

    #endregion

    #region 生命周期

    private void Start()
    {
        // 从存档读取当前关卡
        LoadCurrentLevel();

        // 更新显示
        UpdateDisplay();

        // 自动开始
        if (autoStartLevel)
        {
            StartCoroutine(AutoStartCoroutine());
        }
    }

    #endregion

    #region 私有方法

    private void LoadCurrentLevel()
    {
        currentLevelIndex = 0; // 默认 1-1

        if (SaveManager.Instance != null)
        {
            GlobalSaveData globalData = SaveManager.Instance.GetGlobalData();
            if (globalData != null)
            {
                currentLevelIndex = Mathf.Clamp(globalData.currentLevel, 0, levelSceneNames.Length - 1);
            }
        }

        Debug.Log($"[SimpleLevelProgress] 当前关卡索引: {currentLevelIndex} (1-{currentLevelIndex + 1})");
    }

    private void UpdateDisplay()
    {
        // 更新标题
        if (titleText != null)
        {
            titleText.text = $"LEVEL 1-{currentLevelIndex + 1}";
        }

        // 更新所有关卡图标
        for (int i = 0; i < levelIcons.Length; i++)
        {
            bool isCurrent = (i == currentLevelIndex);

            // 更新图标颜色
            if (levelIcons[i] != null)
            {
                levelIcons[i].color = isCurrent ? currentIconColor : normalIconColor;
            }

            // 更新文本颜色
            if (i < levelNumberTexts.Length && levelNumberTexts[i] != null)
            {
                levelNumberTexts[i].color = isCurrent ? currentTextColor : normalTextColor;
            }
        }
    }

    private IEnumerator AutoStartCoroutine()
    {
        yield return new WaitForSeconds(displayDuration);
        StartLevel();
    }

    private void StartLevel()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levelSceneNames.Length)
        {
            Debug.LogError("[SimpleLevelProgress] 无效的关卡索引");
            return;
        }

        string sceneName = levelSceneNames[currentLevelIndex];

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SimpleLevelProgress] 关卡场景名未配置");
            return;
        }

        Debug.Log($"[SimpleLevelProgress] 开始关卡: 1-{currentLevelIndex + 1} -> {sceneName}");

        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(sceneName);
        }
    }

    #endregion

    #region 公共方法 (可选，用于按钮)

    /// <summary>
    /// 手动开始关卡
    /// </summary>
    public void ManualStartLevel()
    {
        StopAllCoroutines();
        StartLevel();
    }

    /// <summary>
    /// 返回存档选择
    /// </summary>
    public void GoBack()
    {
        StopAllCoroutines();

        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene("2_Save");
        }
    }

    #endregion
}
