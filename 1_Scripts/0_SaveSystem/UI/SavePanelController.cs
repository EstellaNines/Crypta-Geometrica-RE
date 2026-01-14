using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 存档面板控制器
/// 管理存档槽位的显示、选择和操作
/// </summary>
public class SavePanelController : MonoBehaviour
{
    #region 序列化字段

    [Header("槽位引用")]
    [SerializeField] private SaveSlotUI[] saveSlots;

    [Header("UI 引用")]
    [SerializeField] private Button backButton;

    [Header("配置")]
    [SerializeField] private string gameSceneName = "3_Game";
    [SerializeField] private string startSceneName = "1_Start";

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 初始化槽位
        for (int i = 0; i < saveSlots.Length; i++)
        {
            if (saveSlots[i] != null)
            {
                saveSlots[i].Initialize(i, OnContinueClicked, OnDeleteRequested);
            }
        }

        // 绑定按钮事件
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void OnEnable()
    {
        // 监听保存/加载完成消息
        MessageManager.AddListener<bool, string>(MessageType.SAVE_OPERATION_DONE, OnSaveOperationDone);
        MessageManager.AddListener<bool, string>(MessageType.LOAD_OPERATION_DONE, OnLoadOperationDone);

        // 刷新所有槽位
        RefreshAllSlots();
    }

    private void OnDisable()
    {
        MessageManager.RemoveListener<bool, string>(MessageType.SAVE_OPERATION_DONE, OnSaveOperationDone);
        MessageManager.RemoveListener<bool, string>(MessageType.LOAD_OPERATION_DONE, OnLoadOperationDone);
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 刷新所有槽位显示
    /// </summary>
    public void RefreshAllSlots()
    {
        foreach (var slot in saveSlots)
        {
            if (slot != null)
            {
                slot.Refresh();
            }
        }
    }

    #endregion

    #region 事件处理 - 槽位

    /// <summary>
    /// Continue/Select 按钮点击回调
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    /// <param name="hasData">是否有存档数据</param>
    private void OnContinueClicked(int slotIndex, bool hasData)
    {
        Debug.Log($"[SavePanelController] 槽位 {slotIndex} 点击, 有数据: {hasData}");

        // 设置当前槽位
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrentSlot(slotIndex);
        }

        if (hasData)
        {
            // 有存档：加载游戏
            LoadGame(slotIndex);
        }
        else
        {
            // 无存档：创建新存档并开始游戏
            CreateNewSaveAndStart(slotIndex);
        }
    }

    private void OnDeleteRequested(int slotIndex)
    {
        Debug.Log($"[SavePanelController] 删除槽位: {slotIndex}");

        // 直接删除存档
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave(slotIndex);
        }

        // 刷新槽位
        if (slotIndex < saveSlots.Length && saveSlots[slotIndex] != null)
        {
            saveSlots[slotIndex].Refresh();
        }
    }

    #endregion

    #region 事件处理 - 按钮

    private void OnBackClicked()
    {
        Debug.Log("[SavePanelController] 返回");

        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(startSceneName);
        }
    }

    #endregion

    #region 存档操作

    private void CreateNewSaveAndStart(int slotIndex)
    {
        Debug.Log($"[SavePanelController] 创建新存档: 槽位 {slotIndex}");

        if (SaveManager.Instance != null)
        {
            // 设置当前槽位
            SaveManager.Instance.SetCurrentSlot(slotIndex);

            // 保存初始数据
            SaveManager.Instance.SaveGame(slotIndex);
        }

        // 加载游戏场景
        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(gameSceneName);
        }
    }

    private void LoadGame(int slotIndex)
    {
        Debug.Log($"[SavePanelController] 加载存档并进入游戏: 槽位 {slotIndex}");

        if (SaveManager.Instance != null)
        {
            // 只加载存档数据到内存，不切换场景
            SaveManager.Instance.LoadGameDataOnly(slotIndex);
        }

        // 直接跳转到 LevelProgress 场景
        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(gameSceneName);
        }
    }

    #endregion

    #region 消息处理

    private void OnSaveOperationDone(bool success, string message)
    {
        Debug.Log($"[SavePanelController] 保存完成: {success}, {message}");

        if (success)
        {
            RefreshAllSlots();
        }
    }

    private void OnLoadOperationDone(bool success, string message)
    {
        Debug.Log($"[SavePanelController] 加载完成: {success}, {message}");

        if (!success)
        {
            Debug.LogError($"[SavePanelController] 加载失败: {message}");
        }
    }

    #endregion
}
