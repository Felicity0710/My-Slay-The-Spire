using Godot;
using System;
using System.Collections.Generic;

public partial class EventScene : Control
{
    private sealed class EventOption
    {
        public string Label = string.Empty;
        public Action OnSelected = () => { };
        public OptionVariant Variant = OptionVariant.Default;
    }

    private enum OptionVariant { Default, Leave, Back }

    private Label _titleLabel = null!;
    private Label _descLabel = null!;
    private Label _breadcrumbLabel = null!;
    private VBoxContainer _optionsVBox = null!;
    private TextureRect _eventTexture = null!;
    private Label _placeholderIcon = null!;

    private readonly List<List<EventOption>> _menuStack = new();
    private readonly List<string> _breadcrumbStack = new();

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("event");
        AudioManager.PlayBgm("event");
        AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());
        AddChild(GD.Load<PackedScene>("res://Scenes/RunStatusOverlay.tscn").Instantiate());

        _titleLabel = GetNode<Label>("%TitleLabel");
        _descLabel = GetNode<Label>("%DescLabel");
        _breadcrumbLabel = GetNode<Label>("%BreadcrumbLabel");
        _optionsVBox = GetNode<VBoxContainer>("%OptionsVBox");
        _eventTexture = GetNode<TextureRect>("%EventTexture");
        _placeholderIcon = GetNode<Label>("%PlaceholderIcon");

        LocalizationSettings.LanguageChanged += OnLanguageChanged;
        BindEvent();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged() => BindEvent();

    // ═══════════════════════════════════════════════════════════════
    // Event Router
    // ═══════════════════════════════════════════════════════════════

    private void BindEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        var id = state.PendingEventId;
        _menuStack.Clear();
        _breadcrumbStack.Clear();

        switch (id)
        {
            case "shrine":       Bind("Ancient Shrine", "A quiet shrine hums with forgotten energy. Will you pray for its blessing, or claim its relic?", "🏛", BuildShrineRoot()); break;
            case "brewer":       Bind("Mysterious Brew", "A bubbling cauldron sits unattended. A faded label reads: \"Drink at thy own peril.\"", "🧪", BuildBrewerRoot()); break;
            case "healer":       Bind("Wandering Healer", "A cloaked figure offers you a choice: quick relief now, or lasting vitality.", "💚", BuildHealerRoot()); break;
            case "altar":        Bind("Card Altar", "An ancient altar accepts offerings of cards. The runes suggest transformation or improvement.", "📜", BuildAltarRoot()); break;
            case "chest":        Bind("Mysterious Chest", "A heavy iron chest sits in the corner. A note warns: \"Some treasures bite back.\"", "📦", BuildChestRoot()); break;
            case "dealer":       Bind("Shady Dealer", "A hooded merchant beckons you closer. \"Special price, just for you — paid in blood.\"", "🎲", BuildDealerRoot()); break;
            case "forge":        Bind("Dark Forge", "The anvil glows with infernal heat. You could forge powerful relics here — at a cost.", "🔨", BuildForgeRoot()); break;
            case "ritual":       Bind("Blood Ritual", "A circle of dried blood marks the floor. The ritual promises permanent power.", "🩸", BuildRitualRoot()); break;
            case "gambling":     Bind("Gambling Den", "Dice rattle on a velvet table. The house always wins — but sometimes, so do you.", "🎰", BuildGamblingRoot()); break;
            case "ambush":       Bind("Bandit Ambush!", "Three cutthroats leap from the shadows! There's no escape — fight or fall.", "🗡", BuildAmbushRoot()); break;
            case "slime_pit":    Bind("Slime Pit", "The floor gives way to a nest of plague rats. Disgusting, but their alchemical glands are valuable.", "🐀", BuildSlimePitRoot()); break;
            case "cursed_idol":  Bind("Cursed Idol", "An obsidian statue oozes dark power. You can take the curse upon yourself, or smash it and fight what awakens.", "🗿", BuildCursedIdolRoot()); break;
            default:             Bind("Shady Dealer", "A dealer offers a risky bargain.", "🎲", BuildDealerRoot()); break;
        }
    }

    private void Bind(string title, string desc, string icon, List<EventOption> root)
    {
        ApplyHeader(LocalizationService.Get($"event.{GetNode<GameState>("/root/GameState").PendingEventId}.title", title),
                    LocalizationService.Get($"event.{GetNode<GameState>("/root/GameState").PendingEventId}.description", desc),
                    icon);
        PushMenu(root, breadcrumb: null);
    }

    // ═══════════════════════════════════════════════════════════════
    // Event Builders
    // ═══════════════════════════════════════════════════════════════

    private bool Zh => LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans;

    // --- Positive Events ---

    private List<EventOption> BuildShrineRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "祈祷 — 最大生命+5，回复5点生命" : "Pray — +5 Max HP, heal 5", ShrinePray),
            Opt(Zh ? "拾取遗物 — 失去8点生命，获得随机遗物…" : "Take Relic — lose 8 HP for a random relic…",
                () => PushMenu(new List<EventOption>
            {
                Opt(Zh ? "确认 — 失去8点生命，获得随机遗物" : "Confirm — lose 8 HP, gain random relic", ShrineRelic)
            }, Zh ? "拾取遗物" : "Take Relic"))
        };
    }

    private List<EventOption> BuildBrewerRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "带走药水 — 获得一瓶随机药水" : "Take the potion — gain a random potion", BrewerTake),
            Opt(Zh ? "当场喝下 — 获得2点力量" : "Drink it now — gain 2 Strength", BrewerDrink),
        };
    }

    private List<EventOption> BuildHealerRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "回复25点生命（免费）" : "Heal 25 HP (free)", HealerHeal),
            Opt(Zh ? "支付35金币 — 最大生命+8并回复8点生命" : "Pay 35 gold — gain +8 Max HP and heal 8 HP", HealerVitality),
        };
    }

    private List<EventOption> BuildAltarRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "升级牌库中一张随机卡牌" : "Upgrade a random card in your deck", AltarUpgrade),
            Opt(Zh ? "转化一张牌 — 移除一张，获得一张随机升级牌" : "Transform a card — remove one, receive a random upgraded card", AltarTransform),
        };
    }

    private List<EventOption> BuildChestRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "打开宝箱 — 获得25-50金币" : "Open the chest — gain 25-50 gold", ChestOpen),
            Opt(Zh ? "不去碰它（安全离开）" : "Leave it alone (safe)", () => FinishEvent()),
        };
    }

    // --- Risky Events ---

    private List<EventOption> BuildDealerRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "购买卡牌 — 失去6点生命，获得快速斩（可能已升级）" : "Buy Card — lose 6 HP, add Quick Slash (may be upgraded)", DealerBuy),
        };
    }

    private List<EventOption> BuildForgeRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "锻造遗物 — 失去12点生命，获得2个随机遗物" : "Forge relics — lose 12 HP, gain 2 random relics", ForgeCraft),
            Opt(Zh ? "离开熔炉" : "Leave the forge", () => FinishEvent()),
        };
    }

    private List<EventOption> BuildRitualRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "进行仪式 — 失去18点生命，本局永久获得3点力量" : "Perform ritual — lose 18 HP, gain 3 permanent Strength this act", RitualPerform),
            Opt(Zh ? "转身离开" : "Walk away", () => FinishEvent()),
        };
    }

    private List<EventOption> BuildGamblingRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "下注25金币 — 50%几率翻倍" : "Bet 25 gold — 50% chance to double it", GamblingBet),
            Opt(Zh ? "不赌" : "Don't gamble", () => FinishEvent()),
        };
    }

    // --- Combat Events ---

    private List<EventOption> BuildAmbushRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "战斗！ — 对战3个强盗，获得40金币" : "Fight! — face 3 cutthroats for 40 gold", AmbushFight),
        };
    }

    private List<EventOption> BuildSlimePitRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "采集腺体 — 对战3个瘟疫鼠，获得随机遗物" : "Harvest glands — fight 3 Plague Rats for a random relic", SlimePitFight),
            Opt(Zh ? "爬出去 — 失去5点生命逃脱" : "Climb out — lose 5 HP escaping", SlimePitEscape),
        };
    }

    private List<EventOption> BuildCursedIdolRoot()
    {
        return new List<EventOption>
        {
            Opt(Zh ? "承受诅咒 — 失去10点生命，获得稀有遗物" : "Take the curse — lose 10 HP, gain a rare relic", CursedIdolTake),
            Opt(Zh ? "砸碎神像 — 对战腐化守卫，获得稀有遗物" : "Smash the idol — fight a Corrupted Guardian for a rare relic", CursedIdolFight),
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Menu Helpers
    // ═══════════════════════════════════════════════════════════════

    private static EventOption Opt(string label, Action action, OptionVariant v = OptionVariant.Default)
        => new() { Label = label, OnSelected = action, Variant = v };

    private void ApplyHeader(string title, string description, string placeholderIcon)
    {
        _titleLabel.Text = title;
        _descLabel.Text = description;
        _placeholderIcon.Text = placeholderIcon;
        _placeholderIcon.Visible = _eventTexture.Texture == null;
        _eventTexture.Visible = _eventTexture.Texture != null;
    }

    private void PushMenu(List<EventOption> options, string? breadcrumb)
    {
        _menuStack.Add(options);
        _breadcrumbStack.Add(breadcrumb ?? string.Empty);
        RenderCurrentMenu();
    }

    private void PopMenu()
    {
        if (_menuStack.Count <= 1) return;
        _menuStack.RemoveAt(_menuStack.Count - 1);
        _breadcrumbStack.RemoveAt(_breadcrumbStack.Count - 1);
        RenderCurrentMenu();
    }

    private void RenderCurrentMenu()
    {
        foreach (var child in _optionsVBox.GetChildren()) child.QueueFree();
        var current = _menuStack[^1];
        var atRoot = _menuStack.Count == 1;

        if (atRoot) { _breadcrumbLabel.Visible = false; }
        else
        {
            var trail = new List<string>();
            for (var i = 1; i < _breadcrumbStack.Count; i++)
                if (!string.IsNullOrEmpty(_breadcrumbStack[i])) trail.Add(_breadcrumbStack[i]);
            _breadcrumbLabel.Text = string.Join("  ›  ", trail);
            _breadcrumbLabel.Visible = trail.Count > 0;
        }

        foreach (var option in current) _optionsVBox.AddChild(BuildOptionButton(option));
        _optionsVBox.AddChild(BuildOptionButton(atRoot
            ? Opt(Zh ? "✕ 离开" : "✕ Leave", LeaveEvent, OptionVariant.Leave)
            : Opt(Zh ? "← 返回" : "← Back", PopMenu, OptionVariant.Back)));
    }

    private Button BuildOptionButton(EventOption option)
    {
        var btn = new Button
        {
            Text = option.Label,
            CustomMinimumSize = new Vector2(0, 60),
            ClipText = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        btn.AddThemeFontSizeOverride("font_size", 16);
        var (normal, hover, fontColor) = OptionStyle(option.Variant);
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", normal);
        btn.AddThemeColorOverride("font_color", fontColor);
        btn.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f, 1f));
        btn.Pressed += () => option.OnSelected();
        return btn;
    }

    private static (StyleBoxFlat normal, StyleBoxFlat hover, Color font) OptionStyle(OptionVariant variant)
    {
        return variant switch
        {
            OptionVariant.Leave => (MakeStyle(0.18f, 0.12f, 0.10f, 0.95f, 0.75f, 0.45f, 0.40f, 0.85f),
                                    MakeStyle(0.30f, 0.18f, 0.14f, 1f, 1f, 0.70f, 0.55f, 1f),
                                    new Color(1f, 0.88f, 0.82f, 1f)),
            OptionVariant.Back => (MakeStyle(0.16f, 0.14f, 0.10f, 0.95f, 0.55f, 0.45f, 0.30f, 0.85f),
                                   MakeStyle(0.22f, 0.20f, 0.14f, 1f, 0.92f, 0.78f, 0.50f, 1f),
                                   new Color(0.95f, 0.92f, 0.78f, 1f)),
            _ => (MakeStyle(0.13f, 0.18f, 0.25f, 0.96f, 0.85f, 0.65f, 0.32f, 0.90f),
                  MakeStyle(0.22f, 0.30f, 0.40f, 1f, 1f, 0.92f, 0.55f, 1f),
                  new Color(1f, 0.94f, 0.78f, 1f)),
        };
    }

    private static StyleBoxFlat MakeStyle(float br, float bg, float bb, float ba, float bdr, float bdg, float bdb, float bda)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(br, bg, bb, ba),
            BorderColor = new Color(bdr, bdg, bdb, bda),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 18, ContentMarginTop = 12, ContentMarginRight = 18, ContentMarginBottom = 12
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Event Actions — Positive
    // ═══════════════════════════════════════════════════════════════

    private void ShrinePray()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.GainMaxHp(5);
        FinishEvent();
    }

    private void ShrineRelic()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 8);
        state.RollRelicOptions(1);
        if (state.PendingRelicOptions.Count > 0)
        {
            state.AddRelic(state.PendingRelicOptions[0]);
            state.PendingRelicOptions.Clear();
        }
        FinishEvent();
    }

    private void BrewerTake()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.TryAddRandomPotion(out _);
        FinishEvent();
    }

    private void BrewerDrink()
    {
        var state = GetNode<GameState>("/root/GameState");
        // Add a free Battle Focus card to represent the strength gain
        state.AddCardToDeck(state.MaybeUpgradeCardId("battle_focus"));
        FinishEvent();
    }

    private void HealerHeal()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Math.Min(state.MaxHp, state.PlayerHp + 25);
        FinishEvent();
    }

    private void HealerVitality()
    {
        var state = GetNode<GameState>("/root/GameState");
        if (state.Gold >= 35)
        {
            state.AddGold(-35);
            state.GainMaxHp(8);
            state.PlayerHp = Math.Min(state.MaxHp, state.PlayerHp + 8);
        }
        else { HealerHeal(); } // Fallback if can't afford
        FinishEvent();
    }

    private void AltarUpgrade()
    {
        var state = GetNode<GameState>("/root/GameState");
        var deck = state.DeckCardIds;
        if (deck.Count > 0)
        {
            var rng = new Random();
            var eligible = new List<string>();
            foreach (var cid in deck)
            {
                if (CardUpgradeRules.CardIdHasUpgrade(cid)) eligible.Add(cid);
            }
            if (eligible.Count > 0)
            {
                var pick = eligible[rng.Next(eligible.Count)];
                var upgraded = CardUpgradeRules.MaybeUpgrade(pick, 1.0, rng);
                state.TryRemoveCardFromDeck(pick);
                state.AddCardToDeck(upgraded);
            }
        }
        FinishEvent();
    }

    private void AltarTransform()
    {
        var state = GetNode<GameState>("/root/GameState");
        var deck = state.DeckCardIds;
        if (deck.Count > 0)
        {
            var rng = new Random();
            var pick = deck[rng.Next(deck.Count)];
            state.TryRemoveCardFromDeck(pick);
            // Add a random upgraded card from reward pool
            var pool = CardData.RewardPoolIds();
            var newCard = pool[rng.Next(pool.Count)];
            state.AddCardToDeck(CardUpgradeRules.MaybeUpgrade(newCard, 0.5, rng));
        }
        FinishEvent();
    }

    private void ChestOpen()
    {
        var state = GetNode<GameState>("/root/GameState");
        var rng = new Random();
        state.AddGold(rng.Next(25, 51));
        FinishEvent();
    }

    // ═══════════════════════════════════════════════════════════════
    // Event Actions — Risky
    // ═══════════════════════════════════════════════════════════════

    private void DealerBuy()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 6);
        state.AddCardToDeck(state.MaybeUpgradeCardId("quick_slash"));
        FinishEvent();
    }

    private void ForgeCraft()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 12);
        state.RollRelicOptions(2);
        foreach (var relic in state.PendingRelicOptions) state.AddRelic(relic);
        state.PendingRelicOptions.Clear();
        FinishEvent();
    }

    private void RitualPerform()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 18);
        // Gain 3 permanent Strength (represented as 3 upgraded Battle Focus cards)
        state.AddCardToDeck(state.MaybeUpgradeCardId("battle_focus"));
        state.AddCardToDeck(state.MaybeUpgradeCardId("battle_focus"));
        state.AddCardToDeck(state.MaybeUpgradeCardId("battle_focus"));
        FinishEvent();
    }

    private void GamblingBet()
    {
        var state = GetNode<GameState>("/root/GameState");
        if (state.Gold >= 25)
        {
            state.AddGold(-25);
            if (new Random().Next(2) == 0) state.AddGold(50); // Win: double
        }
        FinishEvent();
    }

    // ═══════════════════════════════════════════════════════════════
    // Event Actions — Combat
    // ═══════════════════════════════════════════════════════════════

    private void AmbushFight()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.BeginEncounter(MapNodeType.NormalBattle);
        state.ResolveEventFinished();
        state.SetUiPhase("battle");
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void SlimePitFight()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.BeginEncounter(MapNodeType.NormalBattle);
        state.ResolveEventFinished();
        state.SetUiPhase("battle");
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void SlimePitEscape()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 5);
        FinishEvent();
    }

    private void CursedIdolTake()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 10);
        state.RollRelicOptions(1);
        if (state.PendingRelicOptions.Count > 0)
        {
            state.AddRelic(state.PendingRelicOptions[0]);
            state.PendingRelicOptions.Clear();
        }
        FinishEvent();
    }

    private void CursedIdolFight()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.BeginEncounter(MapNodeType.EliteBattle);
        state.ResolveEventFinished();
        state.SetUiPhase("battle");
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    // ═══════════════════════════════════════════════════════════════
    // Navigation
    // ═══════════════════════════════════════════════════════════════

    private void LeaveEvent() => FinishEvent();

    private void FinishEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    // ═══════════════════════════════════════════════════════════════
    // External API
    // ═══════════════════════════════════════════════════════════════

    public EventSnapshot BuildEventSnapshot()
    {
        var state = GetNode<GameState>("/root/GameState");
        var snapshot = new EventSnapshot { EventId = state.PendingEventId };
        var opts = _menuStack.Count > 0 ? _menuStack[^1] : new List<EventOption>();
        for (var i = 0; i < opts.Count; i++)
            snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = i, Label = opts[i].Label });
        return snapshot;
    }

    public List<LegalActionSnapshot> BuildLegalActions()
    {
        var snapshot = BuildEventSnapshot();
        var actions = new List<LegalActionSnapshot>();
        foreach (var option in snapshot.Options)
            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_event_option",
                Label = option.Label,
                Parameters = new Dictionary<string, object?> { ["optionIndex"] = option.OptionIndex }
            });
        return actions;
    }

    public string? TryChooseEventOptionExternally(int? optionIndex, string? eventOption)
    {
        var opts = _menuStack.Count > 0 ? _menuStack[^1] : new List<EventOption>();
        var idx = optionIndex ?? -1;
        if (idx >= 0 && idx < opts.Count) { opts[idx].OnSelected(); return null; }
        return $"Invalid option index {idx}. Available: 0-{opts.Count - 1}.";
    }
}
