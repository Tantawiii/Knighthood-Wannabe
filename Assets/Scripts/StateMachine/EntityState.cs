using UnityEngine;

public abstract class EntityState
{
    protected StateMachine stateMachine;
    protected string animBoolName;
    protected Animator animator;
    protected Rigidbody2D rb;

    protected float stateTimer;
    protected bool triggerCalled;

    public EntityState(StateMachine stateMachine, string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        animator.SetBool(animBoolName, true);
        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    public virtual void Exit()
    {
        animator.SetBool(animBoolName, false);
    }

    public void CallAnimationTrigger()
    {
        triggerCalled = true;
    }

}
