using Godot;
using System;
using System.Collections.Generic;

public partial class RewardScene : Control
{
    private readonly List<Control> _cardSlots = new();
    private readonly List<RewardCardOptionView> _cardPreviewViews = new();
    private readonly Random _rng = new();
    private readonly PackedScene _rewardCardOptionScene = GD.Load<PackedScene>("res://Scenes/RewardCardOptionView.tscn");

    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Button _relicRewardButton = null!;
    private Button _cardPackRewardButton = null!;
    private Button _potionRewardButton = null!;
    private Button _randomRewardButton = null!;
    private Button _skipButton = null!;
    private Control _rewardTypeWrap = null!;
    private Control _cardPackWrap = null!;

    private bool _isChoosingFromCardPack;
    private bool _isChoosingPotionReplacement;
    private string _pendingPotionReplacementId = string.Empty;
    private bool _isPreviewingRelicReward;
    private bool _isPreviewingPotionReward;
    private string _pendingRelicRewardId = string.Empty;
    private string _pendingPotionRewardId = string.Empty;
    private bool _pendingPotionFromRelicFallback;

    public override void _Ready()
    {
        GetNode<GameState>("/root/GameState").SetUiPhase("reward");
        _titleLabel = GetNode<Label>("%Title");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _relicRewardButton = GetNode<Button>("%RelicRewardButton");
        _cardPackRewardButton = GetNode<Button>("%CardPackRewardButton");
        _potionRewardButton = GetNode<Button>("%PotionRewardButton");
        _randomRewardButton = GetNode<Button>("%RandomRewardButton");
        _skipButton = GetNode<Button>("%SkipButton");
        _rewardTypeWrap = GetNode<Control>("Margin/RootVBox/RewardTypeWrap");
        _cardPackWrap = GetNode<Control>("Margin/RootVBox/CardPackWrap");

        _cardSlots.Add(GetNode<Control>("%CardSlot1"));
        _cardSlots.Add(GetNode<Control>("%CardSlot2"));
        _cardSlots.Add(GetNode<Control>("%CardSlot3"));

        _relicRewardButton.Pressed += OnPickRelicReward;
        _cardPackRewardButton.Pressed += OnPickCardPackReward;
        _potionRewardButton.Pressed += OnPickPotionReward;
        _skipButton.Pressed += OnSkipPressed;
        LocalizationSettings.LanguageChanged += RefreshUiText;

        ShowRewardTypeSelection();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RefreshUiText;
    }

    private void ShowRewardTypeSelection()
    {
        ClearRewardSubState();
        RefreshUiText();
        SetRewardTypeState();
    }

    private void RefreshUiText()
    {
        if (_isChoosingPotionReplacement)
        {
            RefreshPotionReplacementText();
            return;
        }

        if (_isPreviewingRelicReward)
        {
            RefreshRelicPreviewText();
            return;
        }

        if (_isPreviewingPotionReward)
        {
            RefreshPotionPreviewText();
            return;
        }

        _relicRewardButton.Text = LocalizationService.Get("ui.reward.relic_reward", "遗物");
        _cardPackRewardButton.Text = LocalizationService.Get("ui.reward.card_pack_reward", "卡牌包");
        _potionRewardButton.Text = LocalizationService.Get("ui.reward.potion_reward", "药水");
        _skipButton.Text = _isChoosingFromCardPack
            ? LocalizationService.Get("ui.reward.back_to_reward_list", "返回奖励列表")
            : LocalizationService.Get("ui.common.skip", "跳过");
        _titleLabel.Text = _isChoosingFromCardPack
            ? LocalizationService.Get("ui.reward.choose_one_card", "选择一张牌")
            : LocalizationService.Get("ui.reward.post_battle", "战后奖励");
        if (!_isChoosingFromCardPack)
        {
            _statusLabel.Text = LocalizationService.Get(
                "ui.reward.reward_type_status",
                "选择一种奖励：遗物 / 卡牌包 / 药水");
        }
    }

    private void SetRewardTypeState()
    {
        ClearRewardSubState();

        _titleLabel.Text = LocalizationService.Get("ui.reward.post_battle", "战后奖励");
        _statusLabel.Text = LocalizationService.Get(
            "ui.reward.reward_type_status",
            "选择一种奖励：遗物 / 卡牌包 / 药水");
        _rewardTypeWrap.Visible = true;
        _cardPackWrap.Visible = false;
        _relicRewardButton.Visible = true;
        _cardPackRewardButton.Visible = true;
        _potionRewardButton.Visible = true;
        _randomRewardButton.Visible = false;
        _randomRewardButton.Disabled = true;

        ClearCardPreviews();

        _skipButton.Text = LocalizationService.Get("ui.common.skip", "跳过");
    }

    private void ClearRewardSubState()
    {
        _isChoosingFromCardPack = false;
        _isChoosingPotionReplacement = false;
        _isPreviewingRelicReward = false;
        _isPreviewingPotionReward = false;
        _pendingPotionReplacementId = string.Empty;
        _pendingRelicRewardId = string.Empty;
        _pendingPotionRewardId = string.Empty;
        _pendingPotionFromRelicFallback = false;
    }

    private void ReturnToRewardTypeSelection(string status)
    {
        SetRewardTypeState();
        if (!string.IsNullOrWhiteSpace(status))
        {
            _statusLabel.Text = status;
        }
    }

    private static PotionData RollRandomPotionData(Random rng)
    {
        var pool = PotionData.AllPotionIds();
        if (pool.Count == 0)
        {
            return PotionData.CreateById("healing_potion");
        }

        var potionId = pool[rng.Next(pool.Count)];
        return PotionData.CreateById(potionId);
    }

    private PotionData GetPendingPreviewPotion()
    {
        return string.IsNullOrWhiteSpace(_pendingPotionRewardId)
            ? PotionData.CreateById("healing_potion")
            : PotionData.CreateById(_pendingPotionRewardId);
    }

    private void ClearCardPreviews()
    {
        foreach (var view in _cardPreviewViews)
        {
            if (IsInstanceValid(view))
            {
                view.QueueFree();
            }
        }

        _cardPreviewViews.Clear();
    }

    private void OnPickRelicReward()
    {
        if (_isChoosingPotionReplacement)
        {
            OnPickPotionReplacementAt(0);
            return;
        }

        if (_isPreviewingRelicReward)
        {
            ConfirmRelicReward();
            return;
        }

        if (_isPreviewingPotionReward)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        state.RollRelicOptions(3);

        if (state.PendingRelicOptions.Count == 0)
        {
            state.PendingRelicOptions.Clear();
            var fallbackPotion = RollRandomPotionData(_rng);
            StartPotionRewardPreview(fallbackPotion, fromRelicFallback: true);
            return;
        }

        var relicId = state.PendingRelicOptions[_rng.Next(state.PendingRelicOptions.Count)];
        state.PendingRelicOptions.Clear();
        StartRelicRewardPreview(relicId);
    }

    private void OnPickCardPackReward()
    {
        if (_isChoosingPotionReplacement)
        {
            OnPickPotionReplacementAt(1);
            return;
        }

        if (_isPreviewingRelicReward || _isPreviewingPotionReward)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        state.RollRewardOptions(3);

        if (state.PendingRewardOptions.Count == 0)
        {
            ReturnToRewardTypeSelection(LocalizationService.Get("ui.reward.card_pack_empty", "当前没有可用卡牌奖励。"));
            return;
        }

        _isChoosingFromCardPack = true;
        _titleLabel.Text = LocalizationService.Get("ui.reward.choose_one_card", "选择一张牌");
        _statusLabel.Text = LocalizationService.Get("ui.reward.select_one_of_three_cards", "从 3 张卡牌中选择 1 张加入牌库。");

        _rewardTypeWrap.Visible = false;
        _cardPackWrap.Visible = true;
        _relicRewardButton.Visible = false;
        _cardPackRewardButton.Visible = false;
        _potionRewardButton.Visible = false;
        _randomRewardButton.Visible = false;
        _skipButton.Text = LocalizationService.Get("ui.reward.back_to_reward_list", "返回奖励列表");

        BuildCardPreviews(state.PendingRewardOptions);
    }

    private void BuildCardPreviews(IReadOnlyList<string> cardIds)
    {
        ClearCardPreviews();

        for (var i = 0; i < _cardSlots.Count; i++)
        {
            var slot = _cardSlots[i];
            foreach (Node child in slot.GetChildren())
            {
                child.QueueFree();
            }

            if (i >= cardIds.Count)
            {
                continue;
            }

            var cardId = cardIds[i];
            var card = CardData.CreateById(cardId);
            var optionView = _rewardCardOptionScene.Instantiate<RewardCardOptionView>();
            optionView.Setup(card);
            optionView.GuiInput += @event =>
            {
                if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                {
                    OnPickCardFromPack(cardId);
                    optionView.AcceptEvent();
                }
            };
            slot.AddChild(optionView);
            _cardPreviewViews.Add(optionView);
        }
    }

    private void OnPickCardFromPack(string cardId)
    {
        var state = GetNode<GameState>("/root/GameState");
        state.AddCardToDeck(cardId);
        state.PendingRewardOptions.Clear();
        ExitToMap(LocalizationService.Format("ui.reward.got_card", "获得卡牌：{0}", CardData.CreateById(cardId).Name));
    }

    private void OnPickPotionReward()
    {
        if (_isChoosingPotionReplacement)
        {
            OnPickPotionReplacementAt(2);
            return;
        }

        if (_isPreviewingPotionReward)
        {
            ConfirmPotionReward();
            return;
        }

        if (_isPreviewingRelicReward)
        {
            return;
        }

        var potion = RollRandomPotionData(_rng);
        StartPotionRewardPreview(potion, fromRelicFallback: false);
    }

    private void OnPickRandomReward()
    {
        ReturnToRewardTypeSelection(LocalizationService.Get("ui.reward.random_disabled", "随机奖励已停用。"));
    }

    private void OnSkipPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PendingRewardOptions.Clear();

        if (_isPreviewingRelicReward)
        {
            ReturnToRewardTypeSelection(LocalizationService.Get(
                "ui.reward.back_from_relic_preview",
                "你放弃了本次遗物奖励，已返回奖励列表。"));
            return;
        }

        if (_isPreviewingPotionReward)
        {
            ReturnToRewardTypeSelection(LocalizationService.Get(
                "ui.reward.back_from_potion_preview",
                "你放弃了本次药水奖励，已返回奖励列表。"));
            return;
        }

        if (_isChoosingPotionReplacement)
        {
            ReturnToRewardTypeSelection(LocalizationService.Get(
                "ui.reward.back_from_potion_replace",
                "你放弃了本次药水替换，已返回奖励列表。"));
            return;
        }

        if (_isChoosingFromCardPack)
        {
            ReturnToRewardTypeSelection(LocalizationService.Get(
                "ui.reward.back_from_card_pack",
                "你放弃了本次卡牌包奖励，已返回奖励列表。"));
            return;
        }

        ExitToMap(LocalizationService.Get("ui.reward.skip_reward", "你跳过了战后奖励。"), clearCardPack: true);
    }

    private void StartRelicRewardPreview(string relicId)
    {
        _isChoosingFromCardPack = false;
        _isChoosingPotionReplacement = false;
        _isPreviewingPotionReward = false;
        _pendingPotionRewardId = string.Empty;
        _pendingPotionFromRelicFallback = false;
        _pendingPotionReplacementId = string.Empty;

        _isPreviewingRelicReward = true;
        _pendingRelicRewardId = relicId;

        _rewardTypeWrap.Visible = true;
        _cardPackWrap.Visible = false;
        ClearCardPreviews();
        RefreshRelicPreviewText();
    }

    private void RefreshRelicPreviewText()
    {
        var relic = string.IsNullOrWhiteSpace(_pendingRelicRewardId)
            ? RelicData.CreateById("lantern")
            : RelicData.CreateById(_pendingRelicRewardId);

        _titleLabel.Text = LocalizationService.Get("ui.reward.post_battle", "战后奖励");
        _statusLabel.Text = LocalizationService.Format(
            "ui.reward.preview_relic_status",
            "确认获得遗物：{0}\\n{1}",
            relic.LocalizedName,
            relic.Description);

        _relicRewardButton.Visible = true;
        _relicRewardButton.Text = LocalizationService.Get("ui.reward.confirm_relic", "确认遗物奖励");
        _cardPackRewardButton.Visible = false;
        _potionRewardButton.Visible = false;
        _randomRewardButton.Visible = false;
        _skipButton.Text = LocalizationService.Get("ui.reward.back_to_reward_list", "返回奖励列表");
    }

    private void ConfirmRelicReward()
    {
        var state = GetNode<GameState>("/root/GameState");
        var relicId = string.IsNullOrWhiteSpace(_pendingRelicRewardId) ? "lantern" : _pendingRelicRewardId;
        var relic = RelicData.CreateById(relicId);

        state.AddRelic(relicId);
        _isPreviewingRelicReward = false;
        _pendingRelicRewardId = string.Empty;
        ExitToMap(LocalizationService.Format("ui.reward.got_relic", "获得遗物：{0}", relic.LocalizedName));
    }

    private void StartPotionRewardPreview(PotionData potion, bool fromRelicFallback)
    {
        var state = GetNode<GameState>("/root/GameState");
        if (state.PotionIds.Count >= GameState.PotionInventoryCapacity)
        {
            StartPotionReplacementSelection(potion);
            return;
        }

        _isChoosingFromCardPack = false;
        _isChoosingPotionReplacement = false;
        _isPreviewingRelicReward = false;
        _pendingRelicRewardId = string.Empty;
        _pendingPotionReplacementId = string.Empty;

        _isPreviewingPotionReward = true;
        _pendingPotionRewardId = potion.Id;
        _pendingPotionFromRelicFallback = fromRelicFallback;

        _rewardTypeWrap.Visible = true;
        _cardPackWrap.Visible = false;
        ClearCardPreviews();
        RefreshPotionPreviewText();
    }

    private void RefreshPotionPreviewText()
    {
        var potion = GetPendingPreviewPotion();
        _titleLabel.Text = LocalizationService.Get("ui.reward.post_battle", "战后奖励");
        _statusLabel.Text = LocalizationService.Format(
            "ui.reward.preview_potion_status",
            "确认获得药水：{0}\\n{1}",
            potion.Name,
            potion.Description);

        _relicRewardButton.Visible = false;
        _cardPackRewardButton.Visible = false;
        _potionRewardButton.Visible = true;
        _potionRewardButton.Text = LocalizationService.Get("ui.reward.confirm_potion", "确认药水奖励");
        _randomRewardButton.Visible = false;
        _skipButton.Text = LocalizationService.Get("ui.reward.back_to_reward_list", "返回奖励列表");
    }

    private void ConfirmPotionReward()
    {
        var state = GetNode<GameState>("/root/GameState");
        var potion = GetPendingPreviewPotion();

        if (!state.TryAddPotion(potion.Id))
        {
            StartPotionReplacementSelection(potion);
            return;
        }

        var fromRelicFallback = _pendingPotionFromRelicFallback;
        _isPreviewingPotionReward = false;
        _pendingPotionRewardId = string.Empty;
        _pendingPotionFromRelicFallback = false;

        if (fromRelicFallback)
        {
            ExitToMap(LocalizationService.Format("ui.reward.pending_relic_full", "遗物已拿满，改为获得药水：{0}", potion.Name));
            return;
        }

        ExitToMap(LocalizationService.Format("ui.reward.got_potion", "获得药水：{0}\\n{1}", potion.Name, potion.Description));
    }

    private void StartPotionReplacementSelection(PotionData incomingPotion)
    {
        _isChoosingFromCardPack = false;
        _isPreviewingRelicReward = false;
        _isPreviewingPotionReward = false;
        _pendingRelicRewardId = string.Empty;
        _pendingPotionRewardId = string.Empty;
        _pendingPotionFromRelicFallback = false;
        _isChoosingPotionReplacement = true;
        _pendingPotionReplacementId = incomingPotion.Id;

        _rewardTypeWrap.Visible = true;
        _cardPackWrap.Visible = false;
        ClearCardPreviews();

        RefreshPotionReplacementText();
    }

    private void RefreshPotionReplacementText()
    {
        var incomingPotion = GetPendingReplacementPotion();
        _titleLabel.Text = LocalizationService.Get("ui.reward.potion_replace_title", "药水栏已满");
        _statusLabel.Text = LocalizationService.Format(
            "ui.reward.potion_replace_status",
            "药水栏已满。请选择一瓶丢弃，以获得：{0}\\n{1}",
            incomingPotion.Name,
            incomingPotion.Description);
        _skipButton.Text = LocalizationService.Get("ui.reward.back_to_reward_list", "返回奖励列表");

        var state = GetNode<GameState>("/root/GameState");
        ConfigurePotionReplacementButton(_relicRewardButton, state, 0);
        ConfigurePotionReplacementButton(_cardPackRewardButton, state, 1);
        ConfigurePotionReplacementButton(_potionRewardButton, state, 2);
        _randomRewardButton.Visible = false;
    }

    private void ConfigurePotionReplacementButton(Button button, GameState state, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= state.PotionIds.Count)
        {
            button.Visible = false;
            return;
        }

        var potion = PotionData.CreateById(state.PotionIds[slotIndex]);
        button.Visible = true;
        button.Text = LocalizationService.Format("ui.reward.potion_replace_option", "丢弃：{0}", potion.Name);
    }

    private PotionData GetPendingReplacementPotion()
    {
        return string.IsNullOrWhiteSpace(_pendingPotionReplacementId)
            ? PotionData.CreateById("healing_potion")
            : PotionData.CreateById(_pendingPotionReplacementId);
    }

    private void OnPickPotionReplacementAt(int slotIndex)
    {
        if (!_isChoosingPotionReplacement)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        var incomingPotion = GetPendingReplacementPotion();

        if (!state.TryConsumePotionAt(slotIndex, out var removedPotion))
        {
            _statusLabel.Text = LocalizationService.Get("ui.reward.potion_replace_invalid", "请选择一个有效的药水槽位。");
            return;
        }

        if (!state.TryAddPotion(incomingPotion.Id))
        {
            state.TryAddPotion(removedPotion.Id);
            _statusLabel.Text = LocalizationService.Format(
                "ui.reward.potion_inventory_full",
                "药水栏已满（最多 {0} 瓶），未获得新药水。",
                GameState.PotionInventoryCapacity);
            RefreshPotionReplacementText();
            return;
        }

        _isChoosingPotionReplacement = false;
        _pendingPotionReplacementId = string.Empty;
        ExitToMap(LocalizationService.Format(
            "ui.reward.potion_replace_done",
            "丢弃药水：{0}\\n获得药水：{1}\\n{2}",
            removedPotion.Name,
            incomingPotion.Name,
            incomingPotion.Description));
    }

    private void ExitToMap(string message, bool clearCardPack = false)
    {
        var state = GetNode<GameState>("/root/GameState");
        if (clearCardPack)
        {
            state.PendingRewardOptions.Clear();
        }

        state.SetUiPhase("map");
        GD.Print(message);
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    public RewardSnapshot BuildRewardSnapshot()
    {
        var state = GetNode<GameState>("/root/GameState");
        var snapshot = new RewardSnapshot
        {
            Mode = _isChoosingFromCardPack
                ? "card_pack"
                : _isChoosingPotionReplacement
                    ? "potion_replace"
                    : _isPreviewingRelicReward
                        ? "relic_preview"
                        : _isPreviewingPotionReward
                            ? "potion_preview"
                            : "reward_type",
            RewardTypes = _isChoosingPotionReplacement
                ? new List<string> { "relic", "card_pack", "potion", "skip" }
                : _isPreviewingRelicReward
                    ? new List<string> { "relic", "skip" }
                    : _isPreviewingPotionReward
                        ? new List<string> { "potion", "skip" }
                        : new List<string> { "relic", "card_pack", "potion", "skip" }
        };

        if (_isPreviewingRelicReward)
        {
            var relic = string.IsNullOrWhiteSpace(_pendingRelicRewardId)
                ? RelicData.CreateById("lantern")
                : RelicData.CreateById(_pendingRelicRewardId);
            snapshot.RelicOptions.Add(new RewardOptionSnapshot
            {
                OptionIndex = 0,
                Id = relic.Id,
                Name = relic.Name,
                Description = relic.Description
            });
            return snapshot;
        }

        if (_isPreviewingPotionReward)
        {
            var potion = GetPendingPreviewPotion();
            snapshot.RelicOptions.Add(new RewardOptionSnapshot
            {
                OptionIndex = 0,
                Id = potion.Id,
                Name = potion.Name,
                Description = potion.Description
            });
            return snapshot;
        }

        if (_isChoosingPotionReplacement)
        {
            for (var i = 0; i < state.PotionIds.Count; i++)
            {
                var potion = PotionData.CreateById(state.PotionIds[i]);
                snapshot.RelicOptions.Add(new RewardOptionSnapshot
                {
                    OptionIndex = i,
                    Id = potion.Id,
                    Name = potion.Name,
                    Description = potion.Description
                });
            }

            return snapshot;
        }

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

    public List<LegalActionSnapshot> BuildLegalActions()
    {
        var actions = new List<LegalActionSnapshot>();
        if (_isPreviewingRelicReward)
        {
            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_reward_type",
                Label = "Confirm relic reward",
                Parameters = new Dictionary<string, object?>
                {
                    ["rewardType"] = "relic"
                }
            });

            actions.Add(new LegalActionSnapshot
            {
                Kind = "skip_reward",
                Label = "Back to reward list"
            });
            return actions;
        }

        if (_isPreviewingPotionReward)
        {
            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_reward_type",
                Label = "Confirm potion reward",
                Parameters = new Dictionary<string, object?>
                {
                    ["rewardType"] = "potion"
                }
            });

            actions.Add(new LegalActionSnapshot
            {
                Kind = "skip_reward",
                Label = "Back to reward list"
            });
            return actions;
        }

        if (_isChoosingPotionReplacement)
        {
            var state = GetNode<GameState>("/root/GameState");
            var rewardTypes = new[] { "relic", "card_pack", "potion" };
            for (var i = 0; i < Math.Min(state.PotionIds.Count, rewardTypes.Length); i++)
            {
                var potion = PotionData.CreateById(state.PotionIds[i]);
                actions.Add(new LegalActionSnapshot
                {
                    Kind = "choose_reward_type",
                    Label = $"Discard potion slot {i}: {potion.Name}",
                    Parameters = new Dictionary<string, object?>
                    {
                        ["rewardType"] = rewardTypes[i]
                    }
                });
            }

            actions.Add(new LegalActionSnapshot
            {
                Kind = "skip_reward",
                Label = "Back to reward list"
            });
            return actions;
        }

        if (_isChoosingFromCardPack)
        {
            var state = GetNode<GameState>("/root/GameState");
            for (var i = 0; i < state.PendingRewardOptions.Count; i++)
            {
                actions.Add(new LegalActionSnapshot
                {
                    Kind = "choose_reward_card",
                    Label = $"Choose reward card option {i}",
                    Parameters = new Dictionary<string, object?>
                    {
                        ["optionIndex"] = i,
                        ["cardId"] = state.PendingRewardOptions[i]
                    }
                });
            }

            actions.Add(new LegalActionSnapshot
            {
                Kind = "skip_reward",
                Label = "Back to reward list"
            });
            return actions;
        }

        foreach (var rewardType in new[] { "relic", "card_pack", "potion" })
        {
            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_reward_type",
                Label = $"Choose reward type {rewardType}",
                Parameters = new Dictionary<string, object?>
                {
                    ["rewardType"] = rewardType
                }
            });
        }

        actions.Add(new LegalActionSnapshot
        {
            Kind = "skip_reward",
            Label = "Skip this reward scene"
        });
        return actions;
    }

    public string? TryChooseRewardTypeExternally(string? rewardType)
    {
        if (_isChoosingFromCardPack)
        {
            return "Reward type can no longer be changed after opening the card pack.";
        }

        var normalized = rewardType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (_isPreviewingRelicReward)
        {
            switch (normalized)
            {
                case "relic":
                    OnPickRelicReward();
                    return null;
                case "skip":
                    OnSkipPressed();
                    return null;
                default:
                    return $"Unsupported relic preview action '{rewardType}'. Use relic/skip.";
            }
        }

        if (_isPreviewingPotionReward)
        {
            switch (normalized)
            {
                case "potion":
                    OnPickPotionReward();
                    return null;
                case "skip":
                    OnSkipPressed();
                    return null;
                default:
                    return $"Unsupported potion preview action '{rewardType}'. Use potion/skip.";
            }
        }

        if (_isChoosingPotionReplacement)
        {
            switch (normalized)
            {
                case "relic":
                    OnPickRelicReward();
                    return null;
                case "card_pack":
                    OnPickCardPackReward();
                    return null;
                case "potion":
                    OnPickPotionReward();
                    return null;
                case "skip":
                    OnSkipPressed();
                    return null;
                default:
                    return $"Unsupported replacement action '{rewardType}'. Use relic/card_pack/potion/skip.";
            }
        }

        switch (normalized)
        {
            case "relic":
                OnPickRelicReward();
                return null;
            case "card_pack":
                OnPickCardPackReward();
                return null;
            case "potion":
                OnPickPotionReward();
                return null;
            case "random":
                return LocalizationService.Get("ui.reward.random_disabled", "Random reward is disabled.");
            case "skip":
                OnSkipPressed();
                return null;
            default:
                return $"Unsupported reward type '{rewardType}'.";
        }
    }

    public string? TryChooseRewardCardExternally(int? optionIndex, string? cardId)
    {
        if (!_isChoosingFromCardPack)
        {
            return "choose_reward_card is only available after selecting the card pack reward.";
        }

        var state = GetNode<GameState>("/root/GameState");
        var resolvedIndex = -1;
        if (optionIndex.HasValue && optionIndex.Value >= 0 && optionIndex.Value < state.PendingRewardOptions.Count)
        {
            resolvedIndex = optionIndex.Value;
        }
        else if (!string.IsNullOrWhiteSpace(cardId))
        {
            resolvedIndex = state.PendingRewardOptions.FindIndex(id =>
                string.Equals(id, cardId, StringComparison.OrdinalIgnoreCase));
        }

        if (resolvedIndex < 0 || resolvedIndex >= state.PendingRewardOptions.Count)
        {
            return "Requested reward card is not available.";
        }

        OnPickCardFromPack(state.PendingRewardOptions[resolvedIndex]);
        return null;
    }

    public string? TrySkipRewardExternally()
    {
        OnSkipPressed();
        return null;
    }
}
