using System.Collections.Generic;
using Engine.Asset;
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
    Vector2Int OutputResolution { get; set; }
    Rect GameViewportRect { get; }
    void OnResize(GraphicsDevice graphicsDevice, int width, int height);
}



public abstract class GameContent : IContent
{
    public App App { get; set; }
    public AssetManager AssetManager { get; } = new();
    public Target Target { get; set; }
    
    public Vector2Int OutputResolution { get; set; }
    public virtual Rect GameViewportRect => Target != null ? new Rect(0, 0, Target.Width, Target.Height) : default;
    

    public GameContent(App app)
    {
        App = app;
        OutputResolution = new Vector2Int(app.Window.WidthInPixels, app.Window.HeightInPixels); 
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


public abstract class EcsGameContent : GameContent, IEcsContent
{
    public EntityStore World { get; set; }
    public List<SystemGroup>? SystemGroups { get; set; } = new();
    
    protected EcsGameContent(App app) : base(app)
    {
        World = new EntityStore();
    }

    public override void Destroy()
    {
        World = null;
    }
}