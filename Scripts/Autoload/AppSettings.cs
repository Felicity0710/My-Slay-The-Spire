using Godot;
using System;

public partial class AppSettings : Node
{
    public static AppSettings Instance { get; private set; } = null!;

    public event Action? SettingsChanged;

    public float MasterVolumePercent { get; private set; }
    public float MusicVolumePercent { get; private set; }
    public int MaxFps { get; private set; }
    public bool VSyncEnabled { get; private set; }
    public bool ShowFpsCounter { get; private set; }
    public Vector2I WindowSize { get; private set; }

    private CanvasLayer _fpsLayer = null!;
    private Label _fpsLabel = null!;

    public override void _Ready()
    {
        Instance = this;
        LocalizationService.EnsureLoaded();

        MasterVolumePercent = VolumeDbToPercent(AudioServer.GetBusVolumeDb(0));
        var musicBus = AudioServer.GetBusIndex("Music");
        MusicVolumePercent = musicBus >= 0
            ? VolumeDbToPercent(AudioServer.GetBusVolumeDb(musicBus))
            : MasterVolumePercent;
        MaxFps = Engine.MaxFps;
        VSyncEnabled = DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;
        ShowFpsCounter = false;
        WindowSize = DisplayServer.WindowGetSize();

        SetupFpsOverlay();
        LocalizationSettings.LanguageChanged += () => _fpsLabel.Text = LocalizationService.Format("ui.hud.fps", "FPS: {0}", Engine.GetFramesPerSecond());
        ApplyAll();
    }

    public override void _Process(double _delta)
    {
        if (!_fpsLabel.Visible)
        {
            return;
        }

        _fpsLabel.Text = LocalizationService.Format("ui.hud.fps", "FPS: {0}", Engine.GetFramesPerSecond());
    }

    public void SetWindowSize(Vector2I size)
    {
        WindowSize = size;
        DisplayServer.WindowSetSize(size);
        NotifyChanged();
    }

    public void SetMasterVolumePercent(float value)
    {
        MasterVolumePercent = Mathf.Clamp(value, 0f, 100f);
        AudioServer.SetBusVolumeDb(0, PercentToVolumeDb(MasterVolumePercent));
        NotifyChanged();
    }

    public void SetMusicVolumePercent(float value)
    {
        MusicVolumePercent = Mathf.Clamp(value, 0f, 100f);
        var musicBus = AudioServer.GetBusIndex("Music");
        if (musicBus >= 0)
        {
            AudioServer.SetBusVolumeDb(musicBus, PercentToVolumeDb(MusicVolumePercent));
        }

        NotifyChanged();
    }

    public void SetMaxFps(int value)
    {
        MaxFps = value;
        Engine.MaxFps = value;
        NotifyChanged();
    }

    public void SetVSyncEnabled(bool enabled)
    {
        VSyncEnabled = enabled;
        DisplayServer.WindowSetVsyncMode(enabled
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);
        NotifyChanged();
    }

    public void SetShowFpsCounter(bool enabled)
    {
        ShowFpsCounter = enabled;
        _fpsLabel.Visible = enabled;
        NotifyChanged();
    }

    public void ApplyAll()
    {
        DisplayServer.WindowSetSize(WindowSize);
        AudioServer.SetBusVolumeDb(0, PercentToVolumeDb(MasterVolumePercent));
        var musicBus = AudioServer.GetBusIndex("Music");
        if (musicBus >= 0)
        {
            AudioServer.SetBusVolumeDb(musicBus, PercentToVolumeDb(MusicVolumePercent));
        }

        Engine.MaxFps = MaxFps;
        DisplayServer.WindowSetVsyncMode(VSyncEnabled
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);
        _fpsLabel.Visible = ShowFpsCounter;
        NotifyChanged();
    }

    private void SetupFpsOverlay()
    {
        _fpsLayer = new CanvasLayer { Layer = 200 };
        AddChild(_fpsLayer);

        _fpsLabel = new Label
        {
            Text = LocalizationService.Format("ui.hud.fps", "FPS: {0}", 0),
            Visible = false
        };
        _fpsLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _fpsLabel.OffsetLeft = 16;
        _fpsLabel.OffsetTop = 16;
        _fpsLabel.OffsetRight = 176;
        _fpsLabel.OffsetBottom = 56;
        _fpsLabel.AddThemeFontSizeOverride("font_size", 24);
        _fpsLayer.AddChild(_fpsLabel);
    }

    private void NotifyChanged()
    {
        SettingsChanged?.Invoke();
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
