using UnityEngine;

/// <summary>
/// 玩家状态基类
/// 定义状态生命周期方法，所有具体状态继承此类
/// </summary>
public abstract class PlayerState
{
    protected PlayerController player;
    protected PlayerStateMachine stateMachine;

    public PlayerState(PlayerController player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 进入状态时调用
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// 每帧更新（Update）
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// 物理更新（FixedUpdate）
    /// </summary>
    public virtual void FixedUpdate() { }

    /// <summary>
    /// 退出状态时调用
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// 播放动画（直接调用Animator.Play）
    /// </summary>
    protected void PlayAnimation(string animName)
    {
        player.Animator.Play(animName);
    }

    /// <summary>
    /// 检查是否可以被打断进入受伤状态
    /// </summary>
    public virtual bool CanBeHurt() => true;
}
