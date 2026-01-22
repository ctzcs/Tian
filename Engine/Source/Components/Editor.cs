using Friflo.Engine.ECS;

namespace Engine.Components;

public struct EditorTag:ITag
{
    
}

public struct EditorInfo:IComponent
{
    public string EntityType;
    public string EntityGroup;
}

public struct Prefab : ITag { }