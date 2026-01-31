using System.Collections.Generic;
using Foster.Framework;

namespace Engine.UI;

/// <summary>
/// 布局方向
/// </summary>
public enum LayoutDirection
{
    Row,
    Column,
    Grid
}

public enum HorizontalAlignment
{
    Left,
    Center,
    Right
}

public enum VerticalAlignment
{
    Top,
    Middle,
    Bottom
}

public struct LayoutConfig
{
    public LayoutDirection Direction;
    //Padding
    public float PaddingLeft;
    public float PaddingRight;
    public float PaddingTop;
    public float PaddingBottom;
    public float ChildGap;
    public HorizontalAlignment AlignX;
    public VerticalAlignment AlignY;

    // Grid
    public int GridColumns;
    public float GridCellWidth;
    public float GridCellHeight;

    // 是否让自身宽/高跟随子元素内容（类似 Clay 的 FIT/HUG）
    public bool AutoWidth;
    public bool AutoHeight;
}

public class UILayoutGroup : UIElement
{
    public UILayoutGroup() : base(true, false, true, new Rect(0, 0, 0, 0))
    {
        Layout = new LayoutConfig
        {
            Direction = LayoutDirection.Row,
            PaddingLeft = 0,
            PaddingRight = 0,
            PaddingTop = 0,
            PaddingBottom = 0,
            ChildGap = 0,
            AlignX = HorizontalAlignment.Left,
            AlignY = VerticalAlignment.Middle,
            GridColumns = 0,
            GridCellWidth = 0,
            GridCellHeight = 0,
            AutoWidth = false,
            AutoHeight = false
        };
    }

    protected override UIElement CreateCloneInstance()
    {
        return new UILayoutGroup();
    }

    public override void Apply()
    {
        var cfg = Layout;

        // 大多数情况下，Group 应该自上而下布局：先算出自己的可用空间，再让子元素在此基础上布局。
        // 否则（自下而上）子元素会在父容器尺寸还是旧值/0 的情况下先布局，出现“不撑满”的现象。
        if (!cfg.AutoWidth && !cfg.AutoHeight)
        {
            base.Apply();
            return;
        }

        // AutoWidth/AutoHeight 需要先让子元素准备好 TargetRect（测量/自适应），父容器才能据此算自身尺寸。
        // 但父容器尺寸变化后，子元素还需要再跑一遍布局，避免最终结果仍然基于旧的父尺寸。
        foreach (var child in children)
            child.Apply();

        if (layoutDirty)
        {
            UpdateLayout();
            layoutDirty = false;
        }

        foreach (var child in children)
            child.Apply();
    }

    /// <summary>
    /// 根据当前可见子元素的总宽高 / 最大宽高，自动调整自身的目标 Rect。
    /// Row: 宽度 = 子元素宽度总和，高度 = 最大子高度；
    /// Column: 高度 = 子元素高度总和，宽度 = 最大子宽度。
    /// 只读取子元素的 TargetRect 作为布局输入，Rect 仍然是配置。
    /// </summary>
    public void AutoSizeToChildren(bool autoWidth = false, bool autoHeight = true)
    {
        var cfg = Layout;

        // 位置来自当前 TargetRect，尺寸由子元素内容决定
        var x = TargetRect.X;
        var y = TargetRect.Y;
        var w = TargetRect.Width;
        var h = TargetRect.Height;

        if (cfg.Direction == LayoutDirection.Row)
        {
            if (autoWidth)
            {
                w = GetVisibleChildrenTotalWidth(
                    includeGap: true,
                    fallbackWidth: 0f,
                    includePadding: true,
                    useTarget: true,
                    updateLayoutNow: false);
            }

            if (autoHeight)
            {
                float maxChildHeight = 0f;
                foreach (var child in children)
                {
                    if (!child.Visible)
                        continue;

                    var ch = child.TargetRect.Height;
                    if (ch > maxChildHeight)
                        maxChildHeight = ch;
                }

                h = maxChildHeight + cfg.PaddingTop + cfg.PaddingBottom;
            }
        }
        else if (cfg.Direction == LayoutDirection.Column)
        {
            if (autoWidth)
            {
                float maxChildWidth = 0f;
                foreach (var child in children)
                {
                    if (!child.Visible)
                        continue;

                    var cw = child.TargetRect.Width;
                    if (cw > maxChildWidth)
                        maxChildWidth = cw;
                }

                w = maxChildWidth + cfg.PaddingLeft + cfg.PaddingRight;
            }

            if (autoHeight)
            {
                h = GetVisibleChildrenTotalHeight(
                    includeGap: true,
                    fallbackHeight: 0f,
                    includePadding: true,
                    useTarget: true,
                    updateLayoutNow: false);
            }
        }
        else // Grid
        {
            var visibleCount = GetVisibleChildrenCount();
            if (visibleCount > 0)
            {
                var cols = cfg.GridColumns;
                var cellW = cfg.GridCellWidth;
                var cellH = cfg.GridCellHeight;

                if (cols <= 0)
                {
                    if (cellW <= 0f)
                        cellW = GetGridFallbackCellWidth();

                    var usable = TargetRect.Width - cfg.PaddingLeft - cfg.PaddingRight;
                    cols = usable > 0f && cellW > 0f
                        ? (int)((usable + cfg.ChildGap) / (cellW + cfg.ChildGap))
                        : 1;
                }

                if (cols < 1)
                    cols = 1;

                if (cellW <= 0f)
                    cellW = GetGridFallbackCellWidth();
                if (cellH <= 0f)
                    cellH = GetGridFallbackCellHeight();

                var rows = (visibleCount + cols - 1) / cols;

                if (autoWidth)
                {
                    w = cols * cellW + (cols - 1) * cfg.ChildGap + cfg.PaddingLeft + cfg.PaddingRight;
                }

                if (autoHeight)
                {
                    h = rows * cellH + (rows - 1) * cfg.ChildGap + cfg.PaddingTop + cfg.PaddingBottom;
                }
            }
        }

        SetTargetRect(new Rect(x, y, w, h));
    }

    protected override void UpdateLayout()
    {
        if (children.Count == 0)
            return;

        base.UpdateLayout();

        var cfg = Layout;

        // 如果配置了 AutoWidth/AutoHeight，则先根据子元素内容更新自身 TargetRect 尺寸
        if (cfg.AutoWidth || cfg.AutoHeight)
            AutoSizeToChildren(cfg.AutoWidth, cfg.AutoHeight);

        float innerX = cfg.PaddingLeft;
        float innerY = cfg.PaddingTop;
        float innerW = TargetRect.Width - cfg.PaddingLeft - cfg.PaddingRight;
        float innerH = TargetRect.Height - cfg.PaddingTop - cfg.PaddingBottom;

        if (cfg.Direction == LayoutDirection.Row)
            LayoutRow(cfg, innerX, innerY, innerW, innerH);
        else if (cfg.Direction == LayoutDirection.Column)
            LayoutColumn(cfg, innerX, innerY, innerW, innerH);
        else
            LayoutGrid(cfg, innerX, innerY, innerW, innerH);
    }
    

    public Rect GetInnerBounds()
    {
        var cfg = Layout;
        var x = cfg.PaddingLeft;
        var y = cfg.PaddingTop;
        var w = rect.Width - cfg.PaddingLeft - cfg.PaddingRight;
        var h = rect.Height - cfg.PaddingTop - cfg.PaddingBottom;
        return new Rect(x, y, w, h);
    }

    public int GetVisibleChildrenCount()
    {
        var count = 0;
        foreach (var child in children)
            if (child.Visible)
                count++;
        return count;
    }

    public float GetVisibleChildrenTotalWidth(bool includeGap = true, float fallbackWidth = 0f, bool includePadding = false, bool useTarget = false, bool updateLayoutNow = false)
    {
        if (updateLayoutNow)
            UpdateLayoutNow(true);

        var cfg = Layout;
        var count = 0;
        var total = 0f;

        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var w = useTarget ? child.TargetRect.Width : child.Rect.Width;
            if (w == 0f && fallbackWidth > 0f)
                w = fallbackWidth;

            total += w;
            count++;
        }

        if (includeGap && count > 1)
            total += cfg.ChildGap * (count - 1);

        if (includePadding)
            total += cfg.PaddingLeft + cfg.PaddingRight;

        return total;
    }

    public float GetVisibleChildrenTotalHeight(bool includeGap = true, float fallbackHeight = 0f, bool includePadding = false, bool useTarget = false, bool updateLayoutNow = false)
    {
        if (updateLayoutNow)
            UpdateLayoutNow(true);

        var cfg = Layout;
        var count = 0;
        var total = 0f;

        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var h = useTarget ? child.TargetRect.Height : child.Rect.Height;
            if (h == 0f && fallbackHeight > 0f)
                h = fallbackHeight;

            total += h;
            count++;
        }

        if (includeGap && count > 1)
            total += cfg.ChildGap * (count - 1);

        if (includePadding)
            total += cfg.PaddingTop + cfg.PaddingBottom;

        return total;
    }

    void LayoutRow(LayoutConfig cfg, float innerX, float innerY, float innerW, float innerH)
    {
        var visibleCount = 0;
        var totalWidth = 0f;
        var totalGrow = 0f;

        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var tr = child.TargetRect;
            totalWidth += tr.Width;

            if (child.GrowX > 0f)
                totalGrow += child.GrowX;

            visibleCount++;
        }

        if (visibleCount == 0)
            return;

        if (visibleCount > 1)
            totalWidth += cfg.ChildGap * (visibleCount - 1);

        float extra = 0f;
        if (totalGrow > 0f)
        {
            var free = innerW - totalWidth;
            if (free > 0f)
            {
                extra = free;
                totalWidth = innerW;
            }
        }

        float startX;
        switch (cfg.AlignX)
        {
            case HorizontalAlignment.Center:
                startX = innerX + (innerW - totalWidth) * 0.5f;
                break;
            case HorizontalAlignment.Right:
                startX = innerX + (innerW - totalWidth);
                break;
            case HorizontalAlignment.Left:
            default:
                startX = innerX;
                break;
        }

        float x = startX;
        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var tr = child.TargetRect;
            float width = tr.Width;
            float height = tr.Height;

            if (extra > 0f && totalGrow > 0f && child.GrowX > 0f)
                width += extra * (child.GrowX / totalGrow);

            if (height == 0)
                height = innerH;

            if (child.MinWidth > 0f && width < child.MinWidth)
                width = child.MinWidth;
            if (child.MaxWidth > 0f && width > child.MaxWidth)
                width = child.MaxWidth;
            if (child.MinHeight > 0f && height < child.MinHeight)
                height = child.MinHeight;
            if (child.MaxHeight > 0f && height > child.MaxHeight)
                height = child.MaxHeight;

            float y;
            switch (cfg.AlignY)
            {
                case VerticalAlignment.Top:
                    y = innerY;
                    break;
                case VerticalAlignment.Bottom:
                    y = innerY + innerH - height;
                    break;
                case VerticalAlignment.Middle:
                default:
                    y = innerY + (innerH - height) * 0.5f;
                    break;
            }
            
            child.SetTargetRect(new Rect(x, y, width, height));
            x += width + cfg.ChildGap;
        }
    }

    void LayoutColumn(LayoutConfig cfg, float innerX, float innerY, float innerW, float innerH)
    {
        var visibleCount = 0;
        var totalHeight = 0f;
        var totalGrow = 0f;

        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var tr = child.TargetRect;
            totalHeight += tr.Height;

            if (child.GrowY > 0f)
                totalGrow += child.GrowY;

            visibleCount++;
        }

        if (visibleCount == 0)
            return;

        if (visibleCount > 1)
            totalHeight += cfg.ChildGap * (visibleCount - 1);

        float extra = 0f;
        if (totalGrow > 0f)
        {
            var free = innerH - totalHeight;
            if (free > 0f)
            {
                extra = free;
                totalHeight = innerH;
            }
        }

        float startY;
        switch (cfg.AlignY)
        {
            case VerticalAlignment.Middle:
                startY = innerY + (innerH - totalHeight) * 0.5f;
                break;
            case VerticalAlignment.Bottom:
                startY = innerY + innerH - totalHeight;
                break;
            case VerticalAlignment.Top:
            default:
                startY = innerY;
                break;
        }

        float y = startY;
        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var tr = child.TargetRect;
            float width = tr.Width;
            float height = tr.Height;

            if (extra > 0f && totalGrow > 0f && child.GrowY > 0f)
                height += extra * (child.GrowY / totalGrow);

            if (width == 0)
                width = innerW;

            if (child.MinWidth > 0f && width < child.MinWidth)
                width = child.MinWidth;
            if (child.MaxWidth > 0f && width > child.MaxWidth)
                width = child.MaxWidth;
            if (child.MinHeight > 0f && height < child.MinHeight)
                height = child.MinHeight;
            if (child.MaxHeight > 0f && height > child.MaxHeight)
                height = child.MaxHeight;

            float x;
            switch (cfg.AlignX)
            {
                case HorizontalAlignment.Left:
                    x = innerX;
                    break;
                case HorizontalAlignment.Right:
                    x = innerX + innerW - width;
                    break;
                case HorizontalAlignment.Center:
                default:
                    x = innerX + (innerW - width) * 0.5f;
                    break;
            }
            
            child.SetTargetRect(new Rect(x, y, width, height));
            y += height + cfg.ChildGap;
        }
    }

    void LayoutGrid(LayoutConfig cfg, float innerX, float innerY, float innerW, float innerH)
    {
        var cols = cfg.GridColumns;
        var cellW = cfg.GridCellWidth;
        var cellH = cfg.GridCellHeight;

        if (cellW <= 0f)
            cellW = GetGridFallbackCellWidth();
        if (cellH <= 0f)
            cellH = GetGridFallbackCellHeight();

        if (cols <= 0)
        {
            cols = innerW > 0f && cellW > 0f
                ? (int)((innerW + cfg.ChildGap) / (cellW + cfg.ChildGap))
                : 1;
        }

        if (cols < 1)
            cols = 1;

        var visibleIndex = 0;
        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var col = visibleIndex % cols;
            var row = visibleIndex / cols;
            visibleIndex++;

            var cellX = innerX + col * (cellW + cfg.ChildGap);
            var cellY = innerY + row * (cellH + cfg.ChildGap);

            var tr = child.TargetRect;
            float width = tr.Width;
            float height = tr.Height;

            if (width <= 0f)
                width = cellW;
            if (height <= 0f)
                height = cellH;

            if (child.MinWidth > 0f && width < child.MinWidth)
                width = child.MinWidth;
            if (child.MaxWidth > 0f && width > child.MaxWidth)
                width = child.MaxWidth;
            if (child.MinHeight > 0f && height < child.MinHeight)
                height = child.MinHeight;
            if (child.MaxHeight > 0f && height > child.MaxHeight)
                height = child.MaxHeight;

            if (width > cellW)
                width = cellW;
            if (height > cellH)
                height = cellH;

            float x;
            switch (cfg.AlignX)
            {
                case HorizontalAlignment.Left:
                    x = cellX;
                    break;
                case HorizontalAlignment.Right:
                    x = cellX + (cellW - width);
                    break;
                case HorizontalAlignment.Center:
                default:
                    x = cellX + (cellW - width) * 0.5f;
                    break;
            }

            float y;
            switch (cfg.AlignY)
            {
                case VerticalAlignment.Top:
                    y = cellY;
                    break;
                case VerticalAlignment.Bottom:
                    y = cellY + (cellH - height);
                    break;
                case VerticalAlignment.Middle:
                default:
                    y = cellY + (cellH - height) * 0.5f;
                    break;
            }

            child.SetTargetRect(new Rect(x, y, width, height));
        }
    }

    float GetGridFallbackCellWidth()
    {
        float max = 0f;
        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var w = child.TargetRect.Width;
            if (w > max)
                max = w;
        }

        return max > 0f ? max : 42f;
    }

    float GetGridFallbackCellHeight()
    {
        float max = 0f;
        foreach (var child in children)
        {
            if (!child.Visible)
                continue;

            var h = child.TargetRect.Height;
            if (h > max)
                max = h;
        }

        return max > 0f ? max : 42f;
    }
}

public class HorizontalGroup : UILayoutGroup
{
    public HorizontalGroup()
    {
        var cfg = Layout;
        cfg.Direction = LayoutDirection.Row;
        Layout = cfg;
    }

    protected override UIElement CreateCloneInstance()
    {
        return new HorizontalGroup();
    }
}

public class VerticalGroup : UILayoutGroup
{
    public VerticalGroup()
    {
        var cfg = Layout;
        cfg.Direction = LayoutDirection.Column;
        Layout = cfg;
    }

    protected override UIElement CreateCloneInstance()
    {
        return new VerticalGroup();
    }
}

public class GridGroup : UILayoutGroup
{
    public GridGroup()
    {
        var cfg = Layout;
        cfg.Direction = LayoutDirection.Grid;
        Layout = cfg;
    }

    protected override UIElement CreateCloneInstance()
    {
        return new GridGroup();
    }
}