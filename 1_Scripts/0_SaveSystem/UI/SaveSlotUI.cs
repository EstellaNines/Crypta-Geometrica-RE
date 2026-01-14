using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 存档槽位 UI 组件
/// 用于显示存档信息并提供保存/加载/删除功能
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    #region 序列化字段

    [Header("槽位配置")]
    [SerializeField, Tooltip("槽位索引 (0-2)")]
    private int slotIndex = 0;

    [Header("UI 引用 - 信息显示")]
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI playTimeText;
    [SerializeField] private TextMeshProUGUI saveTimeText;

    [Header("UI 引用 - 头像")]
    [SerializeField] private Image headIcon;

    [Header("UI 引用 - 生命值")]
    [SerializeField] private GameObject heartCountContainer;
    [SerializeField] private Image[] heartIcons;

    [Header("UI 引用 - 按钮")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private Button deleteButton;

    [Header("按钮文本配置")]
    [SerializeField] private string selectText = "SELECT";
    [SerializeField] private string continueText = "CONTINUE";

    #endregion

    #region 私有变量

    private SaveHeader cachedHeader;
    private SaveData cachedSaveData;
    private bool hasData;
    private Action<int, bool> onContinueClicked;
    private Action<int> onDeleteClicked;

    #endregion

    #region 属性

    /// <summary>槽位索引</summary>
    public int SlotIndex => slotIndex;

    /// <summary>是否有存档数据</summary>
    public bool HasData => hasData;

    /// <summary>缓存的存档头信息</summary>
    public SaveHeader Header => cachedHeader;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 绑定按钮事件
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
    }

    private void OnEnable()
    {
        // 刷新显示
        Refresh();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化槽位
    /// </summary>
    /// <param name="index">槽位索引</param>
    /// <param name="onContinue">Continue/Select 按钮回调 (slotIndex, hasData)</param>
    /// <param name="onDelete">删除回调</param>
    public void Initialize(int index, Action<int, bool> onContinue = null, Action<int> onDelete = null)
    {
        slotIndex = index;
        onContinueClicked = onContinue;
        onDeleteClicked = onDelete;
        Refresh();
    }

    /// <summary>
    /// 刷新槽位显示
    /// </summary>
    public void Refresh()
    {
        // 检查是否有存档数据
        hasData = SaveManager.Instance != null && SaveManager.Instance.HasSaveData(slotIndex);

        // 显示槽位序号
        if (slotNumberText != null)
        {
            slotNumberText.text = (slotIndex + 1).ToString();
        }

        if (hasData)
        {
            // 获取存档头信息
            cachedHeader = SaveManager.Instance.GetSlotHeader(slotIndex);
            cachedSaveData = SaveManager.Instance.GetSaveData(slotIndex);
            ShowSaveInfo();
        }
        else
        {
            cachedHeader = null;
            cachedSaveData = null;
            ShowEmptySlot();
        }

        // 更新删除按钮状态 - 只有存在存档时才显示
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(hasData);
        }

        // 更新 Continue 按钮文本
        UpdateContinueButtonText();
    }

    /// <summary>
    /// 设置头像显示状态
    /// </summary>
    public void SetHeadIconVisible(bool visible)
    {
        if (headIcon != null)
        {
            headIcon.gameObject.SetActive(visible);
        }
    }

    #endregion

    #region 私有方法 - UI 更新

    private void ShowSaveInfo()
    {
        if (cachedHeader != null)
        {
            // 显示游玩时间
            if (playTimeText != null)
            {
                playTimeText.text = cachedHeader.GetFormattedPlayTime();
                playTimeText.gameObject.SetActive(true);
            }

            // 显示存档时间
            if (saveTimeText != null)
            {
                saveTimeText.text = cachedHeader.GetFormattedTime();
                saveTimeText.gameObject.SetActive(true);
            }
        }

        // 显示头像（有存档时显示）
        if (headIcon != null)
        {
            headIcon.gameObject.SetActive(true);
        }

        // 显示生命值
        UpdateHeartDisplay();
    }

    private void ShowEmptySlot()
    {
        // 隐藏或清空游玩时间
        if (playTimeText != null)
        {
            playTimeText.text = "NEW TEXT";
            playTimeText.gameObject.SetActive(true);
        }

        // 隐藏或清空存档时间
        if (saveTimeText != null)
        {
            saveTimeText.text = "NEW TEXT";
            saveTimeText.gameObject.SetActive(true);
        }

        // 隐藏头像（空槽位不显示）
        if (headIcon != null)
        {
            headIcon.gameObject.SetActive(false);
        }

        // 隐藏生命值
        if (heartCountContainer != null)
        {
            heartCountContainer.SetActive(false);
        }
    }

    private void UpdateContinueButtonText()
    {
        if (continueButtonText != null)
        {
            continueButtonText.text = hasData ? continueText : selectText;
        }
    }

    private void UpdateHeartDisplay()
    {
        if (heartCountContainer == null || heartIcons == null) return;

        // 获取当前生命值（从存档数据中）
        int currentHearts = 3; // 默认最大值

        if (cachedSaveData != null && cachedSaveData.globalData != null)
        {
            // 尝试从全局数据获取生命值
            // 假设 GlobalSaveData 有 currentHearts 字段
            // 如果没有，需要在 GlobalSaveData 中添加
            currentHearts = Mathf.Clamp(cachedSaveData.globalData.currentHearts, 0, heartIcons.Length);
        }

        heartCountContainer.SetActive(true);

        // 显示/隐藏生命图标
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                heartIcons[i].gameObject.SetActive(i < currentHearts);
            }
        }
    }

    #endregion

    #region 事件处理

    private void OnContinueButtonClicked()
    {
        Debug.Log($"[SaveSlotUI] 槽位 {slotIndex} Continue/Select 按钮点击, 有数据: {hasData}");
        onContinueClicked?.Invoke(slotIndex, hasData);
    }

    private void OnDeleteButtonClicked()
    {
        Debug.Log($"[SaveSlotUI] 请求删除槽位 {slotIndex}");
        onDeleteClicked?.Invoke(slotIndex);
    }

    #endregion
}
