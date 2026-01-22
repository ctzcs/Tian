namespace Content.Source.EcsExample;

using Engine.Core;
using Engine.Core.Structure;
using Foster.Framework;
using Friflo.Engine.ECS;

/// <summary>
/// 世界入口的模板
/// </summary>
public class EcsExample : IContent
{
    private readonly App app;
    public Target Target { get; }
    public EntityStore World { get; set; }
    public Vector2Int LogicResolution { get; } = Const._720P;
    private readonly Batcher batcher;
    public EcsExample(App app)
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
        Target.Clear(Color.White);
        batcher.Render(Target);
        batcher.Clear();
    }
}