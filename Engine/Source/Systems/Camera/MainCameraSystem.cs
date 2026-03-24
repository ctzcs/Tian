using Engine.Components;
using Engine.Core;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Cursor = Engine.Core.Input.Cursor;
using Vector2 = System.Numerics.Vector2;

namespace Engine.Systems;

public partial class MainCameraSystem:QuerySystem
{
    private GameContent gameContent;
    private EntityStore world;
    private App ctx;
    private float speed;
    private float scaleSpeed;
    private float deltaTime;
    private int pixelsPerUnit;
    private int lastViewportWidth = -1;
    private int lastViewportHeight = -1;
    public MainCameraSystem(App ctx,GameContent gameContent,int pixelsPerUnit = 16)
    {
        this.ctx = ctx;
        speed = 10;
        scaleSpeed = 5;
        this.gameContent = gameContent;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
        CameraUtils.CreateCamera(Engine.Id.MainCamera,gameContent.Target,world,0,Vector2.One,2.5f,pixelsPerUnit);
        ctx.Window.OnResize += OnResize;
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        base.OnRemoveStore(store);
        ctx.Window.OnResize -= OnResize;
    }
    
    protected override void OnUpdate()
    {
        if (!world.HasUniqueEntity(Engine.Id.MainCamera)) return;
        deltaTime = Tick.deltaTime;
        var query = world.Query<Camera2D, CTransform>();
#if DEBUG
        if (!Cursor.IsInViewport())
        {
            return;
        }
#endif
        query.ForEachEntity((ref camera, ref transform, entity) =>
        {
            SyncCameraViewport(ref camera, ref transform);
            
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
                var screenPosition = Cursor.GetScreenPosition(new Vector2(gameContent.Target.Width, gameContent.Target.Height));
                CameraUtils.ZoomAround(ref transform,ref camera,screenPosition,deltaTime * scaleSpeed);
                
            }else if (ctx.Input.Mouse.Wheel.Y > 0)
            {
                var screenPosition = Cursor.GetScreenPosition(new Vector2(gameContent.Target.Width, gameContent.Target.Height));
                CameraUtils.ZoomAround(ref transform,ref camera,screenPosition,-deltaTime * scaleSpeed);
            }

            if (ctx.Input.Keyboard.PressedOrRepeated(Keys.E))
            {
                transform.SetLocalRotation(transform.localRad - 1*Calc.DegToRad);
            }else if (ctx.Input.Keyboard.PressedOrRepeated(Keys.Q))
            {
                transform.SetLocalRotation(transform.localRad + 1*Calc.DegToRad);
            }

            CameraUtils.UpdateCachedMatrices(ref camera, in transform);
        });
    }

    void OnResize()
    {
        if (!world.HasUniqueEntity(Engine.Id.MainCamera)) return;
        var cameraEntity = world.GetUniqueEntity(Engine.Id.MainCamera);
        ref var c = ref cameraEntity.GetComponent<Camera2D>();
        ref var t = ref cameraEntity.GetComponent<CTransform>();
        SyncCameraViewport(ref c, ref t);
    }

    void SyncCameraViewport(ref Camera2D camera, ref CTransform transform)
    {
        var width = gameContent.Target.Width <= 0 ? 1 : gameContent.Target.Width;
        var height = gameContent.Target.Height <= 0 ? 1 : gameContent.Target.Height;

        if (lastViewportWidth == width && lastViewportHeight == height &&
            camera.viewRectInPixels.Width == width && camera.viewRectInPixels.Height == height)
            return;

        var oldHeight = camera.viewRectInPixels.Height <= 0 ? height : camera.viewRectInPixels.Height;
        if (oldHeight != height)
        {
            var zoomScale = (float)height / oldHeight;
            camera.zoom = Calc.Clamp(camera.zoom * zoomScale, 0.001f, 20f);
        }

        camera.viewRectInPixels = new RectInt(0, 0, width, height);
        lastViewportWidth = width;
        lastViewportHeight = height;
        CameraUtils.UpdateCachedMatrices(ref camera, in transform);
    }
}