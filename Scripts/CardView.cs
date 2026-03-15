using Godot;
using System;
using System.Threading.Tasks;

public partial class CardView : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _costLabel = null!;
    private Label _descLabel = null!;

    private bool _dragging;
    private Vector2 _dragOffset;
    private Vector2 _pressStartMouse;
    private Vector2 _homeGlobalPosition;

    public bool IsDragging => _dragging;

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
        _homeGlobalPosition = globalPosition;
        Size = CustomMinimumSize;
        if (_dragging)
        {
            return;
        }

        if (!animate)
        {
            GlobalPosition = globalPosition;
            RotationDegrees = rotationDegrees;
            Scale = poseScale;
            return;
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", globalPosition, 0.12f);
        tween.Parallel().TweenProperty(this, "rotation_degrees", rotationDegrees, 0.12f);
        tween.Parallel().TweenProperty(this, "scale", poseScale, 0.12f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _pressStartMouse = GetGlobalMousePosition();
                _dragging = true;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;
                RotationDegrees = 0;
                Scale = Vector2.One;
                ZIndex = 100;
                DragStarted(this);
                AcceptEvent();
            }
            else if (_dragging)
            {
                _dragging = false;
                var moved = _pressStartMouse.DistanceTo(GetGlobalMousePosition()) > 14f;
                DragEnded(this);

                if (moved)
                {
                    DropAttempted(this, GetGlobalMousePosition());
                }
                else
                {
                    Clicked(this);
                }

                AcceptEvent();
            }
        }
        else if (@event is InputEventMouseMotion && _dragging)
        {
            GlobalPosition = GetGlobalMousePosition() - _dragOffset;
            DragMoved(this, GetGlobalMousePosition());
            AcceptEvent();
        }
    }

    public async Task AnimateBackToHand(float duration = 0.14f)
    {
        if (!IsInsideTree())
        {
            return;
        }

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
        }
    }

    public async Task AnimateToTarget(Vector2 targetGlobalPosition, float duration = 0.12f)
    {
        if (!IsInsideTree())
        {
            return;
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", targetGlobalPosition, duration);
        tween.Parallel().TweenProperty(this, "scale", new Vector2(0.86f, 0.86f), duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task AnimateFromDraw(Vector2 fromGlobalPosition, float duration = 0.2f)
    {
        if (!IsInsideTree())
        {
            return;
        }

        var targetPos = _homeGlobalPosition;
        var targetRot = RotationDegrees;
        var targetScale = Scale;

        GlobalPosition = fromGlobalPosition;
        RotationDegrees = 0f;
        Scale = new Vector2(0.72f, 0.72f);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", targetPos, duration);
        tween.Parallel().TweenProperty(this, "rotation_degrees", targetRot, duration);
        tween.Parallel().TweenProperty(this, "scale", targetScale, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
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
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        _nameLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _nameLabel.AddThemeColorOverride("font_color", new Color("e2e8f0"));

        var costBadge = new PanelContainer();
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
        _costLabel.AddThemeColorOverride("font_color", new Color("93c5fd"));
        costBadge.AddChild(_costLabel);

        _descLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _descLabel.AddThemeColorOverride("font_color", new Color("cbd5e1"));

        vbox.AddChild(_nameLabel);
        vbox.AddChild(costBadge);
        vbox.AddChild(_descLabel);

        RefreshText();
    }

    private void RefreshText()
    {
        _nameLabel.Text = Card.Name;
        _costLabel.Text = $"Cost: {Card.Cost}";
        _descLabel.Text = Card.Description;
    }
}
