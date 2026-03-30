using System.Numerics;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using Cursor = Engine.Core.Input.Cursor;


namespace Engine.Components;


//编辑器中得缩放现在有问题了
//首先让这个相机中直接有所需的数据，而不需要读取Transform
public struct Camera2D:IComponent
{
    
    //TODO 相机的视口，这里应该可能跟Target相关
    /// <summary>
    /// 相机视口，x,y原点为左上角，w,h为参考的Pixels宽高
    /// </summary>
    public RectInt viewRectInPixels;
    public float orthographicSize;
    public int pixelsPerUnit;
    public float aspect;
    public float nearClip;
    public float farClip;

    [Ignore]
    public Vector2 ViewportSizeInPixels => new(viewRectInPixels.Width, viewRectInPixels.Height);

    [Ignore]
    public float PixelsPerWorldUnit => viewRectInPixels.Height / (2f * orthographicSize);

    [Ignore]
    public float ViewWidthInWorld => ViewHeightInWorld * aspect;

    [Ignore]
    public float ViewHeightInWorld => orthographicSize * 2f;

    [Ignore]
    public Matrix3x2 worldToScreenMatrix;

    [Ignore]
    public Matrix3x2 screenToWorldMatrix;
}


public static class CameraUtils
{
    
    /// <summary>
    /// Create Cameraz
    /// </summary>
    /// <param name="cameraId"></param>
    /// <param name="target"></param>
    /// <param name="world"></param>
    /// <param name="rotation"></param>
    /// <param name="size"></param>
    /// <param name="pixelsPerUnit"></param>
    /// <returns></returns>
    public static Entity CreateCamera(string cameraId, IDrawableTarget target,EntityStore world,float rotation ,Vector2 size,float orthographicSize, int pixelsPerUnit)
    {
        var camera = new Camera2D
        {
            pixelsPerUnit = pixelsPerUnit,
            nearClip = -1f,
            farClip = 1f,
        };
        SetOrthographicSize(ref camera, orthographicSize);
        SetViewport(ref camera, target.WidthInPixels, target.HeightInPixels);

        var ent = world.CreateEntity(new UniqueEntity($"{cameraId}"),
            new CTransform(default,Vector2.Zero, rotation,size),
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
    
    public static void SetViewport(ref Camera2D camera, int width, int height)
    {
        width = width <= 0 ? 1 : width;
        height = height <= 0 ? 1 : height;
        camera.viewRectInPixels = new RectInt(0, 0, width, height);
        camera.aspect = (float)width / height;
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

    public static void SetClipPlanes(ref Camera2D camera, float nearClip, float farClip)
    {
        camera.nearClip = nearClip;
        camera.farClip = farClip <= nearClip ? nearClip + 0.001f : farClip;
    }

    public static Matrix4x4 GetProjectionMatrix4x4(in Camera2D camera)
    {
        return Matrix4x4.CreateOrthographic(camera.ViewWidthInWorld, camera.ViewHeightInWorld, camera.nearClip, camera.farClip);
    }

    public static void ZoomAround(ref CTransform transform, ref Camera2D camera, Vector2 mouseScreenPx, float zoomDelta)
    {
        var preWorld = ScreenPxToWorld(mouseScreenPx, transform, camera);
        SetZoom(ref camera, GetZoom(camera) + zoomDelta);
        var postWorld = ScreenPxToWorld(mouseScreenPx, transform, camera);
        var delta = preWorld - postWorld;
        transform.SetLocalPosition(transform.localPosition + delta);
    }
    
    /// <summary>
    /// 如果是target不在视口中心，得让mouse跟target一样移动到原点，然后放缩
    /// 将Screen坐标转到Window坐标系得到新的屏幕坐标
    /// </summary>
    /// <param name="screenPosition"></param>
    /// <param name="window"></param>
    /// <returns></returns>
    public static Vector2 ScreenToViewport(Vector2 screenPosition,Window window)
    {
        return new Vector2(screenPosition.X / window.WidthInPixels, screenPosition.Y / window.HeightInPixels);
    }

    public static Vector2 ViewportToLogicScreen(Vector2 viewport,Vector2 logicScreen)
    {
        return new Vector2(logicScreen.X * viewport.X, logicScreen.Y *  viewport.Y);
    }
    
    /// <summary>
    /// Target 像素坐标（左上角为 (0,0)）转世界坐标。
    /// </summary>
    /// <param name="screenPx">渲染目标（Target）的像素坐标。</param>
    /// <param name="cameraTransform">相机 Transform（世界单位）。</param>
    /// <param name="camera">相机参数（viewRectInPixels 为 Target 像素尺寸）。</param>
    public static Vector2 ScreenPxToWorld(Vector2 screenPx, in CTransform cameraTransform, in Camera2D camera)
    {
        var mat = GetCameraMatrix(cameraTransform, camera);
        Matrix3x2.Invert(mat, out var inv);
        return Vector2.Transform(screenPx, inv);
    }


    public static Vector2 GetWorldMousePosition(Vector2 targetSizePx, in CTransform cameraTransform, in Camera2D camera)
    {
        var screenPx = ViewportToLogicScreen(Cursor.ViewportPosition, targetSizePx);
        return ScreenPxToWorld(screenPx, cameraTransform, camera);
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
        Matrix3x2 result = Matrix3x2.Identity;
        result *= Matrix3x2.CreateTranslation(-cameraTransform.position); //将相机带着整个世界平移到原点
        result *= Matrix3x2.CreateRotation(-cameraTransform.rad);
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


    #region Culling Func
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


public struct NewCamera2D : IComponent
{
    public Vector2 Center; // 相机世界中心
    public float Rad; // 旋转
    public float Zoom; // zoom
    public int Width; //宽度
    public int Height; //高度
    public int PixelPerUnit; // 单元像素
}


public static class CameraExtensions
{
    // 世界 → 相机（View），世界单位下的相机空间
    public static Matrix3x2 GetViewMatrix(this in NewCamera2D cam)
    {
        Matrix3x2 result = Matrix3x2.Identity;
        result *= Matrix3x2.CreateTranslation(-cam.Center);
        result *= Matrix3x2.CreateRotation(-cam.Rad);
        return result;
    }

    // 相机 → 屏幕像素（Projection + Viewport）
    public static Matrix3x2 GetProjectionMatrix(this in NewCamera2D cam)
    {
        Matrix3x2 result = Matrix3x2.Identity;
        float scale = cam.Zoom * cam.PixelPerUnit;
        // Y 轴翻转：世界/相机空间 Y 向上，映射到屏幕像素 Y 向下
        result *= Matrix3x2.CreateScale(scale, -scale);
        result *= Matrix3x2.CreateTranslation(new Vector2(cam.Width * 0.5f, cam.Height * 0.5f));
        return result;
    }

    // 世界 → 屏幕像素：World → View → Screen
    public static Matrix3x2 GetWorldToScreenMatrix(this in NewCamera2D cam)
    {
        var view = cam.GetViewMatrix();
        var proj = cam.GetProjectionMatrix();
        return view * proj;
    }

    // 屏幕像素 → 世界：逆矩阵
    public static Matrix3x2 GetScreenToWorldMatrix(this in NewCamera2D cam)
    {
        var worldToScreen = cam.GetWorldToScreenMatrix();
        Matrix3x2.Invert(worldToScreen, out var inv);
        return inv;
    }

    // 世界空间下的可见范围（AABB，忽略旋转，适合做剔除）
    public static (Vector2 min, Vector2 max) GetWorldViewBounds(this in NewCamera2D cam)
    {
        var size = cam.GetWorldViewSize();
        var half = new Vector2(size.width * 0.5f, size.height * 0.5f);
        var min = cam.Center - half;
        var max = cam.Center + half;
        return (min, max);
    }

    // 世界空间下的视口宽高（世界单位）
    public static (float width, float height) GetWorldViewSize(this in NewCamera2D cam)
    {
        float widthWorld = cam.Width / (cam.Zoom * cam.PixelPerUnit);
        float heightWorld = cam.Height / (cam.Zoom * cam.PixelPerUnit);
        return (widthWorld, heightWorld);
    }

    // 屏幕像素空间下的可见范围（当前实现相机占满整个屏幕）
    public static (Vector2 min, Vector2 max) GetScreenViewBounds(this in NewCamera2D cam)
    {
        var min = Vector2.Zero;
        var max = new Vector2(cam.Width, cam.Height);
        return (min, max);
    }

    // 屏幕像素空间下的视口宽高（像素）
    public static (float width, float height) GetScreenViewSize(this in NewCamera2D cam)
    {
        return (cam.Width, cam.Height);
    }
}