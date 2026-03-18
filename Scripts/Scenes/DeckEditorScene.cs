using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DeckEditorScene : Control
{
    private OptionButton _presetOption = null!;
    private ItemList _deckList = null!;
    private ItemList _catalogList = null!;
    private Label _summaryLabel = null!;
    private Label _statusLabel = null!;
    private LineEdit _searchEdit = null!;

    private readonly List<CardData> _allCards = new();
    private readonly List<CardData> _currentDeck = new();
    private readonly List<CardData> _filteredCards = new();

    public override void _Ready()
    {
        _presetOption = GetNode<OptionButton>("%PresetOption");
        _deckList = GetNode<ItemList>("%DeckList");
        _catalogList = GetNode<ItemList>("%CatalogList");
        _summaryLabel = GetNode<Label>("%SummaryLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _searchEdit = GetNode<LineEdit>("%SearchEdit");

        GetNode<Button>("%BackButton").Pressed += OnBackPressed;
        GetNode<Button>("%ApplyPresetButton").Pressed += OnApplyPresetPressed;
        GetNode<Button>("%AddCardButton").Pressed += OnAddCardPressed;
        GetNode<Button>("%RemoveCardButton").Pressed += OnRemoveCardPressed;
        GetNode<Button>("%ResetButton").Pressed += OnResetPressed;
        GetNode<Button>("%UseDeckButton").Pressed += OnUseDeckPressed;

        _presetOption.ItemSelected += _ => SetStatus("已切换预设，点击“载入预设”同步到编辑区。", false);
        _searchEdit.TextChanged += _ => RefreshCatalogList();

        _allCards.AddRange(CardData.AllCards().OrderBy(card => card.Id));

        PopulatePresetOptions();
        LoadFromSelectedPreset();
        RefreshDeckList();
        RefreshCatalogList();
        SetStatus("可在这里编辑卡组，并作为本次测试起始卡组。", false);
    }

    private void PopulatePresetOptions()
    {
        var state = GetNode<GameState>("/root/GameState");
        var presets = state.DeckPresets();

        _presetOption.Clear();
        var selectedIndex = 0;
        for (var i = 0; i < presets.Count; i++)
        {
            _presetOption.AddItem($"{presets[i].LocalizedName} · {presets[i].LocalizedDescription}");
            if (presets[i].Id == state.SelectedDeckPresetId)
            {
                selectedIndex = i;
            }
        }

        _presetOption.Select(selectedIndex);
    }

    private void OnApplyPresetPressed()
    {
        LoadFromSelectedPreset();
        RefreshDeckList();
        SetStatus("已加载流派预设到编辑区。", false);
    }

    private void LoadFromSelectedPreset()
    {
        _currentDeck.Clear();

        var state = GetNode<GameState>("/root/GameState");
        var presets = state.DeckPresets();
        var selected = (int)_presetOption.Selected;
        if (selected < 0 || selected >= presets.Count)
        {
            return;
        }

        var preset = presets[selected];
        state.SetDeckPreset(preset.Id);

        foreach (var cardId in preset.CardIds)
        {
            _currentDeck.Add(CardData.CreateById(cardId));
        }
    }

    private void OnAddCardPressed()
    {
        var selected = _catalogList.GetSelectedItems();
        if (selected.Length == 0)
        {
            SetStatus("请先从右侧卡牌库选择要加入的卡。", true);
            return;
        }

        var card = _filteredCards[selected[0]];
        _currentDeck.Add(CardData.CreateById(card.Id));
        RefreshDeckList();
        SetStatus($"已加入：{card.Name} ({card.Id})", false);
    }

    private void OnRemoveCardPressed()
    {
        var selected = _deckList.GetSelectedItems();
        if (selected.Length == 0)
        {
            SetStatus("请先从左侧卡组中选择要移除的卡。", true);
            return;
        }

        var index = selected[0];
        if (index < 0 || index >= _currentDeck.Count)
        {
            return;
        }

        var card = _currentDeck[index];
        _currentDeck.RemoveAt(index);
        RefreshDeckList();
        SetStatus($"已移除：{card.Name} ({card.Id})", false);
    }

    private void OnResetPressed()
    {
        LoadFromSelectedPreset();
        RefreshDeckList();
        SetStatus("已重置为所选流派预设。", false);
    }

    private void OnUseDeckPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetCustomDeck(_currentDeck.Select(card => card.Id));
        SetStatus("已应用：该卡组将用于下一次开始游戏或战斗测试。", false);
    }

    private void RefreshDeckList()
    {
        _deckList.Clear();
        for (var i = 0; i < _currentDeck.Count; i++)
        {
            var card = _currentDeck[i];
            _deckList.AddItem($"#{i + 1:00} {card.Name} ({card.Id}) · 费用{card.Cost}");
        }

        var grouped = _currentDeck
            .GroupBy(card => card.Id)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(3)
            .Select(group => $"{group.Key}x{group.Count()}");

        var brief = string.Join('、', grouped);
        _summaryLabel.Text = string.IsNullOrWhiteSpace(brief)
            ? "当前卡组张数：0"
            : $"当前卡组张数：{_currentDeck.Count}（高频：{brief}）";
    }

    private void RefreshCatalogList()
    {
        _catalogList.Clear();
        _filteredCards.Clear();

        var keyword = _searchEdit.Text.Trim();
        foreach (var card in _allCards)
        {
            if (!string.IsNullOrWhiteSpace(keyword)
                && !card.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !card.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _filteredCards.Add(card);
            _catalogList.AddItem($"{card.Name} ({card.Id}) · 费用{card.Cost}");
        }
    }

    private void SetStatus(string message, bool isError)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = isError ? new Color(1f, 0.45f, 0.45f) : new Color(0.75f, 0.95f, 0.75f);
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
