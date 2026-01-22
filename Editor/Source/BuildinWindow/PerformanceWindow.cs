using Engine;
using Engine.Components;
using Engine.Core.Extensions;
using Engine.Systems;

namespace Editor;
using ImGuiNET;

public class PerformanceWindow : EditorWindow
{
    protected override void OnAddWindow()
    {
        base.OnAddWindow();
        IsOpen = true;
    }

    public override void Update()
    {
        if (!ImGui.Begin("Performance"))
        {
            ImGui.End();
            return;
        }

        if (Data.currentContent != null)
        {
            var world = Data.currentContent.World;
            if (world.HasUniqueEntity(BuildInEntityId.Performance))
            {
                var entity = world.GetUniqueEntity(BuildInEntityId.Performance);
                ref var counter = ref entity.GetComponent<FrameCounter>();
                ImGui.Text($"Render FPS: {counter.FPS}");
                ref var stats = ref entity.GetComponent<RenderBatchStats>();
                ImGui.Text($"Batch Count: {stats.BatchCount}");
            }
            
        }
        else
        {
            ImGui.Text("Current content does not expose performance data.");
        }

        ImGui.End();
    }
}