using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Cursor = Engine.Core.Input.Cursor;
using Vector2 = System.Numerics.Vector2;

namespace Engine.Systems;

public partial class CameraSystem:QuerySystem
{
    private EntityStore world;
    private App ctx;
    private float speed;
    private float scaleSpeed;
    private float deltaTime;
    private Target target;
    public static readonly string MainCamera = nameof(MainCamera);
    public CameraSystem(EntityStore world,App ctx,Target target)
    {
        this.world = world;
        this.ctx = ctx;
        speed = 10;
        scaleSpeed = 5;
        this.target = target;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        CameraUtils.CreateCamera(MainCamera,target,world,0,Vector2.One,2.5f,16);
        ctx.Window.OnResize += OnResize;
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        base.OnRemoveStore(store);
        ctx.Window.OnResize -= OnResize;
    }
    
    protected override void OnUpdate()
    {
        if (!world.HasUniqueEntity(MainCamera)) return;
        var cameraEntity = world.GetUniqueEntity(MainCamera);
        deltaTime = Tick.deltaTime;
        var query = world.Query<Camera2D, CTransform>();
        query.ForEachEntity((ref camera, ref transform, entity) =>
        {
            if (ctx.Input.Keyboard.PressedOrRepeated(Keys.Right) 
                || ctx.Input.Keyboard.PressedOrRepeated(Keys.D))
            {
                transform.localPosition.X += deltaTime * speed;
                transform.SetLocalPosition(transform.localPosition);
            }else if (ctx.Input.Keyboard.PressedOrRepeated(Keys.Left)
                      ||ctx.Input.Keyboard.PressedOrRepeated(Keys.A))
            {
                transform.localPosition.X -= deltaTime * speed;
                transform.SetLocalPosition(transform.localPosition);
            }
        
            if (ctx.Input.Keyboard.PressedOrRepeated(Keys.Up)
                ||ctx.Input.Keyboard.PressedOrRepeated(Keys.W))
            {
                transform.localPosition.Y -= deltaTime * speed;
                transform.SetLocalPosition(transform.localPosition);
            }else if (ctx.Input.Keyboard.PressedOrRepeated(Keys.Down)
                      ||ctx.Input.Keyboard.PressedOrRepeated(Keys.S))
            {
                transform.localPosition.Y += deltaTime * speed;
                transform.SetLocalPosition(transform.localPosition);
            }

            if (ctx.Input.Mouse.Wheel.Y < 0)
            {
                var screenPosition = Cursor.GetScreenPosition(new Vector2(target.Width, target.Height));
                CameraUtils.ZoomAround(ref transform,ref camera,screenPosition,deltaTime * scaleSpeed);
                
            }else if (ctx.Input.Mouse.Wheel.Y > 0)
            {
                var screenPosition = Cursor.GetScreenPosition(new Vector2(target.Width, target.Height));
                CameraUtils.ZoomAround(ref transform,ref camera,screenPosition,-deltaTime * scaleSpeed);
            }

            if (ctx.Input.Keyboard.PressedOrRepeated(Keys.E))
            {
                transform.SetLocalRotation(transform.localRad - 1*Calc.DegToRad);
            }else if (ctx.Input.Keyboard.PressedOrRepeated(Keys.Q))
            {
                transform.SetLocalRotation(transform.localRad + 1*Calc.DegToRad);
            }
            
        });
    }

    void OnResize()
    {
        if (!world.HasUniqueEntity(BuildInEntityId.MainCamera)) return;
        var cameraEntity = world.GetUniqueEntity(BuildInEntityId.MainCamera);
        //TODO 除了直接Resize之外，还可以调控camera的缩放比例
        // ref var c = ref cameraEntity.GetComponent<Camera2D>();
        // CameraUtils.SetCameraRectToWindowSize(ref c,ctx.Window);
        
        ref var c = ref cameraEntity.GetComponent<Camera2D>();
        
        c.viewRect = new RectInt(0, 0, target.Width, target.Height);
    }

    
}