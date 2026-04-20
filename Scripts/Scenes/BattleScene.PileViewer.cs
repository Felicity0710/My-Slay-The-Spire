using Godot;
using System.Text;

public partial class BattleScene
{
    private Button _drawPileButton = null!;
    private Button _discardPileButton = null!;
    private Control _pileViewerModal = null!;
    private Label _pileViewerTitle = null!;
    private Label _pileViewerCountLabel = null!;
    private RichTextLabel _pileViewerList = null!;
    private Button _pileViewerCloseButton = null!;

    private bool _pileViewerShowsDrawPile = true;
    private bool _pileViewerInputLockHeld;

    private void SetupPileViewerUi()
    {
        _drawPileButton = GetNode<Button>("%DrawPileButton");
        _discardPileButton = GetNode<Button>("%DiscardPileButton");
        _pileViewerModal = GetNode<Control>("%PileViewerModal");
        _pileViewerTitle = GetNode<Label>("%PileViewerTitle");
        _pileViewerCountLabel = GetNode<Label>("%PileViewerCountLabel");
        _pileViewerList = GetNode<RichTextLabel>("%PileViewerList");
        _pileViewerCloseButton = GetNode<Button>("%PileViewerCloseButton");

        _drawPileButton.Pressed += OnOpenDrawPilePressed;
        _discardPileButton.Pressed += OnOpenDiscardPilePressed;
        _pileViewerCloseButton.Pressed += OnClosePileViewerPressed;

        // Use plain text rendering to avoid accidental BBCode parsing in localized names.
        _pileViewerList.BbcodeEnabled = false;
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

    private void OpenPileViewer(bool showDrawPile)
    {
        if (_battleEnded)
        {
            return;
        }

        _pileViewerShowsDrawPile = showDrawPile;
        if (!_pileViewerModal.Visible)
        {
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

    private void RefreshPileViewerModalContent()
    {
        var pile = _pileViewerShowsDrawPile ? _drawPile : _discardPile;

        _pileViewerTitle.Text = _pileViewerShowsDrawPile
            ? LocalizationService.Get("ui.battle.pile_viewer_draw_title", "Draw Pile")
            : LocalizationService.Get("ui.battle.pile_viewer_discard_title", "Discard Pile");
        _pileViewerCountLabel.Text = LocalizationService.Format("ui.battle.pile_viewer_count", "Cards: {0}", pile.Count);

        if (pile.Count == 0)
        {
            _pileViewerList.Text = LocalizationService.Get("ui.battle.pile_viewer_empty", "(empty)");
            return;
        }

        var sb = new StringBuilder();
        if (_pileViewerShowsDrawPile)
        {
            for (var i = 0; i < pile.Count; i++)
            {
                AppendPileCardLine(sb, i + 1, pile[i]);
            }
        }
        else
        {
            var rank = 1;
            for (var i = pile.Count - 1; i >= 0; i--)
            {
                AppendPileCardLine(sb, rank, pile[i]);
                rank += 1;
            }
        }

        _pileViewerList.Text = sb.ToString().TrimEnd();
        _pileViewerList.ScrollToLine(0);
    }

    private void AppendPileCardLine(StringBuilder sb, int rank, CardData card)
    {
        var costText = LocalizationService.Format("ui.battle.pile_viewer_cost", "Cost {0}", card.Cost);
        sb.Append(rank)
            .Append(". ")
            .Append(card.GetLocalizedName())
            .Append(" (")
            .Append(costText)
            .AppendLine(")");
    }
}
