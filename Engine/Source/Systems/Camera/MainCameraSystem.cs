using System;
using Engine.Components;
using Engine.Core;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Cursor = Engine.Core.Input.Cursor;
using Vector2 = System.Numerics.Vector2;

namespace Engine.Systems;

public partial class MainCameraSystem : QuerySystem
{
    private EntityStore world;
    private App ctx;
    private Func<IDrawableTarget> getCameraViewportTarget;
    private float speed;
    private float zoomStep;
    private float deltaTime;
    private int pixelsPerUnit;
    private int lastViewportWidth = -1;
    private int lastViewportHeight = -1;
    public MainCameraSystem(App ctx,GameContent gameContent,int pixelsPerUnit = 16, Func<IDrawableTarget>? cameraViewportTargetProvider = null)
    {
        this.ctx = ctx;
        speed = 10;
        zoomStep = 0.25f;
        this.pixelsPerUnit = pixelsPerUnit;
        getCameraViewportTarget = cameraViewportTargetProvider ?? (() => gameContent.Target);
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
        var cameraViewportTarget = getCameraViewportTarget();
        var orthographicSize = CameraUtils.ZoomToOrthographicSize(cameraViewportTarget.HeightInPixels, 2.5f, pixelsPerUnit);
        CameraUtils.CreateCamera(Engine.Id.MainCamera,cameraViewportTarget,world,0,Vector2.One,orthographicSize,pixelsPerUnit);
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

            var wheel = ctx.Input.Mouse.Wheel.Y;
            if (wheel != 0)
            {
                var screenPosition = Cursor.GetScreenPosition(new Vector2(camera.viewRectInPixels.Width, camera.viewRectInPixels.Height));
                var zoomDelta = -wheel * zoomStep;
                CameraUtils.ZoomAround(ref transform, ref camera, screenPosition, zoomDelta);
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
        if (!world.HasUniqueEntity(Engine.Id.MainCamera)) return;
        var cameraEntity = world.GetUniqueEntity(Engine.Id.MainCamera);
        ref var c = ref cameraEntity.GetComponent<Camera2D>();
        ref var t = ref cameraEntity.GetComponent<CTransform>();
        SyncCameraViewport(ref c, ref t);
    }

    void SyncCameraViewport(ref Camera2D camera, ref CTransform transform)
    {
        var cameraViewportTarget = getCameraViewportTarget();
        var width = cameraViewportTarget.WidthInPixels <= 0 ? 1 : cameraViewportTarget.WidthInPixels;
        var height = cameraViewportTarget.HeightInPixels <= 0 ? 1 : cameraViewportTarget.HeightInPixels;

        if (lastViewportWidth == width && lastViewportHeight == height &&
            camera.viewRectInPixels.Width == width && camera.viewRectInPixels.Height == height)
            return;

        CameraUtils.SetViewport(ref camera, width, height);
        lastViewportWidth = width;
        lastViewportHeight = height;
    }
}