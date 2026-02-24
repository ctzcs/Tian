/*using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Engine.Performance;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Test;


public partial class DestroyNotActiveEntitySystem:QuerySystem
{
    public DestroyNotActiveEntitySystem()
    {
        
    }

    public override void Update(in float deltaTime)
    {
#if DEBUG
        using var zone = Profiler.BeginZone(nameof(DestroyNotActiveEntitySystem));
#endif
        DestroyQuery(world);
    }

    [Query]
    [All<NoActive,Unit>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Destroy(in Entity entity)
    {
        if (entity.IsAlive())
        {
            world.Destroy(entity);
        }
    }

    protected override void OnUpdate()
    {
        
    }
}*/