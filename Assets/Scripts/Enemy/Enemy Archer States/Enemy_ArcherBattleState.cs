using UnityEngine;

public class Enemy_ArcherBattleState : Enemy_BattleState
{
    private bool canFlip; 
    private bool reachedDeadEnd;
    public Enemy_ArcherBattleState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        reachedDeadEnd = false;
    }

    public override void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();

        if(!enemy.groundDetected || enemy.wallDetected)
        {
            reachedDeadEnd = true;
        }

        if (enemy.PlayerDetection())
        {
            UpdateTargetIfNeeded();
            UpdateBattleTimer();
        }

        if (BattleTimeIsOver())
            stateMachine.ChangeState(enemy.idleState);

        if (CanAttack())
        {
            if(!enemy.PlayerDetection() && canFlip)
            {
                enemy.HandleFlip(DirectionToPlayer());
                canFlip = false;
            }

            enemy.SetVelocity(0, rb.linearVelocity.y);
            
            if (WithinAttackRange() && enemy.PlayerDetection())
            {
                canFlip = true;
                lastTimeAttacked = Time.time;
                stateMachine.ChangeState(enemy.attackState);
            }
        }
        else
        {
            // Archer will move away from the player
            bool shouldWalkAway = reachedDeadEnd == false && DistanceToPlayer() < (enemy.attackDistance * .85f);

            if (shouldWalkAway)
            {
                enemy.SetVelocity((enemy.GetBattleMoveSpeed() * -1) * DirectionToPlayer(), rb.linearVelocity.y);
            }
            else
            {
                enemy.SetVelocity(0, rb.linearVelocity.y);

                if (enemy.PlayerDetection() == false)
                    enemy.HandleFlip(DirectionToPlayer());
            }
        }
    }
}
