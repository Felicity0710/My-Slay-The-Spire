using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class CardEditorScene : Control
{
    private ItemList _cardList = null!;
    private LineEdit _searchEdit = null!;
    private Label _summaryLabel = null!;

    private LineEdit _idEdit = null!;
    private LineEdit _nameEdit = null!;
    private OptionButton _kindOption = null!;
    private SpinBox _costSpin = null!;
    private CheckBox _inStarterDeckCheck = null!;
    private CheckBox _inRewardPoolCheck = null!;
    private TextEdit _descriptionEdit = null!;
    private TextEdit _descriptionZhEdit = null!;

    private ItemList _effectList = null!;
    private OptionButton _effectTypeOption = null!;
    private OptionButton _effectTargetOption = null!;
    private SpinBox _effectAmountSpin = null!;
    private SpinBox _effectRepeatSpin = null!;
    private SpinBox _effectFlatBonusSpin = null!;
    private CheckBox _effectUseAttackerStrengthCheck = null!;
    private CheckBox _effectUseTargetVulnerableCheck = null!;
    private TextEdit _effectsPreviewEdit = null!;

    private Label _statusLabel = null!;
    private RichTextLabel _validationLabel = null!;

    private Button _backButton = null!;
    private Button _reloadButton = null!;
    private Button _validateButton = null!;
    private Button _saveButton = null!;
    private Button _newButton = null!;
    private Button _duplicateButton = null!;
    private Button _deleteButton = null!;
    private Button _applyCardButton = null!;
    private Button _addEffectButton = null!;
    private Button _removeEffectButton = null!;
    private Button _applyEffectButton = null!;

    private Label _cardTitleLabel = null!;
    private Label _effectTitleLabel = null!;
    private Label _idLabel = null!;
    private Label _nameLabel = null!;
    private Label _kindLabel = null!;
    private Label _costLabel = null!;
    private Label _effectTypeLabel = null!;
    private Label _effectTargetLabel = null!;
    private Label _effectAmountLabel = null!;
    private Label _effectRepeatLabel = null!;
    private Label _effectFlatBonusLabel = null!;

    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private CardCatalogData _catalog = new();
    private readonly List<int> _filteredCardIndexes = new();
    private int _selectedCardIndex = -1;
    private int _selectedEffectIndex = -1;

    public override void _Ready()
    {
        _cardList = GetNode<ItemList>("%CardList");
        _searchEdit = GetNode<LineEdit>("%SearchEdit");
        _summaryLabel = GetNode<Label>("%SummaryLabel");

        _idEdit = GetNode<LineEdit>("%IdEdit");
        _nameEdit = GetNode<LineEdit>("%NameEdit");
        _kindOption = GetNode<OptionButton>("%KindOption");
        _costSpin = GetNode<SpinBox>("%CostSpin");
        _inStarterDeckCheck = GetNode<CheckBox>("%InStarterDeckCheck");
        _inRewardPoolCheck = GetNode<CheckBox>("%InRewardPoolCheck");
        _descriptionEdit = GetNode<TextEdit>("%DescriptionEdit");
        _descriptionZhEdit = GetNode<TextEdit>("%DescriptionZhEdit");

        _effectList = GetNode<ItemList>("%EffectList");
        _effectTypeOption = GetNode<OptionButton>("%EffectTypeOption");
        _effectTargetOption = GetNode<OptionButton>("%EffectTargetOption");
        _effectAmountSpin = GetNode<SpinBox>("%EffectAmountSpin");
        _effectRepeatSpin = GetNode<SpinBox>("%EffectRepeatSpin");
        _effectFlatBonusSpin = GetNode<SpinBox>("%EffectFlatBonusSpin");
        _effectUseAttackerStrengthCheck = GetNode<CheckBox>("%EffectUseAttackerStrengthCheck");
        _effectUseTargetVulnerableCheck = GetNode<CheckBox>("%EffectUseTargetVulnerableCheck");
        _effectsPreviewEdit = GetNode<TextEdit>("%EffectsPreviewEdit");

        _statusLabel = GetNode<Label>("%StatusLabel");
        _validationLabel = GetNode<RichTextLabel>("%ValidationLabel");

        _backButton = GetNode<Button>("%BackButton");
        _reloadButton = GetNode<Button>("%ReloadButton");
        _validateButton = GetNode<Button>("%ValidateButton");
        _saveButton = GetNode<Button>("%SaveButton");
        _newButton = GetNode<Button>("%NewButton");
        _duplicateButton = GetNode<Button>("%DuplicateButton");
        _deleteButton = GetNode<Button>("%DeleteButton");
        _applyCardButton = GetNode<Button>("%ApplyCardButton");
        _addEffectButton = GetNode<Button>("%AddEffectButton");
        _removeEffectButton = GetNode<Button>("%RemoveEffectButton");
        _applyEffectButton = GetNode<Button>("%ApplyEffectButton");

        _cardTitleLabel = GetNode<Label>("Margin/Root/Split/RightPanel/CardTitle");
        _effectTitleLabel = GetNode<Label>("Margin/Root/Split/RightPanel/EffectTitle");
        _idLabel = GetNode<Label>("Margin/Root/Split/RightPanel/CardGrid/IdLabel");
        _nameLabel = GetNode<Label>("Margin/Root/Split/RightPanel/CardGrid/NameLabel");
        _kindLabel = GetNode<Label>("Margin/Root/Split/RightPanel/CardGrid/KindLabel");
        _costLabel = GetNode<Label>("Margin/Root/Split/RightPanel/CardGrid/CostLabel");
        _effectTypeLabel = GetNode<Label>("Margin/Root/Split/RightPanel/EffectSplit/EffectRight/EffectGrid/EffectTypeLabel");
        _effectTargetLabel = GetNode<Label>("Margin/Root/Split/RightPanel/EffectSplit/EffectRight/EffectGrid/EffectTargetLabel");
        _effectAmountLabel = GetNode<Label>("Margin/Root/Split/RightPanel/EffectSplit/EffectRight/EffectGrid/EffectAmountLabel");
        _effectRepeatLabel = GetNode<Label>("Margin/Root/Split/RightPanel/EffectSplit/EffectRight/EffectGrid/EffectRepeatLabel");
        _effectFlatBonusLabel = GetNode<Label>("Margin/Root/Split/RightPanel/EffectSplit/EffectRight/EffectGrid/EffectFlatBonusLabel");

        BuildEnumOptions();

        _backButton.Pressed += OnBackPressed;
        _reloadButton.Pressed += ReloadFromDisk;
        _saveButton.Pressed += OnSavePressed;
        _validateButton.Pressed += OnValidatePressed;
        _newButton.Pressed += OnNewCardPressed;
        _duplicateButton.Pressed += OnDuplicateCardPressed;
        _deleteButton.Pressed += OnDeleteCardPressed;
        _applyCardButton.Pressed += OnApplyCardPressed;
        _addEffectButton.Pressed += OnAddEffectPressed;
        _removeEffectButton.Pressed += OnRemoveEffectPressed;
        _applyEffectButton.Pressed += OnApplyEffectPressed;

        _cardList.ItemSelected += OnCardSelected;
        _effectList.ItemSelected += OnEffectSelected;
        _searchEdit.TextChanged += _ => RefreshCardList();
        LocalizationSettings.LanguageChanged += OnLanguageChanged;

        RefreshUiText();
        ReloadFromDisk();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void RefreshUiText()
    {
        _backButton.Text = LocalizationService.Get("ui.card_editor.back", "Back to Menu");
        _reloadButton.Text = LocalizationService.Get("ui.card_editor.reload", "Reload");
        _validateButton.Text = LocalizationService.Get("ui.card_editor.validate", "Validate");
        _saveButton.Text = LocalizationService.Get("ui.card_editor.save", "Save");
        _newButton.Text = LocalizationService.Get("ui.card_editor.new", "New");
        _duplicateButton.Text = LocalizationService.Get("ui.card_editor.duplicate", "Duplicate");
        _deleteButton.Text = LocalizationService.Get("ui.card_editor.delete", "Delete");
        _applyCardButton.Text = LocalizationService.Get("ui.card_editor.apply_card", "Apply Card Changes");
        _addEffectButton.Text = LocalizationService.Get("ui.card_editor.add_effect", "+ Effect");
        _removeEffectButton.Text = LocalizationService.Get("ui.card_editor.remove_effect", "- Effect");
        _applyEffectButton.Text = LocalizationService.Get("ui.card_editor.apply_effect", "Apply Effect Changes");

        _cardTitleLabel.Text = LocalizationService.Get("ui.card_editor.card_info", "Card Info");
        _effectTitleLabel.Text = LocalizationService.Get("ui.card_editor.effect_editor", "Card Effects");
        _idLabel.Text = LocalizationService.Get("ui.card_editor.id_label", "ID");
        _nameLabel.Text = LocalizationService.Get("ui.card_editor.name_label", "Name");
        _kindLabel.Text = LocalizationService.Get("ui.card_editor.kind_label", "Kind");
        _costLabel.Text = LocalizationService.Get("ui.card_editor.cost_label", "Cost");
        _effectTypeLabel.Text = LocalizationService.Get("ui.card_editor.effect_type_label", "Type");
        _effectTargetLabel.Text = LocalizationService.Get("ui.card_editor.effect_target_label", "Target");
        _effectAmountLabel.Text = LocalizationService.Get("ui.card_editor.effect_amount_label", "Amount");
        _effectRepeatLabel.Text = LocalizationService.Get("ui.card_editor.effect_repeat_label", "Repeat");
        _effectFlatBonusLabel.Text = LocalizationService.Get("ui.card_editor.effect_flat_bonus_label", "Flat Bonus");

        _searchEdit.PlaceholderText = LocalizationService.Get("ui.card_editor.search_placeholder", "Search id or name");
        _idEdit.PlaceholderText = LocalizationService.Get("ui.card_editor.id_placeholder", "Unique id");
        _nameEdit.PlaceholderText = LocalizationService.Get("ui.card_editor.name_placeholder", "Card name");
        _descriptionEdit.PlaceholderText = LocalizationService.Get("ui.card_editor.description_placeholder", "Description (English)");
        _descriptionZhEdit.PlaceholderText = LocalizationService.Get("ui.card_editor.description_zh_placeholder", "Description (Chinese)");

        _inStarterDeckCheck.Text = LocalizationService.Get("ui.card_editor.in_starter_deck", "Include in starter deck");
        _inRewardPoolCheck.Text = LocalizationService.Get("ui.card_editor.in_reward_pool", "Include in reward pool");
        _effectUseAttackerStrengthCheck.Text = LocalizationService.Get("ui.card_editor.use_attacker_strength", "Use attacker Strength");
        _effectUseTargetVulnerableCheck.Text = LocalizationService.Get("ui.card_editor.use_target_vulnerable", "Use target Vulnerable");

        BuildEnumOptions();
        if (_catalog.Cards.Count > 0)
        {
            RefreshCardList();
            RefreshEffectList();
        }
    }

    private void BuildEnumOptions()
    {
        _kindOption.Clear();
        _kindOption.AddItem(LocalizationService.Get("ui.card_browser.filters.type_attack", "Attack"));
        _kindOption.AddItem(LocalizationService.Get("ui.card_browser.filters.type_skill", "Skill"));

        _effectTypeOption.Clear();
        foreach (var value in Enum.GetValues<CardEffectType>())
        {
            _effectTypeOption.AddItem(LocalizationService.Get($"ui.card_effect_type.{value.ToString().ToLowerInvariant()}", value.ToString()));
        }

        _effectTargetOption.Clear();
        foreach (var value in Enum.GetValues<CardEffectTarget>())
        {
            _effectTargetOption.AddItem(LocalizationService.Get($"ui.card_effect_target.{value.ToString().ToLowerInvariant()}", value.ToString()));
        }
    }

    private void ReloadFromDisk()
    {
        try
        {
            var path = CardCatalogPersistence.ResolveCardsJsonPath();
            _catalog = CardCatalogPersistence.LoadFromFile(path);
            _selectedCardIndex = -1;
            _selectedEffectIndex = -1;
            RefreshCardList();
            ClearCardEditor();
            SetValidation(new List<string>());
            SetStatus(LocalizationService.Format("ui.card_editor.reload_success", "Loaded cards from {0}", path), false);
        }
        catch (Exception ex)
        {
            _catalog = new CardCatalogData();
            _selectedCardIndex = -1;
            _selectedEffectIndex = -1;
            RefreshCardList();
            ClearCardEditor();
            SetValidation(new List<string> { ex.Message });
            SetStatus(LocalizationService.Format("ui.card_editor.reload_failed", "Failed to load cards: {0}", ex.Message), true);
        }
    }

    private void OnSavePressed()
    {
        try
        {
            if (_selectedCardIndex >= 0)
            {
                ApplyCardEditorToCurrentCard();
            }

            var path = CardCatalogPersistence.ResolveCardsJsonPath();
            CardCatalogPersistence.SaveToFile(path, _catalog);
            SetValidation(new List<string>());
            SetStatus(LocalizationService.Get("ui.card_editor.save_success", "Cards saved."), false);
        }
        catch (Exception ex)
        {
            SetStatus(LocalizationService.Format("ui.card_editor.save_failed", "Failed to save cards: {0}", ex.Message), true);
        }
    }

    private void OnValidatePressed()
    {
        try
        {
            if (_selectedCardIndex >= 0)
            {
                ApplyCardEditorToCurrentCard();
            }

            var errors = CardCatalogPersistence.Validate(_catalog);
            SetValidation(errors);
            SetStatus(
                errors.Count == 0
                    ? LocalizationService.Get("ui.card_editor.validate_success", "Validation passed.")
                    : LocalizationService.Get("ui.card_editor.validate_failed", "Validation found issues. Check list below."),
                errors.Count > 0);
        }
        catch (Exception ex)
        {
            SetValidation(new List<string> { ex.Message });
            SetStatus(LocalizationService.Format("ui.card_editor.validate_error", "Validation error: {0}", ex.Message), true);
        }
    }

    private void OnNewCardPressed()
    {
        var newId = BuildUniqueCardId("new_card");
        _catalog.Cards.Add(new CardEntryData
        {
            Id = newId,
            Name = LocalizationService.Get("ui.card_editor.default_card_name", "New Card"),
            Kind = nameof(CardKind.Skill),
            Cost = 1,
            Description = "",
            DescriptionZh = "",
            ArtPath = "res://Assets/Cards/",
            Effects = new List<CardEffectEntryData>
            {
                new()
                {
                    Type = nameof(CardEffectType.GainBlock),
                    Target = nameof(CardEffectTarget.Player),
                    Amount = 5,
                    Repeat = 1,
                    UseAttackerStrength = false,
                    UseTargetVulnerable = false,
                    FlatBonus = 0
                }
            }
        });

        RefreshCardList();
        SelectCardById(newId);
        SetStatus(LocalizationService.Format("ui.card_editor.new_card_created", "Created new card: {0}", newId), false);
    }

    private void OnDuplicateCardPressed()
    {
        if (!TryGetSelectedCard(out var source))
        {
            SetStatus(LocalizationService.Get("ui.card_editor.duplicate_missing", "Please select a card to duplicate first."), true);
            return;
        }

        var copy = new CardEntryData
        {
            Id = BuildUniqueCardId($"{source.Id}_copy"),
            Name = $"{source.Name} Copy",
            Kind = source.Kind,
            Cost = source.Cost,
            Description = source.Description,
            DescriptionZh = source.DescriptionZh,
            Effects = source.Effects.Select(CloneEffect).ToList()
        };

        _catalog.Cards.Add(copy);
        RefreshCardList();
        SelectCardById(copy.Id);
        SetStatus(LocalizationService.Format("ui.card_editor.duplicate_success", "Duplicated card: {0}", copy.Id), false);
    }

    private void OnDeleteCardPressed()
    {
        if (!TryGetSelectedCard(out var card))
        {
            SetStatus(LocalizationService.Get("ui.card_editor.delete_missing", "Please select a card to delete first."), true);
            return;
        }

        var cardId = card.Id;
        _catalog.Cards.RemoveAt(_selectedCardIndex);
        _catalog.StarterDeck.RemoveAll(id => id == cardId);
        _catalog.RewardPool.RemoveAll(id => id == cardId);

        _selectedCardIndex = -1;
        _selectedEffectIndex = -1;
        RefreshCardList();
        ClearCardEditor();
        SetStatus(LocalizationService.Format("ui.card_editor.delete_success", "Deleted card: {0} and cleaned deck/pool references.", cardId), false);
    }

    private void OnApplyCardPressed()
    {
        try
        {
            ApplyCardEditorToCurrentCard();
            RefreshCardList();
            ReselectCurrentCard();
            SetStatus(LocalizationService.Get("ui.card_editor.apply_success", "Applied card changes."), false);
        }
        catch (Exception ex)
        {
            SetStatus(LocalizationService.Format("ui.card_editor.apply_failed", "Apply failed: {0}", ex.Message), true);
        }
    }

    private void OnCardSelected(long visualIndex)
    {
        if (visualIndex < 0 || visualIndex >= _filteredCardIndexes.Count)
        {
            return;
        }

        _selectedCardIndex = _filteredCardIndexes[(int)visualIndex];
        _selectedEffectIndex = -1;
        PopulateCardEditor(_catalog.Cards[_selectedCardIndex]);
        RefreshEffectList();
    }

    private void ApplyCardEditorToCurrentCard()
    {
        if (!TryGetSelectedCard(out var card))
        {
            throw new InvalidOperationException(LocalizationService.Get("ui.card_editor.no_card_selected", "Please select a card first."));
        }

        var oldId = card.Id;

        card.Id = _idEdit.Text.Trim();
        card.Name = _nameEdit.Text.Trim();
        card.Kind = _kindOption.Selected == 0 ? nameof(CardKind.Attack) : nameof(CardKind.Skill);
        card.Cost = (int)_costSpin.Value;
        card.Description = _descriptionEdit.Text.Trim();
        card.DescriptionZh = _descriptionZhEdit.Text.Trim();

        if (_inStarterDeckCheck.ButtonPressed)
        {
            EnsureContains(_catalog.StarterDeck, card.Id);
        }
        else
        {
            _catalog.StarterDeck.RemoveAll(id => id == card.Id);
        }

        if (_inRewardPoolCheck.ButtonPressed)
        {
            EnsureContains(_catalog.RewardPool, card.Id);
        }
        else
        {
            _catalog.RewardPool.RemoveAll(id => id == card.Id);
        }

        if (!string.Equals(oldId, card.Id, StringComparison.Ordinal))
        {
            ReplacePoolId(_catalog.StarterDeck, oldId, card.Id);
            ReplacePoolId(_catalog.RewardPool, oldId, card.Id);
        }

        CardCatalogPersistence.ValidateOrThrow(_catalog, "editor");
    }

    private void OnAddEffectPressed()
    {
        if (!TryGetSelectedCard(out var card))
        {
            SetStatus(LocalizationService.Get("ui.card_editor.effect_add_missing", "Please select a card first before adding effects."), true);
            return;
        }

        card.Effects.Add(new CardEffectEntryData
        {
            Type = nameof(CardEffectType.Damage),
            Target = nameof(CardEffectTarget.SelectedEnemy),
            Amount = 1,
            Repeat = 1,
            UseAttackerStrength = true,
            UseTargetVulnerable = true,
            FlatBonus = 0
        });

        _selectedEffectIndex = card.Effects.Count - 1;
        RefreshEffectList();
        PopulateEffectEditor(card.Effects[_selectedEffectIndex]);
    }

    private void OnRemoveEffectPressed()
    {
        if (!TryGetSelectedCard(out var card) || _selectedEffectIndex < 0 || _selectedEffectIndex >= card.Effects.Count)
        {
            SetStatus(LocalizationService.Get("ui.card_editor.effect_remove_missing", "Please select a valid effect first."), true);
            return;
        }

        card.Effects.RemoveAt(_selectedEffectIndex);
        _selectedEffectIndex = -1;
        RefreshEffectList();
        ClearEffectEditor();
        SetStatus(LocalizationService.Get("ui.card_editor.effect_removed", "Effect removed."), false);
    }

    private void OnApplyEffectPressed()
    {
        try
        {
            if (!TryGetSelectedCard(out var card) || _selectedEffectIndex < 0 || _selectedEffectIndex >= card.Effects.Count)
            {
                throw new InvalidOperationException(LocalizationService.Get("ui.card_editor.no_card_selected", "Please select a card first."));
            }

            var effect = card.Effects[_selectedEffectIndex];
            effect.Type = Enum.GetValues<CardEffectType>()[_effectTypeOption.Selected].ToString();
            effect.Target = Enum.GetValues<CardEffectTarget>()[_effectTargetOption.Selected].ToString();
            effect.Amount = (int)_effectAmountSpin.Value;
            effect.Repeat = (int)_effectRepeatSpin.Value;
            effect.FlatBonus = (int)_effectFlatBonusSpin.Value;
            effect.UseAttackerStrength = _effectUseAttackerStrengthCheck.ButtonPressed;
            effect.UseTargetVulnerable = _effectUseTargetVulnerableCheck.ButtonPressed;

            CardCatalogPersistence.ValidateOrThrow(_catalog, "effect editor");
            RefreshEffectList();
            SetStatus(LocalizationService.Get("ui.card_editor.effect_updated", "Effect update applied."), false);
        }
        catch (Exception ex)
        {
            SetStatus(LocalizationService.Format("ui.card_editor.effect_update_failed", "Effect update failed: {0}", ex.Message), true);
        }
    }

    private void OnEffectSelected(long index)
    {
        if (!TryGetSelectedCard(out var card) || index < 0 || index >= card.Effects.Count)
        {
            return;
        }

        _selectedEffectIndex = (int)index;
        PopulateEffectEditor(card.Effects[_selectedEffectIndex]);
    }

    private void RefreshCardList()
    {
        _cardList.Clear();
        _filteredCardIndexes.Clear();

        var keyword = _searchEdit.Text.Trim();
        foreach (var pair in _catalog.Cards.Select((c, i) => (Card: c, Index: i)))
        {
            var localizedName = LocalizationService.Get($"card.{pair.Card.Id}.name", pair.Card.Name);
            if (!string.IsNullOrEmpty(keyword)
                && !pair.Card.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !pair.Card.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !localizedName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _filteredCardIndexes.Add(pair.Index);
            _cardList.AddItem(LocalizationService.Format(
                "ui.card_editor.card_entry",
                "{0} - {1} [{2}] Cost {3}",
                pair.Card.Id,
                localizedName,
                pair.Card.Kind,
                pair.Card.Cost));
        }

        _summaryLabel.Text = LocalizationService.Format(
            "ui.card_editor.summary",
            "Cards: {0} total, showing {1}. starterDeck: {2}, rewardPool: {3}",
            _catalog.Cards.Count,
            _filteredCardIndexes.Count,
            _catalog.StarterDeck.Count,
            _catalog.RewardPool.Count);
    }

    private void RefreshEffectList()
    {
        _effectList.Clear();
        if (!TryGetSelectedCard(out var card))
        {
            _effectsPreviewEdit.Text = "[]";
            return;
        }

        for (var i = 0; i < card.Effects.Count; i++)
        {
            var effect = card.Effects[i];
            var effectType = LocalizationService.Get($"ui.card_effect_type.{effect.Type.ToLowerInvariant()}", effect.Type);
            var effectTarget = LocalizationService.Get($"ui.card_effect_target.{effect.Target.ToLowerInvariant()}", effect.Target);
            _effectList.AddItem(LocalizationService.Format(
                "ui.card_editor.effect_item",
                "#{0} {1} -> {2} x{3} amount:{4} bonus:{5}",
                i + 1,
                effectType,
                effectTarget,
                effect.Repeat,
                effect.Amount,
                effect.FlatBonus));
        }

        _effectsPreviewEdit.Text = JsonSerializer.Serialize(card.Effects, _jsonOptions);
    }

    private void PopulateCardEditor(CardEntryData card)
    {
        _idEdit.Text = card.Id;
        _nameEdit.Text = card.Name;
        _kindOption.Select(string.Equals(card.Kind, nameof(CardKind.Attack), StringComparison.OrdinalIgnoreCase) ? 0 : 1);
        _costSpin.Value = card.Cost;
        _descriptionEdit.Text = card.Description;
        _descriptionZhEdit.Text = card.DescriptionZh ?? string.Empty;
        _inStarterDeckCheck.ButtonPressed = _catalog.StarterDeck.Contains(card.Id);
        _inRewardPoolCheck.ButtonPressed = _catalog.RewardPool.Contains(card.Id);
    }

    private void PopulateEffectEditor(CardEffectEntryData effect)
    {
        _effectTypeOption.Select(Array.IndexOf(Enum.GetNames<CardEffectType>(), effect.Type));
        _effectTargetOption.Select(Array.IndexOf(Enum.GetNames<CardEffectTarget>(), effect.Target));
        _effectAmountSpin.Value = effect.Amount;
        _effectRepeatSpin.Value = effect.Repeat;
        _effectFlatBonusSpin.Value = effect.FlatBonus;
        _effectUseAttackerStrengthCheck.ButtonPressed = effect.UseAttackerStrength;
        _effectUseTargetVulnerableCheck.ButtonPressed = effect.UseTargetVulnerable;
    }

    private void ClearCardEditor()
    {
        _idEdit.Text = string.Empty;
        _nameEdit.Text = string.Empty;
        _kindOption.Select(0);
        _costSpin.Value = 0;
        _descriptionEdit.Text = string.Empty;
        _descriptionZhEdit.Text = string.Empty;
        _inStarterDeckCheck.ButtonPressed = false;
        _inRewardPoolCheck.ButtonPressed = false;
        ClearEffectEditor();
        _effectList.Clear();
        _effectsPreviewEdit.Text = "[]";
    }

    private void ClearEffectEditor()
    {
        _effectTypeOption.Select(0);
        _effectTargetOption.Select(0);
        _effectAmountSpin.Value = 0;
        _effectRepeatSpin.Value = 1;
        _effectFlatBonusSpin.Value = 0;
        _effectUseAttackerStrengthCheck.ButtonPressed = true;
        _effectUseTargetVulnerableCheck.ButtonPressed = true;
    }

    private bool TryGetSelectedCard(out CardEntryData card)
    {
        card = null!;
        if (_selectedCardIndex < 0 || _selectedCardIndex >= _catalog.Cards.Count)
        {
            return false;
        }

        card = _catalog.Cards[_selectedCardIndex];
        return true;
    }

    private string BuildUniqueCardId(string baseId)
    {
        var value = string.IsNullOrWhiteSpace(baseId) ? "new_card" : baseId;
        if (!_catalog.Cards.Any(c => c.Id == value))
        {
            return value;
        }

        var i = 1;
        while (_catalog.Cards.Any(c => c.Id == $"{value}_{i}"))
        {
            i++;
        }

        return $"{value}_{i}";
    }

    private void SelectCardById(string cardId)
    {
        var visualIndex = _filteredCardIndexes.FindIndex(i => _catalog.Cards[i].Id == cardId);
        if (visualIndex >= 0)
        {
            _cardList.Select(visualIndex);
            OnCardSelected(visualIndex);
        }
    }

    private void ReselectCurrentCard()
    {
        if (_selectedCardIndex < 0 || _selectedCardIndex >= _catalog.Cards.Count)
        {
            return;
        }

        var id = _catalog.Cards[_selectedCardIndex].Id;
        SelectCardById(id);
    }

    private static CardEffectEntryData CloneEffect(CardEffectEntryData source)
    {
        return new CardEffectEntryData
        {
            Type = source.Type,
            Target = source.Target,
            Amount = source.Amount,
            Repeat = source.Repeat,
            UseAttackerStrength = source.UseAttackerStrength,
            UseTargetVulnerable = source.UseTargetVulnerable,
            FlatBonus = source.FlatBonus
        };
    }

    private static void EnsureContains(List<string> list, string value)
    {
        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }

    private static void ReplacePoolId(List<string> pool, string oldId, string newId)
    {
        for (var i = 0; i < pool.Count; i++)
        {
            if (pool[i] == oldId)
            {
                pool[i] = newId;
            }
        }
    }

    private void SetValidation(List<string> errors)
    {
        _validationLabel.Clear();
        if (errors.Count == 0)
        {
            _validationLabel.Text = LocalizationService.Get("ui.card_editor.validation_cleared", "Validation passed.");
            return;
        }

        _validationLabel.Text = string.Join("\n", errors.Select((error, i) => $"{i + 1}. {error}"));
    }

    private void OnLanguageChanged()
    {
        RefreshUiText();
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void SetStatus(string message, bool isError)
    {
        _statusLabel.Text = message;
        _statusLabel.Modulate = isError ? Colors.IndianRed : Colors.LightGreen;
    }
}
