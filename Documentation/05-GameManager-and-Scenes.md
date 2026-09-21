# 05 — GameManager: Scene Transitions and Respawn Resolution

## Overview

`GameManager` is the one true singleton in this codebase — the only class that actually guards against duplicates and survives scene reloads (see [Concepts-Glossary: Singletons](Concepts-Glossary.md#singletons-instance), and CLAUDE.md's note that every other `Instance` in the project is a plain unguarded static field). Its job: know which scene to load when, fade the screen during the transition, and figure out **where** to place the player once the new scene has finished loading — a surprisingly fiddly problem, since "where" depends on *why* the scene changed (walked through a portal? died and respawned? clicked Continue from the main menu?).

It also implements `ISaveable` (`01-Foundations.md`) itself — worth sitting with, because it means `GameManager` is one of the components `SaveManager` discovers via reflection, not a special case wired in separately. This doc also briefly touches `Object_Checkpoint` and `Object_Waypoint` (full detail in the later `InteractiveObjects` doc) since `GameManager`'s respawn logic leans directly on both.

---

## The singleton, done "correctly" this time

```csharp
public class GameManager : MonoBehaviour, ISaveable
{
    public static GameManager Instance;
    ...
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
```

This is the pattern the Concepts-Glossary singleton entry describes as the "actively guards against duplicates" version — worth contrasting directly against `Player.Instance`/`SaveManager.Instance`, which just assign `Instance = this` with no check at all. Here, if a second `GameManager` ever ends up in a newly-loaded scene (easy to imagine happening if one were accidentally left in a level scene rather than only existing in a persistent bootstrap scene), `Awake()` notices `Instance` is already set to a *different* object and immediately `Destroy`s the newcomer instead of overwriting the reference — so the original, `DontDestroyOnLoad`'d instance keeps running across every scene load for the whole session. Note also this is a plain `public static GameManager Instance;` field, not the `{ get; private set; }` property style used for `Player.Instance` — another small stylistic inconsistency, though the actual protection logic here is the important part, not the field style.

## `ChangeScene` — the public entry point

```csharp
public void ContinuePlay() => ChangeScene(lastScenePlayed, RespawnType.NoneSpecific);
public void RestartScene() => ChangeScene(SceneManager.GetActiveScene().name, RespawnType.NoneSpecific);

public void ChangeScene(string sceneName, RespawnType respawnType)
{
    SaveManager.Instance.SaveGame();
    StartCoroutine(ChangeSceneCo(sceneName, respawnType));
}
```
`ContinuePlay()` (presumably a main-menu "Continue" button) reloads whatever scene was last played; `RestartScene()` reloads the *current* scene (a likely candidate for a death-screen "restart" button, given `Player_Health.Die()` from `03-Player.md` opens a death screen UI). Both funnel into the same `ChangeScene`, which does something easy to miss the significance of: **every scene change triggers a full save first**, via `SaveManager.Instance.SaveGame()`, before the actual transition coroutine even starts. This is what makes it safe to, say, close the game mid-portal-transition without losing progress — the save already happened before the new scene started loading.

## `ChangeSceneCo` — the actual transition, step by step

```csharp
private IEnumerator ChangeSceneCo(string sceneName, RespawnType respawnType)
{
    UI_FadeScreen fadeScreenUI = FindFadeScreenUI();
    fadeScreenUI?.FadeOut();
    yield return fadeScreenUI?.fadeEffectCo;

    SceneManager.LoadScene(sceneName);

    dataLoaded = false;
    yield return null;
    while (dataLoaded == false) yield return null;

    fadeScreenUI = FindFadeScreenUI();
    fadeScreenUI?.FadeIn();

    Player player = Player.Instance;
    if (player == null) yield break;

    Vector3 respawnPosition = GetNewPlayerPostion(respawnType);
    if (respawnPosition != Vector3.zero)
        player.TeleportPlayer(respawnPosition);
}
```

Walking through this in order:

1. **Fade to black, and actually wait for it to finish.** `yield return fadeScreenUI?.fadeEffectCo` is worth pausing on: `fadeEffectCo` is itself a running `Coroutine` (started inside `UI_FadeScreen.FadeOut()`, a later UI doc's territory) — `yield return`ing a `Coroutine` object (rather than `null` or a `WaitForSeconds`) means "pause *this* coroutine until *that* coroutine finishes," letting one coroutine wait on another by reference instead of guessing how long the fade takes.
2. **`SceneManager.LoadScene(sceneName)`** — a synchronous, non-additive scene load: everything in the current scene that *isn't* `DontDestroyOnLoad`'d is torn down, and the named scene loads fresh in its place. `GameManager` itself survives this specifically because of the `DontDestroyOnLoad(gameObject)` call in its own `Awake()` above.
3. **The `dataLoaded` handshake.** This is the part worth understanding carefully, because it's a hand-built "wait until" using nothing but a shared bool: `dataLoaded` is reset to `false`, one frame is skipped (`yield return null`) to let the new scene's objects run their own `Awake()`/`Start()`, and then a `while (dataLoaded == false) yield return null;` loop just keeps waiting, one frame at a time, until something else sets `dataLoaded` back to `true`. That "something else" is `GameManager`'s own `LoadData(GameData data)` method below — because `GameManager` implements `ISaveable`, `SaveManager`'s reflection-based load pass (`01-Foundations.md`) calls `LoadData` on it automatically once the new scene's save data has been applied, and that method's last line is `dataLoaded = true;`. In other words: **this loop is waiting for the entire save/load system to finish restoring the new scene's state**, using the exact same `ISaveable` mechanism every other saveable object in the scene uses — `GameManager` isn't special-cased, it just happens to use its own `LoadData` call as a "the scene is ready" signal for itself.
4. **Fade back in**, then find the player. `if (player == null) yield break;` is a deliberate early exit — some scenes (the Main Menu, for instance) have no `Player` at all, and there's nothing to reposition in that case.
5. **Resolve and apply the respawn position** via `GetNewPlayerPostion(respawnType)` (typo preserved from the actual source — "Postion," not "Position," in case you go looking for it) — `Vector3.zero` is used as a sentinel meaning "no valid position was found," so the teleport is skipped entirely rather than snapping the player to the world origin by mistake.

## `FindFadeScreenUI` — fast path, then a fallback

```csharp
private UI_FadeScreen FindFadeScreenUI()
{
    if (UI.Instance != null) return UI.Instance.fadeUI;
    else return FindFirstObjectByType<UI_FadeScreen>();
}
```
A small but reusable pattern: try the cheap route first (a direct reference through the `UI` hub singleton, covered in a later doc), and only fall back to an actual scene search (`FindFirstObjectByType`, more expensive) if that hub isn't available yet — which can genuinely happen the very first frame or two after a scene load, before `UI`'s own `Awake()` has necessarily run and set its own `Instance`.

## `GetNewPlayerPostion` — the respawn-type dispatcher

```csharp
private Vector3 GetNewPlayerPostion(RespawnType type)
{
    if (type == RespawnType.Portal) { ... }
    if (type == RespawnType.NoneSpecific) { ... }
    return GetWaypointPosition(type);
}
```
Three branches, one per meaningfully different respawn rule (recall `RespawnType` from `01-Foundations.md`: `Enter`, `Exit`, `NoneSpecific`, `Portal`).

### `RespawnType.Portal`
```csharp
Object_Portal portal = Object_Portal.Instance;
Vector3 portalPosition = portal.GetPosition();
portal.SetCanBeTriggered(false);
portal.DisableIfNeeded();
return portalPosition;
```
Reaches through `Object_Portal`'s own (also unguarded) singleton to place the player at the portal's position, then immediately disables that portal from re-triggering — presumably to prevent an instant re-trigger loop the moment the player lands on/near it in the new scene. Full `Object_Portal` behavior is covered in the `InteractiveObjects` doc.

### `RespawnType.NoneSpecific` — "closest unlocked checkpoint or entry waypoint to where I died"
```csharp
var data = SaveManager.Instance.GetGameData();
var checkpoints = FindObjectsByType<Object_Checkpoint>(FindObjectsSortMode.None);
var unlockedCheckpoints = checkpoints
    .Where(cp => data.unlockedCheckpoints.TryGetValue(cp.GetCheckpointID(), out bool unlocked) && unlocked)
    .Select(cp => cp.GetPosition())
    .ToList();

var enterWaypoints = FindObjectsByType<Object_Waypoint>(FindObjectsSortMode.None)
    .Where(wp => wp.GetWaypointType() == RespawnType.Enter)
    .Select(wp => wp.GetPositionAndSetTriggerToFalse())
    .ToList();

var selectedPositions = unlockedCheckpoints.Concat(enterWaypoints).ToList();

if (selectedPositions.Count == 0) return Vector3.zero;

return selectedPositions
    .OrderBy(pos => Vector3.Distance(pos, lastPlayerPosition))
    .First();
```
This is the literal implementation of the rule CLAUDE.md describes as "picks the closest unlocked checkpoint/entry waypoint to the last death position" — worth reading as a five-step LINQ pipeline (see [Concepts-Glossary: LINQ](Concepts-Glossary.md#generic-collections-listt-and-linq)):
1. Find *every* checkpoint currently in the newly-loaded scene.
2. Keep only the ones this save file has actually unlocked — `data.unlockedCheckpoints.TryGetValue(id, out bool unlocked) && unlocked` is doing double duty: `TryGetValue` returns `false` (and leaves `unlocked` at its default `false`) for a checkpoint the player has never even reached, safely avoiding a crash that indexing the dictionary directly (`data.unlockedCheckpoints[id]`) would cause for a missing key — and the `&& unlocked` then also excludes any checkpoint that *does* have an entry but was somehow recorded as `false`.
3. Reduce the surviving checkpoints down to just their `Vector3` positions.
4. Do the same for every `Enter`-type waypoint in the scene (the "front door" of a level, typically) — note `GetPositionAndSetTriggerToFalse()` is a **side-effecting getter** (same idiom flagged in `04-Enemy.md`'s `Enemy.GetPlayerReference()`): calling it to read a position *also* disables that waypoint from immediately re-triggering the moment the player lands on it.
5. **Merge both lists** (`.Concat(...)`) into one combined pool of candidate respawn points, bail out with the `Vector3.zero` sentinel if that pool is empty, and otherwise sort the whole pool by distance to `lastPlayerPosition` (where the player last died — set in `SaveData` below) and take the single closest one.

### Anything else (`Enter`/`Exit`) — `GetWaypointPosition`
```csharp
private Vector3 GetWaypointPosition(RespawnType type)
{
    foreach (var waypoint in FindObjectsByType<Object_Waypoint>(FindObjectsSortMode.None))
        if (waypoint.GetWaypointType() == type)
            return waypoint.GetPositionAndSetTriggerToFalse();
    return Vector3.zero;
}
```
Used when a scene change specifies an exact `Enter` or `Exit` waypoint type directly (e.g. walking through a specific scene-edge trigger that should land you at a *specific* matching waypoint on the other side, not just "the nearest one") — no distance comparison here, just the first waypoint in the new scene matching that exact type.

## `GameManager`'s own `ISaveable` implementation

```csharp
public void LoadData(GameData data)
{
    lastScenePlayed = data.lastScenePlayed;
    lastPlayerPosition = data.lastPlayerPosition;
    if (string.IsNullOrEmpty(lastScenePlayed)) lastScenePlayed = "Level 0";
    dataLoaded = true;
}

public void SaveData(ref GameData data)
{
    string currentSceneName = SceneManager.GetActiveScene().name;
    if (currentSceneName == "MainMenu") return;

    data.lastPlayerPosition = Player.Instance.transform.position;
    data.lastScenePlayed = currentSceneName;
    dataLoaded = false;
}
```
`LoadData` is the exact method the `dataLoaded` wait-loop in `ChangeSceneCo` is waiting on, as described above — `"Level 0"` is hardcoded as the fallback starting scene for a brand-new save file that's never recorded a `lastScenePlayed` yet. `SaveData` explicitly refuses to overwrite the saved position/scene while sitting in the Main Menu (so quitting from the menu doesn't accidentally erase "where I actually was in the game") and resets `dataLoaded = false` at the end — priming it to be flipped back to `true` by the *next* `LoadData` call, keeping the handshake usable across repeated scene changes for the rest of the session.

---

## `LevelManager.cs` — the tiny one

```csharp
public class LevelManager : MonoBehaviour
{
    [SerializeField] private string musicGroupName;
    private void Start() => AudioManager.Instance.StartBGM(musicGroupName);
}
```
11 lines, one job: on scene start, tell the audio system (its own singleton, `AudioManager`, not yet covered in this documentation set) which named music group to start playing for this particular level. Presumably one `LevelManager` instance sits in each level scene with its own `musicGroupName` configured in the Inspector.

---

## A quick look at `Object_Checkpoint` / `Object_Waypoint` (full detail later)

Since `GameManager`'s respawn logic leans directly on both, it's worth knowing roughly what they are even ahead of their own full doc:

- **`Object_Checkpoint`** implements `ISaveable` itself: `OnTriggerEnter2D` activates it (`ActivateCheckpoint(true)`) the moment the player touches it, and its own `SaveData` only ever *adds* its ID to `data.unlockedCheckpoints` (never removes) — checkpoints are permanent once reached. `checkpointID` auto-generates a GUID in the Editor (`OnValidate`, wrapped in `#if UNITY_EDITOR`) the first time it's left blank, guaranteeing every checkpoint gets a unique, stable ID without a person having to type one in by hand.
- **`Object_Waypoint`** is the scene-edge/portal-pair mechanism: `waypointType` (`Enter`/`Exit`) plus a `connectedWaypoint` that `OnValidate` keeps automatically in sync (an `Enter` waypoint always points at `Exit` and vice versa), and touching one calls `GameManager.Instance.ChangeScene(transferToScene, connectedWaypoint)` directly — this is the actual trigger that starts the whole `ChangeSceneCo` flow described above from a walk-through-the-edge-of-the-level interaction.

Both files have some commented-out code from an earlier, simpler save/respawn approach (a single `savedCheckPoint` position rather than a whole unlocked-checkpoints dictionary) — consistent with the handful of other commented-out remnants already flagged in earlier docs (`Entity_DropManager`'s debug `Update`, `Player_Health.Die()`'s old restart-scene lines) as history, not something currently wired up.

---

## How this connects elsewhere

- `GameManager` is discovered and driven by `SaveManager`'s reflection-based `ISaveable` scan, exactly as described in `01-Foundations.md` — nothing registers it manually.
- `Player.TeleportPlayer(Vector3)` (`03-Player.md`) is the method that actually moves the player once a respawn position is resolved.
- `Object_Portal.Instance`, `Object_Checkpoint`, and `Object_Waypoint` get their full treatment in the later `InteractiveObjects` doc; this doc only covers what `GameManager` itself needs from them.
- `GameData` (briefly referenced here for `unlockedCheckpoints`/`lastPlayerPosition`/`lastScenePlayed`) and `SerializableDictionary` get their full treatment in the `SaveSystem` doc.

## Where this pattern could be reused later

The **shared-bool handshake** (`dataLoaded`) between an async-ish coroutine and the reflection-driven `ISaveable` load pass is a genuinely clever, reusable trick for "wait for an entire decoupled subsystem to finish, without that subsystem needing to know anything about who's waiting." The **cheap-reference-first, scene-search-fallback** shape in `FindFadeScreenUI` is worth reaching for any time you have a "usually available via a hub, but might not be wired up yet" reference. And the **LINQ filter → project → merge → sort → take-closest** pipeline in `GetNewPlayerPostion` is a clean, readable template for "pick the best candidate out of several different sources" problems in general — worth remembering over a more manual nested-loop-with-a-running-`closest`-variable approach (which, incidentally, `Player.TryInteract()` in `03-Player.md` *does* use for a similar "closest of several candidates" problem — a good side-by-side of the same problem solved two different ways in the same codebase).

---

**Next:** `06-StatSystem.md` — `Stat.cs`, `StatModifier`, and the four `Stat_*Group` container classes that `Entity_Stats` (`02-Entity.md`) is built from.
