using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Core;

public interface IContent:ILifetime
{
    /// <summary>
    /// 这里是逻辑渲染大小
    /// </summary>
    Target Target { get; } // 感觉这个应该作为Window渲染？随着window大小改变
    public EntityStore World { get; set; }
    
    public Vector2Int LogicResolution { get; }
    
    public List<SystemGroup>? SystemGroups { get; }
}