# 07 — InventorySystem: Items, Equipment, Shops, and Crafting

## Overview

Four files (`Inventory_Base`, `Inventory_Item`, `Inventory_EquipmentSlot`, plus the three subclasses `Inventory_Player`/`Inventory_Merchant`/`Inventory_Storage`) that implement everything about owning, equipping, buying, selling, and crafting items. The single most important idea to get straight before anything else here makes sense:

**`Item_DataSO` (and its subclass `Equipment_DataSO`) is the *design* of an item — one ScriptableObject asset per unique item, shared by everyone. `Inventory_Item` is one *owned copy* of that design — created fresh every single time someone actually gets one.** Two players (or the same player twice) picking up "Iron Sword" both end up with their own separate `Inventory_Item` object, but both point at the exact same one `Equipment_DataSO` asset for its name, icon, base stats, and so on. This doc covers the *owning* side (`Inventory_*`); the *design* side (`Item_DataSO`/`Equipment_DataSO`/`ItemEffect_DataSO` and friends) gets its full treatment in the later `Data-ScriptableObjects` doc — this doc reads just enough of them to explain how the inventory system uses them.

---

## `Inventory_Item.cs` — one owned copy of an item

```csharp
[Serializable]
public class Inventory_Item
{
    private string itemId;
    public Item_DataSO itemData;
    public int stackSize = 1;
    public ItemModifier[] modifiers { get; private set; }
    public ItemEffect_DataSO itemEffect;
    public int buyPrice { get; private set; }
    public int sellPrice { get; private set; }

    public Inventory_Item(Item_DataSO itemData)
    {
        this.itemData = itemData;
        modifiers = GetEquipmentData()?.modifiers;
        itemEffect = itemData.itemEffect;
        buyPrice = itemData.itemPrice;
        sellPrice = Mathf.RoundToInt(itemData.itemPrice * 0.25f);
        itemId = itemData.itemName + " - " + Guid.NewGuid();
    }
```

Every `Inventory_Item` in the entire game — a world pickup, a rolled shop stock item, a crafted result, an item rebuilt from a save file — is created through this one constructor, `new Inventory_Item(someItemData)`. It's the single chokepoint that turns a shared design asset into an actual, ownable instance.

A few details worth understanding:

- **`itemId` is not the same thing as `itemData.saveID`.** `saveID` (covered fully in the `Data` doc) identifies the *asset/design* — stable, generated once from the asset's file path, used so a save file can say "the player owns 3 of asset X." `itemId` here is generated fresh **every time this constructor runs** via `Guid.NewGuid()` (a globally unique random identifier — see `AddModifiers` below for why this specific instance needs its own unique ID rather than reusing `saveID`).
- **`GetEquipmentData()`** uses a pattern-matching check — `if (itemData is Equipment_DataSO equipmentData)` — to safely ask "is this item's design actually an *equipment* item, not just a generic/consumable/material one?" without a cast that could throw. Only equipment has stat `modifiers`; a material or consumable's `modifiers` array ends up `null` via the `?.` on `GetEquipmentData()?.modifiers`.
- **`sellPrice` is hardcoded at 25% of `buyPrice`** — a flat markdown baked into every item at creation time, not something a merchant negotiates per-transaction.

### `AddModifiers`/`RemoveModifiers` — confirming the `06-StatSystem.md` prediction

```csharp
public void AddModifiers(Entity_Stats playerStats)
{
    if (modifiers == null) return;
    foreach (var modifier in modifiers)
    {
        Stat stat = playerStats.GetStatByType(modifier.statType);
        stat.AddModifier(modifier.value, itemId);
    }
}

public void RemoveModifiers(Entity_Stats playerStats)
{
    if (modifiers == null) return;
    foreach (var modifier in modifiers)
        playerStats.GetStatByType(modifier.statType).RemoveModifier(itemId);
}
```
This is exactly the mechanism `06-StatSystem.md` predicted: equipping an item calls `Stat.AddModifier(value, source)` (`06-StatSystem.md`) once per stat the equipment modifies, tagging every one of those modifiers with **this specific equipped instance's own unique `itemId`** as the `source`. That's *why* `itemId` needs to be freshly generated per-instance rather than reusing the shared `saveID`: if two identical swords were both equipped (in different slots) and both used `saveID` as their modifier source, unequipping *one* of them via `RemoveModifier(saveID)` would incorrectly strip the *other* sword's modifiers too, since `RemoveModifier` removes every modifier matching that exact source string. A fresh GUID per instance guarantees equipping/unequipping one copy of an item never touches another copy's bonuses.

### `AddItemEffect`/`RemoveItemEffect` — the Strategy-pattern hook

```csharp
public void AddItemEffect(Player player) => itemEffect?.Subscribe(player);
public void RemoveItemEffect() => itemEffect?.Unsubscribe();
```
`ItemEffect_DataSO` (base, briefly):
```csharp
public class ItemEffect_DataSO : ScriptableObject
{
    public virtual bool CanBeUsed(Player player) => true;
    public virtual void ExecuteEffect() { }
    public virtual void Subscribe(Player player) { this.player = player; }
    public virtual void Unsubscribe() { }
}
```
Four `virtual` hooks, all empty/trivial by default — concrete subclasses (Heal, Buff, lifesteal-on-damage, reactive ice blast, grant/refund skill points — per CLAUDE.md, full detail in the `Data` doc) override whichever ones they actually need. `Subscribe`/`Unsubscribe` exist specifically for *passive*, always-active equipment effects: a lifesteal trinket, for instance, would override `Subscribe` to attach a listener onto `Entity_Combat.OnDoingPhysicalDamage` (`02-Entity.md`) and `Unsubscribe` to detach it — meaning the effect turns on the moment the item is equipped and off the moment it's removed, entirely through this one pair of calls, without `Inventory_Item` or `Inventory_Player` needing to know anything about what the effect actually does. `CanBeUsed`/`ExecuteEffect` are instead for *consumable* items — see `Inventory_Base.TryUseItem` below.

### `GetItemInfo()` — building the tooltip text

```csharp
foreach (var mod in modifiers)
{
    string modType = GetStatType(mod.statType);
    string modValue = IsPercentageStat(mod.statType) ? mod.value.ToString("0.##") + "%" : mod.value.ToString();
    sb.AppendLine("+ " + modValue + " " + modType);
}
```
Uses a `StringBuilder` (efficient for building up a multi-line string piece by piece, better than repeated `string +=` concatenation) with three branches by `itemType` (Material → static "used for crafting" text; Consumable → shows `itemEffect.effectDescription`; everything else → lists each stat modifier as a formatted line). `GetStatType(StatType)` and `IsPercentageStat(StatType)` are two more instances of the recurring **"enum in, something out, via a `switch`"** lookup shape already seen for `Entity_Stats.GetStatByType` and `Player_SkillManager.GetSkillByType` — here mapping a `StatType` to its human-readable display name, and separately flagging which stat types should render as a `%` (crit chance, resistances, evasion, attack speed, armor reduction) versus a flat number.

---

## `Inventory_EquipmentSlot.cs` — the tiniest file in the system

```csharp
[Serializable]
public class Inventory_EquipmentSlot
{
    public ItemType slotType;
    public Inventory_Item equippedItem;
    public bool HasItem() => equippedItem != null && equippedItem.itemData != null;
}
```
A labeled slot (`slotType`, from `01-Foundations.md`'s `ItemType` enum — Weapon, Armor, Trinket, etc.) holding at most one item. `Inventory_Player.equipmentList` is a `List<Inventory_EquipmentSlot>` — meaning the number and type-mix of equipment slots is entirely data-driven by however many entries are configured in the Inspector, not a fixed hardcoded "one weapon slot, one armor slot" structure in code.

---

## `Inventory_Base.cs` — the shared foundation

```csharp
public class Inventory_Base : MonoBehaviour, ISaveable
{
    protected Player player;
    public event Action OnInventoryChanged;
    public int maxInventorySize = 10;
    public List<Inventory_Item> itemList = new List<Inventory_Item>();
    [SerializeField] protected ItemList_DataSO itemDataBase;

    protected virtual void Awake() => player = GetComponent<Player>();
```
Implements `ISaveable` (`01-Foundations.md`) with empty `virtual` `LoadData`/`SaveData` — the template-method pattern seen before with `Entity.SlowDownEntityCo`/`EntityDeath` — each concrete subclass provides its own actual save shape.

Worth noticing: `player = GetComponent<Player>()` only ever finds something on `Inventory_Player` (which genuinely lives on the Player GameObject) — for `Inventory_Merchant`/`Inventory_Storage` (which live on their own separate merchant/storage objects), this silently returns `null` rather than throwing, since `GetComponent` simply returns `null` when nothing matches. `player` is a field on the *shared* base even though only one of the three subclasses ever actually uses it meaningfully.

`itemDataBase` (an `ItemList_DataSO` — briefly, a flat array of every `Item_DataSO` asset plus a `GetItemData(saveID)` lookup, full detail in the `Data` doc) is what every subclass's `LoadData` uses to turn a saved `saveID` string back into a real `Item_DataSO` reference, and from there a fresh `Inventory_Item`.

### The core item-list operations

```csharp
public bool CanAddItem(Inventory_Item itemToAdd)
{
    bool hasStackable = FindStackableItem(itemToAdd) != null;
    return itemList.Count < maxInventorySize || hasStackable;
}

public Inventory_Item FindStackableItem(Inventory_Item itemToAdd) =>
    itemList.Find(item => item.itemData == itemToAdd.itemData && item.CanAddStack());

public virtual void AddItem(Inventory_Item itemToAdd)
{
    var existingStackable = FindStackableItem(itemToAdd);
    if (existingStackable != null) existingStackable.AddToStack();
    else itemList.Add(itemToAdd);
    OnInventoryChanged?.Invoke();
}
```
`CanAddItem` correctly treats "room" as *either* an empty slot *or* an existing compatible stack with space left — so a "full" 10-slot inventory can still absorb one more potion if slot #3 already holds potions and hasn't hit its max stack size yet. `FindStackableItem` matches by **`itemData` reference equality** (same underlying ScriptableObject asset = same item type) combined with `CanAddStack()` (not already maxed out).

```csharp
public Inventory_Item FindItem(Inventory_Item itemToFind) => itemList.Find(item => item == itemToFind);
public Inventory_Item FindSameItem(Inventory_Item itemToFind) => itemList.Find(item => item.itemData == itemToFind.itemData);
```
These two look almost identical and are easy to confuse — the difference matters: **`FindItem` asks "does this *exact* `Inventory_Item` object still exist in the list?"** (reference equality — is it literally the same instance), while **`FindSameItem` asks "is there *any* item of this same type currently in the list?"** (matches by `itemData`, could be a completely different `Inventory_Item` instance). `Inventory_Player.TryUseQuickItem` (below) is the clearest place this distinction actually matters in practice.

```csharp
public void TryUseItem(Inventory_Item itemToUse)
{
    Inventory_Item consumable = itemList.Find(item => item == itemToUse);
    if (consumable == null) return;
    if (!consumable.itemEffect.CanBeUsed(player)) return;

    consumable.itemEffect.ExecuteEffect();
    if (consumable.stackSize > 1) consumable.RemoveFromStack();
    else RemoveOneItem(consumable);
    OnInventoryChanged?.Invoke();
}
```
The consumable-use pipeline: confirm the item's still actually in the inventory, ask the item's own effect whether it's currently allowed to be used (`CanBeUsed` — a concrete effect might say no here, e.g. a healing potion refusing to be used at full health), run the effect, then shrink the stack by one (or remove the whole entry if this was the last one).

`RemoveOneItem`/`RemoveAllItems` mirror the add side (`RemoveAllItems` just calls `RemoveOneItem` in a loop `stackSize` times — simple, if not the most efficient possible implementation, but correct and easy to read). `TriggerUpdateUI()` is a small but important public escape hatch: it just fires `OnInventoryChanged` directly, for any code path that mutates inventory-adjacent state *without* going through `AddItem`/`RemoveOneItem` (several show up below — buffs expiring, storage transfers, shop restocks) but still needs the UI to refresh.

---

## `Inventory_Player.cs`

### Quick-item slots — and where `FindItem` vs `FindSameItem` actually matters

```csharp
public Inventory_Item[] quickItems = new Inventory_Item[2];

public void TryUseQuickItem(int passedSlotNumber)
{
    int slotNumber = passedSlotNumber - 1;
    var itemToUse = quickItems[slotNumber];
    if (itemToUse == null) return;

    TryUseItem(itemToUse);

    if (FindItem(itemToUse) == null)
        quickItems[slotNumber] = FindSameItem(itemToUse);

    TriggerUpdateUI();
    OnQuickSlotUsed?.Invoke(slotNumber);
}
```
Quick slots hold a **direct reference** to one specific `Inventory_Item` object, not a copy. After using it, `FindItem(itemToUse) == null` asks "did that *exact* stack disappear from the inventory?" — true only if that was the very last one and `TryUseItem` removed the entry entirely. If so, `FindSameItem(itemToUse)` looks for *any other* stack of that same item type to backfill the slot with — so if you had two separate stacks of the same potion (unusual, but possible), using the last of one stack would slide the quick slot over to the other. If neither exists, the slot silently ends up pointing at nothing useful until reassigned. This is the concrete payoff of the `FindItem`/`FindSameItem` distinction explained above — get them backwards here and quick slots would either never clear out a depleted item, or clear out items that still have plenty left.

### Equipping — and the health-percentage preservation trick

```csharp
private void EquipItemToSlot(Inventory_Item item, Inventory_EquipmentSlot slot)
{
    float savedHealthPercent = player.health.GetHealthPercent();
    slot.equippedItem = item;
    item.AddModifiers(player.stats);
    slot.equippedItem.AddItemEffect(player);
    player.health.SetHealthPercent(savedHealthPercent);
    RemoveOneItem(item);
}
```
Worth noticing the health handling specifically: equipping a piece of gear can change `MaxHealth` (via a Vitality or direct MaxHealth modifier). If the code did nothing special, current HP (a raw number) would stay exactly what it was — meaning if max health just *increased*, the player would be stuck at a smaller-looking fraction of their new bar; if max health *decreased*, current HP could theoretically exceed the new max. Instead, this snapshots health as a **percentage** (`GetHealthPercent()`, `02-Entity.md`) *before* applying the new modifiers, then re-applies that same percentage (`SetHealthPercent`) *after* — so a player at 50% health before equipping is still at 50% health after, regardless of how much the max changed. `UnequipItemFromSlot` mirrors this exact same trick in reverse.

```csharp
public void TryEquipItem(Inventory_Item item)
{
    var inventoryItem = FindItem(item);
    var matchingSlots = equipmentList.FindAll(slot => slot.slotType == item.itemData.itemType);

    foreach (var slot in matchingSlots)
        if (!slot.HasItem()) { EquipItemToSlot(inventoryItem, slot); return; }

    var slotToReplace = matchingSlots[0];
    var itemToUnequip = slotToReplace.equippedItem;
    UnequipItemFromSlot(itemToUnequip, slotToReplace != null);
    EquipItemToSlot(inventoryItem, slotToReplace);
}
```
Two-step: try an empty matching slot first; if none, forcibly replace the first matching slot. One detail worth flagging honestly: `UnequipItemFromSlot(itemToUnequip, slotToReplace != null)` — by the time this line runs, `slotToReplace` was already read from `matchingSlots[0]` on the line above, which would have already thrown an `IndexOutOfRangeException` if `matchingSlots` were empty. So `slotToReplace` is *always* non-null here, meaning `slotToReplace != null` always evaluates `true` — this call always passes `replacingItem: true`, regardless of what that condition superficially looks like it's checking. Not a bug that causes wrong behavior (the intent — "we're replacing, so bypass the room check" — does hold every time this code path runs), just worth knowing the condition itself isn't actually doing any deciding.

```csharp
public void UnequipItemFromSlot(Inventory_Item unEquipItem, bool replacingItem = false)
{
    if (!CanAddItem(unEquipItem) && !replacingItem) return;
    ...
}
```
The `replacingItem` parameter matters for callers *outside* the always-true path above — `Player_DropManager.DropItems()` (`03-Player.md`) calls this with the default `replacingItem = false`, meaning a death-drop unequip *does* respect "is there room to receive this back" (though in that specific case the item's about to be dropped on the ground anyway, so the practical effect is mostly about whether the unequip is allowed to proceed at all).

### Save/load — items become IDs and counts, not full objects

```csharp
public override void SaveData(ref GameData data)
{
    data.gold = gold;
    data.inventory.Clear();
    data.equippedItems.Clear();
    foreach (var item in itemList)
        if (item?.itemData != null)
        {
            string saveID = item.itemData.saveID;
            if (!data.inventory.ContainsKey(saveID)) data.inventory[saveID] = 0;
            data.inventory[saveID] += item.stackSize;
        }
    foreach (var slot in equipmentList)
        if (slot.HasItem())
            data.equippedItems[slot.equippedItem.itemData.saveID] = slot.slotType;
}
```
Notice what's genuinely saved: just `saveID → total stack count` (`SerializableDictionary<string, int>`) and `saveID → which slot type` for equipment — never a full `Inventory_Item` object, never its `modifiers`/`itemEffect`/prices. That's deliberate: all of that other data is fully determined by `itemData` alone, so there's nothing to gain (and real fragility to risk) from serializing it — `LoadData` just walks the saved dictionaries, looks each `saveID` up in `itemDataBase`, and calls `new Inventory_Item(itemData)` fresh for each one, which naturally regenerates everything correctly from the shared design asset. This is the same "don't save what you can recompute" instinct worth remembering generally.

---

## `Inventory_Merchant.cs` — stock that re-rolls, never persists

```csharp
protected override void Awake() { base.Awake(); FillShopList(); }
```
Notice `Inventory_Merchant` **never overrides `LoadData`/`SaveData`** at all — a shop's stock is intentionally *not* persisted; it's freshly randomized every time `Awake()` runs (i.e., every time the merchant's scene loads).

```csharp
public void TryBuyItem(Inventory_Item itemToBuy, bool buyFullStack)
{
    int amountToBuy = buyFullStack ? itemToBuy.stackSize : 1;
    for (int i = 0; i < amountToBuy; i++)
    {
        if (inventory.gold < itemToBuy.buyPrice) return;
        if (itemToBuy.itemData.itemType == ItemType.Material)
            inventory.storage.AddMaterialToStorage(itemToBuy);
        else if (inventory.CanAddItem(itemToBuy))
            inventory.AddItem(new Inventory_Item(itemToBuy.itemData));

        inventory.gold -= itemToBuy.buyPrice;
        RemoveOneItem(itemToBuy);
    }
}
```
Buys **one unit at a time in a loop**, even for "buy full stack" — and checks affordability *inside* the loop, so buying a stack of 5 when you can only afford 3 correctly buys exactly 3 and then stops, rather than an all-or-nothing check up front that would buy 0. Materials specifically skip the player's general inventory entirely and go straight into the material stash (`inventory.storage.AddMaterialToStorage`) — materials never occupy a regular inventory slot when bought directly.

```csharp
public void TrySellItem(Inventory_Item itemToSell, bool sellFullStack)
{
    int amountToSell = sellFullStack ? itemToSell.stackSize : 1;
    for (int i = 0; i < amountToSell; i++)
    {
        inventory.gold += Mathf.FloorToInt(itemToSell.sellPrice);
        inventory.RemoveOneItem(itemToSell);
    }
}
```
Worth a quick, honest observation: selling **removes the item from the player and pays gold, but never adds the sold item into the merchant's own `itemList`.** A sold item simply vanishes rather than becoming available for the player (or presumably anyone) to buy back later — a reasonable design choice, just worth knowing so you don't go looking for a "buy back" feature that isn't wired up.

```csharp
public void FillShopList()
{
    itemList.Clear();
    List<Inventory_Item> possibleItems = new List<Inventory_Item>();
    foreach (var itemData in shopData.itemList)
    {
        int randomizedStack = Random.Range(itemData.minStackSizeAtShop, itemData.maxStackSizeAtShop + 1);
        possibleItems.Add(new Inventory_Item(itemData) { stackSize = Mathf.Clamp(randomizedStack, 1, itemData.maxStackSize) });
    }

    int finalAmount = Mathf.Clamp(Random.Range(minItemAmount, maxInventorySize + 1), 1, possibleItems.Count);
    for (int i = 0; i < finalAmount; i++)
    {
        int randomIndex = Random.Range(0, possibleItems.Count);
        var item = possibleItems[randomIndex];
        if (CanAddItem(item)) { possibleItems.RemoveAt(randomIndex); AddItem(item); }
    }
}
```
Two-phase roll: first build one `Inventory_Item` per possible shop item with a randomized stack size, then pick a random *subset* of those by repeatedly grabbing a random index and removing it from the candidate pool — a manual "sampling without replacement" shuffle. The net effect: a merchant's actual stock (which items, in what quantities) is different every time its scene loads.

---

## `Inventory_Storage.cs` — the stash, and crafting

Storage is unique among the three subclasses in holding **two separate lists**: the inherited `itemList` (general stored items) and its own `materialStorage` (crafting materials specifically) — kept apart because crafting logic needs to check/consume materials from multiple pools, and separating "materials" from "everything else" keeps that search smaller and clearer.

### Crafting — checking and consuming from three pools, in priority order

```csharp
private void ConsumeMaterials(Inventory_Item itemToCraft)
{
    foreach (var requiredMaterial in itemToCraft.itemData.craftRecipe)
    {
        int amountToConsume = requiredMaterial.stackSize;
        amountToConsume -= ConsumedMaterialsAmount(playerInventory.itemList, requiredMaterial);
        if (amountToConsume > 0) amountToConsume -= ConsumedMaterialsAmount(itemList, requiredMaterial);
        if (amountToConsume > 0) amountToConsume -= ConsumedMaterialsAmount(materialStorage, requiredMaterial);
    }
    TriggerUpdateUI();
}
```
`itemToCraft.itemData.craftRecipe` is a nice bit of reuse worth flagging: `Item_DataSO.craftRecipe` is typed as `Inventory_Item[]` — the exact same runtime class used for *owned* items — but here it's being used purely as a **data pair** ("which item, how many") to describe a recipe requirement, not as an actually-owned item. Same class, two different jobs depending on context.

Consumption checks **player inventory first, then storage's own general items, then the dedicated material stash** — each pool only gets checked if the previous one didn't fully cover the requirement (`if (amountToConsume > 0)`), and `ConsumedMaterialsAmount` **mutates whichever list it's handed** directly (shrinking stacks, removing empty ones) — the same helper method is reused for all three different lists just by passing a different `List<Inventory_Item>` in each time.

```csharp
public int GetAvailableAmountOf(Item_DataSO requiredItem)
{
    int amount = 0;
    foreach (var item in playerInventory.itemList) if (item.itemData == requiredItem) amount += item.stackSize;
    foreach (var item in itemList) if (item.itemData == requiredItem) amount += item.stackSize;
    foreach (var item in materialStorage) if (item.itemData == requiredItem) amount += item.stackSize;
    return amount;
}
```
Contrast this with `ConsumeMaterials` above: **checking whether you have enough** (`HasEnoughMaterials`, built on this method) is order-independent — it just sums quantities across all three pools as one combined total. **Actually consuming** needs order (spend from the player's own bag before dipping into storage) — so the codebase correctly uses two different shapes for what looks like a similar problem: a flat sum for the yes/no check, a sequential subtract for the actual spend.

```csharp
public void AddMaterialToStorage(Inventory_Item itemToAdd)
{
    var stackableItem = StackableInStash(itemToAdd);
    if (stackableItem != null) stackableItem.AddToStack();
    else materialStorage.Add(new Inventory_Item(itemToAdd.itemData));
    TriggerUpdateUI();
    materialStorage = materialStorage.OrderBy(item => item.itemData.itemName).ToList();
}
```
Re-sorts the **entire** `materialStorage` list alphabetically by item name after every single addition — eager re-sorting on write, rather than sorting only when the UI actually displays it, so the stash is always in a consistent, alphabetically-browsable order.

`FromPlayerToStorage`/`FromStorageToPlayer` (transfer methods) both deliberately construct a **fresh** `new Inventory_Item(item.itemData)` for the destination rather than moving the literal source object — simpler than writing custom "move and merge stacks" logic, since creating a plain new instance and handing it to the destination's own `AddItem` lets that method's existing stack-or-append logic handle merging correctly on its own.

`Inventory_Storage.SaveData` is the one override in this whole system that actually calls `base.SaveData(ref data)` first (even though the base implementation is empty) before adding its own two dictionaries (`storageItems`, `storageMaterials`) — a small but genuinely more defensive habit than `Inventory_Player.SaveData`, which skips the `base.` call entirely; harmless today since the base does nothing, but calling it is the safer pattern in case that ever changes.

---

## How this connects elsewhere

- `Entity_Stats.GetStatByType` (`02-Entity.md`) and `Stat.AddModifier`/`RemoveModifier` (`06-StatSystem.md`) are exactly what `Inventory_Item.AddModifiers`/`RemoveModifiers` are built on — this doc is the confirmation of what `06-StatSystem.md` predicted about equipment.
- `Player_DropManager.DropItems()` (`03-Player.md`) is a real caller of `RemoveAllItems`/`UnequipItemFromSlot` from outside the inventory system itself.
- `Player.OnEnable`'s `QuickItemSlot_1`/`QuickItemSlot_2` input bindings (`03-Player.md`) are what actually trigger `TryUseQuickItem`.
- `Item_DataSO`, `Equipment_DataSO`, `ItemList_DataSO`, and the full `ItemEffect_DataSO` subclass family get their complete treatment in the later `Data-ScriptableObjects` doc — this doc only read as much of them as needed to explain the inventory side accurately.
- `Object_Blacksmith`/`Object_Merchant`/`Object_Chest`/`Object_ItemPickup` (the `InteractiveObjects` doc) are what actually open these inventories' UI panels or call `AddItem` from the world.

## Where this pattern could be reused later

The **shared design asset (ScriptableObject) + freshly-constructed owned instance** split (`Item_DataSO` vs. `Inventory_Item`) is a strong, broadly reusable pattern for *any* game with an inventory — it's what lets thousands of "Iron Sword" instances exist across many players/saves while only ever authoring "Iron Sword" once. The **per-instance unique modifier-source tag** (`itemId` via `Guid.NewGuid()`) is worth remembering specifically any time multiple copies of the same equippable thing need to apply and cleanly remove their own bonuses independently of each other. And the **"check as a flat sum, but consume in priority order across multiple pools"** split in the crafting code is a good general template for any system where "do I have enough" and "spend it" are related but distinct questions with different correct implementations.

---

**Next:** `08-SkillSystem.md` — `Skill_Base` and the five `Skill_*`/`SkillObject_*` pairs (Dash, Shard, SwordThrow, TimeEcho, Domain Expansion).
