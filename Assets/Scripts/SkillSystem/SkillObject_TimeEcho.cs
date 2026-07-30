using UnityEngine;

public class SkillObject_TimeEcho : SkillObject_Base
{
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private LayerMask groundLayer;
    private Skill_TimeEcho echoManager;

    public void SetUpEcho(Skill_TimeEcho echoManager)
    {
        this.echoManager = echoManager;

        Invoke(nameof(HandleDeath), echoManager.GetTimeEchoDuration());
    }   

    private void Update()
    {
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        StopHorizontalMovement();
    }

    public void HandleDeath()
    {
        Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void StopHorizontalMovement()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
        if (hit.collider != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
}
