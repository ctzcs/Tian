using System.Numerics;
using Engine.Components;
using Engine.Core.Graphics;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

public class AfterRenderWorldSystem:QuerySystem
{
    private Batcher batcher;
    private Matrix3x2 transformMatrix;
    private ArchetypeQuery<Camera2D, CTransform> cameraQuery;

    public AfterRenderWorldSystem(Batcher batcher)
    {
        this.batcher = batcher;
    }

    public AfterRenderWorldSystem(RenderContext renderContext)
        : this(renderContext.Batcher)
    {
    }
    
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        cameraQuery = store.Query<Camera2D, CTransform>();
    }

    protected override void OnUpdate()
    {
        //World Matrix
        batcher.PopMatrix();
    }
}