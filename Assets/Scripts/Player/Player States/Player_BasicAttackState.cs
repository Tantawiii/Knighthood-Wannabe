using UnityEngine;

public class Player_BasicAttackState : PlayerState
{
    float attackVelocityTimer;
    float lastAttackTime;

    bool comboAttackQueued;

    const int FirstComboIndex = 1; //We start with the animator's combo index which is 1.
    int attackDir;
    int comboIndex = 1;
    int comboLimit = 3;
    public Player_BasicAttackState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
        if(comboLimit != player.attackVelocity.Length)
            comboLimit = player.attackVelocity.Length;
    }

    public override void Enter()
    {
        base.Enter();

        comboAttackQueued = false;

        ResetComboIndexIfNeeded();

        SyncAttackSpeed();

        // Define attack direction according to player input.
        attackDir = player.moveInput.x != 0 ? (int) player.moveInput.x : player.facingDir;

        //if(player.moveInput.x != 0)
        //    attackDir = (int)player.moveInput.x;
        //else 
        //    attackDir = player.facingDir;

        animator.SetInteger("basicAttackIndex", comboIndex);

        ApplyAttackVelocity();
    }

    public override void Update()
    {
        base.Update();
        HandleAttackVelocity();

        if (input.Player.Attack.WasPressedThisFrame())
        {
            QueueNextAttack();
        }

        if (triggerCalled)
        {
            HandleStateExit();
        }
    }

    public override void Exit()
    {
        base.Exit();

        comboIndex++;
        lastAttackTime = Time.time;
    }
    private void HandleStateExit()
    {
        if (comboAttackQueued)
        {
            animator.SetBool(animBoolName, false);
            player.EnterAttackStateWithDelay();
        }
        else
            stateMachine.ChangeState(player.idleState);
    }

    private void QueueNextAttack()
    {
        if (comboIndex < comboLimit)
            comboAttackQueued = true;
    }

    private void ResetComboIndexIfNeeded()
    {
        if (comboIndex > comboLimit || Time.time > lastAttackTime + player.comboResetTime)
        {
            comboIndex = FirstComboIndex;
        }
    }

    private void HandleAttackVelocity()
    {
        attackVelocityTimer -= Time.deltaTime;

        if (attackVelocityTimer < 0)
        {
            player.SetVelocity(0, rb.linearVelocity.y);
        }
    }

    private void ApplyAttackVelocity()
    {
        Vector2 attackVelocity = player.attackVelocity[comboIndex - 1];
        attackVelocityTimer = player.attackVelocityDuration;
        player.SetVelocity(attackVelocity.x * attackDir, attackVelocity.y);
    }
}
