using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenu : Control
{
    private Label _gameNameLabel = null!;
    private Label _taglineLabel = null!;
    private Button _startButton = null!;
    private Button _battleTestButton = null!;
    private Button _continueButton = null!;
    private Button _quitButton = null!;
    private Button _languageButton = null!;
    private MenuButton _toolsButton = null!;
    private Button _optionsButton = null!;
    private Label _hintLabel = null!;

    private Control _settingsModal = null!;
    private OptionButton _resolutionOption = null!;
    private HSlider _masterVolumeSlider = null!;
    private HSlider _musicVolumeSlider = null!;
    private Label _settingsTitle = null!;
    private Label _resolutionLabel = null!;
    private Label _masterVolumeLabel = null!;
    private Label _musicVolumeLabel = null!;
    private Label _maxFpsLabel = null!;
    private Label _vsyncLabel = null!;
    private Label _fpsCounterLabelText = null!;
    private OptionButton _maxFpsOption = null!;
    private CheckBox _vsyncCheckBox = null!;
    private CheckBox _fpsCounterCheckBox = null!;
    private Button _settingsCloseButton = null!;

    private List<Vector2I> _windowSizes = new();
    private readonly int[] _fpsCaps = { 0, 30, 60, 120, 144, 165, 240 };
    private OptionButton _deckPresetOption = null!;
    private Label _deckPresetLabel = null!;

    private enum ToolAction
    {
        CardBrowser,
        CardEditor,
        DeckEditor,
        RelicCompendium
    }

    public override void _Ready()
    {
        _gameNameLabel = GetNode<Label>("LeftMenu/Panel/Content/GameName");
        _taglineLabel = GetNode<Label>("LeftMenu/Panel/Content/Tagline");
        GetNode<GameState>("/root/GameState").SetUiPhase("main_menu");
        _startButton = GetNode<Button>("%StartButton");
        _battleTestButton = GetNode<Button>("%BattleTestButton");
        _continueButton = GetNode<Button>("LeftMenu/Panel/Content/ContinueButton");
        _quitButton = GetNode<Button>("%QuitButton");
        _languageButton = GetNode<Button>("%LanguageButton");
        _toolsButton = GetNode<MenuButton>("%ToolsButton");
        _deckPresetOption = GetNode<OptionButton>("%DeckPresetOption");
        _deckPresetLabel = GetNode<Label>("%DeckPresetLabel");
        _optionsButton = GetNode<Button>("%OptionsButton");
        _hintLabel = GetNode<Label>("LeftMenu/Panel/Content/Hint");

        _settingsModal = GetNode<Control>("%SettingsModal");
        _resolutionOption = GetNode<OptionButton>("%ResolutionOption");
        _masterVolumeSlider = GetNode<HSlider>("%MasterVolumeSlider");
        _musicVolumeSlider = GetNode<HSlider>("%MusicVolumeSlider");
        _settingsTitle = GetNode<Label>("%SettingsTitle");
        _resolutionLabel = GetNode<Label>("%ResolutionLabel");
        _masterVolumeLabel = GetNode<Label>("%MasterVolumeLabel");
        _musicVolumeLabel = GetNode<Label>("%MusicVolumeLabel");
        _maxFpsLabel = GetNode<Label>("%MaxFpsLabel");
        _vsyncLabel = GetNode<Label>("%VsyncLabel");
        _fpsCounterLabelText = GetNode<Label>("%FpsCounterLabelText");
        _maxFpsOption = GetNode<OptionButton>("%MaxFpsOption");
        _vsyncCheckBox = GetNode<CheckBox>("%VsyncCheckBox");
        _fpsCounterCheckBox = GetNode<CheckBox>("%FpsCounterCheckBox");
        _settingsCloseButton = GetNode<Button>("%SettingsCloseButton");

        _startButton.Pressed += OnStartPressed;
        _battleTestButton.Pressed += OnBattleTestPressed;
        _quitButton.Pressed += OnQuitPressed;
        _languageButton.Pressed += OnLanguagePressed;
        _deckPresetOption.ItemSelected += OnDeckPresetSelected;
        _optionsButton.Pressed += OnOptionsPressed;
        _settingsCloseButton.Pressed += OnSettingsClosePressed;

        SetupSettingsUi();
        SetupToolsMenu();
        LocalizationSettings.LanguageChanged += OnLanguageChanged;

        RefreshUiText();
        RefreshSettingsText();
        PopulateDeckPresets();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void OnStartPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.StartNewRun();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void OnCardBrowserPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/CardBrowserScene.tscn");
    }

    private void OnBattleTestPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.StartBattleTestRun();
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnCardEditorPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/CardEditorScene.tscn");
    }

    private void OnRelicCompendiumPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/RelicCompendiumScene.tscn");
    }

    private void OnDeckEditorPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/DeckEditorScene.tscn");
    }

    private void SetupToolsMenu()
    {
        var popup = _toolsButton.GetPopup();
        popup.IdPressed += OnToolsMenuItemPressed;
        RebuildToolsMenu();
    }

    private void RebuildToolsMenu()
    {
        var popup = _toolsButton.GetPopup();
        popup.Clear();
        popup.AddItem(LocalizationService.Get("ui.main_menu.card_browser", "Card Compendium"), (int)ToolAction.CardBrowser);
        popup.AddItem(LocalizationService.Get("ui.main_menu.card_editor", "Card Editor"), (int)ToolAction.CardEditor);
        popup.AddItem(LocalizationService.Get("ui.main_menu.deck_editor", "Deck Archetype Editor"), (int)ToolAction.DeckEditor);
        popup.AddItem(LocalizationService.Get("ui.main_menu.relic_compendium", "Relic Compendium"), (int)ToolAction.RelicCompendium);
    }

    private void OnToolsMenuItemPressed(long id)
    {
        switch ((ToolAction)id)
        {
            case ToolAction.CardBrowser:
                OnCardBrowserPressed();
                break;
            case ToolAction.CardEditor:
                OnCardEditorPressed();
                break;
            case ToolAction.DeckEditor:
                OnDeckEditorPressed();
                break;
            case ToolAction.RelicCompendium:
                OnRelicCompendiumPressed();
                break;
        }
    }

    private void OnLanguagePressed()
    {
        LocalizationSettings.ToggleLanguage();
    }

    private void OnLanguageChanged()
    {
        RefreshUiText();
        PopulateDeckPresets();
        RefreshSettingsText();
    }

    private void OnDeckPresetSelected(long index)
    {
        var state = GetNode<GameState>("/root/GameState");
        var presets = state.DeckPresets();
        if (index < 0 || index >= presets.Count)
        {
            return;
        }

        state.SetDeckPreset(presets[(int)index].Id);
        RefreshSettingsText();
    }

    private void OnOptionsPressed()
    {
        _settingsModal.Visible = true;
    }

    private void OnSettingsClosePressed()
    {
        _settingsModal.Visible = false;
    }

    private void RefreshUiText()
    {
        _gameNameLabel.Text = LocalizationService.Get("ui.main_menu.game_name", "SLAY THE HS");
        _taglineLabel.Text = LocalizationService.Get("ui.main_menu.tagline", "Pixel deckbuilder roguelike adventure");
        _languageButton.Text = LocalizationSettings.LanguageButtonText();
        _deckPresetLabel.Text = LocalizationService.Get("ui.main_menu.deck_preset", "Deck Preset");
        _startButton.Text = LocalizationService.Get("ui.main_menu.start_run", "Start Run (Map)");
        _battleTestButton.Text = LocalizationService.Get("ui.main_menu.battle_test", "Battle Test");
        _continueButton.Text = LocalizationService.Get("ui.main_menu.continue", "Continue (Coming Soon)");
        _toolsButton.Text = LocalizationService.Get("ui.main_menu.tools", "Tools");
        _optionsButton.Text = LocalizationService.Get("ui.main_menu.settings", "Settings");
        _quitButton.Text = LocalizationService.Get("ui.main_menu.quit", "Quit");
        _hintLabel.Text = LocalizationService.Get(
            "ui.main_menu.hint",
            "Tip: replace BackgroundImage with your own full-screen background art.");
        RebuildToolsMenu();
    }

    private void PopulateDeckPresets()
    {
        var state = GetNode<GameState>("/root/GameState");
        var presets = state.DeckPresets();
        _deckPresetOption.Clear();

        var selectedIndex = 0;
        for (var i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            _deckPresetOption.AddItem(
                LocalizationService.Format("ui.main_menu.deck_preset_item", "{0} - {1}", preset.LocalizedName, preset.LocalizedDescription));
            if (preset.Id == state.SelectedDeckPresetId)
            {
                selectedIndex = i;
            }
        }

        _deckPresetOption.Select(selectedIndex);
    }

    private void SetupSettingsUi()
    {
        PopulateResolutionOptions();
        PopulateMaxFpsOptions();

        _resolutionOption.ItemSelected += OnResolutionSelected;
        _masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
        _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
        _maxFpsOption.ItemSelected += OnMaxFpsSelected;
        _vsyncCheckBox.Toggled += OnVsyncToggled;
        _fpsCounterCheckBox.Toggled += OnFpsCounterToggled;

        var settings = AppSettings.Instance;
        _masterVolumeSlider.Value = settings.MasterVolumePercent;
        _musicVolumeSlider.Value = settings.MusicVolumePercent;
        _vsyncCheckBox.ButtonPressed = settings.VSyncEnabled;
        _fpsCounterCheckBox.ButtonPressed = settings.ShowFpsCounter;

        _settingsModal.Visible = false;
        RefreshSettingsText();
    }

    private void PopulateResolutionOptions()
    {
        _resolutionOption.Clear();
        _windowSizes = BuildSupportedResolutionList();

        for (var i = 0; i < _windowSizes.Count; i += 1)
        {
            var size = _windowSizes[i];
            _resolutionOption.AddItem($"{size.X} x {size.Y}", i);
        }

        var currentSize = AppSettings.Instance.WindowSize;
        var selectedIndex = _windowSizes.FindIndex(size => size == currentSize);
        _resolutionOption.Select(selectedIndex >= 0 ? selectedIndex : 0);
    }

    private void PopulateMaxFpsOptions()
    {
        _maxFpsOption.Clear();
        for (var i = 0; i < _fpsCaps.Length; i += 1)
        {
            var cap = _fpsCaps[i];
            _maxFpsOption.AddItem(cap <= 0
                ? LocalizationService.Get("ui.options.max_fps.unlimited", "Unlimited")
                : cap.ToString(), i);
        }

        var currentCap = AppSettings.Instance.MaxFps;
        var index = System.Array.IndexOf(_fpsCaps, currentCap);
        _maxFpsOption.Select(index >= 0 ? index : 0);
    }

    private static List<Vector2I> BuildSupportedResolutionList()
    {
        var screen = DisplayServer.WindowGetCurrentScreen();
        var screenSize = DisplayServer.ScreenGetSize(screen);

        var presets = new[]
        {
            new Vector2I(1024, 576),
            new Vector2I(1152, 648),
            new Vector2I(1280, 720),
            new Vector2I(1280, 800),
            new Vector2I(1280, 960),
            new Vector2I(1366, 768),
            new Vector2I(1440, 900),
            new Vector2I(1536, 864),
            new Vector2I(1600, 900),
            new Vector2I(1680, 1050),
            new Vector2I(1920, 1080),
            new Vector2I(1920, 1200),
            new Vector2I(2048, 1152),
            new Vector2I(2048, 1536),
            new Vector2I(2560, 1080),
            new Vector2I(2560, 1440),
            new Vector2I(2560, 1600),
            new Vector2I(3440, 1440),
            new Vector2I(3840, 1600),
            new Vector2I(3840, 2160)
        };

        var unique = new HashSet<Vector2I>();
        for (var i = 0; i < presets.Length; i += 1)
        {
            var preset = presets[i];
            if (preset.X <= screenSize.X && preset.Y <= screenSize.Y)
            {
                unique.Add(preset);
            }
        }

        var currentWindowSize = DisplayServer.WindowGetSize();
        unique.Add(currentWindowSize);

        if (unique.Count == 0)
        {
            unique.Add(new Vector2I(1280, 720));
            unique.Add(new Vector2I(1920, 1080));
        }

        return unique
            .OrderBy(size => size.X * size.Y)
            .ThenBy(size => size.X)
            .ThenBy(size => size.Y)
            .ToList();
    }

    private void RefreshSettingsText()
    {
        _optionsButton.Text = LocalizationService.Get("ui.main_menu.settings", "Settings");
        _settingsTitle.Text = LocalizationService.Get("ui.main_menu.settings", "Settings");
        _resolutionLabel.Text = LocalizationService.Get("ui.options.resolution_label", "Resolution");
        _masterVolumeLabel.Text = LocalizationService.Get("ui.options.master_volume", "Master Volume");
        _musicVolumeLabel.Text = LocalizationService.Get("ui.options.music_volume", "Music Volume");
        _maxFpsLabel.Text = LocalizationService.Get("ui.options.max_fps", "Max FPS");
        _vsyncLabel.Text = LocalizationService.Get("ui.options.vsync", "VSync");
        _fpsCounterLabelText.Text = LocalizationService.Get("ui.options.fps_counter", "FPS Counter");
        _settingsCloseButton.Text = LocalizationService.Get("ui.common.close", "Close");

        var noLimitText = LocalizationService.Get("ui.options.max_fps.unlimited", "Unlimited");
        if (_maxFpsOption.ItemCount > 0)
        {
            _maxFpsOption.SetItemText(0, noLimitText);
        }
    }

    private void OnResolutionSelected(long index)
    {
        if (index < 0 || index >= _windowSizes.Count)
        {
            return;
        }

        AppSettings.Instance.SetWindowSize(_windowSizes[(int)index]);
    }

    private void OnMaxFpsSelected(long index)
    {
        if (index < 0 || index >= _fpsCaps.Length)
        {
            return;
        }

        AppSettings.Instance.SetMaxFps(_fpsCaps[(int)index]);
    }

    private void OnVsyncToggled(bool enabled)
    {
        AppSettings.Instance.SetVSyncEnabled(enabled);
    }

    private void OnFpsCounterToggled(bool enabled)
    {
        AppSettings.Instance.SetShowFpsCounter(enabled);
    }

    private void OnMasterVolumeChanged(double value)
    {
        AppSettings.Instance.SetMasterVolumePercent((float)value);
    }

    private void OnMusicVolumeChanged(double value)
    {
        AppSettings.Instance.SetMusicVolumePercent((float)value);
    }
}
