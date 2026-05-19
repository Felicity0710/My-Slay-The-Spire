using Godot;
using System.Collections.Generic;

public partial class CharacterSelectScene : Control
{
    private Label _titleLabel = null!;
    private Label _nameLabel = null!;
    private Label _hpLabel = null!;
    private Label _deckSizeLabel = null!;
    private Label _abilityHeader = null!;
    private Label _descriptionLabel = null!;
    private Label _portraitGlyph = null!;
    private PanelContainer _portraitPanel = null!;
    private HBoxContainer _avatarRow = null!;
    private Button _backButton = null!;
    private Button _startButton = null!;

    private readonly List<Button> _avatarButtons = new();
    private string _selectedId = string.Empty;

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("character_select");

        _titleLabel = GetNode<Label>("%TitleLabel");
        _nameLabel = GetNode<Label>("%CharacterNameLabel");
        _hpLabel = GetNode<Label>("%HpLabel");
        _deckSizeLabel = GetNode<Label>("%DeckSizeLabel");
        _abilityHeader = GetNode<Label>("%AbilityHeader");
        _descriptionLabel = GetNode<Label>("%DescriptionLabel");
        _portraitGlyph = GetNode<Label>("%PortraitGlyph");
        _portraitPanel = GetNode<PanelContainer>("Margin/VBox/MainHBox/PortraitPanel");
        _avatarRow = GetNode<HBoxContainer>("%AvatarRow");
        _backButton = GetNode<Button>("%BackButton");
        _startButton = GetNode<Button>("%StartButton");

        _backButton.Pressed += OnBackPressed;
        _startButton.Pressed += OnStartPressed;

        BuildAvatarRow();
        SelectCharacter(state.SelectedDeckPresetId);

        LocalizationSettings.LanguageChanged += RefreshText;
        RefreshText();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RefreshText;
    }

    private void BuildAvatarRow()
    {
        foreach (var child in _avatarRow.GetChildren())
        {
            child.QueueFree();
        }
        _avatarButtons.Clear();

        foreach (var preset in DeckPresetCatalog.All())
        {
            var capturedId = preset.Id;
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(96, 110),
                ClipText = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            btn.AddThemeFontSizeOverride("font_size", 14);
            btn.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.78f, 1f));
            btn.AddThemeColorOverride("font_hover_color", Colors.White);
            // Built lazily inside SelectCharacter via ApplyAvatarStyle.
            btn.Pressed += () => SelectCharacter(capturedId);
            _avatarRow.AddChild(btn);
            _avatarButtons.Add(btn);
        }
    }

    private void SelectCharacter(string id)
    {
        var preset = DeckPresetCatalog.Resolve(id);
        _selectedId = preset.Id;

        // Repaint every avatar so only the selected one shows the highlighted
        // border, then refresh the info panel + portrait.
        var presets = DeckPresetCatalog.All();
        for (var i = 0; i < _avatarButtons.Count && i < presets.Count; i++)
        {
            var p = presets[i];
            var isSelected = string.Equals(p.Id, _selectedId);
            ApplyAvatarStyle(_avatarButtons[i], p, isSelected);
            _avatarButtons[i].Text = $"{p.Glyph}\n{p.LocalizedName}";
            _avatarButtons[i].TooltipText = p.LocalizedDescription;
        }

        // Info panel + portrait reflect the newly selected character.
        _nameLabel.Text = preset.LocalizedName;
        _nameLabel.Modulate = preset.Accent;
        _descriptionLabel.Text = preset.LocalizedDescription;
        _portraitGlyph.Text = preset.Glyph;
        _portraitGlyph.Modulate = preset.Accent;
        _deckSizeLabel.Text = LocalizationService.Format(
            "ui.character_select.deck_size",
            "🎴 Deck: {0} cards",
            preset.CardIds.Count);

        // Portrait backdrop tinted by character accent.
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(preset.Accent.R * 0.18f, preset.Accent.G * 0.18f, preset.Accent.B * 0.18f, 0.92f),
            BorderColor = new Color(preset.Accent.R, preset.Accent.G, preset.Accent.B, 0.85f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14
        };
        _portraitPanel.AddThemeStyleboxOverride("panel", bg);
    }

    private static void ApplyAvatarStyle(Button btn, DeckPresetData preset, bool selected)
    {
        var normal = BuildAvatarBox(preset.Accent, selected ? 0.95f : 0.55f, selected ? 4 : 2, selected ? 0.30f : 0.18f);
        var hover = BuildAvatarBox(preset.Accent, 1f, selected ? 4 : 3, 0.36f);
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", normal);
        btn.AddThemeStyleboxOverride("focus", normal);
        btn.Modulate = selected ? Colors.White : new Color(1f, 1f, 1f, 0.85f);
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

    private void RefreshText()
    {
        _titleLabel.Text = LocalizationService.Get("ui.character_select.title", "Choose Your Character");
        _hpLabel.Text = LocalizationService.Format("ui.character_select.max_hp", "❤ Max HP: {0}", 80);
        _abilityHeader.Text = LocalizationService.Get("ui.character_select.ability_header", "✦ Ability");
        _backButton.Text = "← " + LocalizationService.Get("ui.character_select.back", "Back");
        _startButton.Text = "▶ " + LocalizationService.Get("ui.character_select.start", "Begin Adventure");

        // Re-apply selection — language-dependent labels (name / desc) need
        // their localized values pulled from the catalog again.
        if (!string.IsNullOrEmpty(_selectedId))
        {
            SelectCharacter(_selectedId);
        }
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void OnStartPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetDeckPreset(_selectedId);
        state.StartNewRun();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }
}
