using UnityEngine;

public enum SkillUpgradeType
{
    None,

    // ----- Dash Tree -----
    Dash, // Dash forward a short distance
    Dash_CloneOnStart, // Create a clone when dash starts
    Dash_CloneOnStartAndArrival, // Create a clone when dash starts and when it ends
    Dash_ShardOnStart, // Create a shard when dash starts
    Dash_ShardOnStartAndArrival, // Create a shard when dash starts and when it ends
    
    // ----- Shard Tree -----
    Shard, // Create a shard
    Shard_MoveToEnemy, // Move to the nearest enemy when shard is created
    Shard_MultiCast, // Shards can have up to N charges, and can be cast multiple times in a row
    Shard_Teleport, // Teleport to the last shard created
    Shard_TeleportHpRewind, // Teleport to the last shard created and heal back to your hp when you casted the shard

     // ----- Sword Throw Tree -----
    SwordThrow, // Throw a sword that can hit an enemy
    SwordThrow_Spin, // Spin the sword when thrown, allowing it to hit multiple enemies static placement
    SwordThrow_Pierce, // Sword can pierce through enemies, allowing it to hit multiple enemies in a line
    SwordThrow_Bounce, // Sword can bounce off enemies, allowing it to hit multiple enemies in a single throw
}
