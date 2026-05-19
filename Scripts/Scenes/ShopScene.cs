using Godot;
using System;
using System.Collections.Generic;

public partial class ShopScene : Control
{
    private enum ShopItemKind
    {
        Card,
        Relic,
        Potion
    }

    private sealed class ShopItem
    {
        public ShopItemKind Kind { get; init; }
        public string Id { get; init; } = string.Empty;
        public int Price { get; set; }
        public bool Sold { get; set; }
        public Button BuyButton { get; set; } = null!;
        public Label NameLabel { get; set; } = null!;
    }

    private const int RemoveServiceBasePrice = 75;

    private Label _titleLabel = null!;
    private Label _goldLabel = null!;
    private Label _statusLabel = null!;
    private RunStatusOverlay _statusOverlay = null!;
    private VBoxContainer _itemsVBox = null!;
    private GridContainer _cardGrid = null!;
    private GridContainer _relicGrid = null!;
    private GridContainer _potionGrid = null!;
    private Label _cardSectionLabel = null!;
    private Label _relicSectionLabel = null!;
    private Label _potionSectionLabel = null!;
    private Control _cardSection = null!;
    private Control _relicSection = null!;
    private Control _potionSection = null!;
    private Button _removeCardButton = null!;
    private Button _robButton = null!;
    private Button _leaveButton = null!;

    private Control _removeOverlay = null!;
    private Label _removeTitleLabel = null!;
    private Label _removeHintLabel = null!;
    private GridContainer _removeCardGrid = null!;
    private Button _confirmRemoveButton = null!;
    private Button _cancelRemoveButton = null!;
    private int _removeSelectedIndex = -1;
    private readonly List<PanelContainer> _removeCardFrames = new();

    private Random _rng = null!;
    private readonly List<ShopItem> _items = new();
    private bool _removeServiceUsed;
    private bool _robbed;

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("shop");
        _rng = state.Rng;
        AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());
        _statusOverlay = GD.Load<PackedScene>("res://Scenes/RunStatusOverlay.tscn").Instantiate<RunStatusOverlay>();
        AddChild(_statusOverlay);

        _titleLabel = GetNode<Label>("%TitleLabel");
        _goldLabel = GetNode<Label>("%GoldLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _itemsVBox = GetNode<VBoxContainer>("%ItemsVBox");
        _cardGrid = GetNode<GridContainer>("%CardGrid");
        _relicGrid = GetNode<GridContainer>("%RelicGrid");
        _potionGrid = GetNode<GridContainer>("%PotionGrid");
        _cardSectionLabel = GetNode<Label>("%CardSectionLabel");
        _relicSectionLabel = GetNode<Label>("%RelicSectionLabel");
        _potionSectionLabel = GetNode<Label>("%PotionSectionLabel");
        _cardSection = GetNode<Control>("%CardSection");
        _relicSection = GetNode<Control>("%RelicSection");
        _potionSection = GetNode<Control>("%PotionSection");
        _removeCardButton = GetNode<Button>("%RemoveCardButton");
        _robButton = GetNode<Button>("%RobButton");
        _leaveButton = GetNode<Button>("%LeaveButton");

        _removeOverlay = GetNode<Control>("%RemoveOverlay");
        _removeTitleLabel = GetNode<Label>("%RemoveTitleLabel");
        _removeHintLabel = GetNode<Label>("%RemoveHint");
        _removeCardGrid = GetNode<GridContainer>("%RemoveCardGrid");
        _confirmRemoveButton = GetNode<Button>("%ConfirmRemoveButton");
        _cancelRemoveButton = GetNode<Button>("%CancelRemoveButton");

        _removeCardButton.Pressed += OnRemoveCardPressed;
        _robButton.Pressed += OnRobPressed;
        _leaveButton.Pressed += OnLeavePressed;
        _confirmRemoveButton.Pressed += OnConfirmRemovePressed;
        _cancelRemoveButton.Pressed += OnCancelRemovePressed;

        _removeOverlay.Visible = false;
        _titleLabel.Text = LocalizationService.Get("ui.shop.title", "Shop");

        var returningFromRobVictory = state.PendingMerchantFightVictory && state.ShopSnapshotHasData;
        if (returningFromRobVictory)
        {
            RestoreInventoryFromSnapshot(state);
            _robbed = true;
            _removeServiceUsed = state.ShopSnapshotRemoveServiceUsed;
            state.ConsumePendingMerchantFightVictory();
            state.ClearShopSnapshot();
        }
        else
        {
            BuildInventory();
        }

        RenderItems();
        var welcome = returningFromRobVictory
            ? LocalizationService.Get(
                "ui.shop.status_rob_victory",
                "You defeated the merchant! Help yourself to anything left behind.")
            : LocalizationService.Get("ui.shop.status_welcome", "Welcome, traveler. Pick your wares.");
        RefreshUi(welcome);
    }

    private void RestoreInventoryFromSnapshot(GameState state)
    {
        _items.Clear();
        foreach (var entry in state.ShopSnapshot)
        {
            if (!TryParseShopItemKind(entry.Kind, out var kind))
            {
                continue;
            }

            _items.Add(new ShopItem
            {
                Kind = kind,
                Id = entry.Id,
                Price = entry.Price,
                Sold = entry.Sold
            });
        }
    }

    private static bool TryParseShopItemKind(string raw, out ShopItemKind kind)
    {
        switch (raw)
        {
            case "card":
                kind = ShopItemKind.Card;
                return true;
            case "relic":
                kind = ShopItemKind.Relic;
                return true;
            case "potion":
                kind = ShopItemKind.Potion;
                return true;
            default:
                kind = ShopItemKind.Card;
                return false;
        }
    }

    private static string ShopItemKindToString(ShopItemKind kind)
    {
        return kind switch
        {
            ShopItemKind.Card => "card",
            ShopItemKind.Relic => "relic",
            ShopItemKind.Potion => "potion",
            _ => "card"
        };
    }

    private void BuildInventory()
    {
        _items.Clear();

        var state = GetNode<GameState>("/root/GameState");
        var cardPool = CardData.RewardPoolIds();
        cardPool.RemoveAll(id => id == "strike" || id == "defend");
        Shuffle(cardPool);
        var cardCount = Math.Min(3, cardPool.Count);
        for (var i = 0; i < cardCount; i++)
        {
            var rolledId = state.MaybeUpgradeCardId(cardPool[i]);
            var card = CardData.CreateById(rolledId);
            var price = 45 + card.Cost * 10 + _rng.Next(0, 16);
            if (card.IsUpgraded)
            {
                price += 30;
            }
            _items.Add(new ShopItem
            {
                Kind = ShopItemKind.Card,
                Id = rolledId,
                Price = price
            });
        }

        var relicPool = new List<string>(RelicData.AllRelicIds());
        relicPool.RemoveAll(state.HasRelic);
        Shuffle(relicPool);
        var relicCount = Math.Min(2, relicPool.Count);
        for (var i = 0; i < relicCount; i++)
        {
            _items.Add(new ShopItem
            {
                Kind = ShopItemKind.Relic,
                Id = relicPool[i],
                Price = 140 + _rng.Next(0, 61)
            });
        }

        var potionPool = new List<string>(PotionData.AllPotionIds());
        Shuffle(potionPool);
        var potionCount = Math.Min(2, potionPool.Count);
        for (var i = 0; i < potionCount; i++)
        {
            _items.Add(new ShopItem
            {
                Kind = ShopItemKind.Potion,
                Id = potionPool[i],
                Price = 50 + _rng.Next(0, 31)
            });
        }
    }

    private void RenderItems()
    {
        ClearGrid(_cardGrid);
        ClearGrid(_relicGrid);
        ClearGrid(_potionGrid);

        _cardSectionLabel.Text = LocalizationService.Get("ui.shop.section_cards", "Cards");
        _relicSectionLabel.Text = LocalizationService.Get("ui.shop.section_relics", "Relics");
        _potionSectionLabel.Text = LocalizationService.Get("ui.shop.section_potions", "Potions");

        var hasCards = false;
        var hasRelics = false;
        var hasPotions = false;

        foreach (var item in _items)
        {
            var tile = BuildItemTile(item);
            switch (item.Kind)
            {
                case ShopItemKind.Card:
                    _cardGrid.AddChild(tile);
                    hasCards = true;
                    break;
                case ShopItemKind.Relic:
                    _relicGrid.AddChild(tile);
                    hasRelics = true;
                    break;
                case ShopItemKind.Potion:
                    _potionGrid.AddChild(tile);
                    hasPotions = true;
                    break;
            }
        }

        _cardSection.Visible = hasCards;
        _relicSection.Visible = hasRelics;
        _potionSection.Visible = hasPotions;
    }

    private static void ClearGrid(GridContainer grid)
    {
        foreach (Node child in grid.GetChildren())
        {
            child.QueueFree();
        }
    }

    private Control BuildItemTile(ShopItem item)
    {
        var tile = new PanelContainer();
        var tileStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.13f, 0.18f, 0.95f),
            BorderColor = TileBorderColor(item.Kind),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 10,
            ContentMarginTop = 10,
            ContentMarginRight = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0, 0, 0, 0.4f),
            ShadowSize = 4
        };
        tile.AddThemeStyleboxOverride("panel", tileStyle);
        tile.CustomMinimumSize = item.Kind == ShopItemKind.Card
            ? new Vector2(200, 260)
            : new Vector2(160, 200);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        tile.AddChild(vbox);

        var nameLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.78f, 1));

        switch (item.Kind)
        {
            case ShopItemKind.Card:
            {
                var card = CardData.CreateById(item.Id);
                nameLabel.Text = card.GetLocalizedName();
                vbox.AddChild(nameLabel);

                var kindLabel = new Label
                {
                    Text = card.Kind switch
                    {
                        CardKind.Attack => LocalizationService.Get("ui.card_kind.attack", "Attack"),
                        CardKind.Skill => LocalizationService.Get("ui.card_kind.skill", "Skill"),
                        _ => card.Kind.ToString()
                    },
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                kindLabel.AddThemeFontSizeOverride("font_size", 11);
                kindLabel.AddThemeColorOverride("font_color", card.Kind == CardKind.Attack
                    ? new Color("fca5a5")
                    : new Color("93c5fd"));
                vbox.AddChild(kindLabel);

                var art = new TextureRect
                {
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                    CustomMinimumSize = new Vector2(0, 92),
                    Texture = TryLoadTexture(card.ArtPath)
                };
                vbox.AddChild(art);

                var costLabel = new Label
                {
                    Text = LocalizationService.Format("ui.shop.card_cost", "Cost {0}", card.Cost),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                costLabel.AddThemeFontSizeOverride("font_size", 13);
                costLabel.AddThemeColorOverride("font_color", new Color("7dd3fc"));
                vbox.AddChild(costLabel);

                var descLabel = new Label
                {
                    Text = card.GetLocalizedDescription(),
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                descLabel.AddThemeFontSizeOverride("font_size", 11);
                descLabel.AddThemeColorOverride("font_color", new Color("cbd5e1"));
                vbox.AddChild(descLabel);
                break;
            }
            case ShopItemKind.Relic:
            {
                var relic = RelicData.CreateById(item.Id);
                nameLabel.Text = relic.LocalizedName;

                var icon = new TextureRect
                {
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                    CustomMinimumSize = new Vector2(0, 80),
                    Texture = TryLoadTexture(CombatVisualCatalog.GetRelicIconPath(item.Id))
                };
                vbox.AddChild(icon);
                vbox.AddChild(nameLabel);

                var descLabel = new Label
                {
                    Text = relic.LocalizedDescription,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                descLabel.AddThemeFontSizeOverride("font_size", 11);
                descLabel.AddThemeColorOverride("font_color", new Color("cbd5e1"));
                vbox.AddChild(descLabel);
                break;
            }
            case ShopItemKind.Potion:
            {
                var potion = PotionData.CreateById(item.Id);
                var potionName = LocalizationService.Get($"potion.{potion.Id}.name", potion.Name);
                var potionDesc = LocalizationService.Get($"potion.{potion.Id}.description", potion.Description);
                nameLabel.Text = potionName;

                var swatch = new PanelContainer();
                var swatchStyle = new StyleBoxFlat
                {
                    BgColor = PotionSwatchColor(item.Id),
                    BorderColor = new Color(1, 1, 1, 0.35f),
                    BorderWidthLeft = 1,
                    BorderWidthTop = 1,
                    BorderWidthRight = 1,
                    BorderWidthBottom = 1,
                    CornerRadiusTopLeft = 40,
                    CornerRadiusTopRight = 40,
                    CornerRadiusBottomLeft = 40,
                    CornerRadiusBottomRight = 40
                };
                swatch.AddThemeStyleboxOverride("panel", swatchStyle);
                swatch.CustomMinimumSize = new Vector2(0, 70);
                var swatchIcon = new Label
                {
                    Text = "🧪",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                swatchIcon.AddThemeFontSizeOverride("font_size", 36);
                swatch.AddChild(swatchIcon);
                vbox.AddChild(swatch);

                vbox.AddChild(nameLabel);

                var descLabel = new Label
                {
                    Text = potionDesc,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                descLabel.AddThemeFontSizeOverride("font_size", 11);
                descLabel.AddThemeColorOverride("font_color", new Color("cbd5e1"));
                vbox.AddChild(descLabel);
                break;
            }
        }

        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(spacer);

        var priceButton = new Button
        {
            Text = FormatPrice(item.Price),
            CustomMinimumSize = new Vector2(0, 38)
        };
        priceButton.AddThemeFontSizeOverride("font_size", 14);
        var captured = item;
        priceButton.Pressed += () => OnBuyPressed(captured);
        vbox.AddChild(priceButton);

        item.NameLabel = nameLabel;
        item.BuyButton = priceButton;
        return tile;
    }

    private static Color TileBorderColor(ShopItemKind kind) => kind switch
    {
        ShopItemKind.Card => new Color(0.49f, 0.68f, 0.82f, 0.7f),
        ShopItemKind.Relic => new Color(0.95f, 0.65f, 0.30f, 0.8f),
        ShopItemKind.Potion => new Color(0.55f, 0.85f, 0.60f, 0.7f),
        _ => new Color(0.6f, 0.6f, 0.6f, 0.6f)
    };

    private static Color PotionSwatchColor(string potionId) => potionId switch
    {
        "healing_potion" => new Color(0.85f, 0.30f, 0.30f, 0.85f),
        "strength_potion" => new Color(0.92f, 0.55f, 0.20f, 0.85f),
        "swift_potion" => new Color(0.45f, 0.85f, 0.55f, 0.85f),
        "guard_potion" => new Color(0.40f, 0.65f, 0.95f, 0.85f),
        "fury_potion" => new Color(0.75f, 0.30f, 0.85f, 0.85f),
        _ => new Color(0.6f, 0.6f, 0.6f, 0.85f)
    };

    private static Texture2D TryLoadTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null!;
        }

        if (ResourceLoader.Exists(path))
        {
            return GD.Load<Texture2D>(path);
        }

        return null!;
    }

    private string FormatPrice(int price)
    {
        if (_robbed || price <= 0)
        {
            return LocalizationService.Get("ui.shop.free", "Free");
        }

        return LocalizationService.Format("ui.shop.buy_price", "💰 {0}", price);
    }

    private void OnBuyPressed(ShopItem item)
    {
        if (item.Sold)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        var effectivePrice = _robbed ? 0 : item.Price;
        if (!state.TrySpendGold(effectivePrice))
        {
            RefreshUi(LocalizationService.Get("ui.shop.status_no_gold", "Not enough gold."));
            return;
        }

        string acquired;
        switch (item.Kind)
        {
            case ShopItemKind.Card:
                state.AddCardToDeck(item.Id);
                acquired = CardData.CreateById(item.Id).GetLocalizedName();
                break;
            case ShopItemKind.Relic:
                state.AddRelic(item.Id);
                acquired = RelicData.CreateById(item.Id).LocalizedName;
                break;
            case ShopItemKind.Potion:
                if (!state.TryAddPotion(item.Id))
                {
                    state.AddGold(effectivePrice);
                    RefreshUi(LocalizationService.Format(
                        "ui.shop.status_potion_full",
                        "Potion belt is full (max {0}).",
                        GameState.PotionInventoryCapacity));
                    return;
                }
                acquired = PotionData.CreateById(item.Id).Name;
                break;
            default:
                return;
        }

        item.Sold = true;
        item.BuyButton.Disabled = true;
        item.BuyButton.Text = LocalizationService.Get("ui.shop.sold", "Sold");
        RefreshUi(LocalizationService.Format("ui.shop.status_bought", "Bought: {0}", acquired));
    }

    private void OnRemoveCardPressed()
    {
        if (_removeServiceUsed)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");
        var price = _robbed ? 0 : RemoveServiceBasePrice;
        if (state.Gold < price)
        {
            RefreshUi(LocalizationService.Get("ui.shop.status_no_gold", "Not enough gold."));
            return;
        }

        if (state.DeckCardIds.Count == 0)
        {
            RefreshUi(LocalizationService.Get("ui.shop.status_deck_empty", "Your deck is empty."));
            return;
        }

        ShowRemoveOverlay();
    }

    private void ShowRemoveOverlay()
    {
        var state = GetNode<GameState>("/root/GameState");
        _removeTitleLabel.Text = LocalizationService.Get("ui.shop.remove_title", "Choose a card to remove");
        _removeHintLabel.Text = LocalizationService.Get("ui.shop.remove_hint", "Click a card to select. Click Confirm to delete it from your deck.");
        _confirmRemoveButton.Text = LocalizationService.Get("ui.shop.remove_confirm", "Confirm");
        _cancelRemoveButton.Text = LocalizationService.Get("ui.shop.remove_cancel", "Cancel");

        // Tear down old grid contents and rebuild fresh — deck contents may
        // have shifted since last open.
        foreach (var child in _removeCardGrid.GetChildren())
        {
            child.QueueFree();
        }
        _removeCardFrames.Clear();
        _removeSelectedIndex = -1;

        for (var i = 0; i < state.DeckCardIds.Count; i++)
        {
            var capturedIndex = i;
            var card = CardData.CreateById(state.DeckCardIds[i]);
            var frame = BuildRemoveCardTile(card, capturedIndex);
            _removeCardGrid.AddChild(frame);
            _removeCardFrames.Add(frame);
        }

        _confirmRemoveButton.Disabled = true;
        _removeOverlay.Visible = true;
    }

    private PanelContainer BuildRemoveCardTile(CardData card, int deckIndex)
    {
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(210, 290),
            MouseFilter = MouseFilterEnum.Pass
        };
        frame.AddThemeStyleboxOverride("panel", BuildRemoveTileStyle(selected: false));

        var view = new CardView();
        view.SetUseTopLevel(false);
        view.SetDragEnabled(false);
        view.Setup(card);
        view.Clicked = _ => SelectRemoveCard(deckIndex);
        frame.AddChild(view);
        return frame;
    }

    private static StyleBoxFlat BuildRemoveTileStyle(bool selected) => new()
    {
        BgColor = selected ? new Color(0.30f, 0.10f, 0.10f, 0.95f) : new Color(0.10f, 0.13f, 0.16f, 0.95f),
        BorderColor = selected ? new Color(1f, 0.50f, 0.50f, 1f) : new Color(0.55f, 0.42f, 0.22f, 0.85f),
        BorderWidthLeft = selected ? 4 : 2,
        BorderWidthTop = selected ? 4 : 2,
        BorderWidthRight = selected ? 4 : 2,
        BorderWidthBottom = selected ? 4 : 2,
        CornerRadiusTopLeft = 12,
        CornerRadiusTopRight = 12,
        CornerRadiusBottomLeft = 12,
        CornerRadiusBottomRight = 12,
        ContentMarginLeft = 4,
        ContentMarginTop = 4,
        ContentMarginRight = 4,
        ContentMarginBottom = 4,
        ShadowColor = selected ? new Color(0.85f, 0.30f, 0.30f, 0.55f) : new Color(0, 0, 0, 0.3f),
        ShadowSize = selected ? 10 : 4
    };

    private void SelectRemoveCard(int deckIndex)
    {
        _removeSelectedIndex = deckIndex;
        for (var i = 0; i < _removeCardFrames.Count; i++)
        {
            if (!IsInstanceValid(_removeCardFrames[i]))
            {
                continue;
            }
            _removeCardFrames[i].AddThemeStyleboxOverride("panel", BuildRemoveTileStyle(i == deckIndex));
        }
        _confirmRemoveButton.Disabled = false;
    }

    private void OnConfirmRemovePressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        var index = _removeSelectedIndex;
        if (index < 0 || index >= state.DeckCardIds.Count)
        {
            return;
        }

        var price = _robbed ? 0 : RemoveServiceBasePrice;
        if (!state.TrySpendGold(price))
        {
            RefreshUi(LocalizationService.Get("ui.shop.status_no_gold", "Not enough gold."));
            return;
        }

        var removedId = state.DeckCardIds[index];
        state.DeckCardIds.RemoveAt(index);

        _removeServiceUsed = true;
        _removeOverlay.Visible = false;
        _removeSelectedIndex = -1;

        var card = CardData.CreateById(removedId);
        RefreshUi(LocalizationService.Format(
            "ui.shop.status_removed_card",
            "Removed card: {0}",
            card.GetLocalizedName()));
    }

    private void OnCancelRemovePressed()
    {
        _removeOverlay.Visible = false;
    }

    private void OnRobPressed()
    {
        if (_robbed)
        {
            return;
        }

        var state = GetNode<GameState>("/root/GameState");

        var snapshot = new List<GameState.ShopInventoryEntry>();
        foreach (var item in _items)
        {
            snapshot.Add(new GameState.ShopInventoryEntry
            {
                Kind = ShopItemKindToString(item.Kind),
                Id = item.Id,
                Price = item.Price,
                Sold = item.Sold
            });
        }

        state.SaveShopSnapshot(snapshot, _removeServiceUsed);
        state.BeginMerchantFight();
        state.SetUiPhase("battle");
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void OnLeavePressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ResolveShopExit();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void RefreshUi(string status)
    {
        var state = GetNode<GameState>("/root/GameState");
        _goldLabel.Text = state.Gold.ToString();
        if (!string.IsNullOrWhiteSpace(status))
        {
            _statusLabel.Text = status;
        }

        // Mirror gold/deck/relic/potion changes into the floating status bar.
        // Without this, players see e.g. their deck count stay stale after
        // buying a card — and the potion slot row doesn't show the new bottle.
        if (IsInstanceValid(_statusOverlay))
        {
            _statusOverlay.Refresh();
        }

        var removePrice = _robbed ? 0 : RemoveServiceBasePrice;
        if (_removeServiceUsed)
        {
            _removeCardButton.Disabled = true;
            _removeCardButton.Text = LocalizationService.Get("ui.shop.remove_done", "🗡 Remove service used");
        }
        else
        {
            _removeCardButton.Disabled = false;
            _removeCardButton.Text = removePrice <= 0
                ? LocalizationService.Get("ui.shop.remove_free", "🗡 Remove a card\nFree")
                : LocalizationService.Format("ui.shop.remove_price", "🗡 Remove a card\n💰 {0}", removePrice);
        }

        if (_robbed)
        {
            _robButton.Disabled = true;
            _robButton.Text = LocalizationService.Get("ui.shop.rob_done", "Merchant has fled");
        }
        else
        {
            _robButton.Text = LocalizationService.Get("ui.shop.rob", "Rob the shop");
        }

        _leaveButton.Text = LocalizationService.Get("ui.shop.leave", "Leave");
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
