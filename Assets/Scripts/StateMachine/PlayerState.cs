public abstract class PlayerState : EntityState
{
    protected Player player;
    protected PlayerInputSet input;


    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;

        animator = player.animator;
        rb = player.rb;
        input = player.input;
        entityStats = player.entityStats;
    }

    public override void Update()
    {
        base.Update();
        if (input.Player.Dash.WasPressedThisFrame() && CanDash())
        {
            stateMachine.ChangeState(player.dashState);
        }
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();

        animator.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private bool CanDash()
    {
        if(player.wallDetected || stateMachine.currentState == player.dashState)
            return false;

        return true;
    }
}
