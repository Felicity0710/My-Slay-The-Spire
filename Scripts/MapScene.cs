using Godot;
using System.Text;

public partial class MapScene : Control
{
    private Label _runInfoLabel = null!;
    private Label _statusLabel = null!;
    private Label _relicLabel = null!;
    private VBoxContainer _mapRows = null!;

    public override void _Ready()
    {
        _runInfoLabel = GetNode<Label>("%RunInfoLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _relicLabel = GetNode<Label>("%RelicLabel");
        _mapRows = GetNode<VBoxContainer>("%MapRows");

        GetNode<Button>("%MenuButton").Pressed += OnMenuPressed;

        RefreshUi();
    }

    private void RefreshUi()
    {
        var state = GetNode<GameState>("/root/GameState");

        _runInfoLabel.Text =
            $"Floor: {state.Floor}    HP: {state.PlayerHp}/{state.MaxHp}    Deck: {state.DeckCardIds.Count}    Wins: {state.BattlesWon}";

        _statusLabel.Text = "选择一条路线向上爬塔。当前可选节点会高亮。";

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

        BuildMapRows(state);
    }

    private void BuildMapRows(GameState state)
    {
        foreach (var child in _mapRows.GetChildren())
        {
            child.QueueFree();
        }

        for (var row = state.MapLayout.Count - 1; row >= 0; row--)
        {
            var rowBox = new HBoxContainer();
            rowBox.Alignment = BoxContainer.AlignmentMode.Center;
            rowBox.AddThemeConstantOverride("separation", 8);

            var rowLabel = new Label();
            rowLabel.Text = $"{row + 1:00}";
            rowLabel.CustomMinimumSize = new Vector2(36, 0);
            rowBox.AddChild(rowLabel);

            for (var col = 0; col < state.MapLayout[row].Count; col++)
            {
                var nodeType = state.GetMapNodeType(row, col);
                var button = new Button();
                button.CustomMinimumSize = new Vector2(64, 44);
                button.Text = $"{state.MapNodeSymbol(nodeType)}\n{state.MapNodeLabel(nodeType)}";

                var isCurrentRow = row == state.CurrentMapRow;
                var canSelect = state.CanChooseMapNode(row, col);

                button.Disabled = !canSelect;
                if (!isCurrentRow)
                {
                    button.Modulate = new Color(0.55f, 0.55f, 0.55f, 0.95f);
                }
                else if (canSelect)
                {
                    button.Modulate = new Color(1f, 0.9f, 0.45f, 1f);
                }

                if (canSelect)
                {
                    var captureCol = col;
                    button.Pressed += () => OnNodePressed(captureCol);
                }

                rowBox.AddChild(button);
            }

            _mapRows.AddChild(rowBox);
        }
    }

    private void OnNodePressed(int column)
    {
        var state = GetNode<GameState>("/root/GameState");
        if (!state.ChooseMapNode(column, out var nodeType))
        {
            return;
        }

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
                _statusLabel.Text = "你在篝火休息，恢复了 18 点生命。";
                RefreshUi();
                break;
            case MapNodeType.Shop:
                state.ResolveShopNode();
                _statusLabel.Text = "你进入商店补给，获得了恢复或额外卡牌。";
                RefreshUi();
                break;
        }
    }

    private void OnMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
