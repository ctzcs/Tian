using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Utility;
using Foster.Framework;

namespace Engine.UI_2;

public class Ui2ScrollView : UIElement
{
    public UIElement Viewport { get; }
    public UIElement Content { get; }

    public float ScrollOffset { get; private set; }
    public float ScrollStep { get; set; } = 40f;

    public Color Background { get; set; } = new Color(20, 20, 20, 255);
    public Color ScrollBarColor { get; set; } = new Color(0, 0, 0, 160);
    public Color ThumbColor { get; set; } = new Color(255, 255, 255, 180);

    public float BarWidth { get; set; } = 8f;
    public float MinThumbSize { get; set; } = 16f;
    public bool AlwaysShowBar { get; set; } = false;

    readonly Ui2Slider scrollBar;

    float contentHeight;
    float viewHeight;
    float maxScroll;
    float trackHeight;
    float thumbHeight;
    bool syncingScrollBar;
    bool draggingContent;
    float dragLastY;

    public Ui2ScrollView()
    {
        Interactable = true;
        BackgroundEnabled = false;
        ClipChildren = false;

        ChildrenLayout.LayoutType = LayoutType.Absolute;

        Viewport = new UIElement
        {
            ClipChildren = true,
            BackgroundEnabled = false
        };
        Viewport.ChildrenLayout.LayoutType = LayoutType.Absolute;
        AddChild(Viewport);

        Content = new VContainer
        {
            AnimateLayout = false
        };
        Viewport.AddChild(Content);

        scrollBar = new Ui2Slider
        {
            Direction = Ui2SliderDirection.Vertical
        };
        scrollBar.ValueChanged += HandleScrollBarChanged;
        AddChild(scrollBar);

        OnPointerDown += HandlePointerDown;
        OnPointerMove += HandlePointerMove;
        OnPointerUp += HandlePointerUp;
    }

    public override void Arrange(Rect rect)
    {
        base.Arrange(rect);

        float contentWidth = rect.Width;
        if (BarWidth > 0f)
        {
            contentWidth = rect.Width - BarWidth;
            if (contentWidth < 0f)
                contentWidth = 0f;
        }

        var viewportRect = new Rect(0f, 0f, contentWidth, rect.Height);
        Viewport.Arrange(viewportRect);

        var contentRect = new Rect(0f, -ScrollOffset, contentWidth, rect.Height);
        Content.Arrange(contentRect);

        UpdateScrollMetrics(contentWidth, rect.Height);

        bool showBar = BarWidth > 0f && (AlwaysShowBar || contentHeight > viewHeight);
        if (!showBar && BarWidth > 0f && contentWidth != rect.Width)
        {
            viewportRect = new Rect(0f, 0f, rect.Width, rect.Height);
            Viewport.Arrange(viewportRect);

            contentRect = new Rect(0f, -ScrollOffset, rect.Width, rect.Height);
            Content.Arrange(contentRect);
            UpdateScrollMetrics(rect.Width, rect.Height);
        }

        UpdateScrollBarState(rect, showBar);
    }

    public void SetScroll(float offset)
    {
        if (maxScroll <= 0f)
        {
            ScrollOffset = 0f;
            return;
        }

        var clamped = Mathf.Clamp(offset, 0f, maxScroll);
        if (MathF.Abs(clamped - ScrollOffset) < 0.0001f)
            return;

        ScrollOffset = clamped;

        var lr = Content.LayoutRect;
        Content.Arrange(new Rect(lr.X, -ScrollOffset, lr.Width, lr.Height));
    }

    public void ScrollBy(float delta)
    {
        SetScroll(ScrollOffset + delta);
    }

    void HandlePointerDown(Ui2PointerEvent e)
    {
        if (maxScroll <= 0f)
            return;

        var local = Vector2.Transform(e.Position, InverseWorldMatrix);
        if (scrollBar.Display && BarWidth > 0f && local.X >= Viewport.LayoutRect.Width)
            return;

        draggingContent = true;
        dragLastY = local.Y;
    }

    void HandlePointerMove(Ui2PointerEvent e)
    {
        if (!draggingContent)
            return;

        var local = Vector2.Transform(e.Position, InverseWorldMatrix);
        var deltaY = local.Y - dragLastY;
        dragLastY = local.Y;
        ScrollBy(-deltaY);
    }

    void HandlePointerUp(Ui2PointerEvent e)
    {
        draggingContent = false;
    }

    float MeasureArrangedContentHeight()
    {
        float maxBottom = Content.ChildrenLayout.PaddingTop;

        foreach (var child in Content.Children)
        {
            if (!child.Display || !child.Visible)
                continue;

            var style = child.Layout;
            float bottom = child.TargetRect.Y + child.TargetRect.Height + style.MarginBottom;
            if (bottom > maxBottom)
                maxBottom = bottom;
        }

        return maxBottom + Content.ChildrenLayout.PaddingBottom;
    }

    void UpdateScrollMetrics(float contentWidth, float viewportHeight)
    {
        viewHeight = viewportHeight;
        if (viewHeight < 0f)
            viewHeight = 0f;

        var measured = Content.Measure(new Vector2(contentWidth, viewportHeight));
        var arrangedHeight = MeasureArrangedContentHeight();
        contentHeight = measured.Y;
        if (arrangedHeight > contentHeight)
            contentHeight = arrangedHeight;
        if (contentHeight < viewHeight)
            contentHeight = viewHeight;

        maxScroll = contentHeight - viewHeight;
        if (maxScroll < 0f)
            maxScroll = 0f;

        trackHeight = viewportHeight;
        if (trackHeight < 0f)
            trackHeight = 0f;

        if (contentHeight <= 0f || trackHeight <= 0f)
        {
            thumbHeight = 0f;
            return;
        }

        var visibleRatio = viewHeight / contentHeight;
        thumbHeight = trackHeight * visibleRatio;
        if (thumbHeight < MinThumbSize)
            thumbHeight = MinThumbSize;
        if (thumbHeight > trackHeight)
            thumbHeight = trackHeight;
    }

    Rect GetThumbRect()
    {
        if (trackHeight <= 0f || thumbHeight <= 0f || contentHeight <= viewHeight)
            return new Rect(0f, 0f, 0f, 0f);

        var t = maxScroll <= 0f ? 0f : ScrollOffset / maxScroll;
        var maxThumbPos = trackHeight - thumbHeight;
        var thumbY = maxThumbPos * t;

        var x = LayoutRect.Width - BarWidth;
        if (x < 0f)
            x = 0f;

        return new Rect(x, thumbY, BarWidth, thumbHeight);
    }

    float GetCurrentScrollValue()
    {
        var value = -Content.TargetRect.Y;
        if (value < 0f)
            value = 0f;
        if (value > maxScroll)
            value = maxScroll;
        return value;
    }

    void HandleScrollBarChanged(Ui2Slider slider, float value)
    {
        if (syncingScrollBar)
            return;
        if (maxScroll <= 0f)
            return;

        SetScroll(value);
    }

    void UpdateScrollBarState(Rect rect, bool showBar)
    {
        if (!showBar)
        {
            scrollBar.Display = false;
            scrollBar.Visible = false;
            scrollBar.Interactable = false;
            return;
        }

        scrollBar.Display = true;
        scrollBar.Visible = true;
        scrollBar.Interactable = true;
        scrollBar.Direction = Ui2SliderDirection.Vertical;
        scrollBar.Min = 0f;
        scrollBar.Max = maxScroll > 0f ? maxScroll : 1f;
        scrollBar.TrackColor = ScrollBarColor;
        scrollBar.FillColor = Color.Transparent;
        scrollBar.ThumbColor = ThumbColor;
        scrollBar.FillAreaPadding = Vector4.Zero;
        scrollBar.HandleSlideAreaPadding = Vector4.Zero;
        scrollBar.ThumbSize = thumbHeight;

        float barX = rect.Width - BarWidth;
        if (barX < 0f)
            barX = 0f;

        scrollBar.Arrange(new Rect(barX, 0f, BarWidth, rect.Height));

        syncingScrollBar = true;
        if (maxScroll <= 0f)
        {
            scrollBar.SetValue(0f, false);
        }
        else
        {
            var value = GetCurrentScrollValue();
            scrollBar.SetValue(value, false);
        }
        syncingScrollBar = false;
    }

    public override void CollectDrawCommands(List<Ui2DrawCommand> commands, int depth)
    {
        if (!Visible || !Display)
            return;

        var size = new Vector2(LayoutRect.Width, LayoutRect.Height);
        if (size.X <= 0f || size.Y <= 0f)
            return;

        UpdateScrollMetrics(Viewport.TargetRect.Width, Viewport.TargetRect.Height);

        var matrix = WorldMatrix;
        var rect = new Rect(0f, 0f, size.X, size.Y);

        commands.Add(new Ui2DrawCommand(
            Ui2DrawCommandType.Background,
            rect,
            Background,
            depth,
            matrix: matrix));

        int nextDepth = depth + 1;
        foreach (var child in Children)
            child.CollectDrawCommands(commands, nextDepth);

    }

    public override void UpdateWorldMatrix(Matrix3x2 parentMatrix)
    {
        base.UpdateWorldMatrix(parentMatrix);
    }
}

