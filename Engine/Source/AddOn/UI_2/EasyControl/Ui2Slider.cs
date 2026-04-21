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

    public Subtexture? TrackSubtexture { get; set; }
    public Subtexture? FillSubtexture { get; set; }
    public Subtexture? ThumbSubtexture { get; set; }

    public Ui2ImageFillMode TrackFillMode { get; set; } = Ui2ImageFillMode.Stretch;
    public Ui2ImageFillMode FillFillMode { get; set; } = Ui2ImageFillMode.Stretch;
    public Ui2ImageFillMode ThumbFillMode { get; set; } = Ui2ImageFillMode.Stretch;

    public Vector4 TrackNineSliceBorder { get; set; }
    public Vector4 FillNineSliceBorder { get; set; }
    public Vector4 ThumbNineSliceBorder { get; set; }

    // left, top, right, bottom: legacy inner padding (used as fallback)
    public Vector4 TrackPadding { get; set; }

    // left, top, right, bottom: fill rendering area inside track
    public Vector4 FillAreaPadding { get; set; }

    // left, top, right, bottom: handle movement area inside track
    public Vector4 HandleSlideAreaPadding { get; set; }

    // Keep NineSlice fill stable when progress is very small.
    public bool ClampNineSliceFillMinSize { get; set; } = true;

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
        var handlePad = IsZeroPadding(HandleSlideAreaPadding) ? TrackPadding : HandleSlideAreaPadding;
        var handleRect = ResolveInnerRect(size, handlePad);

        float length;
        float pos;

        if (Direction == Ui2SliderDirection.Horizontal)
        {
            length = handleRect.Width;
            pos = local.X - handleRect.X;
        }
        else
        {
            length = handleRect.Height;
            pos = local.Y - handleRect.Y;
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

    void AddSkinOrColor(List<Ui2DrawCommand> commands, Rect rect, Color color, int depth, Matrix3x2 matrix,
        Subtexture? subtexture, Ui2ImageFillMode fillMode, Vector4 nineSliceBorder)
    {
        if (rect.Width <= 0f || rect.Height <= 0f)
            return;

        if (subtexture.HasValue)
        {
            if (fillMode == Ui2ImageFillMode.NineSlice)
            {
                float halfW = rect.Width * 0.5f;
                float halfH = rect.Height * 0.5f;
                float left = MathF.Min(nineSliceBorder.X, halfW);
                float right = MathF.Min(nineSliceBorder.Z, halfW);
                float top = MathF.Min(nineSliceBorder.Y, halfH);
                float bottom = MathF.Min(nineSliceBorder.W, halfH);
                nineSliceBorder = new Vector4(left, top, right, bottom);
            }

            commands.Add(new Ui2DrawCommand(
                Ui2DrawCommandType.Image,
                rect,
                color,
                depth,
                subtexture: subtexture,
                imageFillMode: fillMode,
                nineSliceBorder: nineSliceBorder,
                matrix: matrix));
        }
        else
        {
            commands.Add(new Ui2DrawCommand(
                Ui2DrawCommandType.Background,
                rect,
                color,
                depth,
                matrix: matrix));
        }
    }

    bool IsZeroPadding(Vector4 p)
        => p.X == 0f && p.Y == 0f && p.Z == 0f && p.W == 0f;

    Rect ResolveInnerRect(Vector2 size, Vector4 padding)
    {
        float x = padding.X;
        float y = padding.Y;
        float w = size.X - padding.X - padding.Z;
        float h = size.Y - padding.Y - padding.W;
        if (w < 0f) w = 0f;
        if (h < 0f) h = 0f;
        return new Rect(x, y, w, h);
    }

    Vector4 ResolveAreaPadding(Vector4 specificPadding)
        => IsZeroPadding(specificPadding) ? TrackPadding : specificPadding;

    float ComputeFillLength(float areaLength, float normalized, bool nineSlice, float borderStart, float borderEnd)
    {
        var t = Mathf.Clamp(normalized, 0f, 1f);

        // t==1 is full; for 0<=t<1 we allow NineSlice minimum-size clamp.
        if (t >= 1f)
            return areaLength;

        var length = areaLength * t;
        if (!ClampNineSliceFillMinSize || !nineSlice)
            return length;

        var minLength = MathF.Max(0f, borderStart + borderEnd);
        if (length < minLength)
            length = minLength;
        if (length > areaLength)
            length = areaLength;
        return length;
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

        AddSkinOrColor(commands, rect, TrackColor, depth, matrix, TrackSubtexture, TrackFillMode, TrackNineSliceBorder);

        var t = GetNormalized();
        Rect fillRect;
        Rect thumbRect;

        var fillArea = ResolveInnerRect(size, ResolveAreaPadding(FillAreaPadding));
        var handleArea = ResolveInnerRect(size, ResolveAreaPadding(HandleSlideAreaPadding));

        if (Direction == Ui2SliderDirection.Horizontal)
        {
            var fillWidth = ComputeFillLength(
                fillArea.Width,
                t,
                FillSubtexture.HasValue && FillFillMode == Ui2ImageFillMode.NineSlice,
                FillNineSliceBorder.X,
                FillNineSliceBorder.Z);

            fillRect = new Rect(fillArea.X, fillArea.Y, fillWidth, fillArea.Height);

            var thumbWidth = MathF.Min(ThumbSize, handleArea.Width);
            if (thumbWidth < 0f)
                thumbWidth = 0f;

            var thumbCenter = handleArea.X + handleArea.Width * t;
            var thumbX = thumbCenter - thumbWidth * 0.5f;
            if (thumbX < handleArea.X)
                thumbX = handleArea.X;
            if (thumbX + thumbWidth > handleArea.X + handleArea.Width)
                thumbX = handleArea.X + handleArea.Width - thumbWidth;

            thumbRect = new Rect(thumbX, handleArea.Y, thumbWidth, handleArea.Height);
        }
        else
        {
            var fillHeight = ComputeFillLength(
                fillArea.Height,
                t,
                FillSubtexture.HasValue && FillFillMode == Ui2ImageFillMode.NineSlice,
                FillNineSliceBorder.Y,
                FillNineSliceBorder.W);

            fillRect = new Rect(fillArea.X, fillArea.Y, fillArea.Width, fillHeight);

            var thumbHeight = MathF.Min(ThumbSize, handleArea.Height);
            if (thumbHeight < 0f)
                thumbHeight = 0f;

            var thumbCenter = handleArea.Y + handleArea.Height * t;
            var thumbY = thumbCenter - thumbHeight * 0.5f;
            if (thumbY < handleArea.Y)
                thumbY = handleArea.Y;
            if (thumbY + thumbHeight > handleArea.Y + handleArea.Height)
                thumbY = handleArea.Y + handleArea.Height - thumbHeight;

            thumbRect = new Rect(handleArea.X, thumbY, handleArea.Width, thumbHeight);
        }

        AddSkinOrColor(commands, fillRect, FillColor, depth, matrix, FillSubtexture, FillFillMode, FillNineSliceBorder);
        AddSkinOrColor(commands, thumbRect, ThumbColor, depth, matrix, ThumbSubtexture, ThumbFillMode, ThumbNineSliceBorder);

        int nextDepth = depth + 1;
        foreach (var child in Children)
            child.CollectDrawCommands(commands, nextDepth);
    }
}
