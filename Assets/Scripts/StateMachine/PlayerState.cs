public abstract class PlayerState : EntityState
{
    protected Player player;
    protected PlayerInputSet input;
    protected Player_SkillManager skillManager;


    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;

        animator = player.animator;
        rb = player.rb;
        input = player.input;
        entityStats = player.entityStats;
        skillManager = player.skillManager;
    }

    public override void Update()
    {
        base.Update();
        if (input.Player.Dash.WasPressedThisFrame() && CanDash())
        {
            skillManager.dash.SetSkillOnCooldown();
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
        if(skillManager.dash.CanUseSkill() == false)
            return false;

        if(player.wallDetected || stateMachine.currentState == player.dashState)
            return false;

        return true;
    }
}
