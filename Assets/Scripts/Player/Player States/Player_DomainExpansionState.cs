using UnityEngine;

public class Player_DomainExpansionState : PlayerState
{
    private Vector2 originalPosition;
    private float originalGravity;
    private float maxDistanceToGoUp;

    private bool isLevitating;
    private bool createdDomain;

    public Player_DomainExpansionState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        originalPosition = player.transform.position;
        originalGravity = rb.gravityScale;
        maxDistanceToGoUp = GetAvailableRiseDistance();

        player.SetVelocity(0, player.riseSpeed);
    }

    public override void Update()
    {
        base.Update();

        if (Vector2.Distance(originalPosition, player.transform.position) >= maxDistanceToGoUp && isLevitating == false)
        {
            Levitate();
        }

        if(isLevitating)
        {
            // Skill Manager cast skills

            if(stateTimer <= 0)
            {
                stateMachine.ChangeState(player.idleState);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();

        rb.gravityScale = originalGravity;
        isLevitating = false;
        createdDomain = false;
    }

    private void Levitate()
    {
        isLevitating = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;

        stateTimer = 2;
        // Get Levitation Duration
        if(createdDomain == false)
        {
            createdDomain = true;
            skillManager.domainExpansion.CreateDomain();
        }
    }

    private float GetAvailableRiseDistance()
    {
        RaycastHit2D hit = Physics2D.Raycast(player.transform.position, Vector2.up, player.riseMaxDistance, player.GetWhatIsGround());
        return hit.collider != null ? hit.distance - 1f : player.riseMaxDistance; // Subtracting 1f to ensure the player doesn't collide with the ceiling
    }
}
