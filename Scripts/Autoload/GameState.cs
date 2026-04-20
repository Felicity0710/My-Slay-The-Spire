using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameState : Node
{
    private Random _rng = new();

    private const int MapWidth = 5;
    private const int MapRows = 8;
    public const int PotionInventoryCapacity = 3;

    public int MaxHp { get; private set; } = 80;
    public int PlayerHp { get; set; } = 80;
    public int Floor { get; private set; } = 1;
    public int BattlesWon { get; private set; }
    public int PotionCharges { get; private set; }
    public string CurrentUiPhase { get; private set; } = "main_menu";
    public bool ExternalFastMode { get; private set; }

    public List<string> DeckCardIds { get; } = new();
    public List<string> RelicIds { get; } = new();
    public List<string> PotionIds { get; } = new();

    public string SelectedDeckPresetId { get; private set; } = "starter";
    public bool HasCustomDeckOverride { get; private set; }

    private readonly List<string> _customDeckOverrideIds = new();

    public List<List<MapNodeType>> MapLayout { get; } = new();
    public List<List<List<int>>> MapConnections { get; } = new();

    public int CurrentMapRow { get; private set; }
    public int CurrentMapColumn { get; private set; } = -1;

    private int PendingMapColumn { get; set; } = -1;

    public MapNodeType PendingEncounterType { get; private set; } = MapNodeType.NormalBattle;
    public string PendingEventId { get; private set; } = string.Empty;

    public List<string> PendingRewardOptions { get; } = new();
    public List<string> PendingRelicOptions { get; } = new();

    public void StartNewRun()
    {
        SetUiPhase("map");
        MaxHp = 80;
        PlayerHp = 80;
        Floor = 1;
        BattlesWon = 0;
        PotionCharges = 0;

        ApplySelectedDeckPreset();

        RelicIds.Clear();
        PotionIds.Clear();

        PendingEncounterType = MapNodeType.NormalBattle;
        PendingEventId = string.Empty;
        PendingRewardOptions.Clear();
        PendingRelicOptions.Clear();

        CurrentMapRow = 0;
        CurrentMapColumn = -1;
        PendingMapColumn = -1;

        GenerateMap();
    }

    public IReadOnlyList<DeckPresetData> DeckPresets()
    {
        return DeckPresetCatalog.All();
    }

    public void SetDeckPreset(string presetId)
    {
        SelectedDeckPresetId = DeckPresetCatalog.Resolve(presetId).Id;
    }

    public void SetCustomDeck(IEnumerable<string> cardIds)
    {
        _customDeckOverrideIds.Clear();
        foreach (var id in cardIds)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            _customDeckOverrideIds.Add(id.Trim());
        }

        HasCustomDeckOverride = _customDeckOverrideIds.Count > 0;
    }

    public void ClearCustomDeckOverride()
    {
        _customDeckOverrideIds.Clear();
        HasCustomDeckOverride = false;
    }

    public void StartBattleTestRun(string? presetId = null)
    {
        StartBattleTestRun(presetId, null, null, null, null);
    }

    public void StartBattleTestRun(string? presetId, string? encounterType, int? floorOverride, int? seed, bool? randomized)
    {
        ResetRandom(seed);
        var useRandomizedScenario = randomized ?? (string.IsNullOrWhiteSpace(presetId) && string.IsNullOrWhiteSpace(encounterType) && !floorOverride.HasValue);
        var resolvedPresetId = !string.IsNullOrWhiteSpace(presetId)
            ? DeckPresetCatalog.Resolve(presetId).Id
            : useRandomizedScenario ? ChooseRandomBattleTestPresetId() : SelectedDeckPresetId;

        if (!string.IsNullOrWhiteSpace(resolvedPresetId))
        {
            SetDeckPreset(resolvedPresetId);
        }

        StartNewRun();
        Floor = floorOverride.HasValue && floorOverride.Value > 0
            ? floorOverride.Value
            : useRandomizedScenario ? _rng.Next(1, MapRows + 1) : 1;

        var resolvedEncounterType = ResolveBattleTestEncounterType(encounterType, useRandomizedScenario);
        SetUiPhase("battle");
        BeginEncounter(resolvedEncounterType);
    }

    public void SetUiPhase(string phase)
    {
        CurrentUiPhase = string.IsNullOrWhiteSpace(phase) ? "unknown" : phase.Trim().ToLowerInvariant();
    }

    public void SetExternalFastMode(bool enabled)
    {
        ExternalFastMode = enabled;
    }

    private void ResetRandom(int? seed)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    private string ChooseRandomBattleTestPresetId()
    {
        var presets = DeckPresets();
        if (presets.Count == 0)
        {
            return "starter";
        }

        return presets[_rng.Next(presets.Count)].Id;
    }

    private MapNodeType ResolveBattleTestEncounterType(string? encounterType, bool randomized)
    {
        if (!string.IsNullOrWhiteSpace(encounterType))
        {
            var normalized = encounterType.Trim();
            if (string.Equals(normalized, "normal", StringComparison.OrdinalIgnoreCase))
            {
                return MapNodeType.NormalBattle;
            }

            if (string.Equals(normalized, "elite", StringComparison.OrdinalIgnoreCase))
            {
                return MapNodeType.EliteBattle;
            }

            if (Enum.TryParse<MapNodeType>(normalized, true, out var parsed))
            {
                return parsed;
            }
        }

        if (!randomized)
        {
            return MapNodeType.NormalBattle;
        }

        return _rng.NextDouble() < 0.28 ? MapNodeType.EliteBattle : MapNodeType.NormalBattle;
    }

    private void ApplySelectedDeckPreset()
    {
        DeckCardIds.Clear();

        if (HasCustomDeckOverride && _customDeckOverrideIds.Count > 0)
        {
            DeckCardIds.AddRange(_customDeckOverrideIds);
            return;
        }

        var preset = DeckPresetCatalog.Resolve(SelectedDeckPresetId);
        SelectedDeckPresetId = preset.Id;
        DeckCardIds.AddRange(preset.CardIds);
    }

    public bool HasRelic(string id)
    {
        return RelicIds.Contains(id);
    }

    public void AddRelic(string id)
    {
        if (!RelicIds.Contains(id))
        {
            RelicIds.Add(id);
        }
    }

    public List<CardData> CreateDeckCards()
    {
        var list = new List<CardData>();
        foreach (var id in DeckCardIds)
        {
            list.Add(CardData.CreateById(id));
        }

        return list;
    }

    public void AddCardToDeck(string id)
    {
        DeckCardIds.Add(id);
    }


    public void AddPotion(string potionId)
    {
        TryAddPotion(potionId);
    }

    public PotionData AddRandomPotion()
    {
        TryAddRandomPotion(out var potion);
        return potion;
    }

    public bool TryAddPotion(string potionId)
    {
        if (string.IsNullOrWhiteSpace(potionId) || PotionIds.Count >= PotionInventoryCapacity)
        {
            return false;
        }

        PotionIds.Add(potionId);
        PotionCharges = PotionIds.Count;
        return true;
    }

    public bool TryAddRandomPotion(out PotionData potion)
    {
        var pool = PotionData.AllPotionIds();
        if (pool.Count == 0)
        {
            potion = PotionData.CreateById("healing_potion");
            return TryAddPotion(potion.Id);
        }

        var potionId = pool[_rng.Next(pool.Count)];
        potion = PotionData.CreateById(potionId);
        return TryAddPotion(potionId);
    }

    public bool TryConsumePotionAt(int index, out PotionData potion)
    {
        potion = PotionData.CreateById("healing_potion");
        if (index < 0 || index >= PotionIds.Count)
        {
            return false;
        }

        var potionId = PotionIds[index];
        PotionIds.RemoveAt(index);
        PotionCharges = PotionIds.Count;
        potion = PotionData.CreateById(potionId);
        return true;
    }

    public void AddPotionCharge(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        var cap = Math.Min(PotionCharges + amount, PotionInventoryCapacity);
        while (PotionIds.Count < cap)
        {
            PotionIds.Add("healing_potion");
        }

        PotionCharges = PotionIds.Count;
    }

    public bool CanChooseMapNode(int row, int column)
    {
        if (row != CurrentMapRow || row < 0 || row >= MapLayout.Count)
        {
            return false;
        }

        if (column < 0 || column >= MapLayout[row].Count)
        {
            return false;
        }

        if (CurrentMapRow == 0)
        {
            return true;
        }

        if (CurrentMapColumn < 0 || CurrentMapColumn >= MapWidth)
        {
            // Fallback for debug/forced encounters that did not originate from map node selection.
            return true;
        }

        return MapConnections[CurrentMapRow - 1][CurrentMapColumn].Contains(column);
    }

    public MapNodeType GetMapNodeType(int row, int column)
    {
        return MapLayout[row][column];
    }

    public bool ChooseMapNode(int column, out MapNodeType nodeType)
    {
        nodeType = MapNodeType.NormalBattle;
        if (!CanChooseMapNode(CurrentMapRow, column))
        {
            return false;
        }

        nodeType = MapLayout[CurrentMapRow][column];
        PendingMapColumn = column;
        PendingEncounterType = nodeType;
        return true;
    }

    public void BeginEncounter(MapNodeType nodeType)
    {
        PendingEncounterType = nodeType;
    }

    public void ResolveBattleVictory()
    {
        BattlesWon += 1;

        if (HasRelic("charm"))
        {
            PlayerHp = Math.Min(PlayerHp + 5, MaxHp);
        }

        if (HasRelic("blood_vial"))
        {
            PlayerHp = Math.Min(PlayerHp + 2, MaxHp);
        }

        AdvanceFloor();
    }

    public void GainMaxHp(int amount)
    {
        MaxHp += amount;
        PlayerHp = Math.Min(PlayerHp + amount, MaxHp);
    }

    public void ResolveRestNode()
    {
        PlayerHp = Math.Min(PlayerHp + 18, MaxHp);
        AdvanceFloor();
    }

    public void ResolveShopNode()
    {
        if (_rng.Next(2) == 0)
        {
            PlayerHp = Math.Min(PlayerHp + 12, MaxHp);
        }
        else
        {
            var pool = CardData.RewardPoolIds();
            AddCardToDeck(pool[_rng.Next(pool.Count)]);
        }

        AdvanceFloor();
    }

    public void BeginRandomEvent()
    {
        PendingEventId = _rng.Next(2) == 0 ? "shrine" : "gamble";
    }

    public void ResolveEventFinished()
    {
        PendingEventId = string.Empty;
        AdvanceFloor();
    }

    public string MapNodeLabel(MapNodeType type)
    {
        return type switch
        {
            MapNodeType.NormalBattle => LocalizationService.Get("map.node.normal", "Normal") ,
            MapNodeType.EliteBattle => LocalizationService.Get("map.node.elite", "Elite") ,
            MapNodeType.Event => LocalizationService.Get("map.node.event", "Event") ,
            MapNodeType.Rest => LocalizationService.Get("map.node.rest", "Rest") ,
            MapNodeType.Shop => LocalizationService.Get("map.node.shop", "Shop") ,
            _ => LocalizationService.Get("map.node.unknown", "Unknown")
        };
    }

    public string MapNodeSymbol(MapNodeType type)
    {
        return type switch
        {
            MapNodeType.NormalBattle => LocalizationService.Get("map.node_symbol.normal", "\u2694") ,
            MapNodeType.EliteBattle => LocalizationService.Get("map.node_symbol.elite", "\u2620") ,
            MapNodeType.Event => LocalizationService.Get("map.node_symbol.event", "\u25c6") ,
            MapNodeType.Rest => LocalizationService.Get("map.node_symbol.rest", "\u2665") ,
            MapNodeType.Shop => LocalizationService.Get("map.node_symbol.shop", "$") ,
            _ => LocalizationService.Get("map.node_symbol.unknown", "\uff1f")
        };
    }

    public void RollRewardOptions(int count)
    {
        PendingRewardOptions.Clear();

        var pool = CardData.RewardPoolIds();
        pool.RemoveAll(id => id == "strike" || id == "defend");

        if (pool.Count == 0)
        {
            return;
        }

        // Guarantee at least one build-enabler so rewards feel less like starter deck filler.
        var buildCards = pool.Where(IsBuildEnablerCard).ToList();
        if (buildCards.Count > 0 && count > 0)
        {
            var guaranteed = buildCards[_rng.Next(buildCards.Count)];
            PendingRewardOptions.Add(guaranteed);
            pool.Remove(guaranteed);
        }

        Shuffle(pool);

        var cap = Math.Min(count - PendingRewardOptions.Count, pool.Count);
        for (var i = 0; i < cap; i++)
        {
            PendingRewardOptions.Add(pool[i]);
        }
    }

    public void RollRelicOptions(int count)
    {
        PendingRelicOptions.Clear();

        var pool = new List<string>(RelicData.AllRelicIds());
        pool.RemoveAll(HasRelic);

        if (pool.Count == 0)
        {
            return;
        }

        Shuffle(pool);
        var cap = Math.Min(count, pool.Count);
        for (var i = 0; i < cap; i++)
        {
            PendingRelicOptions.Add(pool[i]);
        }
    }

    private void GenerateMap()
    {
        MapLayout.Clear();
        MapConnections.Clear();

        for (var row = 0; row < MapRows; row++)
        {
            var rowTypes = new List<MapNodeType>(MapWidth);
            for (var col = 0; col < MapWidth; col++)
            {
                rowTypes.Add(RollNodeType(row));
            }

            MapLayout.Add(rowTypes);
        }

        for (var row = 0; row < MapRows - 1; row++)
        {
            var rowConnections = new List<List<int>>(MapWidth);
            for (var col = 0; col < MapWidth; col++)
            {
                var next = new List<int> { col };
                if (col > 0 && _rng.NextDouble() < 0.55)
                {
                    next.Add(col - 1);
                }

                if (col < MapWidth - 1 && _rng.NextDouble() < 0.55)
                {
                    next.Add(col + 1);
                }

                Shuffle(next);
                rowConnections.Add(next);
            }

            MapConnections.Add(rowConnections);
        }

        EnforceProgressionLandmarks();
    }

    private void EnforceProgressionLandmarks()
    {
        MapLayout[0][_rng.Next(MapWidth)] = MapNodeType.NormalBattle;
        MapLayout[3][_rng.Next(MapWidth)] = MapNodeType.Shop;
        MapLayout[6][_rng.Next(MapWidth)] = MapNodeType.Rest;
        MapLayout[7][_rng.Next(MapWidth)] = MapNodeType.EliteBattle;
    }

    private MapNodeType RollNodeType(int row)
    {
        var roll = _rng.NextDouble();

        if (row <= 1)
        {
            return roll < 0.65 ? MapNodeType.NormalBattle : MapNodeType.Event;
        }

        if (row >= MapRows - 2)
        {
            if (roll < 0.45)
            {
                return MapNodeType.EliteBattle;
            }

            return roll < 0.75 ? MapNodeType.NormalBattle : MapNodeType.Rest;
        }

        if (roll < 0.40)
        {
            return MapNodeType.NormalBattle;
        }

        if (roll < 0.60)
        {
            return MapNodeType.Event;
        }

        if (roll < 0.78)
        {
            return MapNodeType.Shop;
        }

        if (roll < 0.90)
        {
            return MapNodeType.Rest;
        }

        return MapNodeType.EliteBattle;
    }

    private void AdvanceFloor()
    {
        Floor += 1;
        if (PendingMapColumn >= 0)
        {
            CurrentMapColumn = PendingMapColumn;
        }

        PendingMapColumn = -1;
        CurrentMapRow += 1;

        if (CurrentMapRow >= MapRows)
        {
            CurrentMapRow = 0;
            CurrentMapColumn = -1;
            GenerateMap();
        }
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool IsBuildEnablerCard(string cardId)
    {
        var card = CardData.CreateById(cardId);
        foreach (var effect in card.Effects)
        {
            if (effect.Type == CardEffectType.GainStrength ||
                effect.Type == CardEffectType.GainEnergy ||
                effect.Type == CardEffectType.Heal)
            {
                return true;
            }

            if (effect.Type == CardEffectType.Damage && effect.Target == CardEffectTarget.AllEnemies)
            {
                return true;
            }

            if (effect.Type == CardEffectType.ApplyVulnerable && effect.Target == CardEffectTarget.AllEnemies)
            {
                return true;
            }
        }

        return false;
    }
}
