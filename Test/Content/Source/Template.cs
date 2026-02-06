using Engine.Core;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Source;

/// <summary>
/// 世界入口的模板
/// </summary>
public class Template : IContent
{
    private readonly App app;
    public Target Target { get; }
    public EntityStore World { get; set; }
    public Vector2Int LogicResolution { get; } = Const._720P;
    public List<SystemGroup> SystemGroups { get; }
    private readonly Batcher batcher;
    public Template(App app)
    {
        this.app = app;
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }
    public void Start()
    {
    }

    public void Destroy()
    {
        batcher.Dispose();
        Target.Dispose();
        World = null;
    }

    public void Update()
    {
    }

    public void Render()
    {
        Target.Clear(Const.DefaultColor);
        batcher.Render(Target);
        batcher.Clear();
    }
}