using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleScene
{
    private void SetupSettingsUi()
    {
        PopulateResolutionOptions();
        PopulateMaxFpsOptions();

        _resolutionOption.ItemSelected += OnResolutionSelected;
        _maxFpsOption.ItemSelected += OnMaxFpsSelected;
        _vsyncCheckBox.Toggled += OnVsyncToggled;
        _fpsCounterCheckBox.Toggled += OnFpsCounterToggled;
        _masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
        _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;

        var settings = AppSettings.Instance;
        _vsyncCheckBox.ButtonPressed = settings.VSyncEnabled;
        _fpsCounterCheckBox.ButtonPressed = settings.ShowFpsCounter;
        _masterVolumeSlider.Value = settings.MasterVolumePercent;
        _musicVolumeSlider.Value = settings.MusicVolumePercent;

        _settingsModal.Visible = false;
        RefreshSettingsText();
    }

    private void RefreshSettingsText()
    {
        _settingsButton.Text = LocalizationService.Get("ui.battle.settings", "Settings");
        _settingsTitle.Text = LocalizationService.Get("ui.battle.settings", "Settings");
        _resolutionLabel.Text = LocalizationService.Get("ui.battle.settings_resolution", "Resolution");
        _maxFpsLabel.Text = LocalizationService.Get("ui.battle.settings_max_fps", "Max FPS");
        _vsyncLabel.Text = LocalizationService.Get("ui.battle.settings_vsync", "VSync");
        _fpsCounterLabelText.Text = LocalizationService.Get("ui.battle.settings_fps_counter", "Show FPS");
        _masterVolumeLabel.Text = LocalizationService.Get("ui.battle.settings_master_volume", "Master Volume");
        _musicVolumeLabel.Text = LocalizationService.Get("ui.battle.settings_music_volume", "Music Volume");
        _settingsCloseButton.Text = LocalizationService.Get("ui.common.close", "Close");
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

        var selectedIndex = _windowSizes.FindIndex(s => s == AppSettings.Instance.WindowSize);
        _resolutionOption.Select(selectedIndex >= 0 ? selectedIndex : 0);
    }

    private void PopulateMaxFpsOptions()
    {
        _maxFpsOption.Clear();
        for (var i = 0; i < _fpsCaps.Length; i++)
        {
            var cap = _fpsCaps[i];
            _maxFpsOption.AddItem(cap <= 0 ? "Unlimited" : cap.ToString(), i);
        }

        var currentCap = AppSettings.Instance.MaxFps;
        var index = Array.IndexOf(_fpsCaps, currentCap);
        _maxFpsOption.Select(index >= 0 ? index : 0);
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

    private void OnOpenSettingsPressed()
    {
        _settingsModal.Visible = true;
        RefreshSettingsText();
    }

    private void OnCloseSettingsPressed()
    {
        _settingsModal.Visible = false;
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

    private void RefreshBattleStaticText()
    {
        RefreshSettingsText();
        _endTurnButton.Text = LocalizationService.Get("ui.battle.end_turn", "End Turn");
        _backButton.Text = LocalizationService.Get("ui.battle.back_to_map", "Back To Map");
        _testVictoryButton.Text = LocalizationService.Get("ui.battle.test_victory_button", "Test Victory");
        _logTitleLabel.Text = LocalizationService.Get("ui.battle.log_title", "Action Log");
        _turnBannerLabel.Text = LocalizationService.Get("ui.battle.turn_player", "Player Turn");
    }

    private void OnLanguageChanged()
    {
        RefreshBattleStaticText();
        RefreshUi();
    }
}
