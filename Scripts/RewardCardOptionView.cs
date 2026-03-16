using Godot;

public partial class RewardCardOptionView : PanelContainer
{
    private Label? _nameLabel;
    private Label? _costLabel;
    private RichTextLabel? _descLabel;

    public string CardId { get; private set; } = string.Empty;

    public override void _Ready()
    {
        EnsureNodes();

        var normalStyle = new StyleBoxFlat
        {
            BgColor = new Color("18212b"),
            BorderColor = new Color("7aa8cf"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.45f),
            ShadowSize = 5
        };

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color("1c2936"),
            BorderColor = new Color("93c5fd"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.55f),
            ShadowSize = 7
        };

        AddThemeStyleboxOverride("panel", normalStyle);
        _costLabel!.AddThemeColorOverride("font_color", new Color("93c5fd"));
        _descLabel!.AddThemeColorOverride("default_color", new Color("cbd5e1"));

        MouseEntered += () =>
        {
            AddThemeStyleboxOverride("panel", hoverStyle);
            Scale = new Vector2(1.04f, 1.04f);
        };

        MouseExited += () =>
        {
            AddThemeStyleboxOverride("panel", normalStyle);
            Scale = Vector2.One;
        };
    }

    private void EnsureNodes()
    {
        if (_nameLabel != null && _costLabel != null && _descLabel != null)
        {
            return;
        }

        _nameLabel = GetNode<Label>("Margin/VBox/NameLabel");
        _costLabel = GetNode<Label>("Margin/VBox/CostLabel");
        _descLabel = GetNode<RichTextLabel>("Margin/VBox/DescLabel");
    }

    public void Setup(CardData card)
    {
        EnsureNodes();

        CardId = card.Id;
        _nameLabel!.Text = card.Name;
        _costLabel!.Text = $"Cost: {card.Cost}";

        var text = card.Description
            .Replace("Deal", "[color=#fca5a5]Deal[/color]")
            .Replace("Gain", "[color=#93c5fd]Gain[/color]")
            .Replace("Block", "[color=#93c5fd]Block[/color]")
            .Replace("Vulnerable", "[color=#e9d5ff]Vulnerable[/color]")
            .Replace("Draw", "[color=#a5f3fc]Draw[/color]")
            .Replace("Heal", "[color=#86efac]Heal[/color]")
            .Replace("damage", "[color=#fda4af]damage[/color]");

        _descLabel!.Text = text;
    }
}
