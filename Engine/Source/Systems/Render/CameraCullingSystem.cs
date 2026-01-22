using System.Numerics;
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

public struct InsiderView:ITag{}

//After Camera
public class CameraCullingSystem:QuerySystem<CTransform,SpriteRenderer>
{
    private EntityStore World;
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        World = store;
    }

    protected override void OnUpdate()
    {
        if (!World.HasUniqueEntity("MainCamera")) return;
        var cameraEntity = World.GetUniqueEntity("MainCamera");
        var camTransform = cameraEntity.GetComponent<CTransform>();
        var camera = cameraEntity.GetComponent<Camera2D>();

        var viewMinMax = CameraUtils.GetViewMinAndMaxInWorld(camTransform, camera);
        
        Query.EachEntity(new CullingSpriteRendererEach()
        {
            CommandBuffer = CommandBuffer,
            viewMinMax = viewMinMax,
            camera2D = camera,
        });
        CommandBuffer.Playback();
    }
    
    
    struct CullingSpriteRendererEach:IEachEntity<CTransform,SpriteRenderer>
    {
        public CommandBuffer CommandBuffer;
        public (Vector2, Vector2) viewMinMax;
        public Camera2D camera2D;
        public void Execute(ref CTransform transform, ref SpriteRenderer sr, int entity)
        {
            if (CameraUtils.IsVisible(transform, sr, viewMinMax.Item1, viewMinMax.Item2,camera2D))
            {
                CommandBuffer.AddTag<InsiderView>(entity);
            }
            else
            {
                CommandBuffer.RemoveTag<InsiderView>(entity);
            }
        }
    }
}


public class CameraCullingDebugSystem : QuerySystem
{
    private EntityStore World;
    private Batcher _batcher;
    public CameraCullingDebugSystem(Batcher batcher)
    {
        this._batcher = batcher;
    }
    
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        World = store;
    }
    protected override void OnUpdate()
    {
        if (!World.HasUniqueEntity("MainCamera")) return;
        var cameraEntity = World.GetUniqueEntity("MainCamera");
        var camTransform = cameraEntity.GetComponent<CTransform>();
        var camera = cameraEntity.GetComponent<Camera2D>();
        ref var checkBox = ref cameraEntity.GetComponent<CheckBox>();
        var (width,height) = CameraUtils.GetViewWidthHeightInWorld(camTransform, camera);
        checkBox.rect.Width = (width) - 1;
        checkBox.rect.Height = (height) - 1;
    }
}