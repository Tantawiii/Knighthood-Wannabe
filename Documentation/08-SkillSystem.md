# 08 — SkillSystem: Skill_Base, SkillObject_Base, and the Five Skills

## Overview

The biggest system in the codebase by density (16 files, ~1,380 lines) — and the one that finally cashes in the `SkillUpgradeType` enum from `01-Foundations.md`. Every one of that enum's ~28 values (`Dash_CloneOnStart`, `Shard_MultiCast`, `SwordThrow_Pierce`, `TimeEcho_HealWisp`, `Domain_EchoSpam`, and so on) gets checked *somewhere* in this folder — this doc is where you see exactly what each upgrade node actually does.

The shape repeats five times: a **`Skill_X`** component (lives on a child GameObject under the player, per `Player_SkillManager` in `03-Player.md`) is the "manager" — it knows which upgrade is currently unlocked, holds cooldown/config data, and decides *what* should happen when the skill is used. A **`SkillObject_X`** is the actual spawned prefab (a projectile, an echo, a domain) — it's what physically exists in the world and does the moment-to-moment work (move, collide, damage, expire). Both halves build on their own shared base class, `Skill_Base` and `SkillObject_Base` respectively.

---

## `Skill_Base.cs` — every skill's shared manager logic

```csharp
public class Skill_Base : MonoBehaviour
{
    public Player_SkillManager skillManager { get; private set; }
    public Player player { get; private set; }
    public DamageScaleData damageScaleData { get; private set; }

    [SerializeField] protected SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType;
    [SerializeField] protected float cooldown;
    [SerializeField] private Skill_DataSO skillData;
    private float lastTimeUsed;

    protected virtual void Awake()
    {
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();
        lastTimeUsed -= cooldown;
        damageScaleData = new DamageScaleData();
    }
```

`GetComponentInParent` (rather than `GetComponent`) confirms what `03-Player.md` said about `Player_SkillManager`: each skill lives on its own child GameObject, reaching *up* to find the player and skill manager it belongs to. `lastTimeUsed -= cooldown` in `Awake()` is a small but deliberate trick: `OnCooldown()` (below) checks `Time.time < lastTimeUsed + cooldown`, so pre-emptively subtracting `cooldown` from a starting `lastTimeUsed` of `0` guarantees the very first cooldown check ever made passes — a skill isn't born already on cooldown.

### `upgradeType` is the single source of truth for "which version of this skill is active"

```csharp
protected bool Unlocked(SkillUpgradeType upgradeType) => this.upgradeType == upgradeType;
```
This one-line method is the load-bearing piece of the *entire* SkillSystem. Because `upgradeType` holds exactly one value at a time (CLAUDE.md's "mutually-exclusive upgrade paths"), every branch you'll see below in every concrete skill — `if (Unlocked(SkillUpgradeType.Shard_MultiCast)) { ... }` — is really asking "is *this specific node*, out of this skill's whole tree, the one currently equipped?" There's no separate object or flag per upgrade; one component just gets reconfigured to behave differently depending on this single field.

### `SetSkillUpgrade` — how a skill actually changes what it does

```csharp
public void SetSkillUpgrade(Skill_DataSO skillData)
{
    UpgradeData upgradeData = skillData.upgradeData;
    this.upgradeType = upgradeData.upgradeType;
    this.cooldown = upgradeData.cooldown;
    damageScaleData = upgradeData.damageScaleData;

    player.ui.inGameUI.GetSkillSlot(skillType)?.SetUpSkillSlot(skillData);
    ResetCooldown();
}
```
`Skill_DataSO` (briefly — full detail in the `Data` doc) is a ScriptableObject asset per skill-tree *node*, holding a nested `UpgradeData` (`upgradeType`, `cooldown`, `damageScaleData`). Calling `SetSkillUpgrade` — presumably from the Skill Tree UI whenever the player spends a point to unlock a node — **overwrites this live component's own `upgradeType`/`cooldown`/`damageScaleData`** with whatever that node specifies. This confirms exactly how "mutually exclusive upgrade paths" works mechanically: picking a new node in a skill's tree doesn't add a new object, it reconfigures the one `Skill_X` component in place. It also tells the in-game UI's skill slot to refresh its icon and resets the cooldown, so unlocking/upgrading a skill never accidentally leaves it on cooldown.

```csharp
protected virtual void Start()
{
    // Deferred to Start so Player.ui, UI.inGameUI and UI_InGame.skillSlots
    // are all initialized before SetSkillUpgrade touches the in-game UI.
    if (skillData != null && skillData.unlockedByDefault)
        SetSkillUpgrade(skillData);
}
```
Worth quoting the source's own comment here directly — it's an unusually explicit explanation of an `Awake`-vs-`Start` ordering decision (see [Concepts-Glossary](Concepts-Glossary.md#monobehaviour--the-unity-lifecycle)): the optional `skillData` field lets a skill start the game already unlocked at some base tier (e.g. tier-1 Dash), but doing that setup has to wait until `Start()` specifically because it reaches into the UI hub chain, which needs its own `Awake()`s to have already run first.

### Cooldown bookkeeping

```csharp
protected bool OnCooldown() => Time.time < lastTimeUsed + cooldown;
public void SetSkillOnCooldown()
{
    player.ui.inGameUI.GetSkillSlot(skillType)?.StartCooldown(cooldown);
    lastTimeUsed = Time.time;
}
public void ReduceCooldownBy(float cooldownReduction) => lastTimeUsed += cooldownReduction;
public void ResetCooldown()
{
    player.ui.inGameUI.GetSkillSlot(skillType)?.ResetCooldown();
    lastTimeUsed = Time.time - cooldown;
}
```
`ReduceCooldownBy` is exactly the method `Player_SkillManager.ReduceAllSkillsCooldownBy` (`03-Player.md`) loops over every skill to call — pushing `lastTimeUsed` further into the past is a clean one-line way to shorten remaining wait time without touching `cooldown` itself. `ResetCooldown` does the opposite extreme (same trick as the `Awake()` initialization above): pretend the skill was used one full cooldown ago, so it's instantly available.

`CanUseSkill()` (the base gate every concrete skill's own override extends via `base.CanUseSkill()`) is just "is this skill unlocked at all, and is it off cooldown" — nothing more.

---

## `SkillObject_Base.cs` — every spawned skill object's shared damage logic

```csharp
public class SkillObject_Base : MonoBehaviour
{
    protected Animator anim;
    protected Rigidbody2D rb;
    protected Entity_Stats playerStats;
    protected Transform ownerTransform;
    protected DamageScaleData damageScaleData;
    protected ElementType usedElementType;
    protected bool targetGotHit;
    protected Transform lastTarget;
```
Notice `Awake()` here only fetches `anim`/`rb` — the rest (`playerStats`, `ownerTransform`, `damageScaleData`) get filled in later by whichever `Skill_X` spawns this object, via its own `SetUp*`/`Setup*` method, since those values only exist once the manager actually creates and configures the instance.

```csharp
protected void DamageEnemiesInRadius(Transform t, float radius)
{
    foreach (var target in EnemiesAround(t, radius))
    {
        IDamagable damageable = target.GetComponent<IDamagable>();
        if (damageable == null) continue;

        AttackData attackData = playerStats.GetAttackData(damageScaleData);
        Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

        targetGotHit = damageable.TakeDamage(attackData.physicalDamage, attackData.elementalDamage, attackData.elementType, ownerTransform);

        if (attackData.elementType != ElementType.None)
            statusHandler.ApplyStatusEffect(attackData.elementType, attackData.elementalEffectData);

        if (targetGotHit && onHitVFX != null)
        {
            lastTarget = target.transform;
            Instantiate(onHitVFX, target.transform.position, Quaternion.identity);
        }
        usedElementType = attackData.elementType;
    }
}
```
This is worth directly comparing to `Entity_Combat.PreformAttack()` from `02-Entity.md` — same shape (overlap-check, filter by `IDamagable`, build `AttackData`, apply damage, apply status effect), reused here as the **one shared damage-dealing method every skill object in the game calls**, instead of every skill re-implementing its own version. `usedElementType`/`lastTarget` are left set as side effects after the loop — several concrete skill objects read these afterward (e.g. to color an explosion VFX by whichever element was actually dealt). `FindClosestTarget()` (a simple nearest-enemy-within-10-units scan) is the other piece of shared machinery, reused by the Shard homing upgrade and by `TimeEcho`'s initial facing.

---

## `Skill_Dash` — no `SkillObject_Dash` at all

```csharp
public void OnStartEffect()
{
    if (Unlocked(SkillUpgradeType.Dash_CloneOnStart) || Unlocked(SkillUpgradeType.Dash_CloneOnStartAndArrival))
        CreateClone();
    if (Unlocked(SkillUpgradeType.Dash_ShardOnStart) || Unlocked(SkillUpgradeType.Dash_ShardOnStartAndArrival))
        CreateShard();
}

private void CreateShard() => skillManager.shard.CreateRawShard();
private void CreateClone() => skillManager.timeEcho.CreateTimeEcho();
```
The simplest skill, structurally: no `TryUseSkill()` override at all (recall from `01-Foundations.md` that dashing is triggered directly from `PlayerState.Update()`'s own `CanDash()` check, not through the normal skill-slot flow), and **no dedicated `SkillObject_Dash`** — its `OnStartEffect()`/`OnEndEffect()` hooks (called from `Player_DashState.Enter()`/`Exit()`, `03-Player.md`) just reach straight into `skillManager.shard`/`skillManager.timeEcho` and reuse *their* spawning methods. This is the clearest example in the system of skills composing each other rather than each needing a fully independent implementation for every upgrade permutation.

---

## `Skill_Shard` / `SkillObject_Shard` — five upgrade paths, one field

```csharp
public override void TryUseSkill()
{
    if (!CanUseSkill()) return;
    if (Unlocked(SkillUpgradeType.Shard)) HandleShardRegular();
    if (Unlocked(SkillUpgradeType.Shard_MoveToEnemy)) HandleShardMoving();
    if (Unlocked(SkillUpgradeType.Shard_MultiCast)) HandleShardMultiCast();
    if (Unlocked(SkillUpgradeType.Shard_Teleport)) HandleShardTeleport();
    if (Unlocked(SkillUpgradeType.Shard_TeleportHpRewind)) HandleShardTeleportHpRewind();
}
```
This `if`-per-upgrade shape (not `else if`) is the pattern every skill's `TryUseSkill()` follows — safe because `Unlocked()` can only ever be `true` for one of these at a time, since `upgradeType` only ever holds one value.

- **`Shard` (base)** — `HandleShardRegular()`: plant a stationary shard, put the skill on cooldown immediately.
- **`Shard_MoveToEnemy`** — `HandleShardMoving()`: same, but the shard homes toward the nearest enemy (`MoveTowardsClosestTarget`) while counting down to detonation.
- **`Shard_MultiCast`** — a genuinely different resource model: instead of the normal cooldown gate, this upgrade uses **charges** (`maxCharges`/`currentCharges`). Each cast spends one charge and calls `CreateRawShard` (the explicit-parameters overload, not the auto-configuring `CreateShard`) — a background `RechargeShardCo` coroutine (started once, guarded by `isRecharging` so only one ever runs) slowly regenerates one charge every `cooldown` seconds until back at `maxCharges`. The skill's own cooldown timer is essentially unused for this upgrade path — availability is governed by `currentCharges > 0` instead.
- **`Shard_Teleport` / `Shard_TeleportHpRewind`** — a two-press mechanic:
  ```csharp
  void HandleShardTeleport()
  {
      if (currentShard == null) CreateShard();
      else { SwapPlayerPosition(); SetSkillOnCooldown(); }
  }
  void SwapPlayerPosition()
  {
      Vector3 shardPosition = currentShard.transform.position;
      Vector3 playerPosition = player.transform.position;
      currentShard.transform.position = playerPosition;
      currentShard.Explode();
      player.TeleportPlayer(shardPosition);
  }
  ```
  First press plants a shard (with a much longer `shardExistDuration` detonation window than the default — it's meant to sit and wait, not detonate quickly). Second press swaps the shard to the player's *current* position, detonates it there, and teleports the player to where the shard *was*. Notice `SetSkillOnCooldown()` is only called on the **second** press — planting the shard alone doesn't cost the cooldown. The `HpRewind` variant additionally snapshots `playerHealth.GetHealthPercent()` at the moment of planting and restores it on the second press — a genuine "rewind," both position and health, back to how things were when the shard was planted.
- **`ForceCooldown` / `OnShardExploded`** — closes a real gap the two-press design would otherwise have:
  ```csharp
  if (Unlocked(SkillUpgradeType.Shard_Teleport) || Unlocked(SkillUpgradeType.Shard_TeleportHpRewind))
      currentShard.OnShardExploded += ForceCooldown;

  private void ForceCooldown()
  {
      if (!OnCooldown()) { SetSkillOnCooldown(); currentShard.OnShardExploded -= ForceCooldown; }
  }
  ```
  If a planted teleport-shard is left alone long enough to self-detonate naturally (its `shardExistDuration` timer runs out) *before* the player ever triggers the second press, this subscription steps in and puts the skill on cooldown anyway. Without it, a player could plant-and-forget teleport shards indefinitely for free, since the cooldown would otherwise only ever get set by the (never-reached) second-press path.

`SkillObject_Shard.Explode()` ties back to `Entity_VFX` (`02-Entity.md`): it spawns an explosion VFX and colors it via `shardManager.player.VFX.GetElementColor(usedElementType)` — the exact same element-color lookup used for status-effect tinting, reused here for a skill's visual feedback.

---

## `Skill_SwordThrow` / `SkillObject_Sword` family — real trajectory physics

Four prefabs (regular, pierce, spin, bounce), mapped 1:1 to an upgrade node via a simple if/else chain (`GetSwordPrefab()`), and one genuinely interesting bit of applied physics worth reading closely.

### The recall-on-reuse trick

```csharp
public override bool CanUseSkill()
{
    UpdateThrowPower();
    if (currentSword != null)
    {
        currentSword.GetSwordBackToPlayer();
        return false;
    }
    return base.CanUseSkill();
}
```
If a sword is already out, pressing the skill button again doesn't throw a second one — it calls the sword's own recall method and reports "can't use" (since the press was consumed as a recall instead of a throw). One button, two meanings, depending on state.

### Trajectory prediction — actual projectile-motion math

```csharp
private Vector2 CalculateTrajectoryPoint(Vector2 direction, float time)
{
    float scaleThrowPower = currentThrowPower * 10;
    Vector2 initialVelocity = direction * scaleThrowPower;
    Vector2 gravityEffect = 0.5f * Physics2D.gravity * swordGravity * (time * time);
    Vector2 predictedPosition = (initialVelocity * time) + gravityEffect;
    Vector2 playerPosition = transform.root.position;
    return playerPosition + predictedPosition;
}
```
This is the standard constant-acceleration position formula — `position = initial_velocity × time + ½ × acceleration × time²` — applied directly: `Physics2D.gravity * swordGravity` is the effective downward acceleration (the sword prefab's own configured gravity scale multiplying the scene's global gravity), and `PredictTrajectory(direction)` calls this once per dot (20 of them, `predictionDotSpacing` seconds apart) to place a trail of dots along the *actual* physics arc the sword would follow if thrown right now — not an approximation, the same math the Rigidbody2D's own physics simulation will produce once the sword is actually released.

`GenerateDots()` creates all 20 dot objects **once**, up front in `Awake()`, and `EnableDot(bool)` just toggles their visibility — a small, genuine example of manual object pooling (instantiate once, reuse by enabling/disabling) worth noting specifically because CLAUDE.md calls out that the project otherwise has **no** object pooling anywhere else.

### `SkillObject_Sword` (base) — face-your-velocity and the comeback safety net

```csharp
protected virtual void Update()
{
    transform.right = rb.linearVelocity; // Rotate the sword to face the direction of its velocity
    HandleComeback();
}
```
Assigning a `Vector2` (the Rigidbody's velocity) directly to `transform.right` is a compact, common 2D trick: `transform.right` is itself a direction, so setting it to the velocity vector reorients the object so its local +X axis always points the way it's currently moving — a cheap "face where I'm going" without any manual angle/`Quaternion` math.

```csharp
protected void HandleComeback()
{
    float distance = Vector2.Distance(transform.position, playerTransform.position);
    if (distance > maxAllowedDistance) GetSwordBackToPlayer();
    if (!shouldComeback) return;
    transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, comebackSpeed * Time.deltaTime);
    if (distance < 0.5f) Destroy(gameObject);
}
```
Auto-recalls itself if it ever drifts past `maxAllowedDistance` (25 units) even without the player asking — a safety net against a thrown sword flying off-screen forever. The base `OnTriggerEnter2D` deals radius damage and calls `StopSword`, which stops physics simulation **and literally reparents the sword's Transform onto whatever it hit** (`transform.SetParent(collision.transform)`) — so a sword stuck in, say, a moving platform rides along with it rather than staying at a fixed world position.

### The three subclasses — what's actually different about each

- **`SkillObject_SwordSpin`** overrides `Update()` **entirely** (no `base.Update()` — it no longer auto-rotates to face velocity, since it's spinning in place rather than flying) and instead ticks a repeating attack timer (`HandleAttack`, dealing radius damage at a fixed `attackPerSecond` rate — the same "countdown, act, reset" rhythm as `Entity_StatusHandler`'s burn ticks in `02-Entity.md`, just driven by a manually-reset field instead of a coroutine). `HandleStopping()` freezes physics once it drifts past `maxDistance` from the player — note it just stops simulating in place, it doesn't auto-recall the way the base class's `HandleComeback` does; an automatic return is instead scheduled once via `Invoke(nameof(GetSwordBackToPlayer), swordManager.maxSpinDuration)` in `SetupSword`.
- **`SkillObject_SwordPierce`** decrements `amountToPierce` on every enemy hit and only actually *stops* (reparenting/freezing, via the inherited `StopSword`) once either that count reaches zero or it physically collides with the `Ground` layer — flying through enemies freely until one of those two conditions is met.
- **`SkillObject_SwordBounce`** is the most involved: on first impact, it snapshots every enemy currently nearby (`enemyTargets = EnemiesAround(transform, 10)`, captured *once*, not re-queried per bounce) and then hops between them, tracking `selectedBefore` so it prefers targets it hasn't bounced to yet in this throw — once every currently-alive target has been visited at least once, it clears that visited list and starts a fresh cycle rather than getting stuck (`GetValidTargets()`/`GetAliveTargets()`, the latter also filtering out any target that's since become `null`, i.e. died mid-bounce-sequence).

---

## `Skill_TimeEcho` / `SkillObject_TimeEcho` — a clone that can become a healing wisp

### The manager side: config gated behind `upgradeType`

```csharp
public bool ShouldBeWisp() =>
    upgradeType == SkillUpgradeType.TimeEcho_HealWisp
    || upgradeType == SkillUpgradeType.TimeEcho_CleanseWisp
    || upgradeType == SkillUpgradeType.TimeEcho_CooldownWisp;

public int GetMaxAttacks()
{
    if (upgradeType == SkillUpgradeType.TimeEcho_SingleAttack || upgradeType == SkillUpgradeType.TimeEcho_DuplicateChance) return 1;
    if (upgradeType == SkillUpgradeType.TimeEcho_MultiAttack) return maxAttacks;
    return 0;
}
```
Same "manager answers upgrade-dependent questions, the spawned object asks and acts" split seen in `Skill_Shard`/`Skill_Base.GetDetonationTime` — every wisp-type upgrade returns `0` max attacks, meaning **a wisp-form echo never attacks at all**, it's purely a delayed, delivered effect.

```csharp
public void CreateTimeEcho(Vector3 targetPosition = default(Vector3))
{
    Vector3 positionToSpawn = targetPosition != default(Vector3) ? targetPosition : transform.position;
    ...
}
```
Worth flagging as a genuine (if minor) limitation of the default-parameter pattern used here: `default(Vector3)` is `(0,0,0)`, the same value as an intentionally-passed world origin — so a caller that genuinely wanted to spawn an echo exactly at `(0,0,0)` would have that request misread as "no position given, use the caster's own position instead." Unlikely to matter given real level geometry, but a real edge case in how this specific overload works.

### The spawned object: attack, then maybe become a wisp

```csharp
public void SetUpEcho(Skill_TimeEcho echoManager)
{
    ...
    maxAttacks = echoManager.GetMaxAttacks();
    Invoke(nameof(HandleDeath), echoManager.GetTimeEchoDuration());
    FlipToTarget();
    anim.SetBool("canAttack", maxAttacks > 0);
}
```
Same manager-hands-everything-to-the-object handoff pattern as `SkillObject_Shard.SetUpShard`/`SkillObject_Sword.SetupSword`. `FlipToTarget()` reuses the shared `FindClosestTarget()` to visually orient the echo toward an enemy on spawn.

```csharp
public void PerformAttack()
{
    DamageEnemiesInRadius(targetCheck, 1);
    if (!targetGotHit) return;

    bool canDuplicate = Random.value <= echoManager.GetDuplicateChance();
    float xOffset = transform.position.x < lastTarget.position.x ? 1 : -1;
    if (canDuplicate)
        echoManager.CreateTimeEcho(lastTarget.position + new Vector3(xOffset, 0, 0));
}
```
Called from `SkillObject_AnimationTriggers.AttackTrigger()` — yet another instance of the Animation-Event-bridge pattern from `02-Entity.md`, this time applied to a skill object rather than Player/Enemy. Only rolls the Duplicate Chance upgrade if the attack actually connected, and if it triggers, spawns a second echo positioned just to the side of whatever was hit (offset direction computed from which side the target was on) — the payoff of the `TimeEcho_DuplicateChance` upgrade node.

`SkillObject_AnimationTriggers.TryTerminate(int currentAttackIndex)` is a variant of the same bridge worth noting specifically because it's the first example in this documentation set of a Unity Animation Event **passing an argument** (Unity supports a single int/float/string/object parameter per event) — checking whether this was the echo's *final* configured swing and, if so, ending its life immediately rather than waiting for the separately-scheduled duration timer.

### Becoming a wisp

```csharp
public void HandleDeath()
{
    Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
    if (echoManager.ShouldBeWisp()) TurnIntoWisp();
    else Destroy(gameObject);
}

private void TurnIntoWisp()
{
    shouldMoveToPlayer = true;
    anim.gameObject.SetActive(false);
    wispTrail.gameObject.SetActive(true);
    rb.simulated = false;
}
```
`HandleDeath` is called both when the echo's duration timer runs out **and** — via `SkillObject_Health : Entity_Health` overriding just `Die()` to call `timeEcho.HandleDeath()` — when the echo takes enough damage in combat to actually die. That second path matters a lot for the healing math below:

```csharp
private void HandlePlayerTouch()
{
    float healthAmount = echoHealth.lastDamageTaken * echoManager.GetPercentOfDamageHealed();
    playerHealth.IncreaseHealth(healthAmount);
    skillManager.ReduceAllSkillsCooldownBy(echoManager.GetCooldownReduction());
    if (echoManager.CanRemoveNegativeEffects()) statusHandler.RemoveAllNegativeEffects();
    Destroy(gameObject);
}
```
`echoHealth.lastDamageTaken` reuses `Entity_Health`'s own damage-tracking field from `02-Entity.md` — the Heal Wisp upgrade heals the player a percentage of **the last hit the echo itself took**. Put together with the previous paragraph: the echo has to actually survive combat and take a real hit for its eventual wisp-healing to be worth anything — an echo that expires from its duration timer without ever being attacked heals nothing when it reaches the player, while one that tanked a big hit right before dying delivers a meaningful heal. A genuine risk/reward relationship baked into how these two systems (health tracking, wisp payoff) intersect.

---

## `Skill_DomainExpansion` / `SkillObject_DomainExpansion` — the ultimate

Directly continues `03-Player.md`'s `Player_DomainExpansionState` section — this is where `CreateDomain()`, `DoSpellCasting()`, `GetDomainDuration()`, and `InstantDomain()` (all called from that state) actually live.

```csharp
public void CreateDomain()
{
    spellPerSecond = GetSpellsToCast() / GetDomainDuration();
    GameObject domain = Instantiate(domainPrefab, transform.position, Quaternion.identity);
    domain.GetComponent<SkillObject_DomainExpansion>().SetUpDomain(this);
}

public void DoSpellCasting()
{
    spellCastTimer -= Time.deltaTime;
    if (currentTarget == null) currentTarget = FindTargetInDomain();
    if (currentTarget != null && spellCastTimer <= 0)
    {
        CastSpell(currentTarget);
        spellCastTimer = 1f / spellPerSecond;
        currentTarget = null;
    }
}
```
`spellPerSecond` is computed once up front from "how many total spells this upgrade wants to cast" divided by "how long the domain lasts" — spreading a fixed total count evenly across the whole active duration, rather than a flat fire-rate independent of duration.

```csharp
private void CastSpell(Transform target)
{
    if (upgradeType == SkillUpgradeType.Domain_EchoSpam)
    {
        Vector3 offset = Random.value < .5f ? new Vector2(1, 0) : new Vector2(-1, 0);
        skillManager.timeEcho.CreateTimeEcho(target.position + offset);
    }
    if (upgradeType == SkillUpgradeType.Domain_ShardSpam)
        skillManager.shard.CreateRawShard(target, true);
}
```
The clearest cross-skill composition in the whole system: Domain Expansion doesn't have its own attack logic at all for these two upgrades — it just repeatedly calls into `skillManager.timeEcho`/`skillManager.shard` directly, the same idea first seen in `Skill_Dash`. Note `CreateRawShard(target, true)` forces `shardsCanMove = true` explicitly, **regardless of whether the player's independently-chosen Shard upgrade would normally allow movement** — Domain Expansion's shard-spam works the same way no matter what's unlocked in the Shard tree specifically.

```csharp
public bool InstantDomain() =>
    upgradeType != SkillUpgradeType.Domain_EchoSpam && upgradeType != SkillUpgradeType.Domain_ShardSpam;
```
This is exactly what `PlayerState.Update()` (`01-Foundations.md`) checks to decide whether pressing Ultimate triggers an instant `CreateDomain()` on the spot, or transitions into the full rise-and-levitate `Player_DomainExpansionState` sequence — the Slowing Down upgrade is a quick instant-cast, while the two "spam" upgrades need the player suspended in the air for their repeated-casting duration.

`SkillObject_DomainExpansion` handles the visible growth/shrink (`Vector3.Lerp` toward a target scale, then shrinking to zero and self-destructing after `duration`) and, via trigger enter/exit, applies the domain's slow to any `Enemy` that steps in (`enemy.SlowDownEntity(duration, slowDownPercent, true)` — the `true` "can override current slowdown" argument from `02-Entity.md`, so the domain's slow always wins over any weaker slow already active) and removes it on exit. `Skill_DomainExpansion.ClearEnemiesFromDomain()` explicitly calls `StopSlowDown()` on every tracked enemy when the domain ends, so nothing is left permanently slowed after the fact.

---

## How this connects elsewhere

- `SkillUpgradeType` (`01-Foundations.md`) is the enum every branch in this entire system checks via `Unlocked(...)`.
- `Player_DashState`, `Player_SwordThrowState`, `Player_DomainExpansionState`, and `Player_AnimationTrigger.ThrowSword()` (`03-Player.md`) are the concrete call sites that trigger everything documented here.
- `Entity_Combat.PreformAttack()` and `Entity_VFX`/`Entity_StatusHandler` (`02-Entity.md`) share the exact same damage-application and status-effect shape reused here via `SkillObject_Base.DamageEnemiesInRadius`.
- `Player_SkillManager.ReduceAllSkillsCooldownBy`/`GetSkillByType` (`03-Player.md`) operate on every `Skill_Base` uniformly through `allSkills`.
- `Skill_DataSO`/`UpgradeData` get their full ScriptableObject-side treatment in the later `Data-ScriptableObjects` doc.

## Where this pattern could be reused later

The **manager component + spawned-object component, both built on a shared base, with the manager answering "what should happen" and the object handling "make it actually happen"** split is a strong, reusable shape for any ability/spell system with multiple distinct skills sharing common plumbing (cooldowns, damage application, targeting). The **single mutually-exclusive `upgradeType` field checked via one small `Unlocked()` helper** is a notably simple, readable way to implement branching upgrade trees without a sprawling inheritance hierarchy per upgrade combination — worth remembering over more "proper"-looking but more complex alternatives (a strategy object per upgrade, a full class per node) when the actual behavioral differences are this size. And the **skills-calling-into-other-skills'-managers** pattern (`Skill_Dash`/`Skill_DomainExpansion` reaching into `skillManager.shard`/`skillManager.timeEcho`) is a good reminder that composition between sibling systems can be simpler and more maintainable than duplicating spawn/behavior logic per feature that happens to want a similar effect.

---

**Next:** `09-SaveSystem.md` — `SaveManager`, `GameData`, `FileDataHandler`, and `SerializableDictionary`.
