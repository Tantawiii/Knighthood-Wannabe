# Concepts Glossary

This file explains the general programming and Unity ideas that show up **over and over** across the codebase. Instead of re-explaining "what is a coroutine" every single time a new script uses one, every doc in this `Documentation/` folder links back here the first time a concept appears. Read this once, then treat it as a dictionary you come back to.

Nothing here is specific to *this* game — these are things you'll use in basically any Unity project, so they're worth actually understanding rather than memorizing.

---

## MonoBehaviour & the Unity lifecycle

Any class that writes `: MonoBehaviour` after its name can be attached to a GameObject in the Unity Editor as a **component**. In exchange, Unity will automatically call certain methods on it at certain times, *if you define them* — you never call these yourself.

The ones you'll see constantly in this codebase:

- **`Awake()`** — called once, as early as possible, before any `Start()` on any script runs. This project uses it almost exclusively to do `GetComponent<T>()` lookups — "go find my sibling components and remember them" — because you want that wiring done before anything else tries to use it.
- **`Start()`** — called once, after all `Awake()`s have run. Used when a script needs to reference something set up by *another* script's `Awake()`.
- **`Update()`** — called once per rendered frame. Frame rate varies, so anything time-based in here uses `Time.deltaTime` (the time since the last frame) to stay consistent regardless of frame rate.
- **`FixedUpdate()`** — called at a fixed timestep (independent of frame rate), used for physics. Not heavily used directly in this codebase since `Rigidbody2D.linearVelocity` is mostly set from `Update`/state code and Unity's physics step picks it up.
- **`OnDrawGizmos()`** — Editor-only, draws debug shapes (circles, lines) in the Scene view. Never runs in a real build. Used throughout for visualizing things like attack range or ground-check raycasts so you can *see* invisible logic while tweaking values.

You never call `Awake`/`Start`/`Update` yourself — Unity's engine loop finds every active MonoBehaviour in the scene and calls these methods on it automatically, in this order, every frame.

## `[SerializeField]` and the Inspector

```csharp
[SerializeField] private float moveSpeed = 8f;
```

Normally a `private` field is invisible outside its class, including in the Unity Editor's Inspector panel. `[SerializeField]` is an attribute that tells Unity "expose this field in the Inspector anyway, so a designer (or you, tweaking numbers) can edit it per-GameObject without touching code" — while still keeping it `private` from other *scripts*. This is why so many fields in this codebase are `private` but still have Inspector-friendly default values and `[Header("...")]`/`[Tooltip("...")]` attributes grouping them.

`public` fields are also visible in the Inspector automatically (no attribute needed) — you'll see a mix of both. The general pattern in this codebase: `[SerializeField] private` for values only this class should read internally, `public` for values other scripts are expected to reach into directly.

## `[Serializable]` (class-level) vs. `[SerializeField]` (field-level)

```csharp
[Serializable]
public class Stat
{
    [SerializeField] float baseValue;
    [SerializeField] List<StatModifier> modifiers = new List<StatModifier>();
}
```

These look similar and are easy to mix up, but they answer two different questions. `[SerializeField]` (covered above) is about **visibility** — "show this specific private field in the Inspector." `[Serializable]`, put on a whole plain C# class (one that is *not* a `MonoBehaviour` or `ScriptableObject`), answers a different question: "Unity's serializer is allowed to look inside this class at all." Without it, a field of that class's type would just show up blank/uneditable in the Inspector — Unity wouldn't know how to break it down into something it can save and display.

This matters here because `Stat` isn't a component or an asset by itself — it's a small reusable "value plus its modifiers" building block that gets *nested* as a field inside other serializable things (a `Stat_OffenseGroup`, which is itself nested inside `Entity_Stats`, an actual `MonoBehaviour`). `[Serializable]` is what makes that whole chain of nesting actually show up, editable, in the Inspector — each layer needs the attribute for Unity to serialize through it into the next. You'll see this same attribute on several small "data bundle" classes throughout the project (`AttackData`, `DamageScaleData`, `ElementalEffectData`, and the `Stat_*Group` containers) for exactly this reason — none of them are components or ScriptableObjects, they're just structured data meant to be nested inside something that is.

## Auto-properties, and `{ get; private set; }`

```csharp
public bool isDead { get; private set; }
```

This is a shorthand for "a property with an invisible backing field." Read it as: *anyone* can read `isDead` from outside the class, but only code *inside this class* can set it. This pattern shows up constantly — it's how a class exposes state to the rest of the game without letting outside code corrupt that state directly. Compare to a plain `public bool isDead;` field, which anyone could set from anywhere — the property version is safer because only the class that owns the truth about `isDead` can change it.

## Interfaces

```csharp
public interface IDamagable
{
    public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer);
}
```

An interface is a **contract with no implementation** — it says "any class that implements me *must* provide this method," but doesn't say how. This is different from inheriting a base class: a class can only inherit from *one* base class, but it can implement *as many interfaces as it wants*. It's also different in intent — inheritance means "IS-A, shares behavior," an interface means "CAN-DO-THIS, no shared code required."

Why this matters here: `Entity_Health` and `Object_Chest` are completely unrelated classes (one is a living creature's health, one is a treasure chest) but they **both** implement `IDamagable`. Combat code (`Entity_Combat.PreformAttack()`) doesn't need to know or care which one it hit — it just checks "does this thing implement `IDamagable`? If so, call `TakeDamage` on it." This is the core reason interfaces exist: they let you write code that works against *any* type that fulfills a contract, without hard-coding a list of "if it's a Player, do X, if it's a Chest, do Y."

## Abstract classes

```csharp
public abstract class EntityState { ... }
```

`abstract` means "this class can never be instantiated directly — it only exists to be inherited from." It's a middle ground between an interface (no implementation at all) and a normal class (fully usable on its own): an abstract class *can* contain real, shared code (like `EntityState.Update()` ticking `stateTimer`), while still forcing subclasses to fill in the pieces that don't make sense to share (each state's own `Enter`/`Update`/`Exit` behavior, via `virtual` methods subclasses override).

## Inheritance & `virtual`/`override`

```csharp
public class Player : Entity { ... }
protected virtual void Awake() { ... }   // in Entity
protected override void Awake() { ... }  // in Player, likely calls base.Awake()
```

`Player : Entity` means "a Player *is an* Entity, plus extra stuff." It automatically gets every field and method `Entity` defines. Marking a method `virtual` in the base class means "subclasses are allowed to replace this method's behavior" via `override`. Inside an override, calling `base.Awake()` runs the *original* base-class version first (so you extend behavior instead of losing it entirely). This is how `Entity`'s shared collision/state-machine plumbing is reused by both `Player` and `Enemy` without copy-pasting it into each.

## Events & delegates (`event Action`)

```csharp
public event Action OnHealthUpdate;
...
OnHealthUpdate?.Invoke();
```

An `event` is a list of "please call me when this happens" subscribers. `Action` is a built-in delegate type meaning "a method that takes no parameters and returns nothing" (there's also `Action<float>` for a method taking one `float` parameter, etc. — the type in the angle brackets matches the parameter types).

Any other script can subscribe with `someObject.OnHealthUpdate += MyMethod;`, and when the owning script calls `OnHealthUpdate?.Invoke()`, every subscribed method runs. The `?.` before `Invoke` matters: if *nobody* has subscribed yet, the event is `null`, and calling `.Invoke()` on `null` directly would crash — `?.` says "only invoke if this isn't null."

This is this project's whole strategy for **cross-system communication** (see CLAUDE.md) — instead of `Entity_Health` needing to know about the UI health bar directly, it just shouts "my health changed!" into the void via an event, and whatever cares (a health bar, in this case) subscribes and reacts. This keeps systems decoupled: `Entity_Health` would work fine even with zero listeners.

## Enums

```csharp
public enum ElementType { None, Fire, Ice, Lightning }
```

An enum is a fixed, named set of options — safer and more readable than passing around raw numbers or strings (`element == 2` tells you nothing; `element == ElementType.Fire` does). Under the hood each entry is just an integer (`None = 0, Fire = 1, ...`), but you almost never need to think about that. Enums are used throughout for anything that's "pick one of a known, small set of categories" — element types, item types, stat types, and so on.

## The Finite State Machine (FSM) pattern

A **state machine** models something that's always in exactly *one* of several named "states" at a time, with rules for what triggers a switch between them. A character can be Idle, Moving, Jumping, Attacking, or Dead — never two of those at once, and never something outside that list.

The reason this pattern exists (instead of one giant `Update()` full of `if isJumping ... else if isAttacking ...`) is that it scales. Each state is its **own small class** that only has to know about its own behavior: what happens the moment you *enter* this state, what happens every frame *while* you're in it, and what happens the moment you *leave* it. Adding a new state (say, a new attack) means writing one new small class, not editing a sprawling `if/else` chain that every other state also lives inside. See `01-Foundations.md` for the actual engine (`StateMachine`, `EntityState`) this project builds on top of.

## ScriptableObjects

A `ScriptableObject` is a Unity class for **data that lives as an asset file**, not attached to any GameObject in a scene. Instead of hard-coding "a Fire Sword does 12 damage" directly into a script, you make a `ScriptableObject` class describing the *shape* of an item (name, damage, icon, rarity...), then create individual `.asset` files from it in the Project window — one per actual item — and fill in their fields in the Inspector like a form.

Why bother: designers (or you, in "designer hat" mode) can create/balance dozens of items, skills, or stat presets without touching code, and the same asset can be referenced from multiple places in the project without duplicating data. This project mirrors every `Scripts/Data/*.cs` ScriptableObject *class* with actual `.asset` *instances* under `Assets/Data/` — e.g. `Item_DataSO.cs` defines the shape, `Assets/Data/Item Data/...` holds the actual items built from it.

## Singletons (`Instance`)

```csharp
public static GameManager Instance { get; private set; }
```

A singleton is a class designed to have exactly **one** live instance, reachable from anywhere via a static property (`GameManager.Instance`) instead of needing a direct reference passed around. This is convenient for true "there's only ever one of these" systems (a game manager, an audio manager).

Worth knowing early: this project's singletons are **not all built the same way**. `GameManager.Instance` actively guards against duplicates and survives scene loads (`DontDestroyOnLoad`). Others (`SaveManager.Instance`, `Player.Instance`) are just a plain static field set in `Awake()`, with no duplicate protection — if a scene reload ever created two of that object, the second `Awake()` would silently overwrite the reference to the first. Not a bug you need to fix, just something to keep in mind if you ever see a singleton behaving oddly after a scene change.

## Coroutines (`IEnumerator`, `yield return`, `StartCoroutine`)

```csharp
private IEnumerator BurnEffectCo(float duration, float totalDamage)
{
    for (int i = 0; i < tickCount; i++)
    {
        entity_Health.ReduceHealth(damagePerTick);
        yield return new WaitForSeconds(tickInterval);
    }
}
```

A normal method runs start-to-finish in one frame and can't "pause." A coroutine is a method that **can pause itself and resume later**, which is exactly what you need for "do something, wait, do something else" gameplay logic (a damage-over-time tick, a knockback that lasts 0.2 seconds, a flash effect that reverts after a delay).

- The method returns `IEnumerator` and uses `yield return ...` instead of `return ...`.
- `yield return new WaitForSeconds(1f)` means "pause here, and come back to this exact line after 1 second."
- `yield return null` means "pause for exactly one frame."
- You never call a coroutine method directly like a normal function — you hand it to Unity with `StartCoroutine(MyMethodCo())`, and Unity manages resuming it over time.
- `StopCoroutine(handle)` / `StopAllCoroutines()` cancels one early — used a lot here so a *new* status effect can cut off an *old* one instead of both running simultaneously.

Naming convention you'll notice: coroutine methods in this codebase are suffixed `Co` (`KnockbackCo`, `BurnEffectCo`, `ShockEffectCo`) purely so it's obvious at a glance which methods are coroutines.

## `??=` (null-coalescing assignment)

```csharp
knockbackCoroutine ??= StartCoroutine(KnockbackCo(knockback, duration));
```

Shorthand for "if `knockbackCoroutine` is currently `null`, assign it; otherwise leave it alone." Used here so that if a knockback coroutine is already running, a second knockback call doesn't start a *second* overlapping one.

## Null-conditional operator (`?.`)

```csharp
entityVFX?.PlayOnDamageVFX();
```

Means "call `PlayOnDamageVFX()` only if `entityVFX` isn't `null`; otherwise, do nothing (and don't crash)." Used defensively whenever a component reference *might* not exist on a given GameObject (e.g. an enemy without a VFX component) but calling its method anyway shouldn't break the game.

## Raycasts & overlap checks (`Physics2D`)

```csharp
groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
```

A **raycast** fires an invisible line from a point in a direction and reports whether it hit something on a given `LayerMask` (a filter for "only care about objects tagged with these physics layers"). This is how the game checks "is there ground beneath my feet?" or "is there a wall in front of me?" without any actual collision happening — it's a *query*, not a physical interaction.

```csharp
Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
```

An **overlap check** instead asks "what's inside this shape right now?" — used for melee hit detection (everything inside a circle in front of the attacker, on the "hittable" layer, gets hit).

Both patterns rely on empty child GameObjects placed at specific spots (a `groundCheck` transform at the character's feet, a `targetCheck` transform in front of them) purely to mark *where* the check originates — they have no visual or component of their own, they're just position markers. `OnDrawGizmos` visualizes these in the Scene view since otherwise they're invisible.

## `LayerMask`

A `LayerMask` is a filter used by physics queries (raycasts, overlap checks) so they only "see" objects on certain Unity physics layers (e.g. a "Ground" layer, an "Enemy" layer) — set up in the Editor, referenced in code as `[SerializeField] LayerMask whatIsGround;`. It's what makes a raycast for "is there ground below me" ignore the player's own collider, other enemies, pickups, etc.

## Generic collections (`List<T>`) and LINQ

```csharp
List<Item_DataSO> possibleDrops = new List<Item_DataSO>();
possibleDrops = possibleDrops.OrderByDescending(item => item.itemRarity).ToList();
```

`List<T>` is a resizable array — a `List<Item_DataSO>` holds any number of `Item_DataSO` items, unlike a fixed-size array. LINQ (`System.Linq`) adds query-style methods on top of collections — `.OrderByDescending(x => x.someValue)` sorts by a value you specify without writing a manual sort loop, `.ToList()` converts the result back into a `List<T>`. You'll see this style ("filter, then sort, then take some") in a few places doing data processing rather than frame-by-frame gameplay logic.

## `[ContextMenu]`

```csharp
[ContextMenu("Update Default Stat Setup")]
public void ApplyDefaultStatSetup() { ... }
```

Adds a menu item to a component's right-click (⋮) menu in the Inspector, letting you trigger that method by hand from the Editor without writing a custom Editor script or pressing Play. Handy for one-off setup/debug actions like "reset this Entity's stats to its default `ScriptableObject` values."

## The new Input System's generated wrapper (`PlayerInputSet`)

```csharp
input = new PlayerInputSet();
...
if (input.Player.Jump.WasPressedThisFrame()) { ... }
input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
```

`Assets/InputSystem/PlayerInputSet.inputactions` is an asset you edit visually in the Editor (not by hand) — it defines named "actions" (Jump, Attack, Dash, Movement, Mouse...) and which physical keys/buttons/mouse movements they map to. Unity generates a C# class (`PlayerInputSet.cs`) from that asset, with one property per action, grouped under an "action map" (here, everything lives under `input.Player.*`). You never edit the generated file directly — you edit the `.inputactions` asset and Unity regenerates the wrapper.

Two different ways this project reads an action, both valid for different needs:
- **Polling**, inside a state's `Update()`: `input.Player.Jump.WasPressedThisFrame()` — "was this action's button pressed on exactly this frame?" (there's also `.WasReleasedThisFrame()`, and implicitly "is it currently held" via other methods this codebase doesn't happen to use). This fits states cleanly, since states are already being ticked every frame by the state machine.
- **Subscribing**, once, in `Player.OnEnable()`: `input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();` — "call this lambda whenever the Movement action's value changes." `performed` fires when the action produces a value (e.g. a stick is pushed), `canceled` fires when it returns to neutral (stick released). `ctx.ReadValue<Vector2>()` reads the actual value out of the event — `Vector2` here because Movement is a 2D stick/WASD-style action; a button-only action like Jump wouldn't need `ReadValue` at all, its `performed` firing is the whole signal. This style fits continuous values (movement, mouse position) better than polling would.

`input.Enable()` / `input.Disable()` (called from `Player.OnEnable`/`OnDisable`, and also from `Player_DeadState.Enter()` to lock out all input on death) turn the entire action map on or off at once — while disabled, none of the `WasPressedThisFrame()` polls or `performed` events fire, regardless of what the physical keyboard/controller is doing.

## Structs, `default`, and why a "no hit" raycast isn't `null`

```csharp
RaycastHit2D hit = Physics2D.Raycast(...);
if (hit.collider == null) return default;
```

`RaycastHit2D` is a `struct`, not a `class`. The difference that matters here: a `class` variable can be `null` (it's a reference that might point to nothing), but a `struct` is a value type — a variable of struct type always contains *some* actual value, it can never itself be `null`. So a method that returns `RaycastHit2D` can't signal "no hit" by returning `null` the way a method returning a `class` type could.

Instead, `Physics2D.Raycast` always returns a real `RaycastHit2D`, and represents "nothing was hit" by returning one whose `.collider` field is `null` — the *fields inside* the struct can still be reference types and be null, even though the struct itself can't be. `default` (short for `default(RaycastHit2D)`) produces a struct with every field zeroed/nulled out, which is exactly this "nothing here" value — you'll see `return default;` used as the explicit "I'm returning an empty/no-hit result" idiom in a couple of places (e.g. `Enemy.PlayerDetection()`).

---

*New terms get added here as later documentation chunks are written — if a term you need isn't here yet, it likely belongs to a system that hasn't been documented yet (see `README.md` for what's covered so far).*
