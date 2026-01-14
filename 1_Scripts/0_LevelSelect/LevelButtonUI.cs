using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 单个关卡按钮 UI 组件
/// </summary>
public class LevelButtonUI : MonoBehaviour
{
    #region 序列化字段

    [Header("UI 引用")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject completedMark;
    [SerializeField] private GameObject lockedOverlay;

    [Header("样式")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    #endregion

    #region 私有变量

    private int chapterIndex;
    private int levelIndex;
    private LevelData levelData;
    private Action<int, int, LevelData> onLevelSelected;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化关卡按钮
    /// </summary>
    public void Initialize(int chapter, int level, LevelData data, Action<int, int, LevelData> onSelected)
    {
        chapterIndex = chapter;
        levelIndex = level;
        levelData = data;
        onLevelSelected = onSelected;

        UpdateDisplay();
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    public void UpdateDisplay()
    {
        if (levelData == null) return;

        // 显示关卡名称
        if (levelNameText != null)
        {
            levelNameText.text = levelData.displayName;
        }

        // 显示图标
        if (iconImage != null && levelData.icon != null)
        {
            iconImage.sprite = levelData.icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        // 显示完成标记
        if (completedMark != null)
        {
            completedMark.SetActive(levelData.isCompleted);
        }

        // 显示锁定遮罩
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!levelData.isUnlocked);
        }

        // 设置按钮交互状态
        if (button != null)
        {
            button.interactable = levelData.isUnlocked;
        }

        // 设置颜色
        if (levelNameText != null)
        {
            levelNameText.color = levelData.isUnlocked ? unlockedColor : lockedColor;
        }
    }

    #endregion

    #region 事件处理

    private void OnButtonClicked()
    {
        if (levelData != null && levelData.isUnlocked)
        {
            Debug.Log($"[LevelButtonUI] 选择关卡: {levelData.displayName}");
            onLevelSelected?.Invoke(chapterIndex, levelIndex, levelData);
        }
    }

    #endregion
}
