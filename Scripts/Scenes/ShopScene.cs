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
    private VBoxContainer _itemsVBox = null!;
    private Button _removeCardButton = null!;
    private Button _robButton = null!;
    private Button _leaveButton = null!;

    private Control _removeOverlay = null!;
    private Label _removeTitleLabel = null!;
    private ItemList _removeDeckList = null!;
    private Button _confirmRemoveButton = null!;
    private Button _cancelRemoveButton = null!;

    private readonly Random _rng = new();
    private readonly List<ShopItem> _items = new();
    private bool _removeServiceUsed;
    private bool _robbed;

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("shop");

        _titleLabel = GetNode<Label>("%TitleLabel");
        _goldLabel = GetNode<Label>("%GoldLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _itemsVBox = GetNode<VBoxContainer>("%ItemsVBox");
        _removeCardButton = GetNode<Button>("%RemoveCardButton");
        _robButton = GetNode<Button>("%RobButton");
        _leaveButton = GetNode<Button>("%LeaveButton");

        _removeOverlay = GetNode<Control>("%RemoveOverlay");
        _removeTitleLabel = GetNode<Label>("%RemoveTitleLabel");
        _removeDeckList = GetNode<ItemList>("%RemoveDeckList");
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
        foreach (Node child in _itemsVBox.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var item in _items)
        {
            var row = new HBoxContainer();
            row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddThemeConstantOverride("separation", 12);

            var label = new Label
            {
                Text = DescribeItem(item),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.CustomMinimumSize = new Vector2(0, 56);
            row.AddChild(label);

            var button = new Button
            {
                Text = FormatPrice(item.Price),
                CustomMinimumSize = new Vector2(150, 48)
            };
            var captured = item;
            button.Pressed += () => OnBuyPressed(captured);
            row.AddChild(button);

            item.NameLabel = label;
            item.BuyButton = button;
            _itemsVBox.AddChild(row);
        }
    }

    private string DescribeItem(ShopItem item)
    {
        switch (item.Kind)
        {
            case ShopItemKind.Card:
            {
                var card = CardData.CreateById(item.Id);
                var cost = LocalizationService.Format("ui.shop.card_cost", "Cost {0}", card.Cost);
                return $"[{LocalizationService.Get("ui.shop.label_card", "Card")}] {card.GetLocalizedName()}\n{cost} · {card.GetLocalizedDescription()}";
            }
            case ShopItemKind.Relic:
            {
                var relic = RelicData.CreateById(item.Id);
                return $"[{LocalizationService.Get("ui.shop.label_relic", "Relic")}] {relic.LocalizedName}\n{relic.LocalizedDescription}";
            }
            case ShopItemKind.Potion:
            {
                var potion = PotionData.CreateById(item.Id);
                return $"[{LocalizationService.Get("ui.shop.label_potion", "Potion")}] {potion.Name}\n{potion.Description}";
            }
        }

        return string.Empty;
    }

    private string FormatPrice(int price)
    {
        if (_robbed || price <= 0)
        {
            return LocalizationService.Get("ui.shop.free", "Free");
        }

        return LocalizationService.Format("ui.shop.buy_price", "Buy {0}g", price);
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
        _confirmRemoveButton.Text = LocalizationService.Get("ui.shop.remove_confirm", "Confirm");
        _cancelRemoveButton.Text = LocalizationService.Get("ui.shop.remove_cancel", "Cancel");

        _removeDeckList.Clear();
        for (var i = 0; i < state.DeckCardIds.Count; i++)
        {
            var card = CardData.CreateById(state.DeckCardIds[i]);
            _removeDeckList.AddItem(LocalizationService.Format(
                "ui.shop.remove_entry",
                "#{0:00} {1} · Cost {2}",
                i + 1,
                card.GetLocalizedName(),
                card.Cost));
        }

        if (state.DeckCardIds.Count > 0)
        {
            _removeDeckList.Select(0);
        }

        _removeOverlay.Visible = true;
    }

    private void OnConfirmRemovePressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        var selected = _removeDeckList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        var index = selected[0];
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
        _goldLabel.Text = LocalizationService.Format("ui.shop.gold_label", "Gold: {0}", state.Gold);
        if (!string.IsNullOrWhiteSpace(status))
        {
            _statusLabel.Text = status;
        }

        var removePrice = _robbed ? 0 : RemoveServiceBasePrice;
        if (_removeServiceUsed)
        {
            _removeCardButton.Disabled = true;
            _removeCardButton.Text = LocalizationService.Get("ui.shop.remove_done", "Remove service used");
        }
        else
        {
            _removeCardButton.Disabled = false;
            _removeCardButton.Text = removePrice <= 0
                ? LocalizationService.Get("ui.shop.remove_free", "Remove a card: Free")
                : LocalizationService.Format("ui.shop.remove_price", "Remove a card: {0}g", removePrice);
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
