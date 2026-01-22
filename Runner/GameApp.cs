using System.Numerics;
using Content.Test_Batcher;
using Content.Test;
using Engine.Components;
using Engine.Core;
using Engine.Physics;
using Engine.Render;
using Foster.Framework;
using Friflo.Engine.ECS;
using Cursor = Engine.Core.Input.Cursor;

namespace Content;

public class GameApp : App
{
    IContent content;
    Batcher batcher;
    public GameApp(in AppConfig config) : base(in config)
    {
        //RegisterEcsComponentsForAot();
        //GraphicsDevice.VSync = true;
        WindowSetting();
        UpdateMode = UpdateMode.FixedStep(30,false);
        //lifetime = new FrogSample(this);
        batcher = new Batcher(GraphicsDevice);
        //content = new RadianceCascades.RadianceCascades(this) ; 
        //content = new TestSample(this);
        content = new TestBatcher(this);
    }

    protected override void Startup()
    {
        content.Start();
        
    }

    protected override void Shutdown()
    {
        
        content.Destroy();

    }

    protected override void Update()
	{
        //Cursor.ViewportPosition = CameraUtils.ScreenToViewport(Input.Mouse.Position, Window);
		var winSize = Window.BoundsInPixels().Size;
		var center = winSize / 2;
		var target = content.Target;
		var scale = Calc.Min(
			winSize.X / (float)target.Width,
			winSize.Y / (float)target.Height);
		var imageOffset = center - target.Bounds.Size / 2 * scale;
		var rate = (Input.Mouse.Position - imageOffset) / (target.Bounds.Size * scale);
		rate.X = Calc.Clamp(rate.X, 0f, 1f);
		rate.Y = Calc.Clamp(rate.Y, 0f, 1f);
		Cursor.ViewportPosition = rate;
		content.Update();
	}


    protected override void Render()
    {
        content.Render();
        //batcher.Render(Window);
        // draw screen to window
        {
            Window.Clear(Color.Black);
            //比如Mac上的size就是实际大小的数倍
            var size = Window.BoundsInPixels().Size;
            var center = size/2;
            var screenTarget = content.Target;
            var scale = Calc.Min(
                size.X / (float)screenTarget.Width,
                size.Y / (float)screenTarget.Height);
            //Log.Info( $"{size}__{scale}__{screenTarget.Bounds}");
            batcher.PushSampler(new(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
            batcher.Image(screenTarget, center, screenTarget.Bounds.Size / 2, Vector2.One * scale, 0, Color.White);
            batcher.PopSampler();
            batcher.Render(Window);
            batcher.Clear();
        }
    }



    void WindowSetting()
    {
        Window.Resizable = true;
        Window.OnResize += OnResize;
        
    }
    
    void OnResize()
    {
        
    }
}
