using Friflo.Engine.ECS.Systems;

namespace Content.Source.EcsExample;

using Engine.Core;
using Foster.Framework;
using Friflo.Engine.ECS;

/// <summary>
/// 世界入口的模板
/// </summary>
public class EcsExample : GameContent
{
    private readonly App app;
    private readonly Batcher batcher;
    public EcsExample(App app)  : base(app)
    {
        this.app = app;
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }

    public override void Destroy()
    {
        batcher.Dispose();
        Target.Dispose();
        World = null;
    }

    public override void Update()
    {
    }

    public override void Render()
    {
        Target.Clear(Color.White);
        batcher.Render(Target);
        batcher.Clear();
    }
}