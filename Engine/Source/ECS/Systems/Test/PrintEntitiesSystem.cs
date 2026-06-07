/*using Arch.AOT.SourceGenerator;
using Arch.Core;
using Arch.System;
using Engine.Utility;
using Foster.Framework;

namespace Engine.Test;

[Component]
public struct PrintEntity
{
    
}


public partial class PrintEntitiesSystem : BaseSystem<World, float>
{
    public PrintEntitiesSystem(World world) : base(world)
    {
    }


    public override void Update(in float t)
    {
        base.Update(in t);
        PrintEntitiesQuery(World);
    }

    [Query]
    [All(typeof(PrintEntity))]
    void PrintEntities(in Entity entity)
    {
        Log.Info($"{entity.Id}:{EcsUtils.PrintEntity(entity)}");
    }
}*/