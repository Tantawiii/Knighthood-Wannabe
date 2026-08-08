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

    // ----- Time Echo Tree -----
    TimeEcho, // Create a time echo that mimics your actions for a short duration
    TimeEcho_SingleAttack, // Time echo can perform a single attack when created
    TimeEcho_MultiAttack, // Time echo can perform N attacks when created
    TimeEcho_DuplicateChance, // Time echo has a chance to create multiple echoes when created
    TimeEcho_HealWisp, // Time echo can create a healing wisp when created
    TimeEcho_CleanseWisp, // Time echo can create a cleansing wisp when created, removes negative effects from the player when created
    TimeEcho_CooldownWisp, // Time echo decreases the cooldown of all skills when created by N seconds.

    // ----- Domain Expansion Tree -----
    Domain_SlowingDown, // Create a domain expansion that allows the player to slow down time for a short duration
    Domain_EchoSpam, // Create a domain expansion that allows the player to spam time echoes for a short duration
    Domain_ShardSpam, // Create a domain expansion that allows the player to spam time shards for a short duration
}
