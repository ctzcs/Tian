

using System.Numerics;
using Friflo.Engine.ECS;

namespace Content.Test;


public struct Building:IComponent
{
    
}
public struct BuildingCatch:IComponent
{
    public int radius;
}

/// <summary>
/// 找到建筑
/// </summary>
public struct FindBuilding
{
    public Vector2 targetPos;
}


/// <summary>
/// 取东西
/// </summary>
public struct GetRes:IComponent
{
    
}



/// <summary>
/// 放下所有的东西
/// </summary>

public struct PutDownRes:IComponent
{
    
}