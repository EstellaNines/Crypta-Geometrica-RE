using UnityEngine;

/// <summary>
/// 玩家状态机
/// 管理状态切换和状态生命周期调用
/// </summary>
public class PlayerStateMachine
{
    public PlayerState CurrentState { get; private set; }

    /// <summary>
    /// 初始化状态机，设置初始状态
    /// </summary>
    public void Initialize(PlayerState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
    }

    /// <summary>
    /// 切换到新状态
    /// </summary>
    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
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
}
