
using Box2D.NET;
using Foster.Framework;
using System.Numerics;
using static Box2D.NET.B2MathFunction;

public class DebugContext
{
    private readonly Batcher _batcher;
    private Matrix3x2 _view;
    private Matrix3x2 _worldToPixel;
    private readonly float _lineWeight;
    private readonly int _circleSteps;

    public B2DebugDraw DebugDraw { get; }

    public DebugContext(Batcher batcher, float lineWeight = 2f, int circleSteps = 32)
    {
        _batcher = batcher;
        _view = Matrix3x2.Identity;
        _worldToPixel = _view;
        _lineWeight = lineWeight;
        _circleSteps = circleSteps;

        var draw = B2Types.b2DefaultDebugDraw();
        draw.DrawPolygonFcn = DrawPolygon;
        draw.DrawSolidPolygonFcn = DrawSolidPolygon;
        draw.DrawCircleFcn = DrawCircle;
        draw.DrawSolidCircleFcn = DrawSolidCircle;
        draw.DrawSolidCapsuleFcn = DrawSolidCapsule;
        draw.drawLineFcn = DrawLine;
        draw.DrawTransformFcn = DrawTransform;
        draw.DrawPointFcn = DrawPoint;
        draw.DrawStringFcn = DrawString;
        draw.context = this;
        DebugDraw = draw;
    }

    public void SetView(Matrix3x2 view)
    {
        _view = view;
        _worldToPixel = view;
    }

    private static Color ToColor(B2HexColor c) => new Color((int)c);
    private static Vector2 ToPixels(DebugContext ext, in B2Vec2 v)
        => Vector2.Transform(new Vector2(v.X, v.Y), ext._worldToPixel);

    private static void DrawPolygon(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context)
    {
        var ext = (DebugContext)context;
        var col = ToColor(color);
        if (vertexCount <= 1) return;
        var prev = ToPixels(ext, vertices[0]);
        for (int i = 1; i < vertexCount; i++)
        {
            var curr = ToPixels(ext, vertices[i]);
            ext._batcher.Line(prev, curr, ext._lineWeight, col);
            prev = curr;
        }
        var first = ToPixels(ext, vertices[0]);
        ext._batcher.Line(prev, first, ext._lineWeight, col);
    }

    private static void DrawSolidPolygon(in B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context)
    {
        var ext = (DebugContext)context;
        var col = ToColor(color);
        if (vertexCount < 3) return;
        var a = ToPixels(ext, b2TransformPoint(transform, vertices[0]));
        for (int i = 1; i < vertexCount - 1; i++)
        {
            var b = ToPixels(ext, b2TransformPoint(transform, vertices[i]));
            var c = ToPixels(ext, b2TransformPoint(transform, vertices[i + 1]));
            ext._batcher.Triangle(a, b, c, col);
        }
    }

    private static void DrawCircle(in B2Vec2 center, float radius, B2HexColor color, object context)
    {
        var ext = (DebugContext)context;
        var col = ToColor(color);
        var c = ToPixels(ext, center);
        ext._batcher.CircleLine(c, radius, ext._lineWeight, ext._circleSteps, col);
    }

    private static void DrawSolidCircle(in B2Transform transform, float radius, B2HexColor color, object context)
    {
        var ext = (DebugContext)context;
        var col = ToColor(color);
        var c = ToPixels(ext, transform.p);
        ext._batcher.Circle(c, radius, ext._circleSteps, col);
    }

    private static void DrawSolidCapsule(in B2Vec2 p1, in B2Vec2 p2, float radius, B2HexColor color, object context)
    {
        var ext = (DebugContext)context;
        var col = ToColor(color);
        var a = ToPixels(ext, p1);
        var b = ToPixels(ext, p2);
        var r = radius;
        var d = b - a;
        if (d.LengthSquared() < float.Epsilon)
        {
            ext._batcher.Circle(a, r, ext._circleSteps, col);
            return;
        }
        var n = Vector2.Normalize(new Vector2(-d.Y, d.X)) * r;
        ext._batcher.Quad(a - n, b - n, b + n, a + n, col);
        ext._batcher.Circle(a, r, ext._circleSteps, col);
        ext._batcher.Circle(b, r, ext._circleSteps, col);
    }

    private static void DrawLine(in B2Vec2 p1, in B2Vec2 p2, B2HexColor color, object context)
    {
        var ext = (DebugContext)context;
        var col = ToColor(color);
        ext._batcher.Line(ToPixels(ext, p1), ToPixels(ext, p2), ext._lineWeight, col);
    }

    private static void DrawTransform(in B2Transform transform, object context)
    {
        var ext = (DebugContext)context;
        var origin = ToPixels(ext, transform.p);
        float len = 0.5f;
        var x = ToPixels(ext, b2MulAdd(transform.p, len, b2Rot_GetXAxis(transform.q)));
        var y = ToPixels(ext, b2MulAdd(transform.p, len, b2Rot_GetYAxis(transform.q)));
        ext._batcher.Line(origin, x, ext._lineWeight, Color.Red);
        ext._batcher.Line(origin, y, ext._lineWeight, Color.Green);
    }

    private static void DrawPoint(in B2Vec2 p, float size, B2HexColor color, object context)
    {
        var ext = (DebugContext)context;
        var col = ToColor(color);
        ext._batcher.Circle(ToPixels(ext, p), size, Math.Max(8, ext._circleSteps), col);
    }

    private static void DrawString(in B2Vec2 p, string s, B2HexColor color, object context) {}
}