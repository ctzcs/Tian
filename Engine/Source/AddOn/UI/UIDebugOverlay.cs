using System.Numerics;
using Engine.Asset;
using Foster.Framework;

namespace Engine.UI;

public class UIDebugOverlay
{
    public bool Enabled = true;
    public Color BoxColor = new Color(0, 200, 255, 120);
    public Color TextColor = Color.Yellow;
    public Color HighlightColor = new Color(255, 255, 0, 220);
    public Color PanelBgColor = new Color(0, 0, 0, 180);
    public float Thickness = 1f;

    public void Render(Batcher batcher, UIRoot uiRoot)
    {
        if (!Enabled) return;

        var root = uiRoot.Root;
        RenderOutline(batcher, root);

        var hovered = uiRoot.DebugLastOver;
        if (hovered != null)
            RenderInfoPanel(batcher, hovered);
    }

    void RenderOutline(Batcher batcher, UIElement element)
    {
        if (!element.Visible) return;

        var wr = element.WorldRect;
        DrawRectOutline(batcher, wr, BoxColor, Thickness);

        foreach (var child in element.Children)
            RenderOutline(batcher, child);
    }

    void RenderInfoPanel(Batcher batcher, UIElement element)
    {
        if (Assets.Font == null)
            return;

        float lineH = Assets.Font.Height + Assets.Font.LineGap;
        var start = new Vector2(10, 10);
        var p = start;

        var wr = element.WorldRect;
        var info1 = $"{element.GetType().Name} [{wr.X:0},{wr.Y:0},{wr.Width:0}x{wr.Height:0}]";
        var info2 = $"Ratio W:{element.WidthRatioToParent:0.##} H:{element.HeightRatioToParent:0.##} PosX:{element.XRatioToParent:0.##} PosY:{element.YRatioToParent:0.##}";
        var info3 = $"Grow X:{element.GrowX:0.##} Y:{element.GrowY:0.##} Min({element.MinWidth:0},{element.MinHeight:0}) Max({element.MaxWidth:0},{element.MaxHeight:0})";

        // 简单估算面板宽度
        var maxText = info1.Length > info2.Length ? info1 : info2;
        if (info3.Length > maxText.Length) maxText = info3;
        var size = Assets.Font.SizeOf(maxText.AsSpan());
        var panelRect = new Rect(start.X - 4, start.Y - 4, size.X + 8, lineH * 3 + 4);
        batcher.Quad(new Quad(panelRect), PanelBgColor);

        batcher.Text(Assets.Font, info1.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(Assets.Font, info2.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(Assets.Font, info3.AsSpan(), p, new Vector2(0, 0), TextColor);

        // 高亮当前元素轮廓
        DrawRectOutline(batcher, wr, HighlightColor, Thickness + 1f);
    }

    static void DrawRectOutline(Batcher batcher, Rect r, Color c, float t)
    {
        if (t <= 0f) t = 1f;
        batcher.Quad(new Quad(new Rect(r.X, r.Y, r.Width, t)), c);
        batcher.Quad(new Quad(new Rect(r.X, r.Y + r.Height - t, r.Width, t)), c);
        batcher.Quad(new Quad(new Rect(r.X, r.Y, t, r.Height)), c);
        batcher.Quad(new Quad(new Rect(r.X + r.Width - t, r.Y, t, r.Height)), c);
    }
}


/* // 字段
private UIDebugOverlay uiDebug = new UIDebugOverlay();

// 渲染阶段：在 UI 绘制之后调用 Overlay
if (uiRoot.IsOpen)
{
    uiRoot.Render(batcher);
    uiDebug.Render(batcher, uiRoot);
    batcher.Render(target);
}

// 可选热键开关
if (ctx.Input.Keyboard.Pressed(Keys.O))
    uiDebug.Enabled = !uiDebug.Enabled; */