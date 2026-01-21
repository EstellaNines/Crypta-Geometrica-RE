using UnityEngine;
using Sirenix.OdinInspector;

namespace CryptaGeometrica.Enemy.States
{
    /// <summary>
    /// 巡逻模式枚举
    /// </summary>
    public enum PatrolMode
    {
        /// <summary>
        /// 地面模式 - 只左右移动，检测悬崖和墙壁
        /// </summary>
        Ground = 0,

        /// <summary>
        /// 飞行模式 - 任意方向移动
        /// </summary>
        Flying = 1
    }

    /// <summary>
    /// 巡逻状态
    /// 敌人随机方向移动，无行走动画（漂浮状态）
    /// 地面模式支持悬崖和墙壁检测，自动转向
    /// </summary>
    [System.Serializable]
    public class PatrolState : EnemyStateBase
    {
        #region Configuration

        [TitleGroup("巡逻设置")]
        [LabelText("巡逻模式")]
        [SerializeField]
        private PatrolMode _patrolMode = PatrolMode.Ground;

        [TitleGroup("巡逻设置")]
        [LabelText("移动速度倍率")]
        [MinValue(0.1f)]
        [SerializeField]
        private float _speedMultiplier = 1f;

        [TitleGroup("地面检测")]
        [LabelText("地面检测距离")]
        [ShowIf("_patrolMode", PatrolMode.Ground)]
        [SerializeField]
        private float _groundCheckDistance = 1.5f;

        [TitleGroup("地面检测")]
        [LabelText("地面检测偏移X")]
        [Tooltip("前方多远开始检测地面")]
        [ShowIf("_patrolMode", PatrolMode.Ground)]
        [SerializeField]
        private float _groundCheckOffsetX = 0.6f;

        [TitleGroup("地面检测")]
        [LabelText("墙壁检测距离")]
        [ShowIf("_patrolMode", PatrolMode.Ground)]
        [SerializeField]
        private float _wallCheckDistance = 0.3f;

        [TitleGroup("地面检测")]
        [LabelText("墙壁检测高度")]
        [Tooltip("从敌人脚底往上多高检测墙壁")]
        [ShowIf("_patrolMode", PatrolMode.Ground)]
        [SerializeField]
        private float _wallCheckHeight = 0.3f;

        [TitleGroup("地面检测")]
        [LabelText("转向冷却时间")]
        [Tooltip("防止快速连续转向导致抽搐")]
        [ShowIf("_patrolMode", PatrolMode.Ground)]
        [SerializeField]
        private float _turnCooldown = 0.3f;

        [TitleGroup("地面检测")]
        [LabelText("地面层 (Ground)")]
        [Tooltip("选择 Tilemap 所在的 Ground 层")]
        [ShowIf("_patrolMode", PatrolMode.Ground)]
        [SerializeField]
        private LayerMask _groundLayer;

        #endregion

        #region Runtime

        private float _timer;
        private float _patrolDuration;
        private Vector2 _moveDirection;
        private float _lastTurnTime;

        #endregion

        #region Constructor

        public PatrolState()
        {
            _stateName = EnemyController.STATE_PATROL;
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            LogInfo($"Entered Patrol State (Mode: {_patrolMode})");

            // 计算巡逻时间
            if (enemy?.Config != null)
            {
                _patrolDuration = enemy.Config.GetRandomPatrolDuration();
            }
            else
            {
                _patrolDuration = 3f;
            }

            _timer = 0f;
            _lastTurnTime = -_turnCooldown; // 允许立即转向

            // 计算移动方向
            CalculateMoveDirection();
            UpdateFacingDirection();
        }

        public override void Update()
        {
            _timer += Time.deltaTime;

            // 巡逻时间到，切换回待机状态
            if (_timer >= _patrolDuration)
            {
                if (stateMachine.HasState(EnemyController.STATE_IDLE))
                {
                    ChangeState(EnemyController.STATE_IDLE);
                }
            }
        }

        public override void FixedUpdate()
        {
            if (enemy?.Config == null) return;

            // 地面模式检测悬崖和墙壁
            if (_patrolMode == PatrolMode.Ground)
            {
                if (ShouldTurnAround())
                {
                    TurnAround();
                }
            }

            // 应用移动
            float speed = enemy.Config.MoveSpeed * _speedMultiplier;
            Vector2 velocity = _moveDirection * speed;

            if (_patrolMode == PatrolMode.Ground)
            {
                enemy.SetVelocityX(velocity.x);
            }
            else
            {
                enemy.SetVelocity(velocity);
            }
        }

        public override void Exit()
        {
            if (enemy != null)
            {
                enemy.SetVelocityX(0f);
                if (_patrolMode == PatrolMode.Flying)
                {
                    enemy.SetVelocity(Vector2.zero);
                }
            }

            LogInfo("Exited Patrol State");
        }

        #endregion

        #region Ground Detection

        /// <summary>
        /// 检测是否需要转向（悬崖或墙壁）
        /// </summary>
        private bool ShouldTurnAround()
        {
            if (enemy == null) return false;

            // 转向冷却检查
            if (Time.time - _lastTurnTime < _turnCooldown)
            {
                return false;
            }

            Vector2 pos = enemy.transform.position;
            float direction = _moveDirection.x;

            // 检测前方是否有悬崖（地面检测）
            Vector2 groundCheckPos = pos + new Vector2(_groundCheckOffsetX * direction, 0f);
            RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, _groundCheckDistance, _groundLayer);

            if (!groundHit.collider)
            {
                LogInfo("检测到悬崖，转向");
                return true;
            }

            // 检测前方是否有墙壁
            Vector2 wallCheckPos = pos + new Vector2(0, _wallCheckHeight);
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, new Vector2(direction, 0), _wallCheckDistance, _groundLayer);

            if (wallHit.collider)
            {
                LogInfo("检测到墙壁，转向");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 转向
        /// </summary>
        private void TurnAround()
        {
            _moveDirection.x = -_moveDirection.x;
            _lastTurnTime = Time.time;
            UpdateFacingDirection();
            LogInfo($"转向，新方向: {_moveDirection.x}");
        }

        /// <summary>
        /// 更新朝向
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (_moveDirection.x != 0 && enemy != null)
            {
                enemy.SetFacingDirection(_moveDirection.x > 0);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 计算移动方向
        /// </summary>
        private void CalculateMoveDirection()
        {
            switch (_patrolMode)
            {
                case PatrolMode.Ground:
                    _moveDirection = Random.value > 0.5f ? Vector2.right : Vector2.left;
                    break;

                case PatrolMode.Flying:
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    _moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    break;

                default:
                    _moveDirection = Vector2.right;
                    break;
            }

            LogInfo($"Move direction: {_moveDirection}");
        }

        #endregion

        #region Debug Gizmos

#if UNITY_EDITOR
        /// <summary>
        /// 在 Scene 视图绘制检测射线（需要在 EnemyController 中调用）
        /// </summary>
        public void DrawGizmos()
        {
            if (enemy == null || _patrolMode != PatrolMode.Ground) return;

            Vector2 pos = enemy.transform.position;
            float direction = _moveDirection.x != 0 ? _moveDirection.x : 1;

            // 绘制地面检测射线
            Vector2 groundCheckPos = pos + new Vector2(_groundCheckOffsetX * direction, 0f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheckPos, groundCheckPos + Vector2.down * _groundCheckDistance);

            // 绘制墙壁检测射线
            Vector2 wallCheckPos = pos + new Vector2(0, _wallCheckHeight);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wallCheckPos, wallCheckPos + new Vector2(direction * _wallCheckDistance, 0));
        }
#endif

        #endregion
    }
}

