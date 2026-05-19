using Godot;

public partial class MainMenu : Control
{
    private Label _gameNameLabel = null!;
    private Label _taglineLabel = null!;
    private Label _logoSubtitle = null!;
    private Button _startButton = null!;
    private Button _abandonRunButton = null!;
    private Button _achievementsButton = null!;
    private Button _bestiaryButton = null!;
    private Button _relicCompendiumButton = null!;
    private Button _cardBrowserButton = null!;
    private Button _optionsButton = null!;
    private Button _quitButton = null!;

    public override void _Ready()
    {
        GetNode<GameState>("/root/GameState").SetUiPhase("main_menu");

        _gameNameLabel = GetNode<Label>("%GameName");
        _taglineLabel = GetNode<Label>("%Tagline");
        _logoSubtitle = GetNode<Label>("%LogoSubtitle");
        _startButton = GetNode<Button>("%StartButton");
        _abandonRunButton = GetNode<Button>("%AbandonRunButton");
        _achievementsButton = GetNode<Button>("%AchievementsButton");
        _bestiaryButton = GetNode<Button>("%BestiaryButton");
        _relicCompendiumButton = GetNode<Button>("%RelicCompendiumButton");
        _cardBrowserButton = GetNode<Button>("%CardBrowserButton");
        _optionsButton = GetNode<Button>("%OptionsButton");
        _quitButton = GetNode<Button>("%QuitButton");

        _startButton.Pressed += OnStartOrContinuePressed;
        _abandonRunButton.Pressed += OnAbandonRunPressed;
        _achievementsButton.Pressed += () => Navigate("res://Scenes/AchievementsScene.tscn");
        _bestiaryButton.Pressed += () => Navigate("res://Scenes/BestiaryScene.tscn");
        _relicCompendiumButton.Pressed += () => Navigate("res://Scenes/RelicCompendiumScene.tscn");
        _cardBrowserButton.Pressed += () => Navigate("res://Scenes/CardBrowserScene.tscn");
        _optionsButton.Pressed += () => Navigate("res://Scenes/SettingsScene.tscn");
        _quitButton.Pressed += OnQuitPressed;

        LocalizationSettings.LanguageChanged += OnLanguageChanged;
        RefreshUiText();
        RefreshSaveSlotState();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        RefreshUiText();
        RefreshSaveSlotState();
    }

    private void RefreshUiText()
    {
        _gameNameLabel.Text = LocalizationService.Get("ui.main_menu.game_name", "SLAY THE HS");
        _taglineLabel.Text = LocalizationService.Get("ui.main_menu.tagline", "Pixel deckbuilder roguelike adventure");
        _logoSubtitle.Text = LocalizationService.Get("ui.main_menu.logo_subtitle", "Slay the Tower");
        _achievementsButton.Text = "🏆 " + LocalizationService.Get("ui.main_menu.achievements", "Achievements");
        _bestiaryButton.Text = "🐉 " + LocalizationService.Get("ui.main_menu.bestiary", "Bestiary");
        _relicCompendiumButton.Text = "💎 " + LocalizationService.Get("ui.main_menu.relic_compendium", "Relic Compendium");
        _cardBrowserButton.Text = "🎴 " + LocalizationService.Get("ui.main_menu.card_browser", "Card Compendium");
        _optionsButton.Text = "⚙ " + LocalizationService.Get("ui.main_menu.settings", "Settings");
        _quitButton.Text = "✕ " + LocalizationService.Get("ui.main_menu.quit", "Quit");
    }

    // Start ↔ Continue toggle — when a save file exists, the primary button
    // resumes the run instead of starting a fresh one. The danger Abandon
    // button only shows when there's actually something to abandon.
    private void RefreshSaveSlotState()
    {
        var hasSave = SaveSystem.HasSave();
        _abandonRunButton.Visible = hasSave;
        if (hasSave)
        {
            _startButton.Text = "▶ " + LocalizationService.Get("ui.main_menu.continue", "Continue");
        }
        else
        {
            _startButton.Text = "▶ " + LocalizationService.Get("ui.main_menu.start_run", "Start Game");
        }
        _abandonRunButton.Text = "✕ " + LocalizationService.Get("ui.main_menu.abandon_save", "Abandon save");
    }

    private void OnStartOrContinuePressed()
    {
        if (SaveSystem.HasSave())
        {
            ContinueExistingRun();
        }
        else
        {
            BeginNewRun();
        }
    }

    private void BeginNewRun()
    {
        // CharacterSelectScene handles SetDeckPreset + StartNewRun before the
        // player walks onto the map.
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("character_select");
        GetTree().ChangeSceneToFile("res://Scenes/CharacterSelectScene.tscn");
    }

    private void ContinueExistingRun()
    {
        var state = GetNode<GameState>("/root/GameState");
        if (!state.TryLoadSaveAndApply(out var scenePath))
        {
            RefreshSaveSlotState();
            return;
        }

        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile(
            string.IsNullOrWhiteSpace(scenePath) ? "res://Scenes/MapScene.tscn" : scenePath);
    }

    private void OnAbandonRunPressed()
    {
        SaveSystem.Delete();
        RefreshSaveSlotState();
    }

    private void Navigate(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
