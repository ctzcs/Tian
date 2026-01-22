namespace Engine.Core.Input;
//Player Input
public class PlayerInput
{
    //Whether if it was consumed by ImGui, if true then game viewport will not update the input info
    
    //We have mouse position
    //If In Editor -> mousePosition = Camera.GetCursorWorldPosition(ImGuiOffset,ImGuiViewportSize)
    //If In Game -> mousePosition = Camera.GetCursorWorldPosition(Point.Zero,WindowSize)
    
    
    /*
     *
     *
     * var io = ImGui.GetIO();
     * bool imguiWantsMouse = io.WantCaptureMouse;
     * bool imguiWantsKeyboard = io.WantCaptureKeyboard;
     * if (ActiveScene is EditorScene editorScene && editorScene.IsMouseOverEditor)
        {
            // 如果鼠标在 Scene 视口上，我们强制允许游戏输入
            // 这样即使 ImGui 认为它捕获了鼠标，我们也把输入权还给游戏逻辑
            imguiWantsMouse = false;
        }
     * Input.MouseConsumed = imguiWantsMouse;
     * Input.KeyboardConsumed = imguiWantsKeyboard;
     */
}