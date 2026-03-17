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

        MouseFilter = MouseFilterEnum.Stop;
        _nameLabel!.MouseFilter = MouseFilterEnum.Ignore;
        _costLabel!.MouseFilter = MouseFilterEnum.Ignore;
        _descLabel!.MouseFilter = MouseFilterEnum.Ignore;

        AddThemeStyleboxOverride("panel", normalStyle);
        _costLabel!.AddThemeColorOverride("font_color", new Color("93c5fd"));
        _descLabel!.AddThemeColorOverride("default_color", new Color("cbd5e1"));

        MouseEntered += () =>
        {
            AddThemeStyleboxOverride("panel", hoverStyle);
        };

        MouseExited += () =>
        {
            AddThemeStyleboxOverride("panel", normalStyle);
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
        _costLabel!.Text = $"{LocalizationSettings.CostLabel()}: {card.Cost}";

        var text = LocalizationSettings.HighlightCardDescription(card.GetLocalizedDescription());

        _descLabel!.Text = text;
    }
}
