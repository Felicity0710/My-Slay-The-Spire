using Godot;

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
    private Button _settingsCloseButton = null!;

    private readonly Vector2I[] _windowSizes =
    {
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080)
    };

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
        _resolutionOption.Clear();
        for (var i = 0; i < _windowSizes.Length; i += 1)
        {
            var size = _windowSizes[i];
            _resolutionOption.AddItem($"{size.X} x {size.Y}", i);
        }

        _resolutionOption.ItemSelected += OnResolutionSelected;
        _masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
        _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;

        _masterVolumeSlider.Value = VolumeDbToPercent(AudioServer.GetBusVolumeDb(0));

        var musicBus = AudioServer.GetBusIndex("Music");
        _musicVolumeSlider.Value = musicBus >= 0
            ? VolumeDbToPercent(AudioServer.GetBusVolumeDb(musicBus))
            : _masterVolumeSlider.Value;

        var currentSize = DisplayServer.WindowGetSize();
        for (var i = 0; i < _windowSizes.Length; i += 1)
        {
            if (_windowSizes[i] == currentSize)
            {
                _resolutionOption.Select(i);
                break;
            }
        }

        if (_resolutionOption.Selected < 0)
        {
            _resolutionOption.Select(0);
        }

        _settingsModal.Visible = false;
        RefreshSettingsText();
    }

    private void RefreshSettingsText()
    {
        var isZh = LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans;
        _optionsButton.Text = isZh ? "设置" : "Settings";
        _settingsTitle.Text = isZh ? "设置" : "Settings";
        _resolutionLabel.Text = isZh ? "分辨率" : "Resolution";
        _masterVolumeLabel.Text = isZh ? "主音量" : "Master Volume";
        _musicVolumeLabel.Text = isZh ? "音乐音量" : "Music Volume";
        _settingsCloseButton.Text = isZh ? "关闭" : "Close";
    }

    private void OnResolutionSelected(long index)
    {
        if (index < 0 || index >= _windowSizes.Length)
        {
            return;
        }

        DisplayServer.WindowSetSize(_windowSizes[index]);
    }

    private void OnMasterVolumeChanged(double value)
    {
        AudioServer.SetBusVolumeDb(0, PercentToVolumeDb((float)value));
    }

    private void OnMusicVolumeChanged(double value)
    {
        var musicBus = AudioServer.GetBusIndex("Music");
        if (musicBus < 0)
        {
            return;
        }

        AudioServer.SetBusVolumeDb(musicBus, PercentToVolumeDb((float)value));
    }

    private static float PercentToVolumeDb(float percent)
    {
        if (percent <= 0f)
        {
            return -80f;
        }

        return Mathf.LinearToDb(percent / 100f);
    }

    private static float VolumeDbToPercent(float db)
    {
        if (db <= -80f)
        {
            return 0f;
        }

        return Mathf.DbToLinear(db) * 100f;
    }
}
