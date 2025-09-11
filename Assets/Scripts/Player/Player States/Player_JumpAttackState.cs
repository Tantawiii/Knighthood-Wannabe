public class Player_JumpAttackState : PlayerState
{
    bool touchedGround;
    public Player_JumpAttackState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        touchedGround = false;

        player.SetVelocity(player.jumpAttackVelocity.x * player.facingDir, player.jumpAttackVelocity.y);
    }

    public override void Update()
    {
        base.Update();

        if (player.groundDetected && !touchedGround)
        {
            touchedGround = true;
            animator.SetTrigger("jumpAttackTrigger");
            player.SetVelocity(0,rb.linearVelocity.y);
        }

        if (triggerCalled && player.groundDetected)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}
