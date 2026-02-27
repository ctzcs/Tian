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

public enum UiTestSection
{
    Rotation,
    TextOverflow,
    Grid,
    ScrollView,
    Slider
}

public sealed class UiTestScene : GameContent
{
    private readonly App app;
    private readonly Batcher batcher;

    private SystemRoot updateRoot = null!;
    private SystemRoot renderRoot = null!;

    private UIRoot uiRoot = null!;
    private UIDebugOverlay debugOverlay = null!;

    private SpriteFont? font;

    private UiTestLeftPanel? leftPanel;
    private UiTestRotationPanel? rotationPanel;
    private UiTestRightPanel? rightPanel;

    private bool showRotation = true;
    private bool showTextOverflow = true;
    private bool showGrid = true;
    private bool showScroll = true;
    private bool showSlider = true;
    

    public UiTestScene(App app)  : base(app)
    {
        this.app = app;

        Target = new Target(app.GraphicsDevice, LogicResolution.X, LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
        SystemGroups = new();
        InitAssets();
        BuildSystems();
        BuildUI();
        SetUiOpen(true);
    }
    

    public override void Destroy()
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
        rotationPanel = null;
        rightPanel = null;
    }

    public override void Update()
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
        rotationPanel?.Update((float)app.Time.Seconds);
    }

    public override void Render()
    {
        Target.Clear(Rgb(18, 18, 22));

        renderRoot.Update(new UpdateTick(app.Time.Delta, (float)app.Time.Seconds));

        batcher.Render(Target);
        batcher.Clear();
    }

    private void InitAssets()
    {
        //Assets.Load(app.GraphicsDevice);
        AssetsV1.Pack(Assets.ContentAssetsPath,"pack.zip");
        AssetsV1.LazyInitializeCache("pack.zip");
        Assets.LoadSpritesFromGz(app.GraphicsDevice);
        var codepoints = FontUtility.GetCodepoints(7500,FontLanguage.SimplifiedChinese);
        
        //font = app.GraphicsDevice.Defaults.SpriteFont;
        font = new SpriteFont(
            app.GraphicsDevice,
            Path.Join(Assets.ContentAssetsPath, "Fonts", "SmileySans-Oblique.ttf"),
            32,
            codepoints);
        //font.Sampler = new TextureSampler(TextureFilter.Linear, TextureWrap.Clamp);
        
        Assets.SetFont(font);
    }

    private void BuildSystems()
    {
        uiRoot = new UIRoot(app, LogicResolution);
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
        rotationPanel = null;
        rightPanel = null;

        uiRoot.Root.ClearChildren();

        var dock = new HorizontalGroup()
            .WithViewportRatio(new Rect(0f, 0f, 1f, 1f))
            .WithRect(new Rect(0, 0, LogicResolution.X, LogicResolution.Y))
            .WithPadding(16)
            .WithChildGap(16)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Top);
        dock.AnimateLayout = false;

        var leftPanelRoot = BuildLeftPanel();
        leftPanelRoot.AnimateLayout = false;

        var rotationPanelRoot = BuildRotationPanel();
        rotationPanelRoot.AnimateLayout = false;

        var rightPanelRoot = BuildRightPanel();
        rightPanelRoot.AnimateLayout = false;

        dock.WithChildren(leftPanelRoot, rotationPanelRoot, rightPanelRoot);
        uiRoot.Root.WithChild(dock);

        leftPanel?.SeedItems(5);
        //ApplyTestVisibility();
    }

    private VerticalGroup BuildLeftPanel()
    {
        leftPanel = new UiTestLeftPanel(uiRoot, () =>
        {
            BuildUI();
            SetUiOpen(true);
        }, GetSectionVisible, SetSectionVisible);
        return leftPanel.Root;
    }

    private VerticalGroup BuildRotationPanel()
    {
        rotationPanel = new UiTestRotationPanel(uiRoot);
        return rotationPanel.Root;
    }

    private VerticalGroup BuildRightPanel()
    {
        rightPanel = new UiTestRightPanel(uiRoot);
        return rightPanel.Root;
    }

    private bool GetSectionVisible(UiTestSection section)
    {
        return section switch
        {
            UiTestSection.Rotation => showRotation,
            UiTestSection.TextOverflow => showTextOverflow,
            UiTestSection.Grid => showGrid,
            UiTestSection.ScrollView => showScroll,
            UiTestSection.Slider => showSlider,
            _ => true
        };
    }

    private void SetSectionVisible(UiTestSection section, bool visible)
    {
        switch (section)
        {
            case UiTestSection.Rotation:
                showRotation = visible;
                break;
            case UiTestSection.TextOverflow:
                showTextOverflow = visible;
                break;
            case UiTestSection.Grid:
                showGrid = visible;
                break;
            case UiTestSection.ScrollView:
                showScroll = visible;
                break;
            case UiTestSection.Slider:
                showSlider = visible;
                break;
        }

        ApplyTestVisibility();
    }

    private void ApplyTestVisibility()
    {
        if (rotationPanel != null)
            rotationPanel.Root.Visible = showRotation;

        if (rightPanel == null)
            return;

        var root = rightPanel.Root;
        root.ClearChildren();

        if (showTextOverflow)
            root.WithChild(rightPanel.TextOverflowSection);
        if (showGrid)
            root.WithChild(rightPanel.GridSection);
        if (showSlider)
            root.WithChild(rightPanel.SliderSection);
        if (showScroll)
            root.WithChild(rightPanel.ScrollSection);

        uiRoot.Root.Apply();
        uiRoot.Root.UpdateLayoutNow(true);
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