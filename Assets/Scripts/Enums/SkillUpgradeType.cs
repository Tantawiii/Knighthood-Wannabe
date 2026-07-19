using UnityEngine;

public enum SkillUpgradeType
{
    None,

    // ----- Dash Tree -----
    Dash,
    Dash_CloneOnStart, // Create a clone when dash starts
    Dash_CloneOnStartAndArrival, // Create a clone when dash starts and when it ends
    Dash_ShardOnStart, // Create a shard when dash starts
    Dash_ShardOnStartAndArrival, // Create a shard when dash starts and when it ends
    
    // ----- Shard Tree -----
    Shard,
    Shard_MoveToEnemy, // Move to the nearest enemy when shard is created
    Shard_MultiCast, // Shards can have up to N charges, and can be cast multiple times in a row
    Shard_Teleport, // Teleport to the last shard created
    Shard_TeleportAndHeal, // Teleport to the last shard created and heal back to your hp when you casted the shard
}
