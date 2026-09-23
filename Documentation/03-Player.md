# 03 — Player: Controls, Combat, and the 15 Player States

## Overview

`Player` extends `Entity` (see `02-Entity.md`) and adds everything specific to *being the human-controlled character*: reading the Input System, owning skills/inventory/quests, and — the bulk of this doc — the 15 concrete states built on `PlayerState` (from `01-Foundations.md`) that define exactly what the player can do in every situation: standing still, walking, jumping, dashing, mid-combo, aiming a sword throw, casting the ultimate, dead.

Same component-per-concern structure as `Entity`: `Player_Stats`, `Player_Health`, `Player_Combat`, `Player_VFX`, `Player_DropManager`, `Player_AnimationTrigger` each *extend* their `Entity_*` counterpart, adding player-only behavior on top via `override`. `Player_SkillManager` and `Player_QuestManager` are entirely new components with no Enemy equivalent.

---

## `Player.cs` — the hub

```csharp
public class Player : Entity
{
    public static Player Instance { get; private set; }
    public static event Action OnPlayerDeath;

    public UI ui { get; private set; }
    public PlayerInputSet input { get; private set; }
    public Player_SkillManager skillManager { get; private set; }
    public Player_VFX VFX { get; private set; }
    public Entity_Health health { get; private set; }
    public Entity_StatusHandler statusHandler { get; private set; }
    public Player_Combat combat { get; private set; }
    public Inventory_Player inventory { get; private set; }
    public Player_Stats stats { get; private set; }
    public Player_QuestManager questManager { get; private set; }
    ...
```

### Why so many `public ... { get; private set; }` properties in one place

This is a deliberate **hub pattern**: rather than every single state and every other script doing its own `GetComponent<Player_Combat>()`, `Player.Awake()` does all those lookups *once* and exposes the results as read-only properties. Every one of the 15 states below reaches things like `player.health`, `player.skillManager`, `player.VFX` through this hub instead of re-fetching components themselves — cheaper, and it means "how do I get to the combat component from a state" always has one obvious answer.

- **`Instance`** — a singleton (see [Concepts-Glossary: Singletons](Concepts-Glossary.md#singletons-instance)), set with no duplicate-protection, same caveat as noted there. Used by other systems that need "the" player without a scene reference — e.g. `Enemy_Health.Start()` grabs `Player.Instance.questManager` directly.
- **`OnPlayerDeath`** — a **static** event, worth pausing on: because there's only ever one player, this event doesn't need an instance to subscribe to (`Player.OnPlayerDeath += ...` rather than `somePlayerInstance.OnPlayerDeath += ...`). `Enemy.cs` subscribes to this in its own `OnEnable()`/unsubscribes in `OnDisable()` to know "the player just died, stop fighting" — covered in `04-Enemy.md`.

### `Awake()` — wiring components and building every state

```csharp
protected override void Awake()
{
    base.Awake();
    Instance = this;

    ui = FindAnyObjectByType<UI>();
    VFX = GetComponent<Player_VFX>();
    ... // more GetComponent calls
    input = new PlayerInputSet();
    ui.SetUpControlsUI(input);

    idleState = new Player_IdleState(this, stateMachine, "idle");
    moveState = new Player_MoveState(this, stateMachine, "move");
    jumpState = new Player_JumpState(this, stateMachine, "jumpFall");
    fallState = new Player_FallState(this, stateMachine, "jumpFall");
    wallSlideState = new Player_WallSlideState(this, stateMachine, "wallSlide");
    wallJumpState = new Player_WallJumpState(this, stateMachine, "jumpFall");
    dashState = new Player_DashState(this, stateMachine, "dash");
    basicAttackState = new Player_BasicAttackState(this, stateMachine, "basicAttack");
    jumpAttackState = new Player_JumpAttackState(this, stateMachine, "jumpAttack");
    deadState = new Player_DeadState(this, stateMachine, "dead");
    counterAttackState = new Player_CounterAttackState(this, stateMachine, "counterAttack");
    swordThrowState = new Player_SwordThrowState(this, stateMachine, "swordThrow");
    domainExpansionState = new Player_DomainExpansionState(this, stateMachine, "jumpFall");
}
```

`FindAnyObjectByType<UI>()` is a one-time scene search for the UI hub (covered in a later doc) — acceptable here because it only runs once at startup, not every frame. `input = new PlayerInputSet()` creates the generated Input System wrapper (see [Concepts-Glossary: the new Input System](Concepts-Glossary.md#the-new-input-systems-generated-wrapper-playerinputset)); `ui.SetUpControlsUI(input)` hands that same instance to the UI so UI panels can also read/disable input through the same object rather than each creating their own.

Worth noticing in the state list: **`jumpState`, `fallState`, `wallJumpState`, and `domainExpansionState` all share the animator bool `"jumpFall"`.** These are four *logically* distinct states — different code, different transition rules — that all happen to want the exact same "airborne" animation. A nice concrete example of the point made in `01-Foundations.md` that the code-side state machine and the Animator Controller are two separate systems kept in sync only by these string names; they don't have to be 1:1.

### `SlowDownEntityCo` override — this is what Ice chill actually *does* to the player

```csharp
protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
{
    float originalMoveSpeed = moveSpeed;
    ...
    Vector2[] originalAttackVelocity = new Vector2[attackVelocity.Length];
    Array.Copy(attackVelocity, originalAttackVelocity, attackVelocity.Length);

    float speedMultiplier = 1 - slowMultiplier;
    moveSpeed *= speedMultiplier;
    jumpForce *= speedMultiplier;
    animator.speed *= speedMultiplier;
    wallJumpForce *= speedMultiplier;
    jumpAttackVelocity *= speedMultiplier;
    for (int i = 0; i < attackVelocity.Length; i++) attackVelocity[i] *= speedMultiplier;

    yield return new WaitForSeconds(duration);

    moveSpeed = originalMoveSpeed;
    // ...restore everything else the same way
}
```

Recall from `02-Entity.md` that `Entity.SlowDownEntityCo` is an empty stub — this is the real implementation, and it's the direct payoff of that template-method design: `Entity_StatusHandler.ApplyChillEffect` calls `entity.SlowDownEntity(...)` without knowing or caring that it's a `Player` underneath; this override is what actually happens.

One detail worth understanding even though it's not commented in the source: **`Array.Copy(attackVelocity, originalAttackVelocity, ...)` instead of `originalAttackVelocity = attackVelocity`.** An array is a reference type — if this had just written `originalAttackVelocity = attackVelocity`, both variables would point at the *same* array in memory, so scaling `attackVelocity[i] *= speedMultiplier` right afterward would also silently scale `originalAttackVelocity`, defeating the whole point of "remember the original values to restore later." `Array.Copy` makes an actual second array with duplicated values, which is what makes the restore-after-`WaitForSeconds` block below correct.

### Death, combos, interaction, input wiring

```csharp
public override void EntityDeath()
{
    base.EntityDeath();
    OnPlayerDeath?.Invoke();
    stateMachine.ChangeState(deadState);
}
```
Fires the static event, letting every subscribed `Enemy` know to stand down, and switches to `deadState` — see that state's section below for how it locks things down.

```csharp
public void EnterAttackStateWithDelay()
{
    if (queuedAttackCo != null) StopCoroutine(queuedAttackCo);
    queuedAttackCo = StartCoroutine(EnterAttackStateWithDelayCo());
}

private IEnumerator EnterAttackStateWithDelayCo()
{
    yield return new WaitForEndOfFrame();
    stateMachine.ChangeState(basicAttackState);
}
```
This exists purely to support the combo system in `Player_BasicAttackState` (below). When a combo continues, the code needs to re-enter the *same* `basicAttackState` object (to play the next swing), but it waits one frame (`WaitForEndOfFrame`) before doing so. The likely reason: `Player_BasicAttackState.HandleStateExit()` (below) manually clears the Animator bool the moment the combo continuation is decided, and re-entering the state on the very same frame — setting that same bool straight back to `true` again — risks the Animator Controller not registering a clean false→true transition, since both changes would land in the same frame. Waiting one full frame guarantees the "false" is actually observed before "true" happens again, so the attack animation restarts cleanly instead of potentially not transitioning at all.

```csharp
public void TryInteract()
{
    Transform closest = null;
    float closestDistance = Mathf.Infinity;
    Collider2D[] interactablesAround = Physics2D.OverlapCircleAll(transform.position, 1.5f);

    foreach (var target in interactablesAround)
    {
        IInteractable interactable = target.GetComponent<IInteractable>();
        if (interactable == null) continue;

        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance < closestDistance) { closestDistance = distance; closest = target.transform; }
    }

    if (closest == null) return;
    closest.GetComponent<IInteractable>().Interact();
}
```
Same overlap-check idea as `Entity_Combat.PreformAttack()`, but note this call to `OverlapCircleAll` **doesn't pass a `LayerMask`** — it checks every collider in range regardless of physics layer, relying entirely on "does this have an `IInteractable`?" as the filter. Finds the *closest* interactable within 1.5 units, not just the first one found, and calls `Interact()` on it alone — so standing between two NPCs only interacts with the nearer one.

```csharp
private void OnEnable()
{
    input.Enable();
    input.Player.Mouse.performed += ctx => mousePosition = ctx.ReadValue<Vector2>();
    input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
    input.Player.Movement.canceled += ctx => moveInput = Vector2.zero;
    input.Player.Spell.performed += ctx => skillManager.shard.TryUseSkill();
    input.Player.Spell.performed += ctx => skillManager.timeEcho.TryUseSkill();
    input.Player.Interact.performed += ctx => TryInteract();
    input.Player.QuickItemSlot_1.performed += ctx => inventory.TryUseQuickItem(1);
    input.Player.QuickItemSlot_2.performed += ctx => inventory.TryUseQuickItem(2);
}
```
This is the subscribing half of input handling (the polling half — `WasPressedThisFrame()` — happens inside the states themselves). Notice **one action, `Spell`, is wired to two different skills** (`shard.TryUseSkill()` *and* `timeEcho.TryUseSkill()`) — both fire on the same button press. This only makes sense once you know (from the SkillSystem doc, later) that `TryUseSkill()` presumably checks internally whether that particular skill is the one currently equipped/unlocked for that slot — pressing "Spell" tries both, and whichever one is actually active does something, the other silently no-ops. Movement and Mouse are continuous values read via `ReadValue<Vector2>()` every time they change; Jump/Attack/Dash/etc. are read by polling inside states instead, since "was this pressed this exact frame" is naturally a per-frame question a state's `Update()` is already positioned to ask.

---

## Player's own component family

### `Player_SkillManager.cs`

```csharp
public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_SwordThrow swordThrow { get; private set; }
    public Skill_TimeEcho timeEcho { get; private set; }
    public Skill_DomainExpansion domainExpansion { get; private set; }
    public Skill_Base[] allSkills { get; private set; }

    private void Awake()
    {
        dash = GetComponentInChildren<Skill_Dash>();
        ... // one GetComponentInChildren per skill
        allSkills = GetComponentsInChildren<Skill_Base>();
    }
```

Each of the five skills lives as its own component on a **child** GameObject under the player, hence `GetComponentInChildren`, not `GetComponent` — full detail in the SkillSystem doc, but functionally this is the same hub idea as `Player.cs` itself, scoped to just skills. `GetComponentsInChildren<Skill_Base>()` (plural, no specific type) collects *all* of them into one array by their shared base class, useful for "do something to every skill" operations without listing all five by name:

```csharp
public void ReduceAllSkillsCooldownBy(float cooldownReduction)
{
    foreach (var skill in allSkills) skill.ReduceCooldownBy(cooldownReduction);
}

public Skill_Base GetSkillByType(SkillType skillType)
{
    switch (skillType)
    {
        case SkillType.Dash: return dash;
        case SkillType.TimeShard: return shard;
        ...
        default: Debug.LogWarning(...); return null;
    }
}
```
`GetSkillByType` is the **same lookup-table pattern** as `Entity_Stats.GetStatByType` from `02-Entity.md` — an enum (`SkillType`, from `01-Foundations.md`) mapped to the actual object via a `switch`, so generic code (a skill-tree UI node, a save-file entry) can say "give me `SkillType.Dash`" without a hardcoded reference to the `dash` field specifically. You'll likely see this same "enum in, concrete object out via switch" shape again in later systems — it's this codebase's go-to way of letting data-driven code (UI, save files) address specific gameplay objects generically.

### `Player_Stats.cs` — buffs on top of the stat system

```csharp
public class Player_Stats : Entity_Stats
{
    private List<string> activeBuff = new List<string>();

    public bool CanApplyBuff(string buffName) => !activeBuff.Contains(buffName);

    public void ApplyBuff(BuffEffectData[] buffsToApply, float duration, string buffName)
    {
        StartCoroutine(BuffDurationCo(buffsToApply, duration, buffName));
    }

    private IEnumerator BuffDurationCo(BuffEffectData[] buffsToApply, float duration, string buffName)
    {
        activeBuff.Add(buffName);
        foreach (var buff in buffsToApply)
            GetStatByType(buff.Type).AddModifier(buff.Value, buffName);

        yield return new WaitForSeconds(duration);

        foreach (var buff in buffsToApply)
            GetStatByType(buff.Type).RemoveModifier(buffName);

        inventory.TriggerUpdateUI();
        activeBuff.Remove(buffName);
    }
}
```
Uses `GetStatByType` (inherited from `Entity_Stats`) plus `Stat.AddModifier(value, source)`/`RemoveModifier(source)` (from `Stat.cs`, briefly introduced in `02-Entity.md`, fully covered later). The `buffName` string doubles as the modifier's `source` tag, which is exactly what lets `RemoveModifier(buffName)` later remove precisely the modifiers *this* buff added, without touching modifiers any other buff or equipment piece added. `CanApplyBuff` — a simple `List<string>.Contains` check — is what an item effect (covered later) calls first to avoid double-applying (and therefore double-removing) the same named buff while it's still active. After the buff wears off, `inventory.TriggerUpdateUI()` tells the character sheet to refresh its displayed numbers now that the temporary modifiers are gone.

### `Player_DropManager.cs` — "lose your stuff on death," not loot tables

```csharp
public override void DropItems()
{
    List<Inventory_Item> inventoryCopy = new List<Inventory_Item>(inventory.itemList);
    List<Inventory_EquipmentSlot> equipmentCopy = new List<Inventory_EquipmentSlot>(inventory.equipmentList);

    foreach (var item in inventoryCopy)
    {
        if (Random.Range(0f, 100f) < chanceToLoseItem)
        {
            CreateItemDrop(item.itemData);
            inventory.RemoveAllItems(item);
        }
    }

    foreach (var equipment in equipmentCopy)
    {
        if (equipment.HasItem() && Random.Range(0f, 100f) < chanceToLoseItem)
        {
            var item = equipment.equippedItem;
            CreateItemDrop(item.itemData);
            inventory.UnequipItemFromSlot(item);
            inventory.RemoveAllItems(item);
        }
    }
}
```
This `override`s `Entity_DropManager.DropItems()` **entirely** — it doesn't call `base.DropItems()` at all, because the Player's death-drop rule is a completely different design from the Enemy's rarity-budget loot roll covered in `02-Entity.md`. Instead of rolling from a curated `ItemList_DataSO` table, the player has an independent `chanceToLoseItem` (default 90%) roll **per item currently owned** — every inventory stack and every equipped item individually risks being dropped and removed. A punishing "lose your gear on death" mechanic, not a reward mechanic.

The `new List<>(inventory.itemList)` / `new List<>(inventory.equipmentList)` copies at the top are important, not decorative: the loop body calls `inventory.RemoveAllItems(item)` / `UnequipItemFromSlot(item)`, which mutate the *real* inventory lists. Iterating directly over a list while removing items from it mid-loop is a classic C# error (`InvalidOperationException: Collection was modified`), so the code deliberately loops over a frozen snapshot copy instead, while still mutating the live inventory safely from inside that loop. Worth remembering as a general technique any time you need to both iterate *and* mutate the same collection.

### `Player_VFX.cs` — the dash afterimage effect

```csharp
public void DoImageEcho(float duration)
{
    if (imageEchoCoroutine != null) StopCoroutine(imageEchoCoroutine);
    imageEchoCoroutine = StartCoroutine(ImageEchoCo(duration));
}

private IEnumerator ImageEchoCo(float duration)
{
    float timeTracker = 0f;
    while (timeTracker < duration)
    {
        CreateImageEcho();
        yield return new WaitForSeconds(imageEchoInterval);
        timeTracker += imageEchoInterval;
    }
}

private void CreateImageEcho()
{
    GameObject echo = Instantiate(imageEchoPrefab, transform.position, transform.rotation);
    echo.GetComponentInChildren<SpriteRenderer>().sprite = spriteRenderer.sprite;
}
```
Called from `Player_DashState.Enter()` (below) with `player.dashDuration`. Every `imageEchoInterval` seconds (default 0.05s) for the whole dash, it spawns a copy of the echo prefab and — the key trick — **steals the player's exact current animation frame** (`spriteRenderer.sprite`) and puts it on the echo. That's what makes each echo look like a frozen snapshot of the player mid-dash-pose rather than a generic blob; the echo prefab itself presumably just fades/destroys itself afterward (its own `VFX_AutoController`, covered in a later doc).

### `Player_QuestManager.cs`

```csharp
public void AddProgress(string questTargetId, int amount = 1)
{
    foreach (var quest in activeQuests)
    {
        if (quest.questDataSO.questTargetID != questTargetId) continue;
        quest.AddQuestProgress(amount);
    }
}
```
Called from `Enemy_Health.Die()` (see `04-Enemy.md`) with that enemy's own `questTargetID` — killing an enemy automatically advances *every* active quest that happens to target that same ID string, without the enemy needing to know anything about quests beyond carrying that one ID field. `AcceptQuest`/`QuestIsActive` round out the API — wrapping a `Quest_DataSO` into runtime `QuestData` on accept, checking by reference via `List.Find` with a lambda — full detail belongs to a later Quest-focused doc, included here only because `Player_QuestManager` itself lives in the `Player/` folder.

### `Player_Combat.cs` — adds the counter-attack check

```csharp
public class Player_Combat : Entity_Combat
{
    public bool CounterAttackPreformed()
    {
        bool hasPerformedCounter = false;
        foreach (var target in GetDetectedColliders())
        {
            ICounterable counterable = target.GetComponent<ICounterable>();
            if (counterable == null) continue;
            if (counterable.CanBeCountered)
            {
                counterable?.HandleCounter();
                hasPerformedCounter = true;
            }
        }
        return hasPerformedCounter;
    }
}
```
Reuses `GetDetectedColliders()` (declared `protected` in `Entity_Combat` specifically so this subclass could reuse the same overlap-circle logic instead of duplicating it) but checks for `ICounterable` (from `01-Foundations.md`) instead of `IDamagable` — the same "overlap check, filter by interface" shape as `PreformAttack`, applied to a different contract. `Player_CounterAttackState` (below) is what actually calls this.

### `Player_Health.cs` and `Player_AnimationTrigger.cs`

```csharp
protected override void Die()
{
    base.Die();
    // GameManager.Instance.SetLastPlayerPosition(transform.position);
    // GameManager.Instance.RestartScene();
    player.ui.OpenDeathScreenUI();
}
```
Runs everything `Entity_Health.Die()` normally does (set `isDead`, call `EntityDeath()` → `deadState`, roll drops via `Player_DropManager`), then opens the death screen UI. The two commented-out lines are leftover from an earlier, simpler respawn approach (restart the whole scene from a remembered position) that predates the current checkpoint/waypoint-based `GameManager` respawn flow described in CLAUDE.md — safe to ignore, mentioned only so you recognize it as history rather than a hint something's broken.

```csharp
public class Player_AnimationTrigger : Entity_AnimationTriggers
{
    private void ThrowSword() => player.skillManager.swordThrow.ThrowSword();
}
```
Same Animation-Event-bridge pattern as `Entity_AnimationTriggers.AttackTrigger()` from `02-Entity.md` — `ThrowSword()` has no C# callers either; it's invoked by name from a specific frame of the sword-throw animation clip, timing the actual projectile spawn to the visual swing rather than the button press.

---

## The 15 Player states

### The two shared middle layers

Two states aren't meant to be entered directly — they exist purely so the *actually* concrete states below them can share logic, the same "put it as low as it's needed" idea from `01-Foundations.md`.

**`Player_GroundedState`** (extends `PlayerState`) — the "what can happen while standing on the ground" checklist:
```csharp
public override void Update()
{
    base.Update();
    if (rb.linearVelocity.y < 0 && !player.groundDetected) stateMachine.ChangeState(player.fallState);
    if (input.Player.Jump.WasPressedThisFrame()) stateMachine.ChangeState(player.jumpState);
    if (input.Player.Attack.WasPressedThisFrame()) stateMachine.ChangeState(player.basicAttackState);
    if (input.Player.CounterAttack.WasPressedThisFrame()) stateMachine.ChangeState(player.counterAttackState);
    if (input.Player.RangeAttack.WasPressedThisFrame() && skillManager.swordThrow.CanUseSkill())
        stateMachine.ChangeState(player.swordThrowState);
}
```
`Player_IdleState` and `Player_MoveState` both inherit this, meaning "jump, attack, counter, and sword-throw are all always available whether you're standing still or walking" without either state repeating the checks.

**`Player_AiredState`** (extends `PlayerState`) — the airborne equivalent:
```csharp
public override void Update()
{
    base.Update();
    if (player.moveInput.x != 0)
        player.SetVelocity(player.moveInput.x * (player.moveSpeed * player.inAirMoveMultiplier), rb.linearVelocity.y);
    if (input.Player.Attack.WasPressedThisFrame()) stateMachine.ChangeState(player.jumpAttackState);
}
```
`Player_JumpState` and `Player_FallState` both inherit this — reduced-speed air control (`inAirMoveMultiplier`, default 0.7, i.e. 70% of ground speed) and the jump-attack dive, available in both the rising and falling halves of a jump.

### Locomotion states

- **`Player_IdleState`** (extends `GroundedState`) — zeroes horizontal velocity on `Enter`. Its `Update` has a small but deliberate carve-out: if you're holding the direction you're already facing *and* a wall is detected that way, it returns early rather than switching to `Move`, so you don't visibly "walk in place" against a wall you're pushing into; any other horizontal input switches to `Move` normally.
- **`Player_MoveState`** (extends `GroundedState`) — switches back to `Idle` the instant there's no input or a wall blocks the way, but note the order: `SetVelocity(...)` is still called at the bottom of `Update` *after* that `ChangeState(idleState)` call. Calling `ChangeState` only swaps which state object is "current" for *next* frame — it doesn't stop the rest of the currently-running method, so this exact frame still applies movement velocity as `Move`, even though `Idle` is technically already the active state by the time this method returns. A subtle but useful thing to understand about how these C# state machines behave, since it's easy to assume `ChangeState` interrupts execution the way a `return` would.
- **`Player_JumpState`** (extends `AiredState`) — `Enter` sets vertical velocity to `jumpForce`. Its guard against switching to `Fall` is explained by a comment directly in the source worth quoting: falling-velocity checks could otherwise misfire the same frame `Player_JumpAttackState` sets its own negative Y velocity, so the check explicitly excludes `player.jumpAttackState` from triggering a `Fall` transition.
- **`Player_FallState`** (extends `AiredState`) — switches to `Idle` on landing, or `WallSlide` if a wall's detected while still falling.
- **`Player_WallSlideState`** (extends `PlayerState` directly — sliding is neither "grounded" nor plain "airborne," so it doesn't fit either middle layer) — holding down accelerates the slide to full fall speed, otherwise `wallSlideSlowMultiplier` (default 0.7) slows it into a controlled slide. Jump input goes to `WallJump`; losing the wall goes to `Fall`; touching ground goes to `Idle`, with a small correction (`if (player.facingDir != player.moveInput.x) player.Flip();`) so landing while still holding the opposite direction from your wall-facing doesn't leave you facing the wrong way.
- **`Player_WallJumpState`** (extends `PlayerState`) — pushes off with `wallJumpForce`, negated against `facingDir` (away from whichever wall you're jumping off) — falling switches to `Fall`, grabbing another wall mid-air switches back to `WallSlide`.
- **`Player_DashState`** (extends `PlayerState`) — see full breakdown below, it's one of the richer states.
- **`Player_DeadState`** (extends `PlayerState`) — `Enter()` disables input entirely (`input.Disable()`) and sets `rb.simulated = false`, which fully turns off physics for this Rigidbody — no more gravity, no more residual velocity, the character freezes exactly in its death pose. No `Update()` override at all: nothing more ever happens in this state. Worth noting as an asymmetry with `Enemy_DeadState` (`04-Enemy.md`): the Enemy version explicitly calls `stateMachine.SwitchOffStateMachine()`, while `Player_DeadState` never does — the player achieves the same "stop reacting to everything" end result through disabled input and disabled physics instead, not through the state-machine kill switch from `01-Foundations.md`.

#### `Player_DashState` in detail
```csharp
public override void Enter()
{
    base.Enter();
    skillManager.dash.OnStartEffect();
    player.VFX.DoImageEcho(player.dashDuration);
    stateTimer = player.dashDuration;
    dashDir = player.moveInput.x != 0 ? (int)player.moveInput.x : player.facingDir;
    originalGravityScale = rb.gravityScale;
    rb.gravityScale = 0;
    player.health.SetCanTakeDamage(false);
}
```
`skillManager.dash.OnStartEffect()` (and the mirrored `OnEndEffect()` in `Exit()`) are hooks into the actual `Skill_Dash` object — a preview of the SkillSystem doc: depending on which upgrade path is unlocked (recall `Dash_CloneOnStart`/`Dash_ShardOnStart`/etc. from `01-Foundations.md`'s `SkillUpgradeType`), these calls are where a clone or time-shard actually gets spawned at the start/end of a dash. Gravity is zeroed for a flat, non-arcing dash, and damage is disabled for the dash's duration (i-frames) — the same `SetCanTakeDamage(false)`/`(true)` pattern reused again in `Player_DomainExpansionState` below. `CancelDash()` in `Update()` checks for a wall mid-dash and bails early into `Idle`/`WallSlide` rather than letting the dash carry the player into (and visually through) solid geometry.

### Combat states

- **`Player_BasicAttackState`** — the melee combo system, detailed below.
- **`Player_JumpAttackState`** — a dive attack: `Enter` launches with a fixed `jumpAttackVelocity` in the facing direction; the first frame it detects ground (guarded by a `touchedGround` flag so this only fires once), it triggers the Animator's `"jumpAttackTrigger"` **trigger parameter** and kills horizontal velocity. A quick note on *trigger* parameters versus the *bool* parameters used elsewhere: a trigger is a one-shot pulse the Animator consumes automatically once it's used to fire a transition, unlike a bool which stays set until code explicitly flips it back — better suited here for "the exact moment of landing impact" than a bool would be. Once `triggerCalled` fires (the landing animation reaching its key frame) and the player is grounded, it returns to `Idle`.
- **`Player_CounterAttackState`** — `Enter` immediately calls `player_Combat.CounterAttackPreformed()` and remembers whether it actually connected (`counteredSomebody`), feeding that into a `"counterAttackPerformed"` Animator bool so a successful counter can play a different animation from a whiffed one. `Update` forces horizontal velocity to zero every frame as an explicit safety measure (per the source's own comment) against residual momentum during the counter animation, and returns to `Idle` either once the (successful) animation's trigger fires, or once the recovery timer runs out on a whiff.
- **`Player_SwordThrowState`** — the one state driven by the mouse instead of movement input. `DirectionToMouse()` converts the on-screen mouse position into a world-space aim direction via `Camera.main.ScreenToWorldPoint`; every frame while this state is active the player is held stationary and kept facing the cursor while `skillManager.swordThrow.PredictTrajectory(...)` presumably draws an aim preview. Pressing Attack "locks in" the throw (confirms direction, disables the aim dot, flags an Animator bool); releasing the button that opened this state, or the animation trigger firing, both return to `Idle` — so this state doubles as "hold to aim, press Attack to commit, or just let go to cancel."
- **`Player_DomainExpansionState`** — the ultimate, detailed below.

#### `Player_BasicAttackState` in detail — the combo system

```csharp
int comboIndex = 1;
int comboLimit = 3;
public Player_BasicAttackState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
{
    if (comboLimit != player.attackVelocity.Length)
        comboLimit = player.attackVelocity.Length;
}
```
`comboLimit` is data-driven: it's synced in the constructor to however many entries exist in `player.attackVelocity[]` (an Inspector-configured array on `Player.cs`) rather than being hardcoded — add a 4th entry to that array in the Inspector and the combo automatically supports a 4th hit.

```csharp
public override void Enter()
{
    base.Enter();
    comboAttackQueued = false;
    ResetComboIndexIfNeeded();
    SyncAttackSpeed();
    attackDir = player.moveInput.x != 0 ? (int)player.moveInput.x : player.facingDir;
    animator.SetInteger("basicAttackIndex", comboIndex);
    ApplyAttackVelocity();
}
```
`ResetComboIndexIfNeeded()` resets `comboIndex` back to `1` if either the combo previously maxed out, or too much real time (`comboResetTime`) passed since the last swing — so attacking again after a pause always starts a fresh combo rather than resuming mid-chain. `SyncAttackSpeed()` (from `EntityState`, `01-Foundations.md`) scales the Animator's playback speed by the attack-speed stat. `animator.SetInteger("basicAttackIndex", comboIndex)` is how the Animator Controller knows *which* swing animation variant to play for this hit. `ApplyAttackVelocity()` looks up `player.attackVelocity[comboIndex - 1]` and applies it as a brief directional lunge (cleared again after `attackVelocityDuration` by `HandleAttackVelocity()` in `Update`) — each combo hit gets its own configurable little forward burst.

```csharp
private void HandleStateExit()
{
    if (comboAttackQueued)
    {
        animator.SetBool(animBoolName, false);
        player.EnterAttackStateWithDelay();
    }
    else
        stateMachine.ChangeState(player.idleState);
}
```
This is the crux of the whole combo system, and it's easy to misread: **all combo hits reuse the exact same `Player_BasicAttackState` object** (`player.basicAttackState`, constructed once in `Player.Awake()`), rather than there being a separate state per combo hit. `Player.EnterAttackStateWithDelay()` (covered above) waits one frame, then calls `stateMachine.ChangeState(player.basicAttackState)` — and since that's *already* the current state, `ChangeState` still runs `Exit()` (which increments `comboIndex`) followed by `Enter()` again on the same object, which is what actually advances and replays the combo. Pressing Attack again mid-swing just sets `comboAttackQueued = true` (via `QueueNextAttack()`, itself guarded so you can't queue past `comboLimit`); whether that queued flag is true or false at the moment the current swing's animation trigger fires is what decides "chain into the next hit" versus "go back to `Idle`."

#### `Player_DomainExpansionState` in detail — the ultimate

```csharp
public override void Enter()
{
    base.Enter();
    originalPosition = player.transform.position;
    originalGravity = rb.gravityScale;
    maxDistanceToGoUp = GetAvailableRiseDistance();
    player.SetVelocity(0, player.riseSpeed);
    player.health.SetCanTakeDamage(false);
}

private float GetAvailableRiseDistance()
{
    RaycastHit2D hit = Physics2D.Raycast(player.transform.position, Vector2.up, player.riseMaxDistance, player.GetWhatIsGround());
    return hit.collider != null ? hit.distance - 1f : player.riseMaxDistance;
}
```
Before rising, it raycasts straight up (see [Concepts-Glossary: structs & `default`](Concepts-Glossary.md#structs-default-and-why-a-no-hit-raycast-isnt-null) for how "no hit" is represented) to check for a low ceiling within `riseMaxDistance`, trimming 1 unit of clearance off if one's found — so the rise-up animation never visually clips the player through terrain.

```csharp
public override void Update()
{
    base.Update();
    if (Vector2.Distance(originalPosition, player.transform.position) >= maxDistanceToGoUp && !isLevitating)
        Levitate();

    if (isLevitating)
    {
        skillManager.domainExpansion.DoSpellCasting();
        if (stateTimer <= 0)
        {
            isLevitating = false;
            rb.gravityScale = originalGravity;
            stateMachine.ChangeState(player.idleState);
        }
    }
}

private void Levitate()
{
    isLevitating = true;
    rb.linearVelocity = Vector2.zero;
    rb.gravityScale = 0;
    stateTimer = skillManager.domainExpansion.GetDomainDuration();
    if (!createdDomain) { createdDomain = true; skillManager.domainExpansion.CreateDomain(); }
}
```
Two phases: rise straight up until reaching the (possibly ceiling-clamped) distance, then `Levitate()` freezes the player in a zero-gravity hover, starts a fresh `stateTimer` from the skill's own configured duration, and — guarded by `createdDomain` so it only ever happens once per cast — spawns the actual domain effect. Every frame while levitating, `skillManager.domainExpansion.DoSpellCasting()` runs — this is presumably where upgrade-path-specific behavior lives (recall `Domain_EchoSpam`/`Domain_ShardSpam`/`Domain_SlowingDown` from `01-Foundations.md`'s `SkillUpgradeType` enum), covered fully in the SkillSystem doc. Same `SetCanTakeDamage(false)`/`(true)` i-frame pattern as `Player_DashState` protects the player for the whole cast.

---

## How this connects elsewhere

- Every state here is built on `PlayerState`/`EntityState` from `01-Foundations.md` — read that first if any of the `Enter`/`Update`/`Exit`/`triggerCalled` vocabulary here felt unfamiliar.
- `Player`'s `health`/`combat`/`stats`/`VFX` properties are `Player_*` subclasses of the `Entity_*` components from `02-Entity.md` — the override sections above show exactly what each one adds on top.
- `Player_SkillManager` and the skill hooks seen in `Player_DashState`/`Player_DomainExpansionState`/`Player_SwordThrowState` all point forward to the SkillSystem doc, where `Skill_Base` and each `Skill_*`/`SkillObject_*` pair are covered in full.
- `Player_QuestManager.AddProgress` is called from `Enemy_Health.Die()` — see `04-Enemy.md`, next.
- `Player.OnPlayerDeath` (static event) is subscribed to by `Enemy.cs` — also covered next.

## Where this pattern could be reused later

The **hub component** pattern (`Player.cs` resolving every sibling component once in `Awake()` and exposing them as properties) is worth reaching for in any Unity project with a player object that has many interacting parts — it keeps `GetComponent` calls to a minimum and gives every other script one obvious place to reach the player's pieces from. The **combo system's "reuse one state instance, re-`Enter`/`Exit` it to advance an internal index"** trick is a neat, non-obvious technique worth remembering specifically for any attack-chain/combo mechanic in a future FSM-driven character controller — it avoids needing N nearly-identical state classes for an N-hit combo. And the **i-frame-via-flag** pattern (`SetCanTakeDamage(false)` on enter, `(true)` on exit, used identically in Dash and Domain Expansion) is a simple, reusable way to grant temporary invulnerability tied to a state's lifetime without any separate timer bookkeeping.

---

**Next:** `04-Enemy.md` — `Enemy.cs`, its smaller component family, the battle-AI state machine (Idle → Move → Battle → Attack/Stunned/Dead), and the concrete `Enemy_Skeleton`.
