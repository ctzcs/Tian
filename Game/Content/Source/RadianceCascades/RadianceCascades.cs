using System.Numerics;
using Engine.Core;
using Engine.MiniAudio;
using Engine.Asset.v1;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.RadianceCascades;

public class RadianceCascades:GameContent
{
    private readonly App app;
    
    private readonly Batcher batcher;

    public RadianceCascades(App app) : base(app)
    {
        this.app = app;
        LogicResolution = Const._720P;
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
        Foster.Audio.Audio.Update();
    }

    public override void Render()
    {
        Target.Clear(Color.White);
        
        batcher.Rect(new Rect(0, 0, Target.Width/2, Target.Height/2), Color.Red);
        batcher.Circle(new Vector2(100, 100), 100, 100, Color.Blue);
        batcher.Render(Target);
        batcher.Clear();
    }

    
}