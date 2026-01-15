using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 玩家状态栏UI控制器
/// 显示生命值、头像动画、受伤反馈
/// 由PlayerStatusUIManager管理生命周期
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
    #region 单例（兼容直接访问）

    private static PlayerStatusUI instance;
    public static PlayerStatusUI Instance => instance;

    #endregion

    #region UI引用

    [Header("背景")]
    [SerializeField] private RawImage background;
    [SerializeField] private RawImage borderBackground;

    [Header("头像")]
    [SerializeField] private Animator headIconAnimator;

    [Header("生命值")]
    [SerializeField] private GameObject heartContainer;
    [SerializeField] private Image[] hearts;

    #endregion

    #region 配置

    [Header("背景颜色配置")]
    [SerializeField] private Color bgNormalColor = new Color(0.1f, 0.6f, 0.1f);
    [SerializeField] private Color bgHurtColor = new Color(0.7f, 0.1f, 0.1f);

    [Header("边框颜色配置")]
    [SerializeField] private Color borderNormalColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color borderHurtColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("受伤持续时间")]
    [SerializeField] private float hurtColorDuration = 1f;

    [Header("眨眼动画配置")]
    [SerializeField] private string blinkAnimationName = "Blink";
    [SerializeField] private float[] blinkIntervals = { 10f, 15f, 20f };

    #endregion

    #region 私有字段

    private PlayerController playerController;
    private int lastHealth = -1;
    private Coroutine blinkCoroutine;
    private Coroutine hurtColorCoroutine;

    #endregion

    #region 生命周期

    private void Awake()
    {
        // 单例设置（兼容直接访问）
        if (instance == null)
        {
            instance = this;
        }

        // 初始化颜色
        SetBackgroundColors(false);
    }

    private void Start()
    {
        // 查找玩家
        FindPlayer();

        // 启动眨眼协程
        StartBlinkRoutine();

        // 初始化生命值显示
        if (playerController != null)
        {
            UpdateHealthDisplay(playerController.CurrentHealth);
        }
    }

    private void Update()
    {
        // 尝试查找玩家（场景切换后）
        if (playerController == null)
        {
            FindPlayer();
        }

        // 同步生命值显示
        if (playerController != null && playerController.CurrentHealth != lastHealth)
        {
            UpdateHealthDisplay(playerController.CurrentHealth);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    #endregion

    #region 玩家查找

    /// <summary>
    /// 查找场景中的玩家
    /// </summary>
    private void FindPlayer()
    {
        playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            lastHealth = playerController.CurrentHealth;
            UpdateHealthDisplay(lastHealth);
        }
    }

    #endregion

    #region 生命值显示

    /// <summary>
    /// 更新生命值显示
    /// </summary>
    private void UpdateHealthDisplay(int health)
    {
        lastHealth = health;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
            {
                hearts[i].gameObject.SetActive(i < health);
            }
        }
    }

    #endregion

    #region 受伤反馈

    /// <summary>
    /// 触发受伤UI反馈（由PlayerController调用）
    /// </summary>
    public void OnPlayerHurt()
    {
        if (hurtColorCoroutine != null)
        {
            StopCoroutine(hurtColorCoroutine);
        }
        hurtColorCoroutine = StartCoroutine(HurtColorRoutine());
    }

    /// <summary>
    /// 受伤颜色变化协程
    /// </summary>
    private IEnumerator HurtColorRoutine()
    {
        SetBackgroundColors(true);
        yield return new WaitForSeconds(hurtColorDuration);
        SetBackgroundColors(false);
        hurtColorCoroutine = null;
    }

    /// <summary>
    /// 设置背景和边框颜色
    /// </summary>
    /// <param name="isHurt">是否为受伤状态</param>
    private void SetBackgroundColors(bool isHurt)
    {
        if (background != null)
        {
            background.color = isHurt ? bgHurtColor : bgNormalColor;
        }
        if (borderBackground != null)
        {
            borderBackground.color = isHurt ? borderHurtColor : borderNormalColor;
        }
    }

    #endregion

    #region 眨眼动画

    /// <summary>
    /// 启动眨眼协程
    /// </summary>
    private void StartBlinkRoutine()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// 眨眼动画协程
    /// </summary>
    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // 随机选择间隔时间
            float interval = blinkIntervals[Random.Range(0, blinkIntervals.Length)];
            yield return new WaitForSeconds(interval);

            // 播放眨眼动画
            PlayBlinkAnimation();
        }
    }

    /// <summary>
    /// 播放眨眼动画
    /// </summary>
    private void PlayBlinkAnimation()
    {
        if (headIconAnimator != null)
        {
            headIconAnimator.Play(blinkAnimationName, 0, 0f);
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 强制刷新UI状态
    /// </summary>
    public void RefreshUI()
    {
        FindPlayer();
        if (playerController != null)
        {
            UpdateHealthDisplay(playerController.CurrentHealth);
        }
    }

    /// <summary>
    /// 设置生命值显示（手动设置）
    /// </summary>
    public void SetHealth(int health)
    {
        UpdateHealthDisplay(health);
    }

    #endregion
}
