using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;

/// <summary>
/// 封面图片动态效果控制器
/// 实现呼吸缩放、缓慢位移、淡入淡出等效果
/// </summary>
public class CoverImageAnimator : MonoBehaviour
{
    #region 配置参数

    [TitleGroup("目标组件")]
    [LabelText("目标图片")]
    [Required("请指定目标Image组件")]
    [SerializeField] private Image targetImage;

    [TitleGroup("目标组件")]
    [LabelText("目标RectTransform")]
    [SerializeField] private RectTransform targetRect;

    [TitleGroup("呼吸缩放")]
    [LabelText("启用呼吸效果")]
    [SerializeField] private bool enableBreathing = true;

    [TitleGroup("呼吸缩放")]
    [LabelText("最小缩放")]
    [ShowIf("enableBreathing")]
    [Range(0.9f, 1.0f)]
    [SerializeField] private float breathingMinScale = 1.0f;

    [TitleGroup("呼吸缩放")]
    [LabelText("最大缩放")]
    [ShowIf("enableBreathing")]
    [Range(1.0f, 1.2f)]
    [SerializeField] private float breathingMaxScale = 1.05f;

    [TitleGroup("呼吸缩放")]
    [LabelText("呼吸周期")]
    [ShowIf("enableBreathing")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(1f, 10f)]
    [SerializeField] private float breathingDuration = 4f;

    [TitleGroup("缓慢位移")]
    [LabelText("启用位移效果")]
    [SerializeField] private bool enablePanning = true;

    [TitleGroup("缓慢位移")]
    [LabelText("位移范围")]
    [ShowIf("enablePanning")]
    [SuffixLabel("像素", Overlay = true)]
    [SerializeField] private Vector2 panRange = new Vector2(20f, 10f);

    [TitleGroup("缓慢位移")]
    [LabelText("位移周期")]
    [ShowIf("enablePanning")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(5f, 20f)]
    [SerializeField] private float panDuration = 10f;

    [TitleGroup("淡入效果")]
    [LabelText("启用淡入")]
    [SerializeField] private bool enableFadeIn = true;

    [TitleGroup("淡入效果")]
    [LabelText("淡入时长")]
    [ShowIf("enableFadeIn")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0.5f, 3f)]
    [SerializeField] private float fadeInDuration = 1.5f;

    [TitleGroup("淡入效果")]
    [LabelText("淡入延迟")]
    [ShowIf("enableFadeIn")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0f, 2f)]
    [SerializeField] private float fadeInDelay = 0.2f;

    #endregion

    #region 私有变量

    private Vector3 originalScale;
    private Vector2 originalPosition;
    private Sequence breathingSequence;
    private Sequence panningSequence;
    private Tween fadeTween;
    private bool isPaused = false;

    #endregion

    #region 生命周期

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        if (targetRect == null)
            targetRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        originalScale = targetRect.localScale;
        originalPosition = targetRect.anchoredPosition;
        Initialize();
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    private void OnDisable()
    {
        KillAllTweens();
    }

    #endregion

    #region 公共方法

    [TitleGroup("调试")]
    [Button("初始化效果", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void Initialize()
    {
        if (enableFadeIn)
        {
            SetAlpha(0f);
            PlayFadeIn(() => StartAnimations());
        }
        else
        {
            StartAnimations();
        }
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/控制")]
    [Button("播放淡入")]
    public void PlayFadeIn(Action onComplete = null)
    {
        fadeTween?.Kill();
        fadeTween = targetImage.DOFade(1f, fadeInDuration)
            .SetDelay(fadeInDelay)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/控制")]
    [Button("播放淡出")]
    public void PlayFadeOut(float duration = 0.5f, Action onComplete = null)
    {
        fadeTween?.Kill();
        fadeTween = targetImage.DOFade(0f, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/状态")]
    [Button("暂停")]
    public void Pause()
    {
        SetPaused(true);
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/状态")]
    [Button("恢复")]
    public void Resume()
    {
        SetPaused(false);
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;

        if (paused)
        {
            breathingSequence?.Pause();
            panningSequence?.Pause();
        }
        else
        {
            breathingSequence?.Play();
            panningSequence?.Play();
        }
    }

    [TitleGroup("调试")]
    [Button("重置到默认", ButtonSizes.Medium)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    public void ResetToDefault()
    {
        KillAllTweens();
        targetRect.localScale = originalScale;
        targetRect.anchoredPosition = originalPosition;
        SetAlpha(1f);
    }

    public void RestartAnimations()
    {
        KillAllTweens();
        StartAnimations();
    }

    #endregion

    #region 私有方法

    private void StartAnimations()
    {
        if (enableBreathing)
            StartBreathingEffect();

        if (enablePanning)
            StartPanningEffect();
    }

    private void StartBreathingEffect()
    {
        breathingSequence?.Kill();
        breathingSequence = DOTween.Sequence();

        breathingSequence.Append(
            targetRect.DOScale(originalScale * breathingMaxScale, breathingDuration / 2f)
                .SetEase(Ease.InOutSine)
        );

        breathingSequence.Append(
            targetRect.DOScale(originalScale * breathingMinScale, breathingDuration / 2f)
                .SetEase(Ease.InOutSine)
        );

        breathingSequence.SetLoops(-1, LoopType.Restart);
    }

    private void StartPanningEffect()
    {
        panningSequence?.Kill();
        panningSequence = DOTween.Sequence();

        float halfDuration = panDuration / 4f;

        panningSequence.Append(
            targetRect.DOAnchorPos(originalPosition + new Vector2(panRange.x, panRange.y), halfDuration)
                .SetEase(Ease.InOutSine)
        );

        panningSequence.Append(
            targetRect.DOAnchorPos(originalPosition + new Vector2(-panRange.x, panRange.y), halfDuration)
                .SetEase(Ease.InOutSine)
        );

        panningSequence.Append(
            targetRect.DOAnchorPos(originalPosition + new Vector2(-panRange.x, -panRange.y), halfDuration)
                .SetEase(Ease.InOutSine)
        );

        panningSequence.Append(
            targetRect.DOAnchorPos(originalPosition + new Vector2(panRange.x, -panRange.y), halfDuration)
                .SetEase(Ease.InOutSine)
        );

        panningSequence.SetLoops(-1, LoopType.Restart);
    }

    private void SetAlpha(float alpha)
    {
        if (targetImage != null)
        {
            Color c = targetImage.color;
            c.a = alpha;
            targetImage.color = c;
        }
    }

    private void KillAllTweens()
    {
        breathingSequence?.Kill();
        panningSequence?.Kill();
        fadeTween?.Kill();

        breathingSequence = null;
        panningSequence = null;
        fadeTween = null;
    }

    #endregion
}
