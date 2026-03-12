
using Friflo.Engine.ECS;


namespace Content.Test;


public struct FollowLine:IComponent
{
    public Entity line;
    public int nextIndex;
}