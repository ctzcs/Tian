using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

/// <summary>
/// 单子节点容器。
/// 负责背景、Padding，并将第一个可见子节点布局到自己的 ContentRect 中。
/// 适合做“面板 + 内容”结构。
/// </summary>
public class BoxContainer : UIElement
{
    public BoxContainer()
    {
        Layout.LayoutType = LayoutType.Absolute;
    }
    public override Vector2 Measure(Vector2 availableSize)
    {
        float innerW = availableSize.X - Layout.PaddingLeft - Layout.PaddingRight;
        float innerH = availableSize.Y - Layout.PaddingTop - Layout.PaddingBottom;

        if (innerW < 0f)
            innerW = 0f;
        if (innerH < 0f)
            innerH = 0f;

        var child = GetFirstVisibleChild();
        var childSize = child?.Measure(new Vector2(innerW, innerH)) ?? Vector2.Zero;

        float width = Layout.Width > 0f
            ? Layout.Width
            : childSize.X + Layout.PaddingLeft + Layout.PaddingRight;

        float height = Layout.Height > 0f
            ? Layout.Height
            : childSize.Y + Layout.PaddingTop + Layout.PaddingBottom;

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

        if (innerW < 0f)
            innerW = 0f;
        if (innerH < 0f)
            innerH = 0f;

        bool contentAssigned = false;

        foreach (var child in Children)
        {
            if (!child.Display || !child.Visible)
                continue;

            if (contentAssigned)
            {
                child.Arrange(new Rect(0f, 0f, 0f, 0f));
                continue;
            }

            var style = child.Layout;

            float marginLeft = style.MarginLeft;
            float marginRight = style.MarginRight;
            float marginTop = style.MarginTop;
            float marginBottom = style.MarginBottom;

            float availableW = innerW - marginLeft - marginRight;
            float availableH = innerH - marginTop - marginBottom;

            if (availableW < 0f)
                availableW = 0f;
            if (availableH < 0f)
                availableH = 0f;

            var measured = child.Measure(new Vector2(availableW, availableH));

            float width = style.Width > 0f ? measured.X : availableW;
            float height = style.Height > 0f ? measured.Y : availableH;

            if (style.MinWidth > 0f && width < style.MinWidth)
                width = style.MinWidth;
            if (style.MaxWidth > 0f && width > style.MaxWidth)
                width = style.MaxWidth;
            if (style.MinHeight > 0f && height < style.MinHeight)
                height = style.MinHeight;
            if (style.MaxHeight > 0f && height > style.MaxHeight)
                height = style.MaxHeight;

            if (width > availableW)
                width = availableW;
            if (height > availableH)
                height = availableH;

            float x;
            switch (style.AlignX)
            {
                case HorizontalAlignment.End:
                    x = innerX + innerW - marginRight - width;
                    break;
                case HorizontalAlignment.Center:
                    x = innerX + marginLeft + (availableW - width) * 0.5f;
                    break;
                case HorizontalAlignment.Stretch:
                case HorizontalAlignment.Start:
                default:
                    x = innerX + marginLeft;
                    break;
            }

            float y;
            switch (style.AlignY)
            {
                case VerticalAlignment.End:
                    y = innerY + innerH - marginBottom - height;
                    break;
                case VerticalAlignment.Center:
                    y = innerY + marginTop + (availableH - height) * 0.5f;
                    break;
                case VerticalAlignment.Stretch:
                case VerticalAlignment.Start:
                default:
                    y = innerY + marginTop;
                    break;
            }

            var childRect = new Rect(x, y, width, height);
            child.Arrange(childRect);

            contentAssigned = true;
        }
    }

    private UIElement? GetFirstVisibleChild()
    {
        foreach (var child in Children)
        {
            if (child.Display && child.Visible)
                return child;
        }

        return null;
    }
}