using System.Numerics;
using Engine.UI_2;
using Foster.Framework;

public class UIDebugger
{
    public bool Enabled = true;
    public Color BoxColor = new Color(0, 200, 255, 120);
    public Color TextColor = Color.Yellow;
    public Color HighlightColor = new Color(255, 255, 0, 220);
    public Color SelectedColor = new Color(255, 80, 180, 220);
    public Color PanelBgColor = new Color(0, 0, 0, 180);
    public bool ShowInfoPanel = false;
    public float Thickness = 1f;
    public UIElement? SelectedElement;

    private UIElement? lastLoggedHovered;

    public void Render(Batcher batcher, UICanvas canvas, SpriteFont? font)
    {
        if (!Enabled)
            return;

        RenderOutline(batcher, canvas.Root);

        var hovered = canvas.DebugHovered;
        if (hovered != null)
        {
            LogHovered(hovered);
            if (ShowInfoPanel)
                RenderInfoPanel(batcher, hovered, hovered.GetWorldRect(), font);
        }
    }

    public void Render(Batcher batcher, UICanvas canvas, Rect viewport, Rect outputViewport, SpriteFont? font)
    {
        if (!Enabled)
            return;

        var clip = TransformRect((canvas.ClipRect ?? viewport).GetIntersection(viewport), viewport, outputViewport);
        if (clip.Width <= 0f || clip.Height <= 0f)
            return;

        float scaleX = outputViewport.Width / viewport.Width;
        float scaleY = outputViewport.Height / viewport.Height;
        var matrix = Matrix3x2.CreateScale(scaleX, scaleY)
                     * Matrix3x2.CreateTranslation(outputViewport.X - viewport.X * scaleX, outputViewport.Y - viewport.Y * scaleY);

        var hovered = canvas.DebugHovered;
        batcher.PushScissor(clip.Int());
        batcher.PushMatrix(matrix, true);
        RenderOutline(batcher, canvas.Root);
        if (SelectedElement != null && SelectedElement.Visible && SelectedElement.Display)
            DrawRectOutline(batcher, SelectedElement.GetWorldRect(), SelectedColor, Thickness + 2f);
        if (hovered != null)
            DrawRectOutline(batcher, hovered.GetWorldRect(), HighlightColor, Thickness + 1f);
        batcher.PopMatrix();
        batcher.PopScissor();

        if (hovered != null)
        {
            LogHovered(hovered);
            if (ShowInfoPanel)
                RenderInfoPanel(batcher, hovered, TransformRect(hovered.GetWorldRect(), viewport, outputViewport), font);
        }
    }

    void RenderOutline(Batcher batcher, UIElement element)
    {
        if (!element.Visible || !element.Display)
            return;

        var rect = element.GetWorldRect();
        DrawRectOutline(batcher, rect, BoxColor, Thickness);

        foreach (var child in element.Children)
            RenderOutline(batcher, child);
    }

    void LogHovered(UIElement hovered)
    {
        if (hovered == lastLoggedHovered)
            return;

        lastLoggedHovered = hovered;

        var wr = hovered.GetWorldRect();
        var layout = hovered.Layout;
        var interactable = hovered.Interactable ? "true" : "false";

        Log.Info(
            $"[UI2 Hover] {hovered.GetType().Name} " +
            $"Rect({wr.X:0},{wr.Y:0},{wr.Width:0}x{wr.Height:0}) " +
            $"Grow:{layout.Grow:0.##} " +
            $"Min({layout.MinWidth:0},{layout.MinHeight:0}) " +
            $"Max({layout.MaxWidth:0},{layout.MaxHeight:0}) " +
            $"Interactable:{interactable}");
    }

    void RenderInfoPanel(Batcher batcher, UIElement element, Rect displayRect, SpriteFont? font)
    {
        if (font == null)
            return;

        float lineH = font.Height + font.LineGap;
        var start = new Vector2(10, 10);
        var p = start;

        var wr = displayRect;
        var info1 = $"{element.GetType().Name} [{wr.X:0},{wr.Y:0},{wr.Width:0}x{wr.Height:0}]";
        var layout = element.Layout;
        var info2 = $"Grow:{layout.Grow:0.##} Min({layout.MinWidth:0},{layout.MinHeight:0}) Max({layout.MaxWidth:0},{layout.MaxHeight:0})";
        var info3 = $"Hover:true Interactable:{(element.Interactable ? "true" : "false")}";
        var info4 = $"Padding L:{layout.PaddingLeft:0} R:{layout.PaddingRight:0} T:{layout.PaddingTop:0} B:{layout.PaddingBottom:0}";

        var maxText = info1.Length > info2.Length ? info1 : info2;
        if (info3.Length > maxText.Length)
            maxText = info3;
        if (info4.Length > maxText.Length)
            maxText = info4;

        var size = font.SizeOf(maxText.AsSpan());
        var panelRect = new Rect(start.X - 4, start.Y - 4, size.X + 8, lineH * 4 + 4);
        batcher.Quad(new Quad(panelRect), PanelBgColor);

        batcher.Text(font, info1.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(font, info2.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(font, info3.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(font, info4.AsSpan(), p, new Vector2(0, 0), TextColor);

        DrawRectOutline(batcher, wr, HighlightColor, Thickness + 1f);
    }

    static Rect TransformRect(Rect rect, Rect from, Rect to)
    {
        if (from.Width <= 0f || from.Height <= 0f)
            return rect;

        return new Rect(
            to.X + (rect.X - from.X) / from.Width * to.Width,
            to.Y + (rect.Y - from.Y) / from.Height * to.Height,
            rect.Width / from.Width * to.Width,
            rect.Height / from.Height * to.Height);
    }

    static void DrawRectOutline(Batcher batcher, Rect r, Color c, float t)
    {
        if (t <= 0f)
            t = 1f;

        batcher.Quad(new Quad(new Rect(r.X, r.Y, r.Width, t)), c);
        batcher.Quad(new Quad(new Rect(r.X, r.Y + r.Height - t, r.Width, t)), c);
        batcher.Quad(new Quad(new Rect(r.X, r.Y, t, r.Height)), c);
        batcher.Quad(new Quad(new Rect(r.X + r.Width - t, r.Y, t, r.Height)), c);
    }
}
