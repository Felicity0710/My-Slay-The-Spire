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

        BuildEnumOptions();

        GetNode<Button>("%BackButton").Pressed += OnBackPressed;
        GetNode<Button>("%ReloadButton").Pressed += ReloadFromDisk;
        GetNode<Button>("%SaveButton").Pressed += OnSavePressed;
        GetNode<Button>("%ValidateButton").Pressed += OnValidatePressed;
        GetNode<Button>("%NewButton").Pressed += OnNewCardPressed;
        GetNode<Button>("%DuplicateButton").Pressed += OnDuplicateCardPressed;
        GetNode<Button>("%DeleteButton").Pressed += OnDeleteCardPressed;
        GetNode<Button>("%ApplyCardButton").Pressed += OnApplyCardPressed;

        GetNode<Button>("%AddEffectButton").Pressed += OnAddEffectPressed;
        GetNode<Button>("%RemoveEffectButton").Pressed += OnRemoveEffectPressed;
        GetNode<Button>("%ApplyEffectButton").Pressed += OnApplyEffectPressed;

        _cardList.ItemSelected += OnCardSelected;
        _effectList.ItemSelected += OnEffectSelected;
        _searchEdit.TextChanged += _ => RefreshCardList();

        ReloadFromDisk();
    }

    private void BuildEnumOptions()
    {
        _kindOption.Clear();
        _kindOption.AddItem(nameof(CardKind.Attack));
        _kindOption.AddItem(nameof(CardKind.Skill));

        _effectTypeOption.Clear();
        foreach (var value in Enum.GetValues<CardEffectType>())
        {
            _effectTypeOption.AddItem(value.ToString());
        }

        _effectTargetOption.Clear();
        foreach (var value in Enum.GetValues<CardEffectTarget>())
        {
            _effectTargetOption.AddItem(value.ToString());
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
            SetStatus($"已加载：{path}", false);
        }
        catch (Exception ex)
        {
            _catalog = new CardCatalogData();
            _selectedCardIndex = -1;
            _selectedEffectIndex = -1;
            RefreshCardList();
            ClearCardEditor();
            SetValidation(new List<string> { ex.Message });
            SetStatus($"加载失败：{ex.Message}", true);
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
            SetStatus("保存成功。", false);
        }
        catch (Exception ex)
        {
            SetStatus($"保存失败：{ex.Message}", true);
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
            SetStatus(errors.Count == 0 ? "校验通过。" : "校验发现问题，请查看下方列表。", errors.Count > 0);
        }
        catch (Exception ex)
        {
            SetValidation(new List<string> { ex.Message });
            SetStatus($"校验失败：{ex.Message}", true);
        }
    }

    private void OnNewCardPressed()
    {
        var newId = BuildUniqueCardId("new_card");
        _catalog.Cards.Add(new CardEntryData
        {
            Id = newId,
            Name = "New Card",
            Kind = nameof(CardKind.Skill),
            Cost = 1,
            Description = "",
            DescriptionZh = "",
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
        SetStatus($"已新增卡牌：{newId}", false);
    }

    private void OnDuplicateCardPressed()
    {
        if (!TryGetSelectedCard(out var source))
        {
            SetStatus("请先选择要复制的卡牌。", true);
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
        SetStatus($"已复制卡牌：{copy.Id}", false);
    }

    private void OnDeleteCardPressed()
    {
        if (!TryGetSelectedCard(out var card))
        {
            SetStatus("请先选择要删除的卡牌。", true);
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
        SetStatus($"已删除卡牌：{cardId}（并清理牌池引用）", false);
    }

    private void OnApplyCardPressed()
    {
        try
        {
            ApplyCardEditorToCurrentCard();
            RefreshCardList();
            ReselectCurrentCard();
            SetStatus("已应用卡牌信息。", false);
        }
        catch (Exception ex)
        {
            SetStatus($"应用失败：{ex.Message}", true);
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
            throw new InvalidOperationException("请先选择卡牌。 ");
        }

        var oldId = card.Id;

        card.Id = _idEdit.Text.Trim();
        card.Name = _nameEdit.Text.Trim();
        card.Kind = _kindOption.GetItemText(_kindOption.Selected);
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
            SetStatus("请先选择卡牌后再新增效果。", true);
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
            SetStatus("请先选择要删除的效果。", true);
            return;
        }

        card.Effects.RemoveAt(_selectedEffectIndex);
        _selectedEffectIndex = -1;
        RefreshEffectList();
        ClearEffectEditor();
        SetStatus("已删除效果。", false);
    }

    private void OnApplyEffectPressed()
    {
        try
        {
            if (!TryGetSelectedCard(out var card) || _selectedEffectIndex < 0 || _selectedEffectIndex >= card.Effects.Count)
            {
                throw new InvalidOperationException("请先选择要编辑的效果。 ");
            }

            var effect = card.Effects[_selectedEffectIndex];
            effect.Type = _effectTypeOption.GetItemText(_effectTypeOption.Selected);
            effect.Target = _effectTargetOption.GetItemText(_effectTargetOption.Selected);
            effect.Amount = (int)_effectAmountSpin.Value;
            effect.Repeat = (int)_effectRepeatSpin.Value;
            effect.FlatBonus = (int)_effectFlatBonusSpin.Value;
            effect.UseAttackerStrength = _effectUseAttackerStrengthCheck.ButtonPressed;
            effect.UseTargetVulnerable = _effectUseTargetVulnerableCheck.ButtonPressed;

            CardCatalogPersistence.ValidateOrThrow(_catalog, "effect editor");
            RefreshEffectList();
            SetStatus("已应用效果修改。", false);
        }
        catch (Exception ex)
        {
            SetStatus($"效果更新失败：{ex.Message}", true);
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
            if (!string.IsNullOrEmpty(keyword)
                && !pair.Card.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !pair.Card.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _filteredCardIndexes.Add(pair.Index);
            _cardList.AddItem($"{pair.Card.Id} · {pair.Card.Name} [{pair.Card.Kind}] Cost {pair.Card.Cost}");
        }

        _summaryLabel.Text = $"共 {_catalog.Cards.Count} 张卡，展示 {_filteredCardIndexes.Count} 张。starterDeck: {_catalog.StarterDeck.Count}，rewardPool: {_catalog.RewardPool.Count}";
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
            _effectList.AddItem($"#{i + 1} {effect.Type} -> {effect.Target} x{effect.Repeat} amount:{effect.Amount} bonus:{effect.FlatBonus}");
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
        _effectTypeOption.Select(FindOptionIndex(_effectTypeOption, effect.Type));
        _effectTargetOption.Select(FindOptionIndex(_effectTargetOption, effect.Target));
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

    private static int FindOptionIndex(OptionButton option, string text)
    {
        for (var i = 0; i < option.ItemCount; i++)
        {
            if (string.Equals(option.GetItemText(i), text, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
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
            _validationLabel.Text = "✅ 当前无校验错误。";
            return;
        }

        _validationLabel.Text = string.Join("\n", errors.Select((error, i) => $"{i + 1}. {error}"));
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
