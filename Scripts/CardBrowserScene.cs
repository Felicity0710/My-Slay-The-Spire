using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CardBrowserScene : Control
{
    private LineEdit _searchInput = null!;
    private OptionButton _kindFilter = null!;
    private OptionButton _costFilter = null!;
    private OptionButton _effectFilter = null!;
    private VBoxContainer _cardList = null!;
    private Label _countLabel = null!;

    private List<CardData> _allCards = new();

    public override void _Ready()
    {
        _searchInput = GetNode<LineEdit>("%SearchInput");
        _kindFilter = GetNode<OptionButton>("%KindFilter");
        _costFilter = GetNode<OptionButton>("%CostFilter");
        _effectFilter = GetNode<OptionButton>("%EffectFilter");
        _cardList = GetNode<VBoxContainer>("%CardList");
        _countLabel = GetNode<Label>("%CountLabel");

        _allCards = CardData.AllCards();
        BuildFilterOptions(_allCards);

        _searchInput.TextChanged += _ => RefreshCards();
        _kindFilter.ItemSelected += _ => RefreshCards();
        _costFilter.ItemSelected += _ => RefreshCards();
        _effectFilter.ItemSelected += _ => RefreshCards();

        GetNode<Button>("%ResetButton").Pressed += ResetFilters;
        GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");

        RefreshCards();
    }

    private void BuildFilterOptions(IReadOnlyList<CardData> cards)
    {
        _kindFilter.Clear();
        _kindFilter.AddItem("全部类型");
        _kindFilter.SetItemMetadata(0, -1);
        _kindFilter.AddItem("攻击");
        _kindFilter.SetItemMetadata(1, (int)CardKind.Attack);
        _kindFilter.AddItem("技能");
        _kindFilter.SetItemMetadata(2, (int)CardKind.Skill);

        _costFilter.Clear();
        _costFilter.AddItem("全部费用");
        _costFilter.SetItemMetadata(0, -1);
        foreach (var cost in cards.Select(card => card.Cost).Distinct().OrderBy(value => value))
        {
            _costFilter.AddItem($"{cost}费");
            _costFilter.SetItemMetadata(_costFilter.ItemCount - 1, cost);
        }

        _effectFilter.Clear();
        _effectFilter.AddItem("全部效果");
        _effectFilter.SetItemMetadata(0, -1);
        AddEffectOption("伤害", CardEffectType.Damage);
        AddEffectOption("格挡", CardEffectType.GainBlock);
        AddEffectOption("易伤", CardEffectType.ApplyVulnerable);
        AddEffectOption("抽牌", CardEffectType.DrawCards);
        AddEffectOption("力量", CardEffectType.GainStrength);
        AddEffectOption("能量", CardEffectType.GainEnergy);
        AddEffectOption("治疗", CardEffectType.Heal);

        _kindFilter.Select(0);
        _costFilter.Select(0);
        _effectFilter.Select(0);
    }

    private void AddEffectOption(string label, CardEffectType effect)
    {
        _effectFilter.AddItem(label);
        _effectFilter.SetItemMetadata(_effectFilter.ItemCount - 1, (int)effect);
    }

    private void ResetFilters()
    {
        _searchInput.Text = string.Empty;
        _kindFilter.Select(0);
        _costFilter.Select(0);
        _effectFilter.Select(0);
        RefreshCards();
    }

    private void RefreshCards()
    {
        foreach (Node child in _cardList.GetChildren())
        {
            child.QueueFree();
        }

        var filter = new CardBrowserFilterState
        {
            SearchText = _searchInput.Text,
            Kind = SelectedKind(),
            Cost = SelectedCost(),
            EffectType = SelectedEffectType()
        };

        var filteredCards = CardBrowserFilter.Apply(_allCards, filter);
        _countLabel.Text = $"共 {filteredCards.Count} 张";

        foreach (var card in filteredCards)
        {
            _cardList.AddChild(CreateCardItem(card));
        }
    }

    private CardKind? SelectedKind()
    {
        if (_kindFilter.Selected <= 0)
        {
            return null;
        }

        return (CardKind)_kindFilter.GetItemMetadata(_kindFilter.Selected).AsInt32();
    }

    private int? SelectedCost()
    {
        if (_costFilter.Selected <= 0)
        {
            return null;
        }

        return _costFilter.GetItemMetadata(_costFilter.Selected).AsInt32();
    }

    private CardEffectType? SelectedEffectType()
    {
        if (_effectFilter.Selected <= 0)
        {
            return null;
        }

        return (CardEffectType)_effectFilter.GetItemMetadata(_effectFilter.Selected).AsInt32();
    }

    private static PanelContainer CreateCardItem(CardData card)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = card.Kind == CardKind.Attack ? new Color("3f1d1d") : new Color("1c2f3f"),
            BorderColor = card.Kind == CardKind.Attack ? new Color("f87171") : new Color("67e8f9"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        });

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 4);
        margin.AddChild(body);

        var title = new Label
        {
            Text = $"[{card.Cost}] {card.Name} ({(card.Kind == CardKind.Attack ? "攻击" : "技能")})",
            Modulate = Colors.White
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        body.AddChild(title);

        var desc = new Label
        {
            Text = card.DescriptionZh,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color("e5e7eb")
        };
        body.AddChild(desc);

        return panel;
    }
}
