using System.Numerics;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using Cursor = Engine.Core.Input.Cursor;


namespace Engine.Components;


public struct Camera2D : IComponent
{
    /// <summary>
    /// 相机对应的渲染目标像素区域，左上角为原点。
    /// 当前项目里通常等于逻辑分辨率下的 Target 像素尺寸。
    /// </summary>
    public RectInt viewRectInPixels;

    /// <summary>
    /// 正交相机半高，单位是世界单位。
    /// </summary>
    public float orthographicSize;

    /// <summary>
    /// 资源像素到世界单位的换算基准。
    /// 例如 16 表示 16 像素约定为 1 个世界单位。
    /// </summary>
    public int pixelsPerUnit;

    [Ignore]
    public float PixelsPerWorldUnit => viewRectInPixels.Height / (2f * orthographicSize);

    [Ignore]
    public float ViewHeightInWorld => orthographicSize * 2f;

    [Ignore]
    public float ViewWidthInWorld => ViewHeightInWorld * viewRectInPixels.Width / viewRectInPixels.Height;

    [Ignore]
    public Matrix3x2 worldToScreenMatrix;

    [Ignore]
    public Matrix3x2 screenToWorldMatrix;
}


public static class CameraUtils
{
    #region Core
    public static void SetViewport(ref Camera2D camera, int width, int height)
    {
        width = width <= 0 ? 1 : width;
        height = height <= 0 ? 1 : height;
        camera.viewRectInPixels = new RectInt(0, 0, width, height);
    }

    public static void SetOrthographicSize(ref Camera2D camera, float orthographicSize)
    {
        camera.orthographicSize = Calc.Clamp(orthographicSize, 0.001f, 100000f);
    }

    public static float ZoomToOrthographicSize(int viewportHeight, float zoom, int pixelsPerUnit)
    {
        viewportHeight = viewportHeight <= 0 ? 1 : viewportHeight;
        pixelsPerUnit = pixelsPerUnit <= 0 ? 1 : pixelsPerUnit;
        zoom = Calc.Clamp(zoom, 0.001f, 20f);
        return viewportHeight / (2f * zoom * pixelsPerUnit);
    }

    public static float GetZoom(in Camera2D camera)
    {
        return camera.viewRectInPixels.Height / (2f * camera.orthographicSize * camera.pixelsPerUnit);
    }

    public static void SetZoom(ref Camera2D camera, float zoom)
    {
        SetOrthographicSize(ref camera, ZoomToOrthographicSize(camera.viewRectInPixels.Height, zoom, camera.pixelsPerUnit));
    }

    /// <summary>
    /// 将渲染目标上的像素坐标转换为世界坐标。
    /// </summary>
    /// <param name="targetPixelPosition">Target 像素坐标，左上角为 (0,0)。</param>
    /// <param name="cameraTransform">相机 Transform，单位是世界单位。</param>
    /// <param name="camera">相机参数，viewRectInPixels 表示 Target 像素尺寸。</param>
    public static Vector2 ScreenPxToWorld(Vector2 targetPixelPosition, in CTransform cameraTransform, in Camera2D camera)
    {
        var inv = camera.screenToWorldMatrix;
        if (inv == default)
        {
            var mat = GetCameraMatrix(cameraTransform, camera);
            Matrix3x2.Invert(mat, out inv);
        }
        return Vector2.Transform(targetPixelPosition, inv);
    }

    public static Vector2 WorldToScreenPx(Vector2 worldPosition, in CTransform cameraTransform, in Camera2D camera)
    {
        var mat = camera.worldToScreenMatrix;
        if (mat == default)
            mat = GetCameraMatrix(cameraTransform, camera);
        return Vector2.Transform(worldPosition, mat);
    }

    public static Vector2 WorldToViewport(Vector2 worldPosition, in CTransform cameraTransform, in Camera2D camera)
    {
        var screen = WorldToScreenPx(worldPosition, cameraTransform, camera);
        float width = Math.Max(1, camera.viewRectInPixels.Width);
        float height = Math.Max(1, camera.viewRectInPixels.Height);
        return new Vector2(screen.X / width, screen.Y / height);
    }

    /// <summary>
    /// 从世界坐标到屏幕坐标的矩阵
    /// World To Screen
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="camera"></param>
    /// <returns></returns>
    public static Matrix3x2 GetCameraMatrix(in CTransform transform,in Camera2D camera)
    {
        //Copy from raylib
        // The camera in world-space is set by
        //   1. Move it to target
        //   2. Rotate by -rotation and scale by (1/zoom)
        //      When setting higher scale, it's more intuitive for the world to become bigger (= camera become smaller),
        //      not for the camera getting bigger, hence the invert. Same deal with rotation
        //   3. Move it by (-offset);
        //      Offset defines target transform relative to screen, but since we're effectively "moving" screen (camera)
        //      we need to do it into opposite direction (inverse transform)

        // Having camera transform in world-space, inverse of it gives the modelview transform
        // Since (A*B*C)' = C'*B'*A', the modelview is
        //   1. Move to offset
        //   2. Rotate and Scale
        //   3. Move by -target
        
        /*Matrix3x2 result = Matrix3x2.Identity;
        
        Matrix3x2 matOrigin = Matrix3x2.CreateTranslation(-transform.position); //将相机带着整个世界平移到原点
        
        //也就是说，移动1 其实移动16像素
        Matrix3x2 matUnitScale = Matrix3x2.CreateScale(camera.pixelsPerUnit);       // 单位 → 像素
        Matrix3x2 matScale = Matrix3x2.CreateScale(camera.zoom,camera.zoom); 
        Matrix3x2 matRotate = Matrix3x2.CreateRotation(transform.rad);
        Matrix3x2 matTranslation = Matrix3x2.CreateTranslation(camera.viewRect.Center); //把世界原点摆到屏幕中希望的位置
        result *= matOrigin;
        result *= matUnitScale;
        result *= matScale;
        result *= matRotate;
        result *= matTranslation;
        return result;*/
        return GetViewMatrix(transform, camera) * GetProjectionMatrix(camera);
    }

    public static void UpdateCachedMatrices(ref Camera2D camera, in CTransform transform)
    {
        camera.worldToScreenMatrix = GetCameraMatrix(transform, camera);
        Matrix3x2.Invert(camera.worldToScreenMatrix, out camera.screenToWorldMatrix);
    }
    
    //世界到相机矩阵
    public static Matrix3x2 GetViewMatrix(in CTransform cameraTransform, in Camera2D camera)
    {
        var transform = cameraTransform;
        var position = transform.GetWorldPosition();
        var rotation = transform.GetWorldRotation();
        Matrix3x2 result = Matrix3x2.Identity;
        result *= Matrix3x2.CreateTranslation(-position); //将相机带着整个世界平移到原点
        result *= Matrix3x2.CreateRotation(-rotation);
        return result;
    }

    //相机->屏幕像素矩阵
    public static Matrix3x2 GetProjectionMatrix(in Camera2D camera)
    {
        // 将世界空间的Y向上翻转成Y向下，再把原点挪到视口中心。
        Matrix3x2 result = Matrix3x2.Identity;
        //世界坐标里 1 个单位，在屏幕上要画 scale 个像素 。
        float scale = camera.PixelsPerWorldUnit;
        result *= Matrix3x2.CreateScale(scale,-scale);
        result *= Matrix3x2.CreateTranslation(camera.viewRectInPixels.Width*0.5f, camera.viewRectInPixels.Height*0.5f);
        return result;
    }

    #endregion

    #region Tools
    /// <summary>
    /// Create Cameraz
    /// </summary>
    /// <param name="cameraId"></param>
    /// <param name="target"></param>
    /// <param name="world"></param>
    /// <param name="rotation"></param>
    /// <param name="scale">相机 Transform 的缩放，不是屏幕尺寸。</param>
    /// <param name="pixelsPerUnit"></param>
    /// <returns></returns>
    public static Entity CreateCamera(string cameraId, IDrawableTarget target,EntityStore world,float rotation ,Vector2 scale,float orthographicSize, int pixelsPerUnit)
    {
        var camera = new Camera2D
        {
            pixelsPerUnit = pixelsPerUnit,
        };
        SetOrthographicSize(ref camera, orthographicSize);
        SetViewport(ref camera, target.WidthInPixels, target.HeightInPixels);

        var ent = world.CreateEntity(new UniqueEntity($"{cameraId}"),
            new CTransform(default,Vector2.Zero, rotation,scale),
            camera,
            new CheckBox()
            {
                Pivot = RectPivot.Center
            },
            new MetaGroup()
            {
                GroupName = "Unique",
                SubGroupName = "BuildIn"
            });
        
        return ent;
    }

    public static void ZoomAround(ref CTransform transform, ref Camera2D camera, Vector2 mouseScreenPx, float zoomDelta)
    {
        var preMatrix = GetCameraMatrix(transform, camera);
        Matrix3x2.Invert(preMatrix, out var preInv);
        var preWorld = Vector2.Transform(mouseScreenPx, preInv);
        SetZoom(ref camera, GetZoom(camera) + zoomDelta);
        var postMatrix = GetCameraMatrix(transform, camera);
        Matrix3x2.Invert(postMatrix, out var postInv);
        var postWorld = Vector2.Transform(mouseScreenPx, postInv);
        var delta = preWorld - postWorld;
        transform.SetLocalPosition(transform.localPosition + delta);
    }

    public static Vector2 ViewportToLogicScreen(Vector2 viewportPosition,Vector2 targetSizeInPixels)
    {
        return new Vector2(targetSizeInPixels.X * viewportPosition.X, targetSizeInPixels.Y * viewportPosition.Y);
    }

    public static Vector2 GetWorldMousePosition(Vector2 targetSizeInPixels, in CTransform cameraTransform, in Camera2D camera)
    {
        var targetPixelPosition = ViewportToLogicScreen(Cursor.GameViewportPosition, targetSizeInPixels);
        return ScreenPxToWorld(targetPixelPosition, cameraTransform, camera);
    }

    /// <summary>
    /// 剔除SpriteRenderer
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="sr"></param>
    /// <param name="viewMin"></param>
    /// <param name="viewMax"></param>
    /// <param name="camera2D"></param>
    /// <returns></returns>
    public static bool IsVisible(in CTransform transform, in SpriteRenderer sr,Vector2 viewMin,Vector2 viewMax,in Camera2D camera2D)
    {
        // 世界空间下实体中心
        var pos = transform.position;

        // 估算实体的世界尺寸（不考虑旋转，够用了）
        var halfSize = new Vector2(sr.subtexture.Width, sr.subtexture.Height) 
                       * transform.scale * 0.5f / camera2D.pixelsPerUnit; //考虑像素转实际坐标

        var min = pos - halfSize;
        var max = pos + halfSize;
        // AABB 相交
        return max.X >= viewMin.X && min.X <= viewMax.X
                                  && max.Y >= viewMin.Y && min.Y <= viewMax.Y;
    }


    public static (Vector2, Vector2) GetViewMinAndMaxInPixels(in CTransform camTransform, in Camera2D camera)
    {
        var camCenterPixels = camTransform.position * camera.pixelsPerUnit;
        var halfSizePixels = new Vector2(camera.ViewWidthInWorld, camera.ViewHeightInWorld) * 0.5f * camera.pixelsPerUnit;

        var viewMin = camCenterPixels - halfSizePixels;
        var viewMax = camCenterPixels + halfSizePixels;
        return (viewMin, viewMax);
    }
    
    public static (float, float) GetViewWidthHeightInPixels(in CTransform camTransform, in Camera2D camera)
    {
        var viewSizePixels = new Vector2(camera.ViewWidthInWorld, camera.ViewHeightInWorld) * camera.pixelsPerUnit;
        return (viewSizePixels.X, viewSizePixels.Y);
    }
    
    
    public static (Vector2, Vector2) GetViewMinAndMaxInWorld(in CTransform camTransform, in Camera2D camera)
    {
        var halfSizeWorld = new Vector2(camera.ViewWidthInWorld, camera.ViewHeightInWorld) * 0.5f;
        var camCenter = camTransform.position;

        var viewMin = camCenter - halfSizeWorld;
        var viewMax = camCenter + halfSizeWorld;
        return (viewMin, viewMax);
    }
    
    public static (float, float) GetViewWidthHeightInWorld(in Camera2D camera)
    {
        return (camera.ViewWidthInWorld, camera.ViewHeightInWorld);
    }
    #endregion
    
}
