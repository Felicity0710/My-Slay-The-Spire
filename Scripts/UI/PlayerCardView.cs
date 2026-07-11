using Godot;

public partial class PlayerCardView : Control
{
    private Label _nameLabel = null!;
    private Control _hpBarContainer = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private ColorRect _portraitBg = null!;
    private ColorRect _portraitGlow = null!;
    private HBoxContainer _statusRow = null!;
    private Label _summaryLabel = null!;

    public override void _Ready()
    {
        CacheNodes();
    }

    public void Configure(PlayerUnit player, bool inputLocked)
    {
        CacheNodes();

        _nameLabel.Visible = false;
        _summaryLabel.Visible = false;

        ConfigureHpBar(player);

        // Remove portrait background — character silhouette only
        _portraitBg.Color = new Color(0, 0, 0, 0);
        _portraitGlow.Color = new Color(0, 0, 0, 0);

        RebuildStatusRow(player);
    }

    public Control EffectTarget() => _portraitBg;

    private void ConfigureHpBar(PlayerUnit player)
    {
        _hpBar.MaxValue = Mathf.Max(player.MaxHp, 1);
        _hpBar.Value = Mathf.Max(player.Hp, 0);
        _hpLabel.Text = $"{player.Hp} / {player.MaxHp}";

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
            BgColor = new Color("16a34a"),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _hpBar.AddThemeStyleboxOverride("background", trackStyle);
        _hpBar.AddThemeStyleboxOverride("fill", fillStyle);
    }

    private void RebuildStatusRow(PlayerUnit player)
    {
        foreach (Node child in _statusRow.GetChildren())
        {
            child.QueueFree();
        }

        if (player.Block > 0)
        {
            _statusRow.AddChild(EnemyCardView.StatusChip("🛡", player.Block, new Color("93c5fd"),
                LocalizationService.Get("ui.battle.status_tooltip.block", "Block absorbs incoming damage.")));
        }
        if (player.Strength > 0)
        {
            _statusRow.AddChild(EnemyCardView.StatusChip("💪", player.Strength, new Color("fca5a5"),
                LocalizationService.Get("ui.battle.status_tooltip.strength", "Strength increases attack damage.")));
        }
        if (player.Vulnerable > 0)
        {
            _statusRow.AddChild(EnemyCardView.StatusChip("🩸", player.Vulnerable, new Color("d8b4fe"),
                LocalizationService.Get("ui.battle.status_tooltip.vulnerable", "Vulnerable increases damage taken by 50%.")));
        }
    }

    private TextureRect? _portraitTexture;

    public void SetPortrait(string path)
    {
        CacheNodes();
        // Always make the background transparent
        _portraitBg.Color = new Color(0, 0, 0, 0);
        _portraitGlow.Color = new Color(0, 0, 0, 0);

        if (_portraitTexture == null)
        {
            _portraitTexture = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            _portraitTexture.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _portraitBg.AddChild(_portraitTexture);
        }

        if (ResourceLoader.Exists(path))
        {
            _portraitTexture.Texture = GD.Load<Texture2D>(path);
            _portraitTexture.Visible = true;
        }
    }

    private void CacheNodes()
    {
        if (_hpLabel != null)
        {
            return;
        }

        _nameLabel = GetNode<Label>("Margin/VBox/NameLabel");
        _hpBarContainer = GetNode<Control>("Margin/VBox/HpBarContainer");
        _hpBar = GetNode<ProgressBar>("Margin/VBox/HpBarContainer/HpBar");
        _hpLabel = GetNode<Label>("Margin/VBox/HpBarContainer/HpLabel");
        _portraitBg = GetNode<ColorRect>("Margin/VBox/PortraitBg");
        _portraitGlow = GetNode<ColorRect>("Margin/VBox/PortraitBg/PortraitGlow");
        _statusRow = GetNode<HBoxContainer>("Margin/VBox/StatusRow");
        _summaryLabel = GetNode<Label>("Margin/VBox/SummaryLabel");
    }
}
