using Godot;
using System.Collections.Generic;

public partial class MapCanvas : Control
{
    private const int CurveSegments = 18;
    private const float CurveOffsetRatio = 0.09f;
    private const float LineWidth = 1.1f;
    private const float DashLength = 6f;
    private const float GapLength = 5f;

    // Seed is a zoom-invariant identifier for the edge (derived from node
    // row/col indices). The curve's bend direction keys off Seed instead of
    // the on-screen midpoint, so zooming never flips a road to the other side.
    private readonly List<(Vector2 Start, Vector2 End, Color Tint, int Seed)> _lines = new();

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
            var points = BuildCurve(line.Start, line.End, line.Seed);
            DrawDashedCurve(points, line.Tint, LineWidth, DashLength, GapLength);
        }
    }

    public void SetLines(IEnumerable<(Vector2 Start, Vector2 End, Color Tint, int Seed)> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        QueueRedraw();
    }

    // Subtle quadratic-Bezier arc between two map nodes. The control point sits
    // perpendicular to the segment with a deterministic sign keyed off the
    // edge's stable Seed (node indices) — NOT the on-screen midpoint — so the
    // arc keeps its shape and bend direction at every zoom level.
    private static Vector2[] BuildCurve(Vector2 start, Vector2 end, int seed)
    {
        var diff = end - start;
        var length = diff.Length();
        if (length < 0.5f)
        {
            return new[] { start, end };
        }

        var perp = new Vector2(-diff.Y, diff.X) / length;
        var midpoint = (start + end) * 0.5f;
        var sign = (seed & 1) == 0 ? 1f : -1f;
        var offset = length * CurveOffsetRatio * sign;
        var control = midpoint + perp * offset;

        var points = new Vector2[CurveSegments + 1];
        for (var i = 0; i <= CurveSegments; i++)
        {
            var t = i / (float)CurveSegments;
            var omt = 1f - t;
            points[i] = omt * omt * start + 2f * omt * t * control + t * t * end;
        }
        return points;
    }

    // Walks the polyline by arc length, emitting fixed-length dash segments
    // separated by gaps. Handles dashes that straddle multiple polyline edges
    // because the underlying curve is approximated by many short segments.
    private void DrawDashedCurve(Vector2[] points, Color color, float width, float dashLength, float gapLength)
    {
        if (points.Length < 2)
        {
            return;
        }

        var cumulative = new float[points.Length];
        cumulative[0] = 0f;
        for (var i = 1; i < points.Length; i++)
        {
            cumulative[i] = cumulative[i - 1] + (points[i] - points[i - 1]).Length();
        }
        var total = cumulative[^1];
        if (total < 0.5f)
        {
            return;
        }

        Vector2 Sample(float dist)
        {
            if (dist <= 0f) return points[0];
            if (dist >= total) return points[^1];
            for (var i = 1; i < points.Length; i++)
            {
                if (cumulative[i] >= dist)
                {
                    var span = cumulative[i] - cumulative[i - 1];
                    var t = span <= 0f ? 0f : (dist - cumulative[i - 1]) / span;
                    return points[i - 1].Lerp(points[i], t);
                }
            }
            return points[^1];
        }

        var step = dashLength + gapLength;
        for (var d = 0f; d < total; d += step)
        {
            var a = Sample(d);
            var b = Sample(Mathf.Min(d + dashLength, total));
            DrawLine(a, b, color, width, true);
        }
    }
}
