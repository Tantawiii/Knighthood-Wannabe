# 09 — SaveSystem: SaveManager, GameData, FileDataHandler, SerializableDictionary

## Overview

Four files that turn `ISaveable` (`01-Foundations.md`) from an interface into an actual working save/load pipeline:

- **`SaveManager`** orchestrates *when* saving/loading happens and finds every `ISaveable` object via reflection.
- **`GameData`** is the plain data bag everything gets read from and written into.
- **`FileDataHandler`** is the part that actually touches the disk (JSON + a simple obfuscation cipher).
- **`SerializableDictionary`** is a small, widely-useful workaround for a genuine Unity limitation.

This doc is where `01-Foundations.md`'s description of the reflection-based discovery system, and `05-GameManager-and-Scenes.md`'s `dataLoaded` handshake, both get their concrete implementation shown in full.

---

## `SaveManager.cs` — the orchestrator

```csharp
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private FileDataHandler dataHandler;
    private GameData gameData;
    private List<ISaveable> allSaveables;
```
An unguarded singleton (see [Concepts-Glossary](Concepts-Glossary.md#singletons-instance)) — same caveat as `Player.Instance`, contrasted with the actually-guarded `GameManager.Instance` from `05-GameManager-and-Scenes.md`.

### `Start()` is a coroutine — a Unity feature worth knowing about

```csharp
IEnumerator Start()
{
    Debug.Log(Application.persistentDataPath);
    dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
    allSaveables = FindISaveables();

    yield return null; // Wait for one frame to ensure all saveables are initialized before loading data
    LoadGame();
}
```
Worth calling out explicitly: `Start()` here **returns `IEnumerator`**, which Unity specifically supports — any lifecycle method (`Start`, `Update`, etc.) written this way is automatically run *as* a coroutine, with no explicit `StartCoroutine()` call needed. This isn't obvious from the method signature alone if you haven't seen it before. The `yield return null;` — waiting exactly one frame before calling `LoadGame()` — is explained directly by the comment: it gives every *other* script's own `Awake()`/`Start()` a chance to finish setting itself up before this code starts pushing loaded save data out to all of them.

`Application.persistentDataPath` (logged here for visibility while developing) is a platform-specific, writable directory **outside** the project's own files — this is where the actual save JSON physically lives on whatever machine the game is running on (a different real path on Windows vs. Mac vs. a built player vs. the Editor).

### `SaveGame()` / `LoadGame()` — the reflection-driven round trip

```csharp
public void SaveGame()
{
    foreach (var saveable in allSaveables)
        saveable.SaveData(ref gameData);
    dataHandler.SaveData(gameData);
}

public void LoadGame()
{
    allSaveables ??= FindISaveables();
    gameData = dataHandler.LoadData();
    if (gameData == null)
    {
        Debug.Log("No data was found. A new game will be started.");
        gameData = new GameData();
    }
    foreach (var saveable in allSaveables)
        saveable.LoadData(gameData);
}
```
This is the exact mechanism `01-Foundations.md` describes: **every** `ISaveable` in `allSaveables` gets `SaveData(ref gameData)` called on it in turn, each one reading/writing its own slice of the *same* shared `gameData` object — which is why `SaveData` takes `GameData` by `ref` in the first place: every saveable writes directly into one shared instance rather than returning its own separate blob that would need merging.

`LoadGame()` does the mirror operation: load (or, if nothing was found on disk, start a fresh `new GameData()`) and then push that data out to every saveable via `LoadData(gameData)`. **This loop is what eventually calls `GameManager.LoadData(...)`** (`05-GameManager-and-Scenes.md`) — the exact call that flips `dataLoaded = true` and unblocks `ChangeSceneCo`'s wait loop. `allSaveables ??= FindISaveables();` only re-scans the scene if the list is currently `null` — `DeleteSaveData()` below deliberately relies on this by calling `LoadGame()` again without clearing `allSaveables` first, reusing the same already-found list of scene objects since only the *data* changed, not which objects exist.

### `FindISaveables()` — the reflection scan, in full

```csharp
private List<ISaveable> FindISaveables()
{
    return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .OfType<ISaveable>()
        .ToList();
}
```
This is the concrete code behind "no manual registry" from `01-Foundations.md`. Worth reading precisely:

- `FindObjectsByType<MonoBehaviour>` finds **literally every MonoBehaviour component currently in the loaded scene** — an intentionally broad (and moderately expensive) scan, but one that only runs once per scene load, not per frame.
- `FindObjectsInactive.Include` specifically means it also finds components on currently-**disabled** GameObjects, not just active ones — this matters, since a checkpoint or a hidden UI panel that happens to start inactive still needs to be found and hooked into the save system.
- `.OfType<ISaveable>()` (a LINQ filter — see [Concepts-Glossary](Concepts-Glossary.md#generic-collections-listt-and-linq)) then narrows that huge list down to just the components that also happen to implement `ISaveable`, discarding everything else.

Add a new saveable feature anywhere in the project, and as long as its component implements `ISaveable`, it's automatically found here — nothing else needs to change.

### Housekeeping

```csharp
private void OnApplicationQuit() => SaveGame();

[ContextMenu("Delete Save Data")]
public void DeleteSaveData()
{
    dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
    dataHandler.DeleteData();
    LoadGame();
}
```
`OnApplicationQuit` is a safety net beyond the save-on-scene-change behavior already covered in `05-GameManager-and-Scenes.md` — it catches the case where the player just quits the game entirely without any scene transition happening at all. `DeleteSaveData()` (see [Concepts-Glossary: `[ContextMenu]`](Concepts-Glossary.md#contextmenu)) deletes the file on disk *and* immediately calls `LoadGame()` again — since the file is now gone, `dataHandler.LoadData()` returns `null`, which triggers the "start fresh" fallback, meaning deleting the save resets the *current play session's* live state too, not just the file.

---

## `GameData.cs` — the plain data bag

Briefly introduced in `05-GameManager-and-Scenes.md`; the full shape (all fields `[Serializable]`, no methods beyond a constructor initializing every `SerializableDictionary`):
```csharp
public int gold;
public List<Inventory_Item> itemList;
public SerializableDictionary<string, int> inventory;        // itemSaveID => stacksize
public SerializableDictionary<string, int> storageItems;
public SerializableDictionary<string, int> storageMaterials;
public SerializableDictionary<string, ItemType> equippedItems; // itemSaveID => slotType
public int skillPoints;
public SerializableDictionary<string, bool> skillTreeUI;
public SerializableDictionary<SkillType, SkillUpgradeType> skillUpgrades;
public SerializableDictionary<string, bool> unlockedCheckpoints;
public SerializableDictionary<string, Vector3> inScenePortals;
public string portalDestinationSceneName;
public bool returningFromTown;
public string lastScenePlayed;
public Vector3 lastPlayerPosition;
```
Worth a quick, honest observation: `public List<Inventory_Item> itemList;` near the top is never actually read from or written to by any of the `SaveData`/`LoadData` code covered so far (`Inventory_Player`, `Inventory_Storage`, `07-InventorySystem.md`) — they all use the dictionary-based fields (`inventory`, `storageItems`, etc.) instead. It appears to be an unused leftover field from an earlier version of the save shape, not something currently wired up to anything.

The constructor's whole job is initializing every `SerializableDictionary` field to a fresh empty instance — necessary because, unlike a plain field, a `Dictionary`-based field left at its default (`null`) would cause a `NullReferenceException` the first time any code tried to read/write it, before a save file was ever loaded to populate it.

---

## `FileDataHandler.cs` — actually touching the disk

Not a `MonoBehaviour` — a plain C# class, constructed once by `SaveManager` with a directory, a filename, and an encryption flag.

### `SaveData`/`LoadData` — JSON, wrapped in `try`/`catch`

```csharp
public void SaveData(GameData gameData)
{
    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        string dataToSave = JsonUtility.ToJson(gameData, true);
        if (encrtyptData) dataToSave = EncryptDecrypt(dataToSave);

        using (FileStream stream = new FileStream(fullPath, FileMode.Create))
        using (StreamWriter writer = new StreamWriter(stream))
            writer.Write(dataToSave);
    }
    catch (Exception e)
    {
        Debug.LogError("Error occured when trying to save data to file: " + fullPath + "\n" + e);
    }
}
```
`JsonUtility.ToJson(gameData, true)` — the `true` requests pretty-printed (indented, human-readable) JSON rather than one dense line, at the cost of a slightly larger file — worth it here since it means you can actually open the save file in a text editor and read it while debugging. The `using` blocks are the C# language feature that guarantees a resource like a file handle gets properly closed the moment its block ends, even if an exception is thrown partway through. The whole thing is wrapped in `try`/`catch` — a failed save logs an error but doesn't crash the game, a reasonable defensive choice for I/O code that can fail for reasons outside the game's control (disk full, permissions, etc.).

`LoadData()` mirrors this exactly in reverse, with one extra step worth noting: it checks `File.Exists(fullPath)` **first** and returns `null` if there's no file at all — this is precisely the `null` that `SaveManager.LoadGame()` checks for to decide "no save exists yet, start a new game."

### `EncryptDecrypt` — obfuscation, not real security

```csharp
private string encryptionCodeWord = "UATAIAM";

private string EncryptDecrypt(string data)
{
    string modifiedData = "";
    for (int i = 0; i < data.Length; i++)
        modifiedData += (char)(data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]);
    return modifiedData;
}
```
This is a repeating-key **XOR cipher**: each character of the data is XOR'd against the corresponding character of a short repeating keyword (`i % encryptionCodeWord.Length` cycles the 7-character key over and over to match data of any length). The genuinely useful property being relied on here: **XOR is its own inverse** — XOR-ing the same value twice with the same key returns the original — which is exactly why one method, `EncryptDecrypt`, handles *both* directions; encrypting and decrypting are literally the same operation.

Worth being direct about what this actually provides: **this is obfuscation, not real security.** A fixed, short, repeating-key XOR cipher is straightforward to break — the save file's underlying JSON has very predictable structure (field names, punctuation), making a known-plaintext or frequency-based attack on the key trivial for anyone who actually wanted to. Its real purpose is much more modest: stop a curious or cheating player from casually opening the save file in a plain text editor and editing their gold or item counts by hand, not to protect against any serious or motivated attacker. It's worth internalizing the difference between "obfuscated" and "cryptographically secure" clearly, since the word "encryption" (used in this project's own variable/field names, and in CLAUDE.md) can otherwise imply a much stronger guarantee than this code actually provides.

---

## `SerializableDictionary.cs` — a genuinely useful, widely-known workaround

### The problem this solves

Unity's `JsonUtility` (used directly by `FileDataHandler` above) can only serialize a specific, limited set of shapes: primitives, `[Serializable]` classes/structs, arrays, and `List<T>` of serializable element types. A plain C# `Dictionary<TKey, TValue>` is **not** one of them — its internal implementation (hash buckets, etc.) isn't a simple field layout Unity's reflection-based serializer knows how to walk. Without a workaround, every one of `GameData`'s dictionary fields (`inventory`, `unlockedCheckpoints`, `inScenePortals`, and so on) simply couldn't be saved at all.

### The workaround

```csharp
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        this.Clear();
        if (keys.Count != values.Count)
            throw new Exception(...);
        for (int i = 0; i < keys.Count; i++)
            this.Add(keys[i], values[i]);
    }
}
```
Two things make this work together:

1. **It actually *inherits* from `Dictionary<TKey, TValue>`.** In code, everywhere this type is used (`data.unlockedCheckpoints.TryGetValue(...)`, `data.inScenePortals[currentSceneName] = ...`), it genuinely *is* a real dictionary — every normal dictionary method and the indexer work exactly as expected, because they're inherited, not reimplemented.
2. **It implements `ISerializationCallbackReceiver`**, a special Unity interface with exactly two hook methods that Unity's serializer calls automatically, at exactly the right moments: `OnBeforeSerialize()` right before Unity would try to write this object out, and `OnAfterDeserialize()` right after Unity has finished reading it back in.

The trick: alongside the (unserializable) dictionary data, this class keeps **two parallel `List<T>` fields** — `keys` and `values` — which Unity *can* serialize natively. `OnBeforeSerialize()` dumps the dictionary's current contents into those two lists (same order, `keys[i]` paired with `values[i]`) right before Unity serializes — so Unity ends up actually writing out the two lists to JSON, never touching the dictionary internals at all. `OnAfterDeserialize()` reverses it: once Unity has read the two lists back from JSON, clear the real dictionary and rebuild it by zipping the two lists back together pair by pair — with a sanity-check exception if the two lists ever ended up different lengths, a sign something went wrong and the data can't be trusted to reconstruct correctly.

This is a well-known pattern in the wider Unity community, not something unique to this project — genuinely worth remembering for any future Unity project, since "I need to save a Dictionary and `JsonUtility` won't let me" is a limitation you will hit again.

---

## How this connects elsewhere

- `ISaveable`, and the whole "no manual registry" idea, come from `01-Foundations.md` — this doc is where that description becomes real, runnable code.
- `GameManager`'s own `ISaveable` implementation and the `dataLoaded` handshake (`05-GameManager-and-Scenes.md`) are driven entirely by `SaveManager.LoadGame()`'s loop.
- `Inventory_Player`/`Inventory_Storage` (`07-InventorySystem.md`) are the heaviest users of `SerializableDictionary` and `GameData`'s item-related fields.
- `Object_Checkpoint` and `Object_Portal` (next doc, `10-InteractiveObjects.md`) both implement `ISaveable` and are discovered by this exact same reflection scan.

## Where this pattern could be reused later

`SerializableDictionary` is worth keeping as a ready-made utility for any future Unity project — the "inherit the real collection, then serialize a parallel representation via `ISerializationCallbackReceiver`" trick generalizes beyond dictionaries to any data shape `JsonUtility` doesn't natively support. The **reflection-based interface discovery** (`FindObjectsByType<T>().OfType<ISomeInterface>()`) is a broadly useful technique any time you want a system that "just works" for any object opting in via an interface, without a hand-maintained registry list anywhere. And the honest distinction drawn here between **obfuscation and real encryption** is worth carrying forward as a habit: naming something "encryption" doesn't make it secure, and it's worth being precise — in your own code's comments/naming, and in your own head — about which one you've actually built.

---

**Next:** `10-InteractiveObjects.md` — Checkpoint, Waypoint, Portal (fully, building on what was previewed here), NPC, Merchant, Blacksmith, Chest, ItemPickup, Buff.
