using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 打字机效果触发器
/// 玩家进入区域后自动播放打字机效果显示文本
/// 支持多句话分段显示，模拟NPC说话效果
/// </summary>
public class TypewriterTrigger : MonoBehaviour
{
    #region 序列化字段

    [Header("文本配置")]
    [SerializeField]
    [TextArea(3, 10)]
    [Tooltip("使用 | 分隔多句话，例如: 你好！|欢迎来到训练区。|祝你好运！")]
    private string displayText = "你好！|欢迎来到训练区。|祝你好运！";

    [SerializeField]
    [Tooltip("每个字符的显示间隔（秒），越小越快")]
    private float typingSpeed = 0.03f;

    [SerializeField]
    [Tooltip("开始显示前的延迟")]
    private float startDelay = 0.2f;

    [SerializeField]
    [Tooltip("每句话之间的停顿时间")]
    private float sentenceDelay = 0.8f;

    [SerializeField]
    [Tooltip("句子分隔符")]
    private char sentenceSeparator = '|';

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
    [SerializeField] private float soundInterval = 2;

    [Header("Gizmo 显示")]
    [SerializeField] private Color gizmoColor = Color.yellow;

    #endregion

    #region 私有变量

    private bool hasPlayed = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private int soundCounter = 0;
    private List<string> sentences = new List<string>();

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

        // 解析句子
        ParseSentences();
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

    /// <summary>
    /// 解析文本为多个句子
    /// </summary>
    private void ParseSentences()
    {
        sentences.Clear();

        if (string.IsNullOrEmpty(displayText))
        {
            return;
        }

        string[] parts = displayText.Split(sentenceSeparator);
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                sentences.Add(trimmed);
            }
        }

        Debug.Log($"[TypewriterTrigger] 解析出 {sentences.Count} 个句子");
    }

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

        // 重新解析句子（以防运行时修改）
        ParseSentences();

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

    /// <summary>
    /// 多句话打字机效果协程
    /// </summary>
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

        // 逐句显示
        for (int sentenceIndex = 0; sentenceIndex < sentences.Count; sentenceIndex++)
        {
            string sentence = sentences[sentenceIndex];

            // 清空当前文本（显示新句子）
            if (targetText != null)
            {
                targetText.text = "";
            }

            // 逐字显示当前句子
            for (int charIndex = 0; charIndex < sentence.Length; charIndex++)
            {
                if (targetText != null)
                {
                    targetText.text += sentence[charIndex];
                }

                // 播放打字音效
                PlayTypingSound();

                // 等待
                yield return new WaitForSeconds(typingSpeed);
            }

            Debug.Log($"[TypewriterTrigger] 句子 {sentenceIndex + 1}/{sentences.Count} 完成: {sentence}");

            // 如果不是最后一句，等待后继续下一句
            if (sentenceIndex < sentences.Count - 1)
            {
                yield return new WaitForSeconds(sentenceDelay);
            }
        }

        isTyping = false;
        Debug.Log($"[TypewriterTrigger] 所有句子打字机效果完成: {name}");
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
    /// 立即显示全部文本（显示最后一句）
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
            // 显示最后一句话
            if (sentences.Count > 0)
            {
                targetText.text = sentences[sentences.Count - 1];
            }
            else
            {
                targetText.text = displayText;
            }
        }
    }

    /// <summary>
    /// 获取所有句子列表
    /// </summary>
    public List<string> GetSentences()
    {
        return new List<string>(sentences);
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

