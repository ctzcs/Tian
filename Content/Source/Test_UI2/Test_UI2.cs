using Engine.Asset;
using Engine.Asset.v1;
using Engine.Core;
using Engine.UI_2;
using Engine.Utility;
using Foster.Framework;
using ImGuiNET;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Source.Test_UI2;

public class Test_UI2 : GameContent
{
    private readonly Batcher batcher;
    private readonly UIRoot uiRoot;
    private readonly MainCanvasDemo mainCanvas;
    private readonly DragDirectoryDemo dragCanvas;
    private readonly UIDebugger uiDebugger;

    public Test_UI2(App app) : base(app)
    {
        Target = new Target(app.GraphicsDevice, LogicResolution.X, LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
        SystemGroups = new List<SystemGroup>();

        uiRoot = new UIRoot(app, LogicResolution);
        mainCanvas = new MainCanvasDemo(uiRoot, LogicResolution, app);
        dragCanvas = new DragDirectoryDemo(uiRoot);
        uiDebugger = new UIDebugger();
        
        AssetsV1.LazyInitializeCache("pack.zip");
        Assets.LoadSpritesFromGz(app.GraphicsDevice);

        var codepoints = FontUtility.GetCodepoints(7500, FontLanguage.SimplifiedChinese);
        var font = new SpriteFont(
            app.GraphicsDevice,
            Path.Join(Assets.ContentAssetsPath, "Fonts", "Monaco.ttf"),
            32,
            codepoints);
        Assets.SetFont(font);
    }
    

    public override void Destroy()
    {
        batcher.Dispose();
        Target.Dispose();
        World = null;
    }

    public override void Update()
    {
        uiRoot.Update();

        dragCanvas.Update((float)Ctx.Time.Delta);

        var keyboard = Ctx.Input.Keyboard;
        if (keyboard.Pressed(Keys.O))
            uiDebugger.Enabled = !uiDebugger.Enabled;

        if (keyboard.Pressed(Keys.T))
            mainCanvas.ToggleLayout();
    }

    public override void Render()
    {
        Target.Clear(Const.DefaultColor);
        uiRoot.Render(batcher);
        uiDebugger.Render(batcher, mainCanvas.Canvas);
        batcher.Render(Target);
        batcher.Clear();
    }

    public override void OnResize(GraphicsDevice graphicsDevice, int width, int height)
    {
        base.OnResize(graphicsDevice, width, height);
        uiRoot.OnResize(width, height);
    }
}

public sealed class TestUI2Editor : GameEditor
{
    private int clickCount;
    private bool showStats = true;

    public override string Name => "Test_UI2";

    protected override void Register()
    {
        RegisterWindow("Test_UI2 Tools", DrawWindow);
    }

    private void DrawWindow()
    {
        ImGui.Text($"Editor: {Name}");
        ImGui.Checkbox("Show Stats", ref showStats);

        if (ImGui.Button("Ping"))
            clickCount++;

        ImGui.SameLine();
        ImGui.Text($"Clicks: {clickCount}");

        var content = CurrentContent as Test_UI2;
        if (content == null)
        {
            ImGui.Text("Current Content is not Test_UI2");
            return;
        }

        if (showStats)
        {
            ImGui.Text($"LogicResolution: {content.LogicResolution.X} x {content.LogicResolution.Y}");
        }
    }
}
