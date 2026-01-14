using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

/// <summary>
/// 主界面控制器
/// 协调封面动画和矩阵雨效果
/// </summary>
public class StartPanelController : MonoBehaviour
{
    #region 组件引用

    [TitleGroup("效果组件")]
    [LabelText("封面动画器")]
    [Required("请指定封面动画组件")]
    [SerializeField] private CoverImageAnimator coverAnimator;

    [TitleGroup("效果组件")]
    [LabelText("矩阵雨效果")]
    [Required("请指定矩阵雨效果组件")]
    [SerializeField] private MatrixRainEffect matrixRain;

    [TitleGroup("UI组件")]
    [LabelText("主画布组")]
    [SerializeField] private CanvasGroup mainCanvasGroup;

    [TitleGroup("UI组件")]
    [LabelText("开始按钮")]
    [SerializeField] private Button startButton;

    [TitleGroup("UI组件")]
    [LabelText("设置按钮")]
    [SerializeField] private Button settingsButton;

    [TitleGroup("UI组件")]
    [LabelText("菜单界面")]
    [InfoBox("需要添加淡入和从左到右移动动效的菜单界面")]
    [SerializeField] private RectTransform menuPanel;

    [TitleGroup("UI组件")]
    [LabelText("游戏标题图片")]
    [InfoBox("需要添加淡入和从右到左移动动效的标题图片")]
    [SerializeField] private RectTransform titleImage;

    [TitleGroup("过渡设置")]
    [LabelText("过渡时长")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0.1f, 2f)]
    [SerializeField] private float transitionDuration = 0.5f;

    [TitleGroup("菜单动效设置")]
    [LabelText("菜单入场时长")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0.1f, 3f)]
    [SerializeField] private float menuEnterDuration = 0.8f;

    [TitleGroup("菜单动效设置")]
    [LabelText("菜单起始偏移")]
    [InfoBox("菜单从左侧偏移多少像素开始移动")]
    [SuffixLabel("像素", Overlay = true)]
    [SerializeField] private float menuStartOffset = -300f;

    [TitleGroup("菜单动效设置")]
    [LabelText("菜单移动缓动")]
    [SerializeField] private Ease menuMoveEase = Ease.OutCubic;

    [TitleGroup("菜单动效设置")]
    [LabelText("菜单淡入缓动")]
    [SerializeField] private Ease menuFadeEase = Ease.InOutQuad;

    [TitleGroup("菜单动效设置")]
    [LabelText("菜单入场延迟")]
    [InfoBox("菜单动效开始前的等待时间")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0f, 5f)]
    [SerializeField] private float menuEnterDelay = 1f;

    [TitleGroup("标题动效设置")]
    [LabelText("标题入场时长")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0.1f, 3f)]
    [SerializeField] private float titleEnterDuration = 0.8f;

    [TitleGroup("标题动效设置")]
    [LabelText("标题起始偏移")]
    [InfoBox("标题从右侧偏移多少像素开始移动")]
    [SuffixLabel("像素", Overlay = true)]
    [SerializeField] private float titleStartOffset = 300f;

    [TitleGroup("标题动效设置")]
    [LabelText("标题移动缓动")]
    [SerializeField] private Ease titleMoveEase = Ease.OutCubic;

    [TitleGroup("标题动效设置")]
    [LabelText("标题淡入缓动")]
    [SerializeField] private Ease titleFadeEase = Ease.InOutQuad;

    [TitleGroup("标题动效设置")]
    [LabelText("标题入场延迟")]
    [InfoBox("标题动效开始前的等待时间")]
    [SuffixLabel("秒", Overlay = true)]
    [Range(0f, 5f)]
    [SerializeField] private float titleEnterDelay = 0.5f;

    #endregion

    #region 运行时状态

    [TitleGroup("运行时状态")]
    [LabelText("是否过渡中")]
    [ShowInInspector, ReadOnly]
    private bool isTransitioning = false;

    #endregion

    #region 生命周期

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveAllListeners();
        if (settingsButton != null)
            settingsButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region 公共方法

    [TitleGroup("调试")]
    [Button("初始化", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void Initialize()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (matrixRain != null)
            matrixRain.Initialize();

        PlayMenuEnterAnimation();
        PlayTitleEnterAnimation();
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/效果")]
    [Button("暂停所有效果")]
    public void PauseEffects()
    {
        if (coverAnimator != null)
            coverAnimator.SetPaused(true);
        if (matrixRain != null)
            matrixRain.Pause();
    }

    [TitleGroup("调试")]
    [ButtonGroup("调试/效果")]
    [Button("恢复所有效果")]
    public void ResumeEffects()
    {
        if (coverAnimator != null)
            coverAnimator.SetPaused(false);
        if (matrixRain != null)
            matrixRain.Resume();
    }

    [TitleGroup("调试")]
    [Button("设置矩阵雨强度")]
    public void SetMatrixRainIntensity([PropertyRange(0f, 1f)] float intensity)
    {
        if (matrixRain != null)
            matrixRain.SetIntensity(intensity);
    }

    [TitleGroup("调试")]
    [Button("播放退出过渡", ButtonSizes.Medium)]
    [GUIColor(0.8f, 0.6f, 0.2f)]
    public void PlayExitTransition(System.Action onComplete = null)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (coverAnimator != null)
        {
            coverAnimator.PlayFadeOut(transitionDuration);
        }

        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.DOFade(0f, transitionDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    isTransitioning = false;
                    onComplete?.Invoke();
                });
        }
        else
        {
            DOVirtual.DelayedCall(transitionDuration, () =>
            {
                isTransitioning = false;
                onComplete?.Invoke();
            });
        }
    }

    [TitleGroup("调试")]
    [Button("播放菜单入场动效", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.6f, 0.8f)]
    public void PlayMenuEnterAnimation()
    {
        if (menuPanel == null)
        {
            Debug.LogWarning("[StartPanelController] 菜单界面未设置");
            return;
        }

        CanvasGroup menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (menuCanvasGroup == null)
        {
            menuCanvasGroup = menuPanel.gameObject.AddComponent<CanvasGroup>();
        }

        Vector2 originalPosition = menuPanel.anchoredPosition;
        Vector2 startPosition = new Vector2(originalPosition.x + menuStartOffset, originalPosition.y);

        menuPanel.anchoredPosition = startPosition;
        menuCanvasGroup.alpha = 0f;

        Sequence menuSequence = DOTween.Sequence();

        if (menuEnterDelay > 0f)
        {
            menuSequence.AppendInterval(menuEnterDelay);
        }

        menuSequence.Append(menuPanel.DOAnchorPos(originalPosition, menuEnterDuration)
            .SetEase(menuMoveEase));

        menuSequence.Join(menuCanvasGroup.DOFade(1f, menuEnterDuration)
            .SetEase(menuFadeEase));

        menuSequence.OnComplete(() =>
        {
            Debug.Log("[StartPanelController] 菜单入场动效完成");
        });

        menuSequence.Play();
    }

    [TitleGroup("调试")]
    [Button("播放标题入场动效", ButtonSizes.Medium)]
    [GUIColor(0.6f, 0.4f, 0.8f)]
    public void PlayTitleEnterAnimation()
    {
        if (titleImage == null)
        {
            Debug.LogWarning("[StartPanelController] 标题图片未设置");
            return;
        }

        CanvasGroup titleCanvasGroup = titleImage.GetComponent<CanvasGroup>();
        if (titleCanvasGroup == null)
        {
            titleCanvasGroup = titleImage.gameObject.AddComponent<CanvasGroup>();
        }

        Vector2 originalPosition = titleImage.anchoredPosition;
        Vector2 startPosition = new Vector2(originalPosition.x + titleStartOffset, originalPosition.y);

        titleImage.anchoredPosition = startPosition;
        titleCanvasGroup.alpha = 0f;

        Sequence titleSequence = DOTween.Sequence();

        if (titleEnterDelay > 0f)
        {
            titleSequence.AppendInterval(titleEnterDelay);
        }

        titleSequence.Append(titleImage.DOAnchorPos(originalPosition, titleEnterDuration)
            .SetEase(titleMoveEase));

        titleSequence.Join(titleCanvasGroup.DOFade(1f, titleEnterDuration)
            .SetEase(titleFadeEase));

        titleSequence.OnComplete(() =>
        {
            Debug.Log("[StartPanelController] 标题入场动效完成");
        });

        titleSequence.Play();
    }

    #endregion

    #region 按钮回调

    private void OnStartButtonClicked()
    {
        if (isTransitioning) return;

        Debug.Log("[StartPanelController] 开始游戏");

        PlayExitTransition(() =>
        {
            // 使用异步场景管理器加载游戏场景
            // 会先加载LoadingScene，然后异步加载目标场景
            AsyncSceneManager.Instance.LoadScene("GameScene");
        });
    }

    private void OnSettingsButtonClicked()
    {
        if (isTransitioning) return;

        Debug.Log("[StartPanelController] 打开设置");

        PauseEffects();

        // TODO: 打开设置界面
        // 关闭设置后调用 ResumeEffects()
    }

    #endregion
}
