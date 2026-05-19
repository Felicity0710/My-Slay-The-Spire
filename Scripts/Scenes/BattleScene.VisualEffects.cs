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

    // Fire-and-forget player attack animation. Lunges toward the target,
    // invokes onImpact when the card reaches its apex (so damage text /
    // hit flash appear at the right moment), then recoils back to base.
    // ~220ms in, ~360ms out → committed attack feel without dragging.
    private void PlayPlayerAttackAnimation(Control target, System.Action onImpact)
    {
        if (!IsInstanceValid(target) || !IsInstanceValid(_playerPanel))
        {
            onImpact?.Invoke();
            return;
        }
        var playerCenter = _playerPanel.GetGlobalRect().GetCenter();
        var targetCenter = target.GetGlobalRect().GetCenter();
        var dir = (targetCenter - playerCenter);
        if (dir.LengthSquared() < 1f)
        {
            onImpact?.Invoke();
            return;
        }
        dir = dir.Normalized() * 70f;
        var dirX = dir.X;
        var dirY = dir.Y;

        var lungeIn = CreateTween();
        lungeIn.SetEase(Tween.EaseType.Out);
        lungeIn.SetTrans(Tween.TransitionType.Cubic);
        lungeIn.TweenMethod(Callable.From<float>(SetPlayerPunchX), _playerPunchX, dirX, 0.22f);
        lungeIn.Parallel().TweenMethod(Callable.From<float>(SetPlayerPunchY), _playerPunchY, dirY, 0.22f);
        lungeIn.Finished += () =>
        {
            onImpact?.Invoke();

            var recoil = CreateTween();
            recoil.SetEase(Tween.EaseType.Out);
            recoil.SetTrans(Tween.TransitionType.Cubic);
            recoil.TweenMethod(Callable.From<float>(SetPlayerPunchX), dirX, 0f, 0.36f);
            recoil.Parallel().TweenMethod(Callable.From<float>(SetPlayerPunchY), dirY, 0f, 0.36f);
        };
    }

    private void SetPlayerPunchX(float value) => _playerPunchX = value;
    private void SetPlayerPunchY(float value) => _playerPunchY = value;

    // Damage number that drops downward. Modeled directly on the proven
    // SpawnFloatingText pattern but flipped — single tween, no chain, no
    // pre-pop, just drop + fade so there's no chance of a tween-config quirk
    // hiding the label.
    private void SpawnFallingDamage(Control target, int damage, Color tint)
    {
        if (!IsInstanceValid(target) || damage <= 0)
        {
            return;
        }

        var label = new Label
        {
            Text = damage.ToString(),
            Modulate = tint,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TopLevel = true,
            ZIndex = 220
        };
        var isBig = damage >= 12;
        label.AddThemeFontSizeOverride("font_size", isBig ? 42 : 34);
        label.AddThemeColorOverride("font_color", tint);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 1f));
        label.AddThemeConstantOverride("outline_size", 6);

        _effectsLayer.AddChild(label);

        var targetRect = target.GetGlobalRect();
        var start = new Vector2(
            targetRect.Position.X + targetRect.Size.X * 0.5f - 26f,
            targetRect.Position.Y + targetRect.Size.Y * 0.25f);
        label.GlobalPosition = start;

        // Drop ~80px over ~1.0s with gravity easing, fade out in the back half
        // so the number lingers long enough to read before it disappears.
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(label, "global_position", start + new Vector2(0f, 80f), 1.0f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.65f).SetDelay(0.35f);
        tween.Finished += () =>
        {
            if (IsInstanceValid(label))
            {
                label.QueueFree();
            }
        };
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

        // Big art-style title centered over the arena. The label scales up
        // from 0.4× with a Back ease (overshoot), holds briefly, then fades
        // out while scaling down slightly — reads as a stage announcement.
        _turnBannerLabel.Text = text;
        _turnBannerLabel.AddThemeColorOverride("font_color", tint);
        _turnBanner.Visible = true;
        _turnBanner.Modulate = new Color(1f, 1f, 1f, 0f);
        _turnBanner.PivotOffset = _turnBanner.Size * 0.5f;
        _turnBanner.Scale = new Vector2(0.4f, 0.4f);

        var tweenIn = CreateTween();
        tweenIn.SetEase(Tween.EaseType.Out);
        tweenIn.SetTrans(Tween.TransitionType.Back);
        tweenIn.TweenProperty(_turnBanner, "scale", new Vector2(1.0f, 1.0f), 0.32f);
        tweenIn.Parallel().TweenProperty(_turnBanner, "modulate:a", 1f, 0.22f);
        await ToSignal(tweenIn, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(0.55f), SceneTreeTimer.SignalName.Timeout);

        var tweenOut = CreateTween();
        tweenOut.SetEase(Tween.EaseType.In);
        tweenOut.SetTrans(Tween.TransitionType.Cubic);
        tweenOut.TweenProperty(_turnBanner, "modulate:a", 0f, 0.28f);
        tweenOut.Parallel().TweenProperty(_turnBanner, "scale", new Vector2(0.92f, 0.92f), 0.28f);
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

        // Match SpawnFallingDamage's pacing — visible long enough to read,
        // fade delayed so the number is fully readable for ~0.4s before it
        // starts disappearing.
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(label, "scale", label.Scale * (isCritStyle ? 1.15f : 1.07f), 0.15f);
        tween.Parallel().TweenProperty(label, "global_position", start + new Vector2(0f, -22f), 0.18f);
        tween.TweenProperty(label, "scale", isCritStyle ? new Vector2(1.12f, 1.12f) : Vector2.One, 0.1f);
        tween.Parallel().TweenProperty(label, "global_position", start + new Vector2(0f, -80f), 0.85f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.6f).SetDelay(0.25f);
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
