using UnityEngine;

public class Enemy_Slime : Enemy, ICounterable
{
    [Header("Slime Specific")]
    [SerializeField] private bool hasStunRecovery = true;
    public bool CanBeCountered { get => canBeStunned; }

    protected override void Awake()
    {
        base.Awake();

        idleState = new Enemy_IdleState(this, stateMachine, "idle");
        moveState = new Enemy_MoveState(this, stateMachine, "move");
        attackState = new Enemy_AttackState(this, stateMachine, "attack");
        battleState = new Enemy_BattleState(this, stateMachine, "battle");
        stunnedState = new Enemy_StunnedState(this, stateMachine, "stunned");

        deadState = new Enemy_DeadState(this, stateMachine, "idle");
        
        animator.SetBool("hasStunRecovery", hasStunRecovery);
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
    }

    [ContextMenu("Stun Enemy")]
    public void HandleCounter()
    {
        // unnecessary due to having check similar to it in player combat
        //if (!CanBeCountered)
        //    return;

        stateMachine.ChangeState(stunnedState);
    }
}
