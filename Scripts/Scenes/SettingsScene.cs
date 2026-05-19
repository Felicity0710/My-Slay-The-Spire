using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Full-screen settings page reached from the MainMenu. Matches the in-game
// NodeSettingsOverlay's section-header/section-body styling so the look is
// consistent across all settings touchpoints. Adds a Language toggle at the
// top — moved off the main menu by user request.
public partial class SettingsScene : Control
{
    private static readonly int[] FpsCaps = { 0, 30, 60, 120, 144, 165, 240 };

    private Label _titleLabel = null!;
    private Label _sectionLanguageLabel = null!;
    private Button _languageButton = null!;
    private Label _sectionDisplayLabel = null!;
    private Label _resolutionLabel = null!;
    private OptionButton _resolutionOption = null!;
    private Label _maxFpsLabel = null!;
    private OptionButton _maxFpsOption = null!;
    private Label _vsyncLabel = null!;
    private CheckBox _vsyncCheckBox = null!;
    private Label _fpsCounterLabel = null!;
    private CheckBox _fpsCounterCheckBox = null!;
    private Label _sectionAudioLabel = null!;
    private Label _masterVolumeLabel = null!;
    private HSlider _masterVolumeSlider = null!;
    private Label _musicVolumeLabel = null!;
    private HSlider _musicVolumeSlider = null!;
    private Button _closeButton = null!;

    private List<Vector2I> _windowSizes = new();

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("%TitleLabel");
        _sectionLanguageLabel = GetNode<Label>("%SectionLanguageLabel");
        _languageButton = GetNode<Button>("%LanguageButton");
        _sectionDisplayLabel = GetNode<Label>("%SectionDisplayLabel");
        _resolutionLabel = GetNode<Label>("%ResolutionLabel");
        _resolutionOption = GetNode<OptionButton>("%ResolutionOption");
        _maxFpsLabel = GetNode<Label>("%MaxFpsLabel");
        _maxFpsOption = GetNode<OptionButton>("%MaxFpsOption");
        _vsyncLabel = GetNode<Label>("%VsyncLabel");
        _vsyncCheckBox = GetNode<CheckBox>("%VsyncCheckBox");
        _fpsCounterLabel = GetNode<Label>("%FpsCounterLabel");
        _fpsCounterCheckBox = GetNode<CheckBox>("%FpsCounterCheckBox");
        _sectionAudioLabel = GetNode<Label>("%SectionAudioLabel");
        _masterVolumeLabel = GetNode<Label>("%MasterVolumeLabel");
        _masterVolumeSlider = GetNode<HSlider>("%MasterVolumeSlider");
        _musicVolumeLabel = GetNode<Label>("%MusicVolumeLabel");
        _musicVolumeSlider = GetNode<HSlider>("%MusicVolumeSlider");
        _closeButton = GetNode<Button>("%CloseButton");

        PopulateResolutionOptions();
        PopulateMaxFpsOptions();

        var settings = AppSettings.Instance;
        _vsyncCheckBox.ButtonPressed = settings.VSyncEnabled;
        _fpsCounterCheckBox.ButtonPressed = settings.ShowFpsCounter;
        _masterVolumeSlider.Value = settings.MasterVolumePercent;
        _musicVolumeSlider.Value = settings.MusicVolumePercent;

        _languageButton.Pressed += OnLanguagePressed;
        _resolutionOption.ItemSelected += OnResolutionSelected;
        _maxFpsOption.ItemSelected += OnMaxFpsSelected;
        _vsyncCheckBox.Toggled += OnVsyncToggled;
        _fpsCounterCheckBox.Toggled += OnFpsCounterToggled;
        _masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
        _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
        _closeButton.Pressed += OnClosePressed;

        LocalizationSettings.LanguageChanged += RefreshText;
        RefreshText();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RefreshText;
    }

    private void RefreshText()
    {
        _titleLabel.Text = LocalizationService.Get("ui.settings.title", "Settings");
        _sectionLanguageLabel.Text = "🌐 " + LocalizationService.Get("ui.settings.section_language", "Language");
        _languageButton.Text = LocalizationSettings.LanguageButtonText();
        _sectionDisplayLabel.Text = "🖥 " + LocalizationService.Get("ui.node_settings.section_display", "Display");
        _resolutionLabel.Text = LocalizationService.Get("ui.battle.settings_resolution", "Resolution");
        _maxFpsLabel.Text = LocalizationService.Get("ui.battle.settings_max_fps", "Max FPS");
        _vsyncLabel.Text = LocalizationService.Get("ui.battle.settings_vsync", "VSync");
        _fpsCounterLabel.Text = LocalizationService.Get("ui.battle.settings_fps_counter", "Show FPS");
        _sectionAudioLabel.Text = "🔊 " + LocalizationService.Get("ui.node_settings.section_audio", "Audio");
        _masterVolumeLabel.Text = LocalizationService.Get("ui.battle.settings_master_volume", "Master Volume");
        _musicVolumeLabel.Text = LocalizationService.Get("ui.battle.settings_music_volume", "Music Volume");
        _closeButton.Text = "← " + LocalizationService.Get("ui.common.back", "Back");

        if (_maxFpsOption.ItemCount > 0)
        {
            _maxFpsOption.SetItemText(0, LocalizationService.Get("ui.options.max_fps.unlimited", "Unlimited"));
        }
    }

    private void PopulateResolutionOptions()
    {
        _resolutionOption.Clear();
        _windowSizes = BuildSupportedResolutionList();
        for (var i = 0; i < _windowSizes.Count; i++)
        {
            var size = _windowSizes[i];
            _resolutionOption.AddItem($"{size.X} x {size.Y}", i);
        }

        var idx = _windowSizes.FindIndex(s => s == AppSettings.Instance.WindowSize);
        _resolutionOption.Select(idx >= 0 ? idx : 0);
    }

    private void PopulateMaxFpsOptions()
    {
        _maxFpsOption.Clear();
        for (var i = 0; i < FpsCaps.Length; i++)
        {
            var cap = FpsCaps[i];
            _maxFpsOption.AddItem(cap <= 0 ? "Unlimited" : cap.ToString(), i);
        }

        var idx = Array.IndexOf(FpsCaps, AppSettings.Instance.MaxFps);
        _maxFpsOption.Select(idx >= 0 ? idx : 0);
    }

    private static List<Vector2I> BuildSupportedResolutionList()
    {
        var screen = DisplayServer.WindowGetCurrentScreen();
        var screenSize = DisplayServer.ScreenGetSize(screen);
        var presets = new[]
        {
            new Vector2I(1024, 576), new Vector2I(1152, 648), new Vector2I(1280, 720),
            new Vector2I(1280, 800), new Vector2I(1366, 768), new Vector2I(1600, 900),
            new Vector2I(1920, 1080), new Vector2I(2560, 1440), new Vector2I(3440, 1440), new Vector2I(3840, 2160)
        };

        var unique = new HashSet<Vector2I>();
        foreach (var preset in presets)
        {
            if (preset.X <= screenSize.X && preset.Y <= screenSize.Y)
            {
                unique.Add(preset);
            }
        }
        unique.Add(AppSettings.Instance.WindowSize);
        return unique.OrderBy(s => s.X * s.Y).ThenBy(s => s.X).ThenBy(s => s.Y).ToList();
    }

    private void OnLanguagePressed()
    {
        LocalizationSettings.ToggleLanguage();
    }

    private void OnResolutionSelected(long index)
    {
        if (index < 0 || index >= _windowSizes.Count) return;
        AppSettings.Instance.SetWindowSize(_windowSizes[(int)index]);
    }

    private void OnMaxFpsSelected(long index)
    {
        if (index < 0 || index >= FpsCaps.Length) return;
        AppSettings.Instance.SetMaxFps(FpsCaps[(int)index]);
    }

    private void OnVsyncToggled(bool enabled) => AppSettings.Instance.SetVSyncEnabled(enabled);
    private void OnFpsCounterToggled(bool enabled) => AppSettings.Instance.SetShowFpsCounter(enabled);
    private void OnMasterVolumeChanged(double value) => AppSettings.Instance.SetMasterVolumePercent((float)value);
    private void OnMusicVolumeChanged(double value) => AppSettings.Instance.SetMusicVolumePercent((float)value);

    private void OnClosePressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
