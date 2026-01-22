using Foster.Framework;
using Friflo.Engine.ECS;

namespace Engine.Components;
//TODO 这里需要改成一个不需要位置的Rect,如果没有Transform就无法绘制
public struct CheckBox:IComponent
{
    public bool IsEnable;
    public Rect rect;
    public RectPivot Pivot;
}

public enum RectPivot
{
    BottomCenter = 0,
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    TopCenter,
    LeftCenter,
    RightCenter,
}