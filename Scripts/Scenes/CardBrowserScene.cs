using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CardBrowserScene : Control
{
    private readonly PackedScene _cardViewScene = GD.Load<PackedScene>("res://Scenes/RewardCardOptionView.tscn");

    private LineEdit _searchInput = null!;
    private OptionButton _kindFilter = null!;
    private OptionButton _costFilter = null!;
    private OptionButton _effectFilter = null!;
    private GridContainer _cardGrid = null!;
    private Label _countLabel = null!;
    private Label _titleLabel = null!;
    private Button _resetButton = null!;
    private Button _backButton = null!;

    private List<CardData> _allCards = new();

    public override void _Ready()
    {
        _searchInput = GetNode<LineEdit>("%SearchInput");
        _kindFilter = GetNode<OptionButton>("%KindFilter");
        _costFilter = GetNode<OptionButton>("%CostFilter");
        _effectFilter = GetNode<OptionButton>("%EffectFilter");
        _cardGrid = GetNode<GridContainer>("%CardGrid");
        _countLabel = GetNode<Label>("%CountLabel");
        _titleLabel = GetNode<Label>("Margin/Root/Header/Title");
        _resetButton = GetNode<Button>("%ResetButton");
        _backButton = GetNode<Button>("%BackButton");

        _allCards = CardData.AllCards();
        BuildFilterOptions(_allCards);

        _searchInput.TextChanged += _ => RefreshCards();
        _kindFilter.ItemSelected += _ => RefreshCards();
        _costFilter.ItemSelected += _ => RefreshCards();
        _effectFilter.ItemSelected += _ => RefreshCards();
        LocalizationSettings.LanguageChanged += RefreshFiltersAndCards;

        _resetButton.Pressed += ResetFilters;
        _backButton.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");

        RefreshFiltersAndCards();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RefreshFiltersAndCards;
    }

    private void BuildFilterOptions(IReadOnlyList<CardData> cards)
    {
        _kindFilter.Clear();
        _kindFilter.AddItem(LocalizationService.Get("ui.card_browser.filters.all", "All"));
        _kindFilter.SetItemMetadata(0, -1);
        _kindFilter.AddItem(LocalizationService.Get("ui.card_browser.filters.type_attack", "Attack"));
        _kindFilter.SetItemMetadata(1, (int)CardKind.Attack);
        _kindFilter.AddItem(LocalizationService.Get("ui.card_browser.filters.type_skill", "Skill"));
        _kindFilter.SetItemMetadata(2, (int)CardKind.Skill);

        _costFilter.Clear();
        _costFilter.AddItem(LocalizationService.Get("ui.card_browser.filters.all", "All"));
        _costFilter.SetItemMetadata(0, -1);
        foreach (var cost in cards.Select(card => card.Cost).Distinct().OrderBy(value => value))
        {
            _costFilter.AddItem(LocalizationService.Format("ui.card_browser.cost_filter", "{0} Cost", cost));
            _costFilter.SetItemMetadata(_costFilter.ItemCount - 1, cost);
        }

        _effectFilter.Clear();
        _effectFilter.AddItem(LocalizationService.Get("ui.card_browser.filters.all", "All"));
        _effectFilter.SetItemMetadata(0, -1);
        AddEffectOption(LocalizationService.Get("ui.card_browser.effect_damage", "Damage"), CardEffectType.Damage);
        AddEffectOption(LocalizationService.Get("ui.card_browser.effect_block", "Gain Block"), CardEffectType.GainBlock);
        AddEffectOption(LocalizationService.Get("ui.card_browser.effect_vulnerable", "Apply Vulnerable"), CardEffectType.ApplyVulnerable);
        AddEffectOption(LocalizationService.Get("ui.card_browser.effect_draw", "Draw"), CardEffectType.DrawCards);
        AddEffectOption(LocalizationService.Get("ui.card_browser.effect_strength", "Gain Strength"), CardEffectType.GainStrength);
        AddEffectOption(LocalizationService.Get("ui.card_browser.effect_energy", "Gain Energy"), CardEffectType.GainEnergy);
        AddEffectOption(LocalizationService.Get("ui.card_browser.effect_heal", "Heal"), CardEffectType.Heal);

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
        foreach (Node child in _cardGrid.GetChildren())
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
        _countLabel.Text = LocalizationService.Format("ui.card_browser.count_format", "{0} cards", filteredCards.Count);

        foreach (var card in filteredCards)
        {
            var cardNode = _cardViewScene.Instantiate<RewardCardOptionView>();
            cardNode.Setup(card);
            _cardGrid.AddChild(cardNode);
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

    private void RefreshFiltersAndCards()
    {
        _searchInput.PlaceholderText = LocalizationService.Get("ui.card_browser.search_placeholder", "Search card name or description");
        _titleLabel.Text = LocalizationService.Get("ui.card_browser.title", "Card Compendium");
        _resetButton.Text = LocalizationService.Get("ui.card_browser.reset", "Reset");
        _backButton.Text = LocalizationService.Get("ui.card_browser.back", "Back");
        BuildFilterOptions(_allCards);
        RefreshCards();
    }
}
