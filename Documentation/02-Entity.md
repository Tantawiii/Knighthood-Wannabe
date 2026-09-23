# 02 — Entity: The Shared Foundation of Player & Enemy

## Overview

`Player` and `Enemy` are both, fundamentally, "a thing that moves, has health, can attack, can be hurt, and reacts to elemental effects." Rather than write all of that twice, this codebase pulls the **shared 90%** into a base class, `Entity`, plus a family of small companion components that sit alongside it on the same GameObject: `Entity_Health`, `Entity_Combat`, `Entity_Stats`, `Entity_StatusHandler`, `Entity_VFX`, `Entity_SFX`, `Entity_DropManager`, `Entity_AnimationTriggers`.

**Why split into so many small components instead of one big `Entity` class with everything in it?** This is Unity's component model working as intended: each `Entity_*` script does *one* job (health, combat, stats...) and finds its siblings via `GetComponent<T>()` in `Awake()`. This keeps each file small and readable, lets you reason about "how does health work" without also reading combat code, and — practically — lets `Player` and `Enemy` each add their *own* extra layer on top of just the pieces they need (`Player_Health` extends `Entity_Health`, but not every `Entity_*` component necessarily gets a Player-specific subclass). This "one concern per component" style is also why `Entity_AnimationTriggers` is separate from `Entity_Combat`, even though the trigger's whole job is to call into combat — see its section below for why.

If you haven't read `01-Foundations.md` yet, some references here (`StateMachine`, `IDamagable`) will make more sense if you do — this doc builds directly on it.

---

## `Entity.cs` — the base class

```csharp
public class Entity : MonoBehaviour
{
    public event Action onFlipped;

    public Animator animator { get; private set; }
    public Rigidbody2D rb { get; private set; }
    public Entity_SFX sfx { get; private set; }

    protected StateMachine stateMachine;

    private bool facingRight = true;
    public int facingDir { get; private set; } = 1;
    ...
```

Note this is **not** `abstract` — unlike `EntityState`, `Entity` could technically be attached directly to a GameObject on its own. In practice it never is; `Player` and `Enemy` always extend it (`public class Player : Entity`).

### Fields

- **`onFlipped`** — an event (see [Concepts-Glossary: Events](Concepts-Glossary.md#events--delegates-event-action)) fired whenever the entity turns around to face the other direction. Anything that needs to know "did this character just flip" (certain VFX positioning, for instance) subscribes to this instead of polling `facingDir` every frame.
- **`animator`, `rb`, `sfx`** — auto-properties with `private set`, meaning any script can *read* `somePlayer.animator`, but only `Entity` itself can *assign* it. These get filled in once, in `Awake()`, and never change afterward.
- **`stateMachine`** — `protected`, not `public`: only `Entity` and its subclasses (`Player`, `Enemy`) need direct access to swap states; outside scripts have no business reaching into another object's state machine directly.
- **`facingRight` / `facingDir`** — two representations of the same idea, kept for different audiences: `facingRight` is a private bool used internally by the flip logic, `facingDir` is a public `int` (`1` or `-1`) meant for *math* — multiplying a knockback force or a wall-check raycast direction by `facingDir` naturally points it whichever way the entity is facing, without an `if (facingRight) ... else ...` at every call site.
- **Collision detection fields** (`groundCheckDistance`, `wallCheckDistance`, `whatIsGround`, `groundCheck`, `primaryWallCheck`, `secondaryWallCheck`) — see the raycast concept note below; these are the tunable distances and the `Transform` markers raycasts fire from.
- **`isKnocked`, `knockbackCoroutine`, `slowdownCouroutine`** — private state for the knockback/slowdown systems below (note the file's own typo, `slowdownCouroutine` — flagged here only so you don't think it's a different variable if you search for it).

### `Awake()` and `Update()`

```csharp
protected virtual void Awake()
{
    animator = GetComponentInChildren<Animator>();
    rb = GetComponent<Rigidbody2D>();
    sfx = GetComponent<Entity_SFX>();
    stateMachine = new StateMachine();
}

protected virtual void Update()
{
    HandleCollisionDetection();
    stateMachine.UpdateActiveState();
}
```

Both are `virtual` — `Player.Awake()`/`Player.Update()` and `Enemy.Awake()`/`Enemy.Update()` override these and, by convention throughout this codebase, call `base.Awake()`/`base.Update()` first to get this shared setup, then add their own extra work. `animator` is fetched with `GetComponentInChildren` specifically because the Animator often lives on a child sprite GameObject, not the root — while `Rigidbody2D` is fetched with plain `GetComponent` because physics components live on the root object that actually moves.

`Update()` is short on purpose: every frame, refresh "am I touching ground/a wall right now," then let whichever state is currently active do its own thing. This one line is what makes the state machine described in `01-Foundations.md` actually run every frame.

### Movement helpers: `SetVelocity`, `HandleFlip`, `Flip`

```csharp
public void SetVelocity(float xVelocity, float yVelocity)
{
    if (isKnocked) return;
    rb.linearVelocity = new Vector2(xVelocity, yVelocity);
    HandleFlip(xVelocity);
}

public void HandleFlip(float xVelocity)
{
    if (facingRight && xVelocity < 0) Flip();
    else if (!facingRight && xVelocity > 0) Flip();
}

public void Flip()
{
    facingRight = !facingRight;
    transform.Rotate(0, 180, 0);
    facingDir *= -1;
    onFlipped?.Invoke();
}
```

`SetVelocity` is the **single chokepoint** every state uses to actually move the character, rather than states setting `rb.linearVelocity` directly — which is exactly why the knockback guard (`if (isKnocked) return;`) only has to exist in one place. While knocked back, *any* state trying to move the character is silently ignored until the knockback coroutine finishes and clears `isKnocked`.

`Flip()` is a classic 2D sprite-flip trick: rather than mirroring the sprite image itself, it rotates the whole Transform 180° around the Y axis, which visually mirrors *everything* attached to it (sprite, hitbox children, VFX spawn points) in one operation. `facingDir *= -1` keeps the numeric direction in sync with the rotation.

### Ground/wall detection: `HandleCollisionDetection`, `OnDrawGizmos`

```csharp
private void HandleCollisionDetection()
{
    groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);

    if (secondaryWallCheck != null)
        wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround)
                && Physics2D.Raycast(secondaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    else
        wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
}
```

See [Concepts-Glossary: Raycasts](Concepts-Glossary.md#raycasts--overlap-checks-physics2d) for what a raycast is. `groundCheck`/`primaryWallCheck`/`secondaryWallCheck` are empty child Transforms positioned at the character's feet and sides in the prefab — this method fires an invisible line down from the feet (ground) and sideways from the body (wall), both filtered to the `whatIsGround` layer.

The wall check has a subtle detail worth understanding: `secondaryWallCheck` is **optional** (checked for `null`). When it *is* assigned, wall detection requires **both** raycasts to hit — presumably to avoid a false "wall detected" from a single raycast clipping a small ledge or corner, requiring two points along the body to agree there's a solid wall there. `Vector2.right * facingDir` is a good example of the `facingDir` int pulling its weight: the exact same line works whether the character faces left or right, since multiplying by `-1` flips the direction automatically.

`OnDrawGizmos()` draws these same checks as lines in the Scene view (Editor-only, see [Concepts-Glossary: MonoBehaviour lifecycle](Concepts-Glossary.md#monobehaviour--the-unity-lifecycle)), so you can visually confirm the check points are positioned correctly while tweaking a prefab, instead of guessing blind.

### Knockback: `RecieveKnockBack`, `KnockbackCo`

```csharp
public void RecieveKnockBack(Vector2 knockback, float duration)
{
    knockbackCoroutine ??= StartCoroutine(KnockbackCo(knockback, duration));
}

private IEnumerator KnockbackCo(Vector2 knockback, float duration)
{
    isKnocked = true;
    rb.linearVelocity = knockback;
    yield return new WaitForSeconds(duration);
    rb.linearVelocity = Vector2.zero;
    isKnocked = false;
}
```

See [Concepts-Glossary: Coroutines](Concepts-Glossary.md#coroutines-ienumerator-yield-return-startcoroutine) and [`??=`](Concepts-Glossary.md#-null-coalescing-assignment) if unfamiliar. The `??=` guard means a knockback already in progress won't be restarted/stacked by another knockback call arriving mid-flight — only a genuinely *new* knockback (once `knockbackCoroutine` is cleared) starts fresh.

One thing worth flagging: this codebase never explicitly nulls `knockbackCoroutine` back out after the coroutine finishes, so a careful read shows this guard only prevents overlap while the coroutine reference is still considered non-null by Unity's coroutine handle semantics — worth keeping an eye on if back-to-back knockbacks ever behave oddly. Either way, the important behavioral takeaway holds: `SetVelocity` (above) refuses to move the character while `isKnocked` is `true`, so knockback always wins over normal movement input for its duration.

### Slowdown: a template method for subclasses

```csharp
public virtual void SlowDownEntity(float duration, float slowMultiplier, bool canOverrideCurrentSlowdown = false)
{
    if (slowdownCouroutine != null)
    {
        if (canOverrideCurrentSlowdown) StopCoroutine(slowdownCouroutine);
        else return;
    }
    slowdownCouroutine = StartCoroutine(SlowDownEntityCo(duration, slowMultiplier));
}

protected virtual IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
{
    yield return null;
}
```

`SlowDownEntityCo` is deliberately a **no-op stub** here — it just waits one frame and does nothing. `Entity` defines *when* a slowdown should start, and guards against overlapping ones, in the surrounding `SlowDownEntity` logic, but leaves *what slowing down actually means* to whichever subclass overrides `SlowDownEntityCo` (presumably `Player`/`Enemy` reduce a move-speed stat for the duration). This is a common pattern: the base class owns the reusable "don't let a new slowdown interrupt a stronger existing one unless told to" bookkeeping, while delegating the actual effect to subclasses that know their own movement system. `Entity_StatusHandler.ApplyChillEffect` (below) is what calls `entity.SlowDownEntity(...)` when an Ice status effect lands.

### `EntityDeath()`, `CurrentStateAnimationTrigger()`, `GetWhatIsGround()`

```csharp
public virtual void EntityDeath() { }
public void CurrentStateAnimationTrigger() => stateMachine.currentState.AnimationTrigger();
public LayerMask GetWhatIsGround() => whatIsGround;
```

`EntityDeath()` is another empty `virtual` stub — `Entity_Health.Die()` calls `entity?.EntityDeath()` on death, and `Player`/`Enemy` override it with their own actual death handling (switching to a dead state, disabling input, dropping loot triggers, etc. — covered in `03-Player.md`/`04-Enemy.md`). `CurrentStateAnimationTrigger()` is the forwarding link mentioned in `01-Foundations.md`'s explanation of `triggerCalled` — this is the method that gets called from `Entity_AnimationTriggers` (below), which itself gets called by a Unity Animation Event. `GetWhatIsGround()` exposes the private `whatIsGround` LayerMask to other scripts that need to run their own ground-layer checks (e.g. a skill object checking what it's allowed to land on).

---

## `Entity_Health.cs` — damage, healing, death

```csharp
public class Entity_Health : MonoBehaviour, IDamagable
{
    public event Action OnTakingDamage;
    public event Action OnHealthUpdate;
    ...
```

Implements `IDamagable` (see `01-Foundations.md`) — the reason combat code can damage Player and Enemy through one shared code path.

### Setup

```csharp
protected virtual void Awake()
{
    entityStats = GetComponent<Entity_Stats>();
    healthBar = GetComponentInChildren<Slider>();
    entityVFX = GetComponent<Entity_VFX>();
    entity = GetComponent<Entity>();
    dropManager = GetComponent<Entity_DropManager>();
    SetupHealth();
}

private void SetupHealth()
{
    if (entityStats == null) return;
    currentHealth = entityStats.GetMaxHealth();
    OnHealthUpdate += UpdateHealthBar;
    UpdateHealthBar();
    InvokeRepeating(nameof(RegenerateHealth), 0, regenInterval);
}
```

Notice `OnHealthUpdate += UpdateHealthBar;` — this class **subscribes to its own event**. That looks redundant at first (why not just call `UpdateHealthBar()` directly wherever health changes?) but it means the health-bar-updating logic is decoupled the exact same way an *external* listener would be: `UpdateHealthBar` doesn't need special-case treatment, it's just one more thing that happens to care when `OnHealthUpdate` fires, alongside whatever the UI later subscribes for its own health display.

`InvokeRepeating(nameof(RegenerateHealth), 0, regenInterval)` is a built-in Unity MonoBehaviour method that calls `RegenerateHealth()` on a repeating timer — starting immediately, then every `regenInterval` seconds — without needing a coroutine. A simpler tool for "just call this method on a fixed schedule forever," compared to coroutines, which are better suited to a *finite*, sequenced series of steps.

### `TakeDamage` — the core combat math

```csharp
public virtual bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
{
    if (isDead || !canTakeDamage) return false;
    if (AttackEvaded()) return false;

    Entity_Stats attackerStats = damageDealer.GetComponent<Entity_Stats>();
    float armorReduction = attackerStats != null ? attackerStats.GetArmorReduction() : 0;

    float mitigation = entityStats != null ? entityStats.GetArmorMitigation(armorReduction) : 0;
    float physicalDamageTaken = damage * (1 - mitigation);

    float resistance = entityStats != null ? entityStats.GetElementalResistance(element) : 0;
    float elementalDamageTaken = elementalDamage * (1 - resistance);

    TakeKnockback(damageDealer, physicalDamageTaken);
    ReduceHealth(physicalDamageTaken + elementalDamageTaken);
    lastDamageTaken = physicalDamageTaken + elementalDamageTaken;

    OnTakingDamage?.Invoke();
    return true;
}
```

Read this top to bottom as a pipeline:

1. **Early outs** — already dead, or temporarily invulnerable (`canTakeDamage`, toggled via `SetCanTakeDamage` — useful for i-frames or scripted invulnerability windows), or the defender rolled an evade. Any of these return `false` immediately and *nothing else in this method runs* — no knockback, no health loss, no event.
2. **Read the attacker's armor-piercing stat** (`GetArmorReduction`) — note the `attackerStats != null ?` guard: `damageDealer` might not have an `Entity_Stats` at all (e.g. damage from a scripted trap or hazard rather than a stat-driven character), so this treats "no stats" as "0% armor reduction" rather than crashing.
3. **Apply the defender's mitigation to physical damage, and resistance to elemental damage separately** — two independent damage-reduction pipelines, which is why `Entity_Stats` (below) has two distinct calculation methods for them.
4. **Trigger knockback based on the *post-mitigation* damage** — meaning a heavily-armored character not only takes less damage but also gets knocked back based on that already-reduced number, not the attack's raw damage.
5. **Apply the total to health, remember it as `lastDamageTaken`** (used elsewhere, e.g. for damage-number UI popups), **and fire the event.**

### Evasion, knockback sizing, health mutation

```csharp
private bool AttackEvaded()
{
    if (entityStats == null) return false;
    else return UnityEngine.Random.Range(0, 100) < entityStats.GetEvasion();
}
```
A straightforward percentage roll: `GetEvasion()` returns a 0–100 value, and a random 0–100 roll landing *under* it counts as a dodge. `UnityEngine.Random` is written with its full namespace here specifically to disambiguate from `System.Random` — both exist, and this file already has `using System;` at the top, so without the full `UnityEngine.` prefix the compiler wouldn't know which `Random` you meant.

```csharp
private Vector2 CalculateKnockback(float damage, Transform damageDealer)
{
    int direction = transform.position.x > damageDealer.position.x ? 1 : -1;
    Vector2 knockback = IsHeavyDamage(damage) ? heavyKnockbackPower : knockbackPower;
    knockback.x *= direction;
    return knockback;
}

private bool IsHeavyDamage(float damage) =>
    entityStats != null && damage / entityStats.GetMaxHealth() > heavyDamageThreshold;
```
Two knockback presets (`knockbackPower` vs `heavyKnockbackPower`, each with its own duration) rather than a smoothly scaling force — `IsHeavyDamage` just asks "did this single hit cost more than `heavyDamageThreshold` (default 30%) of my max health?" to decide which preset applies. `direction` is computed by comparing X positions, not using the attacker's own `facingDir` — it always pushes the defender *away from* the attacker's position, regardless of which way the attacker happens to be facing.

```csharp
public void ReduceHealth(float damage)
{
    currentHealth -= damage;
    entityVFX?.PlayOnDamageVFX();
    OnHealthUpdate?.Invoke();
    if (currentHealth <= 0) Die();
}

protected virtual void Die()
{
    isDead = true;
    entity?.EntityDeath();
    dropManager?.DropItems();
}
```
`ReduceHealth` is `public`, not called only from `TakeDamage` — `Entity_StatusHandler`'s burn-tick and lightning-strike effects call it directly too, since damage-over-time isn't really "an attack" in the `TakeDamage` sense (no attacker Transform, no evasion roll — a burn tick shouldn't be dodgeable after the fact). `Die()` is where the death "fan-out" happens: flip `isDead` (which locks out all future damage via `TakeDamage`'s early-out, and — combined with `Entity`'s `SwitchOffStateMachine`, called elsewhere in a subclass's own death handling — locks out further state changes), tell the `Entity` subclass to run its own death behavior, and tell the drop manager to roll loot. Both calls use `?.` since not every `Entity_Health` owner necessarily has a `dropManager` (an object that dies but never drops loot, for instance).

### Reading/exposing health elsewhere

```csharp
public float GetHealthPercent() => currentHealth / entityStats.GetMaxHealth();
public void SetHealthPercent(float health) { currentHealth = entityStats.GetMaxHealth() * Mathf.Clamp01(health); OnHealthUpdate?.Invoke(); }
public float GetCurrentHealthValue() => currentHealth;
public void EnableHealthBar(bool enable) => healthBar?.transform.parent.gameObject.SetActive(enable);
```
A small public API for anything that needs to read or restore health as a 0–1 percentage (UI bars, save/load restoring exact HP, a full-heal item effect) without reaching into `currentHealth` directly — that field stays `[SerializeField] protected`, visible in the Inspector for debugging but not freely writable from outside code. `Mathf.Clamp01` in `SetHealthPercent` is a safety net ensuring a bad input value (e.g. `1.4f` from a buggy caller) can't set health above max or below zero.

---

## `Entity_Combat.cs` — melee hit detection

```csharp
public class Entity_Combat : MonoBehaviour
{
    public event Action<float> OnDoingPhysicalDamage;
    ...
    public void PreformAttack()
    {
        bool targetGotHit = false;
        foreach (var target in GetDetectedColliders())
        {
            IDamagable damagable = target.GetComponent<IDamagable>();
            if (damagable == null) continue;

            AttackData attackData = stats.GetAttackData(basicDamageScale);
            Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

            float physicalDamage = attackData.physicalDamage;
            float elementalDamage = attackData.elementalDamage;
            ElementType element = attackData.elementType;

            targetGotHit = damagable.TakeDamage(physicalDamage, elementalDamage, element, transform);

            if (element != ElementType.None)
                statusHandler.ApplyStatusEffect(element, attackData.elementalEffectData);

            if (targetGotHit)
            {
                OnDoingPhysicalDamage?.Invoke(physicalDamage);
                vfx.CreateOnHitVFX(target.transform, attackData.isCritical, element);
                sfx?.PlayAttackHitSFX();
            }
        }
        if (!targetGotHit) sfx?.PlayAttackMissSFX();
    }

    protected Collider2D[] GetDetectedColliders() =>
        Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
}
```

`PreformAttack()` (the method name has this spelling throughout the actual code — noted so you recognize it rather than assume a typo when searching) is called once per attack **from an Animation Event**, not from `Update()` — see `Entity_AnimationTriggers` below for that wiring, and `01-Foundations.md`'s `triggerCalled` explanation for why attacks are timed off the animation rather than the button press.

Walking through the loop: it grabs every collider inside the overlap circle (see [Concepts-Glossary: overlap checks](Concepts-Glossary.md#raycasts--overlap-checks-physics2d)), skips anything that isn't `IDamagable` (`continue` moves to the next collider), then for each valid target builds a **fresh** `AttackData` — meaning **damage is rolled independently per target hit in a single swing**, so a crit against one enemy in a multi-hit swing doesn't force a crit against another. It applies the attack via the `IDamagable` interface, then — *only if the element isn't `None`* — applies the matching status effect.

Worth a careful read: `statusHandler.ApplyStatusEffect` is called regardless of `targetGotHit`, right after the `TakeDamage` call. So an evaded hit still applies a status effect in the current code, since the element check runs independently of whether damage landed — it's easy to assume "no hit = no effects," but the code as written doesn't quite guarantee that.

`OnDoingPhysicalDamage` is an `Action<float>` (carries the damage dealt as a parameter) — this is what lifesteal-style item effects (`ItemEffect_HealOnDoingDamage`, documented later) subscribe to, to heal the attacker by some fraction of whatever damage this event reports. The miss/hit SFX split (`PlayAttackMissSFX` only if the loop never set `targetGotHit` to `true`) gives audio feedback even for whiffed attacks.

---

## `Entity_Stats.cs` — the stat calculation hub

```csharp
public class Entity_Stats : MonoBehaviour
{
    public Stat_SetupSO defaultStat;
    public Stat_OffenseGroup offenseGroup;
    public Stat_DefenseGroup defenseGroup;
    public Stat_ResourceGroup resourceGroup;
    public Stat_MajorGroup majorGroup;
    ...
```

This is the component every other `Entity_*` script leans on for numbers. The four `Stat_*Group` fields are covered fully in a later `StatSystem` doc, but briefly: each is a small container grouping related `Stat` objects (e.g. `offenseGroup.damage`, `offenseGroup.critChance`). A `Stat` (from `Scripts/StatSystem/Stat.cs`) isn't just a raw number — it's a **base value plus a list of modifiers** (from equipment, buffs, etc.), and calling `.GetValue()` on it returns the base value with all current modifiers summed in, cached until something calls `.AddModifier`/`.RemoveModifier` again (a `needToRecalculate` flag), so it doesn't re-sum the whole list every single frame it's read. You'll see `SomeGroup.someStat.GetValue()` throughout this file — that's always this pattern.

### Elemental damage — "highest element wins, others count half"

```csharp
public float GetElementalDamage(out ElementType element, float scaleFactor = 1)
{
    float fireDamage = offenseGroup.fireDamage.GetValue();
    float iceDamage = offenseGroup.iceDamage.GetValue();
    float lightningDamage = offenseGroup.lightningDamage.GetValue();
    float bonusElementalDamage = majorGroup.intelligence.GetValue();

    float highestDamage = fireDamage;
    element = ElementType.Fire;
    if (iceDamage > highestDamage) { highestDamage = iceDamage; element = ElementType.Ice; }
    if (lightningDamage > highestDamage) { highestDamage = lightningDamage; element = ElementType.Lightning; }

    if (highestDamage <= 0) { element = ElementType.None; return 0; }

    float bonusFire = (element == ElementType.Fire) ? 0 : fireDamage * .5f;
    float bonusIce = (element == ElementType.Ice) ? 0 : iceDamage * .5f;
    float bonusLightning = (element == ElementType.Lightning) ? 0 : lightningDamage * .5f;

    float weakerElementalDamage = bonusFire + bonusIce + bonusLightning;
    float finalDamage = highestDamage + bonusElementalDamage + weakerElementalDamage;

    return finalDamage * scaleFactor;
}
```

The `out ElementType element` parameter is worth pausing on if you haven't seen `out` before: it lets a method return **two** pieces of information at once — the normal `return`ed `float` (the damage amount) *and* the `element` that damage counts as, written back into whatever variable the caller passed in. `AttackData`'s constructor is the caller (see below) and needs both.

The actual rule this method encodes: an entity might have some Fire damage, some Ice damage, and some Lightning damage stat all at once (from mixed equipment, say). Rather than dealing three separate hits, the game picks whichever is *highest* as "the" element for this attack — for status-effect purposes, a burn, chill, or shock — but the other two elements aren't wasted: each contributes **half** its value as flat bonus damage on top. `bonusElementalDamage` (from Intelligence) always applies regardless of which element wins. If all three elemental stats are `0`, there's no elemental component at all (`ElementType.None`, `0` damage) — this is the branch that makes a purely-physical attacker correctly deal zero elemental damage rather than some leftover garbage value.

### Mitigation, evasion, crit — each a small formula

```csharp
public float GetArmorMitigation(float armorReduction)
{
    float totalArmor = GetBaseArmor();
    float reductionMultiplier = Mathf.Clamp01(1 - armorReduction);
    float effectiveArmor = totalArmor * reductionMultiplier;
    float mitigation = effectiveArmor / (effectiveArmor + 100);
    return Mathf.Clamp(mitigation, 0, .85f);
}
```
A common "diminishing returns" armor formula (`armor / (armor + 100)`) — as armor grows, mitigation approaches but never reaches 100%, capped explicitly at 85% here so a character can never become fully unhittable no matter how much armor they stack. `armorReduction` (the attacker's own armor-piercing stat) first reduces the *effective* armor being mitigated against, before that formula runs.

```csharp
public float GetEvasion()
{
    float baseEvasion = defenseGroup.evasion.GetValue();
    float bonusEvasion = majorGroup.agility.GetValue() * .5f;
    float totalEvasion = baseEvasion + bonusEvasion;
    return Mathf.Clamp(totalEvasion, 0, 85f);
}

public float GetPhysicalDamage(out bool isCrit, float scaleFactor = 1)
{
    float baseDamage = GetBaseDamage();
    float critChance = GetCritChance();
    float critPower = GetCritPower() / 100;
    isCrit = Random.Range(0, 100) < critChance;
    float finalDamage = isCrit ? baseDamage * critPower : baseDamage;
    return finalDamage * scaleFactor;
}

public float GetBaseDamage() => offenseGroup.damage.GetValue() + majorGroup.strength.GetValue();
public float GetCritChance() => offenseGroup.critChance.GetValue() + (majorGroup.agility.GetValue() * .3f);
public float GetCritPower() => offenseGroup.critPower.GetValue() + (majorGroup.strength.GetValue() * .5f);
public float GetBaseArmor() => defenseGroup.armor.GetValue() + majorGroup.vitality.GetValue();
```
Notice the repeating shape across `GetEvasion`, `GetBaseDamage`, `GetCritChance`, `GetCritPower`, `GetBaseArmor`: each takes a "major" stat (Agility, Strength, Vitality...) and blends it into an "offense/defense" stat at some fractional weight (`.5f`, `.3f`, etc.). This is the RPG-stat-system idea of **primary stats indirectly buffing secondary stats** — e.g. Agility isn't *directly* "evasion," it's a stat that contributes to evasion *and* crit chance *and* (as seen above) elemental resistance, giving each major stat multiple reasons to invest in it. `GetPhysicalDamage`'s crit roll follows the identical "random 0–100 under a percentage stat" pattern already seen in `Entity_Health.AttackEvaded()`.

### `GetElementalResistance`, `GetStatByType`, `ApplyDefaultStatSetup`

```csharp
public float GetElementalResistance(ElementType element)
{
    float baseResistance = 0;
    float bonusResistance = majorGroup.intelligence.GetValue() * .5f;
    switch (element)
    {
        case ElementType.Fire: baseResistance = defenseGroup.fireRes.GetValue(); break;
        case ElementType.Ice: baseResistance = defenseGroup.iceRes.GetValue(); break;
        case ElementType.Lightning: baseResistance = defenseGroup.lightningRes.GetValue(); break;
    }
    float resistance = baseResistance + bonusResistance;
    return Mathf.Clamp(resistance, 0, 75f) / 100;
}
```
Same "primary stat feeds a secondary stat" idea — Intelligence feeds all three elemental resistances at once — capped at 75%, then divided by 100 to convert a percentage into the 0–1 multiplier that `Entity_Health.TakeDamage` multiplies damage by directly (`elementalDamage * (1 - resistance)`).

```csharp
public Stat GetStatByType(StatType type)
{
    switch (type)
    {
        case StatType.MaxHealth: return resourceGroup.maxHealth;
        ...
        default:
            Debug.LogWarning($"StatType {type} not implemented yet.");
            return null;
    }
}
```
This is the lookup table mentioned in `01-Foundations.md`'s `StatType` section — it exists so **generic** code (a stat panel UI that loops over every `StatType` to display it, a tooltip system) doesn't need a hardcoded `if/else` per stat; it just asks "give me the `Stat` object for `StatType.CritChance`" and this method resolves which field that actually is. The `default` case logging a warning rather than crashing is a deliberate soft-failure: if `StatType` ever gets a new enum value that isn't wired up here yet, the game keeps running — just without that stat displaying — and the warning tells you exactly what to go fix.

```csharp
[ContextMenu("Update Default Stat Setup")]
public void ApplyDefaultStatSetup()
{
    if (defaultStat == null) { Debug.Log("No default stat setup assigned"); return; }
    resourceGroup.maxHealth.SetBaseValue(defaultStat.maxHealth);
    ...
}
```
See [Concepts-Glossary: `[ContextMenu]`](Concepts-Glossary.md#contextmenu). This copies every value out of a `Stat_SetupSO` ScriptableObject (a reusable "stat preset" asset — e.g. "Skeleton base stats") into this specific entity's live `Stat` objects, callable by hand from the Inspector's right-click menu. Handy for resetting a prefab's stats to match its intended preset after manually tweaking values while testing.

---

## `Entity_StatusHandler.cs` — elemental status effects

```csharp
public class Entity_StatusHandler : MonoBehaviour
{
    ElementType currentEffect = ElementType.None;
    ...
    public void ApplyStatusEffect(ElementType element, ElementalEffectData effectData)
    {
        if (element == ElementType.Ice && CanBeApplied(ElementType.Ice))
            ApplyChillEffect(effectData.chillDuration, effectData.chillSlowMultiplier);
        if (element == ElementType.Fire && CanBeApplied(ElementType.Fire))
            ApplyBurnEffect(effectData.burnDuration, effectData.burnDamage);
        if (element == ElementType.Lightning && CanBeApplied(ElementType.Lightning))
            ApplyShockEffect(effectData.shockDuration, effectData.shockDamage, effectData.shockCharge);
    }

    public bool CanBeApplied(ElementType element)
    {
        if (element == ElementType.Lightning && currentEffect == ElementType.Lightning) return true;
        return currentEffect == ElementType.None;
    }
}
```

`currentEffect` tracks **at most one active status effect at a time** — `CanBeApplied` is the gatekeeper, and its logic has a deliberate exception worth reading carefully: normally a new status can only start if nothing else is active (`currentEffect == ElementType.None`). But **Lightning is allowed to reapply onto itself** even while already active — this is what makes the "charge and burst" mechanic below work at all, since each Lightning hit needs to add *more* charge even while a shock effect is already ticking. A Fire attack landing on an already-Chilled target, meanwhile, does nothing — no stacking status effects of different types.

### Chill (Ice) — a slow

```csharp
private void ApplyChillEffect(float duration, float slowMultiplier)
{
    float iceResistance = entity_Stats.GetElementalResistance(ElementType.Ice);
    float reducedDuration = duration * (1 - iceResistance);
    StartCoroutine(ChillEffectCo(reducedDuration, slowMultiplier));
}

IEnumerator ChillEffectCo(float duration, float slowMultiplier)
{
    entity.SlowDownEntity(duration, slowMultiplier);
    currentEffect = ElementType.Ice;
    entity_VFX.PlayOnStatusVfx(duration, ElementType.Ice);
    yield return new WaitForSeconds(duration);
    currentEffect = ElementType.None;
}
```
Simplest of the three: resistance shortens the *duration* rather than reducing the slow's strength, delegates the actual slow to `entity.SlowDownEntity` (the template method from `Entity.cs`), plays a status VFX for the same duration, then after waiting, clears `currentEffect` back to `None` — which is what lets a *new* status effect be applied afterward, since `CanBeApplied` checks exactly this flag.

### Burn (Fire) — damage over time, ticking

```csharp
private void ApplyBurnEffect(float duration, float fireDamage)
{
    float fireResistance = entity_Stats.GetElementalResistance(ElementType.Fire);
    float reducedDamage = fireDamage * (1 - fireResistance);
    StartCoroutine(BurnEffectCo(duration, reducedDamage));
}

IEnumerator BurnEffectCo(float duration, float totalDamage)
{
    currentEffect = ElementType.Fire;
    entity_VFX.PlayOnStatusVfx(duration, ElementType.Fire);

    int ticksPerSecond = 2;
    int tickCount = Mathf.RoundToInt(ticksPerSecond * duration);
    float damagePerTick = totalDamage / tickCount;
    float tickInterval = 1f / ticksPerSecond;

    for (int i = 0; i < tickCount; i++)
    {
        entity_Health.ReduceHealth(damagePerTick);
        yield return new WaitForSeconds(tickInterval);
    }
    currentEffect = ElementType.None;
}
```
Here resistance reduces the *total damage pool* rather than the duration — a different design choice from Chill, worth noticing: each element's resistance interacts with its effect differently, there's no single unified "resistance always does X" rule. The `totalDamage` is split evenly across a fixed number of ticks (2 per second) — this is why `Entity_Health.ReduceHealth` is called directly here rather than going through `TakeDamage`: a burn tick isn't a discrete "attack" with an attacker, an evasion roll, or its own knockback, it's a guaranteed drip of damage already calculated once up front.

### Shock (Lightning) — charge and burst, the odd one out

```csharp
private void ApplyShockEffect(float duration, float electrifyDamage, float charge)
{
    float lightningResistance = entity_Stats.GetElementalResistance(ElementType.Lightning);
    float finalcharge = charge * (1 - lightningResistance);
    currentCharge += finalcharge;

    if (currentCharge >= maxCharge)
    {
        LightningStrike(electrifyDamage);
        StopShockEffect();
        return;
    }

    if (shockCo != null) StopCoroutine(shockCo);
    shockCo = StartCoroutine(ShockEffectCo(duration));
}

private void LightningStrike(float electrifyDamage)
{
    Instantiate(lightningStrikeVFX, transform.position, Quaternion.identity);
    entity_Health.ReduceHealth(electrifyDamage);
}
```
This is the effect `CanBeApplied`'s special case exists for: each Lightning hit adds `finalcharge` (resistance-reduced) to a running `currentCharge` total. If enough hits land before the charge visual expires, `currentCharge` crosses `maxCharge` and triggers an instant `LightningStrike` — a burst of damage plus a VFX, distinct from Chill/Burn's steady effect over time. If it *doesn't* cross the threshold yet, a coroutine (`ShockEffectCo`, restarted each hit via `if (shockCo != null) StopCoroutine(shockCo);`) keeps the charge visual playing and resets its expiry timer, so repeated Lightning hits within the effect's window keep extending it rather than the charge silently expiring between hits.

---

## `Entity_DropManager.cs` — loot rolling

```csharp
public List<Item_DataSO> RollDrops()
{
    List<Item_DataSO> possibleDrops = new List<Item_DataSO>();
    List<Item_DataSO> finalDrops = new List<Item_DataSO>();

    foreach (var item in dropData.itemList)
    {
        float dropChance = item.GetDropChance();
        if (Random.Range(0f, 100f) <= dropChance)
            possibleDrops.Add(item);
    }

    possibleDrops = possibleDrops.OrderByDescending(item => item.itemRarity).ToList();

    foreach (var item in possibleDrops)
    {
        if (maxRarityAmount > item.itemRarity)
        {
            finalDrops.Add(item);
            maxRarityAmount -= item.itemRarity;
        }
    }
    return finalDrops;
}

public virtual void DropItems()
{
    if (dropData == null) { Debug.LogWarning(...); return; }
    List<Item_DataSO> itemsToDrop = RollDrops();
    int amountToDrop = Mathf.Min(itemsToDrop.Count, maxItemsToDrop);
    for (int i = 0; i < amountToDrop; i++)
        CreateItemDrop(itemsToDrop[i]);
}
```

`RollDrops()` is a clean three-step algorithm worth tracing through explicitly, since it uses LINQ (see [Concepts-Glossary](Concepts-Glossary.md#generic-collections-listt-and-linq)):

1. **Filter** — roll each possible drop item's own drop chance independently; anything that "hits" goes into `possibleDrops`. The number of items that pass this step is random and unbounded — could be zero, could be all of them.
2. **Sort** — `OrderByDescending(item => item.itemRarity)` reorders `possibleDrops` so the *rarest* (highest-rarity-number) items come first.
3. **Budget-limited greedy fill** — walk the sorted list rarest-first, and as long as there's still room in the `maxRarityAmount` "budget," add the item and subtract its rarity cost from the remaining budget. Processing rarest-first means this naturally prioritizes giving the player the most valuable drops the budget can afford, rather than random ones.

`DropItems()` then caps the *count* of items actually spawned separately, via `maxItemsToDrop` — so there are two independent limits at play: a "value budget" (`maxRarityAmount`) and a "how many objects to physically spawn" cap (`maxItemsToDrop`), taking whichever is more restrictive (`Mathf.Min`). `CreateItemDrop` instantiates the pickup prefab and calls `SetupItem` on its `Object_ItemPickup` component (covered in a later doc) to tell it which item it represents.

You may notice a commented-out `Update()` block in this file (`// if (Input.GetKeyDown(KeyCode.X)) { DropItems(); }`) — that's leftover debug code for manually testing drops during development, not something in use. Safe to ignore if you spot it browsing the file yourself; mentioned here only so you recognize it rather than wonder if it's load-bearing.

---

## `Entity_VFX.cs` — damage flash, status tint, hit effects

```csharp
public void PlayOnDamageVFX()
{
    if (onDamageVFXCoroutine != null) StopCoroutine(onDamageVFXCoroutine);
    onDamageVFXCoroutine = StartCoroutine(OnDamageVFXCo());
}

private IEnumerator OnDamageVFXCo()
{
    spriteRenderer.material = onDamageMaterial;
    yield return new WaitForSeconds(onDamageVFXDuration);
    spriteRenderer.material = originalMaterial;
}
```
The "flash white/red on hit" effect: temporarily swap the sprite's material, wait a fraction of a second, swap it back. Restarting the coroutine on every new hit, instead of letting an old one finish first, is why rapid repeated hits keep the flash looking continuous rather than flickering off between hits.

```csharp
IEnumerator PlayStatusVfxCo(float duration, Color effectColor)
{
    float tickInterval = .25f;
    float timeHasPassed = 0;
    Color lightColor = effectColor * 1.2f;
    Color darkColor = effectColor * .95f;
    bool toggle = false;

    while (timeHasPassed < duration)
    {
        spriteRenderer.color = toggle ? lightColor : darkColor;
        toggle = !toggle;
        yield return new WaitForSeconds(tickInterval);
        timeHasPassed += tickInterval;
    }
    spriteRenderer.color = Color.white;
}
```
The status-effect tint (called by `Entity_StatusHandler` for Chill/Burn/Shock) alternates the sprite's color tint between a slightly lighter and slightly darker version of the effect's color every quarter-second, for a subtle pulsing look, rather than a flat static tint — `effectColor * 1.2f` and `* .95f` work because Unity's `Color` supports scalar multiplication, scaling each of its R/G/B channels.

```csharp
public void CreateOnHitVFX(Transform target, bool isCrit, ElementType element)
{
    GameObject hitPrefab = isCrit ? critHitVFX : hitVFX;
    GameObject vfx = Instantiate(hitPrefab, target.position, Quaternion.identity);
    if (entity.facingDir == -1 && isCrit) vfx.transform.Rotate(0, 180, 0);
}
```
Picks between two different hit-spark prefabs depending on whether the hit crit, spawns it at the target's position, and — only for crits, only when the attacker faces left — flips the VFX 180° so an asymmetric crit-spark graphic still reads correctly regardless of attack direction. Note this is `Entity_VFX` on the *attacker*, reading the *attacker's* `facingDir` (`entity` here refers to whoever owns this VFX component, i.e. whoever landed the hit) — a detail easy to misread since the VFX itself spawns at the *target's* position.

---

## `Entity_SFX.cs` — audio wrapper

```csharp
public void PlayAttackHitSFX() => AudioManager.Instance.PlaySFX(attackHit, audioSource, soundDistance);
public void PlayAttackMissSFX() => AudioManager.Instance.PlaySFX(attackMiss, audioSource, soundDistance);
```
A thin, single-purpose wrapper: it just names *which* sound effect to play (by string name, looked up in `AudioManager`'s own database — covered in a later doc) and forwards to the actual `AudioManager` singleton (see [Concepts-Glossary: Singletons](Concepts-Glossary.md#singletons-instance)) to do the real work. `soundDistance` plus the `OnDrawGizmos`/`drawGizmos` toggle exist purely to visualize, while editing in the Scene view, how far away this sound is meant to be audible from — useful for spatial/positional audio tuning.

---

## `Entity_AnimationTriggers.cs` — the Animation Event bridge

```csharp
public class Entity_AnimationTriggers : MonoBehaviour
{
    Entity entity;
    Entity_Combat entityCombat;

    protected virtual void Awake()
    {
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<Entity_Combat>();
    }

    private void CurrentStateTrigger() => entity.CurrentStateAnimationTrigger();
    private void AttackTrigger() => entityCombat.PreformAttack();
}
```

This entire class exists for one reason: Unity's **Animation Event** system can only call methods on a component that lives on the *same GameObject as the Animator* — which, per `Entity.Awake()`'s `GetComponentInChildren<Animator>()`, is often a child sprite object, not the root `Entity`/`Player`/`Enemy` object. So this small component sits on that child, and its job is purely to receive the animation-triggered call and forward it up to the real logic via `GetComponentInParent`.

**Why this matters for reading the code:** both `CurrentStateTrigger()` and `AttackTrigger()` are `private` and have **no C# callers anywhere in the codebase** — if you `grep` for `CurrentStateTrigger()` you will only find its own definition. That's not dead code; it's called from *inside the Unity Editor*, by name, from a keyframe on an animation clip — a non-code, Editor-only configuration you'd only see by opening the Animation window on a specific clip. This is the single most important "gotcha" pattern to internalize from this whole component family: **animation-driven trigger methods are invisible to a pure code search**, and `CurrentStateTrigger`/`AttackTrigger` are the two currently in use. `CurrentStateTrigger` is the generic one — it fires `triggerCalled = true` on whatever state is currently active (used by states like a dash or a skill cast that need "the animation reached its key moment" timing but aren't specifically a melee attack); `AttackTrigger` is specifically wired to `Entity_Combat.PreformAttack()` for basic melee swings.

---

## How Entity's components connect (the full picture)

A typical hit, traced end to end:

1. An attack animation reaches its "hit frame" → Unity fires an Animation Event → `Entity_AnimationTriggers.AttackTrigger()` → `Entity_Combat.PreformAttack()`.
2. `PreformAttack()` overlap-checks for targets, and for each `IDamagable` found, builds an `AttackData` from the attacker's `Entity_Stats` and calls `TakeDamage` on the target's `Entity_Health`.
3. `Entity_Health.TakeDamage` runs the defender's own `Entity_Stats` mitigation/resistance math, reduces health, triggers `Entity.RecieveKnockBack`, and fires `OnHealthUpdate`/`OnTakingDamage` events.
4. If the attack carried an element, `Entity_StatusHandler.ApplyStatusEffect` on the target kicks off a Chill/Burn/Shock coroutine, which calls back into `Entity_Health.ReduceHealth` (ticks) and `Entity.SlowDownEntity` (Chill) independently of the original attack.
5. `Entity_VFX`/`Entity_SFX` react to all of the above (damage flash, hit sparks, sound) without any of the combat/health/status code needing to know VFX or audio details directly — they're just called as needed.
6. If health hits zero, `Entity_Health.Die()` calls `Entity.EntityDeath()` (overridden per Player/Enemy) and `Entity_DropManager.DropItems()`.

Every arrow in that chain is either a direct method call between sibling components (found via `GetComponent` in `Awake()`) or an event (`OnHealthUpdate`, `OnTakingDamage`, `OnDoingPhysicalDamage`, `onFlipped`) that something else subscribes to — there's no central "combat manager" orchestrating this from outside; the behavior emerges from each small component reacting to the ones next to it.

## Where this pattern could be reused later

The "one base class (`Entity`) for truly universal behavior, plus a family of single-purpose sibling components wired together via `GetComponent` and events" structure is a strong general template for *any* game with multiple character types sharing core mechanics (health, combat, status effects) but differing in control scheme or AI. The specific formulas — armor mitigation curve, elemental "highest wins, others half" rule, charge-based Lightning — are this game's own design, worth remembering as *examples* of RPG damage-formula design more than as universal rules. But the underlying techniques (diminishing-returns mitigation, primary stats feeding multiple secondary stats, template methods like `SlowDownEntityCo` for subclasses to fill in) are all patterns worth reaching for again in future projects with similar stat-driven combat.

---

**Next:** `03-Player.md` — `Player.cs`, its `Player_*` components, and the concrete Player states built on `PlayerState` (covered in `01-Foundations.md`).
