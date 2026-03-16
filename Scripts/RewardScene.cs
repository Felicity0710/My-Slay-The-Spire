using Godot;
using System;
using System.Collections.Generic;

public partial class RewardScene : Control
{
    private readonly List<Button> _cardOptionButtons = new();
    private readonly Random _rng = new();

    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Button _relicRewardButton = null!;
    private Button _cardPackRewardButton = null!;
    private Button _potionRewardButton = null!;
    private Button _randomRewardButton = null!;
    private Button _skipButton = null!;

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

        _cardOptionButtons.Add(GetNode<Button>("%OptionButton1"));
        _cardOptionButtons.Add(GetNode<Button>("%OptionButton2"));
        _cardOptionButtons.Add(GetNode<Button>("%OptionButton3"));

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

        _relicRewardButton.Visible = true;
        _cardPackRewardButton.Visible = true;
        _potionRewardButton.Visible = true;
        _randomRewardButton.Visible = true;

        foreach (var button in _cardOptionButtons)
        {
            button.Visible = false;
            button.Disabled = true;
            button.Text = string.Empty;
        }

        _skipButton.Text = "Skip";
    }

    private void OnPickRelicReward()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.RollRelicOptions(3);

        if (state.PendingRelicOptions.Count == 0)
        {
            state.AddPotionCharge(1);
            state.PendingRelicOptions.Clear();
            ExitToMap("遗物已拿满，改为获得 1 瓶药水。");
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
        _titleLabel.Text = "卡牌包（3 选 1）";
        _statusLabel.Text = "从 3 张卡中选择 1 张加入牌库。";

        _relicRewardButton.Visible = false;
        _cardPackRewardButton.Visible = false;
        _potionRewardButton.Visible = false;
        _randomRewardButton.Visible = false;
        _skipButton.Text = "Skip Card Pack";

        for (var i = 0; i < _cardOptionButtons.Count; i++)
        {
            var button = _cardOptionButtons[i];
            button.Visible = false;
            button.Disabled = true;
            button.Text = string.Empty;

            if (i >= state.PendingRewardOptions.Count)
            {
                continue;
            }

            var cardId = state.PendingRewardOptions[i];
            var card = CardData.CreateById(cardId);
            button.Text = card.ToCardText();
            button.Visible = true;
            button.Disabled = false;
            button.Pressed += () => OnPickCardFromPack(cardId);
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
        state.AddPotionCharge(1);
        ExitToMap("获得 1 瓶药水。\n药水会在地图页显示（当前版本暂未支持战斗中使用）。");
    }

    private void OnPickRandomReward()
    {
        var state = GetNode<GameState>("/root/GameState");

        var choices = new List<string> { "relic", "card_pack", "potion" };
        if (state.RelicIds.Count >= 4)
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
