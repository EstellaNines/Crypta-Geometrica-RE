using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// 单向平台穿透组件
/// 允许玩家从下方跳上平台，按S键从平台上下落
/// </summary>
public class PlatformDropthrough : MonoBehaviour
{
    #region 配置参数

    [Header("平台配置")]
    [Tooltip("平台所在的Layer")]
    [SerializeField] private LayerMask platformLayer;

    [Tooltip("下落禁用碰撞的持续时间（秒）")]
    [SerializeField] private float dropDuration = 0.3f;

    [Header("检测配置")]
    [Tooltip("检测平台的射线长度")]
    [SerializeField] private float platformCheckDistance = 0.2f;

    #endregion

    #region 组件引用

    private PlayerController playerController;
    private Collider2D playerCollider;
    private CancellationTokenSource dropCts;

    #endregion

    #region 状态

    private bool isDropping = false;

    #endregion

    #region 生命周期

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCollider = GetComponent<Collider2D>();
    }

    private void OnDestroy()
    {
        dropCts?.Cancel();
        dropCts?.Dispose();
    }

    private void Update()
    {
        // 检测下落输入（S键或下箭头）
        if (!isDropping && CheckDropInput() && IsOnPlatform())
        {
            DropThroughPlatformAsync().Forget();
        }
    }

    #endregion

    #region 平台穿透逻辑

    /// <summary>
    /// 检测下落输入
    /// </summary>
    private bool CheckDropInput()
    {
        if (playerController == null) return false;
        return playerController.MoveInput.y < -0.5f;
    }

    /// <summary>
    /// 检测是否站在平台上
    /// </summary>
    private bool IsOnPlatform()
    {
        if (playerCollider == null) return false;

        // 从玩家底部向下发射射线检测平台
        Vector2 origin = (Vector2)transform.position + Vector2.down * (playerCollider.bounds.extents.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, platformCheckDistance, platformLayer);

        return hit.collider != null;
    }

    /// <summary>
    /// 异步执行平台穿透
    /// 等待玩家完全离开平台区域后再恢复碰撞，避免卡在平台里
    /// </summary>
    private async UniTaskVoid DropThroughPlatformAsync()
    {
        if (isDropping) return;
        isDropping = true;

        // 取消之前的任务
        dropCts?.Cancel();
        dropCts?.Dispose();
        dropCts = new CancellationTokenSource();

        Collider2D platformCollider = null;

        try
        {
            // 获取当前站立的平台碰撞体
            Vector2 origin = (Vector2)transform.position + Vector2.down * (playerCollider.bounds.extents.y);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, platformCheckDistance, platformLayer);

            if (hit.collider != null)
            {
                platformCollider = hit.collider;
                
                // 临时禁用与该平台的碰撞
                Physics2D.IgnoreCollision(playerCollider, platformCollider, true);

                // 等待玩家完全离开平台区域（检测玩家是否还在平台碰撞体内）
                float maxWaitTime = 2f; // 最大等待时间，防止无限等待
                float elapsedTime = 0f;
                
                // 先等待最小时间让玩家开始下落
                await UniTask.Delay((int)(dropDuration * 1000), cancellationToken: dropCts.Token);
                elapsedTime += dropDuration;

                // 持续检测直到玩家离开平台区域
                while (elapsedTime < maxWaitTime && platformCollider != null)
                {
                    // 检测玩家是否还在与平台重叠
                    bool isOverlapping = IsOverlappingWithPlatform(platformCollider);
                    
                    if (!isOverlapping)
                    {
                        break; // 玩家已离开平台区域
                    }

                    await UniTask.Delay(50, cancellationToken: dropCts.Token); // 每50ms检测一次
                    elapsedTime += 0.05f;
                }

                // 恢复碰撞
                if (platformCollider != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
                }
            }
        }
        catch (System.OperationCanceledException)
        {
            // 任务被取消，确保恢复碰撞
            if (platformCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }
        }
        finally
        {
            isDropping = false;
        }
    }

    /// <summary>
    /// 检测玩家是否与指定平台碰撞体重叠
    /// </summary>
    private bool IsOverlappingWithPlatform(Collider2D platformCollider)
    {
        if (playerCollider == null || platformCollider == null) return false;
        
        // 使用OverlapCollider检测重叠
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(platformLayer);
        filter.useTriggers = false;
        
        Collider2D[] results = new Collider2D[5];
        int count = playerCollider.OverlapCollider(filter, results);
        
        for (int i = 0; i < count; i++)
        {
            if (results[i] == platformCollider)
            {
                return true;
            }
        }
        
        return false;
    }

    #endregion

    #region 调试

    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null) return;

        // 绘制平台检测射线
        Vector2 origin = (Vector2)transform.position + Vector2.down * (playerCollider.bounds.extents.y);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector2.down * platformCheckDistance);
    }

    #endregion
}
