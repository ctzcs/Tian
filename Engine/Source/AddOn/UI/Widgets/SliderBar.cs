using System;
using System.Numerics;
using Engine.Utility;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Engine.UI;

public enum SliderDirection
{
    Horizontal,
    Vertical
}

public class SliderBar : UIElement, IInputListener
{
    public readonly UIElement Track;
    public readonly UIElement Fill;
    public readonly UIElement Thumb;

    public float Min { get; set; } = 0f;
    public float Max { get; set; } = 1f;
    public float Value { get; private set; }
    public float ThumbSize { get; set; } = 16f;
    public SliderDirection Direction { get; set; } = SliderDirection.Horizontal;

    public event Action<SliderBar, float>? ValueChanged;

    float trackLength;
    float thumbLength;

    readonly SliderTrack track;
    readonly UIElement fill;
    readonly SliderThumb thumb;

    public SliderBar(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
        : base(maskable, selectable, visible, rect, parent)
    {
        track = new SliderTrack(this, new Rect(0, 0, 0, 0))
        {
            AnimateLayout = false
        };
        track.BackgroundColor = new Color(0, 0, 0, 80);

        fill = new UIElement(new Rect(0, 0, 0, 0))
        {
            AnimateLayout = false,
            Selectable = false,
            Interactable = false
        };
        fill.BackgroundColor = new Color(80, 120, 220, 180);

        thumb = new SliderThumb(this, new Rect(0, 0, 0, 0))
        {
            AnimateLayout = false
        };
        thumb.BackgroundColor = new Color(255, 255, 255, 160);

        track.AddChild(fill);
        track.AddChild(thumb);

        Track = track;
        Fill = fill;
        Thumb = thumb;

        AddChild(track);
    }

    public SliderBar(Rect rect, UIElement? parent = null)
        : this(maskable: true, selectable: true, visible: true, rect: rect, parent: parent)
    {
    }

    public SliderBar() : this(new Rect(0, 0, 0, 0))
    {
    }

    public float Normalized
    {
        get
        {
            var range = Max - Min;
            return range <= 0f ? 0f : (Value - Min) / range;
        }
        set
        {
            var range = MathF.Max(0f, Max - Min);
            SetValue(Min + Mathf.Clamp01(value) * range);
        }
    }

    public void SetValue(float value, bool notify = true)
    {
        var range = Max - Min;
        var clamped = range <= 0f ? Min : Mathf.Clamp(value, Min, Max);
        if (MathF.Abs(clamped - Value) < 0.001f)
            return;

        Value = clamped;
        InvalidateLayout();

        if (notify)
            ValueChanged?.Invoke(this, Value);
    }

    protected override void UpdateLayout()
    {
        base.UpdateLayout();

        track.SetTargetRect(new Rect(0, 0, TargetRect.Width, TargetRect.Height));

        trackLength = Direction == SliderDirection.Horizontal
            ? track.TargetRect.Width
            : track.TargetRect.Height;

        thumbLength = MathF.Min(ThumbSize, trackLength);
        if (thumbLength < 0f)
            thumbLength = 0f;

        var maxThumb = MathF.Max(0f, trackLength - thumbLength);
        var range = MathF.Max(0f, Max - Min);
        var t = range <= 0f ? 0f : (Value - Min) / range;
        var pos = maxThumb * t;

        var fillLength = MathF.Min(trackLength, pos + thumbLength * 0.5f);
        if (fillLength < 0f)
            fillLength = 0f;

        var fillRect = Direction == SliderDirection.Horizontal
            ? new Rect(0, 0, fillLength, track.TargetRect.Height)
            : new Rect(0, 0, track.TargetRect.Width, fillLength);
        fill.SetTargetRect(fillRect);

        var thumbRect = Direction == SliderDirection.Horizontal
            ? new Rect(pos, 0, thumbLength, track.TargetRect.Height)
            : new Rect(0, pos, track.TargetRect.Width, thumbLength);

        thumb.SetTargetRect(thumbRect);
    }

    internal void DragTo(float localPos)
    {
        var maxThumb = MathF.Max(0f, trackLength - thumbLength);
        var clamped = Mathf.Clamp(localPos, 0f, maxThumb);

        var range = MathF.Max(0f, Max - Min);
        var t = maxThumb <= 0f ? 0f : clamped / maxThumb;
        var value = Min + t * range;

        SetValue(value);
    }

    bool IInputListener.OnPointerDown(UiFrame state) => false;
    bool IInputListener.OnRightPointerDown(UiFrame state) => false;

    sealed class SliderTrack : UIElement, IInputListener
    {
        readonly SliderBar bar;

        public SliderTrack(SliderBar bar, Rect rect)
            : base(maskable: false, selectable: true, visible: true, rect: rect)
        {
            this.bar = bar;
        }

        public bool OnPointerDown(UiFrame state)
        {
            var wr = WorldRect;
            var local = bar.Direction == SliderDirection.Horizontal
                ? state.targetPosition.X - wr.X - bar.thumbLength * 0.5f
                : state.targetPosition.Y - wr.Y - bar.thumbLength * 0.5f;

            bar.DragTo(local);
            return true;
        }

        public bool OnRightPointerDown(UiFrame state) => false;
    }

    sealed class SliderThumb : UIElement, IInputListener
    {
        readonly SliderBar bar;
        bool dragging;
        float grabOffset;

        public SliderThumb(SliderBar bar, Rect rect)
            : base(maskable: false, selectable: true, visible: true, rect: rect)
        {
            this.bar = bar;
        }

        public bool OnPointerDown(UiFrame state)
        {
            dragging = true;
            var wr = WorldRect;
            grabOffset = bar.Direction == SliderDirection.Horizontal
                ? state.targetPosition.X - wr.X
                : state.targetPosition.Y - wr.Y;
            return true;
        }

        public bool OnRightPointerDown(UiFrame state) => false;

        public void OnPointerMoved(UiFrame state)
        {
            if (!dragging)
                return;

            var wr = bar.Track.WorldRect;
            var local = bar.Direction == SliderDirection.Horizontal
                ? state.targetPosition.X - wr.X - grabOffset
                : state.targetPosition.Y - wr.Y - grabOffset;

            bar.DragTo(local);
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