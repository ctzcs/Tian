

using Content.Test;
using Engine.Core;
using Engine.Systems;
using Engine.Systems.Editor;
using Engine.UI;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Source;

/// <summary>
/// 世界入口的模板
/// </summary>
public class Cultivation : IContent
{
    private readonly App app;
    public Target Target { get; }
    public EntityStore World { get; set; }
    public Vector2Int LogicResolution { get; } = Const._720P;
    public List<SystemGroup>? SystemGroups { get; } = new();
    private readonly Batcher batcher;
    private SystemRoot updateRoot = null!;
    private SystemRoot renderRoot = null!;
    private UIRoot uiRoot = null!;
    private UIDebugOverlay debugOverlay = null!;
    public Cultivation(App app)
    {
        this.app = app;
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }
    public void Start()
    {
        RebuildSystem();
    }

    public void Destroy()
    {
        batcher.Dispose();
        Target.Dispose();
        World = null;
    }

    public void Update()
    {
        updateRoot.Update(new UpdateTick(app.Time.Delta,(float)app.Time.Seconds));
    }

    public void Render()
    {
        Target.Clear(Const.DefaultColor);
        renderRoot.Update(new UpdateTick(app.Time.Delta,(float)app.Time.Seconds));
        batcher.Render(Target);
        batcher.Clear();
    }
    
    void RebuildSystem()
    {
        uiRoot = new UIRoot(app, LogicResolution);
        debugOverlay = new UIDebugOverlay { Enabled = true };
        //系统模块构建
        updateRoot = new SystemRoot(World, "update-root");
        updateRoot.Add(new UiSystem(uiRoot,new UIDebugOverlay()));
        updateRoot.Add(new CameraSystem(World,app,Target));
        updateRoot.Add(new CameraCullingSystem());
        updateRoot.Add(new TransformSystem());
        updateRoot.Add(new AnimationSystem());
        renderRoot = new SystemRoot(World,"render-root");
        
        
        renderRoot.Add(new BeforeRenderWorldSystem(batcher));
        renderRoot.Add(new HierarchyOrderSystem());
        
        renderRoot.Add(new PerformanceSystem());
        renderRoot.Add(new CoordinateSystem(app,batcher));
        renderRoot.Add(new SelectableSystem(app));
        renderRoot.Add(new CameraCullingDebugSystem(batcher));
        
        renderRoot.Add(new RenderSystem(app,batcher,Target));
        renderRoot.Add(new AfterRenderWorldSystem(batcher));
        renderRoot.Add(new UiRenderSystem(batcher));
        SystemGroups?.Clear();
        SystemGroups.Add(updateRoot);
        SystemGroups.Add(renderRoot);
    }
}