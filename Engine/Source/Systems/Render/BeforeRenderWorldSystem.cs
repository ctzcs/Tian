using System.Numerics;
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

/// <summary>
/// 开始渲染世界
/// </summary>
public class BeforeRenderWorldSystem:QuerySystem
{
    private readonly Batcher batcher;
    private EntityStore World;

    public BeforeRenderWorldSystem(Batcher batcher)
    {
        this.batcher = batcher;
    }
    
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        World = store;
    }

    protected override void OnUpdate()
    {
        if (!World.HasUniqueEntity(Engine.Id.MainCamera))
            return;

        var cameraEntity = World.GetUniqueEntity(Engine.Id.MainCamera);
        ref var camera = ref cameraEntity.GetComponent<Camera2D>();
        ref var transform = ref cameraEntity.GetComponent<CTransform>();

        var transformMatrix = CameraUtils.GetCameraMatrix(transform, camera);
        batcher.PushMatrix(transformMatrix);
    }
}