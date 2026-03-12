using Engine.Asset.v1;
using Engine.Core;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Audio;

public class ZAudioContent : GameContent
{

    private readonly App app;
    private readonly Batcher batcher;
    public ZAudioContent(App app) : base(app)
    {
        this.app = app;
        int width = 1280;
        int height = 720;
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }
    public override void Start()
    {
        Foster.Audio.Audio.Startup();
        Engine.MiniAudio.Audio.AudioTest();
    }

    public override void Destroy()
    {
        Foster.Audio.Audio.Shutdown();
        AssetsV1.DisposeCache();
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
        batcher.Render(Target);
        batcher.Clear();
    }
    
}