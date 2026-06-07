using Engine;
using Engine.Components;
using Engine.Core.Extensions;
using Engine.ECS;
using Foster.Framework;
using Friflo.Engine.ECS.Systems;

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

        if (Data?.currentContent is not IEcsContent ecsContent)
        {
            ImGui.Text("Current content does not expose performance data.");
            ImGui.End();
            return;
        }
        
        var world = ecsContent.World;
        var systemGroups = ecsContent.SystemGroups;
        if (world != null && world.HasUniqueEntity(Id.Performance))
        {
            var entity = world.GetUniqueEntity(Id.Performance);
            ref var counter = ref entity.GetComponent<FrameCounter>();
            ImGui.Text($"Render FPS: {counter.FPS}");
            ref var stats = ref entity.GetComponent<RenderBatchStats>();
            ImGui.Text($"Batch Count: {stats.BatchCount}");
        }

        if (systemGroups != null)
        {
            if (ImGui.CollapsingHeader("System Groups"))
            {
                foreach (var systemGroup in systemGroups)
                {
                    ImGui.PushID(systemGroup.Name);
                    var enabled = systemGroup.MonitorPerf;
                    if (ImGui.Checkbox(systemGroup.Name, ref enabled))
                    {
                        systemGroup.SetMonitorPerf(enabled);
                    }
                    ImGui.PopID();
                }
            }

            foreach (var systemGroup in systemGroups)
            {
                if (systemGroup.MonitorPerf)
                {
                    Log.Info(systemGroup.GetPerfLog());
                }
            }
        }

        ImGui.End();
    }
}