using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Asset;
using Foster.Framework;

namespace Engine.UI_2;

public class UICanvas
{
    public string? Id { get; set; }
    public Rect? ClipRect { get; set; }

    public UIElement Root { get; }

    private UIElement? lastHovered;
    private UIElement? lastPressed;
    private readonly List<UIElement> hitBuffer = new();
    private List<UIElement>? lastPressedChain;
    private UIElement? pressTarget;
    private Vector2 lastPointerPosition;
    private bool hasLastPointer;

    public UIElement? DebugHovered => lastHovered;

    public UICanvas()
    {
        Root = new UIElement();
    }

    public void Update(float time)
    {
        Root.Update(time);
        Root.UpdateWorldMatrix(Matrix3x2.Identity);
    }

    public void Layout(Rect viewport)
    {
        foreach (var child in Root.Children)
        {
            if (!child.Display || !child.Visible)
                continue;

            var childViewport = viewport;
            var style = child.Layout;

            if (style.SizeMode == LayoutSizeMode.ViewportRatio)
            {
                var nr = style.ViewportRatio;

                float x = viewport.X + viewport.Width * nr.X;
                float y = viewport.Y + viewport.Height * nr.Y;

                float w = viewport.Width;
                float h = viewport.Height;

                if (nr.Width > 0f)
                    w = viewport.Width * nr.Width;
                if (nr.Height > 0f)
                    h = viewport.Height * nr.Height;

                childViewport = new Rect(x, y, w, h);
            }

            if (style.LayoutType == LayoutType.Absolute)
            {
                float marginLeft = style.MarginLeft;
                float marginRight = style.MarginRight;
                float marginTop = style.MarginTop;
                float marginBottom = style.MarginBottom;

                float innerX = childViewport.X + marginLeft;
                float innerY = childViewport.Y + marginTop;
                float innerW = childViewport.Width - marginLeft - marginRight;
                float innerH = childViewport.Height - marginTop - marginBottom;

                if (innerW < 0f)
                    innerW = 0f;
                if (innerH < 0f)
                    innerH = 0f;

                float width = style.Width > 0f ? style.Width : innerW;
                float height = style.Height > 0f ? style.Height : innerH;

                if (style.MinWidth > 0f && width < style.MinWidth)
                    width = style.MinWidth;
                if (style.MaxWidth > 0f && width > style.MaxWidth)
                    width = style.MaxWidth;
                if (style.MinHeight > 0f && height < style.MinHeight)
                    height = style.MinHeight;
                if (style.MaxHeight > 0f && height > style.MaxHeight)
                    height = style.MaxHeight;

                if (width > innerW)
                    width = innerW;
                if (height > innerH)
                    height = innerH;

                float x;
                switch (style.AlignX)
                {
                    case HorizontalAlignment.End:
                        x = innerX + (innerW - width);
                        break;
                    case HorizontalAlignment.Center:
                        x = innerX + (innerW - width) * 0.5f;
                        break;
                    case HorizontalAlignment.Start:
                    case HorizontalAlignment.Stretch:
                    default:
                        x = innerX;
                        break;
                }

                float y;
                switch (style.AlignY)
                {
                    case VerticalAlignment.End:
                        y = innerY + (innerH - height);
                        break;
                    case VerticalAlignment.Center:
                        y = innerY + (innerH - height) * 0.5f;
                        break;
                    case VerticalAlignment.Start:
                    case VerticalAlignment.Stretch:
                    default:
                        y = innerY;
                        break;
                }

                child.Arrange(new Rect(x, y, width, height));
            }
            else
            {
                var size = new Vector2(childViewport.Width, childViewport.Height);
                var measured = child.Measure(size);
                child.Arrange(new Rect(childViewport.X, childViewport.Y, measured.X, measured.Y));
            }
        }
    }

    public void UpdateInput(Vector2 pointerPosition, bool leftPressed, bool leftReleased)
    {
        hitBuffer.Clear();
        Root.HitAll(pointerPosition, hitBuffer);

        var over = hitBuffer.Count > 0 ? hitBuffer[0] : null;
        var moved = !hasLastPointer || pointerPosition != lastPointerPosition;

        if (ClipRect.HasValue && !ClipRect.Value.Contains(pointerPosition))
        {
            hitBuffer.Clear();
            over = null;
        }

        if (over != lastHovered)
        {
            if (lastHovered != null)
            {
                var exitEvent = new Ui2PointerEvent
                {
                    Target = lastHovered,
                    Current = lastHovered,
                    Position = pointerPosition
                };
                lastHovered.OnPointerExit?.Invoke(exitEvent);
            }

            if (over != null)
            {
                var enterEvent = new Ui2PointerEvent
                {
                    Target = over,
                    Current = over,
                    Position = pointerPosition
                };
                over.OnPointerEnter?.Invoke(enterEvent);
            }

            lastHovered = over;
        }

        if (moved)
        {
            if (lastPressedChain != null && pressTarget != null)
            {
                for (int i = 0; i < lastPressedChain.Count; i++)
                {
                    var element = lastPressedChain[i];
                    var moveEvent = new Ui2PointerEvent
                    {
                        Target = pressTarget,
                        Current = element,
                        Position = pointerPosition
                    };

                    element.OnPointerMove?.Invoke(moveEvent);

                    if (!element.PointerPassThrough)
                        break;
                }
            }
            else if (over != null)
            {
                for (int i = 0; i < hitBuffer.Count; i++)
                {
                    var element = hitBuffer[i];
                    var moveEvent = new Ui2PointerEvent
                    {
                        Target = over,
                        Current = element,
                        Position = pointerPosition
                    };

                    element.OnPointerMove?.Invoke(moveEvent);

                    if (!element.PointerPassThrough)
                        break;
                }
            }
        }

        if (leftPressed && hitBuffer.Count > 0)
        {
            pressTarget = hitBuffer[0];
            lastPressedChain = new List<UIElement>(hitBuffer);

            for (int i = 0; i < hitBuffer.Count; i++)
            {
                var element = hitBuffer[i];
                var downEvent = new Ui2PointerEvent
                {
                    Target = pressTarget,
                    Current = element,
                    Position = pointerPosition
                };

                element.OnPointerDown?.Invoke(downEvent);
                lastPressed = element;

                if (!element.PointerPassThrough)
                    break;
            }
        }

        if (leftReleased && lastPressedChain != null)
        {
            var currentHits = new List<UIElement>(hitBuffer);

            for (int i = 0; i < lastPressedChain.Count; i++)
            {
                var element = lastPressedChain[i];
                var upEvent = new Ui2PointerEvent
                {
                    Target = pressTarget ?? element,
                    Current = element,
                    Position = pointerPosition
                };

                element.OnPointerUp?.Invoke(upEvent);

                if (currentHits.Contains(element))
                    element.OnClick?.Invoke(upEvent);

                if (!element.PointerPassThrough)
                    break;
            }

            lastPressedChain = null;
            lastPressed = null;
            pressTarget = null;
        }

        lastPointerPosition = pointerPosition;
        hasLastPointer = true;
    }

    public void Render(Batcher batcher, Rect viewport)
    {
        var commands = new List<Ui2DrawCommand>();

        foreach (var child in Root.Children)
            child.CollectDrawCommands(commands, 0);

        var clip = (ClipRect ?? viewport).GetIntersection(viewport);
        if (clip.Width <= 0f || clip.Height <= 0f)
            return;

        batcher.PushScissor(clip.Int());

        foreach (var cmd in commands)
        {
            var pushedMatrix = cmd.Matrix != Matrix3x2.Identity;
            if (pushedMatrix)
                batcher.PushMatrix(cmd.Matrix, true);

            switch (cmd.Type)
            {
                case Ui2DrawCommandType.Background:
                    batcher.Rect(cmd.Rect, cmd.Color);
                    break;

                case Ui2DrawCommandType.Text:
                    Ui2RenderUtil.RenderText(batcher, cmd);
                    break;

                case Ui2DrawCommandType.Image:
                    Ui2RenderUtil.RenderImage(batcher, cmd);
                    break;

                case Ui2DrawCommandType.ClipPush:
                    batcher.PushScissor(cmd.Rect.GetIntersection(viewport).Int());
                    break;

                case Ui2DrawCommandType.ClipPop:
                    batcher.PopScissor();
                    break;
            }

            if (pushedMatrix)
                batcher.PopMatrix();
        }

        batcher.PopScissor();
    }
}

static class Ui2RenderUtil
{
    public static void RenderText(Batcher batcher, Ui2DrawCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.Text))
            return;

        if (Assets.Font == null)
        {
            var pos = new Vector2(cmd.Rect.X, cmd.Rect.Y);
            var size = cmd.TextSize > 0f ? cmd.TextSize : 16f;
            batcher.Text(cmd.Text.AsSpan(), pos, size, cmd.Color);
            return;
        }

        var align = cmd.TextAlign;
        var box = cmd.Rect;
        var anchor = new Vector2(
            box.X + box.Width * align.X,
            box.Y + box.Height * align.Y);

        switch (cmd.TextOverflow)
        {
            case Ui2TextOverflowMode.ShrinkToFit:
            {
                var content = cmd.Text;
                var baseSize = cmd.TextSize > 0f ? cmd.TextSize : Assets.Font.Size;
                var sizeInfo = Assets.Font.SizeOf(content.AsSpan(), baseSize);
                var boxW = box.Width;
                var boxH = box.Height;

                if (boxW <= 0f || sizeInfo.X <= 0f || sizeInfo.Y <= 0f)
                {
                    batcher.Text(Assets.Font, content.AsSpan(), anchor, align, baseSize, cmd.Color);
                    return;
                }

                var scaleX = boxW / sizeInfo.X;
                var scale = scaleX;

                if (boxH > 0f)
                {
                    var scaleY = boxH / sizeInfo.Y;
                    if (scaleY < scale)
                        scale = scaleY;
                }

                if (scale >= 1f)
                {
                    baseSize = cmd.TextSize > 0f ? cmd.TextSize : Assets.Font.Size;
                    batcher.Text(Assets.Font, content.AsSpan(), anchor, align, baseSize, cmd.Color);
                    return;
                }

                var scaledSize = baseSize * scale;
                if (scaledSize > 0f)
                    batcher.Text(Assets.Font, content.AsSpan(), anchor, align, scaledSize, cmd.Color);
                return;
            }

            case Ui2TextOverflowMode.Wrap:
            case Ui2TextOverflowMode.WrapAutoHeight:
            {
                var boxW = box.Width;
                if (boxW <= 0f)
                {
                    var baseSize = cmd.TextSize > 0f ? cmd.TextSize : Assets.Font.Size;
                    batcher.Text(Assets.Font, cmd.Text.AsSpan(), anchor, align, baseSize, cmd.Color);
                    return;
                }

                var size = cmd.TextSize > 0f ? cmd.TextSize : Assets.Font.Size;
                batcher.TextWrapped(Assets.Font, cmd.Text.AsSpan(), boxW, anchor, align, size, cmd.Color);
                return;
            }

            case Ui2TextOverflowMode.ShrinkAndWrap:
            {
                var content = cmd.Text;
                var boxW = box.Width;
                var boxH = box.Height;
                var baseSize = cmd.TextSize > 0f ? cmd.TextSize : Assets.Font.Size;
                var sizeScale = baseSize / Assets.Font.Size;

                if (boxW <= 0f || boxH <= 0f)
                {
                    batcher.Text(Assets.Font, content.AsSpan(), anchor, align, baseSize, cmd.Color);
                    return;
                }

                var lines = Assets.Font.WrapText(content.AsSpan(), boxW, baseSize);
                var lineCount = lines.Count;

                if (lineCount == 0)
                {
                    batcher.Text(Assets.Font, content.AsSpan(), anchor, align, baseSize, cmd.Color);
                    return;
                }

                var lineHeight = Assets.Font.Height * sizeScale;
                var lineGap = Assets.Font.LineGap * sizeScale;
                var totalHeight = lineHeight * lineCount + lineGap * (lineCount - 1);

                if (totalHeight <= boxH)
                {
                    batcher.TextWrapped(Assets.Font, content.AsSpan(), boxW, anchor, align, baseSize, cmd.Color);
                    return;
                }

                var scale = boxH / totalHeight;
                if (scale <= 0f)
                    return;

                var scaledSize = baseSize * scale;
                if (scaledSize <= 0f)
                    return;

                batcher.TextWrapped(Assets.Font, content.AsSpan(), boxW, anchor, align, scaledSize, cmd.Color);
                return;
            }

            case Ui2TextOverflowMode.None:
            default:
            {
                var baseSize = cmd.TextSize > 0f ? cmd.TextSize : Assets.Font.Size;
                batcher.Text(Assets.Font, cmd.Text.AsSpan(), anchor, align, baseSize, cmd.Color);
                return;
            }
        }
    }

    public static void RenderImage(Batcher batcher, Ui2DrawCommand cmd)
    {
        if (!cmd.Subtexture.HasValue)
            return;

        var sub = cmd.Subtexture.Value;
        var rect = cmd.Rect;

        switch (cmd.ImageFillMode)
        {
            case Ui2ImageFillMode.Original:
                batcher.Image(sub, rect.Position, cmd.Color);
                return;

            case Ui2ImageFillMode.Fit:
                batcher.ImageFit(sub, rect, new Vector2(0.5f, 0.5f), cmd.Color, false, false);
                return;

            case Ui2ImageFillMode.NineSlice:
                RenderNineSlice(batcher, sub, rect, cmd.NineSliceBorder, cmd.Color);
                return;

            case Ui2ImageFillMode.Stretch:
            default:
                batcher.ImageStretch(sub, rect, cmd.Color);
                return;
        }
    }

    static void RenderNineSlice(Batcher batcher, Subtexture sub, Rect rect, Vector4 border, Color color)
    {
        var tex = sub.Texture;
        if (tex == null)
            return;

        var left = border.X;
        var top = border.Y;
        var right = border.Z;
        var bottom = border.W;

        var src = sub.Source;

        var x = rect.X;
        var y = rect.Y;
        var w = rect.Width;
        var h = rect.Height;

        var sx = src.X;
        var sy = src.Y;
        var sw = src.Width;
        var sh = src.Height;

        var il = (int)left;
        var it = (int)top;
        var ir = (int)right;
        var ib = (int)bottom;

        var l = left;
        var t = top;
        var r = right;
        var b = bottom;

        var cx = w - l - r;
        var cy = h - t - b;
        if (cx < 0f)
            cx = 0f;
        if (cy < 0f)
            cy = 0f;

        var sxl = sx;
        var sxm = sx + il;
        var sxr = sx + sw - ir;

        var syl = sy;
        var sym = sy + it;
        var syr = sy + sh - ib;

        var dxl = x;
        var dxm = x + l;
        var dxr = x + w - r;

        var dyl = y;
        var dym = y + t;
        var dyr = y + h - b;

        var tl = new Subtexture(tex, new Rect(sxl, syl, il, it));
        var tm = new Subtexture(tex, new Rect(sxm, syl, sw - il - ir, it));
        var tr = new Subtexture(tex, new Rect(sxr, syl, ir, it));

        var ml = new Subtexture(tex, new Rect(sxl, sym, il, sh - it - ib));
        var mm = new Subtexture(tex, new Rect(sxm, sym, sw - il - ir, sh - it - ib));
        var mr = new Subtexture(tex, new Rect(sxr, sym, ir, sh - it - ib));

        var bl = new Subtexture(tex, new Rect(sxl, syr, il, ib));
        var bm = new Subtexture(tex, new Rect(sxm, syr, sw - il - ir, ib));
        var br = new Subtexture(tex, new Rect(sxr, syr, ir, ib));

        batcher.ImageStretch(tl, new Rect(dxl, dyl, l, t), color);
        batcher.ImageStretch(tm, new Rect(dxm, dyl, cx, t), color);
        batcher.ImageStretch(tr, new Rect(dxr, dyl, r, t), color);

        batcher.ImageStretch(ml, new Rect(dxl, dym, l, cy), color);
        batcher.ImageStretch(mm, new Rect(dxm, dym, cx, cy), color);
        batcher.ImageStretch(mr, new Rect(dxr, dym, r, cy), color);

        batcher.ImageStretch(bl, new Rect(dxl, dyr, l, b), color);
        batcher.ImageStretch(bm, new Rect(dxm, dyr, cx, b), color);
        batcher.ImageStretch(br, new Rect(dxr, dyr, r, b), color);
    }
}

public class UIDebugger
{
    public bool Enabled = true;
    public Color BoxColor = new Color(0, 200, 255, 120);
    public Color TextColor = Color.Yellow;
    public Color HighlightColor = new Color(255, 255, 0, 220);
    public Color PanelBgColor = new Color(0, 0, 0, 180);
    public float Thickness = 1f;

    private UIElement? lastLoggedHovered;

    public void Render(Batcher batcher, UICanvas canvas)
    {
        if (!Enabled)
            return;

        RenderOutline(batcher, canvas.Root);

        var hovered = canvas.DebugHovered;
        if (hovered != null)
        {
            if (hovered != lastLoggedHovered)
            {
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

            RenderInfoPanel(batcher, hovered);
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

    void RenderInfoPanel(Batcher batcher, UIElement element)
    {
        if (Assets.Font == null)
            return;

        float lineH = Assets.Font.Height + Assets.Font.LineGap;
        var start = new Vector2(10, 10);
        var p = start;

        var wr = element.GetWorldRect();
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

        var size = Assets.Font.SizeOf(maxText.AsSpan());
        var panelRect = new Rect(start.X - 4, start.Y - 4, size.X + 8, lineH * 4 + 4);
        batcher.Quad(new Quad(panelRect), PanelBgColor);

        batcher.Text(Assets.Font, info1.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(Assets.Font, info2.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(Assets.Font, info3.AsSpan(), p, new Vector2(0, 0), TextColor);
        p.Y += lineH;
        batcher.Text(Assets.Font, info4.AsSpan(), p, new Vector2(0, 0), TextColor);

        DrawRectOutline(batcher, wr, HighlightColor, Thickness + 1f);
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
