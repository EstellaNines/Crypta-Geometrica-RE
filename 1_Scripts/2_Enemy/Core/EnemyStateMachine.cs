using System.Collections.Generic;
using UnityEngine;

namespace CryptaGeometrica.Enemy
{
    /// <summary>
    /// 敌人状态机
    /// 管理状态切换和状态生命周期调用
    /// 采用列表装载模式，通过状态名称进行切换
    /// </summary>
    [System.Serializable]
    public class EnemyStateMachine
    {
        #region Properties

        /// <summary>
        /// 当前激活的状态
        /// </summary>
        public IEnemyState CurrentState { get; private set; }

        /// <summary>
        /// 当前状态名称
        /// </summary>
        public string CurrentStateName => CurrentState?.StateName ?? "None";

        #endregion

        #region Private Fields

        /// <summary>
        /// 状态注册表（按名称索引）
        /// </summary>
        private readonly Dictionary<string, IEnemyState> _states = new Dictionary<string, IEnemyState>();

        /// <summary>
        /// 敌人控制器引用
        /// </summary>
        private EnemyController _enemy;

        /// <summary>
        /// 启用日志
        /// </summary>
        private bool _enableLogging;

        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化状态机
        /// 遍历状态列表并注册每个状态
        /// </summary>
        /// <param name="enemy">敌人控制器引用</param>
        /// <param name="states">状态列表</param>
        /// <param name="enableLogging">是否启用日志</param>
        public void Initialize(EnemyController enemy, List<EnemyStateBase> states, bool enableLogging = false)
        {
            _enemy = enemy;
            _enableLogging = enableLogging;
            _states.Clear();

            if (states == null || states.Count == 0)
            {
                Debug.LogError($"[EnemyStateMachine] No states provided for initialization!");
                return;
            }

            // 注册所有状态
            foreach (var state in states)
            {
                if (state == null)
                {
                    Debug.LogWarning($"[EnemyStateMachine] Null state found in list, skipping.");
                    continue;
                }

                if (_states.ContainsKey(state.StateName))
                {
                    Debug.LogWarning($"[EnemyStateMachine] Duplicate state name '{state.StateName}', skipping.");
                    continue;
                }

                state.Initialize(enemy, this);
                _states.Add(state.StateName, state);

                if (_enableLogging)
                {
                    Debug.Log($"[EnemyStateMachine] Registered state: {state.StateName}");
                }
            }

            // 进入第一个状态
            if (states.Count > 0 && states[0] != null)
            {
                CurrentState = states[0];
                CurrentState.Enter();

                if (_enableLogging)
                {
                    Debug.Log($"[EnemyStateMachine] Entered initial state: {CurrentStateName}");
                }
            }
        }

        /// <summary>
        /// 切换到指定名称的状态
        /// </summary>
        /// <param name="stateName">目标状态名称</param>
        public void ChangeState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogError($"[EnemyStateMachine] Cannot change to null or empty state name!");
                return;
            }

            if (!_states.TryGetValue(stateName, out var newState))
            {
                Debug.LogError($"[EnemyStateMachine] State '{stateName}' not found!");
                return;
            }

            if (CurrentState == newState)
            {
                return;
            }

            if (_enableLogging)
            {
                Debug.Log($"[EnemyStateMachine] {CurrentStateName} -> {stateName}");
            }

            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }

        /// <summary>
        /// 尝试获取指定名称的状态
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        /// <param name="stateName">状态名称</param>
        /// <param name="state">输出的状态实例</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetState<T>(string stateName, out T state) where T : class, IEnemyState
        {
            if (_states.TryGetValue(stateName, out var foundState) && foundState is T typedState)
            {
                state = typedState;
                return true;
            }

            state = null;
            return false;
        }

        /// <summary>
        /// 检查是否包含指定名称的状态
        /// </summary>
        /// <param name="stateName">状态名称</param>
        /// <returns>是否存在</returns>
        public bool HasState(string stateName)
        {
            return _states.ContainsKey(stateName);
        }

        /// <summary>
        /// 每帧更新当前状态
        /// </summary>
        public void Update()
        {
            CurrentState?.Update();
        }

        /// <summary>
        /// 物理更新当前状态
        /// </summary>
        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }

        #endregion
    }
}
