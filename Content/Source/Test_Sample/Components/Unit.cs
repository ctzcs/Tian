using Friflo.Engine.ECS;

namespace Content.Test;

public struct Unit:IComponent
{
    public bool isActive;
    public GroupType group;
    public UnitType type;

    public override string ToString()
    {
        return $"{group} {type}";
    }
}

public enum GroupType
{
    Player,
    Enemy,
    Building,
    Other,
}

public enum UnitType
{
    Frog,
    A,
    Other,
}