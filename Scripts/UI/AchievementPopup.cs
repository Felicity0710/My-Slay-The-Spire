using Godot;
using System.Collections.Generic;

/// <summary>
/// Displays achievement-unlock notifications as animated popups that slide in
/// from the top-right corner of the screen. Each popup auto-dismisses after a
/// few seconds, stacking vertically. Used on the Victory and Defeat screens.
/// </summary>
public partial class AchievementPopup : Control
{
    private sealed class PopupEntry
    {
        public AchievementData Achievement = null!;
        public PanelContainer Frame = null!;
        public double Elapsed;
        public bool Dismissing;
    }

    private const double DisplayDuration = 3.5;
    private const double DismissDuration = 0.4;
    private const float PopupWidth = 320f;
    private const float PopupHeight = 72f;

    private readonly List<PopupEntry> _popups = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorRight = 1f;
        AnchorTop = 0f;
        OffsetRight = -16;
        OffsetTop = 16;
        SetProcess(true);
    }

    /// <summary>Queue an achievement notification for display.</summary>
    public void ShowAchievement(AchievementData achievement)
    {
        var frame = BuildPopupFrame(achievement);
        AddChild(frame);

        // Start off-screen to the right
        frame.Position = new Vector2(0, _popups.Count * (PopupHeight + 8));
        frame.Modulate = new Color(1, 1, 1, 0);

        _popups.Add(new PopupEntry
        {
            Achievement = achievement,
            Frame = frame,
            Elapsed = 0,
            Dismissing = false
        });
    }

    /// <summary>Show all newly unlocked achievements from this run.</summary>
    public void ShowAllNewAchievements()
    {
        var newIds = AchievementState.GetNewThisRun();
        foreach (var id in newIds)
        {
            var achievement = FindAchievement(id);
            if (achievement != null) ShowAchievement(achievement);
        }
    }

    private static AchievementData? FindAchievement(string id)
    {
        foreach (var a in AchievementCatalog.All())
            if (a.Id == id) return a;
        return null;
    }

    public override void _Process(double delta)
    {
        var needsReposition = false;

        for (var i = _popups.Count - 1; i >= 0; i--)
        {
            var entry = _popups[i];
            if (entry.Dismissing)
            {
                entry.Elapsed += delta;
                var t = Mathf.Clamp((float)(entry.Elapsed / DismissDuration), 0f, 1f);
                var eased = 1f - EaseOutCubic(1f - t);
                entry.Frame.Modulate = new Color(1, 1, 1, 1f - eased);
                entry.Frame.Position = new Vector2(20f * eased, entry.Frame.Position.Y);

                if (t >= 1f)
                {
                    entry.Frame.QueueFree();
                    _popups.RemoveAt(i);
                    needsReposition = true;
                }
            }
            else
            {
                entry.Elapsed += delta;
                // Slide in animation
                if (entry.Elapsed < 0.35)
                {
                    var t = Mathf.Clamp((float)(entry.Elapsed / 0.35), 0f, 1f);
                    var eased = EaseOutCubic(t);
                    entry.Frame.Modulate = new Color(1, 1, 1, eased);
                    entry.Frame.Position = new Vector2(30f * (1f - eased), entry.Frame.Position.Y);
                }
                else if (entry.Elapsed >= DisplayDuration)
                {
                    entry.Dismissing = true;
                    entry.Elapsed = 0;
                }
            }
        }

        if (needsReposition)
        {
            for (var i = 0; i < _popups.Count; i++)
            {
                var targetY = i * (PopupHeight + 8);
                _popups[i].Frame.Position = new Vector2(_popups[i].Frame.Position.X, targetY);
            }
        }
    }

    private static PanelContainer BuildPopupFrame(AchievementData achievement)
    {
        var frame = new PanelContainer
        {
            CustomMinimumSize = new Vector2(PopupWidth, PopupHeight),
            MouseFilter = MouseFilterEnum.Ignore
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.04f, 0.08f, 0.96f),
            BorderColor = new Color(0.85f, 0.65f, 0.25f, 0.9f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ShadowColor = new Color(0, 0, 0, 0.5f), ShadowSize = 8,
            ContentMarginLeft = 12, ContentMarginTop = 8, ContentMarginRight = 12, ContentMarginBottom = 8
        };
        frame.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        hbox.AddThemeConstantOverride("separation", 12);
        frame.AddChild(hbox);

        // Icon
        var iconLabel = new Label
        {
            Text = achievement.Icon,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(44, 44),
            MouseFilter = MouseFilterEnum.Ignore
        };
        iconLabel.AddThemeFontSizeOverride("font_size", 32);
        hbox.AddChild(iconLabel);

        // Text column
        var textVBox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        textVBox.AddThemeConstantOverride("separation", 2);
        hbox.AddChild(textVBox);

        // "Achievement Unlocked!" label
        var headerLabel = new Label
        {
            Text = LocalizationService.Get("ui.achievement.unlocked", "Achievement Unlocked!"),
            MouseFilter = MouseFilterEnum.Ignore
        };
        headerLabel.AddThemeFontSizeOverride("font_size", 10);
        headerLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.25f, 1f));
        textVBox.AddChild(headerLabel);

        // Achievement name
        var nameLabel = new Label
        {
            Text = achievement.LocalizedName,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 15);
        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.7f, 1f));
        textVBox.AddChild(nameLabel);

        return frame;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
