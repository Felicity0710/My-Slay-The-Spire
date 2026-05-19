using Godot;

public partial class EnemyCardView : Button
{
    private PanelContainer _intentBadge = null!;
    private Label _intentIconLabel = null!;
    private Label _intentLabel = null!;
    private Control _hpBarContainer = null!;
    private ProgressBar _hpBar = null!;
    private Label _hpLabel = null!;
    private ColorRect _portraitBg = null!;
    private TextureRect _portrait = null!;
    private ColorRect _targetGlow = null!;
    private Label _nameLabel = null!;
    private Label _traitLabel = null!;
    private HBoxContainer _statusRow = null!;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.None;
        Flat = true;
        Text = string.Empty;
        ClipText = true;

        CacheNodes();

        // Configure() sets PivotOffset = Size * 0.5f, but on the FIRST call
        // (right after AddChild) the grid layout hasn't run yet and Size is
        // still (0,0). That leaves the pivot at the top-left corner, so any
        // selected/hovered scale-up visibly drifts the card right + down
        // instead of pulsing in place. Sync the pivot whenever the layout
        // resizes us — keeps scale-around-center stable forever after.
        Resized += () => PivotOffset = Size * 0.5f;
    }

    private void CacheNodes()
    {
        if (_intentLabel != null)
        {
            return;
        }

        _intentBadge = GetNode<PanelContainer>("Margin/VBox/IntentBadge");
        _intentIconLabel = GetNode<Label>("Margin/VBox/IntentBadge/IntentHBox/IntentIconLabel");
        _intentLabel = GetNode<Label>("Margin/VBox/IntentBadge/IntentHBox/IntentLabel");
        _hpBarContainer = GetNode<Control>("Margin/VBox/HpBarContainer");
        _hpBar = GetNode<ProgressBar>("Margin/VBox/HpBarContainer/HpBar");
        _hpLabel = GetNode<Label>("Margin/VBox/HpBarContainer/HpLabel");
        _portraitBg = GetNode<ColorRect>("Margin/VBox/PortraitBg");
        _portrait = GetNode<TextureRect>("Margin/VBox/PortraitBg/Portrait");
        _targetGlow = GetNode<ColorRect>("Margin/VBox/PortraitBg/TargetGlow");
        _nameLabel = GetNode<Label>("Margin/VBox/NameLabel");
        _traitLabel = GetNode<Label>("Margin/VBox/TraitLabel");
        _statusRow = GetNode<HBoxContainer>("Margin/VBox/StatusRow");

        // Forward hover to the Button so its TooltipText fires when the player
        // mouses over the inner content. Without this, child controls default
        // to MouseFilter=Stop and swallow hover, suppressing the tooltip.
        foreach (var child in new Control[] { GetNode<Control>("Margin"), GetNode<Control>("Margin/VBox"),
            _intentBadge, _intentIconLabel, _intentLabel, _hpBarContainer, _hpBar, _hpLabel,
            _portraitBg, _portrait, _statusRow })
        {
            child.MouseFilter = MouseFilterEnum.Pass;
        }
    }

    public void Configure(
        EnemyUnit enemy,
        string intentCompactText,
        string intentTooltip,
        Color intentTint,
        Texture2D portraitTexture,
        Color stageTint,
        bool isSelected,
        bool isHovered,
        bool isTargetable,
        bool inputLocked,
        float rosterScale = 1f)
    {
        CacheNodes();

        Disabled = !enemy.IsAlive || inputLocked;

        var localizedEnemyName = CombatVisualCatalog.GetLocalizedEnemyName(enemy.ArchetypeId, enemy.Name);
        var traitSummary = CombatVisualCatalog.GetEnemyTraitSummary(enemy.ArchetypeId);
        var traitAccent = CombatVisualCatalog.GetEnemyTraitAccent(enemy.ArchetypeId);

        // Compact slate-grey card shell. The card itself shouldn't compete with
        // the portrait / intent badge for attention.
        var cardStyle = new StyleBoxFlat
        {
            BgColor = enemy.IsAlive
                ? (isHovered ? new Color("1a2d2a") : (isSelected ? new Color("1a2b3b") : new Color("141a22")))
                : new Color("0f1318"),
            BorderColor = !enemy.IsAlive
                ? new Color("374151")
                : isHovered
                    ? traitAccent.Lightened(0.12f)
                    : isSelected
                        ? traitAccent
                        : isTargetable
                            ? traitAccent.Darkened(0.2f)
                            : new Color("2a3340"),
            BorderWidthLeft = isHovered || isSelected ? 3 : 1,
            BorderWidthTop = isHovered || isSelected ? 3 : 1,
            BorderWidthRight = isHovered || isSelected ? 3 : 1,
            BorderWidthBottom = isHovered || isSelected ? 3 : 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        AddThemeStyleboxOverride("normal", cardStyle);
        AddThemeStyleboxOverride("pressed", cardStyle);
        AddThemeStyleboxOverride("hover", cardStyle);

        PivotOffset = Size * 0.5f;
        var scaleMul = isSelected ? 1.035f : (isHovered ? 1.015f : 1f);
        var finalScale = scaleMul * rosterScale;
        Scale = new Vector2(finalScale, finalScale);
        var elevation = isSelected ? -7f : (isHovered ? -3f : 0f);
        Position = new Vector2(Position.X, elevation);

        ConfigureIntent(enemy, intentCompactText, intentTooltip, intentTint);
        ConfigureHpBar(enemy);

        _portraitBg.Color = new Color(stageTint.R, stageTint.G, stageTint.B, enemy.IsAlive ? 1f : 0.45f);
        _portrait.Texture = portraitTexture;

        var glowAlpha = !enemy.IsAlive
            ? 0f
            : isHovered ? 0.18f : isSelected ? 0.24f : isTargetable ? 0.10f : 0f;
        _targetGlow.Color = new Color(traitAccent.R, traitAccent.G, traitAccent.B, glowAlpha);

        TooltipText = enemy.IsAlive
            ? $"{localizedEnemyName}\n{traitSummary}\n{LocalizationService.Get("ui.battle.next_intent_prefix", "Next intent: ")}{intentTooltip}"
            : $"{localizedEnemyName}\n{LocalizationService.Get("ui.status.defeated", "Defeated")}";

        RebuildStatusRow(enemy);
    }

    private void ConfigureIntent(EnemyUnit enemy, string compactText, string tooltip, Color tint)
    {
        if (!enemy.IsAlive)
        {
            _intentBadge.Visible = false;
            return;
        }

        _intentBadge.Visible = true;
        _intentIconLabel.Text = IntentIconGlyph(enemy.IntentType);
        _intentLabel.Text = IntentValueText(compactText);
        _intentIconLabel.Modulate = tint;
        _intentLabel.Modulate = tint;
        _intentBadge.TooltipText = tooltip;

        // Subtle dark backdrop in the intent's accent color.
        var badgeStyle = new StyleBoxFlat
        {
            BgColor = new Color(tint.R * 0.18f, tint.G * 0.18f, tint.B * 0.18f, 0.95f),
            BorderColor = new Color(tint.R, tint.G, tint.B, 0.85f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 8,
            ContentMarginTop = 2,
            ContentMarginRight = 8,
            ContentMarginBottom = 2
        };
        _intentBadge.AddThemeStyleboxOverride("panel", badgeStyle);
    }

    // Strip the "ATK " / "BLK " / "STR +" prefixes — the icon already conveys
    // the kind; the label only needs to show the magnitude.
    private static string IntentValueText(string compactText)
    {
        if (string.IsNullOrEmpty(compactText) || compactText == "-")
        {
            return "?";
        }
        var idx = compactText.LastIndexOf(' ');
        return idx >= 0 && idx < compactText.Length - 1
            ? compactText[(idx + 1)..]
            : compactText;
    }

    private static string IntentIconGlyph(EnemyIntentType type)
    {
        return type switch
        {
            EnemyIntentType.Attack => "⚔",
            EnemyIntentType.Defend => "🛡",
            EnemyIntentType.Buff => "💪",
            _ => "❓"
        };
    }

    private void ConfigureHpBar(EnemyUnit enemy)
    {
        _hpBar.MaxValue = Mathf.Max(enemy.MaxHp, 1);
        _hpBar.Value = Mathf.Max(enemy.Hp, 0);
        _hpLabel.Text = $"{enemy.Hp} / {enemy.MaxHp}";
        _hpLabel.Modulate = enemy.IsAlive ? Colors.White : new Color(1f, 1f, 1f, 0.55f);

        // Style the HP bar with a deep red fill / dark grey track.
        var trackStyle = new StyleBoxFlat
        {
            BgColor = new Color("1f2937"),
            BorderColor = new Color("111827"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var fillStyle = new StyleBoxFlat
        {
            BgColor = enemy.IsAlive ? new Color("dc2626") : new Color("4b5563"),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _hpBar.AddThemeStyleboxOverride("background", trackStyle);
        _hpBar.AddThemeStyleboxOverride("fill", fillStyle);
    }

    public Control EffectTarget() => _portraitBg;

    private void RebuildStatusRow(EnemyUnit enemy)
    {
        foreach (Node child in _statusRow.GetChildren())
        {
            child.QueueFree();
        }

        if (!enemy.IsAlive)
        {
            return;
        }

        if (enemy.Block > 0)
        {
            _statusRow.AddChild(StatusChip("🛡", enemy.Block, new Color("93c5fd"),
                LocalizationService.Get("ui.battle.status_tooltip.block", "Block absorbs incoming damage.")));
        }
        if (enemy.Strength > 0)
        {
            _statusRow.AddChild(StatusChip("💪", enemy.Strength, new Color("fca5a5"),
                LocalizationService.Get("ui.battle.status_tooltip.strength", "Strength increases attack damage.")));
        }
        if (enemy.Vulnerable > 0)
        {
            _statusRow.AddChild(StatusChip("🩸", enemy.Vulnerable, new Color("d8b4fe"),
                LocalizationService.Get("ui.battle.status_tooltip.vulnerable", "Vulnerable increases damage taken by 50%.")));
        }
    }

    public static PanelContainer StatusChip(string icon, int stacks, Color accent, string tooltip)
    {
        var chip = new PanelContainer
        {
            TooltipText = $"{tooltip} ({stacks})",
            MouseFilter = MouseFilterEnum.Pass,
            CustomMinimumSize = new Vector2(36, 22)
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(accent.R * 0.20f, accent.G * 0.20f, accent.B * 0.20f, 0.95f),
            BorderColor = accent,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ContentMarginLeft = 5,
            ContentMarginTop = 1,
            ContentMarginRight = 5,
            ContentMarginBottom = 1
        };
        chip.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        hbox.AddThemeConstantOverride("separation", 2);
        chip.AddChild(hbox);

        var iconLabel = new Label
        {
            Text = icon,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Pass
        };
        iconLabel.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(iconLabel);

        var stackLabel = new Label
        {
            Text = stacks.ToString(),
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Pass
        };
        stackLabel.AddThemeFontSizeOverride("font_size", 13);
        stackLabel.AddThemeColorOverride("font_color", Colors.White);
        hbox.AddChild(stackLabel);

        return chip;
    }
}
