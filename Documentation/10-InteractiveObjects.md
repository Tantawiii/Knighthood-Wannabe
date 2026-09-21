# 10 — InteractiveObjects: Checkpoints, Waypoints, Portals, NPCs, Pickups

## Overview

Nine files covering everything in the world the player can touch, trigger, or hit that isn't Player/Enemy/UI: `Object_Checkpoint` and `Object_Waypoint` (previewed briefly in `05-GameManager-and-Scenes.md`, given their full treatment here), `Object_Portal` (the richest of the three "world state" objects), `Object_NPC` (base for the two shop-keeper types), `Object_Merchant`/`Object_Blacksmith`, and three standalone pieces — `Object_ItemPickup`, `Object_Buff`, `Object_Chest`.

None of these extend `Entity` — this whole folder is a good study in **composition over inheritance**: several of them (`Object_Chest` especially) reach for `Entity_VFX`/`Entity_DropManager`/`IDamagable` via plain `GetComponent`/interface implementation rather than inheriting the full `Entity` state-machine/movement/collision-detection machinery that would be complete overkill for something that never moves.

---

## `Object_Checkpoint.cs` — full treatment

```csharp
public class Object_Checkpoint : MonoBehaviour, ISaveable
{
    [SerializeField] private string checkpointID;
    [SerializeField] private Transform respawnPoint;
    public bool isActive { get; private set; }
```
`checkpointID` auto-generates a GUID the first time it's left blank:
```csharp
private void OnValidate()
{
    #if UNITY_EDITOR
    if (string.IsNullOrEmpty(checkpointID))
        checkpointID = System.Guid.NewGuid().ToString();
    #endif
}
```
`OnValidate()` itself only ever runs in the Editor already (never in a built game), so wrapping its body in `#if UNITY_EDITOR` is technically redundant here — but it's a defensive habit some Unity developers keep regardless, and it does no harm. The result: every checkpoint gets a guaranteed-unique, stable ID without anyone having to type one in by hand.

`isActive` tracks something distinct from "is this checkpoint saved as unlocked" — it's this-session runtime state (drives an Animator bool and a looping fire sound), while the save file's `unlockedCheckpoints` dictionary (`09-SaveSystem.md`) is the permanent record:
```csharp
public void ActivateCheckpoint(bool activate)
{
    isActive = activate;
    anim.SetBool("isActive", activate);
    if (isActive && !fireAudioSource.isPlaying) fireAudioSource.Play();
    if (!isActive) fireAudioSource.Stop();
}

void OnTriggerEnter2D(Collider2D collision) => ActivateCheckpoint(true);
```
Worth a light flag: `OnTriggerEnter2D` activates on **anything** entering the trigger, not specifically checking for the player — in practice only the player would realistically be positioned to walk into one, but nothing in this code itself enforces that.

```csharp
public void LoadData(GameData data)
{
    bool active = data.unlockedCheckpoints.TryGetValue(checkpointID, out active);
    ActivateCheckpoint(active);
}

public void SaveData(ref GameData data)
{
    if (!isActive) return;
    if (!data.unlockedCheckpoints.ContainsKey(checkpointID))
        data.unlockedCheckpoints.Add(checkpointID, true);
}
```
`LoadData`'s one-liner is a compact bit of C# worth untangling: `TryGetValue(checkpointID, out active)` writes its result **into the same local variable `active`** that's being declared on that line, and the whole expression's return value (`true`/`false`, whether the key existed) is what gets assigned to `active` as well — so `active` ends up meaning "was this checkpoint's ID found in the save data at all" (which, if the checkpoint was ever unlocked, is always paired with a `true` value anyway). `SaveData` only ever **adds** an entry, never removes one — checkpoints are a one-way, permanent-once-reached flag, exactly as `05-GameManager-and-Scenes.md` predicted.

---

## `Object_Waypoint.cs` — full treatment

```csharp
[SerializeField] private RespawnType waypointType;
[SerializeField] private RespawnType connectedWaypoint;

private void OnValidate()
{
    gameObject.name = "Object_Waypoint - " + waypointType.ToString() + " - " + transferToScene;
    if (waypointType == RespawnType.Enter) connectedWaypoint = RespawnType.Exit;
    else if (waypointType == RespawnType.Exit) connectedWaypoint = RespawnType.Enter;
}
```
Two Editor-only conveniences bundled into one `OnValidate`: the GameObject's name in the Hierarchy is kept automatically descriptive (purely cosmetic, zero runtime effect, but genuinely useful for finding the right waypoint in a busy scene), and `connectedWaypoint` is kept automatically in sync with `waypointType` — an `Enter` waypoint always implies its pair is an `Exit`, and vice versa, removing an entire class of "forgot to set the matching type correctly" mistakes a level designer could otherwise make by hand.

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (!canBeTriggered) return;
    GameManager.Instance.ChangeScene(transferToScene, connectedWaypoint);
}
private void OnTriggerExit2D(Collider2D other) => canBeTriggered = true;
```
**This is the literal trigger that kicks off everything documented in `05-GameManager-and-Scenes.md`'s `ChangeSceneCo` walkthrough** — walking into a waypoint calls `GameManager.Instance.ChangeScene` directly, passing `connectedWaypoint` (not `waypointType`) as the respawn type, since the goal is to land the player at the *matching* waypoint on the other side, not another one of the same type. `canBeTriggered` resets on exit, so it's not a permanent one-time-use lock — just a guard against re-firing repeatedly while standing inside the trigger zone.

`GetPositionAndSetTriggerToFalse()` is the same **side-effecting getter** idiom already flagged twice before (`Enemy.GetPlayerReference` in `04-Enemy.md`, and its use inside `GameManager.GetNewPlayerPostion` in `05-GameManager-and-Scenes.md`) — reading a waypoint's position also disables it from immediately re-triggering the moment the player lands there.

---

## `Object_Portal.cs` — the richest of the three

```csharp
public class Object_Portal : MonoBehaviour, ISaveable
{
    public static Object_Portal Instance;
    public bool isActive { get; private set; }
    [SerializeField] private Vector2 defaultPosition;
    [SerializeField] private string townSceneName = "Level_0";
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool canBeTriggered;

    private void Awake()
    {
        Instance = this;
        currentSceneName = SceneManager.GetActiveScene().name;
        transform.position = new Vector3(9999, 9999);
    }
```
Another unguarded singleton (consistent with `Player.Instance`/`SaveManager.Instance`). The `transform.position = new Vector3(9999, 9999)` in `Awake()` is a deliberate "start hidden" trick worth understanding: rather than toggling `SetActive(false)` or a separate visibility flag, the portal simply gets moved somewhere in empty space no player could ever realistically reach — meaning its trigger collider physically can't be touched until something explicitly repositions it. A slightly unusual but functional way to represent "this portal isn't currently placed in the world."

### Activating and deactivating

```csharp
public void ActivatePortal(Vector3 position, int facingDir = 1)
{
    isActive = true;
    transform.position = position;
    SaveManager.Instance.GetGameData().inScenePortals.Clear();
    if (facingDir == -1) transform.Rotate(0, 180, 0);
}

public void DisableIfNeeded()
{
    if (!returningFromTown) return;
    SaveManager.Instance.GetGameData().inScenePortals.Remove(currentSceneName);
    isActive = false;
    transform.position = new Vector3(9999, 9999);
}
```
`ActivatePortal` is called when the player summons a portal (per CLAUDE.md, a "portal-scroll item effect") — note it **clears the entire `inScenePortals` dictionary**, not just this scene's entry: only one portal is meant to meaningfully exist/matter at a time, so creating a new one wipes any stale record of a previous one elsewhere. `DisableIfNeeded()` (called from `GameManager.GetNewPlayerPostion`, `05-GameManager-and-Scenes.md`, right after resolving a `Portal`-type respawn) sends the portal back off into the void once the player's used it to travel *from* town — cleanup so it doesn't linger as a still-usable teleport point.

### The two-way "trip to town" mechanic

```csharp
private void UseTeleport()
{
    string destinationScene = IsInTownScene() ? returnSceneName : townSceneName;
    GameManager.Instance.ChangeScene(destinationScene, RespawnType.Portal);
}
```
This one portal type is always a **round trip**: standing in it while in the town scene sends you back to wherever you came from (`returnSceneName`); standing in it anywhere else sends you to town (`townSceneName`, "Level_0" by default). Not a general point-to-point network — just "town, and back."

### Save/load — one dictionary entry per scene

```csharp
public void LoadData(GameData data)
{
    if (IsInTownScene() && data.inScenePortals.Count > 0)
    {
        transform.position = defaultPosition;
        isActive = true;
    }
    else if (data.inScenePortals.TryGetValue(currentSceneName, out Vector3 portalPosition))
    {
        transform.position = portalPosition;
        isActive = true;
    }
    returningFromTown = data.returningFromTown;
    returnSceneName = data.portalDestinationSceneName;
}

public void SaveData(ref GameData data)
{
    data.returningFromTown = IsInTownScene();
    if (isActive && !IsInTownScene())
    {
        data.inScenePortals[currentSceneName] = transform.position;
        data.portalDestinationSceneName = currentSceneName;
    }
    else
    {
        data.inScenePortals.Remove(currentSceneName);
    }
}
```
Two `LoadData` branches: loading **into town**, with *any* portal recorded anywhere (`data.inScenePortals.Count > 0`, meaning the player did travel here via portal from somewhere), places the town-side portal at its fixed `defaultPosition` and activates it — guaranteeing there's always a way back out once you've portaled in. Loading into any **other** scene checks specifically for *that* scene's own recorded position and restores the portal exactly where it was left, in the exact spot the player created it. `SaveData` mirrors this: while active and not in town, record this scene's portal position keyed by scene name; otherwise (inactive, or currently in town) remove any stale entry for this scene.

---

## `Object_NPC.cs` — the shared base for shop-keepers

```csharp
protected Transform player;
protected UI ui;

protected virtual void Awake()
{
    ui = FindFirstObjectByType<UI>();
    startPosition = interactTooltip.transform.position;
    interactTooltip.SetActive(false);
}
```
Same one-time scene search for the `UI` hub as `Player.Awake()` (`03-Player.md`).

```csharp
private void HandleToolTipFloat()
{
    if (interactTooltip == null) return;
    if (interactTooltip.activeSelf)
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange;
        interactTooltip.transform.position = startPosition + new Vector3(0, yOffset);
    }
}
```
A classic smooth up-down bob via a sine wave — the exact same technique reused by `Object_Buff` below, worth recognizing as one small shared idiom rather than two independent inventions.

```csharp
private void HandleNpcFlip()
{
    if (player == null || npc == null) return;
    if (npc.position.x < player.position.x && !facingRight) { npc.Rotate(0f, 180f, 0f); facingRight = !facingRight; }
    else if (npc.position.x > player.position.x && facingRight) { npc.Rotate(0f, 180f, 0f); facingRight = !facingRight; }
}
```
Faces the NPC toward whichever side the player is standing on, every frame while a player reference exists. This is conceptually the same flip trick as `Entity.Flip()`/`HandleFlip()` (`02-Entity.md`) — but reimplemented independently here with its own `facingRight` bool, since `Object_NPC` doesn't extend `Entity` at all and so can't reuse that code. A reasonable design choice (an NPC doesn't need combat/health/a state machine), but worth noticing as duplicated *logic* across otherwise-unrelated class hierarchies.

```csharp
protected virtual void OnTriggerEnter2D(Collider2D collision)
{
    player = collision.transform;
    interactTooltip.SetActive(true);
}
protected virtual void OnTriggerExit2D(Collider2D collision) => interactTooltip.SetActive(false);
```
Base versions just track whoever triggered it and toggle the tooltip — like `Object_Checkpoint`, this doesn't specifically check for the player, it just assumes whatever entered *is* the player (a reasonable assumption given realistic level/collision-layer setup, but not something this code enforces itself). `Object_Merchant`/`Object_Blacksmith` `override` both and call `base.OnTriggerEnter2D`/`Exit2D` first, then layer their own inventory-wiring logic on top — the same "extend, don't replace" template-method shape seen throughout this codebase.

---

## `Object_Merchant.cs` and `Object_Blacksmith.cs`

Both extend `Object_NPC`, both implement `IInteractable` (`01-Foundations.md`), and both have a genuinely **duplicated** private helper worth flagging directly rather than glossing over:

```csharp
// Object_Merchant
private bool EnsurePlayerInventory()
{
    if (inventory == null)
    {
        Transform playerTransform = player != null ? player : FindFirstObjectByType<Player>()?.transform;
        inventory = playerTransform != null ? playerTransform.GetComponent<Inventory_Player>() : null;
        if (inventory != null) merchant.SetInventory(inventory);
    }
    return inventory != null;
}
```
`Object_Blacksmith` has the exact same shape (`playerInventory` instead of `inventory`, `storage.SetInventory` instead of `merchant.SetInventory`) — the same logic, copy-pasted rather than shared through a common method on `Object_NPC` or a further base class between these two. Functionally it works fine (both correctly try the already-known `player` field first, falling back to a fresh scene search — the same "cheap reference first, expensive fallback second" pattern already flagged in `05-GameManager-and-Scenes.md`'s `FindFadeScreenUI`), but it's a clear, spottable case of code that could have been pulled up one level and wasn't — a good contrast against the deliberate composition patterns seen elsewhere in this codebase (like `Skill_Dash` reusing `Skill_Shard`/`Skill_TimeEcho`, `08-SkillSystem.md`), where reuse *was* built in.

### `Object_Merchant.Interact()` — an apparently in-progress feature

```csharp
public void Interact()
{
    if (!EnsurePlayerInventory()) return;
    // ui.merchantUI.SetUpMerchantUI(merchant, inventory);
    // ui.OpenMerchantUI(true);
    ui.OpenQuestUI(questsToOffer);
}
```
Worth being direct about what this shows: the two commented-out lines are the merchant's actual shop UI setup — currently **not wired up to the interact button at all**. Interacting with a merchant right now opens the **quest** UI (offering `questsToOffer`, a `Quest_DataSO[]`) instead of a shop. This lines up with CLAUDE.md's note that the quest system is newer than most other systems — a plausible read is that quest-offering was wired in as (or replaced) the merchant's interact behavior mid-development, and the shop UI connection got commented out rather than removed. Notice `OnTriggerExit2D` still calls `ui.OpenMerchantUI(false)` — closing a panel that `Interact()` itself no longer opens — leftover wiring from before this change, in the same spirit as other "history, not a bug to fix" remnants already flagged in earlier docs (`Entity_DropManager`'s commented debug `Update`, `Player_Health.Die()`'s old restart-scene lines).

```csharp
protected override void Update()
{
    base.Update();
    if (Input.GetKeyDown(KeyCode.Z)) merchant.FillShopList();
}
```
Worth flagging as a developer convenience, not a real feature: pressing `Z` force-rerolls the shop's stock. Note it uses the **old** `Input` class directly, not the new Input System's `PlayerInputSet` used everywhere in actual gameplay code (`03-Player.md`) — a quick debug shortcut that was never meant to be part of the real control scheme.

### `Object_Blacksmith.Interact()` — fully wired

```csharp
public void Interact()
{
    if (!EnsurePlayerInventory()) return;
    ui.storageUI.SetUpStorageUI(storage);
    ui.craftUI.SetUpCraftUI(storage);
    ui.OpenStorageUI(true);
}
```
By contrast, this one's complete — sets up both the Storage and Craft UI panels (both back onto the same `Inventory_Storage`, `07-InventorySystem.md`) and opens Storage, giving direct access to the exact `Inventory_Storage.CraftItem`/`CanCraftItem` machinery documented there.

---

## `Object_ItemPickup.cs` — from the ground into a bag

```csharp
public void SetupItem(Item_DataSO data)
{
    itemData = data;
    SetupVisuals();
    float xDropForce = Random.Range(-dropForce.x, dropForce.x);
    rb.linearVelocity = new Vector2(xDropForce, dropForce.y);
    col.isTrigger = false;
}
```
Called by `Entity_DropManager.CreateItemDrop` (`02-Entity.md`), `Player_DropManager` (`03-Player.md`), or placed directly in a scene with `itemData` pre-assigned (`OnValidate` calls `SetupVisuals()` immediately in the Editor so it shows the right sprite while placing it by hand). A random horizontal "pop" plus a fixed vertical launch gives dropped items a small bouncy arc rather than just appearing static, and — notably — `col.isTrigger = false` means it starts as a **solid** collider, so it can physically fall and bounce via real physics.

```csharp
private void OnCollisionEnter2D(Collision2D collision)
{
    if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && !col.isTrigger)
    {
        col.isTrigger = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }
}
```
The moment it lands on the `Ground` layer, it flips to a **trigger** (so it can now be walked through/detected instead of physically blocking movement) and freezes all further physics movement entirely (`FreezeAll`) — so the item visibly falls and bounces once, then settles permanently in place and becomes pickupable.

```csharp
private void OnTriggerEnter2D(Collider2D collision)
{
    Inventory_Player inventory = collision.GetComponent<Inventory_Player>();
    if (inventory == null) return;

    Inventory_Item itemToAdd = new Inventory_Item(itemData);
    Inventory_Storage storage = inventory?.storage;

    if (itemData.itemType == ItemType.Material && storage != null)
    {
        storage.AddMaterialToStorage(itemToAdd);
        Destroy(gameObject);
        return;
    }
    if (inventory.CanAddItem(itemToAdd))
    {
        inventory.AddItem(itemToAdd);
        Destroy(gameObject);
    }
}
```
Materials skip the general inventory entirely and go straight to the material stash — the exact same rule already seen for direct merchant purchases in `07-InventorySystem.md`'s `Inventory_Merchant.TryBuyItem` (materials never occupy a general inventory slot, whether bought or picked up off the ground). Everything else only gets picked up if `CanAddItem` allows it — if the inventory happens to be full and the item isn't a material, nothing happens and the item simply stays on the ground.

---

## `Object_Buff.cs` — the buff system, finally shown in the wild

```csharp
private void OnTriggerEnter2D(Collider2D collision)
{
    statsToModify = collision.GetComponent<Player_Stats>();
    if (statsToModify != null && statsToModify.CanApplyBuff(buffName))
    {
        statsToModify.ApplyBuff(buffs, buffDuration, buffName);
        Destroy(gameObject);
    }
}
```
A direct, simple caller of `Player_Stats.CanApplyBuff`/`ApplyBuff` (`03-Player.md`) — the exact "buff pickup" this doc set predicted when covering that system. `buffs` is a `BuffEffectData[]` — a tiny `{ StatType Type; float Value; }` pair class configured per-pickup in the Inspector, e.g. a "Strength Potion" prefab might have one entry: `(Strength, +10)`. Uses the same sine-wave floating bob as `Object_NPC`'s tooltip.

---

## `Object_Chest.cs` — composition without `Entity`, and a caching inconsistency worth knowing

```csharp
public class Object_Chest : MonoBehaviour, IDamagable
{
    Rigidbody2D rb => GetComponentInChildren<Rigidbody2D>();
    Animator animator => GetComponentInChildren<Animator>();
    Entity_VFX entityVFX => GetComponent<Entity_VFX>();
    Entity_DropManager dropManager => GetComponent<Entity_DropManager>();
```
Two things worth understanding here. First, the design: `Object_Chest` doesn't extend `Entity` at all — it just implements `IDamagable` directly and reaches for `Entity_VFX`/`Entity_DropManager` via plain `GetComponent`, borrowing exactly the two pieces of `Entity`'s component family it actually needs (`02-Entity.md`) without any of the state-machine/movement/collision-detection machinery a chest has no use for — a clean example of favoring composition over inheritance when a full `Entity` would be overkill for something that never moves.

Second, worth flagging directly: **these four properties use `=>` (expression-bodied) rather than being fields set once in `Awake()`.** That means every single reference to `rb`, `animator`, `entityVFX`, or `dropManager` anywhere in this class triggers a **fresh `GetComponent` call at that exact moment**, rather than reading an already-cached reference. Every other class documented so far caches these in `Awake()` (see `02-Entity.md`'s `Entity.Awake()` for the pattern this deviates from) — `GetComponent` is measurably more expensive than reading a field, and for a chest (interacted with rarely, typically once) the actual cost here is negligible, but it's a genuine, spottable inconsistency with the caching habit used everywhere else in the codebase, worth recognizing for what it is rather than assuming it's intentional.

```csharp
public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
{
    if (!canDropItems) return false;
    dropManager?.DropItems();
    entityVFX.PlayOnDamageVFX();
    animator.SetBool("chestOpen", true);
    rb.linearVelocity = openKnockback;
    rb.angularVelocity = Random.Range(-200f, 200f);
    return true;
}
```
A single hit "opens" the chest — drops loot via the shared `Entity_DropManager` roll (`02-Entity.md`), plays the damage-flash VFX, flips an Animator bool, and pops the chest with a knockback velocity plus a random spin (`angularVelocity`) for a satisfying physical "burst open" flourish rather than the loot just quietly appearing.

Worth a candid, careful observation: **nothing in this method ever sets `canDropItems` back to `false`.** `canDropItems` is only ever read here, never written by code — so as configured, it functions as a manually-set-in-the-Inspector "can this specific chest ever drop items at all" switch (useful for, say, a decorative empty chest), not as an automatic "already opened, don't drop again" lock. Whatever actually prevents a chest from being re-hit and dropping loot a second time — if anything reliably does — isn't happening inside this script. It's possible the chest's changed Animator state or collider setup handles this some other way outside what's shown here, but based on `Object_Chest.cs` alone, this is worth knowing about rather than assuming is fully handled.

---

## How this connects elsewhere

- `Object_Checkpoint`/`Object_Waypoint`/`Object_Portal` are the concrete objects `GameManager.GetNewPlayerPostion` (`05-GameManager-and-Scenes.md`) queries and repositions — this doc completes the picture that chapter only previewed.
- `Object_Checkpoint`/`Object_Portal` both implement `ISaveable` and are discovered by the exact same reflection scan documented in `09-SaveSystem.md`.
- `Object_Merchant`/`Object_Blacksmith`/`Object_ItemPickup` all call directly into `Inventory_Player`/`Inventory_Merchant`/`Inventory_Storage` methods from `07-InventorySystem.md`.
- `Object_Buff` is the real-world caller of `Player_Stats.CanApplyBuff`/`ApplyBuff` from `03-Player.md`.
- `Object_Chest` reuses `IDamagable` (`01-Foundations.md`) and `Entity_VFX`/`Entity_DropManager` (`02-Entity.md`) without inheriting `Entity`.
- `ui.OpenQuestUI`/`OpenMerchantUI`/`OpenStorageUI`/`HideToolTips` all belong to the `UI` hub, covered in the later `UI` doc.

## Where this pattern could be reused later

The **"start hidden by teleporting off-map"** trick in `Object_Portal` is a simple, dependency-free way to represent "not currently placed" for an object that would otherwise need its own visibility/enabled-state bookkeeping — worth remembering as a lightweight alternative to `SetActive(false)` when a trigger-based object specifically needs to stay *enabled* but *unreachable*. The **composition-over-inheritance** choice in `Object_Chest` (implement the interface and borrow just the components you need, rather than inheriting a big base class for two features) is a generally good instinct any time a "shares a *little* behavior with an existing family" object would otherwise be forced to inherit a lot it doesn't need. And the **duplicated `EnsurePlayerInventory` helper** between `Object_Merchant`/`Object_Blacksmith` is worth keeping in mind as a realistic example of when refactoring toward a shared method actually would have paid off — a useful contrast to have alongside the deliberate, well-placed reuse seen in earlier systems.

---

**Next:** `11-Data-ScriptableObjects.md` — `Item_DataSO`/`Equipment_DataSO`, the `ItemEffect_DataSO` family, `Skill_DataSO`, `Stat_SetupSO`, and quest data.
