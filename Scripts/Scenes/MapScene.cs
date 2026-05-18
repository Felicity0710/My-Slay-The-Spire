using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class MapScene : Control
{
    private const float MapCanvasMinWidth = 760f;
    private const float MapCanvasMaxWidth = 1220f;
    private const float MapCanvasBaseHeight = 980f;
    private const float MapViewportHorizontalPadding = 64f;

    private const float MapZoomMin = 0.6f;
    private const float MapZoomMax = 2.0f;
    private const float MapZoomStep = 1.12f;

    private float _zoom = 1f;
    private Vector2 _baseCanvasSize = new(MapCanvasMinWidth, MapCanvasBaseHeight);
    private bool _isPanningMap;

    private Label _titleLabel = null!;
    private Label _runInfoLabel = null!;
    private Label _statusLabel = null!;
    private Label _relicLabel = null!;
    private HBoxContainer _relicIcons = null!;
    private Label _legendLabel = null!;
    private ScrollContainer _mapScroll = null!;
    private MapCanvas _mapCanvas = null!;
    private Button _menuButton = null!;

    private Control _merchantFledOverlay = null!;
    private Label _merchantFledTitleLabel = null!;
    private Label _merchantFledSubtitleLabel = null!;
    private bool _isPlayingMerchantFledTransition;

    public override void _Ready()
    {
        var stateAtEntry = GetNode<GameState>("/root/GameState");
        if (stateAtEntry.RunCompleted)
        {
            CallDeferred(nameof(GoToVictoryScene));
            return;
        }

        _titleLabel = GetNode<Label>("Margin/VBox/TopBar/TitlePanel/TitleVBox/Title");
        stateAtEntry.SetUiPhase("map");

        // Re-save whenever we land back on the map (after StartNewRun, after a
        // battle/event/etc resolves, after Re-enter). Without this, the save
        // remains pinned to "right before the last node click", so picking
        // node A and quitting after winning would restore the player BEFORE A
        // instead of after.
        stateAtEntry.TryWriteSave("res://Scenes/MapScene.tscn");

        AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());
        _runInfoLabel = GetNode<Label>("%RunInfoLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _relicLabel = GetNode<Label>("%RelicLabel");
        _relicIcons = GetNode<HBoxContainer>("%RelicIcons");
        _legendLabel = GetNode<Label>("Margin/VBox/Legend");
        _mapScroll = GetNode<ScrollContainer>("%MapScroll");
        _mapCanvas = GetNode<MapCanvas>("%MapCanvas");
        _menuButton = GetNode<Button>("%MenuButton");

        _merchantFledOverlay = GetNode<Control>("%MerchantFledOverlay");
        _merchantFledTitleLabel = GetNode<Label>("%MerchantFledTitle");
        _merchantFledSubtitleLabel = GetNode<Label>("%MerchantFledSubtitle");
        _merchantFledOverlay.Visible = false;
        _merchantFledOverlay.Modulate = new Color(1, 1, 1, 0);

        SetupDeckViewerUi();
        _menuButton.Pressed += OnMenuPressed;
        _mapScroll.Resized += OnMapViewportResized;

        // Let mouse events flow through MapCanvas to the ScrollContainer; otherwise
        // MapCanvas's default Stop filter consumes wheel and right-click before we
        // can see them. The map node Buttons are siblings under it and still get
        // their own clicks since they're separate Controls.
        _mapCanvas.MouseFilter = MouseFilterEnum.Pass;
        _mapScroll.GuiInput += OnMapScrollGuiInput;

        LocalizationSettings.LanguageChanged += OnLanguageChanged;

        RefreshUi();
        CallDeferred(nameof(ApplyInitialMapLayout));
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_mapScroll))
        {
            _mapScroll.Resized -= OnMapViewportResized;
            _mapScroll.GuiInput -= OnMapScrollGuiInput;
        }

        TearDownDeckViewerUi();
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void OnMapScrollGuiInput(InputEvent @event)
    {
        if (_isPlayingMerchantFledTransition)
        {
            return;
        }

        if (@event is not InputEventMouseButton mb)
        {
            return;
        }

        // Wheel events come through as Pressed=true once (no release).
        if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelUp)
        {
            ZoomBy(MapZoomStep);
            _mapScroll.AcceptEvent();
            return;
        }

        if (mb.Pressed && mb.ButtonIndex == MouseButton.WheelDown)
        {
            ZoomBy(1f / MapZoomStep);
            _mapScroll.AcceptEvent();
            return;
        }

        if (mb.ButtonIndex == MouseButton.Right)
        {
            if (mb.Pressed)
            {
                _isPanningMap = true;
            }
            else if (_isPanningMap)
            {
                _isPanningMap = false;
            }
            _mapScroll.AcceptEvent();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsInstanceValid(_mapScroll))
        {
            return;
        }

        // Right-mouse release ends panning even if the cursor is now off the map.
        if (@event is InputEventMouseButton release
            && release.ButtonIndex == MouseButton.Right
            && !release.Pressed
            && _isPanningMap)
        {
            _isPanningMap = false;
            GetViewport().SetInputAsHandled();
            return;
        }

        // While panning, mouse motion anywhere on screen drags the map.
        if (_isPanningMap && @event is InputEventMouseMotion motion)
        {
            _mapScroll.ScrollVertical -= Mathf.RoundToInt(motion.Relative.Y);
            _mapScroll.ScrollHorizontal -= Mathf.RoundToInt(motion.Relative.X);
            GetViewport().SetInputAsHandled();
        }
    }

    private void GoToVictoryScene()
    {
        GetTree().ChangeSceneToFile("res://Scenes/VictoryScene.tscn");
    }

    private void ApplyInitialMapLayout()
    {
        if (!IsInstanceValid(_mapCanvas))
        {
            return;
        }

        UpdateMapCanvasWidth();
        BuildTreasureMap(GetNode<GameState>("/root/GameState"));
    }

    private void OnMapViewportResized()
    {
        if (!IsInstanceValid(_mapCanvas))
        {
            return;
        }

        UpdateMapCanvasWidth();
        BuildTreasureMap(GetNode<GameState>("/root/GameState"));
    }

    private void UpdateMapCanvasWidth()
    {
        var viewportWidth = _mapScroll.Size.X;
        if (viewportWidth <= 0f)
        {
            return;
        }

        var targetWidth = Mathf.Clamp(
            viewportWidth - MapViewportHorizontalPadding,
            MapCanvasMinWidth,
            MapCanvasMaxWidth);

        _baseCanvasSize = new Vector2(targetWidth, MapCanvasBaseHeight);
        ApplyZoomTransform();
    }

    private void ApplyZoomTransform()
    {
        if (!IsInstanceValid(_mapCanvas))
        {
            return;
        }

        // Reset any leftover Scale from earlier prototypes; geometry is baked into
        // positions and sizes at rebuild time instead.
        _mapCanvas.Scale = Vector2.One;
        _mapCanvas.PivotOffset = Vector2.Zero;
        _mapCanvas.CustomMinimumSize = new Vector2(
            _baseCanvasSize.X * _zoom,
            _baseCanvasSize.Y * _zoom);
    }

    private void ZoomBy(float multiplier)
    {
        var newZoom = Mathf.Clamp(_zoom * multiplier, MapZoomMin, MapZoomMax);
        if (Mathf.IsEqualApprox(newZoom, _zoom))
        {
            return;
        }

        _zoom = newZoom;
        ApplyZoomTransform();

        // Rebuild the map so node positions / sizes pick up the new zoom factor.
        var state = GetNodeOrNull<GameState>("/root/GameState");
        if (state != null)
        {
            BuildTreasureMap(state);
        }

        // Always recenter after zoom — BuildTreasureMap only refocuses if there
        // are selectable nodes on the current row; this guarantees centering.
        CallDeferred(MethodName.CenterMapHorizontally);
    }

    // Returns the SCALED canvas size used by BuildNodePositions — the entire
    // map (background, lines, buttons) is positioned in this scaled space.
    private Vector2 GetEffectiveMapCanvasSize()
    {
        return new Vector2(_baseCanvasSize.X * _zoom, _baseCanvasSize.Y * _zoom);
    }

    private void RefreshUi(string status = "")
    {
        RefreshStaticText();

        if (string.IsNullOrWhiteSpace(status))
        {
            status = LocalizationService.Get(
                "ui.map.status_select_path",
                "Choose a route upward. Available nodes are highlighted.");
        }

        var state = GetNode<GameState>("/root/GameState");

        var potionSummary = state.PotionIds.Count == 0
            ? LocalizationService.Get("ui.map.potion_none", "None")
            : string.Join(", ", state.PotionIds.ConvertAll(id => PotionData.CreateById(id).Name));

        // Each stat is prefixed with a Unicode icon so the row scans like a
        // dashboard rather than a wall of text.
        _runInfoLabel.Text = string.Join("    ", new[]
        {
            "🗺 " + LocalizationService.Format("ui.map.act", "Act {0}/{1}", state.Act, MapProgressionRules.MaxActs),
            "🪜 " + LocalizationService.Format("ui.map.floor", "Floor {0}", state.Floor),
            "❤ " + LocalizationService.Format("ui.map.hp", "HP {0}/{1}", state.PlayerHp, state.MaxHp),
            "💰 " + LocalizationService.Format("ui.map.gold", "Gold {0}", state.Gold),
            "🎴 " + LocalizationService.Format("ui.map.deck", "Deck {0}", state.DeckCardIds.Count),
            "🧪 " + LocalizationService.Format("ui.map.potions", "Potions {0} ({1})", state.PotionCharges, potionSummary),
            "🏆 " + LocalizationService.Format("ui.map.wins", "Wins {0}", state.BattlesWon)
        });

        _statusLabel.Text = status;

        _relicLabel.Text = LocalizationService.Get("ui.map.relics_prefix", "💠 Relics:");
        RefreshRelicIcons(state);

        UpdateMapCanvasWidth();
        RefreshDeckViewerUi(state);
        BuildTreasureMap(state);
    }

    private void RefreshRelicIcons(GameState state)
    {
        foreach (var child in _relicIcons.GetChildren())
        {
            child.QueueFree();
        }

        if (state.RelicIds.Count == 0)
        {
            var none = new Label
            {
                Text = LocalizationService.Get("ui.map.relics_none", "None"),
                Modulate = new Color(1f, 1f, 1f, 0.55f)
            };
            _relicIcons.AddChild(none);
            return;
        }

        foreach (var relicId in state.RelicIds)
        {
            var relic = RelicData.CreateById(relicId);
            var iconPath = CombatVisualCatalog.GetRelicIconPath(relicId);
            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(28, 28),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                TooltipText = relic.ToRelicText(),
                MouseFilter = MouseFilterEnum.Stop,
                Texture = ResourceLoader.Exists(iconPath) ? GD.Load<Texture2D>(iconPath) : null
            };
            _relicIcons.AddChild(icon);
        }
    }

    private void RefreshStaticText()
    {
        _titleLabel.Text = LocalizationService.Get("ui.map.title", "Ancient Route Map");
        _legendLabel.Text = LocalizationService.Get("ui.map.legend", "⚔ Normal  ☠ Elite  ◆ Event  ♥ Rest  $ Shop");
        _menuButton.Text = LocalizationService.Get("ui.map.back_to_menu", "Back To Menu");
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
                var nodeSize = (canSelect ? 70f : 54f) * _zoom;
                nodeButton.Size = new Vector2(nodeSize, nodeSize);
                nodeButton.CustomMinimumSize = new Vector2(nodeSize, nodeSize);
                nodeButton.Text = state.MapNodeSymbol(nodeType);
                nodeButton.TooltipText = $"{row + 1:00}F - {state.MapNodeLabel(nodeType)}";
                nodeButton.Position = positions[row][col] - nodeButton.Size / 2f;
                nodeButton.Disabled = !canSelect;
                var fontSize = Mathf.RoundToInt((canSelect ? 34 : 26) * _zoom);
                nodeButton.AddThemeFontSizeOverride("font_size", fontSize);
                nodeButton.AddThemeColorOverride("font_color", canSelect ? new Color(1f, 0.98f, 0.88f) : Colors.White);
                nodeButton.AddThemeColorOverride("font_focus_color", canSelect ? new Color(1f, 0.98f, 0.88f) : Colors.White);

                nodeButton.Modulate = NodeTint(nodeType, row, state.CurrentMapRow, canSelect);

                if (canSelect)
                {
                    var captureCol = col;
                    nodeButton.Pressed += () => OnNodePressed(captureCol);
                }

                _mapCanvas.AddChild(nodeButton);
            }
        }

        EnsureCurrentSelectableRowVisible(positions, state);
    }

    private void EnsureCurrentSelectableRowVisible(List<List<Vector2>> positions, GameState state)
    {
        if (positions.Count == 0 || state.CurrentMapRow < 0 || state.CurrentMapRow >= positions.Count)
        {
            return;
        }

        var focusYs = new List<float>();
        for (var col = 0; col < positions[state.CurrentMapRow].Count; col++)
        {
            if (state.CanChooseMapNode(state.CurrentMapRow, col))
            {
                focusYs.Add(positions[state.CurrentMapRow][col].Y);
            }
        }

        if (focusYs.Count == 0)
        {
            return;
        }

        var averageY = 0f;
        foreach (var y in focusYs)
        {
            averageY += y;
        }

        averageY /= focusYs.Count;
        CallDeferred(MethodName.ApplyScrollFocus, averageY);
    }

    private void ApplyScrollFocus(float focusY)
    {
        // focusY already comes from BuildNodePositions, which now operates in the
        // scaled canvas space, so no extra zoom multiplication needed here.
        var mapSize = GetEffectiveMapCanvasSize();
        var viewHeight = _mapScroll.Size.Y;
        var maxVerticalScroll = Mathf.Max(0f, mapSize.Y - viewHeight);
        var desired = Mathf.Clamp(focusY - viewHeight * 0.72f, 0f, maxVerticalScroll);
        _mapScroll.ScrollVertical = Mathf.RoundToInt(desired);
        CenterMapHorizontally();
    }

    private void CenterMapHorizontally()
    {
        var mapSize = GetEffectiveMapCanvasSize();
        var viewWidth = _mapScroll.Size.X;
        var maxHorizontalScroll = Mathf.Max(0f, mapSize.X - viewWidth);
        _mapScroll.ScrollHorizontal = maxHorizontalScroll <= 0f
            ? 0
            : Mathf.RoundToInt(maxHorizontalScroll * 0.5f);
    }

    private List<List<Vector2>> BuildNodePositions(GameState state)
    {
        var mapSize = GetEffectiveMapCanvasSize();
        var mapWidth = mapSize.X;
        var mapHeight = mapSize.Y;

        var rows = state.MapLayout.Count;
        var horizontalMargin = 72f;
        var verticalMargin = 50f;
        var yStep = (mapHeight - verticalMargin * 2f) / Math.Max(1, rows - 1);

        var pos = new List<List<Vector2>>(rows);
        for (var row = 0; row < rows; row++)
        {
            var cols = state.MapLayout[row].Count;
            var rowPos = new List<Vector2>(cols);
            var baseY = mapHeight - verticalMargin - yStep * row;

            if (cols == 1)
            {
                // Center the single intro/boss node.
                var x = mapWidth * 0.5f;
                var y = Mathf.Clamp(baseY, verticalMargin - 12f, mapHeight - verticalMargin + 12f);
                rowPos.Add(new Vector2(x, y));
            }
            else
            {
                var xStep = (mapWidth - horizontalMargin * 2f) / Math.Max(1, cols - 1);
                for (var col = 0; col < cols; col++)
                {
                    var baseX = horizontalMargin + xStep * col;

                    var jitterX = Noise(row, col, 17) * xStep * 0.34f;
                    var jitterY = Noise(row, col, 41) * yStep * 0.28f;

                    var x = Mathf.Clamp(baseX + jitterX, horizontalMargin - 16f, mapWidth - horizontalMargin + 16f);
                    var y = Mathf.Clamp(baseY + jitterY, verticalMargin - 12f, mapHeight - verticalMargin + 12f);
                    rowPos.Add(new Vector2(x, y));
                }
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
            MapNodeType.Intro => new Color(0.55f, 0.85f, 0.95f, alpha),
            MapNodeType.Boss => new Color(0.95f, 0.20f, 0.22f, alpha),
            _ => new Color(0.85f, 0.85f, 0.85f, alpha)
        };

        if (canSelect)
        {
            return baseColor.Lightened(0.3f);
        }

        return baseColor;
    }

    private void OnNodePressed(int column)
    {
        if (_isPlayingMerchantFledTransition)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");

        // Snapshot the map state RIGHT BEFORE consuming this click. "Re-enter
        // current node" from inside a node scene restores this snapshot and
        // returns us here, with the same RNG cursor — picking the same node
        // again reproduces every random roll exactly.
        state.SaveNodeEntrySnapshot("res://Scenes/MapScene.tscn");

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
                GetTree().ChangeSceneToFile("res://Scenes/RestScene.tscn");
                break;
            case MapNodeType.Shop:
                if (state.MerchantFled)
                {
                    PlayMerchantFledTransition(state);
                }
                else
                {
                    GetTree().ChangeSceneToFile("res://Scenes/ShopScene.tscn");
                }
                break;
            case MapNodeType.Intro:
                // Heal to full when stepping onto the act intro, then resolve the
                // special intro event in its own scene.
                state.PlayerHp = state.MaxHp;
                GetTree().ChangeSceneToFile("res://Scenes/IntroEventScene.tscn");
                break;
            case MapNodeType.Boss:
                state.BeginEncounter(MapNodeType.Boss);
                GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
                break;
        }
    }

    private void PlayMerchantFledTransition(GameState state)
    {
        _isPlayingMerchantFledTransition = true;
        _merchantFledTitleLabel.Text = LocalizationService.Get(
            "ui.map.merchant_fled_title",
            "The shop is deserted");
        _merchantFledSubtitleLabel.Text = LocalizationService.Get(
            "ui.map.merchant_fled_subtitle",
            "The merchant has fled...");

        _merchantFledOverlay.Visible = true;
        _merchantFledOverlay.Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween();
        tween.TweenProperty(_merchantFledOverlay, "modulate:a", 1.0, 0.35);
        tween.TweenInterval(1.2);
        tween.TweenProperty(_merchantFledOverlay, "modulate:a", 0.0, 0.45);
        tween.TweenCallback(Callable.From(() => OnMerchantFledTransitionFinished(state)));
    }

    private void OnMerchantFledTransitionFinished(GameState state)
    {
        _merchantFledOverlay.Visible = false;
        _isPlayingMerchantFledTransition = false;
        state.ResolveShopNode();
        RefreshUi(LocalizationService.Get(
            "ui.map.shop_empty_status",
            "The merchant has fled. The shop is deserted."));
    }

    private void OnLanguageChanged()
    {
        RefreshUi();
    }

    public string? TryChooseNodeExternally(int column)
    {
        var state = GetNode<GameState>("/root/GameState");
        if (!state.CanChooseMapNode(state.CurrentMapRow, column))
        {
            return $"Map column {column} is not selectable right now.";
        }

        OnNodePressed(column);
        return null;
    }

    private void OnMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
