using Engine.Asset.v1;
using Engine.Core;
using Engine.Core.Structure;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Audio;

public class ZAudioContent : IContent
{

    private readonly App app;
    public Target Target { get; }
    public EntityStore World { get; set; }
    public Vector2Int LogicResolution { get; } = Const._720P;
    public List<SystemGroup> SystemGroups { get; }
    private readonly Batcher batcher;
    public ZAudioContent(App app)
    {
        this.app = app;
        int width = 1280;
        int height = 720;
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }
    public void Start()
    {
        Foster.Audio.Audio.Startup();
        Engine.MiniAudio.Audio.AudioTest();
    }

    public void Destroy()
    {
        Foster.Audio.Audio.Shutdown();
        AssetsV1.DisposeCache();
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
        batcher.Render(Target);
        batcher.Clear();
    }
    
}