using UnityEngine;

public class Enemy_BattleState : EnemyState
{
    Transform player;
    float lastTimeWasInBattle;
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

        if (ShouldRetreat())
        {
            rb.linearVelocity = new Vector2(enemy.retreatVelocity.x * -DirectionToPlayer(), enemy.retreatVelocity.y);
            enemy.HandleFlip(DirectionToPlayer());
        }
    }

    public override void Update()
    {
        base.Update();

        //if (enemy.PlayerDetection())
        //    UpdateBattleTimer();

        if (BattleTimeIsOver())
            stateMachine.ChangeState(enemy.idleState);

        if (WithinAttackRange() && enemy.PlayerDetection())
            stateMachine.ChangeState(enemy.attackState);
        else
            enemy.SetVelocity(enemy.battleMoveSpeed * DirectionToPlayer(), rb.linearVelocity.y);

    }

    private void UpdateBattleTimer() => lastTimeWasInBattle = Time.time;

    private bool BattleTimeIsOver() => Time.time > lastTimeWasInBattle + enemy.battleTimeDuration;

    private bool WithinAttackRange() => DistanceToPlayer() < enemy.attackDistance;

    private bool ShouldRetreat() => DistanceToPlayer() < enemy.minRetreatDistance;

    private float DistanceToPlayer()
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
