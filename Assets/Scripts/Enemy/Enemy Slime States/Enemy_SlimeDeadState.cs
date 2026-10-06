using UnityEngine;

public class Enemy_SlimeDeadState : Enemy_DeadState
{
    private Enemy_Slime slime;
    public Enemy_SlimeDeadState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
        slime = enemy as Enemy_Slime;
    }

    public override void Enter()
    {
        base.Enter();

        // Create new smaller slime on death
        slime.CreateSlimeOnDeath();
    }
}
