using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class MapScene : Control
{
    private Label _runInfoLabel = null!;
    private Label _statusLabel = null!;
    private Label _relicLabel = null!;
    private MapCanvas _mapCanvas = null!;

    public override void _Ready()
    {
        _runInfoLabel = GetNode<Label>("%RunInfoLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _relicLabel = GetNode<Label>("%RelicLabel");
        _mapCanvas = GetNode<MapCanvas>("%MapCanvas");

        GetNode<Button>("%MenuButton").Pressed += OnMenuPressed;

        RefreshUi();
    }

    private void RefreshUi(string status = "选择一条路线向上爬塔。当前可选节点会高亮。")
    {
        var state = GetNode<GameState>("/root/GameState");

        _runInfoLabel.Text =
            $"Floor: {state.Floor}    HP: {state.PlayerHp}/{state.MaxHp}    Deck: {state.DeckCardIds.Count}    Wins: {state.BattlesWon}";

        _statusLabel.Text = status;

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

        BuildTreasureMap(state);
    }

    private void BuildTreasureMap(GameState state)
    {
        foreach (var child in _mapCanvas.GetChildren())
        {
            child.QueueFree();
        }

        var positions = BuildNodePositions(state);
        var lines = new List<(Vector2 Start, Vector2 End, Color Tint)>();

        for (var row = 0; row < state.MapConnections.Count; row++)
        {
            for (var col = 0; col < state.MapConnections[row].Count; col++)
            {
                var start = positions[row][col];
                foreach (var nextCol in state.MapConnections[row][col])
                {
                    var end = positions[row + 1][nextCol];
                    var tint = row == state.CurrentMapRow && state.CurrentMapColumn == col
                        ? new Color(0.95f, 0.84f, 0.48f, 0.95f)
                        : new Color(0.78f, 0.68f, 0.49f, 0.8f);

                    lines.Add((start, end, tint));
                }
            }
        }

        _mapCanvas.SetLines(lines);

        for (var row = 0; row < state.MapLayout.Count; row++)
        {
            for (var col = 0; col < state.MapLayout[row].Count; col++)
            {
                var nodeType = state.GetMapNodeType(row, col);
                var canSelect = state.CanChooseMapNode(row, col);

                var nodeButton = new Button();
                nodeButton.Size = new Vector2(54, 54);
                nodeButton.CustomMinimumSize = new Vector2(54, 54);
                nodeButton.Text = state.MapNodeSymbol(nodeType);
                nodeButton.TooltipText = $"{row + 1:00}F - {state.MapNodeLabel(nodeType)}";
                nodeButton.Position = positions[row][col] - nodeButton.Size / 2f;
                nodeButton.Disabled = !canSelect;
                nodeButton.AddThemeFontSizeOverride("font_size", 26);

                nodeButton.Modulate = NodeTint(nodeType, row, state.CurrentMapRow, canSelect);

                if (canSelect)
                {
                    var captureCol = col;
                    nodeButton.Pressed += () => OnNodePressed(captureCol);
                }

                _mapCanvas.AddChild(nodeButton);
            }
        }
    }

    private List<List<Vector2>> BuildNodePositions(GameState state)
    {
        var mapWidth = _mapCanvas.Size.X;
        var mapHeight = _mapCanvas.Size.Y;

        var rows = state.MapLayout.Count;
        var cols = state.MapLayout[0].Count;

        var horizontalMargin = 72f;
        var verticalMargin = 50f;
        var xStep = (mapWidth - horizontalMargin * 2f) / Math.Max(1, cols - 1);
        var yStep = (mapHeight - verticalMargin * 2f) / Math.Max(1, rows - 1);

        var pos = new List<List<Vector2>>(rows);
        for (var row = 0; row < rows; row++)
        {
            var rowPos = new List<Vector2>(cols);
            for (var col = 0; col < cols; col++)
            {
                var baseX = horizontalMargin + xStep * col;
                var baseY = mapHeight - verticalMargin - yStep * row;

                var jitterX = Noise(row, col, 17) * xStep * 0.34f;
                var jitterY = Noise(row, col, 41) * yStep * 0.28f;

                var x = Mathf.Clamp(baseX + jitterX, horizontalMargin - 16f, mapWidth - horizontalMargin + 16f);
                var y = Mathf.Clamp(baseY + jitterY, verticalMargin - 12f, mapHeight - verticalMargin + 12f);
                rowPos.Add(new Vector2(x, y));
            }

            pos.Add(rowPos);
        }

        return pos;
    }

    private static float Noise(int row, int col, int salt)
    {
        unchecked
        {
            var seed = row * 73856093 ^ col * 19349663 ^ salt * 83492791;
            seed ^= seed >> 13;
            seed *= 1274126177;
            var value = (seed & 0x7fffffff) / (float)int.MaxValue;
            return value * 2f - 1f;
        }
    }

    private static Color NodeTint(MapNodeType type, int row, int currentRow, bool canSelect)
    {
        var alpha = row == currentRow ? 1f : 0.72f;
        if (!canSelect && row == currentRow)
        {
            alpha = 0.45f;
        }

        var baseColor = type switch
        {
            MapNodeType.NormalBattle => new Color(0.88f, 0.52f, 0.35f, alpha),
            MapNodeType.EliteBattle => new Color(0.89f, 0.28f, 0.31f, alpha),
            MapNodeType.Event => new Color(0.72f, 0.63f, 0.86f, alpha),
            MapNodeType.Rest => new Color(0.47f, 0.75f, 0.52f, alpha),
            MapNodeType.Shop => new Color(0.92f, 0.77f, 0.36f, alpha),
            _ => new Color(0.85f, 0.85f, 0.85f, alpha)
        };

        if (canSelect)
        {
            return baseColor.Lightened(0.14f);
        }

        return baseColor;
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
                RefreshUi("你在篝火休息，恢复了 18 点生命。继续向上探索。");
                break;
            case MapNodeType.Shop:
                state.ResolveShopNode();
                RefreshUi("你在商店完成补给，继续下一层路线。");
                break;
        }
    }

    private void OnMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
