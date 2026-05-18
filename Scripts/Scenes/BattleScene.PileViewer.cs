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

    private enum PileViewerTarget
    {
        Draw,
        Discard,
        Exhaust
    }

    private Button _drawPileButton = null!;
    private Button _discardPileButton = null!;
    private Button _exhaustPileButton = null!;
    private Control _pileViewerModal = null!;
    private Label _pileViewerTitle = null!;
    private Label _pileViewerCountLabel = null!;
    private Label _pileViewerSortLabel = null!;
    private OptionButton _pileViewerSortOption = null!;
    private GridContainer _pileViewerCardGrid = null!;
    private Label _pileViewerEmptyLabel = null!;
    private Button _pileViewerCloseButton = null!;

    private PileViewerTarget _pileViewerTarget = PileViewerTarget.Draw;
    private bool _pileViewerInputLockHeld;
    private PileSortMode _pileSortMode = PileSortMode.Natural;
    private readonly List<CardData> _pileViewerDisplayCards = new();

    private void SetupPileViewerUi()
    {
        _drawPileButton = GetNode<Button>("%DrawPileButton");
        _discardPileButton = GetNode<Button>("%DiscardPileButton");
        _exhaustPileButton = GetNode<Button>("%ExhaustPileButton");
        _pileViewerModal = GetNode<Control>("%PileViewerModal");
        _pileViewerTitle = GetNode<Label>("%PileViewerTitle");
        _pileViewerCountLabel = GetNode<Label>("%PileViewerCountLabel");
        _pileViewerSortLabel = GetNode<Label>("%PileViewerSortLabel");
        _pileViewerSortOption = GetNode<OptionButton>("%PileViewerSortOption");
        _pileViewerCardGrid = GetNode<GridContainer>("%PileViewerCardGrid");
        _pileViewerEmptyLabel = GetNode<Label>("%PileViewerEmptyLabel");
        _pileViewerCloseButton = GetNode<Button>("%PileViewerCloseButton");

        _drawPileButton.Pressed += OnOpenDrawPilePressed;
        _discardPileButton.Pressed += OnOpenDiscardPilePressed;
        _exhaustPileButton.Pressed += OnOpenExhaustPilePressed;
        _pileViewerSortOption.ItemSelected += OnPileSortSelected;
        _pileViewerCloseButton.Pressed += OnClosePileViewerPressed;

        // Keep pile viewer above top-level hand cards and VFX.
        _pileViewerModal.Reparent(_overlayCanvas);
        _pileViewerModal.TopLevel = false;
        _pileViewerModal.ZIndex = 1200;
        _pileViewerModal.MouseFilter = Control.MouseFilterEnum.Stop;
        SetProcessUnhandledInput(true);

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

        // With the icon already on the button, keep the text compact (label + count).
        _drawPileButton.Text = LocalizationService.Format(
            "ui.battle.view_draw_pile_short",
            "Draw {0}",
            _drawPile.Count);
        _drawPileButton.TooltipText = LocalizationService.Get(
            "ui.battle.pile_viewer_draw_title",
            "Draw Pile");

        _discardPileButton.Text = LocalizationService.Format(
            "ui.battle.view_discard_pile_short",
            "Discard {0}",
            _discardPile.Count);
        _discardPileButton.TooltipText = LocalizationService.Get(
            "ui.battle.pile_viewer_discard_title",
            "Discard Pile");

        _exhaustPileButton.Text = LocalizationService.Format(
            "ui.battle.view_exhaust_pile_short",
            "Exhaust {0}",
            _exhaustPile.Count);
        _exhaustPileButton.TooltipText = LocalizationService.Get(
            "ui.battle.pile_viewer_exhaust_title",
            "Exhaust Pile");

        var lockForOpen = IsInputLocked() && !_pileViewerModal.Visible;
        _drawPileButton.Disabled = lockForOpen;
        _discardPileButton.Disabled = lockForOpen;
        _exhaustPileButton.Disabled = lockForOpen;

        RefreshPileSortUiText();
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
        OpenPileViewer(PileViewerTarget.Draw);
    }

    private void OnOpenDiscardPilePressed()
    {
        OpenPileViewer(PileViewerTarget.Discard);
    }

    private void OnOpenExhaustPilePressed()
    {
        OpenPileViewer(PileViewerTarget.Exhaust);
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

    private void OpenPileViewer(PileViewerTarget target)
    {
        if (_battleEnded)
        {
            return;
        }

        _pileViewerTarget = target;
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
        IReadOnlyList<CardData> pile = _pileViewerTarget switch
        {
            PileViewerTarget.Draw => _drawPile,
            PileViewerTarget.Discard => _discardPile,
            PileViewerTarget.Exhaust => _exhaustPile,
            _ => _drawPile
        };

        var ordered = BuildOrderedPileCards(pile);
        _pileViewerDisplayCards.Clear();
        _pileViewerDisplayCards.AddRange(ordered);

        _pileViewerTitle.Text = _pileViewerTarget switch
        {
            PileViewerTarget.Draw => LocalizationService.Get("ui.battle.pile_viewer_draw_title", "Draw Pile"),
            PileViewerTarget.Discard => LocalizationService.Get("ui.battle.pile_viewer_discard_title", "Discard Pile"),
            PileViewerTarget.Exhaust => LocalizationService.Get("ui.battle.pile_viewer_exhaust_title", "Exhaust Pile"),
            _ => LocalizationService.Get("ui.battle.pile_viewer_draw_title", "Draw Pile")
        };

        _pileViewerCountLabel.Text = LocalizationService.Format("ui.battle.pile_viewer_count", "Cards: {0}", pile.Count);

        foreach (var child in _pileViewerCardGrid.GetChildren())
        {
            child.QueueFree();
        }

        if (ordered.Count == 0)
        {
            _pileViewerEmptyLabel.Text = LocalizationService.Get("ui.battle.pile_viewer_empty", "(empty)");
            _pileViewerEmptyLabel.Visible = true;
            _pileViewerCardGrid.Visible = false;
            return;
        }

        _pileViewerEmptyLabel.Visible = false;
        _pileViewerCardGrid.Visible = true;

        foreach (var card in ordered)
        {
            _pileViewerCardGrid.AddChild(BuildPileCardTile(card));
        }
    }

    private Control BuildPileCardTile(CardData card)
    {
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(210, 290),
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.13f, 0.16f, 0.95f),
            BorderColor = new Color(0.49f, 0.68f, 0.82f, 0.7f),
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

    private List<CardData> BuildOrderedPileCards(IReadOnlyList<CardData> pile)
    {
        var ordered = new List<CardData>(pile.Count);
        if (_pileSortMode == PileSortMode.Natural)
        {
            if (_pileViewerTarget == PileViewerTarget.Draw)
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
