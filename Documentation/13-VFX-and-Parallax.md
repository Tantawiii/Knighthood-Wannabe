# 13 — VFX and Parallax: Two Small, Independent Systems

## Overview

Three files, two unrelated jobs: `VFX_AutoController` is the generic "make any one-shot visual effect behave sensibly" component CLAUDE.md describes; `ParallaxBackground`/`ParallaxLayer` implement the classic scrolling-background depth illusion. Neither depends on anything else documented so far, and nothing else depends on them beyond calling `Instantiate` on a prefab that happens to carry one.

---

## `VFX_AutoController.cs` — the generic one-shot effect component

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

The idea: rather than every hit-spark, death-effect, or explosion prefab in the game needing its own bespoke "disappear after a bit" script, this one component bundles four independently-togglable behaviors that cover almost every one-shot VFX need. Attach it, tick the checkboxes a given effect wants, done — no code changes per effect.

```csharp
void Start()
{
    if (canFade) StartCoroutine(FadeCo());
    ApplyRandomOffset();
    ApplyRandomRotation();
    if (autoDestroy) Destroy(gameObject, destroyDelay);
}
```

- **`randomOffset`/`randomRotation`** — nudge the spawn position and/or rotation by a small random amount. The purpose is purely visual variety: several hit sparks landing in a cluttered melee, or several impact effects in a row, look noticeably more natural scattered slightly than if every single instance rendered at the exact identical spot and angle.
- **`canFade`** — `FadeCo()` decrements the sprite's alpha by `fadeSpeed` per second until it reaches `0`, using the standard `Time.deltaTime`-accumulated coroutine loop (see [Concepts-Glossary: Coroutines](Concepts-Glossary.md#coroutines-ienumerator-yield-return-startcoroutine)).
- **`autoDestroy`** — uses `Destroy(gameObject, destroyDelay)`, a lesser-known overload of Unity's `Destroy` that schedules destruction after a delay directly, without needing a coroutine or `Invoke` written by hand for the common "just disappear after N seconds" case.

Worth understanding as a real, honest detail rather than assuming it's perfectly coordinated: **the fade and the auto-destroy timer run independently**, on two separately-configured values (`fadeSpeed` controls how long the fade visually takes — roughly `1 / fadeSpeed` seconds to reach zero alpha — while `destroyDelay` is set separately). Nothing in the code ties them together — if `destroyDelay` is shorter than the fade takes, the object is destroyed still partway visible; if longer, it sits fully invisible for a stretch before actually being removed. Whoever configures a given VFX prefab's Inspector values is responsible for tuning the two to line up, not the script itself.

This is also the concrete mechanism behind CLAUDE.md's note that the project has **no object pooling** — every temporary effect across the whole codebase (`Entity_VFX.CreateOnHitVFX` in `02-Entity.md`, `SkillObject_Shard.Explode`'s VFX in `08-SkillSystem.md`, `ItemEffect_IceBlastOnTakingDamage`'s ice-blast/on-hit VFX in `11-Data-ScriptableObjects.md`, and others) is a plain `Instantiate` of a prefab that most likely carries this one shared component to handle its own cleanup, rather than being pulled from and returned to a reusable pool. A reasonable, low-complexity choice at this project's current scale — pooling would only start to matter once VFX spawn rates got heavy enough for the repeated `Instantiate`/`Destroy` calls to show up as a real performance cost.

---

## `ParallaxBackground.cs` / `ParallaxLayer.cs` — scrolling background depth

Per CLAUDE.md, these react to `Camera.main` but don't drive it — Cinemachine (`CLAUDE.md`'s tech stack) owns actual camera movement; this system only *follows* the camera to create a visual depth effect, never moves it.

### The parallax principle

```csharp
public void Move(float distanceToMove) =>
    background.position += Vector3.right * (distanceToMove * parallaxMultiplier);
```

This one line **is** the parallax illusion: `parallaxMultiplier` controls how much a layer moves relative to however far the camera just moved. A multiplier of `1.0` would move a layer exactly in lockstep with the camera (appearing fixed relative to the screen, like a foreground element); a multiplier of `0` would never move at all (appearing infinitely distant). Real background layers typically use small fractional multipliers (e.g. `0.2`, `0.5`) so they visibly lag behind the camera's motion — the classic depth cue where distant mountains appear to drift past slower than nearby trees, purely because they're assigned a smaller multiplier here.

### Setting up: measuring each layer once

```csharp
private void Awake()
{
    mainCam = Camera.main;
    cameraHalfWidth = mainCam.orthographicSize * mainCam.aspect;
    InitialzieLayers();
}
```

Worth knowing this bit of orthographic-camera math, since it's genuinely reusable knowledge: for a 2D orthographic camera, `orthographicSize` is (by Unity's own convention) the camera's **half-height** in world units — multiplying by `aspect` (width ÷ height) converts that into the camera's **half-width**, which is what's needed to know where the camera's visible left/right edges actually are in world space.

`ParallaxLayer.CalculateImageWidth()` measures each layer's actual rendered width once, up front (`background.GetComponent<SpriteRenderer>().bounds.size.x`), rather than recomputing it every frame — the same "measure once, cache the result" instinct established throughout this codebase's `Awake()` methods, here applied to a plain `[Serializable]` helper class (`ParallaxLayer` isn't itself a `MonoBehaviour` — it's the same "small serializable data-plus-behavior class nested in an array" shape as `Stat_OffenseGroup`/`StatModifier` from `06-StatSystem.md`).

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

The classic 2D "endless scrolling background" technique: once a layer's image has scrolled entirely past one edge of the camera's view, silently teleport it by exactly one full image-width in the direction it needs to reappear — since the camera can never see both the old and new position in the same frame, the jump is invisible to the player, creating the illusion of an endless background from a single image. `parallaxOffset` is a small buffer subtracted/added to each edge check, triggering the reposition slightly before the image is *fully* out of view rather than exactly at the boundary, avoiding any visible gap or pop.

Worth being precise about what this implementation actually does: `background` is a **single** `Transform` per `ParallaxLayer` — `LoopBackground` repositions that *same* transform by a full image-width, rather than managing two or more separate tile copies that leapfrog each other (a different, also-common implementation some parallax tutorials use). This means the technique here relies on the underlying background art being wide enough, or seamlessly tileable, that jumping it by exactly one of its own widths doesn't create a visible seam — a requirement on the art asset, not something this script enforces or checks itself.

### Driving it all from `FixedUpdate`

```csharp
private void FixedUpdate()
{
    float currentCameraPositionX = mainCam.transform.position.x;
    float distanceToMove = currentCameraPositionX - lastMainCamPosX;
    lastMainCamPosX = currentCameraPositionX;
    ...
    foreach (ParallaxLayer layer in backgroundLayers)
    {
        layer.Move(distanceToMove);
        layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
    }
}
```
Each layer's movement is derived purely from *how far the camera moved since the last check* (`currentCameraPositionX - lastMainCamPosX`), not the camera's absolute position — which is what allows each layer to apply its own independent multiplier to the same underlying camera motion every physics step.

---

## How this connects elsewhere

- `VFX_AutoController` is most likely attached to the VFX prefabs referenced throughout `02-Entity.md` (hit VFX, status VFX), `08-SkillSystem.md` (shard explosions, echo death effects), and `11-Data-ScriptableObjects.md` (the ice blast effect) — though which specific prefabs actually carry it is Editor/prefab configuration, not something confirmed by reading the `.cs` files alone.
- The Parallax system is purely visual and self-contained — it doesn't call into or get called by any other system documented in this set.

## Where this pattern could be reused later

`VFX_AutoController`'s "one generic, config-driven component covering the common cases" shape is worth reaching for in any future project before writing a bespoke script per visual effect — most one-shot VFX needs really do reduce to some combination of "fade," "jitter," and "clean myself up after N seconds." The parallax system's core technique — deriving each layer's movement from the camera's *frame-to-frame delta* rather than its absolute position, then applying a per-layer multiplier — is the standard, reusable way to build this effect in any 2D engine, not something specific to Unity or this project.

---

**Next:** `14-Audio.md` — `AudioManager`, `AudioRangeController`, and `AudioDatabase_DataSO`. The last chapter.
