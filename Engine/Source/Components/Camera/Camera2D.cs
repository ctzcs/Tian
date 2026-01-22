using System.Numerics;
using Foster.Framework;
using Friflo.Engine.ECS;
using Cursor = Engine.Core.Input.Cursor;


namespace Engine.Components;
public struct Camera2D:IComponent
{
    
    //TODO 相机的视口，这里应该可能跟Target相关
    /// <summary>
    /// 相机视口，x,y为左上角，w,h为参考的Pixels宽高
    /// </summary>
    public Rect viewRect;
    /// <summary>
    /// 缩放比例
    /// </summary>
    public float zoom;// Camera zoom (scaling), should be 1.0f by default
    /// <summary>
    /// 偏移
    /// </summary>
    public Vector2 offset;// Camera offset (displacement from target) 一般是屏幕中心，决定屏幕映射到哪里
    public Vector2 target;// Camera target (rotation and zoom origin) 相机中心或者玩家

    public int pixelsPerUnit;
    /*public Vector2 SetScaleRate(float scaleRateChange,Window window)
    {
        //相机补偿
        float newZoom = Calc.Clamp(zoom + scaleRateChange, 0.1f, 10f);
        //这里不对，应该是逻辑屏幕
        
        var screenPosition = Cursor.GetScreenPosition(logicSize);
        var worldPos = CameraUtils.ScreenToWorld(screenPosition,window,logicSize);
        zoom = newZoom;
        var newWorldPos = CameraUtils.ScreenToWorld(screenPosition,window,logicSize);

        var cursorOffset = worldPos - newWorldPos;
        
        return cursorOffset;
    }*/
    
    
}


public static class CameraUtils
{
    /// <summary>
    /// 放缩
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="camera"></param>
    /// <param name="mouseScreen"></param>
    /// <param name="zoomDelta"></param>
    public static void ZoomAround(ref CTransform transform, ref Camera2D camera, Vector2 mouseScreen, float zoomDelta)
    {
        // TODO 关于Camera的部分，逻辑坐标和窗口坐标的统一
        var preWorld = ScreenToWorld(mouseScreen, transform, camera);
        camera.zoom = Calc.Clamp(camera.zoom + zoomDelta, 0.001f, 20f);
        var postWorld = ScreenToWorld(mouseScreen, transform, camera);
        var delta = preWorld - postWorld;
        transform.SetLocalPosition(transform.localPosition + delta);
        //Log.Info($"World: {preWorld}:{postWorld}");
    }
    
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
    public static Entity CreateCamera(string cameraId,IDrawableTarget target,EntityStore world,float rotation ,Vector2 size,float zoom,int pixelsPerUnit)
    {
        var ent = world.CreateEntity(new UniqueEntity($"{cameraId}"),
            new CTransform(default,Vector2.Zero, rotation,size),
            new Camera2D()
            {
                viewRect = new Rect(0,0, target.WidthInPixels, target.HeightInPixels),
                offset = Vector2.Zero,
                zoom = zoom,
                pixelsPerUnit = pixelsPerUnit
            },
            new CheckBox()
            {
                Pivot = RectPivot.Center
            });
        
        return ent;
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
    /// 屏幕坐标转世界坐标
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="cameraTransform">相机Transform</param>
    /// <param name="camera"></param>
    /// <returns></returns>
    public static Vector2 ScreenToWorld(Vector2 screen, in CTransform cameraTransform, in Camera2D camera)
    {
        var mat = GetCameraMatrix(cameraTransform, camera);       // World -> Screen
        Matrix3x2.Invert(mat, out var inv);                 // Screen -> World
        return Vector2.Transform(screen, inv);              // 正确的向量变换
    }


    public static Vector2 GetWorldMousePosition(Vector2 logicScreen,in CTransform cameraTransform, in Camera2D camera)
    {
        var screenPosition = ViewportToLogicScreen(Cursor.ViewportPosition,logicScreen );//ctx.Input.Mouse.Position;
        var pos = ScreenToWorld(screenPosition, cameraTransform, camera);
        return pos;
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
        
        Matrix3x2 result = Matrix3x2.Identity;
        
        Matrix3x2 matOrigin = Matrix3x2.CreateTranslation(-transform.position); //将相机带着整个世界平移到原点
        
        //也就是说，移动1 其实移动16像素
        Matrix3x2 matUnitScale = Matrix3x2.CreateScale(camera.pixelsPerUnit);       // 单位 → 像素
        Matrix3x2 matScale = Matrix3x2.CreateScale(camera.zoom); 
        Matrix3x2 matRotate = Matrix3x2.CreateRotation(transform.rad);
        Matrix3x2 matTranslation = Matrix3x2.CreateTranslation(camera.viewRect.Center); //把世界原点摆到屏幕中希望的位置
        result *= matOrigin;
        result *= matUnitScale;
        result *= matScale;
        result *= matRotate;
        result *= matTranslation;
        return result;
    }
    
    /* TODO 现在编辑器模式下的鼠标位置不能映射到游戏中
     * 1. Game模式下/Editor模式下，输入鼠标的坐标为窗口的屏幕坐标
     * 2. 窗口屏幕坐标转换成逻辑屏幕坐标
     * 3. 逻辑屏幕坐标转换成视口坐标 
     * 4. 视口坐标转换成世界坐标
     * 锚定鼠标位置放缩：
     * 在改变缩放前后，分别求鼠标屏幕坐标对应的世界坐标，计算两者差值，将相机位置平移这个差值，从而“锚定”该世界点。
     */

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
        var halfSizePixels = new Vector2(camera.viewRect.Width, camera.viewRect.Height) * 0.5f / camera.zoom;

        var viewMin = camCenterPixels - halfSizePixels;
        var viewMax = camCenterPixels + halfSizePixels;
        return (viewMin, viewMax);
    }
    
    public static (float, float) GetViewWidthHeightInPixels(in CTransform camTransform, in Camera2D camera)
    {
        var viewSizePixels = new Vector2(camera.viewRect.Width, camera.viewRect.Height) / camera.zoom;
        return (viewSizePixels.X, viewSizePixels.Y);
    }
    
    
    public static (Vector2, Vector2) GetViewMinAndMaxInWorld(in CTransform camTransform, in Camera2D camera)
    {
        var halfSizeWorld = new Vector2(camera.viewRect.Width, camera.viewRect.Height)
            * 0.5f / (camera.zoom * camera.pixelsPerUnit);
        var camCenter = camTransform.position;

        var viewMin = camCenter - halfSizeWorld;
        var viewMax = camCenter + halfSizeWorld;
        return (viewMin, viewMax);
    }
    
    public static (float, float) GetViewWidthHeightInWorld(in CTransform camTransform, in Camera2D camera)
    {
        var viewSizeWorld = new Vector2(camera.viewRect.Width, camera.viewRect.Height)
            / (camera.zoom * camera.pixelsPerUnit);
        return (viewSizeWorld.X, viewSizeWorld.Y);
    }
    
    
}