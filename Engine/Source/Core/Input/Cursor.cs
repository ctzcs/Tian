using System.Numerics;

namespace Engine.Core.Input;

public static class Cursor
{
    /// <summary>
    /// 0，1的范围
    /// </summary>
    public static Vector2 ViewportPosition;
    
    //TODO 按键的点击啥的，也应该在这里处理, 主要编辑器中点击也会触发Scene中的点击
    // 其实感觉Editor中Run Game也应该改一下，应该是重新new Game(); 然后就能跑了
    public static bool IsOnGameUi;
    
    /// <summary>
    /// TODO 可能类似focus
    /// </summary>
    /// <returns></returns>
    public static bool IsInViewport()
    {
        return ViewportPosition is { X: >= 0 and <= 1, Y: >= 0 and <= 1 };
    }
    
    public static bool CanGameUse()
    {
        //不在GameUI上，且在Viewport中
        return !IsOnGameUi && IsInViewport();
    }
    
    
    
    public static Vector2 GetScreenPosition(Vector2 windowSizeInPixels)
    {
        return ViewportPosition * windowSizeInPixels;
    }
}