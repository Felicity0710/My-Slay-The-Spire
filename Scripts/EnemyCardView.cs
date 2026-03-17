using Godot;

public partial class EnemyCardView : Button
{
    private Label _intentLabel = null!;
    private ColorRect _portraitBg = null!;
    private TextureRect _portrait = null!;
    private ColorRect _targetGlow = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
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
    }

    private void CacheNodes()
    {
        if (_intentLabel != null)
        {
            return;
        }

        _intentLabel = GetNode<Label>("Margin/VBox/IntentRow/IntentLabel");
        _portraitBg = GetNode<ColorRect>("Margin/VBox/PortraitBg");
        _portrait = GetNode<TextureRect>("Margin/VBox/PortraitBg/Portrait");
        _targetGlow = GetNode<ColorRect>("Margin/VBox/PortraitBg/TargetGlow");
        _hpLabel = GetNode<Label>("Margin/VBox/HpLabel");
        _hpBar = GetNode<ProgressBar>("Margin/VBox/HpBar");
        _nameLabel = GetNode<Label>("Margin/VBox/NameLabel");
        _traitLabel = GetNode<Label>("Margin/VBox/TraitLabel");
        _statusRow = GetNode<HBoxContainer>("Margin/VBox/StatusRow");
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
        bool inputLocked)
    {
        CacheNodes();

        Disabled = !enemy.IsAlive || inputLocked;

        var traitSummary = CombatVisualCatalog.GetEnemyTraitSummary(enemy.ArchetypeId);
        var traitAccent = CombatVisualCatalog.GetEnemyTraitAccent(enemy.ArchetypeId);

        var cardStyle = new StyleBoxFlat
        {
            BgColor = enemy.IsAlive
                ? (isHovered ? new Color("1a2d2a") : (isSelected ? new Color("1a2b3b") : new Color("18212b")))
                : new Color("111827"),
            BorderColor = !enemy.IsAlive
                ? new Color("374151")
                : isHovered
                    ? traitAccent.Lightened(0.12f)
                    : isSelected
                        ? traitAccent
                        : isTargetable
                            ? traitAccent.Darkened(0.2f)
                            : new Color("4b5563"),
            BorderWidthLeft = isHovered || isSelected ? 3 : 2,
            BorderWidthTop = isHovered || isSelected ? 3 : 2,
            BorderWidthRight = isHovered || isSelected ? 3 : 2,
            BorderWidthBottom = isHovered || isSelected ? 3 : 2,
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
        Scale = new Vector2(scaleMul, scaleMul);
        var elevation = isSelected ? -7f : (isHovered ? -3f : 0f);
        Position = new Vector2(Position.X, elevation);

        _intentLabel.Text = enemy.IsAlive ? intentCompactText : "KO";
        _intentLabel.Modulate = enemy.IsAlive ? intentTint : new Color("6b7280");
        _intentLabel.TooltipText = enemy.IsAlive ? intentTooltip : "Defeated";

        TooltipText = enemy.IsAlive
            ? $"{enemy.Name}\n{traitSummary}\n下一步意图：{intentTooltip}"
            : $"{enemy.Name}\nDefeated";

        _portraitBg.Color = new Color(stageTint.R, stageTint.G, stageTint.B, enemy.IsAlive ? 1f : 0.45f);
        _portrait.Texture = portraitTexture;

        var glowAlpha = !enemy.IsAlive
            ? 0f
            : isHovered
                ? 0.18f
                : isSelected
                    ? 0.24f
                    : isTargetable
                        ? 0.1f
                        : 0f;
        _targetGlow.Color = new Color(traitAccent.R, traitAccent.G, traitAccent.B, glowAlpha);

        _hpLabel.Text = $"{enemy.Hp}/{enemy.MaxHp}";
        _hpBar.MaxValue = Mathf.Max(enemy.MaxHp, 1);
        _hpBar.Value = Mathf.Max(enemy.Hp, 0);

        _nameLabel.Text = enemy.Name;
        _nameLabel.Modulate = enemy.IsAlive ? Colors.White : new Color("6b7280");

        _traitLabel.Text = traitSummary;
        _traitLabel.Modulate = enemy.IsAlive ? traitAccent.Lightened(0.25f) : new Color("6b7280");
        _traitLabel.TooltipText = traitSummary;

        foreach (Node child in _statusRow.GetChildren())
        {
            child.QueueFree();
        }

        _statusRow.AddChild(CreateStatusChip(
            $"BLK {enemy.Block}",
            "Block: absorbs incoming damage.",
            new Color("93c5fd"),
            !enemy.IsAlive || enemy.Block <= 0));
        _statusRow.AddChild(CreateStatusChip(
            $"STR {enemy.Strength}",
            "Strength: increases attack damage.",
            new Color("fca5a5"),
            !enemy.IsAlive || enemy.Strength <= 0));
        _statusRow.AddChild(CreateStatusChip(
            $"VUL {enemy.Vulnerable}",
            "Vulnerable: takes 50% more damage.",
            new Color("d8b4fe"),
            !enemy.IsAlive || enemy.Vulnerable <= 0));
    }

    public Control EffectTarget() => _portraitBg;

    private static PanelContainer CreateStatusChip(string text, string tooltip, Color accent, bool muted)
    {
        var chip = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = tooltip,
            Modulate = muted ? new Color(1f, 1f, 1f, 0.7f) : Colors.White
        };

        var style = new StyleBoxFlat
        {
            BgColor = muted ? new Color("1f2937") : new Color(accent.R * 0.25f, accent.G * 0.25f, accent.B * 0.25f, 1f),
            BorderColor = muted ? new Color("4b5563") : accent,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5
        };
        chip.AddThemeStyleboxOverride("panel", style);

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = muted ? new Color("9ca3af") : Colors.White
        };
        chip.AddChild(label);

        return chip;
    }
}
