using UnityEngine;

/// <summary>
/// 玩家行走状态
/// 处理水平移动和角色翻转
/// </summary>
public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Enter()
    {
        PlayAnimation("Walk");
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

        // 无水平输入时切换到待机
        if (Mathf.Abs(player.MoveInput.x) < 0.1f)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        // 处理翻转
        player.Flip(player.MoveInput.x);
    }

    public override void FixedUpdate()
    {
        // 应用水平移动
        player.SetVelocityX(player.MoveInput.x * player.MoveSpeed);
    }

    public override void Exit()
    {
        player.SetVelocityX(0);
    }
}
