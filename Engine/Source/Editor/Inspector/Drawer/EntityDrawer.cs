using ImGuiNET;

namespace Engine.Editor.Drawer;

public sealed class BuiltinEntityDrawer : IInspectorDrawer
{
    public int Order => 10;
    public bool Supports(Type type) => type == typeof(Friflo.Engine.ECS.Entity);
    public bool Draw(string label, Type type, ref object? val)
    {
        var entity = val is Friflo.Engine.ECS.Entity e ? e : default;
        var status = entity.IsNull ? "null" : $"id: {entity.Id}";
        ImGui.Text($"{label}: {status}");
        ImGui.SameLine();
        if (ImGui.Button($"Clear##{label}"))
        {
            val = default(Friflo.Engine.ECS.Entity);
            return true;
        }
        return false;
    }
}