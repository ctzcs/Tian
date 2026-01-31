using System.Numerics;
using Engine.Core;
using Engine.MiniAudio;
using Engine.Asset.v1;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.RadianceCascades;

public class RadianceCascades:IContent
{
    private readonly App app;
    public Target Target { get; }
    public EntityStore World { get; set; }
    private readonly Batcher batcher;
    public Vector2Int LogicResolution { get; }
    public List<SystemGroup> SystemGroups { get; }

    public RadianceCascades(App app)
    {
        this.app = app;
        LogicResolution = Const._720P;
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
        Foster.Audio.Audio.Update();
    }

    public void Render()
    {
        Target.Clear(Color.White);
        
        batcher.Rect(new Rect(0, 0, Target.Width/2, Target.Height/2), Color.Red);
        batcher.Circle(new Vector2(100, 100), 100, 100, Color.Blue);
        batcher.Render(Target);
        batcher.Clear();
    }

    
}