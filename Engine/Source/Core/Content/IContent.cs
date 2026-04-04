using System.Collections.Generic;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Core;

public interface IContent : ILifetime
{
    /// <summary>
    /// 这里是最终输出渲染大小
    /// </summary>
    Target Target { get; set; }
    EntityStore World { get; set; }
    Vector2Int OutputResolution { get; set; }
    Rect GameViewportRect { get; }
    
    List<SystemGroup>? SystemGroups { get; set; }

    void OnResize(GraphicsDevice graphicsDevice, int width, int height);
}


public abstract class GameContent : IContent
{
    public App Ctx { get; set; }
    public Target Target { get; set; }
    public EntityStore World { get; set; }
    public Vector2Int OutputResolution { get; set; }
    public virtual Rect GameViewportRect => Target != null ? new Rect(0, 0, Target.Width, Target.Height) : default;
    public List<SystemGroup>? SystemGroups { get; set; } = new();

    public GameContent(App ctx)
    {
        Ctx = ctx;
        OutputResolution = new Vector2Int(ctx.Window.WidthInPixels, ctx.Window.HeightInPixels); 
    }

    public virtual void Start()
    {
        
    }

    public virtual void Destroy()
    {
        
    }

    public virtual void Update()
    {
        
    }

    public virtual void Render()
    {
        
    }

    public virtual void OnResize(GraphicsDevice graphicsDevice, int width, int height)
    {
        OutputResolution = new Vector2Int(width, height);
        Target = new Target(graphicsDevice,width,height);
    }
}