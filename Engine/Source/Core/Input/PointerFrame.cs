using System.Numerics;

namespace Engine.Core.Input;

public struct PointerFrame
{
    public Vector2 ScreenPosition;      // 屏幕坐标
    public Vector2 ContentPosition;     // 渲染Target坐标（像素）
    public Vector2 WorldPosition;       // 世界坐标（可选，无相机时可与 Content 一致）
    public Vector2 DeltaContentPosition;// 本帧内容坐标位移
    public float Wheel;         // 本帧滚轮增量

    public bool LeftDown;
    public bool LeftPressed;
    public bool LeftReleased;

    public bool RightDown;
    public bool RightPressed;
    public bool RightReleased;

    public bool MiddleDown;
    public bool MiddlePressed;
    public bool MiddleReleased;

    public bool Shift;
    public bool Ctrl;
    public bool Alt;

    public bool InsideViewport; // 是否在绘制矩形内
    public bool IsHoverOnUi;
}
