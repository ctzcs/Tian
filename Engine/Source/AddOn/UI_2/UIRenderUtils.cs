using System.Numerics;
using Engine.Core.Extensions;
using Foster.Framework;

namespace Engine.UI_2;

public static class Ui2RenderUtils
{
    public static void RenderText(Batcher batcher, Ui2DrawCommand cmd, SpriteFont? font, bool sharedFontScope = false)
    {
        if (string.IsNullOrEmpty(cmd.Text))
            return;

        if (font == null)
        {
            var pos = new Vector2(cmd.Rect.X, cmd.Rect.Y);
            var size = cmd.TextSize > 0f ? cmd.TextSize : 16f;
            batcher.Text(cmd.Text.AsSpan(), pos, size, cmd.Color);
            return;
        }

        var align = cmd.TextAlign;
        var box = cmd.Rect;
        var anchor = new Vector2(box.X + box.Width * align.X, box.Y + box.Height * align.Y);

        switch (cmd.TextOverflow)
        {
            case Ui2TextOverflowMode.ShrinkToFit:
            {
                var content = cmd.Text;
                var baseSize = cmd.TextSize > 0f ? cmd.TextSize : font.Size;
                var sizeInfo = font.SizeOf(content.AsSpan(), baseSize);
                var boxW = box.Width;
                var boxH = box.Height;
                var lineHeight = GetSingleLineHeight(font, baseSize);
                var visualHeight = MathF.Max(sizeInfo.Y, lineHeight);
                if (boxW <= 0f || sizeInfo.X <= 0f || visualHeight <= 0f)
                {
                    DrawText(batcher, font, content.AsSpan(), anchor, align, baseSize, cmd.Color, sharedFontScope);
                    return;
                }
                var scale = boxW / sizeInfo.X;
                if (boxH > 0f)
                    scale = MathF.Min(scale, boxH / visualHeight);
                if (scale >= 1f)
                {
                    DrawText(batcher, font, content.AsSpan(), anchor, align, baseSize, cmd.Color, sharedFontScope);
                    return;
                }
                var scaledSize = baseSize * scale;
                if (scaledSize > 0f)
                    DrawText(batcher, font, content.AsSpan(), anchor, align, scaledSize, cmd.Color, sharedFontScope);
                return;
            }
            case Ui2TextOverflowMode.Wrap:
            case Ui2TextOverflowMode.WrapAutoHeight:
            {
                var boxW = box.Width;
                var size = cmd.TextSize > 0f ? cmd.TextSize : font.Size;
                if (boxW <= 0f)
                {
                    DrawText(batcher, font, cmd.Text.AsSpan(), anchor, align, size, cmd.Color, sharedFontScope);
                    return;
                }
                DrawWrappedText(batcher, font, cmd.Text.AsSpan(), boxW, anchor, align, size, cmd.Color, sharedFontScope);
                return;
            }
            case Ui2TextOverflowMode.ShrinkAndWrap:
            {
                var content = cmd.Text;
                var boxW = box.Width;
                var boxH = box.Height;
                var baseSize = cmd.TextSize > 0f ? cmd.TextSize : font.Size;
                var sizeScale = baseSize / font.Size;
                if (boxW <= 0f || boxH <= 0f)
                {
                    DrawText(batcher, font, content.AsSpan(), anchor, align, baseSize, cmd.Color, sharedFontScope);
                    return;
                }
                var lines = font.WrapText(content.AsSpan(), boxW, baseSize);
                var lineCount = lines.Count;
                if (lineCount == 0)
                {
                    DrawText(batcher, font, content.AsSpan(), anchor, align, baseSize, cmd.Color, sharedFontScope);
                    return;
                }
                var totalHeight = font.Height * sizeScale * lineCount + font.LineGap * sizeScale * (lineCount - 1);
                if (totalHeight <= boxH)
                {
                    DrawWrappedText(batcher, font, content.AsSpan(), boxW, anchor, align, baseSize, cmd.Color, sharedFontScope);
                    return;
                }
                var scaledSize = baseSize * (boxH / totalHeight);
                if (scaledSize > 0f)
                    DrawWrappedText(batcher, font, content.AsSpan(), boxW, anchor, align, scaledSize, cmd.Color, sharedFontScope);
                return;
            }
            default:
            {
                var baseSize = cmd.TextSize > 0f ? cmd.TextSize : font.Size;
                DrawText(batcher, font, cmd.Text.AsSpan(), anchor, align, baseSize, cmd.Color, sharedFontScope);
                return;
            }
        }
    }

    static void DrawText(Batcher batcher, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, float size, Color color, bool sharedFontScope)
    {
        if (sharedFontScope)
            font.DrawShared(batcher, text, position, justify, size, color);
        else
            batcher.Text(font, text, position, justify, size, color);
    }

    static void DrawWrappedText(Batcher batcher, SpriteFont font, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, float size, Color color, bool sharedFontScope)
    {
        if (sharedFontScope)
            font.DrawWrappedShared(batcher, text, maxLineWidth, position, justify, size, color);
        else
            batcher.TextWrapped(font, text, maxLineWidth, position, justify, size, color);
    }

    static float GetSingleLineHeight(SpriteFont font, float size)
    {
        if (font.Size <= 0f)
            return size;

        var scale = size / font.Size;
        return (font.Height + font.LineGap) * scale;
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

    public static void RenderNineSlice(Batcher batcher, Subtexture sub, Rect rect, Vector4 border, Color color)
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