using UnityEngine;
using Cinemachine;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace CryptaGeometrica.Camera
{
    /// <summary>
    /// Cinemachine 摄像机控制器
    /// 管理虚拟摄像机的视角大小
    /// </summary>
    public class CinemachineCameraManager : MonoBehaviour
    {
        #region Singleton

        public static CinemachineCameraManager Instance { get; private set; }

        #endregion

        #region Configuration

        [BoxGroup("摄像机引用")]
        [LabelText("虚拟摄像机")]
        [Required("必须指定虚拟摄像机！")]
        [SerializeField]
        private CinemachineVirtualCamera _virtualCamera;

        [BoxGroup("视角设置")]
        [LabelText("默认视角大小")]
        [SerializeField]
        private float _defaultOrthoSize = 4f;

        [BoxGroup("视角设置")]
        [LabelText("放大视角大小")]
        [SerializeField]
        private float _zoomedOutOrthoSize = 7f;

        [BoxGroup("视角设置")]
        [LabelText("视角过渡时间")]
        [SerializeField]
        private float _zoomTransitionDuration = 0.5f;

        [BoxGroup("视角设置")]
        [LabelText("过渡缓动曲线")]
        [SerializeField]
        private Ease _zoomEase = Ease.OutQuad;

        #endregion

        #region Debug

        [BoxGroup("调试")]
        [LabelText("当前视角大小")]
        [ShowInInspector, ReadOnly]
        private float CurrentOrthoSize => _virtualCamera != null ? _virtualCamera.m_Lens.OrthographicSize : 0f;

        #endregion

        #region Private Fields

        private Tween _zoomTween;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_virtualCamera == null)
            {
                Debug.LogError($"[{nameof(CinemachineCameraManager)}] Critical: VirtualCamera is missing!");
            }
        }

        private void OnDestroy()
        {
            _zoomTween?.Kill();
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置放大视角（7）
        /// </summary>
        public void SetZoomedOutView()
        {
            SetOrthoSize(_zoomedOutOrthoSize);
        }

        /// <summary>
        /// 设置默认视角（4）
        /// </summary>
        public void SetDefaultView()
        {
            SetOrthoSize(_defaultOrthoSize);
        }

        /// <summary>
        /// 设置指定视角大小（带过渡动画）
        /// </summary>
        public void SetOrthoSize(float targetSize, float? duration = null)
        {
            if (_virtualCamera == null) return;

            _zoomTween?.Kill();
            float transitionDuration = duration ?? _zoomTransitionDuration;

            _zoomTween = DOTween.To(
                () => _virtualCamera.m_Lens.OrthographicSize,
                x => _virtualCamera.m_Lens.OrthographicSize = x,
                targetSize,
                transitionDuration
            ).SetEase(_zoomEase);
        }

        /// <summary>
        /// 立即设置视角大小（无过渡）
        /// </summary>
        public void SetOrthoSizeImmediate(float targetSize)
        {
            if (_virtualCamera == null) return;
            _zoomTween?.Kill();
            _virtualCamera.m_Lens.OrthographicSize = targetSize;
        }

        #endregion

        #region Editor Buttons

#if UNITY_EDITOR
        [BoxGroup("调试")]
        [Button("测试放大视角")]
        private void TestZoomOut() => SetZoomedOutView();

        [BoxGroup("调试")]
        [Button("测试默认视角")]
        private void TestDefaultView() => SetDefaultView();
#endif

        #endregion
    }
}
