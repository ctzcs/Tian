using System.Numerics;
using Engine;
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Cursor = Engine.Core.Input.Cursor;

namespace Content.Test;


public struct InfoState : IComponent
{
    public StateSystem.EState State;
    public int count;
}

public partial class StateSystem : QuerySystem
{
    private EntityStore world;
    private App ctx;
    private Resources res;
    private float stateEase = 1;
    public Entity line = default;
    
    
    public enum EState
    {
        Frog,
        Line,
        Building,
        Bg,
    }
    
    public StateSystem(EntityStore world,App app,Resources res)
    {
        this.world = world;
        this.ctx = app;
        this.res = res;
        
    }


    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world.CreateEntity(new UniqueEntity("InfoState"),
            new InfoState()
        {
            State = EState.Frog,
        });
    }

    protected override void OnUpdate()
    {
        if (!world.HasUniqueEntity("InfoState")) return;
        ref var state = ref world.GetUniqueEntity("InfoState").GetComponent<InfoState>();
        if (!world.HasUniqueEntity(BuildInEntityId.MainCamera)) return;
        var cameraEntity = world.GetUniqueEntity(BuildInEntityId.MainCamera);
        ref var camera = ref cameraEntity.GetComponent<Camera2D>();
        ref var cameraTransform = ref cameraEntity.GetComponent<CTransform>();
        if (!Cursor.CanGameUse())
            return;
        switch (state.State)
        {
            case EState.Frog:
                if (ctx.Input.Keyboard.Pressed(Keys.Space))
                {
                    state.State = EState.Line;
                }
                
                if (ctx.Input.Mouse.Down(MouseButtons.Right))
                {
                    var screenPosition = CameraUtils.ViewportToLogicScreen(Cursor.ViewportPosition,res.target.SizeInPixels() );//ctx.Input.Mouse.Position;
                    var pos = CameraUtils.ScreenToWorld(screenPosition, cameraTransform, camera);
                    //var pos = CameraUtils.ScreenToWorld(screenPosition,ctx.Window,res.logicSize);
                    //TestExt.CreateSimpleFrog(world, pos,0,Vector2.One,texture, Color.Red);
                    int count = 0;
                    for (int i = 0; i < count; i++)
                    {
                        //TestExt.CreateArrayUnit(world, pos);
                        //TestExt.CreateSimpleFrogWithMaterial(world, pos + Vector2.One * i * 4,0,Vector2.One,"frog/0" , Color.Black,res.customMaterial,1);
                        TestExt.CreateRenderSortingTestCase(world, pos);
                    }

                    int countAnim = 20;
                    for (int i = 0; i < countAnim; i++)
                    {
                        TestExt.CreateAnimFrog(world, pos - Vector2.One * i * 4, 0, new Vector2(1,1), "frog", "idle",
                            Color.White, 0);
                    }
                    
                    state.count = state.count + count + countAnim;
                }
                
                break;
            case EState.Line:
                if (ctx.Input.Keyboard.Pressed(Keys.Space))
                {
                    state.State = EState.Building;
                }

                if (ctx.Input.Keyboard.Pressed(Keys.R))
                {
                    CreateRandomLine(in cameraTransform, in camera);
                }
                else if (ctx.Input.Mouse.LeftDown)
                {
                    if (line == default)
                    {
                        line = TestExt.CreatLine(world,"example/2",res.customMaterial,4,Vector2.Zero,0,Vector2.One,Color.Gray,2f);
                    }
                    ref var render = ref line.GetComponent<LineRenderer>();

                    var screenPosition = CameraUtils.ViewportToLogicScreen(Cursor.ViewportPosition, res.target.SizeInPixels());//ctx.Input.Mouse.Position;
                    var pos = CameraUtils.ScreenToWorld(screenPosition, cameraTransform, camera);
                    render.AddPoint(pos);
                }
                else if (ctx.Input.Mouse.RightPressed)
                {
                    if (line != default)
                    {
                        line.GetComponent<LineRenderer>().RemoveLast();
                    }
                }
                else if (ctx.Input.Keyboard.Pressed(Keys.S))
                {
                    line = default;
                }

                break;
            case EState.Building:
                if (ctx.Input.Keyboard.Pressed(Keys.Space))
                {
                    state.State = EState.Bg;
                }
                
                if (ctx.Input.Mouse.LeftPressed)
                {
                    var building = Engine.Asset.Assets.GetSubtexture("bd/2");
                    TestExt.CreateBuilding(world, Vector2.Zero, res.customMaterial,0,Vector2.One, building, Color.Red,1);
                    
                }
                break;
            case EState.Bg:
                if (ctx.Input.Keyboard.Pressed(Keys.Space))
                {
                    state.State = EState.Frog;
                }
                
                if (ctx.Input.Mouse.RightDown)
                {
                    var screenPosition = CameraUtils.ViewportToLogicScreen(Cursor.ViewportPosition,res.target.SizeInPixels() );//ctx.Input.Mouse.Position;
                    var pos = CameraUtils.ScreenToWorld(screenPosition, cameraTransform, camera);
                    int countAnim = 1;
                    for (int i = 0; i < countAnim; i++)
                    {
                        var entity = TestExt.CreateAnimBg(world, pos, 0, new Vector2(1,1), "TestBg_0", "Idle",
                            Color.White, 0);
                    }
                }
                
                break;
        }
    }

    void CreateRandomLine(in CTransform cameraTransform, in Camera2D camera)
    {
        line = TestExt.CreatLine(world, "example/2", res.customMaterial, 4, Vector2.Zero, 0, Vector2.One, Color.White, 1f);

        ref var render = ref line.GetComponent<LineRenderer>();

        int idx = 0;
        world.Query<LineRenderer>().ForEachEntity((ref LineRenderer _, Entity __) => { idx++; });

        var screenPosition = CameraUtils.ViewportToLogicScreen(Cursor.ViewportPosition, res.target.SizeInPixels());
        var anchor = CameraUtils.ScreenToWorld(screenPosition, cameraTransform, camera);

        const float goldenAngle = 2.39996323f;
        const float spacing = 2f;
        float theta = idx * goldenAngle;
        float radius = spacing * MathF.Sqrt(idx);
        var start = anchor + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * radius;

        var rng = Rng.Randomized();

        var axis = new Vector2(rng.Float(-1f, 1f), rng.Float(-1f, 1f)).Normalized(Vector2.UnitX);
        var perp = axis.TurnRight();

        float step = rng.Float(0.6f, 1.0f);
        float amp = rng.Float(0.4f, 1.2f);
        float freq = rng.Float(0.35f, 0.9f);
        float phase = rng.Float(0f, 6.2831853f);

        float drift = 0f;
        for (int i = 0; i < 20; i++)
        {
            drift += rng.Float(-0.15f, 0.15f);
            if (drift < -1f) drift = -1f;
            else if (drift > 1f) drift = 1f;

            float t = i;
            float lateral = (MathF.Sin(t * freq + phase) * 0.65f + drift * 0.35f) * amp;
            var p = start + axis * (t * step) + perp * lateral;
            render.AddPoint(p);
        }
    }
}


public partial class InfoSystem:QuerySystem
{
    private Resources res;
    private EntityStore world;

    public InfoSystem(Resources res)
    {
        this.res = res;
    }
    
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
    }

    protected override void OnUpdate()
    {
        if (!world.HasUniqueEntity("InfoState")) return;
        ref var stateInfo = ref world.GetUniqueEntity("InfoState").GetComponent<InfoState>();
        var state = stateInfo.State;
        res.batcher.Clear();
        //显示文本
        float wholeHeight = 0;
        int maxWidth = 0;
        float heightSpace = 2;
        int leftAlign = 10;
        string frogGroupCountTxt = $"Frog Group Count:{stateInfo.count}";
        string stateTxt = $"State:{state},Press Space To Change";
        string lineTxt = $"left mouse press add point,right mouse press cut down line";
        string frogTxt = $"left mouse press add frog";
        string buildingTxt = $"left mouse press add building--你好?:↑";
        //显示面板
        res.batcher.Quad(new Quad(new Vector2(0,0),new Vector2(600,0),new Vector2(600,100),new Vector2(0,100)),Color.Green);
        res.batcher.Text(res.font,frogGroupCountTxt , new Vector2(leftAlign, wholeHeight), Color.Black);
        wholeHeight += res.font.HeightOf(frogGroupCountTxt) + heightSpace;
        res.batcher.Text(res.font, stateTxt, new Vector2(leftAlign,wholeHeight), Color.Black);
        wholeHeight += res.font.HeightOf(stateTxt) + heightSpace;
        switch (state)
        {
            case StateSystem.EState.Line:

                res.batcher.Text(res.font,lineTxt,new Vector2(leftAlign,wholeHeight),color:Color.Black);
                break;
            case StateSystem.EState.Frog:
                res.batcher.Text(res.font,frogTxt,new Vector2(leftAlign,wholeHeight),color:Color.Black);
                break;
            case StateSystem.EState.Building:
                res.batcher.Text(res.font,buildingTxt,new Vector2(leftAlign,wholeHeight),color:Color.Black);
                break;
        }
        //画出圆心
        /*res.batcher.CircleLine(Vector2.Zero,20,3,10,Color.Black);
        res.batcher.CircleLine( res.logicSize /2f,20,3,10,Color.Red);
        res.batcher.CircleLine(res.logicSize ,20,3,10,Color.Black);
        res.batcher.Render(res.target);
        res.batcher.Clear();*/
    }
}
    
