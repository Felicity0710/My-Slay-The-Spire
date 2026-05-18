using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameState : Node
{
    private GameRng _rng = new();

    public GameRng Rng => _rng;

    private const int MapWidth = 5;
    private const int MapRows = MapProgressionRules.RowsPerAct;
    public const int PotionInventoryCapacity = 3;

    public int MaxHp { get; private set; } = 80;
    public int PlayerHp { get; set; } = 80;
    public int Floor { get; private set; } = 1;
    public int Act { get; private set; } = 1;
    public bool RunCompleted { get; private set; }
    public int BattlesWon { get; private set; }
    public int PotionCharges { get; private set; }
    public int Gold { get; private set; }
    public bool MerchantFled { get; private set; }
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

    public sealed class ShopInventoryEntry
    {
        public string Kind { get; set; } = string.Empty; // "card" | "relic" | "potion"
        public string Id { get; set; } = string.Empty;
        public int Price { get; set; }
        public bool Sold { get; set; }
    }

    public List<ShopInventoryEntry> ShopSnapshot { get; } = new();
    public bool ShopSnapshotHasData { get; private set; }
    public bool ShopSnapshotRemoveServiceUsed { get; private set; }
    public bool PendingMerchantFightVictory { get; private set; }

    public sealed class NodeEntrySnapshot
    {
        public int PlayerHp;
        public int MaxHp;
        public int Gold;
        public List<string> DeckCardIds = new();
        public List<string> Relics = new();
        public List<string> PotionIds = new();
        public int PotionCharges;
        public int Floor;
        public int Act;
        public bool RunCompleted;
        public bool MerchantFled;
        public int CurrentMapRow;
        public int CurrentMapColumn;
        public int PendingMapColumn;
        public MapNodeType PendingEncounterType;
        public string PendingEventId = string.Empty;
        public int BattlesWon;
        public ulong RngState;
        public List<ShopInventoryEntry> ShopSnapshot = new();
        public bool ShopSnapshotHasData;
        public bool ShopSnapshotRemoveServiceUsed;
        public bool PendingMerchantFightVictory;
        public List<string> PendingRewardOptions = new();
        public List<string> PendingPotionRewardOptions = new();
        public List<string> PendingRelicOptions = new();
        public string SceneFilePath = string.Empty;
    }

    private NodeEntrySnapshot? _nodeEntrySnapshot;
    public bool HasNodeEntrySnapshot => _nodeEntrySnapshot != null;

    public void StartNewRun()
    {
        SaveSystem.Delete();
        SetUiPhase("map");
        MaxHp = 80;
        PlayerHp = 80;
        Floor = 1;
        Act = 1;
        RunCompleted = false;
        BattlesWon = 0;
        PotionCharges = 0;
        Gold = 99;
        MerchantFled = false;

        ApplySelectedDeckPreset();

        RelicIds.Clear();
        PotionIds.Clear();

        PendingEncounterType = MapNodeType.NormalBattle;
        PendingEventId = string.Empty;
        PendingRewardOptions.Clear();
        PendingRelicOptions.Clear();
        ClearShopSnapshot();
        PendingMerchantFightVictory = false;

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
        _rng = seed.HasValue ? new GameRng((ulong)(uint)seed.Value) : new GameRng();
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

        var prevRowWidth = MapLayout[CurrentMapRow - 1].Count;
        if (CurrentMapColumn < 0 || CurrentMapColumn >= prevRowWidth)
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

    public sealed class BattleRewardSummary
    {
        public bool IsEliteTier { get; set; }
        public int GoldGained { get; set; }
        public int HealedFromCharm { get; set; }
        public int HealedFromBloodVial { get; set; }
    }

    private static readonly IReadOnlyList<string> EliteTierPotionPool = new[]
    {
        "strength_potion",
        "guard_potion",
        "fury_potion"
    };

    public BattleRewardSummary LastBattleReward { get; private set; } = new();
    public List<string> PendingPotionRewardOptions { get; } = new();

    public void ResolveBattleVictory()
    {
        BattlesWon += 1;
        RollBattleRewardOffers();
        AdvanceFloor();
    }

    private void RollBattleRewardOffers()
    {
        // Wipe any leftovers from a previous battle in case the player skipped picks.
        PendingRewardOptions.Clear();
        PendingPotionRewardOptions.Clear();
        PendingRelicOptions.Clear();

        var summary = new BattleRewardSummary();
        var isElite = PendingEncounterType == MapNodeType.EliteBattle
            || PendingEncounterType == MapNodeType.Boss;
        summary.IsEliteTier = isElite;

        if (HasRelic("charm"))
        {
            var before = PlayerHp;
            PlayerHp = Math.Min(PlayerHp + 5, MaxHp);
            summary.HealedFromCharm = PlayerHp - before;
        }

        if (HasRelic("blood_vial"))
        {
            var before = PlayerHp;
            PlayerHp = Math.Min(PlayerHp + 2, MaxHp);
            summary.HealedFromBloodVial = PlayerHp - before;
        }

        // Gold is auto-granted (not part of the "pick one" picker UI).
        var goldGain = isElite ? _rng.Next(40, 60) : _rng.Next(18, 28);
        AddGold(goldGain);
        summary.GoldGained = goldGain;

        // Card offers. Elite/Boss show one extra card and a boosted upgrade chance.
        var cardOfferCount = isElite ? 4 : 3;
        var baseUpgradeChance = CurrentUpgradeChance();
        var upgradeChance = isElite
            ? Math.Clamp(baseUpgradeChance + 0.20, 0.0, 1.0)
            : baseUpgradeChance;

        var cardPool = CardData.RewardPoolIds();
        cardPool.RemoveAll(id => id == "strike" || id == "defend");
        Shuffle(cardPool);
        for (var i = 0; i < cardOfferCount && i < cardPool.Count; i++)
        {
            PendingRewardOptions.Add(MaybeUpgradeCardId(cardPool[i], upgradeChance));
        }

        // Potion offers. Elite/Boss roll from a higher-impact pool and show more.
        var potionOfferCount = isElite ? 3 : 2;
        var potionPool = isElite
            ? new List<string>(EliteTierPotionPool)
            : new List<string>(PotionData.AllPotionIds());
        Shuffle(potionPool);
        for (var i = 0; i < potionOfferCount && i < potionPool.Count; i++)
        {
            PendingPotionRewardOptions.Add(potionPool[i]);
        }

        // Relic offers — elite/boss only. Show two options; pick one or skip.
        if (isElite)
        {
            var relicPool = new List<string>(RelicData.AllRelicIds());
            relicPool.RemoveAll(HasRelic);
            Shuffle(relicPool);
            var relicOfferCount = Math.Min(2, relicPool.Count);
            for (var i = 0; i < relicOfferCount; i++)
            {
                PendingRelicOptions.Add(relicPool[i]);
            }
        }

        LastBattleReward = summary;
    }

    public bool TakeRewardCardOption(int index)
    {
        if (index < 0 || index >= PendingRewardOptions.Count)
        {
            return false;
        }

        AddCardToDeck(PendingRewardOptions[index]);
        PendingRewardOptions.Clear();
        return true;
    }

    public bool TakeRewardPotionOption(int index)
    {
        if (index < 0 || index >= PendingPotionRewardOptions.Count)
        {
            return false;
        }

        var success = TryAddPotion(PendingPotionRewardOptions[index]);
        PendingPotionRewardOptions.Clear();
        return success;
    }

    public bool TakeRewardRelicOption(int index)
    {
        if (index < 0 || index >= PendingRelicOptions.Count)
        {
            return false;
        }

        AddRelic(PendingRelicOptions[index]);
        PendingRelicOptions.Clear();
        return true;
    }

    public void SkipRewardCards()
    {
        PendingRewardOptions.Clear();
    }

    public void SkipRewardPotions()
    {
        PendingPotionRewardOptions.Clear();
    }

    public void SkipRewardRelics()
    {
        PendingRelicOptions.Clear();
    }

    public void ClearBattleRewardOffers()
    {
        PendingRewardOptions.Clear();
        PendingPotionRewardOptions.Clear();
        PendingRelicOptions.Clear();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Gold += amount;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || Gold < amount)
        {
            return false;
        }

        Gold -= amount;
        return true;
    }

    public bool TryRemoveCardFromDeck(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return false;
        }

        var index = DeckCardIds.IndexOf(cardId);
        if (index < 0)
        {
            return false;
        }

        DeckCardIds.RemoveAt(index);
        return true;
    }

    public void MarkMerchantFled()
    {
        MerchantFled = true;
    }

    public void SaveShopSnapshot(IEnumerable<ShopInventoryEntry> items, bool removeServiceUsed)
    {
        ShopSnapshot.Clear();
        foreach (var item in items)
        {
            ShopSnapshot.Add(new ShopInventoryEntry
            {
                Kind = item.Kind,
                Id = item.Id,
                Price = item.Price,
                Sold = item.Sold
            });
        }

        ShopSnapshotRemoveServiceUsed = removeServiceUsed;
        ShopSnapshotHasData = true;
    }

    public void ClearShopSnapshot()
    {
        ShopSnapshot.Clear();
        ShopSnapshotHasData = false;
        ShopSnapshotRemoveServiceUsed = false;
    }

    public void SaveNodeEntrySnapshot(string sceneFilePath)
    {
        var snap = new NodeEntrySnapshot
        {
            PlayerHp = PlayerHp,
            MaxHp = MaxHp,
            Gold = Gold,
            DeckCardIds = new List<string>(DeckCardIds),
            Relics = new List<string>(RelicIds),
            PotionIds = new List<string>(PotionIds),
            PotionCharges = PotionCharges,
            Floor = Floor,
            Act = Act,
            RunCompleted = RunCompleted,
            MerchantFled = MerchantFled,
            CurrentMapRow = CurrentMapRow,
            CurrentMapColumn = CurrentMapColumn,
            PendingMapColumn = PendingMapColumn,
            PendingEncounterType = PendingEncounterType,
            PendingEventId = PendingEventId,
            BattlesWon = BattlesWon,
            RngState = _rng.State,
            ShopSnapshotHasData = ShopSnapshotHasData,
            ShopSnapshotRemoveServiceUsed = ShopSnapshotRemoveServiceUsed,
            PendingMerchantFightVictory = PendingMerchantFightVictory,
            SceneFilePath = sceneFilePath
        };

        foreach (var entry in ShopSnapshot)
        {
            snap.ShopSnapshot.Add(new ShopInventoryEntry
            {
                Kind = entry.Kind,
                Id = entry.Id,
                Price = entry.Price,
                Sold = entry.Sold
            });
        }

        snap.PendingRewardOptions.AddRange(PendingRewardOptions);
        snap.PendingPotionRewardOptions.AddRange(PendingPotionRewardOptions);
        snap.PendingRelicOptions.AddRange(PendingRelicOptions);

        _nodeEntrySnapshot = snap;

        TryWriteSave(sceneFilePath);
    }

    public bool RestoreNodeEntrySnapshot()
    {
        var snap = _nodeEntrySnapshot;
        if (snap == null)
        {
            return false;
        }

        PlayerHp = snap.PlayerHp;
        MaxHp = snap.MaxHp;
        Gold = snap.Gold;

        DeckCardIds.Clear();
        DeckCardIds.AddRange(snap.DeckCardIds);

        RelicIds.Clear();
        RelicIds.AddRange(snap.Relics);

        PotionIds.Clear();
        PotionIds.AddRange(snap.PotionIds);
        PotionCharges = snap.PotionCharges;

        Floor = snap.Floor;
        Act = snap.Act;
        RunCompleted = snap.RunCompleted;
        MerchantFled = snap.MerchantFled;
        CurrentMapRow = snap.CurrentMapRow;
        CurrentMapColumn = snap.CurrentMapColumn;
        PendingMapColumn = snap.PendingMapColumn;
        PendingEncounterType = snap.PendingEncounterType;
        PendingEventId = snap.PendingEventId;
        BattlesWon = snap.BattlesWon;
        _rng.State = snap.RngState;

        ShopSnapshot.Clear();
        foreach (var entry in snap.ShopSnapshot)
        {
            ShopSnapshot.Add(new ShopInventoryEntry
            {
                Kind = entry.Kind,
                Id = entry.Id,
                Price = entry.Price,
                Sold = entry.Sold
            });
        }

        ShopSnapshotHasData = snap.ShopSnapshotHasData;
        ShopSnapshotRemoveServiceUsed = snap.ShopSnapshotRemoveServiceUsed;
        PendingMerchantFightVictory = snap.PendingMerchantFightVictory;

        PendingRewardOptions.Clear();
        PendingRewardOptions.AddRange(snap.PendingRewardOptions);
        PendingPotionRewardOptions.Clear();
        PendingPotionRewardOptions.AddRange(snap.PendingPotionRewardOptions);
        PendingRelicOptions.Clear();
        PendingRelicOptions.AddRange(snap.PendingRelicOptions);

        return true;
    }

    public string GetNodeEntrySceneFilePath()
    {
        return _nodeEntrySnapshot?.SceneFilePath ?? string.Empty;
    }

    public void ClearNodeEntrySnapshot()
    {
        _nodeEntrySnapshot = null;
    }

    public void BeginMerchantFight()
    {
        PendingEncounterType = MapNodeType.MerchantFight;
        PendingMerchantFightVictory = false;
    }

    public void ResolveMerchantFightVictory()
    {
        // The rob fight is special: no floor advance, no reward roll, no gold reward.
        // Just mark the merchant as fled so the shop unlocks free items on return,
        // and let ShopScene clean up the snapshot once it has restored its UI.
        BattlesWon += 1;
        MerchantFled = true;
        PendingMerchantFightVictory = true;
    }

    public void ConsumePendingMerchantFightVictory()
    {
        PendingMerchantFightVictory = false;
    }

    public void GainMaxHp(int amount)
    {
        MaxHp += amount;
        PlayerHp = Math.Min(PlayerHp + amount, MaxHp);
    }

    public int RestHealAmount()
    {
        return Math.Max(1, (MaxHp * 3) / 10);
    }

    public void ApplyRestHeal()
    {
        PlayerHp = Math.Min(PlayerHp + RestHealAmount(), MaxHp);
        AdvanceFloor();
    }

    public void ApplyRestSkip()
    {
        AdvanceFloor();
    }

    public double CurrentUpgradeChance()
    {
        return MapProgressionRules.UpgradeChanceForAct(Act);
    }

    public string MaybeUpgradeCardId(string cardId, double chance = -1.0)
    {
        var resolvedChance = chance < 0 ? CurrentUpgradeChance() : chance;
        return CardUpgradeRules.MaybeUpgrade(cardId, resolvedChance, _rng);
    }

    public bool DeckCardIsUpgradable(int index)
    {
        if (index < 0 || index >= DeckCardIds.Count)
        {
            return false;
        }

        var id = DeckCardIds[index];
        if (string.IsNullOrEmpty(id) || id.EndsWith("+", StringComparison.Ordinal))
        {
            return false;
        }

        return CardData.CreateById(id).Upgrade != null;
    }

    public bool ApplyRestUpgrade(int deckIndex)
    {
        if (!DeckCardIsUpgradable(deckIndex))
        {
            return false;
        }

        DeckCardIds[deckIndex] = DeckCardIds[deckIndex] + "+";
        AdvanceFloor();
        return true;
    }

    // Legacy entry point kept so the older inline-heal behavior is preserved if any
    // path calls it directly without going through the new rest scene.
    public void ResolveRestNode()
    {
        ApplyRestHeal();
    }

    public void ResolveShopNode()
    {
        // The Shop node is handled interactively by ShopScene; this entry point
        // only advances the map state when the merchant has already fled.
        AdvanceFloor();
    }

    public void ResolveShopExit()
    {
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
            MapNodeType.MerchantFight => LocalizationService.Get("map.node.merchant_fight", "Merchant") ,
            MapNodeType.Intro => LocalizationService.Get("map.node.intro", "Act Intro") ,
            MapNodeType.Boss => LocalizationService.Get("map.node.boss", "Boss") ,
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
            MapNodeType.MerchantFight => LocalizationService.Get("map.node_symbol.merchant_fight", "!") ,
            MapNodeType.Intro => LocalizationService.Get("map.node_symbol.intro", "\u2605") ,
            MapNodeType.Boss => LocalizationService.Get("map.node_symbol.boss", "\u272a") ,
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
            PendingRewardOptions.Add(MaybeUpgradeCardId(guaranteed));
            pool.Remove(guaranteed);
        }

        Shuffle(pool);

        var cap = Math.Min(count - PendingRewardOptions.Count, pool.Count);
        for (var i = 0; i < cap; i++)
        {
            PendingRewardOptions.Add(MaybeUpgradeCardId(pool[i]));
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

        var lastRow = MapRows - 1;
        var restRow = MapRows - 2;

        for (var row = 0; row < MapRows; row++)
        {
            List<MapNodeType> rowTypes;
            if (row == 0)
            {
                // Single intro node at the bottom of the act.
                rowTypes = new List<MapNodeType> { MapNodeType.Intro };
            }
            else if (row == lastRow)
            {
                // Single boss node at the top of the act.
                rowTypes = new List<MapNodeType> { MapNodeType.Boss };
            }
            else if (row == restRow)
            {
                // The row immediately before the boss is always a campfire.
                rowTypes = new List<MapNodeType>(MapWidth);
                for (var col = 0; col < MapWidth; col++)
                {
                    rowTypes.Add(MapNodeType.Rest);
                }
            }
            else
            {
                rowTypes = new List<MapNodeType>(MapWidth);
                for (var col = 0; col < MapWidth; col++)
                {
                    rowTypes.Add(RollNodeType(row));
                }
            }

            MapLayout.Add(rowTypes);
        }

        for (var row = 0; row < MapRows - 1; row++)
        {
            var srcCount = MapLayout[row].Count;
            var dstCount = MapLayout[row + 1].Count;
            var rowConnections = new List<List<int>>(srcCount);

            for (var col = 0; col < srcCount; col++)
            {
                List<int> next;
                if (dstCount == 1)
                {
                    // Every node on this row funnels into the single node above (boss).
                    next = new List<int> { 0 };
                }
                else if (srcCount == 1)
                {
                    // The single intro node connects to every column on the row above.
                    next = new List<int>(dstCount);
                    for (var c = 0; c < dstCount; c++)
                    {
                        next.Add(c);
                    }
                }
                else
                {
                    next = new List<int> { Math.Clamp(col, 0, dstCount - 1) };
                    if (col > 0 && _rng.NextDouble() < 0.55)
                    {
                        next.Add(col - 1);
                    }

                    if (col < dstCount - 1 && _rng.NextDouble() < 0.55)
                    {
                        next.Add(col + 1);
                    }

                    Shuffle(next);
                }

                rowConnections.Add(next);
            }

            MapConnections.Add(rowConnections);
        }

        EnforceProgressionLandmarks();
    }

    private void EnforceProgressionLandmarks()
    {
        // Row 0 is intro, MapRows-1 is boss, MapRows-2 is forced Rest. Place mid-act
        // landmarks among the regular rows (indices 1..MapRows-3).
        if (MapRows >= 4)
        {
            MapLayout[1][_rng.Next(MapLayout[1].Count)] = MapNodeType.NormalBattle;
        }

        if (MapRows >= 6)
        {
            MapLayout[3][_rng.Next(MapLayout[3].Count)] = MapNodeType.Shop;
        }

        if (MapRows >= 8)
        {
            // Mid-act elite encounter on a content row that isn't already forced.
            var eliteRow = MapRows - 4;
            MapLayout[eliteRow][_rng.Next(MapLayout[eliteRow].Count)] = MapNodeType.EliteBattle;
        }
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
            // Finished an act. Either roll into the next act's map, or end the run if the
            // player has cleared the final act.
            var nextAct = Act + 1;
            if (nextAct > MapProgressionRules.MaxActs)
            {
                RunCompleted = true;
                // Keep CurrentMapRow at the post-final value so external snapshots show the
                // player having stepped past the last node. The map layout for the cleared
                // act remains valid until the player returns to the menu.
                return;
            }

            Act = nextAct;
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
