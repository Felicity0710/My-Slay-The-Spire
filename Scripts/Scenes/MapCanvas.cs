using Godot;
using System.Collections.Generic;

public partial class MapCanvas : Control
{
    private readonly List<(Vector2 Start, Vector2 End, Color Tint)> _lines = new();

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.28f, 0.22f, 0.15f, 0.95f));

        var edgeColor = new Color(0.45f, 0.33f, 0.21f, 0.9f);
        DrawRect(new Rect2(Vector2.Zero, Size), edgeColor, false, 3f);

        var dotColor = new Color(0.41f, 0.31f, 0.2f, 0.22f);
        for (var i = 24f; i < Size.Y; i += 40f)
        {
            for (var j = 24f; j < Size.X; j += 40f)
            {
                DrawCircle(new Vector2(j, i), 1.1f, dotColor);
            }
        }

        foreach (var line in _lines)
        {
            DrawLine(line.Start, line.End, line.Tint, 3.5f, true);
            DrawLine(line.Start, line.End, new Color(0f, 0f, 0f, 0.12f), 6f, true);
        }
    }

    public void SetLines(IEnumerable<(Vector2 Start, Vector2 End, Color Tint)> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        QueueRedraw();
    }
}
