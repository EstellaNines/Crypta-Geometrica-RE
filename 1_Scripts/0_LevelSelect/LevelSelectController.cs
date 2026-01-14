using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 关卡选择场景控制器
/// 管理关卡列表的显示和选择
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    #region 序列化字段

    [Header("配置")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("UI 引用 - 关卡容器")]
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private LevelButtonUI levelButtonPrefab;

    [Header("UI 引用 - 章节")]
    [SerializeField] private TextMeshProUGUI chapterTitleText;
    [SerializeField] private Button prevChapterButton;
    [SerializeField] private Button nextChapterButton;

    [Header("UI 引用 - 导航")]
    [SerializeField] private Button backButton;

    [Header("配置")]
    [SerializeField] private string saveSceneName = "2_Save";

    #endregion

    #region 私有变量

    private int currentChapterIndex = 0;
    private LevelButtonUI[] spawnedButtons;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 绑定按钮事件
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (prevChapterButton != null)
        {
            prevChapterButton.onClick.AddListener(OnPrevChapter);
        }

        if (nextChapterButton != null)
        {
            nextChapterButton.onClick.AddListener(OnNextChapter);
        }
    }

    private void Start()
    {
        // 从存档读取当前进度
        LoadProgressFromSave();

        // 显示当前章节
        DisplayChapter(currentChapterIndex);
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);

        if (prevChapterButton != null)
            prevChapterButton.onClick.RemoveListener(OnPrevChapter);

        if (nextChapterButton != null)
            nextChapterButton.onClick.RemoveListener(OnNextChapter);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 显示指定章节
    /// </summary>
    public void DisplayChapter(int chapterIndex)
    {
        if (levelConfig == null || levelConfig.chapters == null) return;
        if (chapterIndex < 0 || chapterIndex >= levelConfig.chapters.Length) return;

        currentChapterIndex = chapterIndex;
        ChapterData chapter = levelConfig.chapters[chapterIndex];

        // 更新章节标题
        if (chapterTitleText != null)
        {
            chapterTitleText.text = chapter.chapterName;
        }

        // 清除旧按钮
        ClearLevelButtons();

        // 生成新按钮
        SpawnLevelButtons(chapter);

        // 更新章节切换按钮状态
        UpdateChapterNavigation();
    }

    /// <summary>
    /// 刷新当前章节显示
    /// </summary>
    public void RefreshCurrentChapter()
    {
        DisplayChapter(currentChapterIndex);
    }

    #endregion

    #region 私有方法 - UI

    private void ClearLevelButtons()
    {
        if (spawnedButtons != null)
        {
            foreach (var btn in spawnedButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
        }
        spawnedButtons = null;
    }

    private void SpawnLevelButtons(ChapterData chapter)
    {
        if (levelButtonContainer == null || levelButtonPrefab == null) return;

        spawnedButtons = new LevelButtonUI[chapter.levels.Length];

        for (int i = 0; i < chapter.levels.Length; i++)
        {
            LevelButtonUI btn = Instantiate(levelButtonPrefab, levelButtonContainer);
            btn.Initialize(currentChapterIndex, i, chapter.levels[i], OnLevelSelected);
            spawnedButtons[i] = btn;
        }
    }

    private void UpdateChapterNavigation()
    {
        if (levelConfig == null) return;

        int chapterCount = levelConfig.chapters.Length;

        if (prevChapterButton != null)
        {
            prevChapterButton.interactable = currentChapterIndex > 0;
        }

        if (nextChapterButton != null)
        {
            nextChapterButton.interactable = currentChapterIndex < chapterCount - 1;
        }
    }

    #endregion

    #region 私有方法 - 存档交互

    private void LoadProgressFromSave()
    {
        if (SaveManager.Instance == null || levelConfig == null) return;

        GlobalSaveData globalData = SaveManager.Instance.GetGlobalData();
        if (globalData == null) return;

        int unlockedLevel = globalData.currentLevel;

        // 根据存档进度解锁关卡
        int globalIndex = 0;
        for (int c = 0; c < levelConfig.chapters.Length; c++)
        {
            for (int l = 0; l < levelConfig.chapters[c].levels.Length; l++)
            {
                LevelData level = levelConfig.chapters[c].levels[l];
                level.isUnlocked = globalIndex <= unlockedLevel;
                level.isCompleted = globalIndex < unlockedLevel;
                globalIndex++;
            }
        }

        Debug.Log($"[LevelSelectController] 已解锁关卡: {unlockedLevel + 1}");
    }

    private void SaveProgressToSave(int globalLevelIndex)
    {
        if (SaveManager.Instance == null) return;

        GlobalSaveData globalData = SaveManager.Instance.GetGlobalData();
        if (globalData == null)
        {
            globalData = new GlobalSaveData();
        }

        globalData.currentLevel = globalLevelIndex;
        SaveManager.Instance.UpdateGlobalData(globalData);
    }

    #endregion

    #region 事件处理

    private void OnLevelSelected(int chapterIndex, int levelIndex, LevelData levelData)
    {
        Debug.Log($"[LevelSelectController] 选择关卡: Chapter {chapterIndex + 1}, Level {levelIndex + 1} ({levelData.displayName})");

        // 更新存档中的当前关卡
        int globalIndex = levelConfig.GetGlobalIndex(chapterIndex, levelIndex);
        SaveProgressToSave(globalIndex);

        // 保存游戏
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame(SaveManager.Instance.CurrentSlotIndex);
        }

        // 加载关卡场景
        if (AsyncSceneManager.Instance != null && !string.IsNullOrEmpty(levelData.sceneName))
        {
            AsyncSceneManager.Instance.LoadScene(levelData.sceneName);
        }
    }

    private void OnBackClicked()
    {
        Debug.Log("[LevelSelectController] 返回");

        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(saveSceneName);
        }
    }

    private void OnPrevChapter()
    {
        if (currentChapterIndex > 0)
        {
            DisplayChapter(currentChapterIndex - 1);
        }
    }

    private void OnNextChapter()
    {
        if (levelConfig != null && currentChapterIndex < levelConfig.chapters.Length - 1)
        {
            DisplayChapter(currentChapterIndex + 1);
        }
    }

    #endregion
}
