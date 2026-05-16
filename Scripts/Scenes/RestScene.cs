using Godot;
using System.Collections.Generic;

public partial class RestScene : Control
{
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Label _hpLabel = null!;
    private Button _restButton = null!;
    private Button _smithButton = null!;
    private Button _skipButton = null!;

    private Control _upgradeOverlay = null!;
    private Label _upgradeTitleLabel = null!;
    private Label _upgradeHintLabel = null!;
    private ItemList _upgradeList = null!;
    private Button _confirmUpgradeButton = null!;
    private Button _cancelUpgradeButton = null!;

    private readonly List<int> _eligibleDeckIndices = new();

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("rest");
        AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());

        _titleLabel = GetNode<Label>("%TitleLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _hpLabel = GetNode<Label>("%HpLabel");
        _restButton = GetNode<Button>("%RestButton");
        _smithButton = GetNode<Button>("%SmithButton");
        _skipButton = GetNode<Button>("%SkipButton");

        _upgradeOverlay = GetNode<Control>("%UpgradeOverlay");
        _upgradeTitleLabel = GetNode<Label>("%UpgradeTitleLabel");
        _upgradeHintLabel = GetNode<Label>("%UpgradeHintLabel");
        _upgradeList = GetNode<ItemList>("%UpgradeList");
        _confirmUpgradeButton = GetNode<Button>("%ConfirmUpgradeButton");
        _cancelUpgradeButton = GetNode<Button>("%CancelUpgradeButton");

        _restButton.Pressed += OnRestPressed;
        _smithButton.Pressed += OnSmithPressed;
        _skipButton.Pressed += OnSkipPressed;
        _confirmUpgradeButton.Pressed += OnConfirmUpgradePressed;
        _cancelUpgradeButton.Pressed += OnCancelUpgradePressed;

        _upgradeOverlay.Visible = false;
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
        _restButton.Text = LocalizationService.Format(
            "ui.rest.heal_button",
            "Rest (Heal {0} HP)",
            healAmount);
        _smithButton.Text = LocalizationService.Get(
            "ui.rest.smith_button",
            "Smith (Upgrade a card)");
        _skipButton.Text = LocalizationService.Get("ui.rest.skip_button", "Skip");

        _upgradeTitleLabel.Text = LocalizationService.Get(
            "ui.rest.upgrade_title",
            "Choose a card to upgrade");
        _upgradeHintLabel.Text = LocalizationService.Get(
            "ui.rest.upgrade_hint",
            "Already-upgraded and unupgradable cards are hidden.");
        _confirmUpgradeButton.Text = LocalizationService.Get(
            "ui.rest.upgrade_confirm",
            "Confirm");
        _cancelUpgradeButton.Text = LocalizationService.Get(
            "ui.rest.upgrade_cancel",
            "Cancel");
    }

    private void OnRestPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ApplyRestHeal();
        ExitToMap();
    }

    private void OnSmithPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        _eligibleDeckIndices.Clear();
        _upgradeList.Clear();

        for (var i = 0; i < state.DeckCardIds.Count; i++)
        {
            if (!state.DeckCardIsUpgradable(i))
            {
                continue;
            }

            var card = CardData.CreateById(state.DeckCardIds[i]);
            _upgradeList.AddItem(LocalizationService.Format(
                "ui.rest.upgrade_entry",
                "#{0:00} {1} -> {2}+",
                i + 1,
                card.GetLocalizedName(),
                card.GetLocalizedName()));
            _eligibleDeckIndices.Add(i);
        }

        if (_eligibleDeckIndices.Count == 0)
        {
            _upgradeList.AddItem(LocalizationService.Get(
                "ui.rest.upgrade_empty",
                "(No cards eligible for upgrade.)"));
            _upgradeList.SetItemDisabled(0, true);
            _confirmUpgradeButton.Disabled = true;
        }
        else
        {
            _confirmUpgradeButton.Disabled = false;
            _upgradeList.Select(0);
        }

        _upgradeOverlay.Visible = true;
    }

    private void OnConfirmUpgradePressed()
    {
        var selected = _upgradeList.GetSelectedItems();
        if (selected.Length == 0 || _eligibleDeckIndices.Count == 0)
        {
            return;
        }

        var listIndex = selected[0];
        if (listIndex < 0 || listIndex >= _eligibleDeckIndices.Count)
        {
            return;
        }

        var deckIndex = _eligibleDeckIndices[listIndex];
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
        _upgradeOverlay.Visible = false;
    }

    private void OnSkipPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ApplyRestSkip();
        ExitToMap();
    }

    private void ExitToMap()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }
}
