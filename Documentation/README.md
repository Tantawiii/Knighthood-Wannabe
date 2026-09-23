# Knighthood Wannabe — Learn-the-Codebase Documentation

This folder is a beginner-friendly, human-written walkthrough of the actual code in this project (`Assets/Scripts/`) — what each class is for and *why* it was built that way, what each function does and why, and what programming/Unity concepts are in play so you can recognize and reuse them later. It's meant to be read alongside the real `.cs` files, not instead of them — each doc points at the actual code and explains it, rather than paraphrasing without context.

**Who this is for:** you followed (or are following) the course this project is based on, some parts clicked and some didn't, and you want a reference that explains the *why* behind the code once the course itself is done — not just "what line does what," but "why is it structured like this, and where else would I use this same idea." See `CLAUDE.md` at the repo root for the drier, purely-structural architecture overview these docs complement.

**This is separate from `Assets/Docs/`**, which is unrelated personal academic material (see `CLAUDE.md`) — nothing to do with this documentation set.

## How to use this

1. Read `Concepts-Glossary.md` first, or at least skim it — every numbered doc below links back into it instead of re-explaining basic terms (interfaces, coroutines, events, etc.) each time.
2. Read the numbered docs in order — later systems build on earlier ones (Player and Enemy both depend on Foundations and Entity, for instance), so reading out of order will mean more forward-references to things not covered yet.
3. Keep the actual `.cs` file open next to the doc for that system — line callouts reference real code, and reading them side by side is how this is meant to work.

## Reading order

| # | Doc | Covers | Status |
|---|-----|--------|--------|
| — | [Concepts-Glossary.md](Concepts-Glossary.md) | Cross-cutting terms: MonoBehaviour lifecycle, `[SerializeField]`, interfaces, events, FSM, ScriptableObjects, singletons, coroutines, raycasts, and more | ✅ Done |
| 01 | [01-Foundations.md](01-Foundations.md) | `Enums/`, `Interface/` (IDamagable, ICounterable, IInteractable, ISaveable), `StateMachine/` (StateMachine, EntityState, PlayerState, EnemyState) | ✅ Done |
| 02 | [02-Entity.md](02-Entity.md) | `Entity.cs` + its component family (`Entity_Health`, `Entity_Combat`, `Entity_Stats`, `Entity_StatusHandler`, `Entity_DropManager`, `Entity_VFX`, `Entity_SFX`, `Entity_AnimationTriggers`) | ✅ Done |
| 03 | [03-Player.md](03-Player.md) | `Player.cs` + `Player_*` components, then the 15 Player states built on `PlayerState` | ✅ Done |
| 04 | [04-Enemy.md](04-Enemy.md) | `Enemy.cs` + `Enemy_*` components, Enemy states, `Enemy_Skeleton` | ✅ Done |
| 05 | [05-GameManager-and-Scenes.md](05-GameManager-and-Scenes.md) | `GameManager.cs`, `LevelManager.cs`, scene transitions, respawn/checkpoint resolution | ✅ Done |
| 06 | [06-StatSystem.md](06-StatSystem.md) | `Stat.cs`, `StatModifier`, the `Stat_*Group` containers | ✅ Done |
| 07 | [07-InventorySystem.md](07-InventorySystem.md) | `Inventory_Base` → `Inventory_Player`/`Inventory_Merchant`/`Inventory_Storage`, equipment, crafting | ✅ Done |
| 08 | [08-SkillSystem.md](08-SkillSystem.md) | `Skill_Base` + per-skill classes, `SkillObject_*` runtime projectiles/effects | ✅ Done |
| 09 | [09-SaveSystem.md](09-SaveSystem.md) | `SaveManager`, `GameData`, `FileDataHandler`, `SerializableDictionary` | ✅ Done |
| 10 | [10-InteractiveObjects.md](10-InteractiveObjects.md) | Checkpoint, Waypoint, Portal, NPC, Merchant, Blacksmith, Chest, ItemPickup, Buff | ✅ Done |
| 11 | [11-Data-ScriptableObjects.md](11-Data-ScriptableObjects.md) | `Data/` ScriptableObject definitions: items, equipment, item effects, skills, stat setups, quests | ✅ Done |
| 12 | [12-UI.md](12-UI.md) | `UI.cs` hub + all sub-panel folders (Character, Craft, In Game, Merchant, Quest, Skill Tree, Storage, Tool Tips) | ✅ Done |
| 13 | [13-VFX-and-Parallax.md](13-VFX-and-Parallax.md) | `VFX_AutoController`, `ParallaxBackground`/`ParallaxLayer` | ✅ Done |
| 14 | [14-Audio.md](14-Audio.md) | `AudioManager.cs`, `AudioRangeController.cs`, `Data/AudioDatabase_DataSO.cs` | ✅ Done |

**The full set is done.** Every system in `Assets/Scripts/` (166 files) is covered by one of the 14 chapters above. New scripts added after this was written obviously won't be reflected here — treat this as a snapshot of the codebase as of when it was written, not a living doc that updates itself.

Each chapter ends with a short "where this pattern could be reused later" note. The idea: once the course is finished, this folder doubles as a personal reference for *your own* future projects, not just an explanation of this one. A few threads worth following across chapters rather than reading each in isolation:

- The **"enum in, matching object out via a lookup"** shape appears independently at least five times (`Entity_Stats.GetStatByType`, `Player_SkillManager.GetSkillByType`, `UI_InGame.GetSkillSlot`, and others) — once you spot it once, you'll recognize it everywhere.
- The **"coroutine, `Time.deltaTime`-accumulated Lerp over a known duration"** template shows up in screen fades, audio fades, and status-effect VFX — genuinely the standard "smooth transition" tool in this codebase.
- A handful of small duplications got flagged honestly as they were found (`EnsurePlayerInventory` in `10-InteractiveObjects.md`, the stat-name lookup tables in `07-InventorySystem.md`/`12-UI.md`, the distance-falloff formula in `14-Audio.md`) — worth treating as real examples of "this could have been shared" rather than assuming every repeated pattern here was deliberate.
- A few genuine gaps or loose ends were also flagged as found, not glossed over: the merchant's shop UI looking mid-transition in `10-InteractiveObjects.md`, `Object_Chest` never re-locking itself, `UI.alternativeInput` appearing to be dead wiring. None of these are "fix this" — they're "this is what the code actually does, which may differ from what you'd guess."
