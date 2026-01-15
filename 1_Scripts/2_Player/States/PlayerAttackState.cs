using UnityEngine;

/// <summary>
/// 玩家攻击状态
/// 播放攻击动画，动画结束后返回
/// </summary>
public class PlayerAttackState : PlayerState
{
    private float attackDuration = 0.5f;
    private float timer;
    private bool durationCached = false;

    public PlayerAttackState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    public override void Enter()
    {
        PlayAnimation("Attack");
        timer = 0f;
        player.SetVelocityX(0);

        // 首次进入时缓存攻击动画时长
        if (!durationCached)
        {
            CacheAttackDuration();
        }
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        // 动画结束后切换状态
        if (timer >= attackDuration)
        {
            if (Mathf.Abs(player.MoveInput.x) > 0.1f)
            {
                stateMachine.ChangeState(player.WalkState);
            }
            else
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }
    }

    public override bool CanBeHurt() => true;

    /// <summary>
    /// 从AnimatorController中获取Attack动画时长并缓存
    /// </summary>
    private void CacheAttackDuration()
    {
        RuntimeAnimatorController controller = player.Animator.runtimeAnimatorController;
        if (controller == null) return;

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name == "Attack")
            {
                attackDuration = clip.length;
                durationCached = true;
                break;
            }
        }
    }
}
