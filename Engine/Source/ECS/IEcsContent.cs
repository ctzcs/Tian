using Engine.Core;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

public interface IEcsContent : IContent
{
    EntityStore World { get; set; }
    List<SystemGroup>? SystemGroups { get; set; }
}