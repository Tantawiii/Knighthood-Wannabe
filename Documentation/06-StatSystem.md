# 06 — StatSystem: `Stat`, `StatModifier`, and the Group Containers

## Overview

This is the smallest system documented so far (5 files, ~113 lines total) but one of the most-used — `Entity_Stats` (`02-Entity.md`) is built entirely out of these pieces, and `Player_Stats`' buff system (`03-Player.md`) works *only* because of how `Stat` tracks modifiers. If you've read either of those docs, you've already seen `GetValue()`, `AddModifier(...)`, and `SetBaseValue(...)` in action — this doc is where those actually live and get explained properly.

---

## `Stat.cs` — a number that remembers what changed it

```csharp
[Serializable]
public class Stat
{
    [SerializeField] float baseValue;
    [SerializeField] List<StatModifier> modifiers = new List<StatModifier>();

    bool needToRecalculate = true;
    float finalValue;

    public float GetValue()
    {
        if (needToRecalculate)
        {
            finalValue = GetFinalValue();
            needToRecalculate = false;
        }
        return finalValue;
    }
    ...
```

The core idea: a `Stat` is never *just* a number. It's a **base value** (from a default stat preset, or later, character progression) plus a **list of modifiers** (from equipment, buffs, whatever else temporarily or permanently changes it) — and `GetValue()` is the one method anything in the game ever calls to get the actual, current, fully-accounted-for number.

`[Serializable]` on the class (see [Concepts-Glossary](Concepts-Glossary.md#serializable-class-level-vs-serializefield-field-level)) is what lets `Stat` be nested as plain fields inside the `Stat_*Group` classes below, which are themselves nested inside `Entity_Stats` — three layers deep, all visible and editable in the Inspector because every layer is marked `[Serializable]`.

### The caching trick: `needToRecalculate`

```csharp
public void AddModifier(float value, string source)
{
    modifiers.Add(new StatModifier(value, source));
    needToRecalculate = true;
}

public void RemoveModifier(string source)
{
    modifiers.RemoveAll(modifier => modifier.source == source);
    needToRecalculate = true;
}

float GetFinalValue()
{
    float finalValue = baseValue;
    foreach (var modifier in modifiers) finalValue += modifier.value;
    return finalValue;
}
```

`GetValue()` is called *constantly* — every combat calculation in `Entity_Stats` (`02-Entity.md`) calls `.GetValue()` on several stats, often several times per attack, and UI stat displays (later doc) likely poll it too. Recomputing `GetFinalValue()` — looping the entire `modifiers` list and summing it — on *every single call* would be wasted work almost all the time, since a stat's modifiers don't actually change most frames.

This is a classic **dirty-flag / lazy caching** pattern: `needToRecalculate` starts `true`, and `GetValue()` only actually does the summing work the *first* time it's called after something changed. Every `GetValue()` call after that just returns the already-computed `finalValue` directly, until `AddModifier` or `RemoveModifier` flips `needToRecalculate` back to `true` — at which point the *next* `GetValue()` call (whenever that happens to be) recomputes once, then goes back to returning the cached number. The net effect: the actual summation only ever runs when the underlying data has genuinely changed, no matter how many times `GetValue()` itself gets called in between. This is a broadly useful technique worth remembering any time you have an expensive-ish computed value that's read far more often than its inputs change.

Note the model itself is intentionally simple: **every modifier is purely additive** — `GetFinalValue()` just sums `baseValue` plus every `modifier.value` in the list, one after another. There's no multiplicative/percentage-modifier concept and no special stacking rules here; if two different modifiers both apply to the same stat, they just add together plainly.

### `AddModifier`/`RemoveModifier` and the `source` string

Each `StatModifier` carries a `source` string — a label for *who* added it, not a description of *what* it does. `RemoveModifier(source)` uses `modifiers.RemoveAll(modifier => modifier.source == source)`, which removes **every** modifier tagged with that exact source in one call, not just one. This is precisely the mechanism `Player_Stats.ApplyBuff` (`03-Player.md`) depends on: it tags every stat modifier a single buff instance adds with that buff's own name as the `source`, so when the buff expires, one `RemoveModifier(buffName)` call per affected stat cleanly removes exactly and only what that buff added — regardless of how many separate `AddModifier` calls it took to apply the buff in the first place. The same mechanism will very likely turn out to be how equipping/unequipping gear applies and removes its own stat bonuses too, once the `InventorySystem` doc covers that — an item's own save ID would make a natural `source` tag for the same reason a buff's name does.

### `SetBaseValue`

```csharp
public void SetBaseValue(float value) => baseValue = value;
```
Sets the *unmodified* floor value directly, bypassing the modifier list entirely. This is what `Entity_Stats.ApplyDefaultStatSetup()` (`02-Entity.md`) calls once per stat to copy values out of a `Stat_SetupSO` preset asset. Note it doesn't itself flip `needToRecalculate` — wait, actually it doesn't need to: `SetBaseValue` changing `baseValue` *does* need a recalculation the next time `GetValue()` runs, but this method as written doesn't set `needToRecalculate = true`. In practice this is only ever called during setup (before `GetValue()` has necessarily been read yet, or where a stale first read is inconsequential), so it doesn't cause an observed problem — but it's worth knowing that, unlike `AddModifier`/`RemoveModifier`, `SetBaseValue` doesn't itself invalidate the cache the way you might expect it to.

## `StatModifier` — a small, deliberately dumb data holder

```csharp
[Serializable]
public class StatModifier
{
    public float value;
    public string source;

    public StatModifier(float value, string source)
    {
        this.value = value;
        this.source = source;
    }
}
```
No behavior at all — just two public fields and a constructor. This is what's sometimes called a **value object**: a small bundle of related data with no logic of its own, whose entire job is to be created, stored in a list, read, and eventually matched-and-removed by `source`. Both fields are `public` rather than properties since there's no invariant to protect here — a `StatModifier` is just "a number and a label," nothing about it needs guarding once constructed.

---

## The four `Stat_*Group` containers

```csharp
[Serializable]
public class Stat_MajorGroup
{
    public Stat strength;
    public Stat agility;
    public Stat intelligence;
    public Stat vitality;
}

[Serializable]
public class Stat_OffenseGroup
{
    public Stat attackSpeed;
    public Stat damage;
    public Stat critPower;
    public Stat critChance;
    public Stat armorReduction;
    public Stat fireDamage;
    public Stat iceDamage;
    public Stat lightningDamage;
}

[Serializable]
public class Stat_DefenseGroup
{
    public Stat armor;
    public Stat evasion;
    public Stat fireRes;
    public Stat iceRes;
    public Stat lightningRes;
}

[Serializable]
public class Stat_ResourceGroup
{
    public Stat maxHealth;
    public Stat healthRegen;
}
```

These four classes have **zero methods between them** — every one is purely a labeled bundle of `Stat` fields, grouped by the same rough categories `StatType` (`01-Foundations.md`) implies: Major (the four classic RPG primary attributes — Strength, Agility, Intelligence, Vitality), Offense (attack-related, including elemental damage stats), Defense (mitigation and resistances), Resource (health and its regen).

**Why bother with four small classes instead of just putting all ~19 `Stat` fields directly on `Entity_Stats`?** Purely for organization and readability — because each is `[Serializable]`, Unity's Inspector renders each group as its own labeled, collapsible section wherever `Entity_Stats` is shown, instead of one long undifferentiated list of 19 numeric fields. It's the same instinct behind the `[Header("...")]` attributes seen throughout other files, just achieved by grouping into actual separate types instead. None of these groupings change how any calculation works — `Entity_Stats.GetBaseDamage()` (`02-Entity.md`) still reaches straight through `offenseGroup.damage.GetValue()` — they exist entirely for the human reading/editing the Inspector, not for the code.

None of the four classes define a constructor — they rely on C#'s implicit default constructor, and Unity's serialization system is what actually initializes each contained `Stat` field to a fresh, empty `Stat` (baseValue `0`, empty modifiers list) the first time the containing `Entity_Stats` component is added to a GameObject. From there, either hand-entered Inspector values or a call to `ApplyDefaultStatSetup()` (`02-Entity.md`) give the stats their real starting numbers.

---

## How this connects elsewhere

- `Entity_Stats` (`02-Entity.md`) owns one of each group (`offenseGroup`, `defenseGroup`, `resourceGroup`, `majorGroup`) and is where every actual damage/mitigation/evasion *formula* lives — this doc only covers the underlying number-storage building block those formulas read from.
- `Player_Stats.ApplyBuff`/`BuffDurationCo` (`03-Player.md`) is the clearest real example of `AddModifier`/`RemoveModifier` and the `source`-tag convention in action.
- `Entity_Stats.GetStatByType(StatType)` and `Player_SkillManager.GetSkillByType(SkillType)` (`02-Entity.md`, `03-Player.md`) are both examples of the same "enum in, concrete object out via a switch" lookup shape — `StatType` is specifically what lets generic code (a stat panel, a buff's `BuffEffectData`) address one of these `Stat` fields by name/enum rather than a hardcoded reference.

## Where this pattern could be reused later

The `Stat`/`StatModifier` pair — a base value plus a list of labeled, individually-removable modifiers, with a lazily-recalculated cached total — is a strong, reusable template for *any* game with temporary or stacking numeric effects (buffs, debuffs, equipment bonuses, aura effects), not just this one. The specific trick worth carrying forward is tagging each modifier with a `source` string identifying *who* added it rather than *what* it does — that's what makes "remove everything this one buff/item added" a single cheap call instead of needing to track a list of exactly which modifiers belong to which effect separately. The dirty-flag caching (`needToRecalculate`) is likewise a broadly useful technique any time a computed value is read far more often than its inputs actually change.

---

**Next:** `07-InventorySystem.md` — `Inventory_Base` and its `Inventory_Player`/`Inventory_Merchant`/`Inventory_Storage` subclasses, equipment, and crafting.
