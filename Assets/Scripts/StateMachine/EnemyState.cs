public abstract class EnemyState : EntityState
{
    protected Enemy enemy;
    protected EnemyState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.enemy = enemy;

        animator = enemy.animator;
        rb = enemy.rb;
        entityStats = enemy.entityStats;
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();

        float battleAnimSpeedMultiplier = enemy.battleMoveSpeed / enemy.moveSpeed;

        animator.SetFloat("battleAnimSpeedMultiplier", battleAnimSpeedMultiplier);
        animator.SetFloat("moveAnimSpeedMultiplier", enemy.moveAnimSpeedMultiplier);
        animator.SetFloat("xVelocity", rb.linearVelocity.x);
    }
}
