using Godot;
using System.Collections.Generic;
using System.Text;

public partial class RewardScene : Control
{
    private enum RewardCategory { None, Cards, Potions, Relics }

    private Label _titleLabel = null!;
    private Label _summaryLabel = null!;
    private Button _continueButton = null!;

    private VBoxContainer _categoryView = null!;
    private Label _categoryHeaderLabel = null!;
    private GridContainer _categoryGrid = null!;

    private VBoxContainer _detailView = null!;
    private Button _backButton = null!;
    private Label _detailHeaderLabel = null!;
    private HBoxContainer _detailOptionsRow = null!;
    private Button _detailSkipButton = null!;

    private RewardCategory _currentCategory = RewardCategory.None;

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("reward");

        _titleLabel = GetNode<Label>("%Title");
        _summaryLabel = GetNode<Label>("%SummaryLabel");
        _continueButton = GetNode<Button>("%ContinueButton");

        _categoryView = GetNode<VBoxContainer>("%CategoryView");
        _categoryHeaderLabel = GetNode<Label>("%CategoryHeaderLabel");
        _categoryGrid = GetNode<GridContainer>("%CategoryGrid");

        _detailView = GetNode<VBoxContainer>("%DetailView");
        _backButton = GetNode<Button>("%BackButton");
        _detailHeaderLabel = GetNode<Label>("%DetailHeaderLabel");
        _detailOptionsRow = GetNode<HBoxContainer>("%DetailOptionsRow");
        _detailSkipButton = GetNode<Button>("%DetailSkipButton");

        _continueButton.Pressed += OnContinuePressed;
        _backButton.Pressed += OnBackPressed;
        _detailSkipButton.Pressed += OnDetailSkipPressed;

        LocalizationSettings.LanguageChanged += RebuildAll;
        ShowCategoryView();
        RebuildAll();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RebuildAll;
    }

    private void RebuildAll()
    {
        RefreshHeaderText();
        if (_currentCategory == RewardCategory.None)
        {
            RebuildCategoryGrid();
        }
        else
        {
            RebuildDetailView();
        }
    }

    private void ShowCategoryView()
    {
        _currentCategory = RewardCategory.None;
        _categoryView.Visible = true;
        _detailView.Visible = false;
    }

    private void ShowDetailView(RewardCategory category)
    {
        _currentCategory = category;
        _categoryView.Visible = false;
        _detailView.Visible = true;
        RebuildDetailView();
    }

    private void RefreshHeaderText()
    {
        var state = GetNode<GameState>("/root/GameState");
        var reward = state.LastBattleReward;

        var rawTitle = reward.IsEliteTier
            ? LocalizationService.Get("ui.reward.title_elite", "Elite Reward")
            : LocalizationService.Get("ui.reward.title_normal", "Battle Reward");
        _titleLabel.Text = "🏆 " + rawTitle;

        _continueButton.Text = "✓ " + LocalizationService.Get("ui.reward.continue", "Continue");
        _backButton.Text = "← " + LocalizationService.Get("ui.reward.back", "Back");

        var sb = new StringBuilder();
        sb.Append("💰 ");
        sb.Append(LocalizationService.Format("ui.reward.gold_line", "Gold +{0}", reward.GoldGained));

        if (reward.HealedFromCharm > 0)
        {
            sb.AppendLine();
            sb.Append("❤ ");
            sb.Append(LocalizationService.Format(
                "ui.reward.charm_heal",
                "Lucky Charm healed {0} HP.",
                reward.HealedFromCharm));
        }

        if (reward.HealedFromBloodVial > 0)
        {
            sb.AppendLine();
            sb.Append("❤ ");
            sb.Append(LocalizationService.Format(
                "ui.reward.blood_vial_heal",
                "Blood Vial healed {0} HP.",
                reward.HealedFromBloodVial));
        }

        _summaryLabel.Text = sb.ToString();
    }

    private void RebuildCategoryGrid()
    {
        ClearChildren(_categoryGrid);

        var state = GetNode<GameState>("/root/GameState");
        var cardCount = state.PendingRewardOptions.Count;
        var potionCount = state.PendingPotionRewardOptions.Count;
        var relicCount = state.PendingRelicOptions.Count;

        _categoryHeaderLabel.Text = LocalizationService.Get(
            "ui.reward.category_header",
            "Select a reward category");

        _categoryGrid.AddChild(BuildCategoryTile(
            "🎴",
            LocalizationService.Get("ui.reward.category_cards", "Cards"),
            cardCount,
            RewardCategory.Cards));

        _categoryGrid.AddChild(BuildCategoryTile(
            "🧪",
            LocalizationService.Get("ui.reward.category_potions", "Potions"),
            potionCount,
            RewardCategory.Potions));

        if (relicCount > 0)
        {
            _categoryGrid.AddChild(BuildCategoryTile(
                "💎",
                LocalizationService.Get("ui.reward.category_relics", "Relics"),
                relicCount,
                RewardCategory.Relics));
        }
    }

    private Button BuildCategoryTile(string icon, string label, int count, RewardCategory category)
    {
        var available = count > 0;
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(200, 168),
            Disabled = !available,
            ClipText = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        btn.AddThemeFontSizeOverride("font_size", 18);
        btn.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.78f, 1f));
        btn.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f, 1f));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.55f, 0.55f, 0.85f));
        btn.AddThemeStyleboxOverride("normal", BuildStyle(0.13f, 0.18f, 0.25f, 0.85f, 0.65f, 0.32f, available));
        btn.AddThemeStyleboxOverride("hover", BuildStyle(0.22f, 0.30f, 0.40f, 1f, 0.92f, 0.55f, available));
        btn.AddThemeStyleboxOverride("pressed", BuildStyle(0.10f, 0.14f, 0.20f, 0.85f, 0.65f, 0.32f, available));
        btn.AddThemeStyleboxOverride("disabled", BuildStyle(0.10f, 0.10f, 0.10f, 0.40f, 0.35f, 0.25f, false));

        var stateText = available
            ? LocalizationService.Format("ui.reward.category_count", "{0} available", count)
            : LocalizationService.Get("ui.reward.category_none", "—");
        btn.Text = $"{icon}\n{label}\n{stateText}";

        if (available)
        {
            btn.Pressed += () => ShowDetailView(category);
        }
        return btn;
    }

    private static StyleBoxFlat BuildStyle(float r, float g, float b, float br, float bg, float bb, bool active)
    {
        var alpha = active ? 0.96f : 0.78f;
        var borderAlpha = active ? 0.90f : 0.55f;
        return new StyleBoxFlat
        {
            BgColor = new Color(r, g, b, alpha),
            BorderColor = new Color(br, bg, bb, borderAlpha),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ShadowColor = new Color(0, 0, 0, active ? 0.45f : 0.20f),
            ShadowSize = active ? 6 : 2,
            ContentMarginLeft = 18,
            ContentMarginTop = 18,
            ContentMarginRight = 18,
            ContentMarginBottom = 18
        };
    }

    private void RebuildDetailView()
    {
        ClearChildren(_detailOptionsRow);

        switch (_currentCategory)
        {
            case RewardCategory.Cards: BuildCardDetail(); break;
            case RewardCategory.Potions: BuildPotionDetail(); break;
            case RewardCategory.Relics: BuildRelicDetail(); break;
        }
    }

    private void BuildCardDetail()
    {
        var state = GetNode<GameState>("/root/GameState");
        var options = state.PendingRewardOptions;

        _detailHeaderLabel.Text = "🎴 " + LocalizationService.Get(
            "ui.reward.card_section_label",
            "Pick 1 card or skip");
        _detailSkipButton.Text = "✕ " + LocalizationService.Get("ui.reward.skip_cards", "Skip cards");

        if (options.Count == 0)
        {
            ShowCategoryView();
            return;
        }

        for (var i = 0; i < options.Count; i++)
        {
            var captureIdx = i;
            var card = CardData.CreateById(options[i]);
            _detailOptionsRow.AddChild(BuildCardTile(card, () => OnTakeCard(captureIdx)));
        }
    }

    private Control BuildCardTile(CardData card, System.Action onPick)
    {
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(220, 300),
            MouseFilter = MouseFilterEnum.Pass
        };
        frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat
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
        });

        var view = new CardView();
        view.SetUseTopLevel(false);
        view.SetDragEnabled(false);
        view.Setup(card);
        view.Clicked = _ => onPick();
        frame.AddChild(view);
        return frame;
    }

    private void BuildPotionDetail()
    {
        var state = GetNode<GameState>("/root/GameState");
        var options = state.PendingPotionRewardOptions;
        var fullInventory = state.PotionIds.Count >= GameState.PotionInventoryCapacity;

        _detailHeaderLabel.Text = "🧪 " + (fullInventory
            ? LocalizationService.Get(
                "ui.reward.potion_section_label_full",
                "Potion belt is full — skip or replace nothing")
            : LocalizationService.Get(
                "ui.reward.potion_section_label",
                "Pick 1 potion or skip"));
        _detailSkipButton.Text = "✕ " + LocalizationService.Get("ui.reward.skip_potions", "Skip potions");

        if (options.Count == 0)
        {
            ShowCategoryView();
            return;
        }

        for (var i = 0; i < options.Count; i++)
        {
            var captureIdx = i;
            var potion = PotionData.CreateById(options[i]);
            _detailOptionsRow.AddChild(BuildPotionTile(potion, fullInventory, () => OnTakePotion(captureIdx)));
        }
    }

    private Control BuildPotionTile(PotionData potion, bool disabled, System.Action onPick)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(240, 160),
            ClipText = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Disabled = disabled
        };
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", new Color(0.95f, 1f, 0.92f, 1));
        btn.AddThemeColorOverride("font_hover_color", new Color(1, 1, 1, 1));
        btn.AddThemeStyleboxOverride("normal", BuildStyle(0.10f, 0.20f, 0.16f, 0.65f, 0.95f, 0.65f, !disabled));
        btn.AddThemeStyleboxOverride("hover", BuildStyle(0.16f, 0.28f, 0.22f, 0.85f, 1f, 0.78f, !disabled));
        btn.AddThemeStyleboxOverride("pressed", BuildStyle(0.08f, 0.16f, 0.12f, 0.65f, 0.95f, 0.65f, !disabled));
        btn.AddThemeStyleboxOverride("disabled", BuildStyle(0.10f, 0.10f, 0.10f, 0.40f, 0.40f, 0.35f, false));

        var name = LocalizationService.Get($"potion.{potion.Id}.name", potion.Name);
        var desc = LocalizationService.Get($"potion.{potion.Id}.description", potion.Description);
        btn.Text = $"🧪 {name}\n\n{desc}";
        if (!disabled)
        {
            btn.Pressed += () => onPick();
        }
        return btn;
    }

    private void BuildRelicDetail()
    {
        var state = GetNode<GameState>("/root/GameState");
        var options = state.PendingRelicOptions;

        _detailHeaderLabel.Text = "💎 " + LocalizationService.Get(
            "ui.reward.relic_section_label",
            "Pick 1 relic or skip");
        _detailSkipButton.Text = "✕ " + LocalizationService.Get("ui.reward.skip_relics", "Skip relic");

        if (options.Count == 0)
        {
            ShowCategoryView();
            return;
        }

        for (var i = 0; i < options.Count; i++)
        {
            var captureIdx = i;
            var relic = RelicData.CreateById(options[i]);
            _detailOptionsRow.AddChild(BuildRelicTile(relic, () => OnTakeRelic(captureIdx)));
        }
    }

    private Control BuildRelicTile(RelicData relic, System.Action onPick)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(260, 160),
            ClipText = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.80f, 1));
        btn.AddThemeColorOverride("font_hover_color", new Color(1, 1, 1, 1));
        btn.AddThemeStyleboxOverride("normal", BuildStyle(0.18f, 0.14f, 0.08f, 0.95f, 0.78f, 0.40f, true));
        btn.AddThemeStyleboxOverride("hover", BuildStyle(0.26f, 0.20f, 0.12f, 1f, 0.92f, 0.55f, true));
        btn.AddThemeStyleboxOverride("pressed", BuildStyle(0.14f, 0.10f, 0.06f, 0.95f, 0.78f, 0.40f, true));

        btn.Text = $"💎 {relic.LocalizedName}\n\n{relic.LocalizedDescription}";
        btn.Pressed += () => onPick();
        return btn;
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
        ShowCategoryView();
        RebuildCategoryGrid();
    }

    private void OnTakePotion(int idx)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.TakeRewardPotionOption(idx);
        ShowCategoryView();
        RebuildCategoryGrid();
    }

    private void OnTakeRelic(int idx)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.TakeRewardRelicOption(idx);
        ShowCategoryView();
        RebuildCategoryGrid();
    }

    private void OnDetailSkipPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        switch (_currentCategory)
        {
            case RewardCategory.Cards: state.SkipRewardCards(); break;
            case RewardCategory.Potions: state.SkipRewardPotions(); break;
            case RewardCategory.Relics: state.SkipRewardRelics(); break;
        }
        ShowCategoryView();
        RebuildCategoryGrid();
    }

    private void OnBackPressed()
    {
        ShowCategoryView();
        RebuildCategoryGrid();
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

        ShowCategoryView();
        RebuildCategoryGrid();
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
