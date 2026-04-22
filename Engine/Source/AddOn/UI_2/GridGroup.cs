using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public class GridGroup : UIElement
{
    public float Gap { get; set; }
    public int Columns { get; set; }
    public float CellWidth { get; set; }
    public float CellHeight { get; set; }

    readonly List<UIElement> visibleChildren = new();

    public GridGroup()
    {
        ChildrenLayout.LayoutType = LayoutType.Grid;
        Columns = 1;
    }

    public override Vector2 Measure(Vector2 availableSize)
    {
        float innerW = availableSize.X - ChildrenLayout.PaddingLeft - ChildrenLayout.PaddingRight;
        float innerH = availableSize.Y - ChildrenLayout.PaddingTop - ChildrenLayout.PaddingBottom;

        visibleChildren.Clear();
        foreach (var child in Children)
        {
            if (!child.Display || !child.Visible)
                continue;
            visibleChildren.Add(child);
        }

        int count = visibleChildren.Count;
        if (count == 0)
            return new Vector2(
                Layout.Width > 0f ? Layout.Width : 0f,
                Layout.Height > 0f ? Layout.Height : 0f);

        int cols = Columns > 0 ? Columns : 1;
        int rows = (count + cols - 1) / cols;

        float cellW = CellWidth;
        float cellH = CellHeight;

        if (cellW <= 0f || cellH <= 0f)
        {
            cellW = 0f;
            cellH = 0f;
            for (int i = 0; i < count; i++)
            {
                var child = visibleChildren[i];
                var childStyle = child.Layout;
                float marginX = childStyle.MarginLeft + childStyle.MarginRight;
                float marginY = childStyle.MarginTop + childStyle.MarginBottom;

                var size = child.Measure(new Vector2(innerW - marginX, innerH - marginY));
                float candidateW = size.X + marginX;
                float candidateH = size.Y + marginY;

                if (candidateW > cellW)
                    cellW = candidateW;
                if (candidateH > cellH)
                    cellH = candidateH;
            }
        }

        float totalW = cols * cellW + (cols - 1) * Gap;
        float totalH = rows * cellH + (rows - 1) * Gap;

        float width = totalW + ChildrenLayout.PaddingLeft + ChildrenLayout.PaddingRight;
        float height = totalH + ChildrenLayout.PaddingTop + ChildrenLayout.PaddingBottom;

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

        var style = ChildrenLayout;

        float innerX = style.PaddingLeft;
        float innerY = style.PaddingTop;
        float innerW = rect.Width - style.PaddingLeft - style.PaddingRight;
        float innerH = rect.Height - style.PaddingTop - style.PaddingBottom;

        visibleChildren.Clear();
        foreach (var child in Children)
        {
            if (!child.Display || !child.Visible)
                continue;
            visibleChildren.Add(child);
        }

        int count = visibleChildren.Count;
        if (count == 0)
            return;

        int cols = Columns > 0 ? Columns : 1;

        float cellW = CellWidth;
        float cellH = CellHeight;

        if (cellW <= 0f || cellH <= 0f)
        {
            cellW = 0f;
            cellH = 0f;
            for (int i = 0; i < count; i++)
            {
                var child = visibleChildren[i];
                var childStyle = child.Layout;
                float marginX = childStyle.MarginLeft + childStyle.MarginRight;
                float marginY = childStyle.MarginTop + childStyle.MarginBottom;

                var size = child.Measure(new Vector2(innerW - marginX, innerH - marginY));
                float candidateW = size.X + marginX;
                float candidateH = size.Y + marginY;

                if (candidateW > cellW)
                    cellW = candidateW;
                if (candidateH > cellH)
                    cellH = candidateH;
            }
        }

        int rows = (count + cols - 1) / cols;

        float gridW = cols * cellW + (cols - 1) * Gap;
        float gridH = rows * cellH + (rows - 1) * Gap;

        float originX;
        switch (style.AlignX)
        {
            case HorizontalAlignment.Center:
                originX = innerX + (innerW - gridW) * 0.5f;
                break;
            case HorizontalAlignment.End:
                originX = innerX + innerW - gridW;
                break;
            case HorizontalAlignment.Start:
            case HorizontalAlignment.Stretch:
            default:
                originX = innerX;
                break;
        }

        float originY;
        switch (style.AlignY)
        {
            case VerticalAlignment.Center:
                originY = innerY + (innerH - gridH) * 0.5f;
                break;
            case VerticalAlignment.End:
                originY = innerY + innerH - gridH;
                break;
            case VerticalAlignment.Start:
            case VerticalAlignment.Stretch:
            default:
                originY = innerY;
                break;
        }

        for (int index = 0; index < count; index++)
        {
            int col = index % cols;
            int row = index / cols;

            float cellX = originX + col * (cellW + Gap);
            float cellY = originY + row * (cellH + Gap);

            var child = visibleChildren[index];
            var childStyle = child.Layout;

            float marginLeft = childStyle.MarginLeft;
            float marginRight = childStyle.MarginRight;
            float marginTop = childStyle.MarginTop;
            float marginBottom = childStyle.MarginBottom;

            float availableW = cellW - marginLeft - marginRight;
            float availableH = cellH - marginTop - marginBottom;
            if (availableW < 0f)
                availableW = 0f;
            if (availableH < 0f)
                availableH = 0f;

            var childSize = child.Measure(new Vector2(availableW, availableH));

            float width = childSize.X;
            float height = childSize.Y;

            if (childStyle.MinWidth > 0f && width < childStyle.MinWidth)
                width = childStyle.MinWidth;
            if (childStyle.MaxWidth > 0f && width > childStyle.MaxWidth)
                width = childStyle.MaxWidth;
            if (childStyle.MinHeight > 0f && height < childStyle.MinHeight)
                height = childStyle.MinHeight;
            if (childStyle.MaxHeight > 0f && height > childStyle.MaxHeight)
                height = childStyle.MaxHeight;

            if (width > availableW)
                width = availableW;
            if (height > availableH)
                height = availableH;

            float x;
            switch (style.AlignX)
            {
                case HorizontalAlignment.Start:
                    x = cellX + marginLeft;
                    break;
                case HorizontalAlignment.End:
                    x = cellX + cellW - marginRight - width;
                    break;
                case HorizontalAlignment.Stretch:
                    width = availableW;
                    x = cellX + marginLeft;
                    break;
                case HorizontalAlignment.Center:
                default:
                    x = cellX + marginLeft + (availableW - width) * 0.5f;
                    break;
            }

            float y;
            switch (style.AlignY)
            {
                case VerticalAlignment.Start:
                    y = cellY + marginTop;
                    break;
                case VerticalAlignment.End:
                    y = cellY + cellH - marginBottom - height;
                    break;
                case VerticalAlignment.Stretch:
                    height = availableH;
                    y = cellY + marginTop;
                    break;
                case VerticalAlignment.Center:
                default:
                    y = cellY + marginTop + (availableH - height) * 0.5f;
                    break;
            }

            child.Arrange(new Rect(x, y, width, height));
        }
    }
}
