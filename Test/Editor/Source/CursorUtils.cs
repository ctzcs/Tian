using System.Numerics;

using ImGuiNET;

public class CursorUtils
{
    //获取屏幕坐标
    public static Vector2 GetScreenPosition()
    {
        return ImGui.GetMousePos();
    }

    //获取窗口坐标(含标题和边框)
    public static Vector2 GetMousePositionInWindow()
    {
        return ImGui.GetMousePos() - ImGui.GetWindowPos();
    }

    //获取内容坐标(不含标题和边框)
    //ImGui.GetContentRegionAvail() ：返回“从当前光标位置到内容区域右下角”剩余可用空间的大小（宽高），单位是当前窗口的本地坐标。
    //ImGui.GetWindowContentRegionMin()：返回“内容区域左上角相对窗口左上角”的偏移（位置），单位是当前窗口的本地坐标。这个值加上 ImGui.GetWindowPos() 可得到内容区域左上角的屏幕坐标。
    public static Vector2 GetMousePositionInContent()
    {
        return ImGui.GetMousePos() - (ImGui.GetWindowPos() + ImGui.GetContentRegionAvail());
    }

    public static Vector2 GetMousePositionInContentRect(Vector2 startPos)
    {
        //Log.Info($"Mouse Position: {ImGui.GetMousePos()} Start Pos: {startPos}");
        return ImGui.GetMousePos() - startPos;
        
    }
    
    public static System.Numerics.Vector2 MouseLocalInCurrentWindow()
    {
        var windowPos = ImGuiNET.ImGui.GetWindowPos();                  // 窗口左上角（屏幕）
        var cursorStartLocal = ImGuiNET.ImGui.GetCursorStartPos();      // 内容起点（相对窗口）
        var scroll = new System.Numerics.Vector2(
            ImGuiNET.ImGui.GetScrollX(),
            ImGuiNET.ImGui.GetScrollY()
        );

        // 当前可见内容原点（屏幕坐标）
        var contentOriginScreen = windowPos + cursorStartLocal - scroll;

        // 鼠标相对内容区域局部坐标（像素）
        return ImGuiNET.ImGui.GetMousePos() - contentOriginScreen;
    }

    // 鼠标是否在当前内容矩形内（近似）：用 CursorStartPos + ContentRegionAvail 估算
    public static (System.Numerics.Vector2 local, bool inside) MouseInCurrentContent()
    {
        var windowPos = ImGuiNET.ImGui.GetWindowPos();
        var cursorStartLocal = ImGuiNET.ImGui.GetCursorStartPos();
        var scroll = new System.Numerics.Vector2(ImGuiNET.ImGui.GetScrollX(), ImGuiNET.ImGui.GetScrollY());

        var min = windowPos + cursorStartLocal - scroll;
        var size = ImGuiNET.ImGui.GetContentRegionAvail();   // 若在窗口开始处调用，等于全内容尺寸
        var max = min + size;

        var mouse = ImGuiNET.ImGui.GetMousePos();
        var local = mouse - min;
        bool inside = local.X >= 0 && local.Y >= 0 && local.X <= size.X && local.Y <= size.Y;

        return (local, inside);
    }
    

}