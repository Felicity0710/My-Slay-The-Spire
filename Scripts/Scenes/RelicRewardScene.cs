using Godot;
using System.Collections.Generic;

public partial class RelicRewardScene : Control
{
    private readonly List<Button> _optionButtons = new();

    public override void _Ready()
    {
        _optionButtons.Add(GetNode<Button>("%OptionButton1"));
        _optionButtons.Add(GetNode<Button>("%OptionButton2"));
        _optionButtons.Add(GetNode<Button>("%OptionButton3"));

        BindOptions();
    }

    private void BindOptions()
    {
        var state = GetNode<GameState>("/root/GameState");

        if (state.PendingRelicOptions.Count == 0)
        {
            state.RollRelicOptions(3);
        }

        for (var i = 0; i < _optionButtons.Count; i++)
        {
            var button = _optionButtons[i];
            if (i >= state.PendingRelicOptions.Count)
            {
                button.Visible = false;
                continue;
            }

            var relicId = state.PendingRelicOptions[i];
            var relic = RelicData.CreateById(relicId);
            button.Text = relic.ToRelicText();
            button.Pressed += () => OnPickRelic(relicId);
        }
    }

    private void OnPickRelic(string relicId)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.AddRelic(relicId);
        state.PendingRelicOptions.Clear();
        GetTree().ChangeSceneToFile("res://Scenes/RewardScene.tscn");
    }
}
