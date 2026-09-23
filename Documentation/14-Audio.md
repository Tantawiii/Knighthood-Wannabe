# 14 — Audio: AudioManager, AudioRangeController, AudioDatabase_DataSO

## Overview

The last chapter — three files implementing music (with crossfading and shuffle), positional sound effects (with distance-based falloff), and the ScriptableObject database both are built on. Despite being added later than most other systems (per CLAUDE.md), this is a genuinely complete, well-thought-out piece of the project — worth reading as a real example of turning "play a sound" into something that sounds good, not just something that technically works.

---

## `AudioManager.cs` — the second properly-guarded singleton

```csharp
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```
Worth noting explicitly: this is the exact same duplicate-guarding pattern as `GameManager.Instance` (`05-GameManager-and-Scenes.md`) — the **second** (of only two) singletons in this entire codebase that actually protects itself, out of the many that don't (`Player.Instance`, `SaveManager.Instance`, `Object_Portal.Instance`, `UI.Instance`).

### Two `AudioSource`s, one for music, one for effects

```csharp
[SerializeField] private AudioSource bgmSource;
[SerializeField] private AudioSource sfxSource;
```
Kept deliberately separate: background music is long, looping (conceptually), and needs its own independent volume-fading; sound effects are short, frequent, and played via `PlayOneShot` (which layers clips on top of each other on the same source rather than replacing what's playing) — mixing the two onto one `AudioSource` would make fading music in/out also affect every sound effect's volume.

### `bgmShouldPlay` — a reconciled intent, not an imperative command

```csharp
private void Update()
{
    if (!bgmSource.isPlaying && bgmShouldPlay)
        if (!string.IsNullOrEmpty(currentMusicGroup)) SwitchMusic(currentMusicGroup);
    if (bgmSource.isPlaying && !bgmShouldPlay) StopBGM();
}
```
Worth understanding as a deliberate design choice, not an odd one: instead of "play this track" being a one-time imperative command, `bgmShouldPlay` is a **standing intent** that `Update()` continuously reconciles against reality, every frame. This is what makes **playlist-style behavior across multiple clips in one music group work for free** — when the currently-playing clip finishes and `bgmSource.isPlaying` becomes `false` while `bgmShouldPlay` is still `true`, `Update()` notices the mismatch and calls `SwitchMusic` again, which (per `SwitchMusicCo` below) picks a **fresh random clip** from the same group — so a "level music" group with several tracks naturally shuffles between them over time, without any dedicated playlist/queue logic anywhere in the code.

### `SwitchMusicCo` — crossfading and a shuffle-without-repeats

```csharp
private IEnumerator SwitchMusicCo(string musicGroup)
{
    AudioClipData newMusicData = audioDatabase?.GetAudioClipData(musicGroup);
    AudioClip newMusicClip = newMusicData?.GetRandomClip();
    ...
    if (newMusicData.clips.Count > 1)
        while (newMusicClip == lastMusicPlayed)
            newMusicClip = newMusicData?.GetRandomClip();

    if (bgmSource.isPlaying)
        yield return FadeVolumeCo(bgmSource, 0f, 1f);

    lastMusicPlayed = newMusicClip;
    bgmSource.clip = newMusicClip;
    bgmSource.volume = 0;
    bgmSource.Play();
    StartCoroutine(FadeVolumeCo(bgmSource, newMusicData.volume, 1f));
}
```
Two details worth calling out clearly:

1. **The re-roll loop only runs when there's more than one clip to choose from** (`newMusicData.clips.Count > 1`) — a small but important guard, since without it, a single-clip group would loop forever trying to pick something "different" from the only option that exists. With more than one clip, it keeps re-rolling until the pick differs from `lastMusicPlayed` — a simple, common "don't repeat the same shuffled track twice in a row" refinement.
2. **The fade-out is awaited; the fade-in isn't.** `yield return FadeVolumeCo(...)` blocks this coroutine until the *old* track has fully faded to silence before the new clip is even assigned — deliberately avoiding an audible overlap/clash between old and new music. But the fade-*in* (`StartCoroutine(FadeVolumeCo(...))`, no `yield`) is fire-and-forget — there's nothing further in this coroutine that needs to wait for it to finish. The asymmetry is intentional: silence must come *before* the swap, but nothing needs to happen *after* the new track starts fading in.

`FadeVolumeCo` itself is the same `Time.deltaTime`-accumulated, elapsed-over-duration `Lerp` shape already seen in `UI_FadeScreen.FadeEffectCo` (`12-UI.md`) — worth explicitly recognizing this as **the standard "smoothly transition to a target over a known duration" coroutine template** used throughout this codebase, now confirmed a third time across three very different subsystems: screen fade, elemental status VFX in `02-Entity.md`, and now audio fade.

### `PlaySFX` vs. `PlayGlobalSFX` — two different `AudioSource` strategies for two different needs

```csharp
public void PlaySFX(string soundName, AudioSource sfxSource, float minDistanceToHearSound = 5.0f)
```
Worth noticing immediately: this method takes an **externally-supplied** `AudioSource` parameter — it deliberately shadows/ignores `AudioManager`'s own private `sfxSource` field. This is exactly what `Entity_SFX.PlayAttackHitSFX`/`PlayAttackMissSFX` (`02-Entity.md`) call, passing *their own* `AudioSource` (fetched via `GetComponentInChildren<AudioSource>()` on that specific entity). The reason: a positional sound needs to actually originate from the *correct world position* — each entity carries its own `AudioSource` component sitting at that entity's location, so `AudioManager` plays the clip *through* that source rather than through one shared source that couldn't represent every possible sound-emitting position at once.

```csharp
float distance = Vector2.Distance(sfxSource.transform.position, player.position);
float t = Mathf.Clamp01(1 - (distance / minDistanceToHearSound));
sfxSource.volume = Mathf.Lerp(0, maxVolume, t * t);
```
Worth explaining the falloff curve precisely: `t` starts at `1` when the listener is right on top of the sound and decreases linearly to `0` at `minDistanceToHearSound` (completely inaudible beyond that range) — but squaring it (`t * t`) before feeding it into the volume `Lerp` produces an **exponential**, not linear, falloff curve. This is the same underlying reasoning as the `Log10` volume-slider conversion in `UI_Options` (`12-UI.md`): human perception of loudness isn't linear, and a straight linear distance-to-volume falloff tends to sound like it cuts off abruptly right near the edge of its range rather than fading away naturally — squaring the interpolation factor is a cheap, common trick for a perceptually smoother falloff without doing actual logarithmic math.

`PlayGlobalSFX(soundName)` is the simpler counterpart — no distance calculation, no position, always plays at the clip's configured volume through `AudioManager`'s own dedicated `sfxSource` — used for sounds with no meaningful world position at all, like `UI_MainMenu.PlayGame()`'s button-click SFX (`12-UI.md`).

---

## `AudioRangeController.cs` — a case where per-frame `Update()` polling *is* the right tool

```csharp
private void Update()
{
    if (player == null) return;
    float distance = Vector2.Distance(player.position, transform.position);
    float t = Mathf.Clamp01(1 - (distance / minDistanceToHearSound));
    float targetVolume = Mathf.Lerp(0, maxVolume, t * t);
    source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * 3f);
}
```
Presumably attached to ambient/environmental sound sources placed around a level (a torch, a waterfall) that continuously need their volume adjusted based on the player's live distance — **exactly the same** `t * t` exponential falloff formula as `AudioManager.PlaySFX` above, worth flagging honestly as a second, independent copy of the same small formula rather than a shared utility. The same kind of small, spottable duplication already noted a few times across this documentation (`UI_StatSlot`'s `GetStatType`/`IsPercentageStat` duplicating `Inventory_Item`'s in `12-UI.md`; `Object_Merchant`/`Object_Blacksmith`'s `EnsurePlayerInventory` in `10-InteractiveObjects.md`).

Worth deliberately contrasting this against `UI_MiniHealthBar` (`12-UI.md`), which specifically **replaced** a per-frame `Update()` with an event-driven correction for performance reasons, with the developer's own comment praising that change. `AudioRangeController` does the opposite — continuous `Update()` polling — and that's the *correct* call here, not a missed optimization: the player's distance to a fixed sound source is a value that's **constantly changing** as the player moves around, with no natural discrete "event" to hook into instead. Recognizing *when* per-frame polling is the right tool versus when it's wasteful (as it was for `UI_MiniHealthBar`'s rare, event-worthy rotation correction) is a real judgment call, not a blanket rule — this file and that one are a genuinely useful side-by-side of both answers.

### A second, distinct smoothing technique worth naming

```csharp
source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * 3f);
```
This is a **different** `Lerp` pattern from the "coroutine, elapsed time over a known duration" shape used everywhere else in this codebase (`UI_FadeScreen`, `AudioManager.FadeVolumeCo`, `Entity_VFX`'s status tinting). Here, every single frame, the *current* volume is nudged partway toward a *continuously-changing* target, using a fixed rate (`3f`) multiplied by `Time.deltaTime` as the interpolation factor — no coroutine, no fixed total duration, because there isn't one: the target itself keeps moving as the player moves. This "ease toward a moving target, frame by frame" technique and the "smoothly reach a fixed target over N seconds" coroutine technique solve genuinely different problems and are both worth keeping as separate, distinct tools — reaching for the wrong one (e.g. trying to coroutine-fade toward a target that keeps changing) would be awkward and wrong.

---

## `AudioDatabase_DataSO.cs` — organized for humans, flattened for the game

```csharp
public List<AudioClipData> player;
public List<AudioClipData> uiAudio;
public List<AudioClipData> mainMenuMusic;
public List<AudioClipData> levelMusic;
public Dictionary<string, AudioClipData> clipCollection;

private void OnEnable()
{
    clipCollection = new Dictionary<string, AudioClipData>();
    AddToCollection(player);
    AddToCollection(uiAudio);
    AddToCollection(mainMenuMusic);
    AddToCollection(levelMusic);
}

public AudioClipData GetAudioClipData(string groupName) =>
    clipCollection.TryGetValue(groupName, out var data) ? data : null;
```
A nice, worth-naming technique: the asset is authored as **four separate, labeled lists** purely for human convenience in the Inspector — grouping related sounds (player SFX, UI SFX, two music categories) so whoever's populating the database isn't scrolling one giant undifferentiated list. At runtime, `OnEnable()` collapses all four into **one flat `Dictionary`**, keyed by each entry's own `audioName` string, giving every lookup (`GetAudioClipData`, called by every `AudioManager` method above) O(1) dictionary access regardless of which category a sound was originally declared under — the category split is purely an authoring-time organizational device with zero effect on runtime behavior.

Worth a small technical note on *why* `OnEnable` specifically: ScriptableObjects don't have the conventional `Awake`/`Start` lifecycle `MonoBehaviour`s do (see [Concepts-Glossary](Concepts-Glossary.md#monobehaviour--the-unity-lifecycle)), since they're not attached to a GameObject in a scene at all. `OnEnable` is the closest equivalent hook Unity provides for a ScriptableObject — firing whenever the asset becomes active/loaded — which is why it's used here for one-time setup rather than a lifecycle method that wouldn't actually run on this type.

```csharp
private void AddToCollection(List<AudioClipData> listToAdd)
{
    foreach (var data in listToAdd)
        if (data != null && !clipCollection.ContainsKey(data.audioName))
            clipCollection.Add(data.audioName, data);
}
```
Worth a brief, honest flag: this silently skips any entry whose `audioName` is already present in the dictionary — meaning if the same name were ever accidentally reused across two different category lists (or twice within one list), whichever gets processed **first** (`player`, then `uiAudio`, then `mainMenuMusic`, then `levelMusic` — the literal order `OnEnable` calls them in) silently wins, with no warning logged about the collision. A small, realistic footgun if two people working on the audio database ever picked the same name independently, though likely rare in practice.

### `AudioClipData` — why `clips` is a list, not a single clip

```csharp
[Serializable]
public class AudioClipData
{
    public string audioName;
    public List<AudioClip> clips = new List<AudioClip>();
    [Range(0f, 1f)] public float volume = 1f;

    public AudioClip GetRandomClip() => (clips.Count == 0 || clips == null) ? null : clips[UnityEngine.Random.Range(0, clips.Count)];
}
```
A single logical, named sound (`"attackHit"`, `"playlist_level1"`) can be backed by **multiple** actual audio clips — this is precisely what enables the music shuffle-without-repeats in `SwitchMusicCo` above, and what gives repeated sound effects (footsteps, hit impacts) natural variety without needing separate uniquely-named entries for every recorded take. `PlaySFX`'s own random pitch variation (`Random.Range(0.95f, 1.1f)`, `02-Entity.md`'s `Entity_SFX`) stacks on top of this clip variety for even more natural-feeling repetition.

One tiny, low-stakes precision note: the null-check order in `GetRandomClip()` (`clips.Count == 0 || clips == null`) checks `.Count` **before** checking for `null` — if `clips` genuinely were `null`, accessing `.Count` on it would already throw a `NullReferenceException` before the `|| clips == null` check could ever run. In practice this never actually triggers a problem, since the field's own initializer (`= new List<AudioClip>()`) means `clips` can never really be `null` to begin with — worth mentioning only in the same spirit as the other small, low-stakes precision notes made throughout this documentation set, not as something that actually causes issues here.

---

## How this connects elsewhere

- `Entity_SFX.PlayAttackHitSFX`/`PlayAttackMissSFX` (`02-Entity.md`) are the clearest real callers of `AudioManager.PlaySFX`.
- `LevelManager.Start()` and `UI_MainMenu.Start()` (`05-GameManager-and-Scenes.md`, `12-UI.md`) both call `AudioManager.Instance.StartBGM(...)`.
- `UI_Options` (`12-UI.md`) controls overall mix volume via `AudioMixer`, entirely independently of this per-clip system — one handles *which* sounds play and at what relative/distance-based loudness, the other handles the *overall* BGM/SFX volume sliders on top of whatever this system already decided to play.

## Where this pattern could be reused later

The **authored-as-labeled-lists, flattened-to-one-dictionary-at-load** technique in `AudioDatabase_DataSO` is a clean, reusable pattern for any data asset where human-friendly organization and runtime lookup efficiency pull in different directions — worth remembering as a general answer to "how do I keep this Inspector-friendly *and* fast to query" for any future lookup-table ScriptableObject. The **state-reconciliation `Update()` loop** in `AudioManager` (maintaining a standing "should this be happening" intent and continuously correcting reality toward it, rather than issuing one-shot imperative commands) is a genuinely useful technique worth keeping in your toolkit for any system that needs to gracefully handle "the thing I started finished on its own and I need to notice and react" — music playlists, particle systems that need re-triggering, anything with a natural end that should trigger a follow-up action.

---

This completes the documentation set. All fourteen chapters plus the glossary now cover every system in `Assets/Scripts/` — see `README.md` for the full index. Re-reading any chapter after finishing the course is likely to surface things that didn't click the first time; the cross-references between chapters (`Entity.onFlipped` → `UI_MiniHealthBar`; the `06-StatSystem.md` equipment prediction confirmed in `07-InventorySystem.md`; the recurring "enum in, object out" lookup shape appearing in five different systems) are worth following even after a first read-through, since a lot of this codebase's design only becomes visible once you can see the same idea show up more than once.
