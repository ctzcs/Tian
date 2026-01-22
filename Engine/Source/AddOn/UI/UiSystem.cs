
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.UI;

public struct UiRoot : IComponent
{
    public bool IsOpen;
}

/// <summary>
/// 放到所有Update系统前
/// </summary>
public class UiSystem : QuerySystem
{
    private readonly UIRoot uiRoot;
    private EntityStore world;
    public UIRoot Root => uiRoot;
    public static string UiRoot => nameof(UiRoot);

    public UiSystem(UIRoot uiRoot)
    {
        this.uiRoot = uiRoot;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
        var e = store.CreateEntity(
            new UniqueEntity(UiRoot),new UiRoot()
            {
                IsOpen = false
            });
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        base.OnRemoveStore(store);
        var e = store.GetUniqueEntity(UiRoot);
        e.DeleteEntity();
    }

    protected override void OnUpdate()
    {
        if (world.HasUniqueEntity(UiRoot))
        {
            var uiRootComponent = world.GetUniqueEntity(UiRoot).GetComponent<UiRoot>();
            uiRoot.IsOpen = uiRootComponent.IsOpen;
        }
        uiRoot.Update(Tick.deltaTime);
    }
}

/// <summary>
/// 放到渲染系统后
/// </summary>
public class UiRenderSystem : QuerySystem
{
    private readonly UIRoot uiRoot;
    private readonly UIDebugOverlay? debugOverlay;
    private readonly Batcher batcher;
    
    public UiRenderSystem(Batcher batcher,UIRoot uiRoot, UIDebugOverlay? debugOverlay=null)
    {
        this.uiRoot = uiRoot;
        this.debugOverlay = debugOverlay;
        this.batcher = batcher;
    }

    protected override void OnUpdate()
    {
        if (!uiRoot.IsOpen)
            return;
        //修正矩阵
        var prevMatrix = batcher.Matrix;
        batcher.Matrix = System.Numerics.Matrix3x2.Identity;

        uiRoot.Render(batcher);
        if (debugOverlay is { Enabled: true })
            debugOverlay.Render(batcher, uiRoot);

        batcher.Matrix = prevMatrix;
    }
}