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

    public override void _Ready()
    {
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
        _randomRewardButton.Pressed += OnPickRandomReward;
        _skipButton.Pressed += OnSkipPressed;

        ShowRewardTypeSelection();
    }

    private void ShowRewardTypeSelection()
    {
        _isChoosingFromCardPack = false;
        _titleLabel.Text = "战后奖励";
        _statusLabel.Text = "选择一种奖励：遗物 / 卡牌包 / 药水 / 随机奖励";

        _rewardTypeWrap.Visible = true;
        _cardPackWrap.Visible = false;
        _relicRewardButton.Visible = true;
        _cardPackRewardButton.Visible = true;
        _potionRewardButton.Visible = true;
        _randomRewardButton.Visible = true;

        ClearCardPreviews();

        _skipButton.Text = "跳过";
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
        var state = GetNode<GameState>("/root/GameState");
        state.RollRelicOptions(3);

        if (state.PendingRelicOptions.Count == 0)
        {
            var fallbackPotion = state.AddRandomPotion();
            state.PendingRelicOptions.Clear();
            ExitToMap($"遗物已拿满，改为获得药水：{fallbackPotion.Name}");
            return;
        }

        var relicId = state.PendingRelicOptions[_rng.Next(state.PendingRelicOptions.Count)];
        var relic = RelicData.CreateById(relicId);
        state.AddRelic(relicId);
        state.PendingRelicOptions.Clear();
        ExitToMap($"获得遗物：{relic.Name}");
    }

    private void OnPickCardPackReward()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.RollRewardOptions(3);

        if (state.PendingRewardOptions.Count == 0)
        {
            ExitToMap("当前没有可用卡牌奖励。");
            return;
        }

        _isChoosingFromCardPack = true;
        _titleLabel.Text = "选择一张牌";
        _statusLabel.Text = "从 3 张卡牌中选择 1 张加入牌库。";

        _rewardTypeWrap.Visible = false;
        _cardPackWrap.Visible = true;
        _relicRewardButton.Visible = false;
        _cardPackRewardButton.Visible = false;
        _potionRewardButton.Visible = false;
        _randomRewardButton.Visible = false;
        _skipButton.Text = "跳过";

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
        ExitToMap($"获得卡牌：{CardData.CreateById(cardId).Name}");
    }

    private void OnPickPotionReward()
    {
        var state = GetNode<GameState>("/root/GameState");
        var potion = state.AddRandomPotion();
        ExitToMap($"获得药水：{potion.Name}\n{potion.Description}");
    }

    private void OnPickRandomReward()
    {
        var state = GetNode<GameState>("/root/GameState");

        var choices = new List<string> { "relic", "card_pack", "potion" };
        if (state.RelicIds.Count >= RelicData.AllRelicIds().Count)
        {
            choices.Remove("relic");
        }

        if (choices.Count == 0)
        {
            choices.Add("potion");
        }

        var pick = choices[_rng.Next(choices.Count)];
        switch (pick)
        {
            case "relic":
                OnPickRelicReward();
                break;
            case "card_pack":
                _statusLabel.Text = "随机奖励：卡牌包";
                OnPickCardPackReward();
                break;
            default:
                OnPickPotionReward();
                break;
        }
    }

    private void OnSkipPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PendingRewardOptions.Clear();

        if (_isChoosingFromCardPack)
        {
            ExitToMap("你跳过了卡牌包奖励。", clearCardPack: true);
            return;
        }

        ExitToMap("你跳过了战后奖励。", clearCardPack: true);
    }

    private void ExitToMap(string message, bool clearCardPack = false)
    {
        var state = GetNode<GameState>("/root/GameState");
        if (clearCardPack)
        {
            state.PendingRewardOptions.Clear();
        }

        GD.Print(message);
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }
}
