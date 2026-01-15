using UnityEngine;

/// <summary>
/// 玩家死亡状态
/// 播放死亡动画，禁用输入，终态
/// </summary>
public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Enter()
    {
        PlayAnimation("Dead");
        player.SetVelocityX(0);

        // 禁用碰撞（可选）
        // player.GetComponent<Collider2D>().enabled = false;

        Debug.Log("[Player] 玩家死亡");

        // TODO: 触发死亡事件，通知GameManager等
    }

    public override void Update()
    {
        // 死亡状态不响应任何输入
        // 可在此检测动画结束后触发重生或游戏结束逻辑
    }

    public override bool CanBeHurt() => false;
}
