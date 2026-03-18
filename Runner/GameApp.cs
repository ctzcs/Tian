using System.Numerics;
using Engine.Core;
using Foster.Framework;
using Cursor = Engine.Core.Input.Cursor;

namespace Content;

public class GameApp : App
{
    IContent content;
    Batcher batcher;
    ContentManager contentManager;
    public GameApp(in AppConfig config) : base(in config)
    {
        //RegisterEcsComponentsForAot();
        //GraphicsDevice.VSync = true;
        WindowSetting();
        UpdateMode = UpdateMode.FixedStep(30,false);
        //lifetime = new FrogSample(this);
        batcher = new Batcher(GraphicsDevice);
        contentManager = new ContentManager();
        //加载ProjectConfig
        var projectConfigPath = ProjectConfigUtils.ResolveProjectConfigPath();
        Log.Info("Get Project Config Path: " + projectConfigPath);
        if (string.IsNullOrWhiteSpace(projectConfigPath))
        {
            Log.Error("ProjectConfig.json not found");
            return;
        }

        ProjectConfig? projectConfig = ProjectConfigUtils.LoadProjectConfig(projectConfigPath);
        if (projectConfig == null)
        {
            Log.Error("Loading content failed in GameApp");
            return;
        }
        Log.Info(projectConfig.GameAssembly);
        var projectDir = ProjectConfigUtils.GetProjectDirectory(projectConfigPath);
        var contentDll = ProjectConfigUtils.ResolveAssemblyPath(projectDir, projectConfig.GameAssembly, projectConfig.BuildOutputDir);
        string contentDllName = Path.GetFileNameWithoutExtension(contentDll);
        if (!File.Exists(contentDll))
            throw new FileNotFoundException($"Assembly not found: {contentDll}");
        contentManager.LoadContentAssembly(contentDllName, contentDll);
        var types = contentManager.GetAvailableContentTypes(contentDllName).ToArray();
        if (types.Length == 0)
            throw new InvalidOperationException($"No content types found in {contentDll}");
        content = contentManager.Create(contentDllName, projectConfig.GameName, this);
        
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
            batcher.PushSampler(new(TextureFilter.Linear, TextureWrap.Clamp, TextureWrap.Clamp));
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
        content.OnResize(GraphicsDevice,Window.WidthInPixels,Window.HeightInPixels);
    }
}
