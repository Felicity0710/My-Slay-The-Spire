using Godot;
using System;
using System.Collections.Generic;

public partial class GameState : Node
{
    private readonly Random _rng = new();

    public int MaxHp { get; private set; } = 80;
    public int PlayerHp { get; set; } = 80;
    public int Floor { get; private set; } = 1;
    public int BattlesWon { get; private set; }

    public List<string> DeckCardIds { get; } = new();
    public List<string> RelicIds { get; } = new();

    public List<MapNodeType> CurrentMapOptions { get; } = new();

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

        DeckCardIds.Clear();
        DeckCardIds.AddRange(CardData.StarterDeckIds());

        RelicIds.Clear();
        CurrentMapOptions.Clear();

        PendingEncounterType = MapNodeType.NormalBattle;
        PendingEventId = string.Empty;
        PendingRewardOptions.Clear();
        PendingRelicOptions.Clear();

        GenerateMapOptions();
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

    public void BeginRandomEvent()
    {
        PendingEventId = _rng.Next(2) == 0 ? "shrine" : "gamble";
    }

    public void ResolveEventFinished()
    {
        PendingEventId = string.Empty;
        AdvanceFloor();
    }

    public void GenerateMapOptions()
    {
        CurrentMapOptions.Clear();

        CurrentMapOptions.Add(MapNodeType.NormalBattle);
        CurrentMapOptions.Add(Pick(new[] { MapNodeType.Event, MapNodeType.Rest, MapNodeType.NormalBattle }));
        CurrentMapOptions.Add(Pick(new[] { MapNodeType.EliteBattle, MapNodeType.Event, MapNodeType.Rest }));

        Shuffle(CurrentMapOptions);
    }

    public string MapNodeLabel(MapNodeType type)
    {
        return type switch
        {
            MapNodeType.NormalBattle => "Normal Battle",
            MapNodeType.EliteBattle => "Elite Battle",
            MapNodeType.Event => "Event",
            MapNodeType.Rest => "Rest Site",
            _ => "Unknown"
        };
    }

    public void RollRewardOptions(int count)
    {
        PendingRewardOptions.Clear();

        var pool = CardData.RewardPoolIds();
        Shuffle(pool);

        var cap = Math.Min(count, pool.Count);
        for (var i = 0; i < cap; i++)
        {
            PendingRewardOptions.Add(pool[i]);
        }
    }

    public void RollRelicOptions(int count)
    {
        PendingRelicOptions.Clear();

        var pool = new List<string> { "lantern", "anchor", "whetstone", "charm" };
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

    private void AdvanceFloor()
    {
        Floor += 1;
        GenerateMapOptions();
    }

    private T Pick<T>(IList<T> items)
    {
        return items[_rng.Next(items.Count)];
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
