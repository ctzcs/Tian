using System.Numerics;
using Engine.Asset;
using Engine.Asset.v1;
using Engine.Core;
using Engine.Core.Extensions;
using Engine.UI;
using Engine.Utility;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Rect = Foster.Framework.Rect;

namespace Content.Source.Test_Ui;

public sealed class UiTestScene : IContent
{
    private readonly App app;
    private readonly Batcher batcher;

    private SystemRoot updateRoot = null!;
    private SystemRoot renderRoot = null!;

    private UIRoot uiRoot = null!;
    private UIDebugOverlay debugOverlay = null!;

    private SpriteFont? font;

    private UiTestLeftPanel? leftPanel;
    private UiTestRightPanel? rightPanel;

    public Target Target { get; }
    public EntityStore World { get; set; }
    public Vector2Int LogicResolution { get; } = Const._720P;
    public List<SystemGroup>? SystemGroups { get; } = new();

    public UiTestScene(App app)
    {
        this.app = app;

        Target = new Target(app.GraphicsDevice, LogicResolution.X, LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();

        InitAssets();
        BuildSystems();
        BuildUI();
        SetUiOpen(true);
    }

    public void Start()
    {
    }

    public void Destroy()
    {
        SetUiOpen(false);

        font?.Dispose();
        font = null;

        Assets.DeleteCache();

        batcher.Dispose();
        Target.Dispose();

        World = null!;
        updateRoot = null!;
        renderRoot = null!;
        uiRoot = null!;
        debugOverlay = null!;
        leftPanel = null;
        rightPanel = null;
    }

    public void Update()
    {
        if (app.Input.Keyboard.Pressed(Keys.U))
            SetUiOpen(!GetUiOpen());

        if (app.Input.Keyboard.Pressed(Keys.O) && debugOverlay != null)
            debugOverlay.Enabled = !debugOverlay.Enabled;

        if (app.Input.Keyboard.Pressed(Keys.R))
        {
            BuildUI();
            SetUiOpen(true);
        }

        if (app.Input.Keyboard.Pressed(Keys.P))
            leftPanel?.AddItem();

        if (app.Input.Keyboard.Pressed(Keys.L))
            leftPanel?.RemoveLastItem();

        updateRoot.Update(new UpdateTick(app.Time.Delta, (float)app.Time.Seconds));
    }

    public void Render()
    {
        Target.Clear(Rgb(18, 18, 22));

        renderRoot.Update(new UpdateTick(app.Time.Delta, (float)app.Time.Seconds));

        batcher.Render(Target);
        batcher.Clear();
    }

    private void InitAssets()
    {
        //Assets.Load(app.GraphicsDevice);
        AssetsV1.LazyInitializeCache("pack.zip");
        Assets.LoadSpritesFromGz(app.GraphicsDevice);
        var codepoints = FontUtility.GetCodepoints(3500,FontLanguage.SimplifiedChinese);
        font = new SpriteFont(
            app.GraphicsDevice,
            Path.Join(Assets.ContentAssetsPath, "Fonts", "SmileySans-Oblique.ttf"),
            32,
            codepoints);

        Assets.SetFont(font);
    }

    private void BuildSystems()
    {
        var logicSize = new Vector2(LogicResolution.X, LogicResolution.Y);

        uiRoot = new UIRoot(app.Input, app.Window, logicSize);
        debugOverlay = new UIDebugOverlay { Enabled = true };

        updateRoot = new SystemRoot(World, "ui-update");
        updateRoot.Add(new UiSystem(uiRoot,new UIDebugOverlay()));

        renderRoot = new SystemRoot(World, "ui-render");
        renderRoot.Add(new UiRenderSystem(batcher));

        SystemGroups!.Clear();
        SystemGroups.Add(updateRoot);
        SystemGroups.Add(renderRoot);
    }

    private void BuildUI()
    {
        leftPanel?.CancelDrag();
        leftPanel = null;
        rightPanel = null;

        uiRoot.Root.ClearChildren();

        var dock = new HorizontalGroup()
            .WithViewportRatio(new Rect(0f, 0f, 1f, 1f))
            .WithRect(new Rect(0, 0, LogicResolution.X, LogicResolution.Y))
            .WithPadding(16)
            .WithChildGap(16)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Top);

        var leftPanelRoot = BuildLeftPanel();

        var rightPanelRoot = BuildRightPanel();

        dock.WithChildren(leftPanelRoot, rightPanelRoot);
        uiRoot.Root.WithChild(dock);

        leftPanel?.SeedItems(5);
    }

    private VerticalGroup BuildLeftPanel()
    {
        leftPanel = new UiTestLeftPanel(uiRoot, () =>
        {
            BuildUI();
            SetUiOpen(true);
        });
        return leftPanel.Root;
    }

    private VerticalGroup BuildRightPanel()
    {
        rightPanel = new UiTestRightPanel(uiRoot);
        return rightPanel.Root;
    }
    
    private bool GetUiOpen()
    {
        var entities = World.ComponentIndex<UniqueEntity, string>()[UiSystem.UiRoot];
        switch (entities.Count)
        {
            case 0:
                return uiRoot.IsOpen;
            case 1:
                return entities[0].GetComponent<UiRoot>().IsOpen;
            default:
                throw new InvalidOperationException($"Multiple entities with name {UiSystem.UiRoot} {entities.Count}");
        }
    }

    private void SetUiOpen(bool open)
    {
        var entities = World.ComponentIndex<UniqueEntity, string>()[UiSystem.UiRoot];
        switch (entities.Count)
        {
            case 0:
                break;
            case 1:
            {
                var e = entities[0];
                ref var c = ref e.GetComponent<UiRoot>();
                c.IsOpen = open;
                break;
            }
            default:
                throw new InvalidOperationException($"Multiple entities with name {UiSystem.UiRoot} {entities.Count}");
        }

        uiRoot.IsOpen = open;
    }

    private static Color Rgb(byte r, byte g, byte b)
        => new Color(r, g, b, 255);
}