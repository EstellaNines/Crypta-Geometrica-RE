using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 传送点管理器
/// 管理场景中的出生点和传送点
/// </summary>
public class TeleportManager : MonoBehaviour, IGameModule
{
    #region 单例

    private static TeleportManager instance;
    public static TeleportManager Instance => instance;

    #endregion

    #region 序列化字段

    [Header("配置")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool saveBeforeTeleport = true;

    #endregion

    #region 私有变量

    private SpawnPoint currentSpawnPoint;
    private List<TeleportPoint> teleportPoints = new List<TeleportPoint>();
    private Transform playerTransform;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[TeleportManager] 检测到重复实例，销毁自身");
            Destroy(this);
            return;
        }
        instance = this;
    }

    #endregion

    #region IGameModule 实现

    public void OnInit()
    {
        Debug.Log("[TeleportManager] 模块初始化完成");
    }

    public void OnUpdate(float deltaTime)
    {
        // 传送点逻辑由各自组件处理
    }

    public void OnDispose()
    {
        currentSpawnPoint = null;
        teleportPoints.Clear();
        playerTransform = null;

        if (instance == this)
        {
            instance = null;
        }

        Debug.Log("[TeleportManager] 模块已销毁");
    }

    #endregion

    #region 公共 API - 注册

    /// <summary>
    /// 注册出生点
    /// </summary>
    public void RegisterSpawnPoint(SpawnPoint point)
    {
        if (point == null) return;

        // 如果是默认出生点或当前没有出生点，则设置为当前出生点
        if (point.IsDefaultSpawn || currentSpawnPoint == null)
        {
            currentSpawnPoint = point;
            Debug.Log($"[TeleportManager] 设置出生点: {point.name}");
        }
    }

    /// <summary>
    /// 取消注册出生点
    /// </summary>
    public void UnregisterSpawnPoint(SpawnPoint point)
    {
        if (currentSpawnPoint == point)
        {
            currentSpawnPoint = null;
        }
    }

    /// <summary>
    /// 注册传送点
    /// </summary>
    public void RegisterTeleportPoint(TeleportPoint point)
    {
        if (point != null && !teleportPoints.Contains(point))
        {
            teleportPoints.Add(point);
            Debug.Log($"[TeleportManager] 注册传送点: {point.name}");
        }
    }

    /// <summary>
    /// 取消注册传送点
    /// </summary>
    public void UnregisterTeleportPoint(TeleportPoint point)
    {
        if (teleportPoints.Contains(point))
        {
            teleportPoints.Remove(point);
        }
    }

    #endregion

    #region 公共 API - 玩家

    /// <summary>
    /// 设置玩家引用
    /// </summary>
    public void SetPlayer(Transform player)
    {
        playerTransform = player;
        Debug.Log($"[TeleportManager] 设置玩家: {player?.name}");
    }

    /// <summary>
    /// 查找并设置玩家
    /// </summary>
    public void FindAndSetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log($"[TeleportManager] 找到玩家: {playerObj.name}");
        }
        else
        {
            Debug.LogWarning($"[TeleportManager] 未找到标签为 '{playerTag}' 的玩家");
        }
    }

    /// <summary>
    /// 获取玩家 Transform
    /// </summary>
    public Transform GetPlayer()
    {
        if (playerTransform == null)
        {
            FindAndSetPlayer();
        }
        return playerTransform;
    }

    #endregion

    #region 公共 API - 传送

    /// <summary>
    /// 将玩家传送到出生点
    /// </summary>
    public void SpawnPlayer()
    {
        if (currentSpawnPoint == null)
        {
            Debug.LogWarning("[TeleportManager] 没有设置出生点");
            return;
        }

        Transform player = GetPlayer();
        if (player == null)
        {
            Debug.LogWarning("[TeleportManager] 未找到玩家");
            return;
        }

        player.position = currentSpawnPoint.GetSpawnPosition();
        Debug.Log($"[TeleportManager] 玩家已传送到出生点: {currentSpawnPoint.name}");
    }

    /// <summary>
    /// 玩家重生
    /// </summary>
    public void RespawnPlayer()
    {
        SpawnPlayer();
    }

    /// <summary>
    /// 传送到目标场景
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    /// <param name="updateLevel">是否更新关卡进度</param>
    public void TeleportToScene(string sceneName, bool updateLevel = true)
    {
        Debug.Log($"[TeleportManager] 传送到场景: {sceneName}");

        // 传送前保存进度
        if (saveBeforeTeleport && SaveManager.Instance != null)
        {
            // 更新关卡进度
            if (updateLevel)
            {
                GlobalSaveData globalData = SaveManager.Instance.GetGlobalData();
                if (globalData != null)
                {
                    globalData.currentLevel++;
                    SaveManager.Instance.UpdateGlobalData(globalData);
                    Debug.Log($"[TeleportManager] 关卡进度更新: {globalData.currentLevel}");
                }
            }

            // 保存游戏
            int slotIndex = SaveManager.Instance.CurrentSlotIndex;
            if (slotIndex >= 0)
            {
                SaveManager.Instance.SaveGame(slotIndex);
                Debug.Log($"[TeleportManager] 已保存到槽位: {slotIndex}");
            }
        }

        // 加载目标场景
        if (AsyncSceneManager.Instance != null)
        {
            AsyncSceneManager.Instance.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("[TeleportManager] AsyncSceneManager 不存在");
        }
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 当前出生点
    /// </summary>
    public SpawnPoint CurrentSpawnPoint => currentSpawnPoint;

    /// <summary>
    /// 是否在传送前保存
    /// </summary>
    public bool SaveBeforeTeleport
    {
        get => saveBeforeTeleport;
        set => saveBeforeTeleport = value;
    }

    #endregion
}
