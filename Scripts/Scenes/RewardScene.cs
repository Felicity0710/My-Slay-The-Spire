using Godot;
using System.Collections.Generic;
using System.Text;

public partial class RewardScene : Control
{
    private Label _titleLabel = null!;
    private Label _summaryLabel = null!;
    private Button _continueButton = null!;

    private VBoxContainer _cardSection = null!;
    private Label _cardSectionLabel = null!;
    private HBoxContainer _cardOptionsRow = null!;
    private Button _cardSkipButton = null!;

    private VBoxContainer _potionSection = null!;
    private Label _potionSectionLabel = null!;
    private HBoxContainer _potionOptionsRow = null!;
    private Button _potionSkipButton = null!;

    private VBoxContainer _relicSection = null!;
    private Label _relicSectionLabel = null!;
    private HBoxContainer _relicOptionsRow = null!;
    private Button _relicSkipButton = null!;

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("reward");

        _titleLabel = GetNode<Label>("%Title");
        _summaryLabel = GetNode<Label>("%SummaryLabel");
        _continueButton = GetNode<Button>("%ContinueButton");

        _cardSection = GetNode<VBoxContainer>("%CardSection");
        _cardSectionLabel = GetNode<Label>("%CardSectionLabel");
        _cardOptionsRow = GetNode<HBoxContainer>("%CardOptionsRow");
        _cardSkipButton = GetNode<Button>("%CardSkipButton");

        _potionSection = GetNode<VBoxContainer>("%PotionSection");
        _potionSectionLabel = GetNode<Label>("%PotionSectionLabel");
        _potionOptionsRow = GetNode<HBoxContainer>("%PotionOptionsRow");
        _potionSkipButton = GetNode<Button>("%PotionSkipButton");

        _relicSection = GetNode<VBoxContainer>("%RelicSection");
        _relicSectionLabel = GetNode<Label>("%RelicSectionLabel");
        _relicOptionsRow = GetNode<HBoxContainer>("%RelicOptionsRow");
        _relicSkipButton = GetNode<Button>("%RelicSkipButton");

        _continueButton.Pressed += OnContinuePressed;
        _cardSkipButton.Pressed += OnSkipCardsPressed;
        _potionSkipButton.Pressed += OnSkipPotionsPressed;
        _relicSkipButton.Pressed += OnSkipRelicsPressed;

        LocalizationSettings.LanguageChanged += RebuildAll;
        RebuildAll();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RebuildAll;
    }

    private void RebuildAll()
    {
        RefreshHeaderText();
        RebuildCardSection();
        RebuildPotionSection();
        RebuildRelicSection();
    }

    private void RefreshHeaderText()
    {
        var state = GetNode<GameState>("/root/GameState");
        var reward = state.LastBattleReward;

        _titleLabel.Text = reward.IsEliteTier
            ? LocalizationService.Get("ui.reward.title_elite", "Elite Reward")
            : LocalizationService.Get("ui.reward.title_normal", "Battle Reward");

        _continueButton.Text = LocalizationService.Get("ui.reward.continue", "Continue");

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Format("ui.reward.gold_line", "Gold +{0}", reward.GoldGained));

        if (reward.HealedFromCharm > 0)
        {
            sb.AppendLine(LocalizationService.Format(
                "ui.reward.charm_heal",
                "Lucky Charm healed {0} HP.",
                reward.HealedFromCharm));
        }

        if (reward.HealedFromBloodVial > 0)
        {
            sb.AppendLine(LocalizationService.Format(
                "ui.reward.blood_vial_heal",
                "Blood Vial healed {0} HP.",
                reward.HealedFromBloodVial));
        }

        _summaryLabel.Text = sb.ToString().TrimEnd();
    }

    private void RebuildCardSection()
    {
        ClearChildren(_cardOptionsRow);

        var state = GetNode<GameState>("/root/GameState");
        var options = state.PendingRewardOptions;

        if (options.Count == 0)
        {
            _cardSection.Visible = false;
            return;
        }

        _cardSection.Visible = true;
        _cardSectionLabel.Text = LocalizationService.Get(
            "ui.reward.card_section_label",
            "Pick 1 card or skip");
        _cardSkipButton.Text = LocalizationService.Get(
            "ui.reward.skip_cards",
            "Skip cards");

        for (var i = 0; i < options.Count; i++)
        {
            var capture = i;
            var card = CardData.CreateById(options[i]);
            var button = new Button
            {
                CustomMinimumSize = new Vector2(220, 110),
                Text = $"{card.GetLocalizedName()}\n{LocalizationSettings.CostLabel()}: {card.Cost}\n\n{card.GetLocalizedDescription()}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ClipText = false
            };
            button.AddThemeFontSizeOverride("font_size", 14);
            button.Pressed += () => OnTakeCard(capture);
            _cardOptionsRow.AddChild(button);
        }
    }

    private void RebuildPotionSection()
    {
        ClearChildren(_potionOptionsRow);

        var state = GetNode<GameState>("/root/GameState");
        var options = state.PendingPotionRewardOptions;

        if (options.Count == 0)
        {
            _potionSection.Visible = false;
            return;
        }

        var fullInventory = state.PotionIds.Count >= GameState.PotionInventoryCapacity;
        _potionSection.Visible = true;
        _potionSectionLabel.Text = fullInventory
            ? LocalizationService.Get(
                "ui.reward.potion_section_label_full",
                "Potion belt is full — skip or replace nothing")
            : LocalizationService.Get(
                "ui.reward.potion_section_label",
                "Pick 1 potion or skip");
        _potionSkipButton.Text = LocalizationService.Get(
            "ui.reward.skip_potions",
            "Skip potions");

        for (var i = 0; i < options.Count; i++)
        {
            var capture = i;
            var potion = PotionData.CreateById(options[i]);
            var potionName = LocalizationService.Get($"potion.{potion.Id}.name", potion.Name);
            var potionDesc = LocalizationService.Get($"potion.{potion.Id}.description", potion.Description);
            var button = new Button
            {
                CustomMinimumSize = new Vector2(220, 110),
                Text = $"{potionName}\n\n{potionDesc}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ClipText = false,
                Disabled = fullInventory
            };
            button.AddThemeFontSizeOverride("font_size", 14);
            button.Pressed += () => OnTakePotion(capture);
            _potionOptionsRow.AddChild(button);
        }
    }

    private void RebuildRelicSection()
    {
        ClearChildren(_relicOptionsRow);

        var state = GetNode<GameState>("/root/GameState");
        var options = state.PendingRelicOptions;

        if (options.Count == 0)
        {
            _relicSection.Visible = false;
            return;
        }

        _relicSection.Visible = true;
        _relicSectionLabel.Text = LocalizationService.Get(
            "ui.reward.relic_section_label",
            "Pick 1 relic or skip");
        _relicSkipButton.Text = LocalizationService.Get(
            "ui.reward.skip_relics",
            "Skip relic");

        for (var i = 0; i < options.Count; i++)
        {
            var capture = i;
            var relic = RelicData.CreateById(options[i]);
            var button = new Button
            {
                CustomMinimumSize = new Vector2(240, 110),
                Text = $"{relic.LocalizedName}\n\n{relic.LocalizedDescription}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ClipText = false
            };
            button.AddThemeFontSizeOverride("font_size", 14);
            button.Pressed += () => OnTakeRelic(capture);
            _relicOptionsRow.AddChild(button);
        }
    }

    private static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnTakeCard(int idx)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.TakeRewardCardOption(idx);
        RebuildCardSection();
    }

    private void OnTakePotion(int idx)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.TakeRewardPotionOption(idx);
        RebuildPotionSection();
    }

    private void OnTakeRelic(int idx)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.TakeRewardRelicOption(idx);
        RebuildRelicSection();
    }

    private void OnSkipCardsPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SkipRewardCards();
        RebuildCardSection();
    }

    private void OnSkipPotionsPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SkipRewardPotions();
        RebuildPotionSection();
    }

    private void OnSkipRelicsPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SkipRewardRelics();
        RebuildRelicSection();
    }

    private void OnContinuePressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ClearBattleRewardOffers();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    // External-control compatibility shims.

    public string TryChooseRewardTypeExternally(string rewardType)
    {
        // No-op: with the new flow all categories are visible at once.
        return "OK (multi-category picker; no type switch needed).";
    }

    public string TryChooseRewardCardExternally(int? optionIndex, string cardId)
    {
        var state = GetNode<GameState>("/root/GameState");
        var idx = optionIndex ?? -1;
        if (idx < 0 && !string.IsNullOrEmpty(cardId))
        {
            idx = state.PendingRewardOptions.FindIndex(id =>
                string.Equals(id, cardId, System.StringComparison.OrdinalIgnoreCase));
        }

        if (!state.TakeRewardCardOption(idx))
        {
            return "Invalid card option index.";
        }

        RebuildCardSection();
        return "OK";
    }

    public string TrySkipRewardExternally()
    {
        OnContinuePressed();
        return "OK";
    }

    public RewardSnapshot BuildRewardSnapshot()
    {
        var state = GetNode<GameState>("/root/GameState");
        var snapshot = new RewardSnapshot
        {
            Mode = "multi_picker",
            RewardTypes = new List<string> { "card", "potion", "relic", "skip" }
        };

        for (var i = 0; i < state.PendingRewardOptions.Count; i++)
        {
            var card = CardData.CreateById(state.PendingRewardOptions[i]);
            snapshot.CardOptions.Add(new RewardOptionSnapshot
            {
                OptionIndex = i,
                Id = card.Id,
                Name = card.Name,
                Description = card.GetLocalizedDescription()
            });
        }

        for (var i = 0; i < state.PendingRelicOptions.Count; i++)
        {
            var relic = RelicData.CreateById(state.PendingRelicOptions[i]);
            snapshot.RelicOptions.Add(new RewardOptionSnapshot
            {
                OptionIndex = i,
                Id = relic.Id,
                Name = relic.Name,
                Description = relic.Description
            });
        }

        return snapshot;
    }
}
