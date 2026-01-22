using System.Diagnostics;
using Engine.Components;
using Engine.Core.Extensions;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

public class PerformanceSystem:QuerySystem
{
    private EntityStore world;
    public const string Performance = nameof(Performance);
    private Entity singleton;
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
        singleton = store.CreateEntity(
            new UniqueEntity(Performance),
            new FrameCounter()
            {
                sw = Stopwatch.StartNew()
            },
            new RenderBatchStats());
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        base.OnRemoveStore(store);
        singleton.DeleteEntity();
    }

    protected override void OnUpdate()
    {
        if (world.HasUniqueEntity(Performance))
        {
            var entity = world.GetUniqueEntity(Performance);
            ref var counter =ref entity.GetComponent<FrameCounter>();
            counter.Frames++;
            var elapsed = counter.sw.Elapsed.TotalSeconds;
            if (elapsed > 1)
            {
                counter.sw.Restart();
                counter.FPS = counter.Frames;
                counter.Frames = 0;
            }
        }
    }
}