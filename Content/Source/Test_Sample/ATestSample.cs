using System.Numerics;
using System.Reflection;
using Engine.Asset;
using Engine.Core;
using Engine.Core.Graphics;
using Engine.Performance;
using Engine.Systems;
using Engine.Systems.Editor;
using Engine.UI;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Test;

public class ATestSample:IContent
{
    private readonly App ctx;
    private readonly Batcher batcher;
    private SystemRoot updateRoot;
    private SystemRoot renderGroup;
    private Rng rng = new(1337);
    private Resources res;
    private float deltaTime = 0;
    private Target target;
    private int width = Const._720P.X;
    private int height = Const._720P.Y;
    public Target Target => target;
    
    public EntityStore World
    {
        get;
        set;
    }

    private Material customMaterial;
    private Target tempTarget;
    

    public ATestSample(App ctx)
    {
        this.ctx = ctx;
        World = new EntityStore();
        Assets.Load(ctx.GraphicsDevice);
        var font = new SpriteFont(ctx.GraphicsDevice, 
            Path.Join(Assets.AssetsPath, "Fonts", "SmileySans-Oblique.ttf"), 
            32);
        Assets.SetFont(font);
        var asm = Assembly.GetExecutingAssembly();
        
        customMaterial = 
            //GraphicsUtils.CreateMaterial(ctx.GraphicsDevice, asm, "Custom", 0, 1, 1, 1);
        //GraphicsUtils.CreateMaterial(ctx.GraphicsDevice, asm,"Dissolve",0,1,1,1);
        GraphicsUtils.CreateMaterial(ctx.GraphicsDevice, asm,"Slime",0,1,1,1);
        batcher = new Batcher(ctx.GraphicsDevice);
        target = new Target(ctx.GraphicsDevice,width,height);
        res = new Resources(
            target,
            font,
            Assets.Atlas,
            batcher,
            new Vector2(width,height),
            customMaterial);
        
        
        tempTarget = new Target(ctx.GraphicsDevice,width,height);
        RebuildSystem();
    }

    void RebuildSystem()
    {
        var uiRoot = BuildUI();
        //系统模块构建
        updateRoot = new SystemRoot(World, "TestGroup");
        updateRoot.Add(new UiSystem(uiRoot));
        updateRoot.Add(new StateSystem(World,ctx,res));
        updateRoot.Add(new BuildingCatchSystem(World,res));
        updateRoot.Add(new FindLineSystem(World,rng));
        updateRoot.Add(new CameraSystem(World,ctx,target));
        updateRoot.Add(new CameraCullingSystem());
        updateRoot.Add(new TransformSystem());
        
        renderGroup = new SystemRoot(World,"render");
        renderGroup.Add(new PerformanceSystem());
        renderGroup.Add(new InfoSystem(res));
        renderGroup.Add(new AnimationSystem());
        renderGroup.Add(new BeforeRenderWorldSystem(batcher));
        renderGroup.Add(new HierarchyOrderSystem());
        
        
#if DEBUG
        renderGroup.Add(new CoordinateSystem(ctx,batcher));
        renderGroup.Add(new SelectableSystem(ctx));
        renderGroup.Add(new CameraCullingDebugSystem(batcher));
#endif
        renderGroup.Add(new RenderSystem(ctx,res.batcher,target));
        renderGroup.Add(new UiRenderSystem(batcher,uiRoot,new UIDebugOverlay()));
#if DEBUG
        updateRoot.SetMonitorPerf(true);
        renderGroup.SetMonitorPerf(true);
#endif

    }
    
    UIRoot BuildUI()
    {
        var uiRoot = new UIRoot(ctx.Input,ctx.Window,res.logicSize);
        uiRoot.IsOpen = true;
        var btn = new Button(true, true, true, new Rect(0, 0, 360, 50))
            {
                Text = "Supreme \n Supreme",
            }
            .WithBackgroundImage(Assets.GetSubtexture("test_ui_rect/0"), ElementImageFillMode.Stretch);

        var panel = new VerticalGroup()
            {
                Rect = new Rect(200, 100, 500, 300),
                Visible = true,
                Selectable = true,
                BackgroundColor = Color.Gray,
                SizeMode = UISizeMode.Pixel,
                NormalizedRect = new  Rect(0.5f, 0.2f, 0.3f, 0.5f),
            }
            .WithLayout((cfg) =>
            {
                cfg.PaddingLeft = 100;
                cfg.PaddingRight = 100;
                cfg.PaddingTop = 10;
                cfg.PaddingBottom = 10;
                cfg.ChildGap = 4;
                cfg.AlignX = HorizontalAlignment.Center;
                cfg.AlignY = VerticalAlignment.Middle;
                cfg.AutoHeight = true;
                return cfg;
            })
            .WithChild(btn);
        
        uiRoot.Root.WithChild(panel);
        
        
        var panel1 = new UIElement(true,true,true,new Rect(400, 150, 500, 200));
        panel1.BackgroundColor = Color.Yellow;
        var btn1 = new Button(true, true, true, new Rect(0, 0, 360, 50));
        btn1.BackgroundColor = Color.Blue;
        btn1.Text = "MEMEMEM";
        panel1.AddChild(btn1);
        
        uiRoot.Root.AddChild(panel1);
        return uiRoot;
    }
    
    public void Start()
    {
        Profiler.AppInfo("Hello AppInfo!");
    }

    public void Destroy()
    {
        if (customMaterial != null)
        {
            if (customMaterial.Vertex.Shader is Foster.Framework.IGraphicResource vShader && !vShader.IsDisposed)
                vShader.Dispose();
            if (customMaterial.Fragment.Shader is Foster.Framework.IGraphicResource fShader && !fShader.IsDisposed)
                fShader.Dispose();
        }

        if (res.font != null)
            res.font.Dispose();

        batcher.Dispose();
        target.Dispose();
        tempTarget.Dispose();

        Engine.Asset.Assets.DeleteCache();

        World = null;
        updateRoot = null;
        renderGroup = null;
        res = null;
        customMaterial = null;
    }

    public void Update()
    {
        deltaTime = ctx.Time.Delta;

        var time = (float)ctx.Time.Seconds;
        var lifeTime = Math.Clamp(time * 2, 0, Single.Pi / 2);
        var strength = 0.5f + 0.5f * MathF.Cos(lifeTime);
        customMaterial.Fragment.SetUniformBuffer(new Vector4(time, strength, 0, 0), slot: 0);

        updateRoot.Update(new UpdateTick(ctx.Time.Delta,ctx.Time.Elapsed.Seconds));
        Log.Info(updateRoot.GetPerfLog());
        Log.Info(renderGroup.GetPerfLog());
    }
    
    public void Render()
    {
        //GameRender
        tempTarget.Clear(new Color(0x3A3A3A));
        target.Clear(Color.Transparent);
        var zone = Profiler.BeginZone("renderGroup");
        renderGroup.Update(new UpdateTick(ctx.Time.Delta,ctx.Time.Elapsed.Milliseconds));
        zone.Dispose();
        batcher.Render(tempTarget);
        batcher.Clear();
        PostProcess(tempTarget,target,customMaterial);//TODO 这个也应该放到系统中
        Profiler.EmitFrameMark();
    }
    
    /// <summary>
    /// 后处理
    /// </summary>
    /// <param name="renderTarget"></param>
    /// <param name="output"></param>
    /// <param name="material"></param>
    private void PostProcess(Target? renderTarget, Target output, Material material)
    {
        if (renderTarget != null)
        {
            output.Clear(Color.Transparent);
            
            //batcher.PushMaterial(material);
            
            // 如果需要，先将 renderTarget 的内容复制或转换为可用的纹理
            
            var sourceTexture = renderTarget.Attachments[0];
            
            // 绘制全屏后处理
            var outputSize = new Vector2(output.Width, output.Height);
            batcher.Image(sourceTexture, outputSize / 2, outputSize / 2, Vector2.One, 0, Color.White);
            
            //batcher.PopMaterial();
            batcher.Render(output);
            batcher.Clear();
        }
    }


    /*void UITest()
    {
        if (ctx.Input.Keyboard.Pressed(Keys.U))
        {
            uiRoot.IsOpen = !uiRoot.IsOpen;
        }
        //TODO ui动画测试
        
        var btn = new Button(true, true, true, new Rect(0, 0, 200, 40))
            {
                Text = "Supreme",

            }
            .WithBackgroundImage(Assets.GetSubtexture("test_ui_rect/1"),
                ElementImageFillMode.NineSlice,
                new  Vector4(7, 7, 7, 7))
            .WithClick((btn, data) => { Log.Info("Click"); })
            .WithHover((btn, data) => { Log.Info("Hover"); });
        var line = new UIElement(true, true, true, new Rect(0, 0, 200, 4))
            .WithBackgroundColor(Color.Blue);
        if (ctx.Input.Keyboard.Pressed(Keys.P))
        {
            panel.WithLayoutAnimation(0.15f, Transition.EaseInOut);

            var btnClone = btn.Clone();
            var btn2Clone = btn.Clone()
                .WithRect(new  Rect(0, 0, 50, 40))
                .WithTextOverflow(ElementTextOverflowMode.ShrinkToFit);
            var lineClone = line.Clone();
            
            
            btn2Clone.AddChild(new UIElement(true, true, true, new Rect(0, 0, 20, 20))
            {
                BackgroundColor = Color.Green,
            });

            var item2 = new HorizontalGroup()
                .WithLayout(cfg =>
                {
                    cfg.AlignX = HorizontalAlignment.Center;
                    cfg.AlignY = VerticalAlignment.Middle;
                    cfg.ChildGap = 2;
                    //cfg.AutoWidth = true;
                    cfg.AutoHeight = true;
                    return cfg;
                })
                .WithChild(btnClone)
                .WithChild(btn2Clone);
            
            var item1 = new VerticalGroup()
                .WithLayout(cfg =>
                {
                    cfg.AlignX = HorizontalAlignment.Center;
                    cfg.AlignY = VerticalAlignment.Middle;
                    cfg.ChildGap = 5;
                    cfg.AutoHeight = true;
                    return cfg;
                })
                .WithChild(item2)
                .WithChild(line);
            panel
                //.WithChild(item)
                .WithChild(item1);
            panel.Apply();
        }

        if (ctx.Input.Keyboard.Pressed(Keys.L))
        {
            if (panel.Children.Count > 0)
            {
                panel.RemoveChild(panel.Children[^1]);
                panel.Apply();
            }
            
           
        }
    }
    */
    
}
