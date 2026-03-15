using Godot;
using System.Collections.Generic;

public partial class RewardScene : Control
{
    private readonly List<Button> _optionButtons = new();

    public override void _Ready()
    {
        _optionButtons.Add(GetNode<Button>("%OptionButton1"));
        _optionButtons.Add(GetNode<Button>("%OptionButton2"));
        _optionButtons.Add(GetNode<Button>("%OptionButton3"));

        GetNode<Button>("%SkipButton").Pressed += OnSkipPressed;

        BindOptions();
    }

    private void BindOptions()
    {
        var state = GetNode<GameState>("/root/GameState");
        if (state.PendingRewardOptions.Count == 0)
        {
            state.RollRewardOptions(3);
        }

        for (var i = 0; i < _optionButtons.Count; i++)
        {
            var button = _optionButtons[i];
            if (i >= state.PendingRewardOptions.Count)
            {
                button.Visible = false;
                continue;
            }

            var cardId = state.PendingRewardOptions[i];
            var card = CardData.CreateById(cardId);
            button.Text = card.ToCardText();
            button.Pressed += () => OnPickCard(cardId);
        }
    }

    private void OnPickCard(string cardId)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.AddCardToDeck(cardId);
        state.PendingRewardOptions.Clear();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void OnSkipPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PendingRewardOptions.Clear();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }
}
