# 11 — Data: ScriptableObjects, Item Effects, and Quest Data

## Overview

`Assets/Scripts/Data/` is the "design" layer — ScriptableObjects (see [Concepts-Glossary](Concepts-Glossary.md#scriptableobjects)) and small `[Serializable]` data classes that earlier chapters have already been reading from constantly, since almost every runtime system is built directly on top of one of these. This chapter pulls all of that together in one place: a quick consolidated recap of the pieces already covered piecemeal (`AttackData`/`DamageScaleData`/`ElementalEffectData` from `02-Entity.md`; `Item_DataSO`/`Equipment_DataSO`/`ItemList_DataSO`/`ItemEffect_DataSO`'s base from `07-InventorySystem.md`; `Skill_DataSO` from `08-SkillSystem.md`; `BuffEffectData` from `10-InteractiveObjects.md`), plus full, new coverage of `Stat_SetupSO`, all seven concrete `ItemEffect_DataSO` subclasses, and the Quest Data trio. `AudioDatabase_DataSO` is deliberately left for the later `Audio` doc, alongside `AudioManager`/`AudioRangeController`.

---

## Already covered — quick recap with links

- **`AttackData`/`DamageScaleData`/`ElementalEffectData`** — the physical/elemental damage-scaling bundle every attack (melee, skill) is built from. Full coverage in `02-Entity.md`.
- **`Item_DataSO`/`Equipment_DataSO`/`ConsumableItem_DataSO`/`ItemList_DataSO`** — the shared item "design" asset hierarchy, and `ItemModifier` (the `{StatType, float}` pair driving equipment bonuses). Full coverage in `07-InventorySystem.md`.
- **`ItemEffect_DataSO`** (the base class: `CanBeUsed`/`ExecuteEffect`/`Subscribe`/`Unsubscribe`) — introduced in `07-InventorySystem.md`; every concrete subclass below builds on it.
- **`Skill_DataSO`/`UpgradeData`** — the skill-tree-node asset feeding `Skill_Base.SetSkillUpgrade`. Full coverage in `08-SkillSystem.md`.
- **`BuffEffectData`** — the tiny `{StatType Type; float Value;}` pair used by both `Player_Stats.ApplyBuff` and `Object_Buff`. Introduced in `10-InteractiveObjects.md`.

### One formula worth finishing now: `Item_DataSO.GetDropChance()`

```csharp
public float GetDropChance()
{
    float maxRarity = 1000f;
    float chance = (maxRarity - itemRarity + 1) / maxRarity * 100;
    return Mathf.Min(chance, maxDropChance);
}
```
`02-Entity.md` covered *how* `Entity_DropManager.RollDrops()` uses this value, but not the formula itself — worth closing that loop. `itemRarity` (0–1000) does **double duty** across the drop system:

- Here, it's inversely proportional to drop chance — an item with `itemRarity` near `0` rolls a chance near 100% (very common), while one near `1000` rolls a chance near `0.1%` (very rare).
- But it's the *same* number `Entity_DropManager.RollDrops()` later treats as a **rarity budget cost** once an item *does* roll successfully — the greedy "spend from `maxRarityAmount`, rarest-first" fill described in `02-Entity.md`.

So one field controls both "how likely is this to be offered at all" and "how much of the loot budget it eats once it's picked" — a genuinely dual-purpose value worth being clear-eyed about rather than assuming it only does one job. `maxDropChance` (default 65%) is a hard ceiling — even an `itemRarity` of `1` never guarantees a 100% drop.

### `Item_DataSO`'s `saveID` — a different "stable ID" technique than you might expect

```csharp
private void OnValidate()
{
    dropChance = GetDropChance();
    #if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        saveID = AssetDatabase.AssetPathToGUID(path);
    #endif
}
```
Worth contrasting directly with `Object_Checkpoint.checkpointID` from `10-InteractiveObjects.md`, which generates a **random** GUID (`System.Guid.NewGuid()`) *once*, gated by "only if currently blank." `saveID` here does something different: it re-derives itself from `AssetDatabase.AssetPathToGUID(path)` **every single time** `OnValidate` runs, with no blank-check at all. This ties the ID to Unity's own internal per-asset GUID (the same one tracked in the asset's `.meta` file) rather than a value this script invents — which means it survives the asset being **renamed or moved** within the project, since Unity's AssetDatabase GUID is attached to the asset itself, not its path or name. That's the entire reason to use this approach over a random GUID or the file path directly — a meaningfully different, and for this specific purpose more robust, technique for the same underlying problem: give this asset a stable identity to save/load by. `Quest_DataSO.questSaveID` (below) uses the exact same `AssetPathToGUID` technique.

---

## `Stat_SetupSO.cs` — the default-stats preset

```csharp
[CreateAssetMenu(menuName = "Game Stat SetUp/Stat SetUp", fileName = "Default Stat SetUp")]
public class Stat_SetupSO : ScriptableObject
{
    public float maxHealth = 100;
    public float healthRegen;
    public float attackSpeed = 1;
    public float damage = 10;
    // ... one float per stat, grouped by [Header] to mirror the Stat_*Group layout
}
```
A flat list of plain `float`s — one per stat, grouped with `[Header(...)]`s that deliberately mirror the `Stat_MajorGroup`/`Stat_OffenseGroup`/`Stat_DefenseGroup`/`Stat_ResourceGroup` split from `06-StatSystem.md`, purely for Inspector readability (the fields themselves are just flat data, no nested structure). This is exactly what `Entity_Stats.ApplyDefaultStatSetup()` (`02-Entity.md`) copies field-by-field into a live entity's actual `Stat` objects via `SetBaseValue(...)` — a `Stat_SetupSO` asset is a reusable "starting numbers" preset (e.g. "Skeleton base stats," "Player base stats") that multiple entities of the same kind can all point at.

---

## `ItemEffect_DataSO` subclasses — the Strategy pattern, seven times over

Every one of these shares the same base "slot" on an `Item_DataSO` (`itemEffect`), and which behavior actually runs is decided entirely by *which asset* is dragged into that field — CLAUDE.md's Strategy-pattern description, now shown concretely across all seven implementations.

### `ItemEffect_Heal` — the simplest consumable

```csharp
public override void ExecuteEffect()
{
    Player player = FindFirstObjectByType<Player>();
    float healAmount = player.stats.GetMaxHealth() * healPercentage;
    player.health.IncreaseHealth(healAmount);
}
```
Only overrides `ExecuteEffect()` — heals a percentage of max health (not a flat number), triggered directly by `Inventory_Base.TryUseItem` (`07-InventorySystem.md`) when a healing potion is consumed.

### `ItemEffect_Buff` — smuggling a parameter through `CanBeUsed`

```csharp
[SerializeField] string buffName = Guid.NewGuid().ToString();

public override bool CanBeUsed(Player player)
{
    if (player.stats.CanApplyBuff(buffName))
    {
        this.player = player;
        return true;
    }
    Debug.Log($"Buff {buffName} is already active and cannot be applied again.");
    return false;
}

public override void ExecuteEffect()
{
    player.stats.ApplyBuff(buffs, duration, buffName);
    player = null;
}
```
Two details worth understanding.

First, `buffName`'s **field initializer** — `= Guid.NewGuid().ToString()` — runs once, when this ScriptableObject asset is first created, baking in a random default name so a freshly-created "buff potion" asset doesn't accidentally collide with some *other* buff's name (recall `06-StatSystem.md`/`03-Player.md`: `buffName` doubles as the `Stat.AddModifier` `source` tag, so uniqueness matters). It's still a `[SerializeField]`, so it can be — and, for a meaningful design, probably should be — overwritten by hand in the Inspector to something readable.

Second, the base `ExecuteEffect()` contract takes **no parameters** (`07-InventorySystem.md`), so how does it know *which player* to apply the buff to? `CanBeUsed(Player player)` — called immediately beforehand by `Inventory_Base.TryUseItem` — quietly stashes `this.player = player` as a side effect of the check itself, and the later parameterless `ExecuteEffect()` reads that stashed reference. A clever (if slightly indirect) way to route data through a fixed interface shape. `player = null;` at the end is a small cleanup habit, guarding against a stale reference lingering on the shared asset after use.

### `ItemEffect_HealOnDoingDamage` — the lifesteal prediction from `07-InventorySystem.md`, confirmed

```csharp
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
public void HealOnDealingDamage(float damage) => player.health.IncreaseHealth(damage * healPercentagePerAttack);
```
This is exactly the mechanism `07-InventorySystem.md` predicted when explaining `Subscribe`/`Unsubscribe`: equipping this item attaches a listener directly onto `Entity_Combat.OnDoingPhysicalDamage` (`02-Entity.md`), unequipping detaches it — the effect is fully "on" only while the item is actually worn, driven entirely by these two hooks and nothing `Inventory_Item` itself needs to know about.

### `ItemEffect_IceBlastOnTakingDamage` — a reactive, self-cooldowned defensive trinket

```csharp
public override void Subscribe(Player player)
{
    base.Subscribe(player);
    player.health.OnTakingDamage += ExecuteEffect;
}

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
```
Subscribes to `Entity_Health.OnTakingDamage` (`02-Entity.md`) rather than the combat/damage-*dealt* event — this is a panic-button effect that reacts to the player *getting hurt*, not to attacking. It only actually fires if **both** an internal cooldown (`Time.time >= lastTimeUsed + cooldown`, the exact same hand-rolled shape as `Skill_Base.OnCooldown` from `08-SkillSystem.md`, reimplemented independently here rather than shared) **and** a low-health threshold (default ≤25%) are satisfied — an emergency effect, not something that fires on every hit.

```csharp
private void DamageEnemiesWithIce()
{
    Collider2D[] enemies = Physics2D.OverlapCircleAll(player.transform.position, 1.5f, whatIsEnemy);
    foreach (var enemy in enemies)
    {
        IDamagable damagable = enemy.GetComponent<IDamagable>();
        if (damagable == null) continue;
        bool targetGotHit = damagable.TakeDamage(0, iceDamage, ElementType.Ice, player.transform);
        enemy.GetComponent<Entity_StatusHandler>()?.ApplyStatusEffect(ElementType.Ice, effectData);
        if (targetGotHit) player.VFX.CreateEffectOf(onHitVFXPrefab, enemy.transform);
    }
}
```
Worth explicitly noticing: this is the **fourth** appearance in this documentation set of the exact same shape — overlap-check for nearby targets, filter by `IDamagable`, deal damage, apply a status effect via `Entity_StatusHandler` (`Entity_Combat.PreformAttack` in `02-Entity.md`; `SkillObject_Base.DamageEnemiesInRadius` in `08-SkillSystem.md`; now here, reimplemented independently a third time rather than reusing either of the other two). A genuine, recurring idiom in this codebase worth recognizing on sight — and also a case where a shared helper *could* have been factored out but wasn't, similar to the `EnsurePlayerInventory` duplication flagged in `10-InteractiveObjects.md`.

### `ItemEffect_PortalScroll` — the payoff of `Object_Portal`

```csharp
public override void ExecuteEffect()
{
    if (SceneManager.GetActiveScene().name == "Level_0") { Debug.Log("You are already in town."); return; }
    Player player = Player.Instance;
    Vector3 portalPosition = player.transform.position + new Vector3(player.facingDir * 1.5f, 0);
    Object_Portal.Instance.ActivatePortal(portalPosition, player.facingDir);
}
```
This is precisely the "portal-scroll item effect" CLAUDE.md and `10-InteractiveObjects.md` both reference — the scroll can't be used already in town, and otherwise summons the portal a short distance in front of the player (offset by `facingDir`, facing the same way the player currently faces), directly calling `Object_Portal.Instance.ActivatePortal(...)` (`10-InteractiveObjects.md`).

### `ItemEffect_GrantSkillPoint` and `ItemEffect_RefundAllSkills` — forward references to the Skill Tree UI

```csharp
// GrantSkillPoint
UI ui = FindFirstObjectByType<UI>();
ui.skillTreeUI.AddSkillPoints(skillPointsToGrant);

// RefundAllSkills
// UI_SkillTree skillTree = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include); // This line is can find skill tree if it is inactive
// skillTree.ResetSkillTree();
UI ui = FindFirstObjectByType<UI>();
ui.skillTreeUI.ResetSkillTree();
```
Both are one-liners reaching into the `UI` hub's skill-tree panel (full detail in the later `UI` doc) — included here mainly to note the commented-out alternative in `RefundAllSkills`: a direct `FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include)` search, with its own inline comment explaining *why* that alternative exists as an option ("can find skill tree if it is inactive" — i.e., it would work even if the skill tree panel starts disabled, unlike going through `ui.skillTreeUI` which presumably assumes the panel reference is already valid). Another example of this codebase's habit of leaving a working alternative approach visible in comments rather than deleting it outright.

---

## Quest Data — the same asset/runtime-instance split, applied a second time

### `Quest_DataSO` — the design

```csharp
public enum RewardGiver { Merchant, Blacksmith, None }

public class Quest_DataSO : ScriptableObject
{
    public string questSaveID;
    public string questName;
    [TextArea] public string questDescription;
    [TextArea] public string questObjective;
    public string questTargetID;
    public int requiredAmount;
    public RewardGiver rewardGiver;
    public Inventory_Item[] questRewards;
    public int goldReward;
}
```
`questTargetID`/`requiredAmount` are exactly what `Player_QuestManager.AddProgress(questTargetId, amount)` (`03-Player.md`) and `Enemy_Health.Die()` calling `questManager.AddProgress(enemy.questTargetID)` (`04-Enemy.md`) tie together — a quest and an enemy agree on progress purely by matching this one string, with no direct reference between them at all.

`questRewards` being typed `Inventory_Item[]` is the **second** appearance of the same reuse trick already seen with `Item_DataSO.craftRecipe` (`07-InventorySystem.md`): the runtime "owned item" class doing double duty purely as a `{item, amount}` data descriptor, never actually representing something anyone owns yet.

`RewardGiver` is worth a small, honest note: it's defined directly in this file rather than living under `Scripts/Enums/` with every other enum in the project (`01-Foundations.md`) — a minor organizational inconsistency, most likely because it was added later in development without circling back to relocate it, rather than a deliberate choice. Functionally it works exactly the same either way.

Also worth being candid about: based on every `InteractiveObjects` script actually read (`10-InteractiveObjects.md`), only `Object_Merchant` currently offers quests (`questsToOffer`) — `Object_Blacksmith.Interact()` opens Storage/Craft UI and never references quests at all — so a quest configured with `rewardGiver = Blacksmith` doesn't have an obvious current effect visible in the code covered so far. It's plausible this field is consumed purely by the Quest UI for display purposes ("turn this in to the Blacksmith") rather than by any NPC's own interact logic — that would only be confirmed by the later `UI` doc.

### `QuestDatabase_DataSO` — the same lookup-list template as `ItemList_DataSO`

```csharp
public Quest_DataSO[] allQuests;
public Quest_DataSO GetQuestDataByID(string id) => allQuests.FirstOrDefault(quest => quest.questSaveID == id && quest != null);

#if UNITY_EDITOR
[ContextMenu("Auto-Fill Quests Data")]
public void CollectQuestsData()
{
    string[] guids = AssetDatabase.FindAssets("t:Quest_DataSO");
    allQuests = guids.Select(guid => AssetDatabase.LoadAssetAtPath<Quest_DataSO>(AssetDatabase.GUIDToAssetPath(guid))).Where(quest => quest != null).ToArray();
    EditorUtility.SetDirty(this);
    AssetDatabase.SaveAssets();
}
#endif
```
Structurally near-identical to `ItemList_DataSO.CollectItemsData` (`07-InventorySystem.md`) — the same "search the whole project for every asset of type X, auto-populate this list, mark the asset dirty and save" Editor-only convenience, just applied to `Quest_DataSO` instead of `Item_DataSO`. A genuinely reusable template worth recognizing as one idea applied twice, not two separate inventions.

### `QuestData` — the runtime, owned instance (mirrors `Inventory_Item`)

```csharp
[Serializable]
public class QuestData
{
    public Quest_DataSO questDataSO;
    public int currentAmount;
    public bool canGetReward;

    public QuestData(Quest_DataSO questDataSO)
    {
        this.questDataSO = questDataSO;
        currentAmount = 0;
        canGetReward = false;
    }

    public bool CanGetReward() => currentAmount >= questDataSO.requiredAmount;
    public void AddQuestProgress(int amount = 1)
    {
        currentAmount += amount;
        canGetReward = CanGetReward();
    }
}
```
This is the **same architectural idea** first established in `07-InventorySystem.md` with `Item_DataSO`/`Inventory_Item`, reapplied almost identically here: `Quest_DataSO` is the shared, asset-based *design* (name, description, target, reward), while `QuestData` is the live, *owned* instance tracking one specific player's actual progress on it — created fresh via `Player_QuestManager.AcceptQuest` (`03-Player.md`: `activeQuests.Add(new QuestData(questDataSO))`). `AddQuestProgress` is exactly what `Player_QuestManager.AddProgress` calls once it's found the matching active quest by `questTargetID`. Recognizing this as the *same pattern* applied to a second, unrelated system (items vs. quests) is worth more than memorizing either instance individually — it's evidence of a genuinely reusable architectural idea in this codebase, not a coincidence.

---

## How this connects elsewhere

- Nearly every earlier chapter reads from this one: `02-Entity.md` (`AttackData` family), `03-Player.md` (`Player_Stats.ApplyBuff` via `ItemEffect_Buff`, `Player_QuestManager` via `QuestData`), `04-Enemy.md` (`questTargetID`), `06-StatSystem.md` (`Stat_SetupSO` → `ApplyDefaultStatSetup`), `07-InventorySystem.md` (the whole `Item_DataSO` family), `08-SkillSystem.md` (`Skill_DataSO`), `10-InteractiveObjects.md` (`ItemEffect_PortalScroll` → `Object_Portal.ActivatePortal`).
- `ui.skillTreeUI.AddSkillPoints`/`ResetSkillTree` (used by two of the item effects here) and how `QuestData`/`Quest_DataSO` actually render in a Quest UI panel both belong to the later `UI` doc.
- `AudioDatabase_DataSO` is covered alongside `AudioManager`/`AudioRangeController` in the later `Audio` doc, not here.

## Where this pattern could be reused later

The **design asset + owned runtime instance** split — first seen with items, now confirmed as the same idea reused for quests — is worth internalizing as a general-purpose pattern for *any* game system with "a catalog of possible things" plus "which ones a specific player currently has/is doing": crafting recipes, achievements, dialogue trees, anything with that shape. The **dual-purpose `itemRarity` field** (drop-chance driver *and* loot-budget cost, from one number) is a compact, worth-remembering technique for keeping a rarity system simple without two separate parallel values to keep in sync. And the **two different "stable ID" techniques** seen across this project — a randomly generated GUID stored once (`Object_Checkpoint`) versus deriving one from Unity's own per-asset `AssetDatabase` GUID (`Item_DataSO`, `Quest_DataSO`) — are worth keeping as two distinct tools: the first for runtime-created or scene-placed objects with no underlying asset, the second specifically for ScriptableObject assets where surviving a rename/move in the Project window matters.

---

**Next:** `12-UI.md` — `UI.cs`, the central hub, and every sub-panel folder (Character, Craft, In Game, Merchant, Quest, Skill Tree, Storage, Tool Tips).
