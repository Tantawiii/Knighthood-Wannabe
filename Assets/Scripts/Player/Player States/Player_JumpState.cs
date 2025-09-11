public class Player_JumpState : Player_AiredState
{
    public Player_JumpState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(rb.linearVelocity.x, player.jumpForce);
    }

    public override void Update()
    {
        base.Update();
        // We need to be sure of we are not into jump attack state, because I set y in jumpAttackVelocity with negative value
        // So it will change in same frame count because the first part of the if condition is true.
        if(rb.linearVelocity.y < 0 && stateMachine.currentState != player.jumpAttackState)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }
}
