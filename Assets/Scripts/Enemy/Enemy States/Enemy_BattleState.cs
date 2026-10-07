using UnityEngine;

public class Enemy_BattleState : EnemyState
{
    protected Transform player;
    protected Transform lastTarget;
    protected float lastTimeWasInBattle;
    protected float lastTimeAttacked;
    public Enemy_BattleState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        UpdateBattleTimer();

        player ??= enemy.GetPlayerReference();
        /*
         * if (player == null)
         *    enemy.GetPlayerReference();
         *
         */

        enemy.HandleFlip(DirectionToPlayer());

        if (ShouldRetreat())
        {
            rb.linearVelocity = new Vector2((enemy.retreatVelocity.x * enemy.activeSlowMultipler) * -DirectionToPlayer(), enemy.retreatVelocity.y);
        }
    }

    public override void Update()
    {
        base.Update();

        if (enemy.PlayerDetection())
        {
            UpdateTargetIfNeeded();
            UpdateBattleTimer();
        }

        if (BattleTimeIsOver())
            stateMachine.ChangeState(enemy.idleState);

        if (WithinAttackRange() && enemy.PlayerDetection() && CanAttack())
        {
            lastTimeAttacked = Time.time;
            stateMachine.ChangeState(enemy.attackState);
        }
        else
        {
            float xVelocity = enemy.chasePlayer ? enemy.GetBattleMoveSpeed(): 0.001f;
            enemy.SetVelocity(xVelocity * DirectionToPlayer() , rb.linearVelocity.y);
        }

    }

    protected bool CanAttack() => Time.time > lastTimeAttacked + enemy.attackCooldown;

    protected void UpdateTargetIfNeeded()
    {
        if(enemy.PlayerDetection() == false)
            return;
        Transform newTarget = enemy.GetPlayerReference().transform;
        if (newTarget != lastTarget)
        {
            lastTarget = newTarget;
            player = newTarget;
        }
    }

    protected void UpdateBattleTimer() => lastTimeWasInBattle = Time.time;

    protected bool BattleTimeIsOver() => Time.time > lastTimeWasInBattle + enemy.battleTimeDuration;

    protected bool WithinAttackRange() => DistanceToPlayer() < enemy.attackDistance;

    private bool ShouldRetreat() => DistanceToPlayer() < enemy.minRetreatDistance;

    protected float DistanceToPlayer()
    {
        if(player == null)
            return float.MaxValue;

        return Mathf.Abs(player.position.x - enemy.transform.position.x);
    }

    protected int DirectionToPlayer()
    {
        if (player == null)
            return 0;

        float verticalDistance = Mathf.Abs(player.position.y - enemy.transform.position.y);
        float horizontalDistance = Mathf.Abs(player.position.x - enemy.transform.position.x);

        // Ignore movement if player is too high and very close horizontally
        if (verticalDistance > 1.5f && horizontalDistance < 1f)
            return 0;

        return player.position.x > enemy.transform.position.x ? 1 : -1;
    }
}
