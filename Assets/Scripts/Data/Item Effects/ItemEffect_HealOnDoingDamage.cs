using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Heal on Doing Damage", fileName = "Item effect data - heal on doing damage")]
public class ItemEffect_HealOnDoingDamage : ItemEffect_DataSO
{
    [SerializeField] private float healPercentagePerAttack = .2f;

    public override void Subscribe(Player player)
    {
        base.Subscribe(player);

        player.combat.OnDoingPhysicalDamage += HealOnDealingDamage;
    }

    public override void Unsubscribe()
    {
        base.Unsubscribe();

        player.combat.OnDoingPhysicalDamage -= HealOnDealingDamage;
        player = null;
    }

    public void HealOnDealingDamage(float damage)
    {
        player.health.IncreaseHealth(damage * healPercentagePerAttack);
    }

}
