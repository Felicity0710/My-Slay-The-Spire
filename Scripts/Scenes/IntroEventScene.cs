using Godot;

public partial class IntroEventScene : Control
{
    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("intro");

        // Clear old TSCN children to avoid conflicts with programmatic UI
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        AddChild(GD.Load<PackedScene>("res://Scenes/NodeSettingsOverlay.tscn").Instantiate());

        state.PlayerHp = state.MaxHp;

        BuildUi(state);
    }

    private void BuildUi(GameState state)
    {
        // ── Full-screen dark backdrop with vignette ──
        var bg = new ColorRect
        {
            Color = new Color(0.02f, 0.01f, 0.04f, 1f),
            AnchorRight = 1f, AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(bg);

        // ── Decorative top/bottom bars ──
        AddChild(MakeBar(top: true));
        AddChild(MakeBar(top: false));

        // ── Center content container ──
        var centerVBox = new VBoxContainer
        {
            AnchorLeft = 0.15f, AnchorRight = 0.85f,
            AnchorTop = 0.10f, AnchorBottom = 0.90f,
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Pass
        };
        centerVBox.AddThemeConstantOverride("separation", 24);
        AddChild(centerVBox);

        // ═══ Act Title ═══
        var actTitle = new Label
        {
            Text = GetActTitle(state.Act),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        actTitle.AddThemeFontSizeOverride("font_size", 42);
        actTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.78f, 0.35f, 1f));
        centerVBox.AddChild(actTitle);

        // ── Ornamental divider ──
        var divider = new Label
        {
            Text = "━━━━━━━ ◈ ━━━━━━━",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        divider.AddThemeFontSizeOverride("font_size", 16);
        divider.AddThemeColorOverride("font_color", new Color(0.65f, 0.55f, 0.35f, 0.7f));
        centerVBox.AddChild(divider);

        // ═══ HP Display ═══
        var hpContainer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        hpContainer.AddThemeConstantOverride("separation", 16);
        centerVBox.AddChild(hpContainer);

        var hpIcon = new Label
        {
            Text = "❤",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        hpIcon.AddThemeFontSizeOverride("font_size", 36);
        hpContainer.AddChild(hpIcon);

        // HP bar background
        var hpBarBg = new PanelContainer
        {
            CustomMinimumSize = new Vector2(300, 28),
            MouseFilter = MouseFilterEnum.Ignore
        };
        var hpBarBgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.04f, 0.04f, 1f),
            BorderColor = new Color(0.55f, 0.35f, 0.25f, 0.6f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        hpBarBg.AddThemeStyleboxOverride("panel", hpBarBgStyle);
        hpContainer.AddChild(hpBarBg);

        // HP bar fill
        var hpPct = (float)state.PlayerHp / state.MaxHp;
        var hpBarFill = new ColorRect
        {
            Color = hpPct > 0.5f ? new Color(0.25f, 0.85f, 0.35f, 1f)
                    : hpPct > 0.25f ? new Color(0.85f, 0.65f, 0.25f, 1f)
                    : new Color(0.85f, 0.25f, 0.25f, 1f),
            Size = new Vector2(294 * hpPct, 24),
            Position = new Vector2(2, 2),
            MouseFilter = MouseFilterEnum.Ignore
        };
        hpBarBg.AddChild(hpBarFill);

        var hpText = new Label
        {
            Text = $"{state.PlayerHp} / {state.MaxHp}",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        hpText.AddThemeFontSizeOverride("font_size", 18);
        hpText.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.85f, 1f));
        hpContainer.AddChild(hpText);

        // ═══ Story Panel ═══
        var storyFrame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(600, 220),
            MouseFilter = MouseFilterEnum.Ignore
        };
        var storyStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.03f, 0.06f, 0.95f),
            BorderColor = new Color(0.7f, 0.55f, 0.28f, 0.75f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ShadowColor = new Color(0, 0, 0, 0.6f), ShadowSize = 16,
            ContentMarginLeft = 28, ContentMarginTop = 22, ContentMarginRight = 28, ContentMarginBottom = 22
        };
        storyFrame.AddThemeStyleboxOverride("panel", storyStyle);
        centerVBox.AddChild(storyFrame);

        // Story text inside a ScrollContainer
        var scrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Pass
        };
        storyFrame.AddChild(scrollContainer);

        var storyText = new Label
        {
            Text = GetActStory(state.Act),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Pass
        };
        storyText.AddThemeFontSizeOverride("font_size", 17);
        storyText.AddThemeColorOverride("font_color", new Color(0.85f, 0.80f, 0.65f, 1f));
        scrollContainer.AddChild(storyText);

        // ═══ Continue Button ═══
        var continueBtn = new Button
        {
            Text = "▸  " + LocalizationService.Get("ui.intro.continue", "Continue"),
            CustomMinimumSize = new Vector2(260, 56),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            ClipText = false
        };
        continueBtn.AddThemeFontSizeOverride("font_size", 20);

        var btnNormal = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.16f, 0.25f, 0.96f),
            BorderColor = new Color(0.85f, 0.65f, 0.32f, 0.85f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 32, ContentMarginTop = 12, ContentMarginRight = 32, ContentMarginBottom = 12
        };
        var btnHover = new StyleBoxFlat
        {
            BgColor = new Color(0.20f, 0.26f, 0.38f, 1f),
            BorderColor = new Color(1f, 0.85f, 0.45f, 1f),
            BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ShadowColor = new Color(1f, 0.75f, 0.25f, 0.25f), ShadowSize = 12,
            ContentMarginLeft = 32, ContentMarginTop = 12, ContentMarginRight = 32, ContentMarginBottom = 12
        };
        continueBtn.AddThemeStyleboxOverride("normal", btnNormal);
        continueBtn.AddThemeStyleboxOverride("hover", btnHover);
        continueBtn.AddThemeStyleboxOverride("pressed", btnNormal);
        continueBtn.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.72f, 1f));
        continueBtn.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 0.88f, 1f));

        continueBtn.Pressed += OnContinuePressed;
        centerVBox.AddChild(continueBtn);
    }

    // ── Decorative top/bottom ornamental bars ──
    private static Control MakeBar(bool top)
    {
        var bar = new ColorRect
        {
            Color = new Color(0.08f, 0.04f, 0.02f, 0.85f),
            AnchorLeft = 0f, AnchorRight = 1f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        if (top) { bar.AnchorTop = 0f; bar.OffsetBottom = 64; }
        else { bar.AnchorBottom = 1f; bar.OffsetTop = -48; }

        // Ornamental line inside the bar
        var ornament = new Label
        {
            Text = "◈ ══════════════════════════════════════════════════ ◈",
            AnchorLeft = 0.5f,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        if (top) { ornament.AnchorTop = 0f; ornament.OffsetTop = 18; }
        else { ornament.AnchorTop = 0f; ornament.OffsetTop = 8; }
        ornament.OffsetLeft = -300;
        ornament.AddThemeFontSizeOverride("font_size", 13);
        ornament.AddThemeColorOverride("font_color", new Color(0.6f, 0.5f, 0.3f, 0.7f));
        bar.AddChild(ornament);

        return bar;
    }

    // ── Act title per chapter ──
    private static string GetActTitle(int act)
    {
        return act switch
        {
            1 => LocalizationService.Get("ui.intro.title_act1", "Act I — The Forsaken Cathedral"),
            2 => LocalizationService.Get("ui.intro.title_act2", "Act II — The Shadow Bazaar"),
            _ => LocalizationService.Get("ui.intro.title_act3", "Act III — The Ancient Sanctum"),
        };
    }

    // ── Story text per chapter ──
    private static string GetActStory(int act)
    {
        return act switch
        {
            1 => LocalizationService.Get("ui.intro.act1",
                "You descend into the depths beneath the forsaken cathedral. "
                + "Whispers of a dark cult echo through the stone corridors. "
                + "The air is thick with incense and decay.\n\n"
                + "The High Priest awaits somewhere below, shielded by his fanatical disciples. "
                + "They will not negotiate. They will not surrender.\n\n"
                + "Steel yourself, adventurer. The dungeon will not yield its secrets easily."),

            2 => LocalizationService.Get("ui.intro.act2",
                "Beneath the ruined outpost, a hidden city thrives — "
                + "the Shadow Bazaar. Thieves, slavers, and black-market dealers "
                + "rule this lawless underworld. Gold talks here, and blood talks louder.\n\n"
                + "The Master Thief Rane watches from the shadows, her blade "
                + "ready to claim whatever you value most. Trust no one."),

            _ => LocalizationService.Get("ui.intro.act3",
                "At the bottom of the abyss, the Ancient Sanctum reveals itself. "
                + "Once a holy temple, now twisted beyond recognition by the corruption "
                + "that seeps from its core.\n\n"
                + "Guardians of living stone, dragons warped by dark magic — all stand "
                + "between you and the source: the Corrupted Heart.\n\n"
                + "This is it. The final battle. Destroy the Heart, or become part of it forever."),
        };
    }

    private void OnContinuePressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }
}
