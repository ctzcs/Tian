using Engine.Components;
using Friflo.Engine.ECS;
using ImGuiNET;

namespace Editor;

public class HierarchyWindow:EditorWindow
{

    protected override void OnAddWindow()
    {
        base.OnAddWindow();
        IsOpen = true;
    }
    public override void Update()
    {
        var world = Data.currentContent?.World;
        //这里应该要一些方便组成Hierarchy的方便函数Utitliy,然后通过一个回调注入进来
        if (ImGui.Begin("Hierarchy"))
        {
            foreach (var entity in world.Entities)
            {
                if (entity.HasComponent<CTransform>())
                {
                    ref var transform = ref entity.GetComponent<CTransform>();
                    if (transform.Parent != default) continue;
                }
                DrawEntityNode(entity);
            }
        }
        ImGui.End();
    }

    private void DrawEntityNode(Entity entity)
    {
        string name = "entity";
        if (entity.HasComponent<UniqueEntity>())
        {
            ref var unique = ref entity.GetComponent<UniqueEntity>();
            name = $"Unique:{unique.uid}";
        }
        
        /*if (entity.HasComponent<Unit>())
        {
            ref var unit = ref entity.GetComponent<Unit>();
            name = unit.ToString();
        }*/
        var isSelected = entity == Data.selectedEntity;
        var flags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow
                                                             | (isSelected ? ImGuiTreeNodeFlags.Selected : 0);
        var open = ImGui.TreeNodeEx($"{name} [{entity.Id}]", flags);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsItemToggledOpen())
            Data.selectedEntity = entity;
        if (open)
        {
            if (entity.HasComponent<CTransform>())
            {
                ref var transform = ref entity.GetComponent<CTransform>();
                var children = transform.Children;
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child.IsNull) continue;
                    ImGui.PushID(child.Id);
                    DrawEntityNode(child);
                    ImGui.PopID();
                }
            }
            ImGui.TreePop();
        }
    }
}