using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Buff", fileName = "Item effect data - buff")]
public class ItemEffect_Buff : ItemEffect_DataSO
{
    [SerializeField] BuffEffectData[] buffs;
    [SerializeField] float duration = 4f;
    [SerializeField] string buffName = Guid.NewGuid().ToString();

    public override bool CanBeUsed(Player player)
    {        
        if(player.stats.CanApplyBuff(buffName))
        {
            this.player = player;
            return true;
        }
        else
        {
            Debug.Log($"Buff {buffName} is already active and cannot be applied again.");
            return false;
        }
    }

    public override void ExecuteEffect()
    {
        player.stats.ApplyBuff(buffs, duration, buffName);
        player = null;
    }
}
