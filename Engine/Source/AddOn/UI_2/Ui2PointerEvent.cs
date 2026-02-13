using System.Numerics;

namespace Engine.UI_2;

public struct Ui2PointerEvent
{
    public UIElement Target; // 整次交互里“真正被点中”的元素
    public UIElement Current; // 当前正在处理这个事件的元素（冒泡链上的一层）
    public Vector2 Position;  // 当前指针位置
}

