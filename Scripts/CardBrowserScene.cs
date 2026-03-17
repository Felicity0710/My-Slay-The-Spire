using Godot;
using System;
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

        BuildFilterOptions();

        _searchInput.TextChanged += _ => RefreshCards();
        _kindFilter.ItemSelected += _ => RefreshCards();
        _costFilter.ItemSelected += _ => RefreshCards();
        _effectFilter.ItemSelected += _ => RefreshCards();

        GetNode<Button>("%ResetButton").Pressed += ResetFilters;
        GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");

        _allCards = CardData.AllCards();
        RefreshCards();
    }

    private void BuildFilterOptions()
    {
        _kindFilter.Clear();
        _kindFilter.AddItem("全部类型", -1);
        _kindFilter.AddItem("攻击", (int)CardKind.Attack);
        _kindFilter.AddItem("技能", (int)CardKind.Skill);

        _costFilter.Clear();
        _costFilter.AddItem("全部费用", -1);
        foreach (var cost in CardData.AllCards().Select(card => card.Cost).Distinct().OrderBy(value => value))
        {
            _costFilter.AddItem($"{cost}费", cost);
        }

        _effectFilter.Clear();
        _effectFilter.AddItem("全部效果", -1);
        _effectFilter.AddItem("伤害", (int)CardEffectType.Damage);
        _effectFilter.AddItem("格挡", (int)CardEffectType.GainBlock);
        _effectFilter.AddItem("易伤", (int)CardEffectType.ApplyVulnerable);
        _effectFilter.AddItem("抽牌", (int)CardEffectType.DrawCards);
        _effectFilter.AddItem("力量", (int)CardEffectType.GainStrength);
        _effectFilter.AddItem("能量", (int)CardEffectType.GainEnergy);
        _effectFilter.AddItem("治疗", (int)CardEffectType.Heal);
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
        var id = _kindFilter.GetSelectedId();
        return id < 0 ? null : (CardKind)id;
    }

    private int? SelectedCost()
    {
        var id = _costFilter.GetSelectedId();
        return id < 0 ? null : id;
    }

    private CardEffectType? SelectedEffectType()
    {
        var id = _effectFilter.GetSelectedId();
        return id < 0 ? null : (CardEffectType)id;
    }

    private static PanelContainer CreateCardItem(CardData card)
    {
        var panel = new PanelContainer();
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
            Text = $"[{card.Cost}] {card.Name} ({(card.Kind == CardKind.Attack ? "攻击" : "技能")})"
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        body.AddChild(title);

        var desc = new Label
        {
            Text = card.DescriptionZh,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        body.AddChild(desc);

        return panel;
    }
}
