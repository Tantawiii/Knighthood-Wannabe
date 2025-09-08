public class Player_DashState : PlayerState
{
    float originalGravityScale;
    int dashDir;
    public Player_DashState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = player.dashDuration;

        dashDir = player.moveInput.x != 0 ? (int)player.moveInput.x : player.facingDir;
        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0;
    }

    public override void Update()
    {
        base.Update();

        CancelDash();

        player.SetVelocity(player.dashSpeed * dashDir, 0);

        if (stateTimer < 0) 
        { 
            if(player.groundDetected)
                stateMachine.ChangeState(player.idleState);
            else
                stateMachine.ChangeState(player.fallState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        rb.gravityScale = originalGravityScale;
        player.SetVelocity(0, 0);
    }

    private void CancelDash()
    {
        if(player.wallDetected)
        {
            if(player.groundDetected)
                stateMachine.ChangeState(player.idleState);
            else 
                stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
