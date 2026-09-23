# 01 — Foundations: Enums, Interfaces, State Machine

## Overview

Everything else in this codebase is built on top of three small folders: `Scripts/Enums/`, `Scripts/Interface/`, and `Scripts/StateMachine/`. None of these do anything exciting on their own — they're **vocabulary and plumbing**. But `Player.cs`, `Enemy.cs`, combat, inventory, and the UI all lean on them constantly, so they're the right place to start.

If a term below is unfamiliar (MonoBehaviour, interface, abstract class, enum, event...), check `Concepts-Glossary.md` — this doc links to it rather than re-explaining from scratch.

---

## Part 1 — Enums (`Scripts/Enums/`)

An enum is a fixed, named list of options (see [Concepts-Glossary: Enums](Concepts-Glossary.md#enums)). This project uses six of them, all tiny, all just data — no methods, no logic.

### `ElementType.cs`
```csharp
public enum ElementType { None, Fire, Ice, Lightning }
```
The three elemental damage types in the game, plus `None` for "no element." Used everywhere elemental damage or resistance is calculated (`Entity_Stats`, `Entity_StatusHandler`, `Entity_VFX` for tinting), and by `IDamagable.TakeDamage(...)` as one of its parameters.

### `ItemType.cs`
```csharp
public enum ItemType { Material, Weapon, Armor, Trinket, Consumable }
```
The high-level category an inventory item falls into. Used by the inventory/equipment system to decide things like which equipment slot an item can go in, or whether it should stack.

### `RespawnType.cs`
```csharp
public enum RespawnType { Enter, Exit, NoneSpecific, Portal }
```
Tells `GameManager.ChangeScene(...)` *how* to decide where the player should appear after a scene change:

- `Enter`/`Exit` — match a specific paired waypoint (portals, scene-edge transitions).
- `NoneSpecific` — "just put me at the closest unlocked checkpoint or entry waypoint to where I last died" (used after respawning from death).
- `Portal` — used by the portal system specifically.

This will make more sense once `GameManager` and `Object_Portal`/`Object_Waypoint` are covered later — for now, just know it's a "which respawn rule applies" flag.

### `SkillType.cs`
```csharp
public enum SkillType { Dash, TimeEcho, TimeShard, SwordThrow, DomainExpansion }
```
The five active skills in the skill tree. Each corresponds to a `Skill_*` class (`Skill_Dash`, `Skill_TimeEcho`, etc.) — this enum is how UI and save data refer to "which skill" without needing a direct C# reference to the class.

### `SkillUpgradeType.cs`
```csharp
public enum SkillUpgradeType
{
    None,
    Dash, Dash_CloneOnStart, Dash_CloneOnStartAndArrival, Dash_ShardOnStart, Dash_ShardOnStartAndArrival,
    Shard, Shard_MoveToEnemy, Shard_MultiCast, Shard_Teleport, Shard_TeleportHpRewind,
    SwordThrow, SwordThrow_Spin, SwordThrow_Pierce, SwordThrow_Bounce,
    TimeEcho, TimeEcho_SingleAttack, TimeEcho_MultiAttack, TimeEcho_DuplicateChance, TimeEcho_HealWisp, TimeEcho_CleanseWisp, TimeEcho_CooldownWisp,
    Domain_SlowingDown, Domain_EchoSpam, Domain_ShardSpam,
}
```
The biggest enum in the project (40 lines), because it's every single **unlockable node** in the skill tree, grouped by which skill tree they belong to (the `// ----- X Tree -----` comments are just organizational, not functional). Notice the naming pattern: `Dash_CloneOnStart` reads as "the Dash tree's CloneOnStart upgrade." This is what `UI/Skill Tree UI/` uses to identify each node, and what the skill-tree save data stores to remember which nodes are unlocked. Each value has an inline `//` comment describing what that upgrade does in-game — worth skimming directly in the file for a one-glance overview of the whole skill tree's design.

### `StatType.cs`
```csharp
public enum StatType
{
    MaxHealth, HealthRegen, Strength, Agility, Intelligence, Vitality,
    AttackSpeed, Damage, CritChance, CritPower, ArmorReduction,
    FireDamage, IceDamage, LightningDamage,
    Armor, Evasion, FireResistance, IceResistance, LightningResistance, ElementalDamage
}
```
Every stat that exists on an Entity. Used by `Entity_Stats.GetStatByType(StatType)` (covered in `02-Entity.md`) to look up the actual `Stat` object for a given type — this is how generic UI code (a stat panel, a tooltip) can say "show me `StatType.CritChance`" without knowing the internal field name `offenseGroup.critChance` directly.

---

## Part 2 — Interfaces (`Scripts/Interface/`)

An interface is a contract: "anything that implements me must provide these methods," with zero shared implementation (see [Concepts-Glossary: Interfaces](Concepts-Glossary.md#interfaces)). All four here are tiny — one or two methods each — which is normal; interfaces are meant to be small, focused promises.

### `IDamagable.cs`
```csharp
public interface IDamagable
{
    public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer);
}
```
The contract for "this thing can be hurt." Implemented by `Entity_Health` (so Player and Enemy can be damaged) and by `Object_Chest` (so a chest can be "hurt" to open it — CLAUDE.md notes chests open by taking damage rather than by interacting). `Entity_Combat.PreformAttack()` (see `02-Entity.md`) doesn't care which of these it hit — it just checks whether the collider it found has an `IDamagable` component and calls `TakeDamage` on it. The `bool` return value tells the caller whether the hit actually landed — a `false` return, e.g. from a dodge, means no on-hit effects should fire.

### `ICounterable.cs`
```csharp
public interface ICounterable
{
    public bool CanBeCountered { get; }
    public void HandleCounter();
}
```
The contract for "this thing can be countered" — tied to the Player's counter-attack state (`Player_CounterAttackState`, documented in `03-Player.md`). `CanBeCountered` is a read-only property, not a method, because it's meant to be checked cheaply every frame ("is a counter currently possible right now?") without side effects; `HandleCounter()` is the actual action taken when a counter successfully lands.

### `IInteractable.cs`
```csharp
public interface IInteractable
{
    public void Interact();
}
```
The contract for "the player can press the interact button on this." Implemented by NPCs, checkpoints, chests-you-talk-to, etc. under `InteractiveObjects/`. Whatever detects "player is near an interactable and pressed the button" just needs an `IInteractable` reference — no big `if (isNPC) ... else if (isCheckpoint) ...` chain required.

### `ISaveable.cs`
```csharp
public interface ISaveable
{
    public void LoadData(GameData data);
    public void SaveData(ref GameData data);
}
```
The contract for "this component has state that should persist between play sessions." This is the interesting one architecturally: per CLAUDE.md, `SaveManager` doesn't keep a manual list of "everything I need to save." Instead it uses reflection at save/load time — `FindObjectsByType<MonoBehaviour>(...).OfType<ISaveable>()` — to find *every* component in the scene that implements `ISaveable`, and calls `SaveData`/`LoadData` on all of them.

That means adding a new saveable feature later is as simple as implementing this interface on the relevant component — you never have to remember to "register" it anywhere else. `SaveData` takes `GameData` **by reference** (`ref GameData data`) specifically so each `ISaveable` can write directly into the same shared save-data object, rather than each returning its own separate blob that something else would have to merge.

---

## Part 3 — The State Machine engine (`Scripts/StateMachine/`)

This is the generic Finite State Machine engine (concept explained in [Concepts-Glossary: FSM](Concepts-Glossary.md#the-finite-state-machine-fsm-pattern)) that both `Player` and `Enemy` are built on. It's deliberately generic — nothing in this folder mentions "jumping" or "attacking"; that's the job of the concrete state classes built on top of it (covered in `03-Player.md` / `04-Enemy.md`).

### `StateMachine.cs` — the engine itself

```csharp
public class StateMachine
{
    public EntityState currentState {  get; private set; }
    public bool canChangeState;

    public void Initialize(EntityState startState)
    {
        canChangeState = true;
        currentState = startState;
        currentState.Enter();
    }

    public void ChangeState(EntityState newState)
    {
        if (!canChangeState)
            return;
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void UpdateActiveState()
    {
        currentState.Update();
    }

    public void SwitchOffStateMachine() => canChangeState = false;
}
```

This is **not** a `MonoBehaviour` — it's a plain C# class. `Player` and `Enemy` each own one as a regular field (`stateMachine = new StateMachine();` in their own `Awake()`), not as a separate component on the GameObject. The state machine has no need to live in the Unity component system itself; it's just a helper object that Player/Enemy delegate to.

- **`Initialize(startState)`** — called once, right after all the individual state objects have been constructed, to pick the first state (usually Idle) and run its `Enter()`.
- **`ChangeState(newState)`** — the only way states ever change. Notice the order: `Exit()` the old state *before* swapping the reference, *then* `Enter()` the new one — so a state's cleanup code always runs before the next state's setup code, never both states "active" at once.
- **`UpdateActiveState()`** — called every frame from `Entity.Update()`; just forwards to whichever state is currently active. This one line is what makes the whole machine "run."
- **`canChangeState` / `SwitchOffStateMachine()`** — a kill switch. When `false`, `ChangeState` becomes a no-op — any attempt to switch states is silently ignored. This exists for situations like death: once an entity is dead, you don't want some other piece of code accidentally putting it back into a Move or Attack state, so the dead state (or whatever triggers death) calls `SwitchOffStateMachine()` to lock the machine in place. There's no way to turn it back on — this is meant to be a one-way "this entity is done" switch, not a pause.

### `EntityState.cs` — the shared base every state inherits from

```csharp
public abstract class EntityState
{
    protected StateMachine stateMachine;
    protected string animBoolName;
    protected Animator animator;
    protected Rigidbody2D rb;
    protected Entity_Stats entityStats;

    protected float stateTimer;
    protected bool triggerCalled;

    public EntityState(StateMachine stateMachine, string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        animator.SetBool(animBoolName, true);
        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();                   
    }

    public virtual void Exit()
    {
        animator.SetBool(animBoolName, false);
    }

    public void AnimationTrigger()
    {
        triggerCalled = true;
    }

    public virtual void UpdateAnimationParameters() { }

    public void SyncAttackSpeed()
    {
        float attackSpeed = entityStats.offenseGroup.attackSpeed.GetValue();
        animator.SetFloat("attackSpeedMultiplier", attackSpeed);
    }
}
```

This is `abstract` (see [Concepts-Glossary: Abstract classes](Concepts-Glossary.md#abstract-classes)) — you never create a plain `EntityState`, only a concrete subclass like `Player_IdleState`.

**Why these specific fields live here, not lower down:** `stateMachine`, `animator`, `rb` (Rigidbody2D), and `entityStats` are things *every* state — Player or Enemy — needs to reference constantly, to change state, drive animation, move, and read stats. Putting them here once means every subclass gets them for free via inheritance instead of redeclaring them.

- **Constructor** — only sets `stateMachine` and `animBoolName` immediately, because those are the only two values available *at construction time* (when `Player`/`Enemy` builds all its state objects in `Awake()`). `animator` and `rb` get filled in by the subclass constructors (`PlayerState`/`EnemyState`) once `player.animator`/`enemy.animator` etc. actually exist.
- **`animBoolName`** — every state is tied to one Animator bool parameter (e.g. `"idle"`, `"jump"`). `Enter()` sets that bool `true`, `Exit()` sets it back to `false`. This is how the Animator Controller in Unity (a separate visual tool, not code) knows which animation to blend into — the state machine and the Animator Controller are two *parallel* state machines, one in code driving gameplay, one in the Animator window driving visuals, kept in sync purely through these bool names.
- **`stateTimer` / `Update()`** — `stateTimer` isn't set here (subclasses set it, e.g. "how long this attack animation lasts"), but the countdown (`stateTimer -= Time.deltaTime`) is shared boilerplate every state needs, so it lives in the shared `Update()`. A subclass's own `override Update()` almost always starts with `base.Update()` to get this countdown for free, then adds its own logic (e.g. "if stateTimer <= 0, go back to Idle").
- **`triggerCalled` / `AnimationTrigger()`** — this pair bridges **Unity's Animation Event system** into this code-side state machine. In the Editor, you can place a marker on a specific frame of an animation clip that calls a named method when playback reaches it — that's how "the sword should actually deal damage on frame 8 of the swing, not the instant you press the button" is implemented. `Entity_AnimationTriggers.CurrentStateTrigger()` (see `02-Entity.md`) is the method the animation event actually calls; it forwards into `Entity.CurrentStateAnimationTrigger()` → `stateMachine.currentState.AnimationTrigger()`, which just flips `triggerCalled` to `true`. The *current* state's own `Update()` then checks `if (triggerCalled) { ... }` to know "the animation has reached the point where the actual game effect (damage, dash movement, etc.) should happen now." This decouples "when does the animation *look* like it hits" from "when does the code make it *actually* hit," while keeping them driven by the same source of truth: the animation clip itself.
- **`SyncAttackSpeed()`** — reads the entity's current attack-speed stat and pushes it into the Animator as a float parameter (`attackSpeedMultiplier`), so the Animator Controller can play attack animations faster or slower based on gameplay stats rather than a fixed clip speed. Called by attack states specifically, not part of the base `Update()`, since only attack animations care about this.

### `PlayerState.cs` — the Player-specific layer

```csharp
public abstract class PlayerState : EntityState
{
    protected Player player;
    protected PlayerInputSet input;
    protected Player_SkillManager skillManager;

    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;
        animator = player.animator;
        rb = player.rb;
        input = player.input;
        entityStats = player.stats;
        skillManager = player.skillManager;
    }

    public override void Update()
    {
        base.Update();
        if (input.Player.Dash.WasPressedThisFrame() && CanDash())
        {
            skillManager.dash.SetSkillOnCooldown();
            stateMachine.ChangeState(player.dashState);
        }

        if (input.Player.Ultimate.WasPressedThisFrame() && skillManager.domainExpansion.CanUseSkill())
        {
            if (skillManager.domainExpansion.InstantDomain())
                skillManager.domainExpansion.CreateDomain();
            else
                stateMachine.ChangeState(player.domainExpansionState);

            skillManager.domainExpansion.SetSkillOnCooldown();
        }
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private bool CanDash()
    {
        if (skillManager.dash.CanUseSkill() == false) return false;
        if (player.wallDetected) return false;
        if (stateMachine.currentState == player.dashState || stateMachine.currentState == player.domainExpansionState) return false;
        return true;
    }
}
```

Still `abstract` — this is the shared base for all 15 `Player_*State` classes, but still not a usable state on its own.

- **Constructor** — this is where `animator`/`rb`/`entityStats` (declared but left empty in `EntityState`) finally get filled in, pulled straight off the `Player` instance passed in. This only works because by the time any `PlayerState` is constructed (in `Player.Awake()`), `player.animator` etc. have already been set up by `Entity.Awake()` running first — inheritance means `Player.Awake()` calls `base.Awake()`, covered in `03-Player.md`.
- **`Update()` handling Dash and Ultimate globally** — deliberate design: dashing and using the ultimate (Domain Expansion) are checked in the *shared* `PlayerState.Update()`, not in each individual state. That means **every** player state — Idle, Move, Jump, mid-attack, etc. — automatically gets "can I dash right now?" checking for free, without each of the 15 states repeating the logic. `input.Player.Dash.WasPressedThisFrame()` comes from the new Input System's generated wrapper — `WasPressedThisFrame()` specifically means "was this button pressed exactly this frame" (as opposed to "is it currently held"), so the dash only triggers once per press, not repeatedly while held.
- **`CanDash()`** — three separate guard conditions, each `return false`-ing early: skill not off cooldown, currently against a wall (can't dash into a wall), or already dashing/using the ultimate (can't dash out of a dash). Reads top-to-bottom as a checklist of reasons dashing *shouldn't* be allowed right now.
- **The Ultimate branch** — slightly more involved: `InstantDomain()` is checked first, and depending on its result, the domain either activates immediately (`CreateDomain()`, no state change) or transitions into a dedicated `domainExpansionState` (presumably because that upgrade path has an animation/duration that needs its own state). This previews the skill tree's "different upgrade paths change *how* a skill behaves" design, covered fully in the SkillSystem doc.
- **`UpdateAnimationParameters()`** — adds `yVelocity` on top of whatever the base class does (nothing, by default), feeding the Animator a float it presumably uses to blend between jump-rising and falling animations.

### `EnemyState.cs` — the Enemy-specific layer

```csharp
public abstract class EnemyState : EntityState
{
    protected Enemy enemy;
    protected EnemyState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.enemy = enemy;
        animator = enemy.animator;
        rb = enemy.rb;
        entityStats = enemy.entityStats;
    }

    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
        float battleAnimSpeedMultiplier = enemy.battleMoveSpeed / enemy.moveSpeed;
        animator.SetFloat("battleAnimSpeedMultiplier", battleAnimSpeedMultiplier);
        animator.SetFloat("moveAnimSpeedMultiplier", enemy.moveAnimSpeedMultiplier);
        animator.SetFloat("xVelocity", rb.linearVelocity.x);
    }
}
```

The Enemy mirror of `PlayerState`, and noticeably smaller: no Dash/Ultimate handling (enemies don't use player skills), no `input` field at all (enemies are driven by AI/FSM logic in their own states, not by reading a controller). The one thing it adds over the base class is animation-speed scaling: `battleAnimSpeedMultiplier` compares the enemy's "in combat" move speed against its normal move speed, so if an enemy moves faster while alert/chasing, its walk animation visually speeds up to match rather than looking like it's sliding across the ground.

**Why `PlayerState` and `EnemyState` both exist as a separate layer instead of everything living directly in `EntityState`:** anything genuinely shared by *all* states (state timer, animation bool toggling, the trigger bridge) lives in `EntityState`. Anything shared by *only* Player states or *only* Enemy states lives in their respective middle layer. This three-layer structure — `EntityState` → `PlayerState`/`EnemyState` → concrete state like `Player_IdleState` — is what lets the Dash/Ultimate shortcut apply to all 15 player states without duplicating it, while never leaking into Enemy states that have no use for it.

---

## How this connects elsewhere

- `Player.cs` and `Enemy.cs` each construct their **full set** of concrete state objects in `Awake()` (e.g. `idleState = new Player_IdleState(this, stateMachine, "idle");`), then call `stateMachine.Initialize(idleState)` in `Start()`.
- `Entity.Update()` calls `stateMachine.UpdateActiveState()` every frame — this one line is what actually drives the whole system once it's set up.
- Concrete state classes (documented in `03-Player.md` and `04-Enemy.md`) override `Enter`/`Update`/`Exit`/`UpdateAnimationParameters` to add the actual gameplay behavior (movement, attack timing, transitions) on top of everything described here.
- `SaveManager` (later doc) relies on `ISaveable` from this doc; `Entity_Combat`/`Entity_Health` (next doc, `02-Entity.md`) rely on `IDamagable`.

## Where this pattern could be reused later

The three-layer state machine here — generic engine → per-character-type shared layer → concrete states — is a solid, reusable template for *any* character-driven game, not just this one. The key transferable idea isn't the code itself, it's the **split of responsibility**: put truly universal logic (timers, animation bool toggling) as low as possible, put "all my players share this" or "all my enemies share this" logic in a middle layer, and keep each concrete state focused only on what makes it unique. The `ISaveable`-via-reflection pattern (no manual registry) is also worth remembering as a general technique any time you want "opt-in" behavior across many unrelated classes without a central list to maintain.

---

**Next:** `02-Entity.md` — the shared base class (`Entity`) and its component family (`Entity_Health`, `Entity_Combat`, `Entity_Stats`, `Entity_StatusHandler`, `Entity_VFX`, `Entity_SFX`, `Entity_DropManager`, `Entity_AnimationTriggers`) that `Player` and `Enemy` both build on.
