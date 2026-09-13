using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    public int gold;
    public List<Inventory_Item> itemList;
    public SerializableDictionary<string, int> inventory; // itemSaveID => stacksize
    public SerializableDictionary<string, int> storageItems;
    public SerializableDictionary<string, int> storageMaterials;

    public SerializableDictionary<string, ItemType> equippedItems; // itemSaveID => slotType

    public int skillPoints;
    public SerializableDictionary<string, bool> skillTreeUI; // skillName => isUnlocked
    public SerializableDictionary<SkillType, SkillUpgradeType> skillUpgrades; // skillType => upgradeType

    // public Vector3 savedCheckPoint; 

    public SerializableDictionary<string,bool> unlockedCheckpoints; // checkpoint id => isUnlocked
    public SerializableDictionary<string, Vector3> inScenePortals; // scene name => portal position

    public string portalDestinationSceneName;
    public bool returningFromTown;
    public GameData()
    {
        inventory = new SerializableDictionary<string, int>();
        storageItems = new SerializableDictionary<string, int>();
        storageMaterials = new SerializableDictionary<string, int>();

        equippedItems = new SerializableDictionary<string, ItemType>();

        skillTreeUI = new SerializableDictionary<string, bool>();
        skillUpgrades = new SerializableDictionary<SkillType, SkillUpgradeType>();

        unlockedCheckpoints = new SerializableDictionary<string,bool>();
        inScenePortals = new SerializableDictionary<string, Vector3>();
    }
}
