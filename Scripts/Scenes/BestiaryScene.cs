using Godot;
using System.Collections.Generic;

public partial class BestiaryScene : Control
{
    private Button _backButton = null!;
    private Label _titleLabel = null!;
    private GridContainer _enemyGrid = null!;
    private PanelContainer _portraitFrame = null!;
    private TextureRect _portraitImage = null!;
    private Label _detailNameLabel = null!;
    private Label _detailTraitLabel = null!;
    private Label _detailEmptyLabel = null!;

    private string _selectedEnemyId = string.Empty;
    private readonly Dictionary<string, Button> _avatarButtons = new();

    public override void _Ready()
    {
        _backButton = GetNode<Button>("%BackButton");
        _titleLabel = GetNode<Label>("%TitleLabel");
        _enemyGrid = GetNode<GridContainer>("%EnemyGrid");
        _portraitFrame = GetNode<PanelContainer>("%PortraitFrame");
        _portraitImage = GetNode<TextureRect>("%PortraitImage");
        _detailNameLabel = GetNode<Label>("%DetailNameLabel");
        _detailTraitLabel = GetNode<Label>("%DetailTraitLabel");
        _detailEmptyLabel = GetNode<Label>("%DetailEmptyLabel");

        _backButton.Pressed += OnBackPressed;

        BuildGrid();
        ClearDetail();

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
        BuildGrid();
        if (!string.IsNullOrEmpty(_selectedEnemyId))
        {
            ShowEnemyDetail(_selectedEnemyId);
        }
    }

    private void RefreshText()
    {
        _titleLabel.Text = "🐉 " + LocalizationService.Get("ui.bestiary.title", "Bestiary");
        _backButton.Text = "← " + LocalizationService.Get("ui.common.back", "Back");
        _detailEmptyLabel.Text = LocalizationService.Get(
            "ui.bestiary.empty",
            "Select an enemy on the left.");
    }

    private void BuildGrid()
    {
        foreach (var child in _enemyGrid.GetChildren())
        {
            child.QueueFree();
        }
        _avatarButtons.Clear();

        foreach (var enemyId in CombatVisualCatalog.AllEnemyIds())
        {
            var capturedId = enemyId;
            var visual = CombatVisualCatalog.GetEnemyVisual(enemyId);
            var accent = CombatVisualCatalog.GetEnemyTraitAccent(enemyId);
            var displayName = CombatVisualCatalog.GetLocalizedEnemyDisplayName(enemyId);

            var btn = new Button
            {
                CustomMinimumSize = new Vector2(150, 170),
                ClipText = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TooltipText = displayName + "\n" + CombatVisualCatalog.GetEnemyTraitSummary(enemyId)
            };
            btn.AddThemeFontSizeOverride("font_size", 14);
            btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.78f));
            btn.AddThemeColorOverride("font_hover_color", Colors.White);
            ApplyAvatarStyle(btn, accent, isSelected: false);

            // Portrait sits on top via a manually-positioned TextureRect since
            // Button does not natively support an icon-over-text layout that
            // keeps proportions across font sizes.
            var iconCenter = new CenterContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorRight = 1f,
                AnchorBottom = 1f
            };
            iconCenter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            iconCenter.OffsetBottom = -28; // leave space for the label
            iconCenter.MouseFilter = MouseFilterEnum.Ignore;

            var portrait = new TextureRect
            {
                Texture = GD.Load<Texture2D>(visual.PortraitPath),
                CustomMinimumSize = new Vector2(110, 110),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            iconCenter.AddChild(portrait);
            btn.AddChild(iconCenter);

            // Name label pinned to the bottom of the button.
            var nameLabel = new Label
            {
                Text = displayName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore
            };
            nameLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            nameLabel.OffsetTop = -32;
            nameLabel.AddThemeFontSizeOverride("font_size", 14);
            nameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.78f));
            btn.AddChild(nameLabel);

            btn.Pressed += () => ShowEnemyDetail(capturedId);
            _enemyGrid.AddChild(btn);
            _avatarButtons[enemyId] = btn;
        }
    }

    private void ShowEnemyDetail(string enemyId)
    {
        _selectedEnemyId = enemyId;
        var visual = CombatVisualCatalog.GetEnemyVisual(enemyId);
        var accent = CombatVisualCatalog.GetEnemyTraitAccent(enemyId);
        var displayName = CombatVisualCatalog.GetLocalizedEnemyDisplayName(enemyId);
        var trait = CombatVisualCatalog.GetEnemyTraitSummary(enemyId);

        _detailEmptyLabel.Visible = false;
        _portraitFrame.Visible = true;
        _detailNameLabel.Visible = true;
        _detailTraitLabel.Visible = true;

        _portraitImage.Texture = GD.Load<Texture2D>(visual.PortraitPath);
        _detailNameLabel.Text = displayName;
        _detailNameLabel.Modulate = accent;
        _detailTraitLabel.Text = trait;

        var portraitBox = new StyleBoxFlat
        {
            BgColor = new Color(visual.StageTint.R, visual.StageTint.G, visual.StageTint.B, 1f),
            BorderColor = new Color(accent.R, accent.G, accent.B, 0.95f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14
        };
        _portraitFrame.AddThemeStyleboxOverride("panel", portraitBox);

        // Repaint avatar tiles so only the selected one shows highlighted border.
        foreach (var kv in _avatarButtons)
        {
            var enemyAccent = CombatVisualCatalog.GetEnemyTraitAccent(kv.Key);
            ApplyAvatarStyle(kv.Value, enemyAccent, kv.Key == enemyId);
        }
    }

    private void ClearDetail()
    {
        _selectedEnemyId = string.Empty;
        _portraitFrame.Visible = false;
        _detailNameLabel.Visible = false;
        _detailTraitLabel.Visible = false;
        _detailEmptyLabel.Visible = true;
    }

    private static void ApplyAvatarStyle(Button btn, Color accent, bool isSelected)
    {
        var normal = BuildAvatarBox(accent, isSelected ? 0.95f : 0.55f, isSelected ? 3 : 2, isSelected ? 0.30f : 0.18f);
        var hover = BuildAvatarBox(accent, 1f, 3, 0.32f);
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", normal);
        btn.AddThemeStyleboxOverride("focus", normal);
    }

    private static StyleBoxFlat BuildAvatarBox(Color accent, float borderAlpha, int borderWidth, float bgMul)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(accent.R * bgMul, accent.G * bgMul, accent.B * bgMul, 0.95f),
            BorderColor = new Color(accent.R, accent.G, accent.B, borderAlpha),
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 6,
            ContentMarginTop = 6,
            ContentMarginRight = 6,
            ContentMarginBottom = 6
        };
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
