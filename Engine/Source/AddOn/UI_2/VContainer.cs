using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

/// <summary>
/// 垂直容器，排成一列
/// </summary>
public class VContainer : UIElement
{
    public float Gap { get; set; }

    readonly List<Vector2> measuredPixel = new();
    readonly List<UIElement> pixelChildren = new();
    readonly List<UIElement> ratioChildren = new();

    public VContainer()
    {
        Layout.LayoutType = LayoutType.Column;
    }

    public override Vector2 Measure(Vector2 availableSize)
    {
        float innerW = availableSize.X - Layout.PaddingLeft - Layout.PaddingRight;
        float innerH = availableSize.Y - Layout.PaddingTop - Layout.PaddingBottom;

        float totalBaseHeight = 0f;
        float totalMarginY = 0f;
        float maxWidth = 0f;
        float maxMarginX = 0f;
        int visibleCount = 0;
        float totalGrow = 0f;

        foreach (var child in Children)
        {
            if (!child.Display || !child.Visible)
                continue;

            var style = child.Layout;
            float marginX = style.MarginLeft + style.MarginRight;
            float marginY = style.MarginTop + style.MarginBottom;

            var childSize = child.Measure(new Vector2(innerW - marginX, innerH - marginY));

            float baseHeight = childSize.Y;
            if (style.Grow > 0f && style.Height <= 0f)
                baseHeight = 0f;

            totalBaseHeight += baseHeight;
            totalMarginY += marginY;
            totalGrow += style.Grow;

            float blockWidth = childSize.X + marginX;
            if (blockWidth > maxWidth)
                maxWidth = blockWidth;
            if (marginX > maxMarginX)
                maxMarginX = marginX;
            visibleCount++;
        }

        if (visibleCount > 1)
            totalBaseHeight += Gap * (visibleCount - 1);

        float contentHeight = totalBaseHeight + totalMarginY;
        if (totalGrow > 0f && contentHeight < innerH)
            contentHeight = innerH;

        float width = maxWidth + Layout.PaddingLeft + Layout.PaddingRight;
        float height = contentHeight + Layout.PaddingTop + Layout.PaddingBottom;

        if (Layout.Width > 0f)
            width = Layout.Width;
        if (Layout.Height > 0f)
            height = Layout.Height;

        if (Layout.MinWidth > 0f && width < Layout.MinWidth)
            width = Layout.MinWidth;
        if (Layout.MaxWidth > 0f && width > Layout.MaxWidth)
            width = Layout.MaxWidth;
        if (Layout.MinHeight > 0f && height < Layout.MinHeight)
            height = Layout.MinHeight;
        if (Layout.MaxHeight > 0f && height > Layout.MaxHeight)
            height = Layout.MaxHeight;

        return new Vector2(width, height);
    }

    public override void Arrange(Rect rect)
    {
        base.Arrange(rect);

        float innerX = Layout.PaddingLeft;
        float innerY = Layout.PaddingTop;
        float innerW = rect.Width - Layout.PaddingLeft - Layout.PaddingRight;
        float innerH = rect.Height - Layout.PaddingTop - Layout.PaddingBottom;

        measuredPixel.Clear();
        pixelChildren.Clear();
        ratioChildren.Clear();
        float totalBaseContentHeight = 0f;
        float totalMarginY = 0f;
        float totalGrow = 0f;
        float totalShrink = 0f;

        foreach (var child in Children)
        {
            if (!child.Display || !child.Visible)
                continue;

            var style = child.Layout;

            if (style.SizeMode == LayoutSizeMode.ViewportRatio)
            {
                ratioChildren.Add(child);
                continue;
            }

            var size = child.Measure(new Vector2(innerW, innerH));
            measuredPixel.Add(size);
            pixelChildren.Add(child);

            float baseHeight = size.Y;
            if (style.Grow > 0f && style.Height <= 0f)
                baseHeight = 0f;

            float marginY = style.MarginTop + style.MarginBottom;

            totalBaseContentHeight += baseHeight;
            totalMarginY += marginY;
            totalGrow += style.Grow;
            if (style.Shrink > 0f)
                totalShrink += style.Shrink;
        }

        int pixelCount = pixelChildren.Count;

        float totalHeight = totalBaseContentHeight + totalMarginY;
        if (pixelCount > 1)
            totalHeight += Gap * (pixelCount - 1);

        float free = innerH - totalHeight;
        if (free < 0f)
            free = 0f;

        float overflow = totalHeight - innerH;
        if (overflow < 0f)
            overflow = 0f;

        float y = innerY;
        for (int i = 0; i < pixelCount; i++)
        {
            var child = pixelChildren[i];
            var size = measuredPixel[i];
            var style = child.Layout;

            float baseHeight = size.Y;
            if (style.Grow > 0f && style.Height <= 0f)
                baseHeight = 0f;

            float height = baseHeight;
            if (totalGrow > 0f && style.Grow > 0f)
                height += free * (style.Grow / totalGrow);

            if (overflow > 0f && totalShrink > 0f && style.Shrink > 0f)
            {
                float shrinkDelta = overflow * (style.Shrink / totalShrink);
                height -= shrinkDelta;
                if (height < 0f)
                    height = 0f;
            }

            float marginLeft = style.MarginLeft;
            float marginRight = style.MarginRight;
            float marginTop = style.MarginTop;
            float marginBottom = style.MarginBottom;

            float width = size.X;
            if (width <= 0f || Layout.AlignX == HorizontalAlignment.Stretch)
                width = innerW - marginLeft - marginRight;

            if (width < 0f)
                width = 0f;

            float x;
            switch (Layout.AlignX)
            {
                case HorizontalAlignment.Start:
                    x = innerX + marginLeft;
                    break;
                case HorizontalAlignment.End:
                    x = innerX + innerW - marginRight - width;
                    break;
                case HorizontalAlignment.Center:
                    x = innerX + marginLeft + (innerW - marginLeft - marginRight - width) * 0.5f;
                    break;
                case HorizontalAlignment.Stretch:
                default:
                    x = innerX + marginLeft;
                    break;
            }

            float contentY = y + marginTop;
            child.Arrange(new Rect(x, contentY, width, height));
            y += marginTop + height + marginBottom + Gap;
        }

        foreach (var child in ratioChildren)
        {
            var style = child.Layout;
            var nr = style.ViewportRatio;

            float cx = innerX + innerW * nr.X;
            float cy = innerY + innerH * nr.Y;

            float cw = innerW;
            float ch = innerH;

            if (nr.Width > 0f)
                cw = innerW * nr.Width;
            if (nr.Height > 0f)
                ch = innerH * nr.Height;

            child.Arrange(new Rect(cx, cy, cw, ch));
        }
    }
}
