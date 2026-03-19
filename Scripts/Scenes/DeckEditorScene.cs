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

    private Button _backButton = null!;
    private Button _applyPresetButton = null!;
    private Button _resetButton = null!;
    private Button _useDeckButton = null!;
    private Button _removeCardButton = null!;
    private Button _addCardButton = null!;
    private Label _deckTitleLabel = null!;
    private Label _catalogTitleLabel = null!;

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

        _backButton = GetNode<Button>("%BackButton");
        _applyPresetButton = GetNode<Button>("%ApplyPresetButton");
        _resetButton = GetNode<Button>("%ResetButton");
        _useDeckButton = GetNode<Button>("%UseDeckButton");
        _removeCardButton = GetNode<Button>("%RemoveCardButton");
        _addCardButton = GetNode<Button>("%AddCardButton");
        _deckTitleLabel = GetNode<Label>("Margin/Root/Split/DeckPanel/DeckTitle");
        _catalogTitleLabel = GetNode<Label>("Margin/Root/Split/CatalogPanel/CatalogTitle");

        _backButton.Pressed += OnBackPressed;
        _applyPresetButton.Pressed += OnApplyPresetPressed;
        _addCardButton.Pressed += OnAddCardPressed;
        _removeCardButton.Pressed += OnRemoveCardPressed;
        _resetButton.Pressed += OnResetPressed;
        _useDeckButton.Pressed += OnUseDeckPressed;

        _presetOption.ItemSelected += _ => SetStatus(
            LocalizationService.Get("ui.deck_editor.preset_switched", "Preset switched. Click Load Preset to sync the editor."),
            false);
        _searchEdit.TextChanged += _ => RefreshCatalogList();
        LocalizationSettings.LanguageChanged += OnLanguageChanged;

        _allCards.AddRange(CardData.AllCards().OrderBy(card => card.Id));

        RefreshUiText();
        PopulatePresetOptions();
        LoadFromSelectedPreset();
        RefreshDeckList();
        RefreshCatalogList();
        SetStatus(LocalizationService.Get("ui.deck_editor.help_tip", "Edit the deck here and use it for your next test run."), false);
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void RefreshUiText()
    {
        _backButton.Text = LocalizationService.Get("ui.deck_editor.back", "Back to Menu");
        _applyPresetButton.Text = LocalizationService.Get("ui.deck_editor.load_preset", "Load Preset");
        _resetButton.Text = LocalizationService.Get("ui.deck_editor.reset", "Reset");
        _useDeckButton.Text = LocalizationService.Get("ui.deck_editor.use_deck", "Use as Starting Deck");
        _removeCardButton.Text = LocalizationService.Get("ui.deck_editor.remove_card_button", "Remove Selected Card");
        _addCardButton.Text = LocalizationService.Get("ui.deck_editor.add_card_button", "Add Selected Card");
        _deckTitleLabel.Text = LocalizationService.Get("ui.deck_editor.deck_title", "Current Deck");
        _catalogTitleLabel.Text = LocalizationService.Get("ui.deck_editor.catalog_title", "Card Catalog");
        _searchEdit.PlaceholderText = LocalizationService.Get("ui.deck_editor.search_placeholder", "Search by id or card name");
    }

    private void PopulatePresetOptions()
    {
        var state = GetNode<GameState>("/root/GameState");
        var presets = state.DeckPresets();

        _presetOption.Clear();
        var selectedIndex = 0;
        for (var i = 0; i < presets.Count; i++)
        {
            _presetOption.AddItem(LocalizationService.Format(
                "ui.deck_editor.preset_option",
                "{0} - {1}",
                presets[i].LocalizedName,
                presets[i].LocalizedDescription));
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
        SetStatus(LocalizationService.Get("ui.deck_editor.preset_loaded", "Preset loaded into the editor."), false);
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
            SetStatus(LocalizationService.Get("ui.deck_editor.select_card_first", "Select a card from the catalog first."), true);
            return;
        }

        var card = _filteredCards[selected[0]];
        _currentDeck.Add(CardData.CreateById(card.Id));
        RefreshDeckList();
        SetStatus(LocalizationService.Format("ui.deck_editor.add_card", "Added: {0} ({1})", card.GetLocalizedName(), card.Id), false);
    }

    private void OnRemoveCardPressed()
    {
        var selected = _deckList.GetSelectedItems();
        if (selected.Length == 0)
        {
            SetStatus(LocalizationService.Get("ui.deck_editor.select_deck_card_first", "Select a card in the deck first."), true);
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
        SetStatus(LocalizationService.Format("ui.deck_editor.remove_card", "Removed: {0} ({1})", card.GetLocalizedName(), card.Id), false);
    }

    private void OnResetPressed()
    {
        LoadFromSelectedPreset();
        RefreshDeckList();
        SetStatus(LocalizationService.Get("ui.deck_editor.deck_reset", "Reset to the selected preset."), false);
    }

    private void OnUseDeckPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetCustomDeck(_currentDeck.Select(card => card.Id));
        SetStatus(LocalizationService.Get("ui.deck_editor.deck_applied", "Applied. This deck will be used for the next run or battle test."), false);
    }

    private void RefreshDeckList()
    {
        _deckList.Clear();
        for (var i = 0; i < _currentDeck.Count; i++)
        {
            var card = _currentDeck[i];
            _deckList.AddItem(LocalizationService.Format(
                "ui.deck_editor.deck_item_line",
                "#{0:00} {1} ({2}) Cost {3}",
                i + 1,
                card.GetLocalizedName(),
                card.Id,
                card.Cost));
        }

        var grouped = _currentDeck
            .GroupBy(card => card.Id)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(3)
            .Select(group => $"{group.Key}x{group.Count()}");

        var brief = string.Join(", ", grouped);
        _summaryLabel.Text = string.IsNullOrWhiteSpace(brief)
            ? LocalizationService.Get("ui.deck_editor.deck_total_zero", "Current deck size: 0")
            : LocalizationService.Format(
                "ui.deck_editor.deck_total_summary",
                "Current deck size: {0} (most common: {1})",
                _currentDeck.Count,
                brief);
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
                && !card.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !card.GetLocalizedName().Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _filteredCards.Add(card);
            _catalogList.AddItem(LocalizationService.Format(
                "ui.deck_editor.catalog_item_line",
                "{0} ({1}) Cost {2}",
                card.GetLocalizedName(),
                card.Id,
                card.Cost));
        }
    }

    private void SetStatus(string message, bool isError)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = isError ? new Color(1f, 0.45f, 0.45f) : new Color(0.75f, 0.95f, 0.75f);
    }

    private void OnLanguageChanged()
    {
        RefreshUiText();
        PopulatePresetOptions();
        RefreshDeckList();
        RefreshCatalogList();
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
