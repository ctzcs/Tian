using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Utility;
using Foster.Framework;

namespace Engine.UI_2;

public enum Ui2SliderDirection
{
    Horizontal,
    Vertical
}

public class Ui2Slider : UIElement
{
    public float Min { get; set; } = 0f;
    public float Max { get; set; } = 1f;
    public float Value { get; private set; }
    public float ThumbSize { get; set; } = 16f;
    public Ui2SliderDirection Direction { get; set; } = Ui2SliderDirection.Horizontal;

    public Color TrackColor { get; set; } = new Color(40, 40, 40, 255);
    public Color FillColor { get; set; } = new Color(80, 120, 220, 255);
    public Color ThumbColor { get; set; } = new Color(230, 230, 230, 255);

    public event Action<Ui2Slider, float>? ValueChanged;

    bool dragging;

    public Ui2Slider()
    {
        Interactable = true;
        BackgroundEnabled = false;

        OnPointerDown += HandlePointerDown;
        OnPointerMove += HandlePointerMove;
        OnPointerUp += HandlePointerUp;
    }

    public void SetValue(float value, bool notify = true)
    {
        var range = Max - Min;
        var clamped = range <= 0f ? Min : Mathf.Clamp(value, Min, Max);
        if (MathF.Abs(clamped - Value) < 0.0001f)
            return;

        Value = clamped;
        if (notify)
            ValueChanged?.Invoke(this, Value);
    }

    float GetNormalized()
    {
        var range = Max - Min;
        if (range <= 0f)
            return 0f;
        return (Value - Min) / range;
    }

    void SetFromLocal(Vector2 local, bool notify)
    {
        var size = new Vector2(LayoutRect.Width, LayoutRect.Height);
        float length;
        float pos;

        if (Direction == Ui2SliderDirection.Horizontal)
        {
            length = size.X;
            pos = local.X;
        }
        else
        {
            length = size.Y;
            pos = local.Y;
        }

        if (length <= 0f)
            return;

        var t = Mathf.Clamp(pos / length, 0f, 1f);
        var range = Max - Min;
        var value = Min + t * range;
        SetValue(value, notify);
    }

    void HandlePointerDown(Ui2PointerEvent e)
    {
        var local = Vector2.Transform(e.Position, InverseWorldMatrix);
        SetFromLocal(local, true);
        dragging = true;
    }

    void HandlePointerMove(Ui2PointerEvent e)
    {
        if (!dragging)
            return;

        var local = Vector2.Transform(e.Position, InverseWorldMatrix);
        SetFromLocal(local, true);
    }

    void HandlePointerUp(Ui2PointerEvent e)
    {
        dragging = false;
    }

    public override void CollectDrawCommands(List<Ui2DrawCommand> commands, int depth)
    {
        if (!Visible || !Display)
            return;

        var size = new Vector2(LayoutRect.Width, LayoutRect.Height);
        if (size.X <= 0f || size.Y <= 0f)
            return;

        var rect = new Rect(0f, 0f, size.X, size.Y);
        var matrix = WorldMatrix;

        commands.Add(new Ui2DrawCommand(
            Ui2DrawCommandType.Background,
            rect,
            TrackColor,
            depth,
            matrix: matrix));

        var t = GetNormalized();
        Rect fillRect;
        Rect thumbRect;

        if (Direction == Ui2SliderDirection.Horizontal)
        {
            var fillWidth = size.X * t;
            if (fillWidth < 0f)
                fillWidth = 0f;
            if (fillWidth > size.X)
                fillWidth = size.X;

            fillRect = new Rect(0f, 0f, fillWidth, size.Y);

            var thumbWidth = MathF.Min(ThumbSize, size.X);
            if (thumbWidth < 0f)
                thumbWidth = 0f;

            var thumbCenter = size.X * t;
            var thumbX = thumbCenter - thumbWidth * 0.5f;
            if (thumbX < 0f)
                thumbX = 0f;
            if (thumbX + thumbWidth > size.X)
                thumbX = size.X - thumbWidth;

            thumbRect = new Rect(thumbX, 0f, thumbWidth, size.Y);
        }
        else
        {
            var fillHeight = size.Y * t;
            if (fillHeight < 0f)
                fillHeight = 0f;
            if (fillHeight > size.Y)
                fillHeight = size.Y;

            fillRect = new Rect(0f, 0f, size.X, fillHeight);

            var thumbHeight = MathF.Min(ThumbSize, size.Y);
            if (thumbHeight < 0f)
                thumbHeight = 0f;

            var thumbCenter = size.Y * t;
            var thumbY = thumbCenter - thumbHeight * 0.5f;
            if (thumbY < 0f)
                thumbY = 0f;
            if (thumbY + thumbHeight > size.Y)
                thumbY = size.Y - thumbHeight;

            thumbRect = new Rect(0f, thumbY, size.X, thumbHeight);
        }

        commands.Add(new Ui2DrawCommand(
            Ui2DrawCommandType.Background,
            fillRect,
            FillColor,
            depth,
            matrix: matrix));

        commands.Add(new Ui2DrawCommand(
            Ui2DrawCommandType.Background,
            thumbRect,
            ThumbColor,
            depth,
            matrix: matrix));

        int nextDepth = depth + 1;
        foreach (var child in Children)
            child.CollectDrawCommands(commands, nextDepth);
    }
}
