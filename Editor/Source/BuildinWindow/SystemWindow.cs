using ImGuiNET;

namespace Editor;

public class SystemWindow:EditorWindow
{
    public override void Update()
    {
        if (ImGui.Begin("System"))
        {
            if (ImGui.CollapsingHeader("[SystemGroup]"))
            {
                if (ImGui.TreeNode("SystemA"))
                {
                    ImGui.TreePop();
                }
                if (ImGui.TreeNode("SystemB"))
                {
                    ImGui.TreePop();
                }
            }
        }
        ImGui.End();
    }
}