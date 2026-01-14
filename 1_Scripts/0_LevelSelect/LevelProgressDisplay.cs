using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 关卡进度显示控制器
/// 显示当前关卡位置（如 1-1、1-2），然后自动进入游戏
/// </summary>
public class LevelProgressDisplay : MonoBehaviour
{
    #region 序列化字段

    [Header("关卡配置")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI chapterText;
    [SerializeField] private Image[] levelIndicators;
    [SerializeField] private Image currentLevelHighlight;

    [Header("进度条 (可选)")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("配置")]
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private bool autoStartLevel = true;

    [Header("按钮 (可选)")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;
    [SerializeField] private string saveSceneName = "2_Save";

    #endregion

    #region 私有变量

    private int currentChapter;
    private int currentLevelInChapter;
    private int globalLevelIndex;
    private LevelData currentLevel;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void Start()
    {
        // 从存档读取当前关卡
        LoadCurrentLevel();

        // 显示关卡信息
        DisplayLevelInfo();

        // 自动开始
        if (autoStartLevel)
        {
            StartCoroutine(AutoStartCoroutine());
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartClicked);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    #endregion

    #region 私有方法

    private void LoadCurrentLevel()
    {
        globalLevelIndex = 0;

        // 从存档获取当前关卡
        if (SaveManager.Instance != null)
        {
            GlobalSaveData globalData = SaveManager.Instance.GetGlobalData();
            if (globalData != null)
            {
                globalLevelIndex = globalData.currentLevel;
            }
        }

        // 计算章节和关卡内索引
        CalculateChapterAndLevel(globalLevelIndex);

        Debug.Log($"[LevelProgressDisplay] 当前关卡: 全局索引={globalLevelIndex}, 章节={currentChapter + 1}, 关卡={currentLevelInChapter + 1}");
    }

    private void CalculateChapterAndLevel(int globalIndex)
    {
        if (levelConfig == null || levelConfig.chapters == null)
        {
            currentChapter = 0;
            currentLevelInChapter = globalIndex;
            return;
        }

        int remaining = globalIndex;
        for (int c = 0; c < levelConfig.chapters.Length; c++)
        {
            int levelsInChapter = levelConfig.chapters[c].levels.Length;
            if (remaining < levelsInChapter)
            {
                currentChapter = c;
                currentLevelInChapter = remaining;
                currentLevel = levelConfig.chapters[c].levels[remaining];
                return;
            }
            remaining -= levelsInChapter;
        }

        // 超出范围，使用最后一关
        currentChapter = levelConfig.chapters.Length - 1;
        currentLevelInChapter = levelConfig.chapters[currentChapter].levels.Length - 1;
        currentLevel = levelConfig.chapters[currentChapter].levels[currentLevelInChapter];
    }

    private void DisplayLevelInfo()
    {
        // 显示当前关卡名称 (如 "1-1")
        if (currentLevelText != null)
        {
            if (currentLevel != null)
            {
                currentLevelText.text = currentLevel.displayName;
            }
            else
            {
                currentLevelText.text = $"{currentChapter + 1}-{currentLevelInChapter + 1}";
            }
        }

        // 显示章节名称
        if (chapterText != null && levelConfig != null && currentChapter < levelConfig.chapters.Length)
        {
            chapterText.text = levelConfig.chapters[currentChapter].chapterName;
        }

        // 更新关卡指示器
        UpdateLevelIndicators();

        // 更新进度条
        UpdateProgressBar();
    }

    private void UpdateLevelIndicators()
    {
        if (levelIndicators == null || levelConfig == null) return;

        int levelsInChapter = levelConfig.chapters[currentChapter].levels.Length;

        for (int i = 0; i < levelIndicators.Length; i++)
        {
            if (levelIndicators[i] != null)
            {
                // 显示当前章节内的关卡数量
                levelIndicators[i].gameObject.SetActive(i < levelsInChapter);

                // 高亮当前关卡
                if (i == currentLevelInChapter && currentLevelHighlight != null)
                {
                    currentLevelHighlight.transform.position = levelIndicators[i].transform.position;
                    currentLevelHighlight.gameObject.SetActive(true);
                }
            }
        }
    }

    private void UpdateProgressBar()
    {
        if (levelConfig == null) return;

        int totalLevels = levelConfig.GetTotalLevelCount();
        float progress = (float)(globalLevelIndex + 1) / totalLevels;

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{globalLevelIndex + 1}/{totalLevels}";
        }
    }

    private IEnumerator AutoStartCoroutine()
    {
        yield return new WaitForSeconds(displayDuration);
        StartLevel();
    }

    private void StartLevel()
    {
        if (currentLevel == null || string.IsNullOrEmpty(currentLevel.sceneName))
        {
            Debug.LogWarning("[LevelProgressDisplay] 关卡场景名未配置");
            return;
        }

        Debug.Log($"[LevelProgressDisplay] 开始关卡: {currentLevel.displayName} -> {currentLevel.sceneName}");

        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(currentLevel.sceneName);
        }
    }

    #endregion

    #region 事件处理

    private void OnStartClicked()
    {
        StopAllCoroutines();
        StartLevel();
    }

    private void OnBackClicked()
    {
        StopAllCoroutines();

        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(saveSceneName);
        }
    }

    #endregion
}
