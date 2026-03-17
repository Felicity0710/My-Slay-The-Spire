using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenu : Control
{
    private Button _startButton = null!;
    private Button _quitButton = null!;
    private Button _languageButton = null!;
    private Button _cardEditorButton = null!;
    private Button _optionsButton = null!;

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

    public override void _Ready()
    {
        _startButton = GetNode<Button>("%StartButton");
        _quitButton = GetNode<Button>("%QuitButton");
        _languageButton = GetNode<Button>("%LanguageButton");
        _cardEditorButton = GetNode<Button>("%CardEditorButton");
        _optionsButton = GetNode<Button>("%OptionsButton");

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
        _quitButton.Pressed += OnQuitPressed;
        _languageButton.Pressed += OnLanguagePressed;
        _cardEditorButton.Pressed += OnCardEditorPressed;
        _optionsButton.Pressed += OnOptionsPressed;
        _settingsCloseButton.Pressed += OnSettingsClosePressed;

        SetupSettingsUi();

        RefreshLanguageButtonText();
    }

    private void OnStartPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.StartNewRun();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnCardEditorPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/CardEditorScene.tscn");
    }

    private void OnLanguagePressed()
    {
        LocalizationSettings.ToggleLanguage();
        RefreshLanguageButtonText();
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

    private void RefreshLanguageButtonText()
    {
        _languageButton.Text = LocalizationSettings.LanguageButtonText();
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
            _maxFpsOption.AddItem(cap <= 0 ? "Unlimited" : cap.ToString(), i);
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
        foreach (var preset in presets)
        {
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
        var isZh = LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans;
        _optionsButton.Text = isZh ? "设置" : "Settings";
        _settingsTitle.Text = isZh ? "设置" : "Settings";
        _resolutionLabel.Text = isZh ? "分辨率" : "Resolution";
        _masterVolumeLabel.Text = isZh ? "主音量" : "Master Volume";
        _musicVolumeLabel.Text = isZh ? "音乐音量" : "Music Volume";
        _maxFpsLabel.Text = isZh ? "最大帧率" : "Max FPS";
        _vsyncLabel.Text = isZh ? "垂直同步" : "VSync";
        _fpsCounterLabelText.Text = isZh ? "显示帧率" : "Show FPS";
        _settingsCloseButton.Text = isZh ? "关闭" : "Close";

        var noLimitText = isZh ? "不限制" : "Unlimited";
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
