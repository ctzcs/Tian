using System.Numerics;
using System.Reflection;
using Engine;
using Engine.Asset;
using Engine.Asset.v1;
using Engine.Components;
using Engine.Core;
using Engine.Core.Graphics;
using Engine.Systems;
using Engine.Systems.Editor;
using Engine.UI;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Test;

public class ATestSample : GameContent
{
    private readonly App ctx;
    private readonly Batcher batcher;
    private SystemRoot updateRoot;
    private SystemRoot renderGroup;
    private Rng rng = new(1337);
    private Resources res;
    private float deltaTime = 0;
    private Material customMaterial;
    private Target tempTarget;
    

    public ATestSample(App ctx)  : base(ctx)
    {
        this.ctx = ctx;
        World = new EntityStore()
        {
            JobRunner = new ParallelJobRunner(Environment.ProcessorCount)
        };
        //资源出适合
        AssetsV1.Pack(Assets.ContentAssetsPath,"pack.zip");
        AssetsV1.LazyInitializeCache("pack.zip");
        Assets.LoadSpritesFromGz(ctx.GraphicsDevice);
        
        
        
        //Assets.Load(ctx.GraphicsDevice);
        var font = new SpriteFont(ctx.GraphicsDevice, 
            Path.Join(Assets.ContentAssetsPath, "Fonts", "SmileySans-Oblique.ttf"), 
            32);
        Assets.SetFont(font);
        var asm = Assembly.GetExecutingAssembly();
        customMaterial = 
            //GraphicsUtils.CreateMaterial(ctx.GraphicsDevice, asm, "Custom", 0, 1, 1, 1);
        GraphicsUtils.CreateMaterial(ctx.GraphicsDevice, asm,"Dissolve",0,1,1,1);
        //GraphicsUtils.CreateMaterial(ctx.GraphicsDevice, asm,"Slime",0,1,1,1);
        batcher = new Batcher(ctx.GraphicsDevice);
        Target = new Target(ctx.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        res = new Resources(
            Target,
            font,
            Assets.Atlas,
            batcher,
            LogicResolution,
            customMaterial);
        tempTarget = new Target(ctx.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        
    }
    

    void RebuildSystem()
    {
        var uiRoot = BuildUI();
        //系统模块构建
        updateRoot = new SystemRoot(World, "TestGroup");
        updateRoot.Add(new UiSystem(uiRoot,new UIDebugOverlay()));
        updateRoot.Add(new StateSystem(World,ctx,res));
        updateRoot.Add(new BuildingCatchSystem(World,res));
        updateRoot.Add(new FindLineSystem(World,rng));
        updateRoot.Add(new BehaviorSystem());
        updateRoot.Add(new MainCameraSystem(ctx,this));
        updateRoot.Add(new CameraCullingSystem());
        updateRoot.Add(new TransformSystem());
        updateRoot.Add(new AnimationSystem());
        renderGroup = new SystemRoot(World,"render");
        
        renderGroup.Add(new InfoSystem(res));
        renderGroup.Add(new BeforeRenderWorldSystem(batcher));
        renderGroup.Add(new HierarchyOrderSystem());
        
        renderGroup.Add(new PerformanceSystem());
        renderGroup.Add(new CoordinateSystem(ctx,batcher));
        renderGroup.Add(new SelectableSystem(ctx));
        renderGroup.Add(new CameraCullingDebugSystem(batcher));
        
        renderGroup.Add(new RenderSystem(ctx,res.batcher,Target));
        renderGroup.Add(new AfterRenderWorldSystem(batcher));
        renderGroup.Add(new UiRenderSystem(batcher));
        SystemGroups.Clear();
        SystemGroups.Add(updateRoot);
        SystemGroups.Add(renderGroup);
    }
    
    UIRoot BuildUI()
    {
        var uiRoot = new UIRoot(ctx,res.logicSize);
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
        btn1.Text = "MEMEMEM你好o";
        panel1.AddChild(btn1);
        
        uiRoot.Root.AddChild(panel1);
        return uiRoot;
    }
    
    public override void Start()
    {
        //构建系统
        RebuildSystem();
        //Profiler.AppInfo("Hello AppInfo!");
    }

    public override void Destroy()
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
        Target.Dispose();
        tempTarget.Dispose();

        Assets.DeleteCache();
        World.JobRunner.Dispose();
        World = null;
        updateRoot = null;
        renderGroup = null;
        res = null;
        customMaterial = null;
    }

    public override void Update()
    {
        deltaTime = ctx.Time.Delta;

        var time = (float)ctx.Time.Seconds;
        var lifeTime = Math.Clamp(time * 2, 0, Single.Pi / 2);
        var strength = 0.5f + 0.5f * MathF.Cos(lifeTime);
        customMaterial.Fragment.SetUniformBuffer(new Vector4(time, strength, 0, 0), slot: 0);
        updateRoot.Update(new UpdateTick(ctx.Time.Delta,(float)ctx.Time.Seconds));
    }
    
    public override void Render()
    {
        //GameRender
        tempTarget.Clear(Const.DefaultColor);
        Target.Clear(Color.Transparent);
        //var zone = Profiler.BeginZone("renderGroup");
        renderGroup.Update(new UpdateTick(ctx.Time.Delta,(float)ctx.Time.Seconds));
        //zone.Dispose();
        // 使用：将该码点嵌入字符串（C# 中转成 char）
        batcher.Render(tempTarget);
        batcher.Clear();
        PostProcess(tempTarget,Target,customMaterial);//TODO 这个也应该放到系统中
        //Profiler.EmitFrameMark();
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


    public override void OnResize(GraphicsDevice graphicsDevice, int width, int height)
    {
        base.OnResize(graphicsDevice, width, height);
        tempTarget = new Target(graphicsDevice, width, height);
    }
}
