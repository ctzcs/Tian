using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Friflo.Engine.ECS;
using ImGuiNET;

namespace Engine.Editor.Drawer;

public sealed class BuiltinCTransformDrawer : IInspectorDrawer
{
    public int Order => 20;
    public bool Supports(Type type) => type == typeof(Engine.Components.CTransform);

    public bool Draw(string label, Type type, ref object? val)
    {
        var fPos = type.GetField("localPosition");
        var fRad = type.GetField("localRad");
        var fScl = type.GetField("localScale");

        var pos = fPos != null ? (Vector2)fPos.GetValue(val)! : default;
        var rad = fRad != null ? (float)fRad.GetValue(val)! : 0f;
        var scl = fScl != null ? (Vector2)fScl.GetValue(val)! : Vector2.One;

        ImGui.PushID(label);
        bool changed = false;

        if (ImGui.DragFloat2("Local Position", ref pos, 0.1f, 0f, 0f, "%.3f"))
        {
            fPos!.SetValue(val, pos);
            var enumType = type.GetNestedType("EDirtyType")!;
            var dirty = Enum.Parse(enumType, "PositionDirty");
            type.GetMethod("SetDirty")!.Invoke(val, new object[] { dirty });
            changed = true;
        }

        if (ImGui.DragFloat("Local Rotation", ref rad, 0.1f, 0f, 0f, "%.3f"))
        {
            fRad!.SetValue(val, rad);
            var enumType = type.GetNestedType("EDirtyType")!;
            var dirty = Enum.Parse(enumType, "RotationDirty");
            type.GetMethod("SetDirty")!.Invoke(val, new object[] { dirty });
            changed = true;
        }

        if (ImGui.DragFloat2("Local Scale", ref scl, 0.1f, 0f, 0f, "%.3f"))
        {
            fScl!.SetValue(val, scl);
            var enumType = type.GetNestedType("EDirtyType")!;
            var dirty = Enum.Parse(enumType, "ScaleDirty");
            type.GetMethod("SetDirty")!.Invoke(val, new object[] { dirty });
            changed = true;
        }

        var fParent = type.GetField("parent");
        var parent = fParent != null ? (Entity)fParent.GetValue(val)! : default;

        var fChildren = type.GetField("children");
        var children = fChildren != null ? (List<Entity>?)fChildren.GetValue(val) : null;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.SetWindowFontScale(0.85f);

        ImGui.TextDisabled("Hierarchy");

        ImGui.TextDisabled("Parent");
        ImGui.SameLine();
        ImGui.Text(parent.IsNull ? "(None)" : $"id: {parent.Id}");

        var count = children?.Count ?? 0;
        ImGui.TextDisabled($"Children ({count})");
        if (count == 0)
        {
            ImGui.SameLine();
            ImGui.Text("(None)");
        }
        else
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var child = children![i];
                if (child.IsNull)
                    continue;

                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(child.Id);
            }

            if (sb.Length > 0)
            {
                ImGui.Indent(10f);
                ImGui.TextWrapped(sb.ToString());
                ImGui.Unindent(10f);
            }
        }

        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopID();
        return changed;
    }
}
