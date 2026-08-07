using UnityEngine;

public class SkillObject_TimeEcho : SkillObject_Base
{
    [SerializeField] private float wispMoveSpeed = 15f;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private LayerMask groundLayer;

    private bool shouldMoveToPlayer;

    private Skill_TimeEcho echoManager;
    private TrailRenderer wispTrail;

    public int maxAttacks { get; private set; }

    public void SetUpEcho(Skill_TimeEcho echoManager)
    {
        this.echoManager = echoManager;
        playerStats = echoManager.player.entityStats;
        ownerTransform = echoManager.player.transform.root;
        damageScaleData = echoManager.damageScaleData;
        maxAttacks = echoManager.GetMaxAttacks();

        Invoke(nameof(HandleDeath), echoManager.GetTimeEchoDuration());
        FlipToTarget();
        
        wispTrail = GetComponentInChildren<TrailRenderer>();
        wispTrail.gameObject.SetActive(false);

        anim.SetBool("canAttack", maxAttacks > 0);
    }   

    private void Update()
    {
        if(shouldMoveToPlayer)
        {
            HandleWispMovement();
        }
        else
        {
            anim.SetFloat("yVelocity", rb.linearVelocity.y);
            StopHorizontalMovement();
        }
    }

    private void HandleWispMovement()
    {
        transform.position = Vector2.MoveTowards(transform.position, ownerTransform.position, wispMoveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, ownerTransform.position) < 0.5f)
        {
            HandlePlayerTouch();
            Destroy(gameObject);
        }
    }

    private void HandlePlayerTouch()
    {
        
    }

    private void FlipToTarget()
    {
        Transform target = FindClosestTarget();

        if (target != null && target.position.x < transform.position.x)
        {
            transform.Rotate(0, 180, 0);
        }
    }

    public void PerformAttack()
    {
        DamageEnemiesInRadius(targetCheck, 1);

        if (!targetGotHit)
        {
            return;
        }

        bool canDuplicate = Random.value <= echoManager.GetDuplicateChance();

        float xOffset = transform.position.x < lastTarget.position.x ? 1 : -1;

        if(canDuplicate)
        {
            echoManager.CreateTimeEcho(lastTarget.position + new Vector3(xOffset, 0, 0));
        }
    }

    public void HandleDeath()
    {
        Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if(echoManager.ShouldBeWisp())
        {
            TurnIntoWisp();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void TurnIntoWisp()
    {
        shouldMoveToPlayer = true;
        anim.gameObject.SetActive(false);
        wispTrail.gameObject.SetActive(true);
        rb.simulated = false;
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
