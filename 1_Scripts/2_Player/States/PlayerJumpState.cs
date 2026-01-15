using UnityEngine;

/// <summary>
/// 玩家跳跃状态
/// 支持二段跳，空中可移动
/// </summary>
public class PlayerJumpState : PlayerState
{
    private bool hasReleasedJump;

    public PlayerJumpState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Enter()
    {
        PlayAnimation("Walk");
        hasReleasedJump = false;

        // 执行跳跃
        player.JumpCount++;
        player.SetVelocityY(player.JumpForce);
    }

    public override void Update()
    {
        // 检查攻击输入
        if (player.AttackInputPressed)
        {
            stateMachine.ChangeState(player.AttackState);
            return;
        }

        // 检测跳跃键释放（用于二段跳判定）
        if (!player.CheckJumpInput())
        {
            hasReleasedJump = true;
        }

        // 二段跳检测
        if (hasReleasedJump && player.CheckJumpInput() && player.JumpCount < player.MaxJumpCount)
        {
            player.JumpCount++;
            player.SetVelocityY(player.JumpForce);
            hasReleasedJump = false;
        }

        // 落地检测
        if (player.IsGrounded && player.Rb.velocity.y <= 0)
        {
            if (Mathf.Abs(player.MoveInput.x) > 0.1f)
            {
                stateMachine.ChangeState(player.WalkState);
            }
            else
            {
                stateMachine.ChangeState(player.IdleState);
            }
            return;
        }

        // 空中翻转
        if (Mathf.Abs(player.MoveInput.x) > 0.1f)
        {
            player.Flip(player.MoveInput.x);
        }
    }

    public override void FixedUpdate()
    {
        // 空中水平移动
        player.SetVelocityX(player.MoveInput.x * player.MoveSpeed);
    }
}
