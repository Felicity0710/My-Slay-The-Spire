using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class CardEditorScene : Control
{
    private ItemList _cardList = null!;
    private LineEdit _idEdit = null!;
    private LineEdit _nameEdit = null!;
    private OptionButton _kindOption = null!;
    private SpinBox _costSpin = null!;
    private TextEdit _descriptionEdit = null!;
    private TextEdit _descriptionZhEdit = null!;
    private TextEdit _effectsEdit = null!;
    private Label _statusLabel = null!;

    private CardCatalogData _catalog = new();
    private int _selectedIndex = -1;

    public override void _Ready()
    {
        _cardList = GetNode<ItemList>("%CardList");
        _idEdit = GetNode<LineEdit>("%IdEdit");
        _nameEdit = GetNode<LineEdit>("%NameEdit");
        _kindOption = GetNode<OptionButton>("%KindOption");
        _costSpin = GetNode<SpinBox>("%CostSpin");
        _descriptionEdit = GetNode<TextEdit>("%DescriptionEdit");
        _descriptionZhEdit = GetNode<TextEdit>("%DescriptionZhEdit");
        _effectsEdit = GetNode<TextEdit>("%EffectsEdit");
        _statusLabel = GetNode<Label>("%StatusLabel");

        _kindOption.AddItem(nameof(CardKind.Attack));
        _kindOption.AddItem(nameof(CardKind.Skill));

        GetNode<Button>("%BackButton").Pressed += OnBackPressed;
        GetNode<Button>("%ReloadButton").Pressed += OnReloadPressed;
        GetNode<Button>("%SaveButton").Pressed += OnSavePressed;
        GetNode<Button>("%NewButton").Pressed += OnNewCardPressed;
        GetNode<Button>("%DeleteButton").Pressed += OnDeleteCardPressed;
        GetNode<Button>("%ApplyButton").Pressed += OnApplyCardPressed;
        _cardList.ItemSelected += OnCardSelected;

        ReloadFromDisk();
    }

    private void ReloadFromDisk()
    {
        try
        {
            var path = CardCatalogPersistence.ResolveCardsJsonPath();
            _catalog = CardCatalogPersistence.LoadFromFile(path);
            RefreshCardList();
            SetStatus($"已加载：{path}", isError: false);
        }
        catch (Exception ex)
        {
            _catalog = new CardCatalogData();
            RefreshCardList();
            SetStatus($"加载失败：{ex.Message}", isError: true);
        }
    }

    private void OnReloadPressed()
    {
        ReloadFromDisk();
    }

    private void OnSavePressed()
    {
        try
        {
            if (_selectedIndex >= 0)
            {
                ApplyEditorToSelectedCard();
            }

            var path = CardCatalogPersistence.ResolveCardsJsonPath();
            CardCatalogPersistence.SaveToFile(path, _catalog);
            SetStatus("保存成功。", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"保存失败：{ex.Message}", isError: true);
        }
    }

    private void OnNewCardPressed()
    {
        var baseId = "new_card";
        var suffix = 1;
        var newId = $"{baseId}_{suffix}";
        while (_catalog.Cards.Any(c => c.Id == newId))
        {
            suffix++;
            newId = $"{baseId}_{suffix}";
        }

        var card = new CardEntryData
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
                    UseTargetVulnerable = false
                }
            }
        };

        _catalog.Cards.Add(card);
        RefreshCardList();
        _selectedIndex = _catalog.Cards.Count - 1;
        _cardList.Select(_selectedIndex);
        PopulateEditor(_catalog.Cards[_selectedIndex]);
        SetStatus($"已新增卡牌：{newId}", isError: false);
    }

    private void OnDeleteCardPressed()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _catalog.Cards.Count)
        {
            SetStatus("请先选择要删除的卡牌。", isError: true);
            return;
        }

        var deletingId = _catalog.Cards[_selectedIndex].Id;
        _catalog.Cards.RemoveAt(_selectedIndex);
        _catalog.StarterDeck.RemoveAll(id => id == deletingId);
        _catalog.RewardPool.RemoveAll(id => id == deletingId);
        RefreshCardList();
        ClearEditor();
        SetStatus($"已删除卡牌：{deletingId}，并移除牌池中的引用。", isError: false);
    }

    private void OnApplyCardPressed()
    {
        try
        {
            ApplyEditorToSelectedCard();
            RefreshCardList();
            _cardList.Select(_selectedIndex);
            SetStatus("已应用当前卡牌修改。", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"应用失败：{ex.Message}", isError: true);
        }
    }

    private void ApplyEditorToSelectedCard()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _catalog.Cards.Count)
        {
            throw new InvalidOperationException("请先在左侧选择卡牌。");
        }

        var effects = JsonSerializer.Deserialize<List<CardEffectEntryData>>(_effectsEdit.Text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<CardEffectEntryData>();

        var oldId = _catalog.Cards[_selectedIndex].Id;
        var card = _catalog.Cards[_selectedIndex];
        card.Id = _idEdit.Text.Trim();
        card.Name = _nameEdit.Text.Trim();
        card.Kind = _kindOption.GetItemText(_kindOption.Selected);
        card.Cost = (int)_costSpin.Value;
        card.Description = _descriptionEdit.Text.Trim();
        card.DescriptionZh = _descriptionZhEdit.Text.Trim();
        card.Effects = effects;

        if (!string.Equals(oldId, card.Id, StringComparison.Ordinal))
        {
            ReplacePoolId(_catalog.StarterDeck, oldId, card.Id);
            ReplacePoolId(_catalog.RewardPool, oldId, card.Id);
        }

        CardCatalogPersistence.ValidateOrThrow(_catalog, "editor");
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

    private void OnCardSelected(long index)
    {
        _selectedIndex = (int)index;
        if (_selectedIndex >= 0 && _selectedIndex < _catalog.Cards.Count)
        {
            PopulateEditor(_catalog.Cards[_selectedIndex]);
        }
    }

    private void RefreshCardList()
    {
        _cardList.Clear();
        foreach (var card in _catalog.Cards)
        {
            _cardList.AddItem($"{card.Id} [{card.Kind}] (Cost {card.Cost})");
        }

        _selectedIndex = -1;
    }

    private void PopulateEditor(CardEntryData card)
    {
        _idEdit.Text = card.Id;
        _nameEdit.Text = card.Name;
        _kindOption.Select(string.Equals(card.Kind, nameof(CardKind.Attack), StringComparison.OrdinalIgnoreCase) ? 0 : 1);
        _costSpin.Value = card.Cost;
        _descriptionEdit.Text = card.Description;
        _descriptionZhEdit.Text = card.DescriptionZh ?? string.Empty;
        _effectsEdit.Text = JsonSerializer.Serialize(card.Effects, new JsonSerializerOptions { WriteIndented = true });
    }

    private void ClearEditor()
    {
        _idEdit.Text = string.Empty;
        _nameEdit.Text = string.Empty;
        _kindOption.Select(0);
        _costSpin.Value = 0;
        _descriptionEdit.Text = string.Empty;
        _descriptionZhEdit.Text = string.Empty;
        _effectsEdit.Text = "[]";
        _selectedIndex = -1;
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
