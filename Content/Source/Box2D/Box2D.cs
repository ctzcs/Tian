using Box2D.NET;
using Engine.Core;
using Engine.Systems;
using Engine.Systems.Editor;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Source;

/// <summary>
/// 世界入口的模板
/// </summary>
public class Box2D : GameContent
{
    private readonly Batcher batcher;
    public Box2D(App app) : base(app)
    {
        Target = new Target(app.GraphicsDevice,LogicResolution.X,LogicResolution.Y);
        batcher = new Batcher(app.GraphicsDevice);
        World = new EntityStore();
    }

    private B2WorldId physicsWorld;
    private DebugContext debug;
    private float zoom = 1f;
    private float pixelsPerMeter = 16f;
    
    public override void Start()
    {
        var defaultConfig = B2Types.b2DefaultWorldDef();
        defaultConfig.gravity = new B2Vec2(0,-10f);
        physicsWorld = B2Worlds.b2CreateWorld(defaultConfig);
        debug = new DebugContext(batcher, lineWeight: 2f, circleSteps: 32);
        var bodyDef = B2Types.b2DefaultBodyDef();
        bodyDef.position = new B2Vec2(0f,-10f);
        var staticBody = B2Bodies.b2CreateBody(physicsWorld,bodyDef);
        B2Shapes.b2CreatePolygonShape(staticBody, B2Types.b2DefaultShapeDef(),B2Geometries.b2MakeBox(100,1f));
        B2Shapes.b2CreatePolygonShape(staticBody, B2Types.b2DefaultShapeDef(),B2Geometries.b2MakeOffsetBox(1,100f,new B2Vec2(-50.0f,0.0f),B2MathFunction.b2Rot_identity));
        B2Shapes.b2CreatePolygonShape(staticBody, B2Types.b2DefaultShapeDef(),B2Geometries.b2MakeOffsetBox(1,100f,new B2Vec2(50.0f,0.0f),B2MathFunction.b2Rot_identity));
        
        bodyDef.type = B2BodyType.b2_dynamicBody;
        var rng = Rng.Randomized();
        for (int i = 0; i < 500; i++)
        {
            var point = rng.PointInside(new Rect(0,10,20, 20));
            bodyDef.position = new B2Vec2(point.X,point.Y); // 稍微堆成一条柱子
            var body = B2Bodies.b2CreateBody(physicsWorld, bodyDef);
            B2Shapes.b2CreateCircleShape(body,
                B2Types.b2DefaultShapeDef(),
                new B2Circle(new B2Vec2(0,0), 0.5f)); // 半径 0.5 米
        }

        BuildSystemPipeline(out updateRoot, out  renderGroup);
    }
    
    SystemRoot updateRoot;
    SystemRoot renderGroup;

    public override void Destroy()
    {
        batcher.Dispose();
        Target.Dispose();
        World = null;
    }

    public override void Update()
    {
        updateRoot.Update(new UpdateTick(deltaTime:Ctx.Time.Delta,time:(float)Ctx.Time.Seconds));
        B2Worlds.b2World_Step(physicsWorld,(float)Ctx.Time.Delta,4);
    }

    
    public override void Render()
    {
        Target.Clear(Const.DefaultColor);
        
        renderGroup.Update(new UpdateTick(deltaTime:Ctx.Time.Delta,time:(float)Ctx.Time.Seconds));
        B2Worlds.b2World_Draw(physicsWorld, debug.DebugDraw);
        batcher.Render(Target);
        batcher.Clear();
    }


    void BuildSystemPipeline(out SystemRoot updateRoot, out SystemRoot renderGroup)
    {
        updateRoot = new SystemRoot(World,"Update");
        updateRoot.Add(new MainCameraSystem(Ctx,this));
        updateRoot.Add(new CameraCullingSystem());
        updateRoot.Add(new TransformSystem());
        
        renderGroup = new SystemRoot(World,"Render");
        renderGroup.Add(new AnimationSystem());
        renderGroup.Add(new BeforeRenderWorldSystem(batcher));
        renderGroup.Add(new HierarchyOrderSystem());
        renderGroup.Add(new PerformanceSystem());
        renderGroup.Add(new CoordinateSystem(Ctx,batcher));
        renderGroup.Add(new SelectableSystem(Ctx));
        renderGroup.Add(new CameraCullingDebugSystem(batcher));
        renderGroup.Add(new RenderSystem(Ctx,batcher,Target));
    }

    
}