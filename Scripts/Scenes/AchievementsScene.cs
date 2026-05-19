using Godot;

public partial class AchievementsScene : Control
{
    private Label _titleLabel = null!;
    private Button _backButton = null!;
    private VBoxContainer _entryList = null!;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("%TitleLabel");
        _backButton = GetNode<Button>("%BackButton");
        _entryList = GetNode<VBoxContainer>("%EntryList");

        _backButton.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");

        BuildList();
        LocalizationSettings.LanguageChanged += OnLanguageChanged;
        RefreshText();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        RefreshText();
        BuildList();
    }

    private void RefreshText()
    {
        _titleLabel.Text = "🏆 " + LocalizationService.Get("ui.achievements.title", "Achievements");
        _backButton.Text = "← " + LocalizationService.Get("ui.common.back", "Back");
    }

    private void BuildList()
    {
        foreach (var child in _entryList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var entry in AchievementCatalog.All())
        {
            _entryList.AddChild(BuildEntryFrame(entry));
        }
    }

    private static PanelContainer BuildEntryFrame(AchievementData entry)
    {
        // Each row is a framed panel: large emoji icon on the left, name +
        // description stacked on the right. Style mimics in-game card frames
        // (dark backdrop + warm gold border) so the screen reads as a piece
        // of the game world rather than an OS dialog.
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 100)
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.06f, 0.04f, 0.95f),
            BorderColor = new Color(0.55f, 0.42f, 0.22f, 0.85f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ShadowColor = new Color(0, 0, 0, 0.45f),
            ShadowSize = 6,
            ContentMarginLeft = 20,
            ContentMarginTop = 14,
            ContentMarginRight = 20,
            ContentMarginBottom = 14
        };
        frame.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        hbox.AddThemeConstantOverride("separation", 22);
        frame.AddChild(hbox);

        // Icon: framed emoji circle, also gold-accented.
        var iconFrame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(76, 76)
        };
        var iconStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.13f, 0.10f, 0.06f, 1f),
            BorderColor = new Color(0.85f, 0.65f, 0.32f, 0.85f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14
        };
        iconFrame.AddThemeStyleboxOverride("panel", iconStyle);

        var iconLabel = new Label
        {
            Text = entry.Icon,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconLabel.AddThemeFontSizeOverride("font_size", 44);
        iconFrame.AddChild(iconLabel);
        hbox.AddChild(iconFrame);

        // Right column: name + description stacked.
        var textVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        textVBox.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(textVBox);

        var name = new Label
        {
            Text = entry.LocalizedName
        };
        name.AddThemeFontSizeOverride("font_size", 22);
        name.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.65f, 1f));
        textVBox.AddChild(name);

        var desc = new Label
        {
            Text = entry.LocalizedDescription,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        desc.AddThemeFontSizeOverride("font_size", 14);
        desc.AddThemeColorOverride("font_color", new Color(0.85f, 0.80f, 0.65f, 1f));
        textVBox.AddChild(desc);

        return frame;
    }
}
