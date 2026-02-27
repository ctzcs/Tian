using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Core;

public interface IContent : ILifetime
{
    /// <summary>
    /// 这里是逻辑渲染大小
    /// </summary>
    Target Target { get; set; } // 感觉这个应该作为Window渲染？随着window大小改变
    EntityStore World { get; set; }
    Vector2Int LogicResolution { get; set; }
    
    List<SystemGroup>? SystemGroups { get; set; }

    void OnResize(GraphicsDevice graphicsDevice, int width, int height);
}


public abstract class GameContent : IContent
{
    public App Ctx { get; set; }
    public Target Target { get; set; }
    public EntityStore World { get; set; }
    public Vector2Int LogicResolution { get; set; }
    public List<SystemGroup>? SystemGroups { get; set; } = new();

    public GameContent(App ctx)
    {
        Ctx = ctx;
        LogicResolution = new Vector2Int(ctx.Window.WidthInPixels, ctx.Window.HeightInPixels); 
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
        LogicResolution = new Vector2Int(width, height);
        Target = new Target(graphicsDevice,width,height);
    }
}