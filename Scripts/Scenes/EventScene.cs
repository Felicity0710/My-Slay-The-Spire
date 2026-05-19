using Godot;
using System;
using System.Collections.Generic;

public partial class EventScene : Control
{
    // A single button in the right-hand option column. Visual variant decides
    // styling (default/leave/back) — behaviour is whatever the action does:
    // resolve the event, push a sub-menu, or pop back to the parent menu.
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

    // Navigation stack: each level is an immutable snapshot of "what to show
    // right now". Sub-menus push, Back pops, root level == stack[0].
    private readonly List<List<EventOption>> _menuStack = new();
    private readonly List<string> _breadcrumbStack = new();

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("event");
        AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());

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

    // ---- Per-event configuration ---------------------------------------------

    private void BindEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        var id = state.PendingEventId;

        _menuStack.Clear();
        _breadcrumbStack.Clear();

        switch (id)
        {
            case "shrine":
                ApplyHeader(
                    LocalizationService.Get("event.shrine.title", "Ancient Shrine"),
                    LocalizationService.Get("event.shrine.description", "A quiet shrine hums with energy."),
                    "🏛");
                PushMenu(BuildShrineRoot(), breadcrumb: null);
                break;

            default: // dealer (and any future single-menu events)
                ApplyHeader(
                    LocalizationService.Get("event.dealer.title", "Shady Dealer"),
                    LocalizationService.Get("event.dealer.description", "A dealer offers a risky bargain."),
                    "🎲");
                PushMenu(BuildDealerRoot(), breadcrumb: null);
                break;
        }
    }

    private List<EventOption> BuildShrineRoot()
    {
        return new List<EventOption>
        {
            new()
            {
                Label = LocalizationService.Get(
                    "event.shrine.option_pray",
                    "Pray — +5 Max HP, heal 5"),
                OnSelected = ShrinePray
            },
            new()
            {
                // Multi-level demo: opens a sub-menu before committing.
                Label = LocalizationService.Get(
                    "event.shrine.option_relic_open",
                    "Take Relic — lose 8 HP for a random relic…"),
                OnSelected = () => PushMenu(BuildShrineRelicConfirm(), breadcrumb:
                    LocalizationService.Get("event.shrine.crumb_relic", "Take Relic"))
            }
        };
    }

    private List<EventOption> BuildShrineRelicConfirm()
    {
        return new List<EventOption>
        {
            new()
            {
                Label = LocalizationService.Get(
                    "event.shrine.option_relic_confirm",
                    "Confirm — lose 8 HP, gain random relic"),
                OnSelected = ShrineRelic
            }
        };
    }

    private List<EventOption> BuildDealerRoot()
    {
        return new List<EventOption>
        {
            new()
            {
                Label = LocalizationService.Get(
                    "event.dealer.option_buy",
                    "Buy Card — lose 6 HP, add Quick Slash"),
                OnSelected = DealerBuy
            }
        };
    }

    // ---- Menu rendering ------------------------------------------------------

    private void ApplyHeader(string title, string description, string placeholderIcon)
    {
        _titleLabel.Text = title;
        _descLabel.Text = description;
        // Texture art isn't shipped yet — fall back to a large emoji icon. When
        // real art lands, swap PlaceholderIcon for EventTexture by setting
        // _eventTexture.Texture.
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
        if (_menuStack.Count <= 1)
        {
            return;
        }
        _menuStack.RemoveAt(_menuStack.Count - 1);
        _breadcrumbStack.RemoveAt(_breadcrumbStack.Count - 1);
        RenderCurrentMenu();
    }

    private void RenderCurrentMenu()
    {
        foreach (var child in _optionsVBox.GetChildren())
        {
            child.QueueFree();
        }

        var current = _menuStack[^1];
        var atRoot = _menuStack.Count == 1;

        // Breadcrumb shows the navigation trail past the root, e.g. "Take Relic".
        if (atRoot)
        {
            _breadcrumbLabel.Visible = false;
        }
        else
        {
            var trail = new List<string>();
            for (var i = 1; i < _breadcrumbStack.Count; i++)
            {
                if (!string.IsNullOrEmpty(_breadcrumbStack[i]))
                {
                    trail.Add(_breadcrumbStack[i]);
                }
            }
            _breadcrumbLabel.Text = string.Join("  ›  ", trail);
            _breadcrumbLabel.Visible = trail.Count > 0;
        }

        foreach (var option in current)
        {
            _optionsVBox.AddChild(BuildOptionButton(option));
        }

        // Always append the closing button — Back for sub-menus, Leave for root.
        _optionsVBox.AddChild(BuildOptionButton(atRoot
            ? new EventOption
            {
                Label = "✕ " + LocalizationService.Get("event.option.leave", "Leave"),
                OnSelected = LeaveEvent,
                Variant = OptionVariant.Leave
            }
            : new EventOption
            {
                Label = "← " + LocalizationService.Get("event.option.back", "Back"),
                OnSelected = PopMenu,
                Variant = OptionVariant.Back
            }));
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
        switch (variant)
        {
            case OptionVariant.Leave:
                return (
                    MakeStyle(0.18f, 0.12f, 0.10f, 0.95f, 0.75f, 0.45f, 0.40f, 0.85f),
                    MakeStyle(0.30f, 0.18f, 0.14f, 1f, 1f, 0.70f, 0.55f, 1f),
                    new Color(1f, 0.88f, 0.82f, 1f));
            case OptionVariant.Back:
                return (
                    MakeStyle(0.16f, 0.14f, 0.10f, 0.95f, 0.55f, 0.45f, 0.30f, 0.85f),
                    MakeStyle(0.22f, 0.20f, 0.14f, 1f, 0.92f, 0.78f, 0.50f, 1f),
                    new Color(0.95f, 0.92f, 0.78f, 1f));
            default:
                return (
                    MakeStyle(0.13f, 0.18f, 0.25f, 0.96f, 0.85f, 0.65f, 0.32f, 0.90f),
                    MakeStyle(0.22f, 0.30f, 0.40f, 1f, 1f, 0.92f, 0.55f, 1f),
                    new Color(1f, 0.94f, 0.78f, 1f));
        }
    }

    private static StyleBoxFlat MakeStyle(
        float br, float bg, float bb, float ba,
        float bdr, float bdg, float bdb, float bda)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(br, bg, bb, ba),
            BorderColor = new Color(bdr, bdg, bdb, bda),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 18,
            ContentMarginTop = 12,
            ContentMarginRight = 18,
            ContentMarginBottom = 12
        };
    }

    // ---- Event-specific actions ---------------------------------------------

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

    private void DealerBuy()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.PlayerHp = Mathf.Max(1, state.PlayerHp - 6);
        state.AddCardToDeck(state.MaybeUpgradeCardId("quick_slash"));
        FinishEvent();
    }

    private void LeaveEvent() => FinishEvent();

    private void FinishEvent()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    // ---- External-control snapshots (unchanged contract) --------------------

    public EventSnapshot BuildEventSnapshot()
    {
        var state = GetNode<GameState>("/root/GameState");
        var snapshot = new EventSnapshot
        {
            EventId = state.PendingEventId
        };

        if (state.PendingEventId == "shrine")
        {
            snapshot.Title = "Ancient Shrine";
            snapshot.Description = "A quiet shrine hums with energy.";
            snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 0, Label = "Pray: +5 Max HP and heal 5" });
            snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 1, Label = "Take Relic: Lose 8 HP, gain random relic" });
            return snapshot;
        }

        snapshot.Title = "Shady Dealer";
        snapshot.Description = "A dealer offers a risky bargain.";
        snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 0, Label = "Buy Card: Lose 6 HP, add Quick Slash" });
        snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 1, Label = "Refuse: Gain nothing" });
        return snapshot;
    }

    public List<LegalActionSnapshot> BuildLegalActions()
    {
        var snapshot = BuildEventSnapshot();
        var actions = new List<LegalActionSnapshot>();
        foreach (var option in snapshot.Options)
        {
            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_event_option",
                Label = option.Label,
                Parameters = new Dictionary<string, object?>
                {
                    ["optionIndex"] = option.OptionIndex
                }
            });
        }

        return actions;
    }

    public string? TryChooseEventOptionExternally(int? optionIndex, string? eventOption)
    {
        var state = GetNode<GameState>("/root/GameState");
        var normalized = eventOption?.Trim().ToLowerInvariant() ?? string.Empty;
        if (state.PendingEventId == "shrine")
        {
            if (optionIndex == 0 || normalized == "pray")
            {
                ShrinePray();
                return null;
            }

            if (optionIndex == 1 || normalized == "relic")
            {
                ShrineRelic();
                return null;
            }

            return $"Unsupported shrine option '{eventOption ?? optionIndex?.ToString() ?? string.Empty}'.";
        }

        if (optionIndex == 0 || normalized == "buy")
        {
            DealerBuy();
            return null;
        }

        if (optionIndex == 1 || normalized == "leave" || normalized == "refuse")
        {
            LeaveEvent();
            return null;
        }

        return $"Unsupported event option '{eventOption ?? optionIndex?.ToString() ?? string.Empty}'.";
    }
}
