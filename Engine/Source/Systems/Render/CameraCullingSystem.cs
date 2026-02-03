using System.Numerics;
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Transform = Foster.Framework.Transform;

namespace Engine.Systems;

public struct InsiderView:ITag{}

//After Camera
public class CameraCullingSystem:QuerySystem
{
    private EntityStore World;
    private ArchetypeQuery<CTransform,SpriteRenderer> query;
    private QueryJob queryJob;
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        World = store;
        query = store.Query<CTransform,SpriteRenderer>();
        
    }

    protected override void OnUpdate()
    {
        if (!World.HasUniqueEntity(Engine.Id.MainCamera)) return;
        var cameraEntity = World.GetUniqueEntity(Engine.Id.MainCamera);
        var camTransform = cameraEntity.GetComponent<CTransform>();
        var camera = cameraEntity.GetComponent<Camera2D>();

        var viewMinMax = CameraUtils.GetViewMinAndMaxInWorld(camTransform, camera);
        
        /*queryJob = query.ForEach((transform, sr, entities) =>
        {
            for (int i = 0; i < entities.Length; i++)
            {
                if (CameraUtils.IsVisible(transform[i], sr[i], viewMinMax.Item1, viewMinMax.Item2,camera))
                {
                    CommandBuffer.Synced.AddTag<InsiderView>(entities[i]);
                }
                else
                {
                    CommandBuffer.Synced.RemoveTag<InsiderView>(entities[i]);
                }
            }
        });
        queryJob.RunParallel();*/
        
        query.EachEntity(new CullingSpriteRendererEach()
        {
            CommandBuffer = CommandBuffer,
            viewMinMax = viewMinMax,
            camera2D = camera,
        });
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
        if (!World.HasUniqueEntity(Engine.Id.MainCamera)) return;
        var cameraEntity = World.GetUniqueEntity(Engine.Id.MainCamera);
        var camera = cameraEntity.GetComponent<Camera2D>();
        ref var checkBox = ref cameraEntity.GetComponent<CheckBox>();
        var (width,height) = CameraUtils.GetViewWidthHeightInWorld(camera);
        checkBox.Size.X = (width) - 1;
        checkBox.Size.Y = (height) - 1;
    }
}