public class Player_CounterAttackState : PlayerState
{
    Player_Combat player_Combat;
    bool counteredSomebody;
    public Player_CounterAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        player_Combat = player.GetComponent<Player_Combat>();
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = player_Combat.GetCounterRecoveryDuration();

        counteredSomebody = player_Combat.CounterAttackPreformed();

        animator.SetBool("counterAttackPerformed", counteredSomebody);
    }

    public override void Update()
    {
        base.Update();

        player.SetVelocity(0, rb.linearVelocity.y); //Safety measure

        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }

        if (stateTimer < 0f && !counteredSomebody)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}
