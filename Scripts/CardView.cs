using Godot;
using System;
using System.Threading.Tasks;

public partial class CardView : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _costLabel = null!;
    private RichTextLabel _descLabel = null!;

    private bool _dragging;
    private bool _playable = true;
    private Vector2 _dragOffset;
    private Vector2 _pressStartMouse;
    private Vector2 _homeGlobalPosition;
    private bool _dragResolvedThisPress;
    private Vector2 _targetGlobalPosition;
    private float _targetRotationDegrees;
    private Vector2 _targetScale = Vector2.One;
    private bool _manualAnimating;
    private float _dragTilt;
    private bool _hoverFocused;
    private bool _hoverDimmed;
    private bool _lockPositionWhileDragging;

    public bool IsDragging => _dragging;
    public bool LockPositionWhileDragging
    {
        get => _lockPositionWhileDragging;
        set => _lockPositionWhileDragging = value;
    }

    public CardData Card { get; private set; } = CardData.CreateById("strike");

    public Action<CardView, Vector2> DropAttempted = (_, _) => { };
    public Action<CardView> Clicked = _ => { };
    public Action<CardView, Vector2> DragMoved = (_, _) => { };
    public Action<CardView> DragStarted = _ => { };
    public Action<CardView> DragEnded = _ => { };
    public Action<CardView, bool> HoverChanged = (_, _) => { };

    public override void _Ready()
    {
        BuildUi();
        MouseFilter = MouseFilterEnum.Stop;
        TopLevel = true;
        Size = CustomMinimumSize;
        SetProcessInput(true);
        SetProcess(true);
        _targetGlobalPosition = GlobalPosition;
        _targetRotationDegrees = RotationDegrees;
        _targetScale = Scale;

        MouseEntered += () =>
        {
            if (!_dragging)
            {
                HoverChanged(this, true);
            }
        };

        MouseExited += () =>
        {
            if (!_dragging)
            {
                HoverChanged(this, false);
            }
        };
    }

    public void Setup(CardData card)
    {
        Card = card;
        if (_nameLabel != null)
        {
            RefreshText();
        }
    }

    public void SetPose(Vector2 globalPosition, float rotationDegrees, Vector2 poseScale, bool animate)
    {
        var uniform = (poseScale.X + poseScale.Y) * 0.5f;
        var uniformScale = new Vector2(uniform, uniform);
        _homeGlobalPosition = globalPosition;
        _targetGlobalPosition = globalPosition;
        _targetRotationDegrees = rotationDegrees;
        _targetScale = uniformScale;
        Size = CustomMinimumSize;
        if (_dragging)
        {
            return;
        }

        if (!animate)
        {
            GlobalPosition = globalPosition;
            RotationDegrees = rotationDegrees;
            Scale = uniformScale;
        }
    }

    public void SetPlayable(bool playable)
    {
        _playable = playable;
        UpdateModulate();
    }

    public void SetFocusState(bool focused, bool dimmed)
    {
        _hoverFocused = focused;
        _hoverDimmed = dimmed;
        UpdateModulate();
    }

    public void PrepareForReuse()
    {
        _dragging = false;
        _dragResolvedThisPress = false;
        _manualAnimating = false;
        _dragTilt = 0f;
        _hoverFocused = false;
        _hoverDimmed = false;
        _lockPositionWhileDragging = false;
        _playable = true;
        ZIndex = 0;
        RotationDegrees = 0f;
        Scale = Vector2.One;
        _targetGlobalPosition = GlobalPosition;
        _targetRotationDegrees = 0f;
        _targetScale = Vector2.One;
        UpdateModulate();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _pressStartMouse = GetGlobalMousePosition();
                _dragging = true;
                _dragResolvedThisPress = false;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;
                RotationDegrees = 0;
                Scale = Vector2.One;
                ZIndex = 100;
                DragStarted(this);
                AcceptEvent();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_dragging || !IsInsideTree())
        {
            return;
        }

        if (@event is InputEventMouseMotion)
        {
            if (@event is InputEventMouseMotion motion)
            {
                _dragTilt = Mathf.Clamp(motion.Relative.X * 0.45f, -14f, 14f);
                RotationDegrees = Mathf.Lerp(RotationDegrees, _dragTilt, 0.42f);
            }
            if (!_lockPositionWhileDragging)
            {
                GlobalPosition = GetGlobalMousePosition() - _dragOffset;
            }
            DragMoved(this, GetGlobalMousePosition());
            var viewport = GetViewport();
            if (viewport != null)
            {
                viewport.SetInputAsHandled();
            }
            return;
        }

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
        {
            ResolveDragRelease();
            var viewport = GetViewport();
            if (viewport != null)
            {
                viewport.SetInputAsHandled();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Size != CustomMinimumSize)
        {
            Size = CustomMinimumSize;
        }

        if (_dragging || _manualAnimating)
        {
            return;
        }

        var t = 1f - Mathf.Exp(-(float)delta * 18f);
        GlobalPosition = GlobalPosition.Lerp(_targetGlobalPosition, t);
        RotationDegrees = Mathf.Lerp(RotationDegrees, _targetRotationDegrees, t);
        Scale = Scale.Lerp(_targetScale, t);
    }

    public async Task AnimateBackToHand(float duration = 0.14f)
    {
        if (!IsInsideTree())
        {
            return;
        }

        _manualAnimating = true;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", _homeGlobalPosition, duration);
        tween.Parallel().TweenProperty(this, "scale", Vector2.One, duration);
        tween.Parallel().TweenProperty(this, "rotation_degrees", 0f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);

        if (IsInsideTree())
        {
            ZIndex = 0;
            _targetGlobalPosition = _homeGlobalPosition;
            _targetRotationDegrees = 0f;
            _targetScale = Vector2.One;
        }
        _manualAnimating = false;
    }

    private void ResolveDragRelease()
    {
        if (!_dragging || _dragResolvedThisPress)
        {
            return;
        }

        _dragResolvedThisPress = true;
        _dragging = false;
        var moved = _pressStartMouse.DistanceTo(GetGlobalMousePosition()) > 6f;
        DragEnded(this);

        if (moved)
        {
            DropAttempted(this, GetGlobalMousePosition());
            return;
        }

        Clicked(this);
    }

    private void UpdateModulate()
    {
        if (_dragging)
        {
            Modulate = Colors.White;
            return;
        }

        var color = _playable ? Colors.White : new Color(0.62f, 0.62f, 0.68f, 0.92f);
        if (_hoverFocused)
        {
            color = new Color(
                Mathf.Clamp(color.R * 1.05f, 0f, 1f),
                Mathf.Clamp(color.G * 1.05f, 0f, 1f),
                Mathf.Clamp(color.B * 1.05f, 0f, 1f),
                color.A);
        }
        if (_hoverDimmed)
        {
            color = new Color(color.R * 0.78f, color.G * 0.78f, color.B * 0.78f, Mathf.Min(color.A, 0.86f));
        }
        Modulate = color;
    }

    public async Task AnimateToTarget(Vector2 targetGlobalPosition, float duration = 0.12f)
    {
        if (!IsInsideTree())
        {
            return;
        }

        _manualAnimating = true;
        var start = GlobalPosition;
        var end = targetGlobalPosition;
        var mid = (start + end) * 0.5f;
        var control = mid + new Vector2(0f, -Mathf.Clamp(Mathf.Abs(end.X - start.X) * 0.16f + 80f, 80f, 150f));
        var startRot = RotationDegrees;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenMethod(Callable.From<float>(p =>
            {
                var a = start.Lerp(control, p);
                var b = control.Lerp(end, p);
                GlobalPosition = a.Lerp(b, p);
                RotationDegrees = Mathf.Lerp(startRot, 0f, p);
            }),
            0f,
            1f,
            duration);
        tween.Parallel().TweenProperty(this, "scale", new Vector2(0.86f, 0.86f), duration);
        await ToSignal(tween, Tween.SignalName.Finished);
        _targetGlobalPosition = GlobalPosition;
        _targetRotationDegrees = RotationDegrees;
        _targetScale = Scale;
        _manualAnimating = false;
    }

    public async Task AnimateFromDraw(Vector2 fromGlobalPosition, float duration = 0.2f)
    {
        if (!IsInsideTree())
        {
            return;
        }

        var targetPos = _targetGlobalPosition;
        var targetRot = _targetRotationDegrees;
        var targetScale = _targetScale;

        GlobalPosition = fromGlobalPosition;
        RotationDegrees = 0f;
        Scale = new Vector2(0.72f, 0.72f);

        _manualAnimating = true;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", targetPos, duration);
        tween.Parallel().TweenProperty(this, "rotation_degrees", targetRot, duration);
        tween.Parallel().TweenProperty(this, "scale", targetScale, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
        _manualAnimating = false;
    }

    private void BuildUi()
    {
        CustomMinimumSize = new Vector2(180, 210);

        var style = new StyleBoxFlat
        {
            BgColor = new Color("18212b"),
            BorderColor = new Color("7aa8cf"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ShadowColor = new Color(0f, 0f, 0f, 0.45f),
            ShadowSize = 5
        };
        AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.MouseFilter = MouseFilterEnum.Ignore;
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        _nameLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        _nameLabel.AddThemeColorOverride("font_color", new Color("e2e8f0"));

        var costBadge = new PanelContainer();
        costBadge.MouseFilter = MouseFilterEnum.Ignore;
        var costStyle = new StyleBoxFlat
        {
            BgColor = new Color("0d2538"),
            BorderColor = new Color("7dd3fc"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        costBadge.AddThemeStyleboxOverride("panel", costStyle);

        _costLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _costLabel.MouseFilter = MouseFilterEnum.Ignore;
        _costLabel.AddThemeColorOverride("font_color", new Color("93c5fd"));
        costBadge.AddChild(_costLabel);

        _descLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = false,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 96)
        };
        _descLabel.MouseFilter = MouseFilterEnum.Ignore;
        _descLabel.AddThemeColorOverride("default_color", new Color("cbd5e1"));

        vbox.AddChild(_nameLabel);
        vbox.AddChild(costBadge);
        vbox.AddChild(_descLabel);

        RefreshText();
    }

    private void RefreshText()
    {
        _nameLabel.Text = Card.Name;
        _costLabel.Text = $"Cost: {Card.Cost}";
        var text = Card.Description
            .Replace("Deal", "[color=#fca5a5]Deal[/color]")
            .Replace("Gain", "[color=#93c5fd]Gain[/color]")
            .Replace("Block", "[color=#93c5fd]Block[/color]")
            .Replace("Vulnerable", "[color=#e9d5ff]Vulnerable[/color]")
            .Replace("Draw", "[color=#a5f3fc]Draw[/color]")
            .Replace("damage", "[color=#fda4af]damage[/color]");
        _descLabel.Text = text;
    }
}
