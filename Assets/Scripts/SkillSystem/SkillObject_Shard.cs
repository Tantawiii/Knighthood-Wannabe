using System;
using UnityEngine;

public class SkillObject_Shard : SkillObject_Base
{
    [SerializeField] private GameObject vfxPrefab;
    Transform target;
    float speed;

    void Update()
    {
        if (target == null)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }

    public void MoveTowardsClosestTarget(float speed)
    {
        target = FindClosestTarget();
        this.speed = speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Enemy>() == null) return;

        Explode();
    }

    private void Explode()
    {
        DamageEnemiesInRadius(transform, targetCheckRadius);
        if (vfxPrefab != null)
        {
            Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    public void SetUpShard(float detinationTime)
    {
        Invoke(nameof(Explode), detinationTime);
    }
}
