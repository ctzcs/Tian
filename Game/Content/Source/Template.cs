using Engine.Core;
using Foster.Framework;
using Friflo.Engine.ECS;

namespace Content.Source;

/// <summary>
/// 世界入口的模板
/// </summary>
public class Template : GameContent
{
    private readonly App app;
    private readonly Batcher batcher;
    public Template(App app)  : base(app)
    {
        this.app = app;
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }
    public override void Start()
    {
    }

    public override void Destroy()
    {
        batcher.Dispose();
        Target.Dispose();
        World = null;
    }



    public override void Render()
    {
        Target.Clear(Const.DefaultColor);
        batcher.Render(Target);
        batcher.Clear();
    }
}