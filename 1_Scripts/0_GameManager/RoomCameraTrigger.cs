using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.Camera
{
    /// <summary>
    /// 房间视角触发器
    /// 玩家进入触发器时视角变为7，离开时视角变为4
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomCameraTrigger : MonoBehaviour
    {
        #region Configuration

        [BoxGroup("视角设置")]
        [LabelText("进入时视角大小")]
        [SerializeField]
        private float _enterOrthoSize = 7f;

        [BoxGroup("视角设置")]
        [LabelText("离开时视角大小")]
        [SerializeField]
        private float _exitOrthoSize = 4f;

        [BoxGroup("触发设置")]
        [LabelText("玩家标签")]
        [SerializeField]
        private string _playerTag = "Player";

        [BoxGroup("调试")]
        [LabelText("启用日志")]
        [SerializeField]
        private bool _enableLogging = true;

        #endregion

        #region Debug

        [BoxGroup("调试")]
        [LabelText("玩家在区域内")]
        [ShowInInspector, ReadOnly]
        private bool _playerInside = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[RoomCameraTrigger] Collider on {name} should be set as Trigger!");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(_playerTag))
            {
                _playerInside = true;

                if (CinemachineCameraManager.Instance != null)
                {
                    CinemachineCameraManager.Instance.SetOrthoSize(_enterOrthoSize);
                    if (_enableLogging) Debug.Log($"[RoomCameraTrigger] 进入 {name}, 视角: {_enterOrthoSize}");
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(_playerTag))
            {
                _playerInside = false;

                if (CinemachineCameraManager.Instance != null)
                {
                    CinemachineCameraManager.Instance.SetOrthoSize(_exitOrthoSize);
                    if (_enableLogging) Debug.Log($"[RoomCameraTrigger] 离开 {name}, 视角: {_exitOrthoSize}");
                }
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            Gizmos.color = _playerInside ? Color.green : Color.cyan;
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }

        #endregion
    }
}
