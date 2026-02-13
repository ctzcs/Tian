using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public enum Ui2DrawCommandType
{
    Background,
    Text,
    Image,
    ClipPush,
    ClipPop
}

public enum Ui2TextOverflowMode
{
    None,
    ShrinkToFit,
    Wrap,
    WrapAutoHeight,
    ShrinkAndWrap
}

public enum Ui2ImageFillMode
{
    Original,
    Stretch,
    Fit,
    NineSlice
}

public readonly struct Ui2DrawCommand
{
    public readonly Ui2DrawCommandType Type;
    public readonly Rect Rect;
    public readonly Color Color;
    public readonly int Depth;
    public readonly Matrix3x2 Matrix;
    public readonly string? Text;
    public readonly float TextSize;
    public readonly Vector2 TextAlign;
    public readonly Ui2TextOverflowMode TextOverflow;
    public readonly Subtexture? Subtexture;
    public readonly Ui2ImageFillMode ImageFillMode;
    public readonly Vector4 NineSliceBorder;

    public Ui2DrawCommand(
        Ui2DrawCommandType type,
        Rect rect,
        Color color,
        int depth,
        string? text = null,
        float textSize = 0f,
        Vector2? textAlign = null,
        Ui2TextOverflowMode textOverflow = Ui2TextOverflowMode.None,
        Subtexture? subtexture = null,
        Ui2ImageFillMode imageFillMode = Ui2ImageFillMode.Stretch,
        Vector4? nineSliceBorder = null,
        Matrix3x2? matrix = null)
    {
        Type = type;
        Rect = rect;
        Color = color;
        Depth = depth;
        Matrix = matrix ?? Matrix3x2.Identity;
        Text = text;
        TextSize = textSize;
        TextAlign = textAlign ?? Vector2.Zero;
        TextOverflow = textOverflow;
        Subtexture = subtexture;
        ImageFillMode = imageFillMode;
        NineSliceBorder = nineSliceBorder ?? Vector4.Zero;
    }
}
