using Godot;

public partial class EventScene : Control
{
    private Label _titleLabel = null!;
    private Label _descLabel = null!;
    private Button _option1Button = null!;
    private Button _option2Button = null!;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("%TitleLabel");
        _descLabel = GetNode<Label>("%DescLabel");
        _option1Button = GetNode<Button>("%Option1Button");
        _option2Button = GetNode<Button>("%Option2Button");
        LocalizationSettings.LanguageChanged += OnLanguageChanged;

        BindEvent();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void BindEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        var id = state.PendingEventId;

        if (id == "shrine")
        {
            _titleLabel.Text = LocalizationService.Get("event.shrine.title", "远古祭坛");
            _descLabel.Text = LocalizationService.Get("event.shrine.description", "一座静默的祭坛散发着能量。");
            _option1Button.Text = LocalizationService.Get("event.shrine.option_pray", "祈祷：最大生命+5，治疗5");
            _option2Button.Text = LocalizationService.Get("event.shrine.option_relic", "拾取遗物：失去8生命，获得随机遗物");
            _option1Button.Pressed += ShrinePray;
            _option2Button.Pressed += ShrineRelic;
        }
        else
        {
            _titleLabel.Text = LocalizationService.Get("event.dealer.title", "可疑商人");
            _descLabel.Text = LocalizationService.Get("event.dealer.description", "一名商人提供了一笔冒险交易。");
            _option1Button.Text = LocalizationService.Get("event.dealer.option_buy", "购买卡牌：失去6生命，获得快速斩");
            _option2Button.Text = LocalizationService.Get("event.dealer.option_refuse", "拒绝：不获得任何奖励");
            _option1Button.Pressed += DealerBuy;
            _option2Button.Pressed += LeaveEvent;
        }
    }

    private void OnLanguageChanged()
    {
        BindEvent();
    }

    private void ShrinePray()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.GainMaxHp(5);
        state.ResolveEventFinished();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void ShrineRelic()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 8);
        state.RollRelicOptions(1);
        if (state.PendingRelicOptions.Count > 0)
        {
            state.AddRelic(state.PendingRelicOptions[0]);
            state.PendingRelicOptions.Clear();
        }

        state.ResolveEventFinished();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void DealerBuy()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 6);
        state.AddCardToDeck("quick_slash");
        state.ResolveEventFinished();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void LeaveEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ResolveEventFinished();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }
}
