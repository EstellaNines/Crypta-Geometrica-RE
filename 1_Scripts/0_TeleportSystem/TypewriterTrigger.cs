using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 打字机效果触发器
/// 玩家进入区域后自动播放打字机效果显示文本
/// </summary>
public class TypewriterTrigger : MonoBehaviour
{
    #region 序列化字段

    [Header("文本配置")]
    [SerializeField] [TextArea(3, 10)] private string displayText = "Welcome to the training area.";
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float startDelay = 0.3f;

    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private GameObject textContainer;

    [Header("触发配置")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool hideOnExit = true;
    [SerializeField] private float hideDelay = 1.0f;

    [Header("音效 (可选)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private float soundInterval = 3;

    [Header("Gizmo 显示")]
    [SerializeField] private Color gizmoColor = Color.yellow;

    #endregion

    #region 私有变量

    private bool hasPlayed = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private int soundCounter = 0;

    #endregion

    #region 生命周期

    private void Start()
    {
        // 初始化 UI
        if (textContainer != null)
        {
            textContainer.SetActive(false);
        }

        if (targetText != null)
        {
            targetText.text = "";
        }
    }

    #endregion

    #region 触发器

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerEnter();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerExit();
        }
    }

    // 3D 触发器支持
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerExit();
        }
    }

    #endregion

    #region 私有方法

    private void OnPlayerEnter()
    {
        if (playOnce && hasPlayed)
        {
            return;
        }

        Debug.Log($"[TypewriterTrigger] 玩家进入，开始打字机效果: {name}");
        StartTypewriter();
    }

    private void OnPlayerExit()
    {
        Debug.Log($"[TypewriterTrigger] 玩家离开: {name}");

        if (hideOnExit)
        {
            if (isTyping)
            {
                // 如果还在打字，立即停止并隐藏
                StopTypewriter();
                HideText();
            }
            else
            {
                // 延迟隐藏
                StartCoroutine(DelayedHide());
            }
        }
    }

    private void StartTypewriter()
    {
        if (isTyping)
        {
            StopTypewriter();
        }

        // 显示容器
        if (textContainer != null)
        {
            textContainer.SetActive(true);
        }

        // 开始打字
        typingCoroutine = StartCoroutine(TypewriterCoroutine());
    }

    private void StopTypewriter()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }

    private IEnumerator TypewriterCoroutine()
    {
        isTyping = true;
        hasPlayed = true;
        soundCounter = 0;

        if (targetText != null)
        {
            targetText.text = "";
        }

        // 开始延迟
        yield return new WaitForSeconds(startDelay);

        // 逐字显示
        for (int i = 0; i < displayText.Length; i++)
        {
            if (targetText != null)
            {
                targetText.text += displayText[i];
            }

            // 播放打字音效
            PlayTypingSound();

            // 等待
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        Debug.Log($"[TypewriterTrigger] 打字机效果完成: {name}");
    }

    private void PlayTypingSound()
    {
        if (audioSource != null && typingSound != null)
        {
            soundCounter++;
            if (soundCounter >= soundInterval)
            {
                soundCounter = 0;
                audioSource.PlayOneShot(typingSound);
            }
        }
    }

    private IEnumerator DelayedHide()
    {
        yield return new WaitForSeconds(hideDelay);
        HideText();
    }

    private void HideText()
    {
        if (textContainer != null)
        {
            textContainer.SetActive(false);
        }

        if (targetText != null)
        {
            targetText.text = "";
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 重置状态，允许再次播放
    /// </summary>
    public void ResetTrigger()
    {
        hasPlayed = false;
        StopTypewriter();
        HideText();
    }

    /// <summary>
    /// 手动触发打字机效果
    /// </summary>
    public void TriggerTypewriter()
    {
        StartTypewriter();
    }

    /// <summary>
    /// 立即显示全部文本
    /// </summary>
    public void ShowFullText()
    {
        StopTypewriter();
        hasPlayed = true;

        if (textContainer != null)
        {
            textContainer.SetActive(true);
        }

        if (targetText != null)
        {
            targetText.text = displayText;
        }
    }

    #endregion

    #region Editor Gizmo

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        Collider2D col2D = GetComponent<Collider2D>();
        Collider col3D = GetComponent<Collider>();

        if (col2D != null)
        {
            Gizmos.DrawWireCube(col2D.bounds.center, col2D.bounds.size);
        }
        else if (col3D != null)
        {
            Gizmos.DrawWireCube(col3D.bounds.center, col3D.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);

        Collider2D col2D = GetComponent<Collider2D>();
        Collider col3D = GetComponent<Collider>();

        if (col2D != null)
        {
            Gizmos.DrawCube(col2D.bounds.center, col2D.bounds.size);
        }
        else if (col3D != null)
        {
            Gizmos.DrawCube(col3D.bounds.center, col3D.bounds.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, Vector3.one * 2);
        }
    }

    #endregion
}
