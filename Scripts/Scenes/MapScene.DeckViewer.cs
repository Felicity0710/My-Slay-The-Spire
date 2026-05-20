using Godot;
using System.Collections.Generic;

public partial class MapScene
{
    private Button _viewDeckButton = null!;
    private Control _deckViewerModal = null!;
    private Label _deckViewerTitleLabel = null!;
    private Label _deckViewerCountLabel = null!;
    private GridContainer _deckViewerCardGrid = null!;
    private Label _deckViewerEmptyLabel = null!;
    private Button _deckViewerCloseButton = null!;
    private readonly List<CardData> _deckViewerCards = new();

    private void SetupDeckViewerUi()
    {
        _viewDeckButton = GetNode<Button>("%ViewDeckButton");
        _deckViewerModal = GetNode<Control>("%DeckViewerModal");
        _deckViewerTitleLabel = GetNode<Label>("%DeckViewerTitleLabel");
        _deckViewerCountLabel = GetNode<Label>("%DeckViewerCountLabel");
        _deckViewerCardGrid = GetNode<GridContainer>("%DeckViewerCardGrid");
        _deckViewerEmptyLabel = GetNode<Label>("%DeckViewerEmptyLabel");
        _deckViewerCloseButton = GetNode<Button>("%DeckViewerCloseButton");

        _viewDeckButton.Pressed += OnViewDeckPressed;
        _deckViewerCloseButton.Pressed += CloseDeckViewer;

        _deckViewerModal.Visible = false;
    }

    private void TearDownDeckViewerUi()
    {
        if (IsInstanceValid(_viewDeckButton))
        {
            _viewDeckButton.Pressed -= OnViewDeckPressed;
        }

        if (IsInstanceValid(_deckViewerCloseButton))
        {
            _deckViewerCloseButton.Pressed -= CloseDeckViewer;
        }
    }

    private void RefreshDeckViewerUi(GameState state)
    {
        if (!IsInstanceValid(_viewDeckButton) || !IsInstanceValid(_deckViewerCardGrid))
        {
            return;
        }

        _viewDeckButton.Text = LocalizationService.Format(
            "ui.map.view_deck",
            "🎴 卡组 {0}",
            state.DeckCardIds.Count);

        _deckViewerTitleLabel.Text = LocalizationService.Get("ui.map.deck_viewer.title", "Deck Viewer");
        _deckViewerCountLabel.Text = LocalizationService.Format(
            "ui.map.deck_viewer.count",
            "Cards: {0}",
            state.DeckCardIds.Count);
        _deckViewerCloseButton.Text = LocalizationService.Get("ui.common.close", "Close");
        _deckViewerEmptyLabel.Text = LocalizationService.Get("ui.map.deck_viewer.empty", "(Empty)");

        // Rebuild the card grid every refresh — deck contents may have changed
        // (shop, smith, events, etc).
        foreach (var child in _deckViewerCardGrid.GetChildren())
        {
            child.QueueFree();
        }

        _deckViewerCards.Clear();

        if (state.DeckCardIds.Count <= 0)
        {
            _deckViewerEmptyLabel.Visible = true;
            _deckViewerCardGrid.Visible = false;
            return;
        }

        _deckViewerEmptyLabel.Visible = false;
        _deckViewerCardGrid.Visible = true;

        foreach (var cardId in state.DeckCardIds)
        {
            var card = CardData.CreateById(cardId);
            _deckViewerCards.Add(card);
            _deckViewerCardGrid.AddChild(BuildDeckViewerTile(card));
        }
    }

    private Control BuildDeckViewerTile(CardData card)
    {
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(210, 290),
            MouseFilter = MouseFilterEnum.Pass
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.13f, 0.16f, 0.95f),
            BorderColor = new Color(0.55f, 0.42f, 0.22f, 0.85f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 4,
            ContentMarginTop = 4,
            ContentMarginRight = 4,
            ContentMarginBottom = 4
        };
        frame.AddThemeStyleboxOverride("panel", style);

        var view = new CardView();
        view.SetUseTopLevel(false);
        view.SetDragEnabled(false);
        view.Setup(card);
        frame.AddChild(view);
        return frame;
    }

    private void OnViewDeckPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        RefreshDeckViewerUi(state);
        _deckViewerModal.Visible = true;
    }

    private void CloseDeckViewer()
    {
        if (IsInstanceValid(_deckViewerModal))
        {
            _deckViewerModal.Visible = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsInstanceValid(_deckViewerModal)
            && _deckViewerModal.Visible
            && @event.IsActionPressed("ui_cancel"))
        {
            CloseDeckViewer();
            GetViewport().SetInputAsHandled();
            return;
        }

        base._UnhandledInput(@event);
    }
}
