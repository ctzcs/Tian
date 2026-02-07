using System;
using System.Numerics;
using Engine.Utility;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Engine.UI;

public class ScrollView : UIElement, IInputListener
{
    public readonly UIElement Viewport;
    public readonly UILayoutGroup Content;
    public readonly UIElement ScrollBar;
    public readonly UIElement ScrollThumb;

    public float ScrollOffset { get; private set; }
    public float ScrollStep { get; set; } = 40f;
    public float BarWidth { get; set; } = 8f;
    public float MinThumbSize { get; set; } = 16f;
    public bool AlwaysShowBar { get; set; }
    public bool EnableWheel { get; set; } = true;

    float contentHeight;
    float viewHeight;
    float maxScroll;
    float trackHeight;
    float thumbHeight;

    readonly ScrollBarTrack barTrack;
    readonly ScrollBarThumb barThumb;

    public ScrollView(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
        : base(maskable, selectable, visible, rect, parent)
    {
        Viewport = new UIElement(new Rect(0, 0, rect.Width, rect.Height))
        {
            Maskable = true,
            Selectable = false,
            Interactable = false,
            AnimateLayout = false
        };

        Content = new VerticalGroup
        {
            AnimateLayout = false,
            Interactable = false,
            Selectable = false
        };

        var cfg = Content.Layout;
        cfg.AutoHeight = true;
        cfg.AutoWidth = false;
        Content.Layout = cfg;

        Viewport.AddChild(Content);

        barTrack = new ScrollBarTrack(this, new Rect(0, 0, 0, 0))
        {
            AnimateLayout = false
        };
        barTrack.BackgroundColor = new Color(0, 0, 0, 80);

        barThumb = new ScrollBarThumb(this, new Rect(0, 0, 0, 0))
        {
            AnimateLayout = false
        };
        barThumb.BackgroundColor = new Color(255, 255, 255, 160);

        barTrack.AddChild(barThumb);

        ScrollBar = barTrack;
        ScrollThumb = barThumb;

        AddChild(Viewport);
        AddChild(barTrack);
    }

    public ScrollView(Rect rect, UIElement? parent = null)
        : this(maskable: true, selectable: true, visible: true, rect: rect, parent: parent)
    {
    }

    public ScrollView() : this(new Rect(0, 0, 0, 0))
    {
    }

    public float ScrollNormalized
    {
        get => maxScroll <= 0f ? 0f : ScrollOffset / maxScroll;
        set => SetScroll(maxScroll * Mathf.Clamp01(value));
    }

    public void SetScroll(float value)
    {
        var clamped = Mathf.Clamp(value, 0f, maxScroll);
        if (MathF.Abs(clamped - ScrollOffset) < 0.001f)
            return;
        ScrollOffset = clamped;
        InvalidateLayout();
    }

    protected override void UpdateLayout()
    {
        base.UpdateLayout();

        var needBar = AlwaysShowBar;
        float viewWidth = TargetRect.Width - (AlwaysShowBar ? BarWidth : 0f);
        if (viewWidth < 0f) viewWidth = 0f;

        var viewRect = new Rect(0, 0, viewWidth, TargetRect.Height);
        Viewport.SetTargetRect(viewRect);

        Content.SetTargetRect(new Rect(0, 0, viewRect.Width, Content.TargetRect.Height));
        Content.UpdateLayoutNow(true);

        contentHeight = Content.TargetRect.Height;
        viewHeight = viewRect.Height;

        if (!AlwaysShowBar)
            needBar = contentHeight > viewHeight + 0.01f;

        if (!needBar)
        {
            viewRect = new Rect(0, 0, TargetRect.Width, TargetRect.Height);
            Viewport.SetTargetRect(viewRect);
            Content.SetTargetRect(new Rect(0, 0, viewRect.Width, Content.TargetRect.Height));
            Content.UpdateLayoutNow(true);
            contentHeight = Content.TargetRect.Height;
            viewHeight = viewRect.Height;
        }

        maxScroll = MathF.Max(0f, contentHeight - viewHeight);
        ScrollOffset = Mathf.Clamp(ScrollOffset, 0f, maxScroll);

        Content.SetTargetRect(new Rect(0, -ScrollOffset, viewRect.Width, contentHeight));

        barTrack.Visible = needBar;
        barTrack.Selectable = needBar;
        barTrack.Interactable = needBar;

        if (!needBar)
        {
            barThumb.CancelDrag();
            return;
        }

        var barRect = new Rect(viewRect.Width, 0, BarWidth, TargetRect.Height);
        barTrack.SetTargetRect(barRect);

        trackHeight = barRect.Height;
        var ratio = contentHeight <= 0f ? 1f : viewHeight / contentHeight;
        thumbHeight = MathF.Max(MinThumbSize, trackHeight * ratio);
        if (thumbHeight > trackHeight) thumbHeight = trackHeight;

        var maxThumbY = MathF.Max(0f, trackHeight - thumbHeight);
        var thumbY = maxScroll > 0f ? (ScrollOffset / maxScroll) * maxThumbY : 0f;

        barThumb.SetTargetRect(new Rect(0, thumbY, barRect.Width, thumbHeight));
    }

    bool IInputListener.OnMouseScrolled(UiFrame state)
    {
        if (!EnableWheel)
            return false;

        if (maxScroll <= 0f)
            return false;

        var delta = -state.Mouse.Wheel.Y * ScrollStep;
        if (MathF.Abs(delta) <= 0.001f)
            return false;

        SetScroll(ScrollOffset + delta);
        return true;
    }

    bool IInputListener.OnPointerDown(UiFrame state) => false;
    bool IInputListener.OnRightPointerDown(UiFrame state) => false;

    internal void DragThumbTo(float localY)
    {
        if (maxScroll <= 0f)
        {
            SetScroll(0f);
            return;
        }

        var maxThumbY = MathF.Max(0f, trackHeight - thumbHeight);
        var clamped = Mathf.Clamp(localY, 0f, maxThumbY);
        var ratio = maxThumbY <= 0f ? 0f : clamped / maxThumbY;
        SetScroll(ratio * maxScroll);
    }

    sealed class ScrollBarTrack : UIElement, IInputListener
    {
        readonly ScrollView view;

        public ScrollBarTrack(ScrollView view, Rect rect)
            : base(maskable: false, selectable: true, visible: true, rect: rect)
        {
            this.view = view;
        }

        public bool OnPointerDown(UiFrame state)
        {
            var barWorld = WorldRect;
            var localY = state.targetPosition.Y - barWorld.Y - view.thumbHeight * 0.5f;
            view.DragThumbTo(localY);
            return true;
        }

        public bool OnRightPointerDown(UiFrame state) => false;
    }

    sealed class ScrollBarThumb : UIElement, IInputListener
    {
        readonly ScrollView view;
        bool dragging;
        float grabOffset;

        public ScrollBarThumb(ScrollView view, Rect rect)
            : base(maskable: false, selectable: true, visible: true, rect: rect)
        {
            this.view = view;
        }

        public bool OnPointerDown(UiFrame state)
        {
            dragging = true;
            var wr = WorldRect;
            grabOffset = state.targetPosition.Y - wr.Y;
            return true;
        }

        public bool OnRightPointerDown(UiFrame state) => false;

        public void OnPointerMoved(UiFrame state)
        {
            if (!dragging)
                return;

            var barWorld = view.ScrollBar.WorldRect;
            var localY = state.targetPosition.Y - barWorld.Y - grabOffset;
            view.DragThumbTo(localY);
        }

        public void OnPointerUp(UiFrame state)
        {
            dragging = false;
        }

        public void CancelDrag()
        {
            dragging = false;
        }
    }
}