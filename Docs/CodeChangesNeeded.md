# Code Changes Needed for Full Content Support

This document lists all C# code changes needed to fully support the new game content. The data files (cards.json, enemies.json, relics.json) and the extended IntentResolver are already written.

---

## 1. New Card Effect Types

**File**: `Scripts/Data/CardData.cs`

Add three new values to `CardEffectType`:

```csharp
public enum CardEffectType
{
    Damage,
    GainBlock,
    ApplyVulnerable,
    DrawCards,
    GainStrength,
    GainEnergy,
    Heal,
    DiscardCards,
    // NEW — Phase 2
    ApplyWeak,       // Reduce enemy attack damage by 25% per stack
    LoseHp,          // Player sacrifices HP (cost for powerful cards)
    GainDexterity,   // +1 Block per stack when gaining Block
}
```

## 2. New Card Keywords

**File**: `Scripts/Data/CardData.cs`

Add two new values to `CardKeyword`:

```csharp
public enum CardKeyword
{
    Retain,
    Exhaust,
    Curious,
    // NEW — Phase 2
    Ethereal,  // If still in hand at end of turn, exhaust this card
    Innate,    // This card always appears in your opening hand
}
```

## 3. ICardEffectRuntime — New Callbacks

**File**: `Scripts/Systems/CardEffectPipeline.cs`

Add three new methods to the `ICardEffectRuntime` interface:

```csharp
public interface ICardEffectRuntime
{
    void ExecuteDamage(CardData card, CardEffectData effect);
    void ExecuteGainBlock(CardData card, CardEffectData effect);
    void ExecuteApplyVulnerable(CardData card, CardEffectData effect);
    void ExecuteGainStrength(CardData card, CardEffectData effect);
    void ExecuteGainEnergy(CardData card, CardEffectData effect);
    void ExecuteHeal(CardData card, CardEffectData effect);
    void ExecuteDiscardCards(CardData card, CardEffectData effect);
    // NEW
    void ExecuteApplyWeak(CardData card, CardEffectData effect);
    void ExecuteLoseHp(CardData card, CardEffectData effect);
    void ExecuteGainDexterity(CardData card, CardEffectData effect);
}
```

Also register the new handlers in the static constructor:

```csharp
static CardEffectPipeline()
{
    // ... existing registrations ...
    RegisterOrReplaceHandler(CardEffectType.ApplyWeak, (card, effect, _, runtime) => runtime.ExecuteApplyWeak(card, effect));
    RegisterOrReplaceHandler(CardEffectType.LoseHp, (card, effect, _, runtime) => runtime.ExecuteLoseHp(card, effect));
    RegisterOrReplaceHandler(CardEffectType.GainDexterity, (card, effect, _, runtime) => runtime.ExecuteGainDexterity(card, effect));
}
```

## 4. EnemyUnit — New Status Fields

**File**: `Scripts/Data/EnemyUnit.cs`

Add `Weak` field:

```csharp
public sealed class EnemyUnit
{
    public string ArchetypeId = "cultist";
    public string Name = "Enemy";
    public string VisualId = "cultist";
    public int Hp;
    public int MaxHp;
    public int Block;
    public int Strength;
    public int Vulnerable;
    public int Weak;        // NEW
    public EnemyIntentType IntentType;
    public int IntentValue;
    public bool IsAlive => Hp > 0;
}
```

## 5. PlayerUnit — New Status Fields

**File**: `Scripts/Data/PlayerUnit.cs`

Add `Weak` and `Dexterity` fields:

```csharp
public sealed class PlayerUnit
{
    public string Name = "Player";
    public int Hp;
    public int MaxHp;
    public int Block;
    public int Strength;
    public int Vulnerable;
    public int Weak;         // NEW
    public int Dexterity;    // NEW
}
```

## 6. BattleScene — Effect Implementations

**File**: `Scripts/Scenes/BattleScene.CardEffects.cs` (or equivalent partial file)

Implement the three new effect callbacks:

```csharp
public void ExecuteApplyWeak(CardData card, CardEffectData effect)
{
    if (effect.Target == CardEffectTarget.Player)
    {
        _player.Weak += effect.Amount;
    }
    else if (effect.Target == CardEffectTarget.SelectedEnemy)
    {
        var enemy = GetSelectedEnemy();
        if (enemy != null) enemy.Weak += effect.Amount;
    }
    else if (effect.Target == CardEffectTarget.AllEnemies)
    {
        foreach (var enemy in _enemies)
        {
            if (enemy.IsAlive) enemy.Weak += effect.Amount;
        }
    }
}

public void ExecuteLoseHp(CardData card, CardEffectData effect)
{
    _player.Hp = Math.Max(0, _player.Hp - effect.Amount);
}

public void ExecuteGainDexterity(CardData card, CardEffectData effect)
{
    _player.Dexterity += effect.Amount;
}
```

## 7. CombatResolver — Weak Modifier

**File**: `Scripts/Systems/CombatResolver.cs`

Add a Weak modifier function:

```csharp
public static int ApplyWeakMultiplier(int baseDamage, int weakStacks)
{
    if (weakStacks <= 0) return baseDamage;
    // Each Weak stack reduces damage by 25% (multiplicative), min 1
    var reduction = (int)(baseDamage * 0.25 * Math.Min(weakStacks, 4));
    return Math.Max(1, baseDamage - reduction);
}
```

## 8. GainBlock with Dexterity

**File**: `Scripts/Scenes/BattleScene.CardEffects.cs`

Modify `ExecuteGainBlock` to add Dexterity bonus:

```csharp
public void ExecuteGainBlock(CardData card, CardEffectData effect)
{
    var blockAmount = effect.Amount + _player.Dexterity; // NEW: +Dex
    _player.Block += blockAmount;
}
```

## 9. Turn Flow — Status Decay

**File**: `Scripts/Systems/TurnFlowResolver.cs`

At end-of-turn, tick down Weak along with Vulnerable:

```csharp
public static void TickStatuses(PlayerUnit player, IReadOnlyList<EnemyUnit> enemies)
{
    // Vulnerable decay
    if (player.Vulnerable > 0) player.Vulnerable--;
    foreach (var enemy in enemies)
    {
        if (enemy.Vulnerable > 0) enemy.Vulnerable--;
        // NEW: Weak decay
        if (enemy.Weak > 0) enemy.Weak--;
    }
    // NEW: Player Weak decay
    if (player.Weak > 0) player.Weak--;
}
```

## 10. Ethereal Keyword Handling

**File**: `Scripts/Scenes/BattleScene.cs`

At end of turn, before moving hand to discard:
- Any card with `Ethereal` keyword goes to exhaust pile instead of discard pile

```csharp
// In EndTurn / TickStatuses area:
foreach (var card in _hand.ToList())
{
    if (card.Keywords.Contains(CardKeyword.Ethereal))
    {
        _hand.Remove(card);
        _exhaustPile.Add(card);
    }
}
```

## 11. Innate Keyword Handling

**File**: `Scripts/Systems/DeckFlowResolver.cs` or `BattleScene.cs`

At combat start, Innate cards should be prioritized into the opening hand:

```csharp
// In DrawOpeningHand or equivalent:
var innateCards = drawPile.Where(c => c.Keywords.Contains(CardKeyword.Innate)).ToList();
foreach (var card in innateCards)
{
    drawPile.Remove(card);
    hand.Add(card);
}
// Then draw remaining cards to fill hand to 5
var remaining = 5 - hand.Count;
// ... normal draw logic for remaining slots
```

## 12. Character-Specific Reward Pools

**File**: `Scripts/Autoload/GameState.cs`

When resolving battle rewards, use character-specific reward pool:

```csharp
// In ResolveBattleVictory or reward generation:
var rewardPool = CardData.RewardPoolIds(); // legacy default

// Check for character-specific pool
if (_selectedPresetId != null)
{
    var charPool = CardData.GetCharacterRewardPool(_selectedPresetId);
    if (charPool.Count > 0) rewardPool = charPool;
}
```

**File**: `Scripts/Data/CardData.cs`

Add a static method to parse character-specific pools from cards.json:

```csharp
public static List<string> GetCharacterRewardPool(string characterId)
{
    return Catalog.CharacterRewardPools.TryGetValue(characterId, out var pool)
        ? new List<string>(pool)
        : RewardPoolIds();
}
```

## 13. Boss Multi-Phase HP Triggers

**File**: `Scripts/Scenes/BattleScene.cs`

For boss fights, check HP thresholds to trigger phase changes:

```csharp
// In ExecuteEnemyTurn or after damage:
if (enemy.ArchetypeId.StartsWith("boss_"))
{
    var hpPercent = (float)enemy.Hp / enemy.MaxHp;
    if (hpPercent <= 0.5f && !enemy.Phase2Triggered)
    {
        enemy.Phase2Triggered = true;
        // Clear debuffs, gain strength, etc.
        enemy.Vulnerable = 0;
        enemy.Weak = 0;
        enemy.Strength += 3;
    }
}
```

This requires adding `Phase2Triggered` to `EnemyUnit`:

```csharp
public bool Phase2Triggered;  // NEW
```

## 14. Localization Keys

**File**: `Data/Localization/en.json` and `Data/Localization/zh_hans.json`

Add localization entries for all new cards, enemies, relics, and keywords. Key format examples:
- Cards: `card.{id}.name`, `card.{id}.description`
- Enemies: already handled by archetype displayName
- Keywords: `keyword.ethereal.name`, `keyword.innate.name`

---

## Implementation Priority

### Immediate (required for data files to work)
- [ ] Extended `IntentResolver` (already written — just needs the enum switch cases to compile)

### Phase 1 (core new mechanics)
- [ ] New `CardEffectType` enum values (ApplyWeak, LoseHp, GainDexterity)
- [ ] `ICardEffectRuntime` interface extension
- [ ] `BattleScene` effect implementations
- [ ] `EnemyUnit` / `PlayerUnit` new fields
- [ ] Status decay in `TurnFlowResolver`
- [ ] `GainBlock + Dexterity` bonus

### Phase 2 (keywords and polish)
- [ ] New `CardKeyword` enum values (Ethereal, Innate)
- [ ] Ethereal end-of-turn handling
- [ ] Innate opening-hand handling

### Phase 3 (content infrastructure)
- [ ] Character-specific reward pools
- [ ] Boss multi-phase triggers
- [ ] Localization keys for all new content
