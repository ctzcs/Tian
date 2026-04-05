using System;
using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public class UICanvas
{
    public string? Id { get; set; }
    public Rect? ClipRect { get; set; }
    public bool Visible { get; set; } = true;

    public UIElement Root { get; }

    internal UICanvasInputState? InputState { get; set; }
    internal UICanvasInputState RequireInputState() => InputState ?? throw new InvalidOperationException("UICanvas is not attached to UIRoot");

    public UIElement? DebugHovered => InputState?.DebugHovered;
    public bool HasPointerCapture => InputState?.HasPointerCapture ?? false;

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

    static Rect TransformRect(Rect rect, Rect from, Rect to)
    {
        if (from.Width <= 0f || from.Height <= 0f)
            return to;
        return new Rect(
            to.X + (rect.X - from.X) / from.Width * to.Width,
            to.Y + (rect.Y - from.Y) / from.Height * to.Height,
            rect.Width / from.Width * to.Width,
            rect.Height / from.Height * to.Height);
    }

    public void Render(Batcher batcher, Rect viewport, Rect outputViewport)
    {
        var commands = new List<Ui2DrawCommand>();

        foreach (var child in Root.Children)
            child.CollectDrawCommands(commands, 0);

        var clip = TransformRect((ClipRect ?? viewport).GetIntersection(viewport), viewport, outputViewport);
        if (clip.Width <= 0f || clip.Height <= 0f)
            return;

        float scaleX = outputViewport.Width / viewport.Width;
        float scaleY = outputViewport.Height / viewport.Height;
        var matrix = Matrix3x2.CreateScale(scaleX, scaleY)
                     * Matrix3x2.CreateTranslation(outputViewport.X - viewport.X * scaleX, outputViewport.Y - viewport.Y * scaleY);
        batcher.PushScissor(clip.Int());
        batcher.PushMatrix(matrix, true);

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
                    Ui2RenderUtils.RenderText(batcher, cmd);
                    break;

                case Ui2DrawCommandType.Image:
                    Ui2RenderUtils.RenderImage(batcher, cmd);
                    break;

                case Ui2DrawCommandType.ClipPush:
                    batcher.PushScissor(TransformRect(cmd.Rect.GetIntersection(viewport), viewport, outputViewport).Int());
                    break;

                case Ui2DrawCommandType.ClipPop:
                    batcher.PopScissor();
                    break;
            }

            if (pushedMatrix)
                batcher.PopMatrix();
        }

        batcher.PopMatrix();
        batcher.PopScissor();
    }
}

