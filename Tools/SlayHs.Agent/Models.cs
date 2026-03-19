using System.Text.Json.Nodes;

namespace SlayHs.Agent;

internal sealed class CommandEnvelope
{
    public string Command { get; set; } = string.Empty;
    public ActionEnvelope? Action { get; set; }
    public long? ExpectedStateVersion { get; set; }
}

internal sealed class ActionEnvelope
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
}

internal sealed class CommandResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
    public long StateVersion { get; set; }
    public SnapshotEnvelope? Snapshot { get; set; }
}

internal sealed class SnapshotEnvelope
{
    public long StateVersion { get; set; }
    public string SceneType { get; set; } = string.Empty;
    public RunEnvelope Run { get; set; } = new();
    public BattleEnvelope? Battle { get; set; }
    public MapEnvelope? Map { get; set; }
    public RewardEnvelope? Reward { get; set; }
    public EventEnvelope? Event { get; set; }
    public List<LegalActionEnvelope> LegalActions { get; set; } = [];
}

internal sealed class RunEnvelope
{
    public int Floor { get; set; }
    public int PlayerHp { get; set; }
    public int MaxHp { get; set; }
    public int BattlesWon { get; set; }
}

internal sealed class BattleEnvelope
{
    public int Turn { get; set; }
    public int Energy { get; set; }
    public bool BattleEnded { get; set; }
    public List<CardEnvelope> Hand { get; set; } = [];
    public List<EnemyEnvelope> Enemies { get; set; } = [];
}

internal sealed class CardEnvelope
{
    public int HandIndex { get; set; }
    public string CardId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Cost { get; set; }
    public bool RequiresEnemyTarget { get; set; }
    public bool IsPlayable { get; set; }
    public string Description { get; set; } = string.Empty;
}

internal sealed class EnemyEnvelope
{
    public int EnemyIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Hp { get; set; }
    public int Block { get; set; }
    public int Strength { get; set; }
    public int Vulnerable { get; set; }
    public bool IsAlive { get; set; }
    public string IntentType { get; set; } = string.Empty;
    public int IntentValue { get; set; }
}

internal sealed class MapEnvelope
{
    public int CurrentRow { get; set; }
    public List<MapRowEnvelope> Rows { get; set; } = [];
}

internal sealed class MapRowEnvelope
{
    public int RowIndex { get; set; }
    public List<MapNodeEnvelope> Nodes { get; set; } = [];
}

internal sealed class MapNodeEnvelope
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool CanSelect { get; set; }
}

internal sealed class RewardEnvelope
{
    public string Mode { get; set; } = string.Empty;
    public List<string> RewardTypes { get; set; } = [];
    public List<RewardOptionEnvelope> CardOptions { get; set; } = [];
    public List<RewardOptionEnvelope> RelicOptions { get; set; } = [];
}

internal sealed class RewardOptionEnvelope
{
    public int OptionIndex { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

internal sealed class EventEnvelope
{
    public string EventId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EventOptionEnvelope> Options { get; set; } = [];
}

internal sealed class EventOptionEnvelope
{
    public int OptionIndex { get; set; }
    public string Label { get; set; } = string.Empty;
}

internal sealed class LegalActionEnvelope
{
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public JsonObject Parameters { get; set; } = [];
}
