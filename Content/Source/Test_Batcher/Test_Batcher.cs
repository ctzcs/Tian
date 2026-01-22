using System.Numerics;
using Engine.Asset;
using Engine.Core;
using Engine.Core.Structure;
using Foster.Framework;
using Friflo.Engine.ECS;

namespace Content.Test_Batcher;

public class TestBatcher:IContent
{
    public Target Target { get; }
    public EntityStore World { get; set; }
    
    public App app { get; }
    public Batcher batcher { get; }
    
    public Vector2Int LogicResolution { get; }

    public TestBatcher(App app)
    {
        this.app = app;
        LogicResolution = Const._2K;
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }
    public void Start()
    {
        
    }

    public void Destroy()
    {
        batcher.Dispose();
        Target.Dispose();
        World = null;
        
    }

    public void Update()
    {
        
    }

    public void Render()
    {
        Target.Clear(Color.White);
        
        // Solid line -- 实线
        batcher.Line(new Vector2(50, 50), new Vector2(250, 50), 4f, Color.Black);

        // Gradient line -- 渐变线
        batcher.Line(new Vector2(50, 80), new Vector2(250, 80), 6f, Color.Red, Color.Blue);

        // Dashed line -- 虚线
        batcher.LineDashed(new Vector2(50, 110), new Vector2(250, 110), 4f, Color.DarkGray, 12f, 0.25f);

        // Rect outline (solid) -- 矩形轮廓线
        batcher.RectLine(new Rect(50, 150, 180, 80), 6f, Color.DarkGreen);

        // Rect outline (dashed) -- 矩形虚线轮廓线
        batcher.RectDashed(new Rect(50, 250, 180, 80), 6f, Color.ForestGreen, 10f, 0f);

        // Triangle outline -- 三角形轮廓线
        batcher.TriangleLine(new Vector2(50, 380), new Vector2(250, 380), new Vector2(150, 460), 6f, Color.Magenta);

        // Quad outline (convex arbitrary shape) -- 任意凸包四边形轮廓线
        var qa = new Vector2(680, 70);
        var qb = new Vector2(820, 70);
        var qc = new Vector2(850, 130);
        var qd = new Vector2(650, 130);
        batcher.QuadLine(qa, qb, qc, qd, 5f, Color.Orange);

        // Filled convex quad (not a rectangle) 凸多边形
        var qOff = new Vector2(0, -60);
        batcher.Quad(qa + qOff, qb + qOff, qc + qOff, qd + qOff, new Color(255, 200, 120, 180));
        
        // Filled convex polygon (triangle fan) + outline 
        {
            var center = new Vector2(860, 240);
            int sides = 6;
            float radius = 55f;
            float rot = 0.25f;
            var poly = new Vector2[sides];

            for (int i = 0; i < sides; i++)
                poly[i] = center + Calc.AngleToVector(rot + (i / (float)sides) * Calc.TAU, radius);

            for (int i = 1; i < sides - 1; i++)
                batcher.Triangle(poly[0], poly[i], poly[i + 1], new Color(120, 190, 255, 170));

            for (int i = 0; i < sides; i++)
                batcher.Line(poly[i], poly[(i + 1) % sides], 4f, Color.SteelBlue);
        }

        // Circle outline -- 圆轮廓线
        batcher.CircleLine(new Vector2(700, 220), 60f, 6f, 64, Color.Blue);

        // Semi-circle outline (arc) - 弧形轮廓线
        batcher.SemiCircleLine(new Vector2(700, 360), 0f, Calc.PI, 60f, 48, 6f, Color.CadetBlue);

        // Rounded rectangle (fill) -- 圆角矩形填充
        batcher.RectRounded(new Rect(320, 150, 180, 80), 20f, Color.LightSkyBlue);
        // Rounded rectangle (outline) -- 圆角矩形轮廓线
        batcher.RectRoundedLine(new Rect(320, 250, 180, 80), 20f, 6f, Color.LightSkyBlue);

        //圆角折线
        // 90° rounded corner using two trimmed lines plus a quarter arc -- 90°圆角：两段截断的直线 + 四分之一圆弧
        float r = 24f;
        // horizontal segment trimmed to leave arc
        batcher.Line(new Vector2(500, 220), new Vector2(560 - r, 220), 6f, Color.Black);
        // vertical segment trimmed
        batcher.Line(new Vector2(560, 220 + r), new Vector2(560, 280), 6f, Color.Black);
        // quarter-circle arc connecting them (center offset by radius from the corner)
        batcher.SemiCircleLine(new Vector2(560 - r, 220 + r), -Calc.PI * 0.5f, 0f, r, 32, 6f, Color.Black);

        // Long slanted lines to compare styles -- 长斜线
        batcher.Line(new Vector2(500, 420), new Vector2(850, 520), 4f, Color.SaddleBrown);
        batcher.LineDashed(new Vector2(500, 450), new Vector2(850, 550), 4f, Color.Sienna, 14f, 0.1f);

        // Cubic Bezier curve -- 贝塞尔曲线
        static Vector2 BezierCubic(in Vector2 p0, in Vector2 p1, in Vector2 p2, in Vector2 p3, float t)
        {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            return (uu * u) * p0 + (3f * uu * t) * p1 + (3f * u * tt) * p2 + (tt * t) * p3;
        }
        
        var b0 = new Vector2(120, 600);
        var b1 = new Vector2(240, 480);
        var b2 = new Vector2(320, 720);
        var b3 = new Vector2(460, 620);

        var prev = b0;
        int steps = 64;
        float lineWeight = 6f;
        for (int s = 1; s <= steps; s++)
        {
            float t = s / (float)steps;
            var curr = BezierCubic(b0, b1, b2, b3, t);
            batcher.Line(prev, curr, lineWeight, Color.Purple);
            prev = curr;
        }
        
        batcher.Text("Batcher extras", new Vector2(980, 20), 24f, Color.Black);

        batcher.Rect(new Rect(980, 60, 220, 90), Color.LightPink);
        batcher.Rect(new Rect(1220, 60, 220, 90), Color.Red, Color.Yellow, Color.Cyan, Color.Blue);

        batcher.Triangle(new Vector2(980, 180), new Vector2(1200, 180), new Vector2(1090, 300), Color.LightGreen);
        batcher.Triangle(new Vector2(1220, 180), new Vector2(1440, 180), new Vector2(1330, 300), Color.Red, Color.Green, Color.Blue);

        batcher.Circle(new Vector2(1090, 420), 60f, 64, Color.White, Color.DeepSkyBlue);
        batcher.CircleDashed(new Vector2(1330, 420), 70f, 8f, 64, Color.DarkSlateGray, 18f, 0f);

        batcher.RadialBar(new Vector2(1090, 560), 0.72f, 36f, 70f, Color.OrangeRed);
        batcher.CheckeredPattern(new Rect(1220, 500, 240, 160), 20f, 20f, new Color(220, 220, 220, 255), new Color(180, 180, 180, 255));

        batcher.PushBlend(BlendMode.Add);
        batcher.Circle(new Vector2(1330, 560), 50f, 48, new Color(255, 0, 0, 120));
        batcher.Circle(new Vector2(1370, 560), 50f, 48, new Color(0, 0, 255, 120));
        batcher.PopBlend();

        var scissor = new RectInt(980, 700, 220, 120);
        batcher.PushScissor(scissor);
        batcher.Circle(new Vector2(1090, 760), 90f, 64, new Color(0, 120, 255, 255));
        batcher.Rect(new Rect(980, 700, 320, 180), new Color(255, 0, 0, 80));
        batcher.PopScissor();
        batcher.RectLine(new Rect(scissor.X, scissor.Y, scissor.Width, scissor.Height), 2f, Color.Red);

        batcher.PushMatrix(new Vector2(1330, 760), new Vector2(80, 40), Vector2.One, Calc.PI * 0.15f);
        batcher.Rect(new Rect(0, 0, 160, 80), new Color(30, 30, 30, 200));
        batcher.PopMatrix();

        var st = Assets.GetSubtexture("frog/0");
        if (!st.IsEmpty)
        {
            batcher.Image(st, new Vector2(1540, 60), Color.White);
            batcher.ImageStretch(st, new Rect(1540, 170, 180, 120), Color.White);
            batcher.ImageFit(st, new Rect(1540, 310, 180, 120), new Vector2(0.5f, 0.5f), Color.White, false, false);

            var clip = new Rect(0, 0, st.Width * 0.5f, st.Height * 0.5f);
            batcher.Image(st, clip, new Vector2(1540, 460), Vector2.Zero, Vector2.One, 0f, Color.White);

            batcher.PushMode(Batcher.Modes.Wash);
            batcher.Image(st, new Vector2(1720, 60), new Color(0, 255, 0, 255));
            batcher.PopMode();
        }

        batcher.Text("Text / wrapped: 你好 123 ABC\nSecond line", new Vector2(980, 860), 22f, Color.Black);
        batcher.TextWrapped("TextWrapped: long long long long long long long long long long long long long", 320f, new Vector2(980, 920), 18f, Color.Black);

        batcher.Render(Target);
        batcher.Clear();
    }

    
}