using UnityEngine;
using System.Collections;

/// <summary>
/// 玩家受伤状态
/// 阶段1：红白闪烁1秒 + 击退
/// 阶段2：透明度闪烁1秒（无敌）
/// 结束后扣血并检查死亡
/// </summary>
public class PlayerHurtState : PlayerState
{
    private Vector2 knockbackDirection;
    private float timer;
    private int phase; // 0=击退闪烁, 1=无敌闪烁, 2=结束
    private Coroutine flashCoroutine;

    public PlayerHurtState(PlayerController player, PlayerStateMachine stateMachine) 
        : base(player, stateMachine) { }

    /// <summary>
    /// 设置击退方向
    /// </summary>
    public void SetKnockbackDirection(Vector2 direction)
    {
        knockbackDirection = direction;
    }

    public override void Enter()
    {
        timer = 0f;
        phase = 0;
        player.IsInvincible = true;
        player.SetVelocityX(0);

        // 应用击退
        Vector2 knockback = new Vector2(
            knockbackDirection.x * player.KnockbackForce,
            player.KnockbackForce * 0.5f
        );
        player.Rb.velocity = knockback;

        // 启动闪烁协程
        flashCoroutine = player.StartCoroutine(FlashRoutine());

        // 触发UI受伤反馈
        PlayerStatusUI.Instance?.OnPlayerHurt();
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        // 阶段切换
        if (phase == 0 && timer >= player.FlashDuration)
        {
            phase = 1;
            timer = 0f;
        }
        else if (phase == 1 && timer >= player.InvincibleDuration)
        {
            phase = 2;
            EndHurtState();
        }
    }

    public override void Exit()
    {
        // 停止闪烁协程
        if (flashCoroutine != null)
        {
            player.StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // 恢复正常颜色
        player.SpriteRenderer.color = Color.white;
        player.IsInvincible = false;
    }

    public override bool CanBeHurt() => false;

    /// <summary>
    /// 结束受伤状态
    /// </summary>
    private void EndHurtState()
    {
        // 扣除生命值
        player.ReduceHealth();

        // 检查是否死亡
        if (!player.IsDead())
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

    /// <summary>
    /// 闪烁效果协程
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        float flashInterval = 0.1f;
        bool isRed = false;

        // 阶段1：红白闪烁
        while (phase == 0)
        {
            player.SpriteRenderer.color = isRed ? Color.white : Color.red;
            isRed = !isRed;
            yield return new WaitForSeconds(flashInterval);
        }

        // 阶段2：透明度闪烁 (255-100)
        bool isVisible = true;
        Color normalColor = Color.white;
        Color fadeColor = new Color(1f, 1f, 1f, 100f / 255f);

        while (phase == 1)
        {
            player.SpriteRenderer.color = isVisible ? normalColor : fadeColor;
            isVisible = !isVisible;
            yield return new WaitForSeconds(flashInterval);
        }

        // 恢复正常
        player.SpriteRenderer.color = Color.white;
    }
}
