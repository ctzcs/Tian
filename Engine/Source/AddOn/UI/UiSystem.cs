
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.UI;

public struct UiRoot : IComponent
{
    public bool IsOpen;
    public bool IsDebugEnabled;
    public UIRoot Ui;
    public UIDebugOverlay? DebugOverlay;
}

/// <summary>
/// 放到所有Update系统前
/// </summary>
public class UiSystem : QuerySystem
{
    private readonly UIRoot uiRoot;
    private readonly UIDebugOverlay? debugOverlay;
    private EntityStore world;
    public static string UiRoot => nameof(UiRoot);

    public UiSystem(UIRoot uiRoot,UIDebugOverlay? debugOverlay = null)
    {
        this.uiRoot = uiRoot;
        this.debugOverlay = debugOverlay;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
        var e = store.CreateEntity(
            new UniqueEntity(UiRoot),
            new UiRoot()
            {
                IsOpen = false,
                IsDebugEnabled = false,
                Ui = uiRoot,
                DebugOverlay = debugOverlay
            },
            new MetaGroup()
            {
                GroupName = "Unique",
                SubGroupName = "BuildIn"
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
            debugOverlay?.Enabled = uiRootComponent.IsDebugEnabled;
        }
        uiRoot.Update(Tick.deltaTime);
        
        
    }
}

/// <summary>
/// 放到渲染系统后
/// </summary>
public class UiRenderSystem : QuerySystem
{
    private readonly Batcher batcher;
    private EntityStore world;
    public UiRenderSystem(Batcher batcher)
    {
        this.batcher = batcher;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
    }

    protected override void OnUpdate()
    {
        if (!world.HasUniqueEntity(Engine.Id.UiRoot))
            return;
        var uiRootComponent = world.GetUniqueEntity(Engine.Id.UiRoot).GetComponent<UiRoot>();
        //TODO 修正矩阵->这里已经Pop World Matrix了，所以可能不需要这样了
        var prevMatrix = batcher.Matrix;
        batcher.Matrix = System.Numerics.Matrix3x2.Identity;
        uiRootComponent.Ui.Render(batcher);
        uiRootComponent.DebugOverlay?.Render(batcher,uiRootComponent.Ui);
        batcher.Matrix = prevMatrix;
    }
}
