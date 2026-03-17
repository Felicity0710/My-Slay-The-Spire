using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameState : Node
{
    private readonly Random _rng = new();

    private const int MapWidth = 5;
    private const int MapRows = 8;

    public int MaxHp { get; private set; } = 80;
    public int PlayerHp { get; set; } = 80;
    public int Floor { get; private set; } = 1;
    public int BattlesWon { get; private set; }
    public int PotionCharges { get; private set; }

    public List<string> DeckCardIds { get; } = new();
    public List<string> RelicIds { get; } = new();
    public List<string> PotionIds { get; } = new();

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
        MaxHp = 80;
        PlayerHp = 80;
        Floor = 1;
        BattlesWon = 0;
        PotionCharges = 0;

        DeckCardIds.Clear();
        DeckCardIds.AddRange(CardData.StarterDeckIds());

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
        if (PotionIds.Count >= 9)
        {
            return;
        }

        PotionIds.Add(potionId);
        PotionCharges = PotionIds.Count;
    }

    public PotionData AddRandomPotion()
    {
        var pool = PotionData.AllPotionIds();
        if (pool.Count == 0)
        {
            var fallback = PotionData.CreateById("healing_potion");
            AddPotion(fallback.Id);
            return fallback;
        }

        var potionId = pool[_rng.Next(pool.Count)];
        AddPotion(potionId);
        return PotionData.CreateById(potionId);
    }

    public void AddPotionCharge(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        var cap = Math.Min(PotionCharges + amount, 9);
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
            MapNodeType.NormalBattle => "Battle",
            MapNodeType.EliteBattle => "Elite",
            MapNodeType.Event => "Event",
            MapNodeType.Rest => "Rest",
            MapNodeType.Shop => "Shop",
            _ => "Unknown"
        };
    }

    public string MapNodeSymbol(MapNodeType type)
    {
        return type switch
        {
            MapNodeType.NormalBattle => "⚔",
            MapNodeType.EliteBattle => "☠",
            MapNodeType.Event => "?",
            MapNodeType.Rest => "✚",
            MapNodeType.Shop => "$",
            _ => "·"
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
