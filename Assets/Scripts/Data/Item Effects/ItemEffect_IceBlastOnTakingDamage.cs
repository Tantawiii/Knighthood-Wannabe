using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Ice Blast On Taking Damage", fileName = "Item Effect - Ice Blast On Taking Damage")]
public class ItemEffect_IceBlastOnTakingDamage : ItemEffect_DataSO
{
    [SerializeField] private ElementalEffectData effectData;
    [SerializeField] private float iceDamage;
    [SerializeField] private LayerMask whatIsEnemy;
    [Space]
    [SerializeField] private float healthPercentTrigger = .25f;
    [SerializeField] private float cooldown = 40f;
    private float lastTimeUsed = -999f;

    [Header("VFX Objects")]
    [SerializeField] private GameObject iceBlastVFXPrefab;
    [SerializeField] private GameObject onHitVFXPrefab;

    public override void ExecuteEffect()
    {
        bool canUse = Time.time >= lastTimeUsed + cooldown;
        bool reachedThreshold = player.health.GetHealthPercent() <= healthPercentTrigger;

        if (canUse && reachedThreshold)
        {
            player.VFX.CreateEffectOf(iceBlastVFXPrefab, player.transform);
            lastTimeUsed = Time.time;
            DamageEnemiesWithIce();
        }
    }

    private void DamageEnemiesWithIce()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(player.transform.position, 1.5f, whatIsEnemy);

        foreach (var enemy in enemies)
        {
            IDamagable damagable = enemy.GetComponent<IDamagable>();

            if (damagable == null)
            {
                continue;
            }
            
            bool targetGotHit = damagable.TakeDamage(0, iceDamage, ElementType.Ice, player.transform);

            Entity_StatusHandler statusHandler = enemy.GetComponent<Entity_StatusHandler>();

            statusHandler?.ApplyStatusEffect(ElementType.Ice, effectData);

            if (targetGotHit)
            {
                player.VFX.CreateEffectOf(onHitVFXPrefab, enemy.transform);
            }
        }
    }

    public override void Subscribe(Player player)
    {
        base.Subscribe(player);
        player.health.OnTakingDamage += ExecuteEffect;
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();
        player.health.OnTakingDamage -= ExecuteEffect;
        player = null;
    }
}
