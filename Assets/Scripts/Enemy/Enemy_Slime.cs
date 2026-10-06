using UnityEngine;

public class Enemy_Slime : Enemy, ICounterable
{
    [Header("Slime Specific")]
    [SerializeField] private GameObject smallSlimePrefab;
    [SerializeField] private int amountOfSmallSlimesToSpawn = 2;
    [SerializeField] private bool hasStunRecovery = true;
    public Enemy_SlimeDeadState slimeDeadState { get; set; }
    public bool CanBeCountered { get => canBeStunned; }

    protected override void Awake()
    {
        base.Awake();

        idleState = new Enemy_IdleState(this, stateMachine, "idle");
        moveState = new Enemy_MoveState(this, stateMachine, "move");
        attackState = new Enemy_AttackState(this, stateMachine, "attack");
        battleState = new Enemy_BattleState(this, stateMachine, "battle");
        stunnedState = new Enemy_StunnedState(this, stateMachine, "stunned");

        slimeDeadState = new Enemy_SlimeDeadState(this, stateMachine, "idle");

        animator.SetBool("hasStunRecovery", hasStunRecovery);
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
    }

    public override void EntityDeath()
    {
        stateMachine.ChangeState(slimeDeadState);
    }

    [ContextMenu("Stun Enemy")]
    public void HandleCounter()
    {
        // unnecessary due to having check similar to it in player combat
        //if (!CanBeCountered)
        //    return;

        stateMachine.ChangeState(stunnedState);
    }

    public void CreateSlimeOnDeath()
    {
        if(smallSlimePrefab == null)
        {
            return;
        }

        for(int i = 0; i < amountOfSmallSlimesToSpawn; i++)
        {
            GameObject newSlime = Instantiate(smallSlimePrefab, transform.position, Quaternion.identity);
            Enemy_Slime slimeScript = newSlime.GetComponent<Enemy_Slime>();

            slimeScript.entityStats.AdjustStatSetup(entityStats.resourceGroup, entityStats.offenseGroup, entityStats.defenseGroup, 0.6f, 1.2f);
            slimeScript.ApplyRespawnVelocity();
            slimeScript.StartBattleCheck(player);
        }
    }


    private void ApplyRespawnVelocity()
    {
        Vector2 velocity = new Vector2(stunnedVelocity.x * Random.Range(-1, 1), stunnedVelocity.y * Random.Range(1, 2));
        SetVelocity(velocity.x, velocity.y);
    }

    private void StartBattleCheck(Transform player)
    {
        EnterBattleState(player);
        InvokeRepeating(nameof(ReEnterBattleState), 0f, .3f);
    }
    
    private void ReEnterBattleState()
    {
        if(stateMachine.currentState == battleState || stateMachine.currentState == attackState)
        {
            CancelInvoke(nameof(ReEnterBattleState));
            return;
        }

        stateMachine.ChangeState(battleState);
    }
}
