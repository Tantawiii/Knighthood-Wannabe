using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Buff", fileName = "Item effect data - buff")]
public class ItemEffect_Buff : ItemEffect_DataSO
{
    [SerializeField] BuffEffectData[] buffs;
    [SerializeField] float duration = 4f;
    [SerializeField] string buffName = Guid.NewGuid().ToString();

    Player_Stats playerStats;
    
    public override bool CanBeUsed()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<Player_Stats>();
        }
        
        if(playerStats.CanApplyBuff(buffName))
        {
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
        playerStats.ApplyBuff(buffs, duration, buffName);
    }
}
