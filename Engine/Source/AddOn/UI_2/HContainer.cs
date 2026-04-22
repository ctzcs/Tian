using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;
/// <summary>
/// 水平容器，排成一行
/// </summary>
public class HContainer : UIElement
{
    public float Gap { get; set; }

    readonly List<Vector2> measuredPixel = new();
    readonly List<UIElement> pixelChildren = new();
    readonly List<UIElement> ratioChildren = new();

    public HContainer()
    {
        ChildrenLayout.LayoutType = LayoutType.Row;
    }

    public override Vector2 Measure(Vector2 availableSize)
    {
        float innerW = availableSize.X - ChildrenLayout.PaddingLeft - ChildrenLayout.PaddingRight;
        float innerH = availableSize.Y - ChildrenLayout.PaddingTop - ChildrenLayout.PaddingBottom;

        float totalBaseWidth = 0f;
        float totalMarginX = 0f;
        float maxHeight = 0f;
        float maxMarginY = 0f;
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

            float baseWidth = childSize.X;
            if (style.Grow > 0f && style.Width <= 0f)
                baseWidth = 0f;

            totalBaseWidth += baseWidth;
            totalMarginX += marginX;
            totalGrow += style.Grow;

            float blockHeight = childSize.Y + marginY;
            if (blockHeight > maxHeight)
                maxHeight = blockHeight;
            if (marginY > maxMarginY)
                maxMarginY = marginY;
            visibleCount++;
        }

        if (visibleCount > 1)
            totalBaseWidth += Gap * (visibleCount - 1);

        float contentWidth = totalBaseWidth + totalMarginX;
        if (totalGrow > 0f && contentWidth < innerW)
            contentWidth = innerW;

        float width = contentWidth + ChildrenLayout.PaddingLeft + ChildrenLayout.PaddingRight;
        float height = maxHeight + ChildrenLayout.PaddingTop + ChildrenLayout.PaddingBottom;

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

        float innerX = ChildrenLayout.PaddingLeft;
        float innerY = ChildrenLayout.PaddingTop;
        float innerW = rect.Width - ChildrenLayout.PaddingLeft - ChildrenLayout.PaddingRight;
        float innerH = rect.Height - ChildrenLayout.PaddingTop - ChildrenLayout.PaddingBottom;

        measuredPixel.Clear();
        pixelChildren.Clear();
        ratioChildren.Clear();
        float totalBaseContentWidth = 0f;
        float totalMarginX = 0f;
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

            float baseWidth = size.X;
            if (style.Grow > 0f && style.Width <= 0f)
                baseWidth = 0f;

            float marginX = style.MarginLeft + style.MarginRight;

            totalBaseContentWidth += baseWidth;
            totalMarginX += marginX;
            totalGrow += style.Grow;
            if (style.Shrink > 0f)
                totalShrink += style.Shrink;
        }

        int pixelCount = pixelChildren.Count;

        float totalWidth = totalBaseContentWidth + totalMarginX;
        if (pixelCount > 1)
            totalWidth += Gap * (pixelCount - 1);

        float free = innerW - totalWidth;
        if (free < 0f)
            free = 0f;

        float overflow = totalWidth - innerW;
        if (overflow < 0f)
            overflow = 0f;

        float x = innerX;
        for (int i = 0; i < pixelCount; i++)
        {
            var child = pixelChildren[i];
            var size = measuredPixel[i];
            var style = child.Layout;

            float baseWidth = size.X;
            if (style.Grow > 0f && style.Width <= 0f)
                baseWidth = 0f;

            float width = baseWidth;
            if (totalGrow > 0f && style.Grow > 0f)
                width += free * (style.Grow / totalGrow);

            if (overflow > 0f && totalShrink > 0f && style.Shrink > 0f)
            {
                float shrinkDelta = overflow * (style.Shrink / totalShrink);
                width -= shrinkDelta;
                if (width < 0f)
                    width = 0f;
            }

            float marginLeft = style.MarginLeft;
            float marginRight = style.MarginRight;
            float marginTop = style.MarginTop;
            float marginBottom = style.MarginBottom;

            float height = size.Y;
            if (height <= 0f || ChildrenLayout.AlignY == VerticalAlignment.Stretch)
                height = innerH - marginTop - marginBottom;

            if (height < 0f)
                height = 0f;

            float y;
            switch (ChildrenLayout.AlignY)
            {
                case VerticalAlignment.Start:
                    y = innerY + marginTop;
                    break;
                case VerticalAlignment.End:
                    y = innerY + innerH - marginBottom - height;
                    break;
                case VerticalAlignment.Center:
                    y = innerY + marginTop + (innerH - marginTop - marginBottom - height) * 0.5f;
                    break;
                case VerticalAlignment.Stretch:
                default:
                    y = innerY + marginTop;
                    break;
            }

            float contentX = x + marginLeft;
            child.Arrange(new Rect(contentX, y, width, height));
            x += marginLeft + width + marginRight + Gap;
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
