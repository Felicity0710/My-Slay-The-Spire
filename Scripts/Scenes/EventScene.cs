using Godot;
using System.Collections.Generic;

public partial class EventScene : Control
{
    private Label _titleLabel = null!;
    private Label _descLabel = null!;
    private Button _option1Button = null!;
    private Button _option2Button = null!;

    public override void _Ready()
    {
        GetNode<GameState>("/root/GameState").SetUiPhase("event");
        _titleLabel = GetNode<Label>("%TitleLabel");
        _descLabel = GetNode<Label>("%DescLabel");
        _option1Button = GetNode<Button>("%Option1Button");
        _option2Button = GetNode<Button>("%Option2Button");

        BindEvent();
    }

    private void BindEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        var id = state.PendingEventId;

        if (id == "shrine")
        {
            _titleLabel.Text = "Ancient Shrine";
            _descLabel.Text = "A quiet shrine hums with energy.";
            _option1Button.Text = "Pray: +5 Max HP and heal 5";
            _option2Button.Text = "Take Relic: Lose 8 HP, gain random relic";
            _option1Button.Pressed += ShrinePray;
            _option2Button.Pressed += ShrineRelic;
        }
        else
        {
            _titleLabel.Text = "Shady Dealer";
            _descLabel.Text = "A dealer offers a risky bargain.";
            _option1Button.Text = "Buy Card: Lose 6 HP, add Quick Slash";
            _option2Button.Text = "Refuse: Gain nothing";
            _option1Button.Pressed += DealerBuy;
            _option2Button.Pressed += LeaveEvent;
        }
    }

    private void ShrinePray()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.GainMaxHp(5);
        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void ShrineRelic()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 8);
        state.RollRelicOptions(1);
        if (state.PendingRelicOptions.Count > 0)
        {
            state.AddRelic(state.PendingRelicOptions[0]);
            state.PendingRelicOptions.Clear();
        }

        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void DealerBuy()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 6);
        state.AddCardToDeck("quick_slash");
        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void LeaveEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    public EventSnapshot BuildEventSnapshot()
    {
        var state = GetNode<GameState>("/root/GameState");
        var snapshot = new EventSnapshot
        {
            EventId = state.PendingEventId
        };

        if (state.PendingEventId == "shrine")
        {
            snapshot.Title = "Ancient Shrine";
            snapshot.Description = "A quiet shrine hums with energy.";
            snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 0, Label = "Pray: +5 Max HP and heal 5" });
            snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 1, Label = "Take Relic: Lose 8 HP, gain random relic" });
            return snapshot;
        }

        snapshot.Title = "Shady Dealer";
        snapshot.Description = "A dealer offers a risky bargain.";
        snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 0, Label = "Buy Card: Lose 6 HP, add Quick Slash" });
        snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 1, Label = "Refuse: Gain nothing" });
        return snapshot;
    }

    public List<LegalActionSnapshot> BuildLegalActions()
    {
        var snapshot = BuildEventSnapshot();
        var actions = new List<LegalActionSnapshot>();
        foreach (var option in snapshot.Options)
        {
            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_event_option",
                Label = option.Label,
                Parameters = new Dictionary<string, object?>
                {
                    ["optionIndex"] = option.OptionIndex
                }
            });
        }

        return actions;
    }

    public string? TryChooseEventOptionExternally(int? optionIndex, string? eventOption)
    {
        var state = GetNode<GameState>("/root/GameState");
        var normalized = eventOption?.Trim().ToLowerInvariant() ?? string.Empty;
        if (state.PendingEventId == "shrine")
        {
            if (optionIndex == 0 || normalized == "pray")
            {
                ShrinePray();
                return null;
            }

            if (optionIndex == 1 || normalized == "relic")
            {
                ShrineRelic();
                return null;
            }

            return $"Unsupported shrine option '{eventOption ?? optionIndex?.ToString() ?? string.Empty}'.";
        }

        if (optionIndex == 0 || normalized == "buy")
        {
            DealerBuy();
            return null;
        }

        if (optionIndex == 1 || normalized == "leave" || normalized == "refuse")
        {
            LeaveEvent();
            return null;
        }

        return $"Unsupported event option '{eventOption ?? optionIndex?.ToString() ?? string.Empty}'.";
    }
}
