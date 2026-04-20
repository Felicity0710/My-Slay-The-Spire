using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class BattleScene
{
    private enum PileSortMode
    {
        Natural = 0,
        CostAsc = 1,
        CostDesc = 2,
        NameAsc = 3
    }

    private Button _drawPileButton = null!;
    private Button _discardPileButton = null!;
    private Control _pileViewerModal = null!;
    private Label _pileViewerTitle = null!;
    private Label _pileViewerCountLabel = null!;
    private Label _pileViewerSortLabel = null!;
    private OptionButton _pileViewerSortOption = null!;
    private ItemList _pileViewerList = null!;
    private Label _pileViewerDetailTitle = null!;
    private RichTextLabel _pileViewerDetailText = null!;
    private Button _pileViewerCloseButton = null!;

    private bool _pileViewerShowsDrawPile = true;
    private bool _pileViewerInputLockHeld;
    private PileSortMode _pileSortMode = PileSortMode.Natural;
    private readonly List<CardData> _pileViewerDisplayCards = new();

    private void SetupPileViewerUi()
    {
        _drawPileButton = GetNode<Button>("%DrawPileButton");
        _discardPileButton = GetNode<Button>("%DiscardPileButton");
        _pileViewerModal = GetNode<Control>("%PileViewerModal");
        _pileViewerTitle = GetNode<Label>("%PileViewerTitle");
        _pileViewerCountLabel = GetNode<Label>("%PileViewerCountLabel");
        _pileViewerSortLabel = GetNode<Label>("%PileViewerSortLabel");
        _pileViewerSortOption = GetNode<OptionButton>("%PileViewerSortOption");
        _pileViewerList = GetNode<ItemList>("%PileViewerList");
        _pileViewerDetailTitle = GetNode<Label>("%PileViewerDetailTitle");
        _pileViewerDetailText = GetNode<RichTextLabel>("%PileViewerDetailText");
        _pileViewerCloseButton = GetNode<Button>("%PileViewerCloseButton");

        _drawPileButton.Pressed += OnOpenDrawPilePressed;
        _discardPileButton.Pressed += OnOpenDiscardPilePressed;
        _pileViewerSortOption.ItemSelected += OnPileSortSelected;
        _pileViewerList.ItemSelected += OnPileCardSelected;
        _pileViewerCloseButton.Pressed += OnClosePileViewerPressed;

        // Keep pile viewer above top-level hand cards and VFX.
        _pileViewerModal.Reparent(_overlayCanvas);
        _pileViewerModal.TopLevel = false;
        _pileViewerModal.ZIndex = 1200;
        _pileViewerModal.MouseFilter = Control.MouseFilterEnum.Stop;
        SetProcessUnhandledInput(true);

        _pileViewerList.AllowReselect = true;
        _pileViewerDetailText.BbcodeEnabled = false;
        _pileViewerModal.Visible = false;
        _pileViewerInputLockHeld = false;

        RefreshPileViewerUi();
    }

    private void RefreshPileViewerUi()
    {
        if (_drawPileButton == null || !IsInstanceValid(_drawPileButton))
        {
            return;
        }

        _drawPileButton.Text = LocalizationService.Format(
            "ui.battle.view_draw_pile",
            "Draw Pile ({0})",
            _drawPile.Count);
        _discardPileButton.Text = LocalizationService.Format(
            "ui.battle.view_discard_pile",
            "Discard Pile ({0})",
            _discardPile.Count);

        var lockForOpen = IsInputLocked() && !_pileViewerModal.Visible;
        _drawPileButton.Disabled = lockForOpen;
        _discardPileButton.Disabled = lockForOpen;

        RefreshPileSortUiText();
        _pileViewerDetailTitle.Text = LocalizationService.Get("ui.battle.pile_viewer_detail_title", "Card Details");
        _pileViewerCloseButton.Text = LocalizationService.Get("ui.common.close", "Close");

        if (_battleEnded && _pileViewerModal.Visible)
        {
            ClosePileViewer();
            return;
        }

        if (_pileViewerModal.Visible)
        {
            RefreshPileViewerModalContent();
        }
    }

    private void OnOpenDrawPilePressed()
    {
        OpenPileViewer(showDrawPile: true);
    }

    private void OnOpenDiscardPilePressed()
    {
        OpenPileViewer(showDrawPile: false);
    }

    private void OnClosePileViewerPressed()
    {
        ClosePileViewer();
    }

    private void OnPileSortSelected(long selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex > (int)PileSortMode.NameAsc)
        {
            return;
        }

        _pileSortMode = (PileSortMode)selectedIndex;
        if (_pileViewerModal.Visible)
        {
            RefreshPileViewerModalContent();
        }
    }

    private void OnPileCardSelected(long selectedIndex)
    {
        ShowPileCardDetail((int)selectedIndex);
    }

    private void OpenPileViewer(bool showDrawPile)
    {
        if (_battleEnded)
        {
            return;
        }

        _pileViewerShowsDrawPile = showDrawPile;
        if (!_pileViewerModal.Visible)
        {
            // Ensure this modal stays on top if multiple overlay elements exist.
            _overlayCanvas.MoveChild(_pileViewerModal, _overlayCanvas.GetChildCount() - 1);
            _pileViewerModal.Visible = true;
            if (!_pileViewerInputLockHeld)
            {
                PushInputLock();
                _pileViewerInputLockHeld = true;
            }
        }

        RefreshPileViewerModalContent();
    }

    private void ClosePileViewer()
    {
        if (!_pileViewerModal.Visible && !_pileViewerInputLockHeld)
        {
            return;
        }

        _pileViewerModal.Visible = false;
        if (_pileViewerInputLockHeld)
        {
            _pileViewerInputLockHeld = false;
            PopInputLock();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Engine.IsEditorHint() || @event == null)
        {
            return;
        }

        if (!_pileViewerInputLockHeld || !IsInstanceValid(_pileViewerModal) || !_pileViewerModal.Visible)
        {
            return;
        }

        if (!@event.IsActionPressed("ui_cancel"))
        {
            return;
        }

        ClosePileViewer();
        GetViewport().SetInputAsHandled();
    }

    private void RefreshPileViewerModalContent()
    {
        var pile = _pileViewerShowsDrawPile ? _drawPile : _discardPile;
        var ordered = BuildOrderedPileCards(pile);
        _pileViewerDisplayCards.Clear();
        _pileViewerDisplayCards.AddRange(ordered);

        _pileViewerTitle.Text = _pileViewerShowsDrawPile
            ? LocalizationService.Get("ui.battle.pile_viewer_draw_title", "Draw Pile")
            : LocalizationService.Get("ui.battle.pile_viewer_discard_title", "Discard Pile");
        _pileViewerCountLabel.Text = LocalizationService.Format("ui.battle.pile_viewer_count", "Cards: {0}", pile.Count);
        _pileViewerList.Clear();

        if (ordered.Count == 0)
        {
            _pileViewerList.AddItem(LocalizationService.Get("ui.battle.pile_viewer_empty", "(empty)"));
            _pileViewerList.SetItemDisabled(0, true);
            _pileViewerDetailText.Text = LocalizationService.Get("ui.battle.pile_viewer_detail_empty", "Select a card to view details.");
            return;
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            _pileViewerList.AddItem(BuildPileCardLine(i + 1, ordered[i]));
        }

        _pileViewerList.Select(0);
        ShowPileCardDetail(0);
    }

    private List<CardData> BuildOrderedPileCards(IReadOnlyList<CardData> pile)
    {
        var ordered = new List<CardData>(pile.Count);
        if (_pileSortMode == PileSortMode.Natural)
        {
            if (_pileViewerShowsDrawPile)
            {
                for (var i = 0; i < pile.Count; i++)
                {
                    ordered.Add(pile[i]);
                }
            }
            else
            {
                for (var i = pile.Count - 1; i >= 0; i--)
                {
                    ordered.Add(pile[i]);
                }
            }

            return ordered;
        }

        for (var i = 0; i < pile.Count; i++)
        {
            ordered.Add(pile[i]);
        }

        switch (_pileSortMode)
        {
            case PileSortMode.CostAsc:
                ordered.Sort(CompareByCostThenName);
                break;
            case PileSortMode.CostDesc:
                ordered.Sort((a, b) => CompareByCostThenName(b, a));
                break;
            case PileSortMode.NameAsc:
                ordered.Sort((a, b) => string.Compare(a.GetLocalizedName(), b.GetLocalizedName(), StringComparison.CurrentCultureIgnoreCase));
                break;
        }

        return ordered;
    }

    private int CompareByCostThenName(CardData left, CardData right)
    {
        var costCompare = left.Cost.CompareTo(right.Cost);
        if (costCompare != 0)
        {
            return costCompare;
        }

        return string.Compare(left.GetLocalizedName(), right.GetLocalizedName(), StringComparison.CurrentCultureIgnoreCase);
    }

    private string BuildPileCardLine(int rank, CardData card)
    {
        var costText = LocalizationService.Format("ui.battle.pile_viewer_cost", "Cost {0}", card.Cost);
        return $"{rank}. {card.GetLocalizedName()} ({costText})";
    }

    private void ShowPileCardDetail(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= _pileViewerDisplayCards.Count)
        {
            _pileViewerDetailText.Text = LocalizationService.Get("ui.battle.pile_viewer_detail_empty", "Select a card to view details.");
            return;
        }

        var card = _pileViewerDisplayCards[selectedIndex];
        var cardTypeKey = card.Kind == CardKind.Attack ? "ui.card_browser.filters.type_attack" : "ui.card_browser.filters.type_skill";
        var cardTypeText = LocalizationService.Get(cardTypeKey, card.Kind.ToString());
        var sb = new StringBuilder();
        sb.AppendLine(card.GetLocalizedName());
        sb.Append(LocalizationService.Format("ui.battle.pile_viewer_cost", "Cost {0}", card.Cost));
        sb.Append(" | ");
        sb.AppendLine(cardTypeText);
        sb.AppendLine();
        sb.Append(card.GetLocalizedDescription());
        _pileViewerDetailText.Text = sb.ToString();
        _pileViewerDetailText.ScrollToLine(0);
    }

    private void RefreshPileSortUiText()
    {
        _pileViewerSortLabel.Text = LocalizationService.Get("ui.battle.pile_viewer_sort_label", "Sort");

        var selected = (int)_pileSortMode;
        _pileViewerSortOption.Clear();
        _pileViewerSortOption.AddItem(LocalizationService.Get("ui.battle.pile_viewer_sort_natural", "Natural"));
        _pileViewerSortOption.AddItem(LocalizationService.Get("ui.battle.pile_viewer_sort_cost_asc", "Cost Low->High"));
        _pileViewerSortOption.AddItem(LocalizationService.Get("ui.battle.pile_viewer_sort_cost_desc", "Cost High->Low"));
        _pileViewerSortOption.AddItem(LocalizationService.Get("ui.battle.pile_viewer_sort_name", "Name A-Z"));

        if (selected < 0 || selected >= _pileViewerSortOption.ItemCount)
        {
            selected = 0;
            _pileSortMode = PileSortMode.Natural;
        }

        _pileViewerSortOption.Select(selected);
    }
}
