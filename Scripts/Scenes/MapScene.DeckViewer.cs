using Godot;
using System.Collections.Generic;

public partial class MapScene
{
    private Button _viewDeckButton = null!;
    private Control _deckViewerModal = null!;
    private Label _deckViewerTitleLabel = null!;
    private Label _deckViewerCountLabel = null!;
    private ItemList _deckViewerItemList = null!;
    private RichTextLabel _deckViewerDetailText = null!;
    private Button _deckViewerCloseButton = null!;
    private readonly List<CardData> _deckViewerCards = new();

    private void SetupDeckViewerUi()
    {
        _viewDeckButton = GetNode<Button>("%ViewDeckButton");
        _deckViewerModal = GetNode<Control>("%DeckViewerModal");
        _deckViewerTitleLabel = GetNode<Label>("%DeckViewerTitleLabel");
        _deckViewerCountLabel = GetNode<Label>("%DeckViewerCountLabel");
        _deckViewerItemList = GetNode<ItemList>("%DeckViewerItemList");
        _deckViewerDetailText = GetNode<RichTextLabel>("%DeckViewerDetailText");
        _deckViewerCloseButton = GetNode<Button>("%DeckViewerCloseButton");

        _viewDeckButton.Pressed += OnViewDeckPressed;
        _deckViewerCloseButton.Pressed += CloseDeckViewer;
        _deckViewerItemList.ItemSelected += OnDeckViewerItemSelected;

        _deckViewerModal.Visible = false;
        _deckViewerDetailText.Text = string.Empty;
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

        if (IsInstanceValid(_deckViewerItemList))
        {
            _deckViewerItemList.ItemSelected -= OnDeckViewerItemSelected;
        }
    }

    private void RefreshDeckViewerUi(GameState state)
    {
        if (!IsInstanceValid(_viewDeckButton) || !IsInstanceValid(_deckViewerItemList))
        {
            return;
        }

        _viewDeckButton.Text = LocalizationService.Format(
            "ui.map.view_deck",
            "View Deck ({0})",
            state.DeckCardIds.Count);

        _deckViewerTitleLabel.Text = LocalizationService.Get("ui.map.deck_viewer.title", "Deck Viewer");
        _deckViewerCountLabel.Text = LocalizationService.Format(
            "ui.map.deck_viewer.count",
            "Cards: {0}",
            state.DeckCardIds.Count);
        _deckViewerCloseButton.Text = LocalizationService.Get("ui.common.close", "Close");

        _deckViewerCards.Clear();
        _deckViewerItemList.Clear();
        _deckViewerDetailText.Text = LocalizationService.Get(
            "ui.map.deck_viewer.detail_empty",
            "Select a card to view details.");

        if (state.DeckCardIds.Count <= 0)
        {
            _deckViewerItemList.AddItem(LocalizationService.Get("ui.map.deck_viewer.empty", "(Empty)"));
            _deckViewerItemList.SetItemDisabled(0, true);
            return;
        }

        for (var i = 0; i < state.DeckCardIds.Count; i++)
        {
            var card = CardData.CreateById(state.DeckCardIds[i]);
            _deckViewerCards.Add(card);

            var itemText = LocalizationService.Format(
                "ui.map.deck_viewer.item",
                "#{0:00} {1} · Cost {2}",
                i + 1,
                card.GetLocalizedName(),
                card.Cost);
            _deckViewerItemList.AddItem(itemText);
        }
    }

    private void OnViewDeckPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        RefreshDeckViewerUi(state);

        _deckViewerModal.Visible = true;
        if (_deckViewerCards.Count > 0)
        {
            _deckViewerItemList.Select(0);
            OnDeckViewerItemSelected(0);
        }
    }

    private void OnDeckViewerItemSelected(long index)
    {
        if (index < 0 || index >= _deckViewerCards.Count)
        {
            return;
        }

        var card = _deckViewerCards[(int)index];
        var costLine = LocalizationService.Format("ui.map.deck_viewer.cost", "Cost: {0}", card.Cost);
        var description = LocalizationSettings.HighlightCardDescription(card.GetLocalizedDescription());

        _deckViewerDetailText.Text =
            $"[b]{card.GetLocalizedName()}[/b]\n{costLine}\n{description}";
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
