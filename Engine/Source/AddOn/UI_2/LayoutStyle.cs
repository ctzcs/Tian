using Foster.Framework;

namespace Engine.UI_2;

public enum LayoutType
{
    None,
    Row,
    Column,
    Grid,
    Absolute
}

public enum HorizontalAlignment
{
    Start,
    Center,
    End,
    Stretch
}

public enum VerticalAlignment
{
    Start,
    Center,
    End,
    Stretch
}

public enum LayoutSizeMode
{
    Pixel,
    ViewportRatio
}

public struct LayoutStyle
{
    public LayoutType LayoutType;
    public float Width;
    public float Height;
    public float MinWidth;
    public float MinHeight;
    public float MaxWidth;
    public float MaxHeight;
    public float MarginLeft;
    public float MarginRight;
    public float MarginTop;
    public float MarginBottom;
    public float Grow;
    public float Shrink;
    public HorizontalAlignment AlignX;
    public VerticalAlignment AlignY;
    public LayoutSizeMode SizeMode;
    public Rect ViewportRatio;
}

public struct ChildrenLayoutStyle
{
    public LayoutType LayoutType;
    public float PaddingLeft;
    public float PaddingRight;
    public float PaddingTop;
    public float PaddingBottom;
    public HorizontalAlignment AlignX;
    public VerticalAlignment AlignY;
}
