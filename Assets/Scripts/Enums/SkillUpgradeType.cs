using UnityEngine;

public enum SkillUpgradeType
{
    // ----- Dash Tree -----
    Dash,
    Dash_CloneOnStart, // Create a clone when dash starts
    Dash_CloneOnStartAndArrival, // Create a clone when dash starts and when it ends
    Dash_ShardOnStart, // Create a shard when dash starts
    Dash_ShardOnStartAndArrival, // Create a shard when dash starts and when it ends
    
}
