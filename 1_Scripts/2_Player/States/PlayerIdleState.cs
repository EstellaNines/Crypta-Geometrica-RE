using UnityEngine;

/// <summary>
/// 玩家待机状态
/// 无输入时播放待机动画，检测状态转换
/// </summary>
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Enter()
    {
        PlayAnimation("Idle");
        player.SetVelocityX(0);
    }

    public override void Update()
    {
        // 检查攻击输入
        if (player.AttackInputPressed)
        {
            stateMachine.ChangeState(player.AttackState);
            return;
        }

        // 检查跳跃输入
        if (player.CheckJumpInput() && player.IsGrounded)
        {
            stateMachine.ChangeState(player.JumpState);
            return;
        }

        // 检查移动输入
        if (Mathf.Abs(player.MoveInput.x) > 0.1f)
        {
            stateMachine.ChangeState(player.WalkState);
            return;
        }
    }
}
