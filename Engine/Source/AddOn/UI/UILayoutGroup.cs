using System.Collections.Generic;
using Engine.Core.Structure;
using Foster.Framework;

namespace Engine.UI;

/// <summary>
/// 布局方向
/// </summary>
public enum LayoutDirection
{
    Row,
    Column
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
            AutoWidth = false,
            AutoHeight = false
        };
    }

    protected override UIElement CreateCloneInstance()
    {
        return new UILayoutGroup();
    }

    //先让子节点 Apply，再自己 UpdateLayout ，是「自下而上」的顺序。
    //父 group 的 AutoWidth / AutoHeight 要靠 子元素的 TargetRect 来算
    public override void Apply()
    {
        foreach (var child in children)
            child.Apply();

        if (layoutDirty)
        {
            UpdateLayout();
            layoutDirty = false;
        }
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
        else // Column
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
        else
            LayoutColumn(cfg, innerX, innerY, innerW, innerH);
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