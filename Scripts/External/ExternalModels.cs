using System.Collections.Generic;

public sealed class ExternalCommandRequest
{
    public string Command { get; set; } = string.Empty;
    public ExternalActionRequest? Action { get; set; }
    public long? ExpectedStateVersion { get; set; }
    public bool? FastMode { get; set; }
}

public sealed class ExternalActionRequest
{
    public string Kind { get; set; } = string.Empty;
    public int? HandIndex { get; set; }
    public string? CardId { get; set; }
    public int? TargetEnemyIndex { get; set; }
    public int? Column { get; set; }
    public string? RewardType { get; set; }
    public int? OptionIndex { get; set; }
    public string? EventOption { get; set; }
    public string? PresetId { get; set; }
    public string? EncounterType { get; set; }
    public int? Floor { get; set; }
    public int? Seed { get; set; }
    public bool? Randomized { get; set; }
}

public sealed class ExternalCommandResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
    public long StateVersion { get; set; }
    public ExternalSnapshot? Snapshot { get; set; }
}

public sealed class ExternalSnapshot
{
    public long StateVersion { get; set; }
    public string SceneType { get; set; } = string.Empty;
    public RunSnapshot Run { get; set; } = new();
    public BattleSnapshot? Battle { get; set; }
    public MapSnapshot? Map { get; set; }
    public RewardSnapshot? Reward { get; set; }
    public EventSnapshot? Event { get; set; }
    public List<LegalActionSnapshot> LegalActions { get; set; } = new();
}

public sealed class RunSnapshot
{
    public int Floor { get; set; }
    public int PlayerHp { get; set; }
    public int MaxHp { get; set; }
    public int BattlesWon { get; set; }
    public int PotionCharges { get; set; }
    public string SelectedDeckPresetId { get; set; } = string.Empty;
    public bool HasCustomDeckOverride { get; set; }
    public List<string> DeckCardIds { get; set; } = new();
    public List<string> RelicIds { get; set; } = new();
    public List<string> PotionIds { get; set; } = new();
    public string PendingEncounterType { get; set; } = string.Empty;
    public string PendingEventId { get; set; } = string.Empty;
}

public sealed class BattleSnapshot
{
    public int Turn { get; set; }
    public int Energy { get; set; }
    public int MaxEnergy { get; set; }
    public bool BattleEnded { get; set; }
    public bool InputLocked { get; set; }
    public int DrawPileCount { get; set; }
    public int DiscardPileCount { get; set; }
    public int SelectedEnemyIndex { get; set; }
    public PlayerBattleSnapshot Player { get; set; } = new();
    public List<CardSnapshot> Hand { get; set; } = new();
    public List<EnemyBattleSnapshot> Enemies { get; set; } = new();
}

public sealed class PlayerBattleSnapshot
{
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Block { get; set; }
    public int Strength { get; set; }
    public int Vulnerable { get; set; }
}

public sealed class EnemyBattleSnapshot
{
    public int EnemyIndex { get; set; }
    public string ArchetypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Block { get; set; }
    public int Strength { get; set; }
    public int Vulnerable { get; set; }
    public bool IsAlive { get; set; }
    public bool IsSelected { get; set; }
    public string IntentType { get; set; } = string.Empty;
    public int IntentValue { get; set; }
    public string IntentText { get; set; } = string.Empty;
}

public sealed class CardSnapshot
{
    public int HandIndex { get; set; }
    public string CardId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Cost { get; set; }
    public bool RequiresEnemyTarget { get; set; }
    public bool IsPlayable { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class MapSnapshot
{
    public int CurrentRow { get; set; }
    public int CurrentColumn { get; set; }
    public List<MapRowSnapshot> Rows { get; set; } = new();
}

public sealed class MapRowSnapshot
{
    public int RowIndex { get; set; }
    public List<MapNodeSnapshot> Nodes { get; set; } = new();
}

public sealed class MapNodeSnapshot
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool CanSelect { get; set; }
    public List<int> NextColumns { get; set; } = new();
}

public sealed class RewardSnapshot
{
    public string Mode { get; set; } = string.Empty;
    public List<string> RewardTypes { get; set; } = new();
    public List<RewardOptionSnapshot> CardOptions { get; set; } = new();
    public List<RewardOptionSnapshot> RelicOptions { get; set; } = new();
}

public sealed class RewardOptionSnapshot
{
    public int OptionIndex { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class EventSnapshot
{
    public string EventId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EventOptionSnapshot> Options { get; set; } = new();
}

public sealed class EventOptionSnapshot
{
    public int OptionIndex { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class LegalActionSnapshot
{
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new();
}
