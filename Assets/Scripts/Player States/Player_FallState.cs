public class Player_FallState : Player_AiredState
{
    public Player_FallState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (player.groundDetected)
        {
            stateMachine.ChangeState(player.idleState);
        }

        if (player.wallDetected)
        {
            stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
