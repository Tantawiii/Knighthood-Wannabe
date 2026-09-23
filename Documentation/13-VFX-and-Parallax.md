# 13 — VFX and Parallax: Two Small, Independent Systems

## Overview

This chapter covers two components that have nothing to do with each other, grouped into one chapter because CLAUDE.md groups them together and they're both small:

- **`VFX_AutoController`** — attached to short-lived visual effect prefabs (hit sparks, explosions, status effects). Its only job is to make a one-shot effect behave sensibly on its own — scatter itself a little, optionally fade out, then delete itself — without every VFX prefab needing its own bespoke script.
- **`ParallaxBackground` / `ParallaxLayer`** — creates the classic "distant background scrolls slower than the foreground" depth illusion as the camera moves.

Neither system depends on anything else documented in this series, and nothing else depends on them beyond `Instantiate`-ing a prefab that happens to carry `VFX_AutoController`. Per CLAUDE.md, the parallax scripts react to `Camera.main` but never move it — Cinemachine owns the actual camera; this system only watches where the camera already is.

---

## `VFX_AutoController.cs` — the generic one-shot effect component

### What it's for

Picture every visual effect in the game — a sword-hit spark, an explosion, an ice-shatter — as a prefab that gets `Instantiate`d, plays for a moment, and then needs to disappear. Without a shared component, *every single one* of those prefabs would need its own script to handle "wait a bit, then delete yourself," "fade out first," and so on. `VFX_AutoController` is that shared script: attach it once to a VFX prefab, tick the checkboxes that particular effect needs in the Inspector, and it handles the rest — no per-effect code.

```csharp
public class VFX_AutoController : MonoBehaviour
{
    [SerializeField] bool autoDestroy = true;
    [SerializeField] float destroyDelay = 1;
    [SerializeField] bool randomOffset = true;
    [SerializeField] bool randomRotation = true;
    [SerializeField] bool canFade;
    [SerializeField] float fadeSpeed = 1f;
    ...
}
```

Four independent, toggleable behaviors, each controlled by its own checkbox:

| Behavior | Field | What it actually does |
|---|---|---|
| Scatter position | `randomOffset` | Nudges the effect's spawn position sideways/up-down by a small random amount |
| Scatter rotation | `randomRotation` | Spins the effect to a random angle on spawn |
| Fade out | `canFade` | Gradually reduces the sprite's alpha (opacity) down to 0 over time |
| Self-destruct | `autoDestroy` | Deletes the GameObject after a configurable delay |

### Functionality walkthrough

```csharp
void Awake()
{
    sr = GetComponentInChildren<SpriteRenderer>();
}

void Start()
{
    if (canFade) StartCoroutine(FadeCo());
    ApplyRandomOffset();
    ApplyRandomRotation();
    if (autoDestroy) Destroy(gameObject, destroyDelay);
}
```

- **`Awake()`** looks up the `SpriteRenderer` once — see [Concepts-Glossary: MonoBehaviour lifecycle](Concepts-Glossary.md#monobehaviour--the-unity-lifecycle) for why `Awake` is the right place for this kind of "find my own components" wiring. The result is stored in `sr` so nothing else in the script has to search for it again.
- **`Start()`** runs everything else, all as one-time setup on the frame the effect spawns: kick off the fade coroutine if this effect fades, scatter its position/rotation if it should, and schedule its own destruction if it should. Notice there is **no `Update()` method anywhere in this class** — that turns out to matter a lot; see "Why this is optimized" below.

```csharp
void ApplyRandomOffset()
{
    if (!randomOffset) return;
    float xOffset = Random.Range(xMinOffset, xMaxOffset);
    float yOffset = Random.Range(yMinOffset, yMaxOffset);
    transform.position += new Vector3(xOffset, yOffset);
}

void ApplyRandomRotation()
{
    if (!randomRotation) return;
    float zRotation = Random.Range(minRotation, maxRotation);
    transform.Rotate(0, 0, zRotation);
}
```

Both are pure one-time nudges: pick a random number inside a configurable range (`xMinOffset`/`xMaxOffset`, `yMinOffset`/`yMaxOffset`, `minRotation`/`maxRotation` — all `[SerializeField]`, tunable per-prefab in the Inspector), then apply it exactly once. The *reason* this exists: if ten identical hit-sparks spawn in the same melee, or ten arrows stick into the same wall, having every single one render at the *exact* same position and angle reads as obviously copy-pasted. A small random scatter is a cheap trick that makes it look natural instead.

```csharp
IEnumerator FadeCo()
{
    Color targetColor = Color.white;
    while (targetColor.a > 0)
    {
        targetColor.a -= (Time.deltaTime * fadeSpeed);
        sr.color = targetColor;
        yield return null;
    }
    sr.color = targetColor;
}
```

This is the standard "count something down every frame until it hits zero" coroutine shape (see [Concepts-Glossary: Coroutines](Concepts-Glossary.md#coroutines-ienumerator-yield-return-startcoroutine)). Each frame it shaves `Time.deltaTime * fadeSpeed` off the sprite's alpha and writes the new color back. Using `Time.deltaTime` (the time elapsed since the last frame) instead of a flat per-frame number means the fade takes the same amount of *real* time regardless of frame rate — a `fadeSpeed` of `1` fades out in roughly 1 second; `2` fades twice as fast, in roughly half a second.

```csharp
if (autoDestroy) Destroy(gameObject, destroyDelay);
```

`Destroy` normally deletes a GameObject the instant it's called. This lesser-known overload takes a delay in seconds and lets *Unity itself* run the countdown internally — no coroutine, no `Invoke`, no extra method needed for the common "just disappear after N seconds" case.

### A real gotcha worth knowing

**The fade and the auto-destroy timer are not connected to each other at all.** `fadeSpeed` controls how long the visual fade takes (roughly `1 / fadeSpeed` seconds); `destroyDelay` is a completely separate number. Nothing in the code keeps them in sync:

- If `destroyDelay` is shorter than the fade takes, the object gets deleted while still partially visible — a visible "pop."
- If `destroyDelay` is longer, the object sits fully invisible, but still present, for a stretch before it's actually removed.

Whoever configures a given VFX prefab's Inspector values is responsible for making these two numbers agree — the script itself doesn't enforce it.

### Why this is optimized

- **No `Update()` method at all.** Every behavior here is either applied once in `Start()` or driven by a coroutine that runs only while actively fading and stops itself the moment alpha hits `0`. Compare this to `UI_MiniHealthBar` in `12-UI.md`, whose *original* code had a similar-looking per-frame correction sitting in `Update()`, with the developer's own comment calling it out directly — *"Quite heavy load on the FPS, does it all the time even if not required"* — before it was replaced with an event-driven fix. `VFX_AutoController` never had that problem to begin with: once an effect is done fading (or if it never fades at all), it costs the engine nothing on a per-frame basis until `Destroy` eventually removes it.
- **`GetComponentInChildren<SpriteRenderer>()` runs exactly once, in `Awake()`** — not inside `FadeCo()`'s per-frame loop. A `GetComponent`-family call walks the GameObject's (and here, its children's) component list looking for a match; cheap as a one-off, but wasteful to repeat every single frame for a reference that never changes after the object spawns. Caching it in `sr` and reusing that same reference inside the fade loop turns "one lookup per frame, for as long as the fade lasts" into "one lookup, ever."
- **`Destroy(gameObject, destroyDelay)` avoids hand-rolling the delay.** Writing this the manual way would mean either a second coroutine (`yield return new WaitForSeconds(delay); Destroy(gameObject);` — another heap-allocated `IEnumerator` object) or an `Invoke("MethodName", delay)` call plus a matching method to invoke. The built-in delayed overload sidesteps both: one line, and the countdown is tracked by Unity's engine internally instead of by extra script state.
- **No object pooling — a deliberate, reasonable tradeoff at this project's current scale, not an oversight.** CLAUDE.md flags this as a known gap: every temporary effect in the codebase (`Entity_VFX.CreateOnHitVFX` in `02-Entity.md`, `SkillObject_Shard.Explode`'s VFX in `08-SkillSystem.md`, `ItemEffect_IceBlastOnTakingDamage`'s VFX in `11-Data-ScriptableObjects.md`) is a plain `Instantiate` of a prefab — almost certainly one carrying this component — rather than something pulled from and returned to a reusable pool. Each `Instantiate` allocates a new GameObject and its components on the managed heap; each `Destroy` marks that memory for later cleanup by Unity's garbage collector. At the game's current pace — a handful of hits and effects per second at most — that cost is small enough to be invisible. It would start to matter if a future skill spawned dozens of particles per second (an area-of-effect ability, a boss fight with constant impacts): frequent allocate-then-garbage-collect cycles like that are a classic source of visible frame-rate stutter in Unity, since a GC pass can briefly pause the whole game to clean up. Object pooling — pre-creating a fixed batch of VFX objects up front and recycling them instead of constantly creating and destroying new ones — is the standard fix *once that becomes a real, measured problem*. Building it now, before anything actually needs it, would just be extra complexity for no current benefit.

---

## `ParallaxBackground.cs` / `ParallaxLayer.cs` — scrolling background depth

### What it's for

This is the classic 2D "layers of background art drift past at different speeds" effect — mountains far away barely move, mid-ground hills move a bit more, near-ground bushes move almost with the camera. It's a purely visual depth illusion: the game world itself is flat, so this fakes depth by moving separate background images at different rates relative to the camera.

`ParallaxBackground` is the `MonoBehaviour` that lives in the scene and drives the effect every physics step. `ParallaxLayer` is a small, plain (non-`MonoBehaviour`) class — one instance per background image — that `ParallaxBackground` holds an array of. This is the same "small `[Serializable]` data-plus-behavior class living inside another component's array" shape as `StatModifier`/`Stat_OffenseGroup` in `06-StatSystem.md`.

### The core trick: move by a multiplier of camera movement

```csharp
public void Move(float distanceToMove) =>
    background.position += Vector3.right * (distanceToMove * parallaxMultiplier);
```

This single line **is** the entire parallax illusion. Every physics step, `ParallaxBackground` measures how far the camera just moved (`distanceToMove`) and hands that number to every layer. Each layer then moves itself by that same distance, scaled by its own `parallaxMultiplier`:

- `parallaxMultiplier = 1.0` → the layer moves exactly as far as the camera did, so it appears glued to the screen (a foreground element).
- `parallaxMultiplier = 0.0` → the layer never moves at all, so it appears infinitely far away.
- `parallaxMultiplier = 0.2` or `0.5` (typical for real background art) → the layer moves, but slower than the camera, so it visibly lags — the classic "far things drift past slower than near things" depth cue. There's no real depth here; it's one number per layer, tuned by eye in the Inspector.

### Setup: measuring the camera and each layer, once

```csharp
private void Awake()
{
    mainCam = Camera.main;
    cameraHalfWidth = mainCam.orthographicSize * mainCam.aspect;
    InitialzieLayers();
}
```

Worth knowing this camera math on its own — it's reusable knowledge outside this file too: for a 2D **orthographic** camera, Unity defines `orthographicSize` as the camera's **half-height** in world units (not the full height — a common trip-up). Multiplying by `aspect` (screen width ÷ height) converts that into the camera's **half-width**, which is exactly what's needed to know where the camera's visible left and right edges sit in world space — used later to detect when a background layer has scrolled off-screen.

```csharp
public void CalculateImageWidth()
{
    imageFullWidth = background.GetComponent<SpriteRenderer>().bounds.size.x;
    imageHalfWidth = imageFullWidth / 2;
}
```

Each `ParallaxLayer` measures its own background sprite's real rendered width, in world units, exactly once, when the scene starts (`InitialzieLayers()` loops over every layer and calls this). That width never changes again — the sprite doesn't resize at runtime — so it's calculated once here and reused forever instead of being recalculated on the fly.

### The infinite-scroll trick

```csharp
public void LoopBackground(float cameraLeftEdge, float cameraRightEdge)
{
    float imageRightEdge = (background.position.x + imageHalfWidth) - parallaxOffset;
    float imageLeftEdge = (background.position.x - imageHalfWidth) + parallaxOffset;
    if (imageRightEdge < cameraLeftEdge) background.position += Vector3.right * imageFullWidth;
    else if (imageLeftEdge > cameraRightEdge) background.position += Vector3.right * -imageFullWidth;
}
```

A single background image obviously isn't infinitely wide, so this is the trick that makes it *look* infinite: once a layer has scrolled entirely past one edge of the camera's view, silently teleport that same image by exactly one full image-width in the direction it needs to reappear. Since the camera can never see both the old position and the new position in the same frame, the jump is completely invisible to the player — it just looks like the background never runs out.

`parallaxOffset` is a small safety margin subtracted/added to the edge checks, so the teleport fires *slightly before* the image is fully out of view rather than exactly at the boundary — avoiding a single-frame gap where the image has just left the screen but hasn't been repositioned yet.

**Important detail about how this specific implementation works:** `background` is a **single** `Transform` per layer — `LoopBackground` teleports that *one* transform, rather than juggling two or three separate tile copies that leapfrog past each other (a different, also-common way to build infinite scrolling). That means this technique depends on the background art itself being wide enough, or seamlessly tileable at its edges, that jumping it by exactly its own width doesn't create a visible seam. That's a requirement on the art asset — the script neither checks nor enforces it.

### Driving it all from `FixedUpdate`

```csharp
private void FixedUpdate()
{
    float currentCameraPositionX = mainCam.transform.position.x;
    float distanceToMove = currentCameraPositionX - lastMainCamPosX;
    lastMainCamPosX = currentCameraPositionX;

    float cameraLeftEdge = currentCameraPositionX - cameraHalfWidth;
    float cameraRightEdge = currentCameraPositionX + cameraHalfWidth;

    foreach (ParallaxLayer layer in backgroundLayers)
    {
        layer.Move(distanceToMove);
        layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
    }
}
```

Every `FixedUpdate` (Unity's fixed physics timestep — 50 times per second by default), this reads the camera's current X position, subtracts the X position recorded at the *previous* `FixedUpdate`, and that difference is `distanceToMove` — how far the camera moved since the last check. That delta, not the camera's absolute position, is what gets handed to every layer. This is what lets each layer apply its own independent multiplier to the *same* underlying camera motion every physics step, instead of every layer needing to know the camera's absolute position and redo the same math from scratch.

### Why this is optimized

- **Each layer's measurements (`imageFullWidth`/`imageHalfWidth`) are cached once in `Awake`, not recomputed every frame.** The alternative — calling `background.GetComponent<SpriteRenderer>().bounds.size.x` directly inside `LoopBackground` — would mean a `GetComponent` lookup *and* a `bounds` recalculation (Unity has to work out the sprite's current world-space bounding box from its mesh and transform) for every single layer, on every single `FixedUpdate` — up to 50 times a second, per layer. Since none of that ever changes after the scene loads, doing it once and reusing two cached floats forever gives the exact same result for a tiny fraction of the work. This is the single biggest deliberate optimization in these two files.
- **`cameraHalfWidth` is cached once in `Awake`**, rather than recomputing `orthographicSize * aspect` on every `FixedUpdate`. It's a cheap multiply on its own, but there's no reason to redo it 50 times a second for a value that, under normal play, never changes. (Worth being honest about a real, if minor, limitation this creates: if the game window were resized or the camera's orthographic size changed *at runtime*, this cached value would go stale, and the edge-detection math would be slightly wrong until the scene reloaded — there's no code path here that recalculates it after `Awake`.)
- **The movement math is a single subtraction and a single multiply per layer — no trigonometry, no per-frame heap allocations.** `distanceToMove * parallaxMultiplier`, and the `Vector3.right * x` it feeds into, are all value-type (`struct`) math living on the stack — unlike, say, instantiating a new `GameObject` or growing a `List<T>`, none of this generates garbage for Unity's garbage collector to clean up later. That means this system carries zero GC-pause risk no matter how long the game runs.
- **Movement is derived from the camera's frame-to-frame delta, not its absolute position.** This keeps the per-layer math to a single subtraction instead of, say, every layer independently tracking and comparing full world positions against the camera's. It also makes the whole system agnostic to *why* the camera moved (the player walking, the camera snapping to a new area, a cutscene) — it only ever reacts to "the camera moved by this much," which is both simpler code and cheaper to compute than reasoning about movement causes.
- **Running from `FixedUpdate` at a fixed timestep, rather than `Update` at a variable frame rate**, ties the parallax motion to a steady, predictable interval instead of whatever the current frame rate happens to be. Per [Concepts-Glossary: MonoBehaviour lifecycle](Concepts-Glossary.md#monobehaviour--the-unity-lifecycle), this codebase mostly avoids driving logic directly from `FixedUpdate` outside of physics — this is a deliberate exception. Since Cinemachine, not this script, is what actually moves the camera, exactly how the two update loops line up in this scene's Cinemachine configuration isn't something confirmed by reading this `.cs` file alone — but reading the camera's position at a fixed, physics-synced interval is the standard way to keep parallax motion from looking subtly different across machines running at different frame rates.

---

## How this connects elsewhere

- `VFX_AutoController` is almost certainly attached to the VFX prefabs referenced throughout `02-Entity.md` (hit VFX, status VFX), `08-SkillSystem.md` (shard explosions, echo death effects), and `11-Data-ScriptableObjects.md` (the ice-blast effect) — though which specific prefabs actually carry it is Editor/prefab configuration, not something confirmed by reading the `.cs` files alone.
- The parallax system is purely visual and self-contained — nothing else documented in this series calls into it, and it doesn't call into anything else. It reacts only to `Camera.main`.
- Neither system was touched by the most recent commit (`572600a`, NPC interaction and quest save/load). That change only touched `Player_QuestManager`, `Object_NPC`, `Object_Blacksmith`/`Object_Merchant`, `GameData`, and `UI_Quest` — none of which reference VFX or parallax, so nothing in this chapter needed updating for it.

## Where this pattern could be reused later

- `VFX_AutoController`'s "one generic, config-driven component covering the common cases" shape is worth reaching for in any future project before writing a bespoke script per visual effect — most one-shot VFX needs really do reduce to some combination of "scatter," "fade," and "clean myself up after N seconds."
- The parallax system's core technique — deriving each layer's movement from the camera's *frame-to-frame delta*, applying a per-layer multiplier, and caching every measurement that doesn't change at runtime — is the standard, reusable way to build this effect in any 2D engine, not something specific to Unity or this project. That last part — "measure once in `Awake`, never again" — is arguably the more valuable lesson of the two files: it's the same instinct worth applying anywhere a value is being read off a component every single frame despite not actually changing every frame.

---

**Next:** `14-Audio.md` — `AudioManager`, `AudioRangeController`, and `AudioDatabase_DataSO`. The last chapter.
