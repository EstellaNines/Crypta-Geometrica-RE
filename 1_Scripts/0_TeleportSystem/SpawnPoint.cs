using UnityEngine;

/// <summary>
/// 出生点组件
/// 标记玩家出生/重生的位置
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    #region 序列化字段

    [Header("配置")]
    [SerializeField] private bool isDefaultSpawn = true;

    [Header("Gizmo 显示")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoRadius = 0.5f;

    #endregion

    #region 生命周期

    private void Start()
    {
        // 自动注册到 TeleportManager
        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.RegisterSpawnPoint(this);
        }
        else
        {
            Debug.LogWarning($"[SpawnPoint] TeleportManager 不存在，无法注册: {name}");
        }
    }

    private void OnDestroy()
    {
        // 取消注册
        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.UnregisterSpawnPoint(this);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取出生位置
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// 获取出生旋转
    /// </summary>
    public Quaternion GetSpawnRotation()
    {
        return transform.rotation;
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 是否为默认出生点
    /// </summary>
    public bool IsDefaultSpawn => isDefaultSpawn;

    #endregion

    #region Editor Gizmo

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        // 绘制方向指示
        Gizmos.DrawLine(transform.position, transform.position + transform.right * gizmoRadius * 1.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius * 0.3f);
    }

    #endregion
}
