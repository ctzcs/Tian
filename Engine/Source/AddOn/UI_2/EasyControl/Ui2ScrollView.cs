using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Utility;
using Foster.Framework;

namespace Engine.UI_2;

public class Ui2ScrollView : UIElement
{
    public UIElement Content { get; }

    public float ScrollOffset { get; private set; }
    public float ScrollStep { get; set; } = 40f;

    public Color Background { get; set; } = new Color(20, 20, 20, 255);
    public Color ScrollBarColor { get; set; } = new Color(0, 0, 0, 160);
    public Color ThumbColor { get; set; } = new Color(255, 255, 255, 180);

    public float BarWidth { get; set; } = 8f;
    public float MinThumbSize { get; set; } = 16f;

    readonly Ui2Slider scrollBar;

    float contentHeight;
    float viewHeight;
    float maxScroll;
    float trackHeight;
    float thumbHeight;
    bool syncingScrollBar;

    public Ui2ScrollView()
    {
        Interactable = true;
        BackgroundEnabled = false;
        ClipChildren = true;

        Content = new ColumnGroup();
        AddChild(Content);

        scrollBar = new Ui2Slider
        {
            Direction = Ui2SliderDirection.Vertical
        };
        scrollBar.ValueChanged += HandleScrollBarChanged;
        AddChild(scrollBar);
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

        var contentRect = new Rect(0f, 0f, contentWidth, rect.Height);
        Content.Arrange(contentRect);

        UpdateScrollMetrics();

        bool showBar = BarWidth > 0f && contentHeight > viewHeight;
        if (!showBar && BarWidth > 0f && contentWidth != rect.Width)
        {
            contentRect = new Rect(0f, 0f, rect.Width, rect.Height);
            Content.Arrange(contentRect);
            UpdateScrollMetrics();
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
    }

    public void ScrollBy(float delta)
    {
        SetScroll(ScrollOffset + delta);
    }

    void UpdateScrollMetrics()
    {
        viewHeight = LayoutRect.Height;
        contentHeight = 0f;

        for (int i = 0; i < Content.Children.Count; i++)
        {
            var child = Content.Children[i];
            if (!child.Display || !child.Visible)
                continue;

            var lr = child.LayoutRect;
            var bottom = lr.Y + lr.Height;
            if (bottom > contentHeight)
                contentHeight = bottom;
        }

        if (contentHeight < viewHeight)
            contentHeight = viewHeight;

        maxScroll = contentHeight - viewHeight;
        if (maxScroll < 0f)
            maxScroll = 0f;

        trackHeight = LayoutRect.Height;
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

    void HandleScrollBarChanged(Ui2Slider slider, float value)
    {
        if (syncingScrollBar)
            return;
        if (maxScroll <= 0f)
            return;

        var t = Mathf.Clamp(value, 0f, 1f);
        SetScroll(maxScroll * t);
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
        scrollBar.Max = 1f;
        scrollBar.TrackColor = ScrollBarColor;
        scrollBar.FillColor = ScrollBarColor;
        scrollBar.ThumbColor = ThumbColor;
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
            var t = ScrollOffset / maxScroll;
            var value = Mathf.Clamp(t, 0f, 1f);
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

        UpdateScrollMetrics();

        var matrix = WorldMatrix;
        var rect = new Rect(0f, 0f, size.X, size.Y);

        commands.Add(new Ui2DrawCommand(
            Ui2DrawCommandType.Background,
            rect,
            Background,
            depth,
            matrix: matrix));

        if (ClipChildren)
            commands.Add(new Ui2DrawCommand(
                Ui2DrawCommandType.ClipPush,
                GetWorldRect(),
                default,
                depth));

        int nextDepth = depth + 1;
        foreach (var child in Children)
            child.CollectDrawCommands(commands, nextDepth);

        if (ClipChildren)
            commands.Add(new Ui2DrawCommand(
                Ui2DrawCommandType.ClipPop,
                GetWorldRect(),
                default,
                depth));

    }

    public override void UpdateWorldMatrix(Matrix3x2 parentMatrix)
    {
        var local = ComputeLocalMatrix();
        WorldMatrix = parentMatrix * local;

        if (!Matrix3x2.Invert(WorldMatrix, out var inv))
            inv = Matrix3x2.Identity;

        InverseWorldMatrix = inv;

        var baseMatrix = WorldMatrix;
        var contentOffset = new Matrix3x2(
            1f, 0f,
            0f, 1f,
            0f, -ScrollOffset);

        var contentMatrix = baseMatrix * contentOffset;

        foreach (var child in Children)
        {
            if (child == Content)
                child.UpdateWorldMatrix(contentMatrix);
            else
                child.UpdateWorldMatrix(baseMatrix);
        }
    }
}

