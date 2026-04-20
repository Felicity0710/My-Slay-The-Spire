using Godot;
using System;
using System.Threading.Tasks;

public partial class BattleScene
{
    private void FlashPanel(Control panel, Color flashColor)
    {
        if (!IsInstanceValid(panel))
        {
            return;
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(panel, "modulate", flashColor, 0.07f);
        tween.TweenProperty(panel, "modulate", Colors.White, 0.16f);
    }

    private void TriggerEnemyHit()
    {
        if (_enemyAnimState == EnemyAnimState.Dying)
        {
            return;
        }

        _enemyAnimState = EnemyAnimState.Hit;
        _enemyAnimTimer = 0.14f;
    }

    private async Task TriggerEnemyDeath()
    {
        if (_enemyAnimState == EnemyAnimState.Dying)
        {
            return;
        }

        _enemyAnimState = EnemyAnimState.Dying;

        if (IsFastMode)
        {
            _enemyDropArea.Modulate = new Color(1f, 1f, 1f, 0.15f);
            _enemyDropArea.Scale = _enemyDropAreaBaseScale * 0.78f;
            return;
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_enemyDropArea, "modulate:a", 0.15f, 0.28f);
        tween.Parallel().TweenProperty(_enemyDropArea, "scale", _enemyDropAreaBaseScale * 0.78f, 0.28f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void SpawnSlashEffect(Control target, Color color)
    {
        var rect = new ColorRect
        {
            Color = color,
            Size = new Vector2(86, 10),
            TopLevel = true,
            ZIndex = 180
        };
        _effectsLayer.AddChild(rect);

        var area = target.GetGlobalRect();
        rect.GlobalPosition = new Vector2(area.Position.X + area.Size.X * 0.5f - 43f, area.Position.Y + area.Size.Y * 0.45f);
        rect.RotationDegrees = -22f;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(rect, "scale", new Vector2(1.3f, 1f), 0.12f);
        tween.Parallel().TweenProperty(rect, "modulate:a", 0f, 0.18f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(rect))
            {
                rect.QueueFree();
            }
        };
    }

    private void SpawnShieldEffect(Control target, Color color)
    {
        var ring = new ColorRect
        {
            Color = new Color(color.R, color.G, color.B, 0.36f),
            Size = new Vector2(58, 58),
            TopLevel = true,
            ZIndex = 180
        };
        _effectsLayer.AddChild(ring);

        var area = target.GetGlobalRect();
        ring.GlobalPosition = new Vector2(area.Position.X + area.Size.X * 0.5f - 29f, area.Position.Y + area.Size.Y * 0.5f - 29f);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(ring, "scale", new Vector2(1.5f, 1.5f), 0.22f);
        tween.Parallel().TweenProperty(ring, "modulate:a", 0f, 0.22f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(ring))
            {
                ring.QueueFree();
            }
        };
    }

    private void SpawnRuneEffect(Control target, Color color)
    {
        var rune = new Label
        {
            Text = "✦",
            Modulate = color,
            TopLevel = true,
            ZIndex = 180
        };
        _effectsLayer.AddChild(rune);

        var area = target.GetGlobalRect();
        rune.GlobalPosition = new Vector2(area.Position.X + area.Size.X * 0.5f - 6f, area.Position.Y + area.Size.Y * 0.5f - 8f);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(rune, "global_position", rune.GlobalPosition + new Vector2(0f, -28f), 0.26f);
        tween.Parallel().TweenProperty(rune, "modulate:a", 0f, 0.26f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(rune))
            {
                rune.QueueFree();
            }
        };
    }

    private void PunchPanel(Control panel, float offsetX)
    {
        if (panel == _playerPanel)
        {
            _playerPunchX += offsetX;
            return;
        }

        _enemyPunchX += offsetX;
    }

    private async void ShakeMain(float intensity, int steps)
    {
        if (!IsInstanceValid(_mainMargin))
        {
            return;
        }

        if (IsFastMode)
        {
            return;
        }

        var original = _mainMargin.Position;
        for (var i = 0; i < steps; i++)
        {
            var x = (float)(_rng.NextDouble() * 2.0 - 1.0) * intensity;
            var y = (float)(_rng.NextDouble() * 2.0 - 1.0) * intensity * 0.5f;
            _mainMargin.Position = original + new Vector2(x, y);
            await ToSignal(GetTree().CreateTimer(0.012f), SceneTreeTimer.SignalName.Timeout);
        }

        _mainMargin.Position = original;
        // Cards are positioned in global space; after screen shake, force a re-layout
        // so they snap back to the correct fan positions.
        LayoutHandCards(false);
    }

    private async Task ShowTurnBanner(string text, Color tint)
    {
        if (IsFastMode)
        {
            _turnBanner.Visible = false;
            return;
        }

        _turnBannerLabel.Text = text;
        _turnBanner.Modulate = new Color(tint, 0f);
        _turnBanner.Visible = true;
        _turnBanner.Position = new Vector2(_turnBanner.Position.X, 20);

        var tweenIn = CreateTween();
        tweenIn.SetEase(Tween.EaseType.Out);
        tweenIn.SetTrans(Tween.TransitionType.Cubic);
        tweenIn.TweenProperty(_turnBanner, "position:y", 34f, 0.15f);
        tweenIn.Parallel().TweenProperty(_turnBanner, "modulate:a", 1f, 0.15f);
        await ToSignal(tweenIn, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(0.18f), SceneTreeTimer.SignalName.Timeout);

        var tweenOut = CreateTween();
        tweenOut.SetEase(Tween.EaseType.Out);
        tweenOut.SetTrans(Tween.TransitionType.Cubic);
        tweenOut.TweenProperty(_turnBanner, "modulate:a", 0f, 0.2f);
        await ToSignal(tweenOut, Tween.SignalName.Finished);
        _turnBanner.Visible = false;
    }

    private void SpawnFloatingText(Control target, string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            Modulate = color,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TopLevel = true,
            ZIndex = 200
        };

        _effectsLayer.AddChild(label);

        var targetRect = target.GetGlobalRect();
        var start = new Vector2(targetRect.Position.X + targetRect.Size.X * 0.5f - 40f, targetRect.Position.Y + 18f);
        label.GlobalPosition = start;

        var isDamage = text.StartsWith("-");
        var value = 0;
        if (isDamage)
        {
            int.TryParse(text.Replace("-", string.Empty), out value);
        }
        var isCritStyle = isDamage && value >= 12;
        label.Scale = isCritStyle ? new Vector2(1.25f, 1.25f) : Vector2.One;
        label.AddThemeFontSizeOverride("font_size", isCritStyle ? 28 : (isDamage ? 23 : 20));
        if (isCritStyle)
        {
            label.AddThemeColorOverride("font_color", new Color("fecaca"));
        }
        else if (isDamage)
        {
            label.AddThemeColorOverride("font_color", new Color("fca5a5"));
        }
        else if (text.Contains("Block", StringComparison.OrdinalIgnoreCase) || text.StartsWith("+"))
        {
            label.AddThemeColorOverride("font_color", new Color("93c5fd"));
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(label, "scale", label.Scale * (isCritStyle ? 1.15f : 1.07f), 0.12f);
        tween.Parallel().TweenProperty(label, "global_position", start + new Vector2(0f, -18f), 0.12f);
        tween.TweenProperty(label, "scale", isCritStyle ? new Vector2(1.12f, 1.12f) : Vector2.One, 0.1f);
        tween.Parallel().TweenProperty(label, "global_position", start + new Vector2(0f, -42f), 0.34f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.34f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(label))
            {
                label.QueueFree();
            }
        };
    }

    private void SpawnCardTrail(Vector2 from, Vector2 to)
    {
        var dir = to - from;
        var len = Math.Max(dir.Length(), 1f);
        var trail = new ColorRect
        {
            Color = new Color("7dd3fc"),
            Size = new Vector2(len, 3),
            TopLevel = true,
            ZIndex = 170
        };
        _effectsLayer.AddChild(trail);
        trail.GlobalPosition = from;
        trail.Rotation = dir.Angle();

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(trail, "modulate:a", 0f, 0.18f);
        tween.Parallel().TweenProperty(trail, "scale:y", 0.2f, 0.18f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(trail))
            {
                trail.QueueFree();
            }
        };
    }

    private void PulseImpact(Control target, float peakScale)
    {
        if (!IsInstanceValid(target))
        {
            return;
        }

        var originalScale = target.Scale;
        target.PivotOffset = target.Size * 0.5f;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(target, "scale", originalScale * peakScale, 0.06f);
        tween.TweenProperty(target, "scale", originalScale, 0.11f);
    }
}
