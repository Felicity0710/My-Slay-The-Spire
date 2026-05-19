using Godot;
using System.Collections.Generic;

public partial class RestScene : Control
{
    // Main view
    private Control _mainView = null!;
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Label _hpLabel = null!;
    private PanelContainer _restTile = null!;
    private Label _restNameLabel = null!;
    private Label _restDescLabel = null!;
    private PanelContainer _smithTile = null!;
    private Label _smithNameLabel = null!;
    private Label _smithDescLabel = null!;
    private Button _skipButton = null!;

    // Upgrade picker view
    private Control _upgradeView = null!;
    private Label _upgradeTitleLabel = null!;
    private Label _upgradeHintLabel = null!;
    private GridContainer _cardGrid = null!;
    private Label _emptyHintLabel = null!;
    private Button _confirmButton = null!;
    private Button _cancelButton = null!;

    // Preview modal (right-click brings this up)
    private Control _previewModal = null!;
    private Label _previewTitleLabel = null!;
    private Button _previewCloseButton = null!;
    private CenterContainer _previewCenter = null!;
    private CardView? _previewCardView;

    private readonly List<int> _eligibleDeckIndices = new();
    private readonly List<PanelContainer> _tileFrames = new();
    private int _selectedListIndex = -1;

    private static readonly StyleBoxFlat TileNormal = MakeTileStyle(new Color("8cc7d9"), 0.30f);
    private static readonly StyleBoxFlat TileSelected = MakeTileStyle(new Color("f5d04a"), 0.70f);

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("rest");
        AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());

        _mainView = GetNode<Control>("%MainView");
        _titleLabel = GetNode<Label>("%TitleLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _hpLabel = GetNode<Label>("%HpLabel");

        _restTile = GetNode<PanelContainer>("%RestTile");
        _restNameLabel = GetNode<Label>("%RestNameLabel");
        _restDescLabel = GetNode<Label>("%RestDescLabel");
        _smithTile = GetNode<PanelContainer>("%SmithTile");
        _smithNameLabel = GetNode<Label>("%SmithNameLabel");
        _smithDescLabel = GetNode<Label>("%SmithDescLabel");
        _skipButton = GetNode<Button>("%SkipButton");

        _upgradeView = GetNode<Control>("%UpgradeView");
        _upgradeTitleLabel = GetNode<Label>("%UpgradeTitleLabel");
        _upgradeHintLabel = GetNode<Label>("%UpgradeHintLabel");
        _cardGrid = GetNode<GridContainer>("%CardGrid");
        _emptyHintLabel = GetNode<Label>("%EmptyHintLabel");
        _confirmButton = GetNode<Button>("%ConfirmUpgradeButton");
        _cancelButton = GetNode<Button>("%CancelUpgradeButton");

        _previewModal = GetNode<Control>("%PreviewModal");
        _previewTitleLabel = GetNode<Label>("%PreviewTitleLabel");
        _previewCloseButton = GetNode<Button>("%PreviewCloseButton");
        _previewCenter = GetNode<CenterContainer>("%PreviewCenter");

        _restTile.GuiInput += OnRestTileInput;
        _smithTile.GuiInput += OnSmithTileInput;
        _skipButton.Pressed += OnSkipPressed;
        _confirmButton.Pressed += OnConfirmUpgradePressed;
        _cancelButton.Pressed += OnCancelUpgradePressed;
        _previewCloseButton.Pressed += ClosePreview;

        _upgradeView.Visible = false;
        _previewModal.Visible = false;
        RefreshUi(state);
    }

    private void RefreshUi(GameState state)
    {
        _titleLabel.Text = LocalizationService.Get("ui.rest.title", "Campfire");
        _statusLabel.Text = LocalizationService.Get(
            "ui.rest.status",
            "Take a moment to recover or improve your deck.");
        _hpLabel.Text = LocalizationService.Format(
            "ui.rest.hp",
            "HP {0}/{1}",
            state.PlayerHp,
            state.MaxHp);

        var healAmount = state.RestHealAmount();
        _restNameLabel.Text = LocalizationService.Get("ui.rest.tile_rest_name", "Rest");
        _restDescLabel.Text = LocalizationService.Format(
            "ui.rest.tile_rest_desc",
            "Heal {0} HP",
            healAmount);

        _smithNameLabel.Text = LocalizationService.Get("ui.rest.tile_smith_name", "Smith");
        _smithDescLabel.Text = LocalizationService.Get(
            "ui.rest.tile_smith_desc",
            "Upgrade a card");

        _skipButton.Text = "✕ " + LocalizationService.Get("ui.rest.skip_button", "Skip");

        _upgradeTitleLabel.Text = LocalizationService.Get(
            "ui.rest.upgrade_title",
            "Choose a card to upgrade");
        _upgradeHintLabel.Text = LocalizationService.Get(
            "ui.rest.upgrade_hint",
            "Left-click to select a card. Right-click to preview the upgraded version.");
        _emptyHintLabel.Text = LocalizationService.Get(
            "ui.rest.upgrade_empty",
            "(No cards eligible for upgrade.)");
        _confirmButton.Text = LocalizationService.Get(
            "ui.rest.upgrade_confirm",
            "✦ Confirm Upgrade");
        _cancelButton.Text = LocalizationService.Get(
            "ui.rest.upgrade_cancel",
            "← Cancel");
        _previewTitleLabel.Text = LocalizationService.Get(
            "ui.rest.preview_title",
            "Upgrade Preview");
    }

    private void OnRestTileInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            var state = GetNode<GameState>("/root/GameState");
            state.ApplyRestHeal();
            ExitToMap();
        }
    }

    private void OnSmithTileInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            ShowUpgradeView();
        }
    }

    private void OnSkipPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ApplyRestSkip();
        ExitToMap();
    }

    private void ShowUpgradeView()
    {
        var state = GetNode<GameState>("/root/GameState");

        foreach (var child in _cardGrid.GetChildren())
        {
            child.QueueFree();
        }
        _eligibleDeckIndices.Clear();
        _tileFrames.Clear();
        _selectedListIndex = -1;

        for (var i = 0; i < state.DeckCardIds.Count; i++)
        {
            if (!state.DeckCardIsUpgradable(i))
            {
                continue;
            }

            _eligibleDeckIndices.Add(i);
            var listIndex = _eligibleDeckIndices.Count - 1;
            var tile = BuildPickerTile(listIndex, state.DeckCardIds[i]);
            _cardGrid.AddChild(tile);
        }

        _emptyHintLabel.Visible = _eligibleDeckIndices.Count == 0;
        _cardGrid.Visible = _eligibleDeckIndices.Count > 0;
        _confirmButton.Disabled = true;

        _mainView.Visible = false;
        _upgradeView.Visible = true;
        _previewModal.Visible = false;
    }

    private PanelContainer BuildPickerTile(int listIndex, string baseId)
    {
        // PanelContainer = the frame whose style we swap between Normal / Selected.
        // It's a container, so it auto-fits its single child (the CardView).
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(220, 300),
            MouseFilter = MouseFilterEnum.Pass
        };
        frame.AddThemeStyleboxOverride("panel", TileNormal);

        var cardView = new CardView();
        cardView.SetUseTopLevel(false);
        cardView.SetDragEnabled(false);
        cardView.Setup(CardData.CreateById(baseId));

        var captured = listIndex;
        cardView.Clicked = _ => SelectListIndex(captured);
        cardView.RightClicked = _ => ShowPreview(captured);

        frame.AddChild(cardView);
        _tileFrames.Add(frame);
        return frame;
    }

    private void SelectListIndex(int listIndex)
    {
        if (listIndex < 0 || listIndex >= _tileFrames.Count)
        {
            return;
        }

        _selectedListIndex = listIndex;
        for (var i = 0; i < _tileFrames.Count; i++)
        {
            var selected = i == listIndex;
            _tileFrames[i].AddThemeStyleboxOverride("panel", selected ? TileSelected : TileNormal);
            _tileFrames[i].Scale = selected ? new Vector2(1.04f, 1.04f) : Vector2.One;
        }

        _confirmButton.Disabled = false;
    }

    private void ShowPreview(int listIndex)
    {
        if (listIndex < 0 || listIndex >= _eligibleDeckIndices.Count)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        var deckIndex = _eligibleDeckIndices[listIndex];
        var upgradedId = state.DeckCardIds[deckIndex] + "+";

        if (_previewCardView != null && IsInstanceValid(_previewCardView))
        {
            _previewCardView.QueueFree();
        }

        _previewCardView = new CardView();
        _previewCardView.SetUseTopLevel(false);
        _previewCardView.SetDragEnabled(false);
        _previewCardView.Setup(CardData.CreateById(upgradedId));
        _previewCardView.Scale = new Vector2(1.6f, 1.6f);
        _previewCardView.MouseFilter = MouseFilterEnum.Ignore;
        _previewCenter.AddChild(_previewCardView);

        _previewModal.Visible = true;
    }

    private void ClosePreview()
    {
        _previewModal.Visible = false;
        if (_previewCardView != null && IsInstanceValid(_previewCardView))
        {
            _previewCardView.QueueFree();
            _previewCardView = null;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_previewModal != null && _previewModal.Visible
            && @event is InputEventMouseButton mb && mb.Pressed
            && (mb.ButtonIndex == MouseButton.Right || mb.ButtonIndex == MouseButton.Middle))
        {
            ClosePreview();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnConfirmUpgradePressed()
    {
        if (_selectedListIndex < 0 || _selectedListIndex >= _eligibleDeckIndices.Count)
        {
            return;
        }

        var deckIndex = _eligibleDeckIndices[_selectedListIndex];
        var state = GetNode<GameState>("/root/GameState");
        if (!state.ApplyRestUpgrade(deckIndex))
        {
            _statusLabel.Text = LocalizationService.Get(
                "ui.rest.upgrade_failed",
                "That card cannot be upgraded.");
            return;
        }

        ExitToMap();
    }

    private void OnCancelUpgradePressed()
    {
        ClosePreview();
        _upgradeView.Visible = false;
        _mainView.Visible = true;
    }

    private void ExitToMap()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private static StyleBoxFlat MakeTileStyle(Color borderColor, float glowAlpha)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.13f, 0.15f, 0.95f),
            BorderColor = borderColor,
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ShadowColor = new Color(borderColor.R, borderColor.G, borderColor.B, glowAlpha),
            ShadowSize = glowAlpha > 0.5f ? 16 : 8
        };
    }
}
