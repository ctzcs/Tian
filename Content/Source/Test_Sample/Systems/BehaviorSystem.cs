using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Content.Test;

public struct Behavior : IComponent
{
    public IBehavior behavior;
}

public interface IBehavior
{
    void Update(){}
}

public class BehaviorA : IBehavior
{
    public Vector3 value;
    public void Update()
    {
        value += Vector3.One;
    }
}

public class BehaviorB : IBehavior
{
    public int value;
    public void Update()
    {
        value++;
    }
}


public class BehaviorSystem:QuerySystem<Behavior>
{
    private EntityStore World;
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        World =  store;
    }

    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref behavior,entity) =>
        {
            behavior.behavior.Update();
        });
    }
}
