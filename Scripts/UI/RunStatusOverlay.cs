using Godot;

// Floating status bar that mirrors the MapScene top status panel — used in
// Shop / Event / Rest / Reward scenes so the player can always see HP / gold /
// deck / relics / potions, AND left-click a potion slot to open a Discard
// popup (the only way to make room when the belt is full and a new potion
// reward shows up).
public partial class RunStatusOverlay : CanvasLayer
{
    // The slot popup offers different actions depending on what scene is
    // hosting the overlay. Outside battle the only sensible action is Discard
    // (free a slot to take a reward potion). Inside battle the player wants
    // to Drink. Callers set `PotionAction` accordingly.
    public enum PotionSlotAction { Discard, Drink }

    public PotionSlotAction PotionAction { get; set; } = PotionSlotAction.Discard;

    // Battle scene uses this to lock the slot popup during animations and on
    // the enemy's turn. When the callback returns false the popup is not
    // shown at all — the click is silently ignored. Default = always allowed.
    public System.Func<bool>? CanOpenSlotMenu;

    // Fired whenever a potion is consumed via the overlay popup. Reward / battle
    // hosts use this to repaint anything dependent on the inventory (e.g. the
    // reward scene's potion options become clickable when a slot frees up; the
    // battle scene needs to apply the drunk potion's effect).
    public System.Action<int, string> PotionConsumed = (_, _) => { };

    private Label _runInfoLabel = null!;
    private Label _relicLabel = null!;
    private HBoxContainer _relicIcons = null!;
    private Label _potionLabel = null!;
    private HBoxContainer _potionSlots = null!;

    // Slot popup — built lazily, anchored to the clicked slot.
    private PopupPanel? _slotPopup;
    private Button? _slotPopupButton;
    private int _popupSlotIndex = -1;

    public override void _Ready()
    {
        _runInfoLabel = GetNode<Label>("%RunInfoLabel");
        _relicLabel = GetNode<Label>("%RelicLabel");
        _relicIcons = GetNode<HBoxContainer>("%RelicIcons");
        _potionLabel = GetNode<Label>("%PotionLabel");
        _potionSlots = GetNode<HBoxContainer>("%PotionSlots");

        Refresh();

        LocalizationSettings.LanguageChanged += Refresh;
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        var state = GetNodeOrNull<GameState>("/root/GameState");
        if (state == null)
        {
            return;
        }

        _runInfoLabel.Text = string.Join("    ", new[]
        {
            "🗺 " + LocalizationService.Format("ui.map.act", "Act {0}/{1}", state.Act, MapProgressionRules.MaxActs),
            "🪜 " + LocalizationService.Format("ui.map.floor", "Floor {0}", state.Floor),
            "❤ " + LocalizationService.Format("ui.map.hp", "HP {0}/{1}", state.PlayerHp, state.MaxHp),
            "💰 " + LocalizationService.Format("ui.map.gold", "Gold {0}", state.Gold),
            "🎴 " + LocalizationService.Format("ui.map.deck", "Deck {0}", state.DeckCardIds.Count),
            "🏆 " + LocalizationService.Format("ui.map.wins", "Wins {0}", state.BattlesWon)
        });

        _relicLabel.Text = LocalizationService.Get("ui.map.relics_prefix", "💠 Relics:");
        _potionLabel.Text = LocalizationService.Get("ui.map.potions_prefix", "🧪 Potions:");

        RefreshRelicIcons(state);
        RefreshPotionSlots(state);
    }

    private void RefreshRelicIcons(GameState state)
    {
        foreach (var child in _relicIcons.GetChildren())
        {
            child.QueueFree();
        }

        if (state.RelicIds.Count == 0)
        {
            var none = new Label
            {
                Text = LocalizationService.Get("ui.map.relics_none", "None"),
                Modulate = new Color(1f, 1f, 1f, 0.55f)
            };
            none.AddThemeFontSizeOverride("font_size", 13);
            _relicIcons.AddChild(none);
            return;
        }

        foreach (var relicId in state.RelicIds)
        {
            var relic = RelicData.CreateById(relicId);
            var iconPath = CombatVisualCatalog.GetRelicIconPath(relicId);
            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(24, 24),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                TooltipText = relic.ToRelicText(),
                MouseFilter = Control.MouseFilterEnum.Stop,
                Texture = ResourceLoader.Exists(iconPath) ? GD.Load<Texture2D>(iconPath) : null
            };
            _relicIcons.AddChild(icon);
        }
    }

    private void RefreshPotionSlots(GameState state)
    {
        foreach (var child in _potionSlots.GetChildren())
        {
            child.QueueFree();
        }

        for (var i = 0; i < GameState.PotionInventoryCapacity; i++)
        {
            string? potionId = i < state.PotionIds.Count ? state.PotionIds[i] : null;
            _potionSlots.AddChild(BuildClickableSlot(potionId, i));
        }
    }

    // Each slot is a Button that mimics MapScene's display slot visually, but
    // a click opens a Discard popup when the slot is filled. Empty slots are
    // disabled — nothing to discard.
    private Button BuildClickableSlot(string? potionId, int slotIndex)
    {
        var filled = !string.IsNullOrEmpty(potionId);
        var (glyph, accent) = filled
            ? PotionVisualForId(potionId!)
            : ("·", new Color(0.55f, 0.55f, 0.55f, 0.55f));

        var btn = new Button
        {
            CustomMinimumSize = new Vector2(40, 40),
            ClipText = true,
            FocusMode = Control.FocusModeEnum.None,
            Disabled = !filled,
            Text = glyph
        };
        btn.AddThemeFontSizeOverride("font_size", filled ? 22 : 18);
        btn.AddThemeColorOverride("font_color", filled ? Colors.White : new Color(1f, 1f, 1f, 0.45f));
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
        btn.AddThemeColorOverride("font_disabled_color", new Color(1f, 1f, 1f, 0.45f));

        var normal = BuildSlotStyle(accent, filled, hovered: false);
        var hover = BuildSlotStyle(accent, filled, hovered: true);
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", normal);
        btn.AddThemeStyleboxOverride("disabled", normal);
        btn.AddThemeStyleboxOverride("focus", normal);

        if (filled)
        {
            var potion = PotionData.CreateById(potionId!);
            var name = LocalizationService.Get($"potion.{potion.Id}.name", potion.Name);
            var desc = LocalizationService.Get($"potion.{potion.Id}.description", potion.Description);
            btn.TooltipText = $"{name}\n{desc}\n\n" + LocalizationService.Get(
                "ui.potion_slot.click_hint",
                "(Click to discard)");
            var capturedIndex = slotIndex;
            btn.Pressed += () => OpenDiscardPopup(btn, capturedIndex);
        }
        else
        {
            btn.TooltipText = LocalizationService.Get("ui.map.potion_slot_empty", "Empty slot");
        }

        return btn;
    }

    private static StyleBoxFlat BuildSlotStyle(Color accent, bool filled, bool hovered)
    {
        return new StyleBoxFlat
        {
            BgColor = filled
                ? new Color(accent.R * (hovered ? 0.32f : 0.20f), accent.G * (hovered ? 0.32f : 0.20f),
                    accent.B * (hovered ? 0.32f : 0.20f), 0.95f)
                : new Color(0.10f, 0.10f, 0.10f, 0.55f),
            BorderColor = filled
                ? new Color(accent.R, accent.G, accent.B, hovered ? 1f : 0.85f)
                : new Color(0.45f, 0.42f, 0.30f, 0.55f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0,
            ContentMarginRight = 0
        };
    }

    private static (string Glyph, Color Accent) PotionVisualForId(string potionId)
    {
        return potionId switch
        {
            "healing_potion" => ("❤", new Color("ef4444")),
            "strength_potion" => ("💪", new Color("fca5a5")),
            "swift_potion" => ("⚡", new Color("fde047")),
            "guard_potion" => ("🛡", new Color("93c5fd")),
            "fury_potion" => ("🔥", new Color("fb923c")),
            _ => ("🧪", new Color("a78bfa"))
        };
    }

    // Pop a small action menu just below the clicked slot. The action label
    // is "Drink" inside battle and "Discard" elsewhere — set by the host scene
    // via PotionAction.
    private void OpenDiscardPopup(Button anchorButton, int slotIndex)
    {
        // Allow the host scene to veto opening (e.g. battle while input locked
        // or on enemy turn). Default is unrestricted.
        if (CanOpenSlotMenu != null && !CanOpenSlotMenu())
        {
            return;
        }

        _popupSlotIndex = slotIndex;

        EnsureSlotPopup();
        if (_slotPopup == null || _slotPopupButton == null)
        {
            return;
        }

        // Refresh the button label every time so language and action mode are
        // always current.
        var labelKey = PotionAction == PotionSlotAction.Drink
            ? "ui.potion_slot.drink"
            : "ui.potion_slot.discard";
        var fallback = PotionAction == PotionSlotAction.Drink ? "Drink" : "Discard";
        var icon = PotionAction == PotionSlotAction.Drink ? "🧪 " : "✕ ";
        _slotPopupButton.Text = icon + LocalizationService.Get(labelKey, fallback);

        var rect = anchorButton.GetGlobalRect();
        _slotPopup.PopupOnParent(new Rect2I(
            (int)(rect.Position.X),
            (int)(rect.Position.Y + rect.Size.Y + 4),
            (int)Mathf.Max(rect.Size.X, 140),
            36));
    }

    private void EnsureSlotPopup()
    {
        if (_slotPopup != null && IsInstanceValid(_slotPopup))
        {
            return;
        }

        _slotPopup = new PopupPanel
        {
            Transient = true
        };
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.05f, 0.98f),
            BorderColor = new Color(0.85f, 0.65f, 0.32f, 0.95f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ShadowColor = new Color(0, 0, 0, 0.6f),
            ShadowSize = 6,
            ContentMarginLeft = 6,
            ContentMarginTop = 4,
            ContentMarginRight = 6,
            ContentMarginBottom = 4
        };
        _slotPopup.AddThemeStyleboxOverride("panel", bg);

        _slotPopupButton = new Button
        {
            CustomMinimumSize = new Vector2(140, 32)
        };
        _slotPopupButton.AddThemeFontSizeOverride("font_size", 15);
        _slotPopupButton.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.78f, 1f));
        _slotPopupButton.AddThemeColorOverride("font_hover_color", Colors.White);
        var btnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.12f, 0.10f, 1f),
            BorderColor = new Color(0.85f, 0.45f, 0.40f, 0.85f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12,
            ContentMarginRight = 12
        };
        _slotPopupButton.AddThemeStyleboxOverride("normal", btnStyle);
        _slotPopupButton.AddThemeStyleboxOverride("hover", btnStyle);
        _slotPopupButton.Pressed += OnConfirmSlotAction;

        _slotPopup.AddChild(_slotPopupButton);
        AddChild(_slotPopup);
    }

    private void OnConfirmSlotAction()
    {
        var state = GetNodeOrNull<GameState>("/root/GameState");
        var slotIndex = _popupSlotIndex;
        if (state == null || slotIndex < 0 || slotIndex >= state.PotionIds.Count)
        {
            _slotPopup?.Hide();
            return;
        }

        if (!state.TryConsumePotionAt(slotIndex, out var consumed))
        {
            _slotPopup?.Hide();
            return;
        }

        _popupSlotIndex = -1;
        _slotPopup?.Hide();
        Refresh();
        // Host scene (e.g. BattleScene applies the effect; RewardScene rebuilds
        // disabled potion options) reacts via this callback. Fire AFTER refresh
        // so the host sees a consistent state.
        PotionConsumed.Invoke(slotIndex, consumed.Id);
    }
}
