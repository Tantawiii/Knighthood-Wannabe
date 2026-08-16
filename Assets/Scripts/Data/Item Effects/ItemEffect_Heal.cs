using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Heal", fileName = "Item effect data - heal")]
public class ItemEffect_Heal : ItemEffect_DataSO
{
    [SerializeField] private float healPercentage = .1f;

    public override void ExecuteEffect()
    {
        Player player = FindFirstObjectByType<Player>();

        float healAmount = player.entityStats.GetMaxHealth() * healPercentage;

        player.health.IncreaseHealth(healAmount);
    }
}
