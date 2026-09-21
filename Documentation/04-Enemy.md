# 04 — Enemy: FSM Battle AI and the Skeleton

## Overview

`Enemy` extends `Entity` the same way `Player` does, but it's a noticeably smaller, simpler cousin: no player input, no skills, no inventory — just enough component wiring to detect the player, decide whether to fight, and attack. Per CLAUDE.md, enemy "AI" in this project is entirely **FSM + raycasts**, no pathfinding — everything in this doc is the concrete mechanism behind that description. `Enemy_Skeleton` is currently the *only* concrete enemy type built on this base (more are planned), and this doc treats it as the worked example of the pattern `Enemy` sets up.

If you haven't read `01-Foundations.md` (the state machine) or `02-Entity.md` (the shared `Entity`/`Entity_Health`/`Entity_Combat` base) yet, this doc leans on both constantly.

---

## `Enemy.cs` — the base class

### Fields worth knowing before anything else

```csharp
public class Enemy : Entity
{
    public string questTargetID;

    public Entity_Stats entityStats { get; private set; }
    public Enemy_Health health { get; private set; }
    public Enemy_IdleState idleState;
    public Enemy_MoveState moveState;
    public Enemy_AttackState attackState;
    public Enemy_BattleState battleState;
    public Enemy_DeadState deadState;
    public Enemy_StunnedState stunnedState;
    ...
```

Two things worth noticing immediately:

1. **`questTargetID`** is a plain string identifying this enemy for quest purposes — this is the exact field `Player_QuestManager.AddProgress(questTargetId)` (see `03-Player.md`) gets handed when this enemy dies. It's how a "kill 5 skeletons" quest works without the quest system needing any direct reference to enemy prefabs.
2. **The six state fields are plain `public` fields (`public Enemy_IdleState idleState;`), not `{ get; private set; }` properties** the way every one of `Player`'s state fields is (see `03-Player.md`). Functionally minor — but it does mean, unlike `Player`, external code could technically reassign `someEnemy.idleState` from outside the class; nothing in this codebase does, but it's a real, if small, inconsistency in how strictly each class protects its own state.

Also notice `entityStats` here is a plain `Entity_Stats`, not a subclass — there's no `Enemy_Stats` the way there's a `Player_Stats` with its buff system; enemies don't need buffs, so they just use the base stat component directly.

### Speed, detection, and the "no states built here" gap

```csharp
[Header("Player Detection")]
[SerializeField] LayerMask whatIsPlayer;
[SerializeField] Transform playerCheck;
[SerializeField] float playerCheckDistance = 10f;
public Transform player { get; private set; }
public float activeSlowMultipler { get; private set; } = 1f;

public float GetMoveSpeed() => moveSpeed * activeSlowMultipler;
public float GetBattleMoveSpeed() => battleMoveSpeed * activeSlowMultipler;

protected override void Awake()
{
    base.Awake();
    entityStats = GetComponent<Entity_Stats>();
    health = GetComponent<Enemy_Health>();
}
```

Compare this `Awake()` to `Player.Awake()` from `03-Player.md`: **`Enemy.Awake()` never constructs any state objects.** `Player` has exactly one concrete form, so `Player.Awake()` builds all 13 of its states directly. `Enemy` is meant to have *multiple* concrete enemy types (only `Enemy_Skeleton` exists so far, per CLAUDE.md's "more enemy types planned"), and different enemy types might not want the exact same set of states or the exact same wiring between them — so state construction is deliberately deferred to each concrete subclass's own `Awake()` (see `Enemy_Skeleton.cs` below). `Enemy` sets up everything *generic* (detection, stats, health) and leaves "which states exist and how they're built" to whoever inherits it.

`GetMoveSpeed()`/`GetBattleMoveSpeed()` are the **only** way any state reads this enemy's actual movement speed — never `moveSpeed`/`battleMoveSpeed` directly. This is Enemy's version of the "slow status effect" mechanism, worth contrasting directly with `Player.SlowDownEntityCo`'s approach from `03-Player.md`: instead of temporarily overwriting-then-restoring several raw fields, Enemy keeps one persistent `activeSlowMultipler` (default `1f`, meaning "no slow") that every speed read is funneled through, and applying a slow just means temporarily changing that one multiplier.

### `SlowDownEntityCo` / `StopSlowDown` overrides

```csharp
protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
{
    activeSlowMultipler = 1 - slowMultiplier;
    animator.speed *= activeSlowMultipler;
    yield return new WaitForSeconds(duration);
    StopSlowDown();
}

public override void StopSlowDown()
{
    activeSlowMultipler = 1f;
    animator.speed = 1f;
    base.StopSlowDown();
}
```
Simpler than `Player`'s version specifically *because* of the multiplier approach: there's nothing to snapshot-and-restore, `StopSlowDown()` just hardcodes both values back to their neutral `1`. `base.StopSlowDown()` (from `Entity.cs`, `02-Entity.md`) clears the coroutine-tracking field so a future slow effect is free to start cleanly.

### Battle triggers: `EnterBattleState`, `HandlePlayerDeath`

```csharp
public void EnterBattleState(Transform player)
{
    this.player = player;
    HandleFlip(player.position.x > transform.position.x ? 1 : -1);

    if (stateMachine.currentState == battleState || stateMachine.currentState == attackState)
        return;

    stateMachine.ChangeState(battleState);
}
```
Called from two different places: `Enemy_GroundedState` (passive — "I spotted the player nearby") and `Enemy_Health.TakeDamage` (reactive — "I just got hit," covered below). `HandleFlip(...)` here is a small reuse trick worth noticing: `Entity.HandleFlip(float xVelocity)` (from `02-Entity.md`) only ever looks at the *sign* of the number passed in, so feeding it a literal `1` or `-1` computed from a position comparison works perfectly well even though it's not actually a velocity — it's just "which way should I be facing." The early-return guard (`if already in battleState or attackState, do nothing`) matters because this method can be called repeatedly (every hit taken, every frame the player's detected) — without it, an enemy mid-swing in `attackState` could get yanked back into a freshly-`Enter`ed `battleState` by a stray detection call, interrupting its own attack animation.

```csharp
private void HandlePlayerDeath()
{
    stateMachine.ChangeState(idleState);
}

private void OnEnable() => Player.OnPlayerDeath += HandlePlayerDeath;
private void OnDisable() => Player.OnPlayerDeath -= HandlePlayerDeath;
```
This is the payoff of `Player.OnPlayerDeath` being a **static** event (`03-Player.md`): every single enemy in the scene subscribes to it independently, and the moment the player dies, *every* enemy — regardless of what it was doing, mid-chase or mid-attack — snaps back to `idleState`. No enemy needs a direct reference to the player to know it died; the static event reaches all of them at once.

### `PlayerDetection` — the actual "can I see the player" check

```csharp
public RaycastHit2D PlayerDetection()
{
    RaycastHit2D hit = Physics2D.Raycast(playerCheck.position, Vector2.right * facingDir, playerCheckDistance, whatIsPlayer | whatIsGround);

    if (hit.collider == null || hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
        return default;

    return hit;
}
```
This is worth reading carefully, because the trick is easy to miss: `whatIsPlayer | whatIsGround` combines **two separate LayerMasks into one** with a bitwise OR, so the single raycast can hit *either* the player *or* the ground. That's deliberate — it's a cheap line-of-sight check: if a wall or platform (on the ground layer) is physically between the enemy and the player, the ray hits *that* first, and the method correctly reports "not detected" (the `hit.collider.gameObject.layer != LayerMask.NameToLayer("Player")` check fails, falling through to `return default;` — see [Concepts-Glossary: structs & `default`](Concepts-Glossary.md#structs-default-and-why-a-no-hit-raycast-isnt-null)) even though the player might technically be within `playerCheckDistance`. A player standing in open line of sight is what actually lets the ray travel far enough to hit the Player layer first and return a real hit. Without merging the two layers into one raycast, you'd need two separate raycasts and extra logic to compare which one hit first — this gets the same result in one call.

`GetPlayerReference()` is a short helper worth flagging for one subtlety: it looks like a simple getter (`public Transform GetPlayerReference()`), but it also has a **side effect** — it writes the result into the `player` field before returning it (`player = PlayerDetection().transform; return player;`). A method named `Get...` quietly mutating state is a bit of a gotcha if you're skimming the class expecting `Get`-prefixed methods to be read-only.

### Gizmos — visualizing the three range bands

```csharp
protected override void OnDrawGizmos()
{
    base.OnDrawGizmos();
    Gizmos.color = Color.yellow; // playerCheckDistance — detection range
    Gizmos.color = Color.blue;   // attackDistance — attack range
    Gizmos.color = Color.green;  // minRetreatDistance — "too close, back off" range
}
```
Three concentric distance thresholds, drawn as three colored lines from `playerCheck` in the Scene view — handy for tuning an enemy's aggro range, attack range, and personal-space range visually rather than guessing at raw numbers in the Inspector.

---

## `Enemy_AnimationTriggers.cs` and `Enemy_VFX.cs`

```csharp
public class Enemy_AnimationTriggers : Entity_AnimationTriggers
{
    private void EnableCounterWindow()
    {
        enemy_VFX.EnableAttackAlert(true);
        enemy.EnableCounterWindow(true);
    }
    private void DisableCounterWindow()
    {
        enemy_VFX.EnableAttackAlert(false);
        enemy.EnableCounterWindow(false);
    }
}
```
Same Animation-Event-bridge idea as `Entity_AnimationTriggers` from `02-Entity.md` (no C# callers, invoked by name from specific frames of an attack windup animation) — but here it drives **two things in lockstep**: the visible "about to attack" warning icon (`Enemy_VFX.EnableAttackAlert`) and whether the player can actually land a counter right now (`Enemy.EnableCounterWindow`, which just flips the `canBeStunned` field). Placing `EnableCounterWindow`/`DisableCounterWindow` animation events at the start and end of the same windup frames is what keeps "the warning icon is showing" and "you actually can counter" visually honest — the player is never shown a counter prompt during a window where countering wouldn't actually work.

`Enemy_VFX.EnableAttackAlert(bool)` itself is just a null-guarded `SetActive` toggle on an `attackAlert` GameObject — nothing more.

---

## `Enemy_Health.cs`

```csharp
protected override void Start()
{
    base.Start();
    enemy = GetComponent<Enemy>();
    questManager = Player.Instance.questManager;
}

public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
{
    if (!canTakeDamage) return false;
    bool wasHit = base.TakeDamage(damage, elementalDamage, element, damageDealer);
    if (!wasHit) return false;

    if (damageDealer.GetComponent<Player>() != null)
        enemy.EnterBattleState(damageDealer);

    return true;
}

protected override void Die()
{
    base.Die();
    questManager.AddProgress(enemy.questTargetID);
}
```
`questManager = Player.Instance.questManager` reaches directly through the `Player.Instance` singleton (`03-Player.md`) — a concrete example of why that singleton exists: any enemy can grab "the" quest manager without a scene reference being wired up manually per-enemy.

The `if (!canTakeDamage) return false;` at the very top of `TakeDamage` is worth a careful look: `Entity_Health.TakeDamage` (the `base.` version, `02-Entity.md`) *already* checks `if (isDead || !canTakeDamage) return false;` as its own first line. So this override checks the same condition **twice** — once here, redundantly, before even calling `base.TakeDamage`. This doesn't change behavior (both checks agree), it's just belt-and-suspenders duplication rather than a bug — worth recognizing as redundant rather than assuming there's a subtle reason for it.

The part that *does* matter: **taking a hit from the Player automatically provokes battle** (`enemy.EnterBattleState(damageDealer)`), independent of `PlayerDetection()`'s passive raycast. This means a player can, say, throw a sword at a skeleton that hasn't spotted them yet, and the skeleton reacts immediately — "aggro on hit," a common and expected RPG behavior, layered on top of (not replacing) the passive line-of-sight detection covered above.

`Die()` is the exact call site `03-Player.md`'s `Player_QuestManager.AddProgress` section refers to — this is where a kill actually reports progress to the quest system, using the `questTargetID` string described above.

---

## `Enemy_Skeleton.cs` — the one concrete enemy

```csharp
public class Enemy_Skeleton : Enemy, ICounterable
{
    public bool CanBeCountered { get => canBeStunned; }

    protected override void Awake()
    {
        base.Awake();
        idleState = new Enemy_IdleState(this, stateMachine, "idle");
        moveState = new Enemy_MoveState(this, stateMachine, "move");
        attackState = new Enemy_AttackState(this, stateMachine, "attack");
        battleState = new Enemy_BattleState(this, stateMachine, "battle");
        deadState = new Enemy_DeadState(this, stateMachine, "idle");
        stunnedState = new Enemy_StunnedState(this, stateMachine, "stunned");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    [ContextMenu("Stun Enemy")]
    public void HandleCounter()
    {
        // unnecessary due to having check similar to it in player combat
        //if (!CanBeCountered)
        //    return;
        stateMachine.ChangeState(stunnedState);
    }
}
```
This is the concrete payoff of the "state construction deferred to the subclass" design explained above — `Enemy_Skeleton.Awake()` is where all six states finally get built and wired to `this` (the skeleton instance) and its `stateMachine`. `Start()` then initializes the machine into `idleState`, mirroring `Player.Start()`'s `stateMachine.Initialize(idleState)` from `03-Player.md` — but here it's the *concrete* subtype doing it, since `Enemy` itself has no `idleState` instance to initialize with until a subclass provides one.

`Enemy_Skeleton : Enemy, ICounterable` — implementing `ICounterable` (from `01-Foundations.md`) is what makes this the exact kind of object `Player_Combat.CounterAttackPreformed()` (`03-Player.md`) can find and counter. `CanBeCountered` is a one-line expression-bodied property (`{ get => canBeStunned; }`) exposing `Enemy`'s protected `canBeStunned` field — which is exactly the flag `Enemy_AnimationTriggers.EnableCounterWindow`/`DisableCounterWindow` toggle during the attack windup, so "can this be countered" is only ever `true` during that specific animation window.

The commented-out redundant check in `HandleCounter()` is worth reading as-is — it's a rare case of the codebase **explicitly documenting why a check was removed**: `Player_Combat.CounterAttackPreformed()` already checks `counterable.CanBeCountered` before ever calling `HandleCounter()` (see `03-Player.md`), so re-checking it here really would be pure redundancy, and whoever wrote this left a note explaining that instead of silently leaving dead code in. `[ContextMenu("Stun Enemy")]` (see [Concepts-Glossary](Concepts-Glossary.md#contextmenu)) lets you manually trigger a stun from the Inspector for testing, without needing to actually land a real counter-attack.

---

## The Enemy states

### `Enemy_GroundedState` — the shared base for Idle and Move

```csharp
public class Enemy_GroundedState : EnemyState
{
    public override void Update()
    {
        base.Update();
        if (enemy.PlayerDetection())
            stateMachine.ChangeState(enemy.battleState);
    }
}
```
The passive detection check: every frame, while idling or moving, check line of sight to the player and immediately switch to `battleState` if found. `Enemy_IdleState` and `Enemy_MoveState` both inherit this for free.

### `Enemy_IdleState` and `Enemy_MoveState` — the patrol loop

```csharp
public override void Enter() { base.Enter(); stateTimer = enemy.idleTime; }
public override void Update() { base.Update(); if (stateTimer < 0) stateMachine.ChangeState(enemy.moveState); }
```
`Enemy_IdleState` just waits out `idleTime`, then moves.

```csharp
public override void Enter()
{
    base.Enter();
    if (!enemy.groundDetected || enemy.wallDetected) enemy.Flip();
}

public override void Update()
{
    base.Update();
    enemy.SetVelocity(enemy.GetMoveSpeed() * enemy.facingDir, rb.linearVelocity.y);
    if (!enemy.groundDetected || enemy.wallDetected)
        stateMachine.ChangeState(enemy.idleState);
}
```
`Enemy_MoveState.Enter()` does a proactive check — "am I about to walk off a ledge, or into a wall?" — and flips around *before* moving at all, once, on entry. `Update()` then keeps walking at `GetMoveSpeed()` (the slow-aware getter from `Enemy.cs`) and, if it hits that same ledge/wall condition *while already moving*, switches to `Idle` instead of flipping again mid-stride — so the visible behavior is: walk one direction until blocked, stop and idle for `idleTime`, then (thanks to `Enter()`'s check running again next time `Move` starts) try the other direction. This is the entire "patrol" behavior for a skeleton with nothing else going on — no waypoints, no path data, just "flip when blocked, walk, repeat."

### `Enemy_BattleState` — the core of the AI

```csharp
public override void Enter()
{
    base.Enter();
    UpdateBattleTimer();
    player ??= enemy.GetPlayerReference();
    enemy.HandleFlip(DirectionToPlayer());
    if (ShouldRetreat())
        rb.linearVelocity = new Vector2((enemy.retreatVelocity.x * enemy.activeSlowMultipler) * -DirectionToPlayer(), enemy.retreatVelocity.y);
}
```
`player ??= enemy.GetPlayerReference();` (see [Concepts-Glossary: `??=`](Concepts-Glossary.md#-null-coalescing-assignment)) only fetches a fresh player reference if one isn't already held — the source even leaves a commented-out "spelled out" version of the same logic directly underneath, evidently as a note explaining what the compact operator does. If entering battle already too close (`ShouldRetreat()`), it immediately kicks off a retreat — note `rb.linearVelocity` is set **directly** here rather than through `enemy.SetVelocity(...)`, meaning this specific push bypasses the usual flip-on-move-direction logic in `SetVelocity`, and `activeSlowMultipler` is applied manually to keep the retreat speed slow-aware too.

```csharp
public override void Update()
{
    base.Update();
    if (enemy.PlayerDetection())
    {
        UpdateTargetIfNeeded();
        UpdateBattleTimer();
    }
    if (BattleTimeIsOver())
        stateMachine.ChangeState(enemy.idleState);
    if (WithinAttackRange() && enemy.PlayerDetection())
        stateMachine.ChangeState(enemy.attackState);
    else
        enemy.SetVelocity(enemy.GetBattleMoveSpeed() * DirectionToPlayer(), rb.linearVelocity.y);
}
```
As long as the player stays visible, `UpdateBattleTimer()` keeps resetting `lastTimeWasInBattle`, so `BattleTimeIsOver()` (`Time.time > lastTimeWasInBattle + battleTimeDuration`) only trips once the player has been *out of sight* for the full duration — this is the "give up the chase" timeout, not an "in combat for too long" timeout. If close enough and still visible, attack; otherwise, keep closing the distance at `GetBattleMoveSpeed()`.

```csharp
protected int DirectionToPlayer()
{
    if (player == null) return 0;
    float verticalDistance = Mathf.Abs(player.position.y - enemy.transform.position.y);
    float horizontalDistance = Mathf.Abs(player.position.x - enemy.transform.position.x);
    if (verticalDistance > 1.5f && horizontalDistance < 1f) return 0;
    return player.position.x > enemy.transform.position.x ? 1 : -1;
}
```
Worth calling out as a small but genuine piece of AI polish: if the player is significantly above or below (say, on a platform overhead, or the enemy standing over a pit) *and* nearly lined up horizontally, this returns `0` — "don't move" — instead of a direction. Without this check, an enemy in that situation would otherwise oscillate uselessly left-right trying to close a horizontal gap that's nearly zero, directly under or over a player it has no actual way to reach. `DistanceToPlayer()`/`WithinAttackRange()`/`ShouldRetreat()` are plain threshold helpers built on top, each returning `float.MaxValue` ("infinitely far") as a safe default if `player` happens to be `null`.

### `Enemy_AttackState`

```csharp
public override void Enter() { base.Enter(); SyncAttackSpeed(); }
public override void Update()
{
    base.Update();
    if (triggerCalled) stateMachine.ChangeState(enemy.battleState);
}
```
Deliberately thin — the actual damage-dealing happens the same way it does for the Player: an Animation Event on the attack clip fires `Entity_AnimationTriggers`' inherited `AttackTrigger()` method (from `02-Entity.md`), which calls `Entity_Combat.PreformAttack()` on whichever `Entity_Combat` component is attached to this enemy (every entity in this component family shares that same base combat component, per CLAUDE.md — `Enemy` just doesn't happen to expose it through a named property the way `Player.combat` does). Once the swing's trigger fires, control returns to `battleState` — **not** `idleState** — so the enemy immediately re-evaluates whether to attack again, retreat, or chase, rather than resetting its whole engagement.

### `Enemy_StunnedState`

```csharp
public override void Enter()
{
    base.Enter();
    enemy_VFX.EnableAttackAlert(false);
    enemy.EnableCounterWindow(false);
    stateTimer = enemy.stunnedDuration;
    rb.linearVelocity = new Vector2(enemy.stunnedVelocity.x * -enemy.facingDir, enemy.stunnedVelocity.y);
}
public override void Update() { base.Update(); if (stateTimer < 0) stateMachine.ChangeState(enemy.idleState); }
```
Explicitly turns off the attack-alert icon and the counter window on entry — a safety measure so an enemy that's *already* stunned can't somehow be countered a second time mid-stun. The stun "bounce" (`rb.linearVelocity` set directly, negated against `facingDir` — knocked backward relative to whichever way it's facing) again bypasses `Entity`'s normal knockback system (`RecieveKnockBack`/`isKnocked` from `02-Entity.md`) entirely — this is a separate, simpler "stun impulse" specific to this one state, not routed through the shared knockback coroutine.

### `Enemy_DeadState` — no `base.Enter()` at all

```csharp
public override void Enter()
{
    animator.enabled = false;
    rb.gravityScale = 12f;
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 15);
    enemy.GetComponent<Collider2D>().enabled = false;
    stateMachine.SwitchOffStateMachine();
}
```
Worth noticing what's *missing*: this `Enter()` never calls `base.Enter()` — meaning it skips `EntityState.Enter()`'s normal "set the animator bool true" behavior entirely (see `01-Foundations.md`). Instead it disables the Animator outright (`animator.enabled = false`), which freezes the sprite on whatever frame it died mid-animation rather than transitioning into any dedicated "dead" animation. Combined with a high gravity scale and an upward velocity pop, this produces the classic "corpse flies up and then just drops/settles, frozen" 2D death effect. The `Collider2D` is disabled so the corpse stops blocking movement or being hittable, and — unlike `Player_DeadState` (`03-Player.md`), which relies on disabled input and disabled physics simulation to go inert — this state explicitly calls `stateMachine.SwitchOffStateMachine()`, the actual kill switch described in `01-Foundations.md`, permanently locking this enemy's state machine so nothing can ever change its state again.

---

## How this connects elsewhere

- Every state here extends `EnemyState`/`EntityState` from `01-Foundations.md`.
- `Enemy_Health`, `Enemy_AnimationTriggers`, `Enemy_VFX` all extend their `Entity_*` counterparts from `02-Entity.md` — same override-on-top-of-shared-base shape as `Player`'s component family.
- `Enemy_Skeleton` implements `ICounterable` (`01-Foundations.md`), which is exactly what `Player_Combat.CounterAttackPreformed()` (`03-Player.md`) looks for.
- `Enemy.OnEnable`/`OnDisable` subscribe to the static `Player.OnPlayerDeath` event (`03-Player.md`).
- `Enemy_Health.Die()` calls into `Player_QuestManager.AddProgress` (`03-Player.md`), and `Enemy_Health.Start()` reaches `Player.Instance` (also `03-Player.md`) directly.

## Where this pattern could be reused later

The **"base class sets up shared plumbing, concrete subclass builds and wires the actual state instances"** split between `Enemy` and `Enemy_Skeleton` is the right template to reach for whenever you're designing a system meant to have multiple concrete variants sharing a core (multiple enemy types here, but the same idea applies to multiple weapon types, multiple vehicle types, anything with a "family of similar-but-not-identical things"). The **layer-combined raycast** trick in `PlayerDetection()` (`whatIsPlayer | whatIsGround` in one call) is a cheap, reusable way to get line-of-sight-aware detection without a second raycast or extra distance comparisons. And the **"don't move if the gap can't actually be closed"** check in `DirectionToPlayer()` is a small but genuinely useful piece of general AI-polish wisdom: cheap distance/threshold guards like this are often what separates AI that *looks* competent from AI that's technically correct but visibly dumb.

---

This completes the first four docs (`Concepts-Glossary`, `01-Foundations`, `02-Entity`, `03-Player`, `04-Enemy`) — see `README.md` for what's planned next (`GameManager`, `StatSystem`, `InventorySystem`, `SkillSystem`, `SaveSystem`, `InteractiveObjects`, `Data`, `UI`, `VFX`/`Parallax`).
