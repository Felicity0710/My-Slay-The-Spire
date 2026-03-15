using Godot;
using System.Text;

public partial class MapScene : Control
{
    private Label _runInfoLabel = null!;
    private Label _statusLabel = null!;
    private Label _relicLabel = null!;

    private Button _nodeButton1 = null!;
    private Button _nodeButton2 = null!;
    private Button _nodeButton3 = null!;

    public override void _Ready()
    {
        _runInfoLabel = GetNode<Label>("%RunInfoLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _relicLabel = GetNode<Label>("%RelicLabel");

        _nodeButton1 = GetNode<Button>("%NodeButton1");
        _nodeButton2 = GetNode<Button>("%NodeButton2");
        _nodeButton3 = GetNode<Button>("%NodeButton3");

        _nodeButton1.Pressed += () => OnNodePressed(0);
        _nodeButton2.Pressed += () => OnNodePressed(1);
        _nodeButton3.Pressed += () => OnNodePressed(2);

        GetNode<Button>("%MenuButton").Pressed += OnMenuPressed;

        RefreshUi();
    }

    private void RefreshUi()
    {
        var state = GetNode<GameState>("/root/GameState");

        if (state.CurrentMapOptions.Count == 0)
        {
            state.GenerateMapOptions();
        }

        _runInfoLabel.Text =
            $"Floor: {state.Floor}    HP: {state.PlayerHp}/{state.MaxHp}    Deck: {state.DeckCardIds.Count}    Wins: {state.BattlesWon}";

        _statusLabel.Text = "Choose your next node.";

        var relicText = new StringBuilder("Relics: ");
        if (state.RelicIds.Count == 0)
        {
            relicText.Append("None");
        }
        else
        {
            for (var i = 0; i < state.RelicIds.Count; i++)
            {
                if (i > 0)
                {
                    relicText.Append(", ");
                }

                relicText.Append(RelicData.CreateById(state.RelicIds[i]).Name);
            }
        }

        _relicLabel.Text = relicText.ToString();

        BindNodeButton(_nodeButton1, state, 0);
        BindNodeButton(_nodeButton2, state, 1);
        BindNodeButton(_nodeButton3, state, 2);
    }

    private void BindNodeButton(Button button, GameState state, int index)
    {
        if (index >= state.CurrentMapOptions.Count)
        {
            button.Visible = false;
            return;
        }

        button.Visible = true;
        var nodeType = state.CurrentMapOptions[index];
        button.Text = state.MapNodeLabel(nodeType);
    }

    private void OnNodePressed(int index)
    {
        var state = GetNode<GameState>("/root/GameState");
        if (index >= state.CurrentMapOptions.Count)
        {
            return;
        }

        var nodeType = state.CurrentMapOptions[index];

        switch (nodeType)
        {
            case MapNodeType.NormalBattle:
            case MapNodeType.EliteBattle:
                state.BeginEncounter(nodeType);
                GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
                break;
            case MapNodeType.Event:
                state.BeginRandomEvent();
                GetTree().ChangeSceneToFile("res://Scenes/EventScene.tscn");
                break;
            case MapNodeType.Rest:
                state.ResolveRestNode();
                _statusLabel.Text = "You rest and recover 18 HP.";
                RefreshUi();
                break;
        }
    }

    private void OnMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
