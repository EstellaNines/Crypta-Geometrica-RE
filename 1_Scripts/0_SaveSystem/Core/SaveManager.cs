using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器
/// 作为 GameManager 的子模块，统一管理游戏存档的保存和加载
/// </summary>
public class SaveManager : MonoBehaviour, IGameModule
{
    #region 单例兼容

    private static SaveManager instance;

    /// <summary>
    /// 单例访问
    /// </summary>
    public static SaveManager Instance
    {
        get
        {
            if (instance == null && GameManager.IsInitialized)
            {
                instance = GameManager.Get<SaveManager>();
            }
            return instance;
        }
    }

    #endregion

    #region 配置

    [Header("存档设置")]
    [SerializeField, Tooltip("是否使用加密存档")]
    private bool useEncryption = false;

    [SerializeField, Tooltip("最大存档槽位数 (不含测试槽)")]
    private int maxSlots = 3;

    [SerializeField, Tooltip("当前使用的存档槽位")]
    private int currentSlotIndex = 0;

    [Header("自动保存")]
    [SerializeField, Tooltip("是否启用自动保存")]
    private bool autoSaveEnabled = true;

    #endregion

    #region 状态

    /// <summary>已注册的可保存对象列表</summary>
    private List<ISaveable> saveables = new List<ISaveable>();

    /// <summary>已注册的 PCG 可保存对象</summary>
    private IPCGSaveable pcgSaveable;

    /// <summary>当前内存中的存档数据</summary>
    private SaveData currentSaveData;

    /// <summary>是否正在执行保存/加载操作</summary>
    private bool isOperating = false;

    /// <summary>等待恢复数据的标志</summary>
    private bool pendingRestore = false;

    /// <summary>游戏开始时间 (用于计算游玩时长)</summary>
    private float sessionStartTime;

    /// <summary>累计游玩时间</summary>
    private float accumulatedPlayTime;

    #endregion

    #region 属性

    /// <summary>是否使用加密</summary>
    public bool UseEncryption => useEncryption;

    /// <summary>当前槽位索引</summary>
    public int CurrentSlotIndex => currentSlotIndex;

    /// <summary>是否有待处理的恢复操作</summary>
    public bool HasPendingRestore => pendingRestore;

    /// <summary>当前存档数据 (只读)</summary>
    public SaveData CurrentData => currentSaveData;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // 立即标记永久消息，防止被 MessageManagerHelper 在场景切换时清理
        MessageManager.MarkAsPermanent(MessageType.SAVE_GAME_REQUEST);
        MessageManager.MarkAsPermanent(MessageType.LOAD_GAME_REQUEST);
        MessageManager.MarkAsPermanent(MessageType.SAVE_OPERATION_DONE);
        MessageManager.MarkAsPermanent(MessageType.LOAD_OPERATION_DONE);
    }

    #endregion

    #region IGameModule 实现

    /// <summary>
    /// 模块初始化
    /// </summary>
    public void OnInit()
    {
        sessionStartTime = Time.time;
        currentSaveData = new SaveData();

        // 注册消息监听
        RegisterMessages();

        // 永久消息标记已在 Awake 中完成
        Debug.Log("[SaveManager] 模块初始化完成");
    }

    /// <summary>
    /// 模块轮询
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        // 当前无需轮询逻辑
    }

    /// <summary>
    /// 模块销毁
    /// </summary>
    public void OnDispose()
    {
        UnregisterMessages();
        saveables.Clear();
        pcgSaveable = null;

        if (instance == this)
        {
            instance = null;
        }

        Debug.Log("[SaveManager] 模块已销毁");
    }

    #endregion

    #region 消息注册

    private void RegisterMessages()
    {
        MessageManager.AddListener<int>(MessageType.SAVE_GAME_REQUEST, OnSaveGameRequest);
        MessageManager.AddListener<int>(MessageType.LOAD_GAME_REQUEST, OnLoadGameRequest);
        MessageManager.AddListener<string>(MessageType.SCENE_LOADING_COMPLETED, OnSceneLoadCompleted);
        MessageManager.AddListener<string>(MessageType.LEVEL_ENTERED, OnLevelEntered);
        MessageManager.AddListener<string>(MessageType.LEVEL_COMPLETED, OnLevelCompleted);
    }

    private void UnregisterMessages()
    {
        MessageManager.RemoveListener<int>(MessageType.SAVE_GAME_REQUEST, OnSaveGameRequest);
        MessageManager.RemoveListener<int>(MessageType.LOAD_GAME_REQUEST, OnLoadGameRequest);
        MessageManager.RemoveListener<string>(MessageType.SCENE_LOADING_COMPLETED, OnSceneLoadCompleted);
        MessageManager.RemoveListener<string>(MessageType.LEVEL_ENTERED, OnLevelEntered);
        MessageManager.RemoveListener<string>(MessageType.LEVEL_COMPLETED, OnLevelCompleted);
    }

    #endregion

    #region 消息处理

    private void OnSaveGameRequest(int slotIndex)
    {
        SaveGame(slotIndex);
    }

    private void OnLoadGameRequest(int slotIndex)
    {
        LoadGame(slotIndex);
    }

    private void OnSceneLoadCompleted(string sceneName)
    {
        if (pendingRestore)
        {
            // 延迟一帧执行恢复，确保场景中的对象已完成初始化
            StartCoroutine(DelayedRestore());
        }
    }

    private void OnLevelEntered(string levelName)
    {
        if (autoSaveEnabled)
        {
            Debug.Log($"[SaveManager] 进入关卡 {levelName}，触发自动保存");
            AutoSave();
        }
    }

    private void OnLevelCompleted(string levelName)
    {
        if (autoSaveEnabled)
        {
            Debug.Log($"[SaveManager] 通关关卡 {levelName}，触发自动保存");
            AutoSave();
        }
    }

    private IEnumerator DelayedRestore()
    {
        yield return null; // 等待一帧

        RestoreAllEntities();
        pendingRestore = false;
    }

    #endregion

    #region 公共 API - 注册

    /// <summary>
    /// 注册可保存对象
    /// </summary>
    public void RegisterSaveable(ISaveable saveable)
    {
        if (saveable == null) return;

        if (!saveables.Contains(saveable))
        {
            saveables.Add(saveable);
            Debug.Log($"[SaveManager] 注册对象: {saveable.SaveID}");
        }
    }

    /// <summary>
    /// 取消注册可保存对象
    /// </summary>
    public void UnregisterSaveable(ISaveable saveable)
    {
        if (saveable == null) return;

        if (saveables.Contains(saveable))
        {
            saveables.Remove(saveable);
            Debug.Log($"[SaveManager] 取消注册: {saveable.SaveID}");
        }
    }

    /// <summary>
    /// 注册 PCG 可保存对象
    /// </summary>
    public void RegisterPCGSaveable(IPCGSaveable saveable)
    {
        pcgSaveable = saveable;
        Debug.Log("[SaveManager] 注册 PCG 对象");
    }

    /// <summary>
    /// 取消注册 PCG 可保存对象
    /// </summary>
    public void UnregisterPCGSaveable()
    {
        pcgSaveable = null;
        Debug.Log("[SaveManager] 取消注册 PCG 对象");
    }

    #endregion

    #region 公共 API - 保存加载

    /// <summary>
    /// 保存游戏到指定槽位
    /// </summary>
    /// <param name="slotIndex">槽位索引 (0-2 为正式槽位, -1 为测试槽位)</param>
    public void SaveGame(int slotIndex)
    {
        if (isOperating)
        {
            Debug.LogWarning("[SaveManager] 正在执行操作，忽略保存请求");
            return;
        }

        StartCoroutine(SaveGameCoroutine(slotIndex));
    }

    /// <summary>
    /// 从指定槽位加载游戏
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    public void LoadGame(int slotIndex)
    {
        if (isOperating)
        {
            Debug.LogWarning("[SaveManager] 正在执行操作，忽略加载请求");
            return;
        }

        StartCoroutine(LoadGameCoroutine(slotIndex));
    }

    /// <summary>
    /// 只加载存档数据到内存，不切换场景
    /// 用于存档选择界面加载存档后手动跳转场景
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    /// <returns>是否加载成功</returns>
    public bool LoadGameDataOnly(int slotIndex)
    {
        try
        {
            string path = SaveUtility.GetSavePath(slotIndex, useEncryption);
            if (!System.IO.File.Exists(path))
            {
                path = SaveUtility.GetSavePath(slotIndex, !useEncryption);
            }

            SaveData data = SaveUtility.LoadFromFile(path, path.EndsWith(".crypta"));

            if (data == null)
            {
                Debug.LogWarning($"[SaveManager] 槽位 {slotIndex} 数据为空或损坏");
                return false;
            }

            currentSaveData = data;
            currentSlotIndex = slotIndex;
            accumulatedPlayTime = data.header.playTime;
            sessionStartTime = Time.time;

            Debug.Log($"[SaveManager] 存档数据已加载到内存: 槽位 {slotIndex}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 加载存档数据失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 自动保存到当前槽位
    /// </summary>
    public void AutoSave()
    {
        if (currentSlotIndex < 0)
        {
            Debug.LogWarning("[SaveManager] 未设置当前槽位，无法自动保存");
            return;
        }

        MessageManager.Broadcast<int>(MessageType.AUTO_SAVE_TRIGGERED, currentSlotIndex);
        SaveGame(currentSlotIndex);
    }

    /// <summary>
    /// 获取槽位头信息
    /// </summary>
    public SaveHeader GetSlotHeader(int slotIndex)
    {
        string path = SaveUtility.GetSavePath(slotIndex, useEncryption);
        return SaveUtility.LoadHeaderOnly(path, useEncryption);
    }

    /// <summary>
    /// 检查槽位是否有存档
    /// </summary>
    public bool HasSaveData(int slotIndex)
    {
        return SaveUtility.HasSaveData(slotIndex);
    }

    /// <summary>
    /// 删除指定槽位的存档
    /// </summary>
    public bool DeleteSave(int slotIndex)
    {
        string jsonPath = SaveUtility.GetSavePath(slotIndex, false);
        string cryptaPath = SaveUtility.GetSavePath(slotIndex, true);

        bool deleted = false;
        deleted |= SaveUtility.DeleteSave(jsonPath);
        deleted |= SaveUtility.DeleteSave(cryptaPath);

        return deleted;
    }

    /// <summary>
    /// 设置当前槽位
    /// </summary>
    public void SetCurrentSlot(int slotIndex)
    {
        currentSlotIndex = slotIndex;
    }

    /// <summary>
    /// 设置是否使用加密
    /// </summary>
    public void SetUseEncryption(bool encrypt)
    {
        useEncryption = encrypt;
    }

    #endregion

    #region 内部方法 - 保存

    private IEnumerator SaveGameCoroutine(int slotIndex)
    {
        isOperating = true;
        bool success = false;
        string message = "";

        try
        {
            // 1. 收集所有数据
            SaveData data = CollectSaveData(slotIndex);

            // 2. 保存到文件
            string path = SaveUtility.GetSavePath(slotIndex, useEncryption);
            SaveUtility.SaveToFile(path, data, useEncryption);

            // 3. 更新当前数据缓存
            currentSaveData = data;
            currentSlotIndex = slotIndex;

            success = true;
            message = $"保存成功: 槽位 {slotIndex}";
            Debug.Log($"[SaveManager] {message}");
        }
        catch (Exception e)
        {
            success = false;
            message = $"保存失败: {e.Message}";
            Debug.LogError($"[SaveManager] {message}");
        }

        isOperating = false;

        // 广播完成消息
        MessageManager.Broadcast<bool, string>(MessageType.SAVE_OPERATION_DONE, success, message);

        yield return null;
    }

    private SaveData CollectSaveData(int slotIndex)
    {
        SaveData data = new SaveData();

        // 头信息
        data.header.version = SaveUtility.SAVE_VERSION;
        data.header.timestamp = DateTime.Now.ToString("O");
        data.header.playTime = accumulatedPlayTime + (Time.time - sessionStartTime);
        data.header.sceneName = SceneManager.GetActiveScene().name;
        data.header.slotIndex = slotIndex;

        // 全局数据 (如果有现有数据则保留)
        if (currentSaveData != null && currentSaveData.globalData != null)
        {
            data.globalData = currentSaveData.globalData;
        }

        // PCG 数据
        if (pcgSaveable != null)
        {
            data.pcgData = pcgSaveable.CapturePCGState();
        }

        // 实体数据
        foreach (var saveable in saveables)
        {
            try
            {
                object state = saveable.CaptureState();
                if (state != null)
                {
                    string json = JsonUtility.ToJson(state);
                    data.entityData[saveable.SaveID] = json;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 捕获状态失败 {saveable.SaveID}: {e.Message}");
            }
        }

        return data;
    }

    #endregion

    #region 内部方法 - 加载

    private IEnumerator LoadGameCoroutine(int slotIndex)
    {
        isOperating = true;
        bool success = false;
        string message = "";

        try
        {
            // 1. 读取文件
            string path = SaveUtility.GetSavePath(slotIndex, useEncryption);

            // 如果加密文件不存在，尝试读取 JSON 文件
            if (!System.IO.File.Exists(path))
            {
                path = SaveUtility.GetSavePath(slotIndex, !useEncryption);
            }

            SaveData data = SaveUtility.LoadFromFile(path, path.EndsWith(".crypta"));

            if (data == null)
            {
                throw new Exception("存档数据为空或损坏");
            }

            // 2. 更新当前数据
            currentSaveData = data;
            currentSlotIndex = slotIndex;
            accumulatedPlayTime = data.header.playTime;
            sessionStartTime = Time.time;

            // 3. 检查是否需要切换场景
            string currentScene = SceneManager.GetActiveScene().name;
            string targetScene = data.header.sceneName;

            if (currentScene != targetScene)
            {
                // 标记待恢复
                pendingRestore = true;

                // 请求场景切换
                if (GameManager.TryGet<AsyncSceneManager>(out var sceneManager))
                {
                    sceneManager.LoadScene(targetScene);
                }
                else
                {
                    SceneManager.LoadScene(targetScene);
                }
            }
            else
            {
                // 直接恢复数据
                RestoreAllEntities();
            }

            success = true;
            message = $"加载成功: 槽位 {slotIndex}";
            Debug.Log($"[SaveManager] {message}");
        }
        catch (Exception e)
        {
            success = false;
            message = $"加载失败: {e.Message}";
            Debug.LogError($"[SaveManager] {message}");
        }

        isOperating = false;

        // 广播完成消息
        MessageManager.Broadcast<bool, string>(MessageType.LOAD_OPERATION_DONE, success, message);

        yield return null;
    }

    private void RestoreAllEntities()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("[SaveManager] 无数据可恢复");
            return;
        }

        // 恢复 PCG 数据
        if (pcgSaveable != null && currentSaveData.pcgData != null)
        {
            try
            {
                pcgSaveable.RestorePCGState(currentSaveData.pcgData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 恢复 PCG 数据失败: {e.Message}");
            }
        }

        // 恢复实体数据
        foreach (var saveable in saveables)
        {
            if (currentSaveData.entityData.TryGetValue(saveable.SaveID, out string json))
            {
                try
                {
                    saveable.RestoreState(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] 恢复状态失败 {saveable.SaveID}: {e.Message}");
                }
            }
        }

        Debug.Log($"[SaveManager] 数据恢复完成，共 {saveables.Count} 个实体");
    }

    #endregion

    #region 公共 API - 全局数据

    /// <summary>
    /// 更新全局数据
    /// </summary>
    public void UpdateGlobalData(GlobalSaveData data)
    {
        if (currentSaveData != null)
        {
            currentSaveData.globalData = data;
        }
    }

    /// <summary>
    /// 获取全局数据
    /// </summary>
    public GlobalSaveData GetGlobalData()
    {
        return currentSaveData?.globalData;
    }

    /// <summary>
    /// 获取指定槽位的完整存档数据（用于 UI 显示）
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    /// <returns>存档数据，不存在返回 null</returns>
    public SaveData GetSaveData(int slotIndex)
    {
        if (!HasSaveData(slotIndex)) return null;

        try
        {
            string path = SaveUtility.GetSavePath(slotIndex, useEncryption);
            if (!System.IO.File.Exists(path))
            {
                path = SaveUtility.GetSavePath(slotIndex, !useEncryption);
            }

            return SaveUtility.LoadFromFile(path, path.EndsWith(".crypta"));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 读取槽位 {slotIndex} 数据失败: {e.Message}");
            return null;
        }
    }

    #endregion

    #region 调试

    /// <summary>
    /// 打印已注册的可保存对象
    /// </summary>
    [ContextMenu("Print Registered Saveables")]
    public void PrintRegisteredSaveables()
    {
        Debug.Log("========== SaveManager 已注册对象 ==========");
        for (int i = 0; i < saveables.Count; i++)
        {
            Debug.Log($"  [{i}] {saveables[i].SaveID}");
        }
        Debug.Log($"PCG 对象: {(pcgSaveable != null ? "已注册" : "未注册")}");
        Debug.Log($"总计: {saveables.Count} 个实体");
        Debug.Log("=============================================");
    }

    #endregion
}
