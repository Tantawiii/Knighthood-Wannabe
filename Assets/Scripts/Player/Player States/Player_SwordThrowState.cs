using UnityEngine;

public class Player_SwordThrowState : PlayerState
{
    public Player_SwordThrowState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(0, rb.linearVelocity.y);

        if (input.Player.Attack.WasPressedThisFrame())
            animator.SetBool("swordThrowPerformed", true);

        if (input.Player.RangeAttack.WasReleasedThisFrame() || triggerCalled)
            stateMachine.ChangeState(player.idleState);
    }

    public override void Exit()
    {
        base.Exit();
        animator.SetBool("swordThrowPerformed", false);
    }
}
