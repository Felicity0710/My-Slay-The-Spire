using Godot;

public partial class PlayerCardView : Control
{
    private Label _nameLabel = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private HBoxContainer _statusRow = null!;
    private Label _summaryLabel = null!;
    private ColorRect _portraitGlow = null!;

    public override void _Ready()
    {
        CacheNodes();
    }

    public void Configure(PlayerUnit player, bool inputLocked)
    {
        CacheNodes();

        _nameLabel.Text = player.Name;
        _hpLabel.Text = $"HP {player.Hp}/{player.MaxHp}";
        _hpBar.MaxValue = Mathf.Max(player.MaxHp, 1);
        _hpBar.Value = Mathf.Max(player.Hp, 0);
        _summaryLabel.Text = $"Block {player.Block} · STR {player.Strength} · VUL {player.Vulnerable}";
        _portraitGlow.Color = inputLocked
            ? new Color(0.4f, 0.55f, 0.75f, 0.2f)
            : new Color(0.54f, 0.8f, 1f, 0.3f);

        RebuildStatusChips(player);
    }

    public Control EffectTarget() => _portraitGlow;

    private void RebuildStatusChips(PlayerUnit player)
    {
        foreach (Node child in _statusRow.GetChildren())
        {
            child.QueueFree();
        }

        _statusRow.AddChild(CreateStatusChip(
            $"BLK {player.Block}",
            "Block: absorbs incoming damage.",
            new Color("93c5fd"),
            player.Block <= 0));

        _statusRow.AddChild(CreateStatusChip(
            $"STR {player.Strength}",
            "Strength: increases attack damage.",
            new Color("fca5a5"),
            player.Strength <= 0));

        _statusRow.AddChild(CreateStatusChip(
            $"VUL {player.Vulnerable}",
            "Vulnerable: takes 50% more damage.",
            new Color("d8b4fe"),
            player.Vulnerable <= 0));
    }

    private void CacheNodes()
    {
        if (_nameLabel != null)
        {
            return;
        }

        _nameLabel = GetNode<Label>("Margin/VBox/NameLabel");
        _hpLabel = GetNode<Label>("Margin/VBox/HpLabel");
        _hpBar = GetNode<ProgressBar>("Margin/VBox/HpBar");
        _statusRow = GetNode<HBoxContainer>("Margin/VBox/StatusRow");
        _summaryLabel = GetNode<Label>("Margin/VBox/SummaryLabel");
        _portraitGlow = GetNode<ColorRect>("Margin/VBox/PortraitBg/PortraitGlow");
    }

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
