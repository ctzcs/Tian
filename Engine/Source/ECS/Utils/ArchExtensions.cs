/*
using Engine.Components;
using Friflo.Engine.ECS;

namespace Engine.ECS.Utils;

public static class ArchExtensions
{
    /// <summary>
    /// Simple Clone an Entity in a World
    /// </summary>
    /// <param name="world"></param>
    /// <param name="cloneEntity"></param>
    /// <returns></returns>
    public static Entity Clone(this World world, Entity cloneEntity)
    {
        var archetype = cloneEntity.GetArchetype();
        var copiedEntity = world.Create(archetype.Signature);
        // NOTE: can we do this without boxing??
        foreach (var c in cloneEntity.GetAllComponents()) {
            copiedEntity.Set(c);
        }
        
        if (!cloneEntity.Has<CTransform>()) return copiedEntity;
        ref var transform = ref cloneEntity.Get<CTransform>();
        //由于复制的引用而不得不更改关系
        List<Entity> children = new List<Entity>(transform.ChildrenCount);
        for (int i = 0; i < transform.ChildrenCount; i++)
        {
            var childPrefab = transform.Children[i];
            Entity newChild = world.Clone(childPrefab);
            ref var childTransform = ref newChild.Get<CTransform>();
            childTransform.Parent = copiedEntity;
            children[i] = newChild;
        }
        transform.ResetChildren(children);
        return copiedEntity;
    }

    /// <summary>
    /// Clone an entity using a commandBuffer
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="cloneEntity"></param>
    /// <returns></returns>
    public static Entity Clone(this CommandBuffer buffer, Entity cloneEntity)
    {
        var archetype = cloneEntity.GetArchetype();
        var copiedEntity = buffer.Create(archetype.Signature);
        // NOTE: can we do this without boxing??
        foreach (var c in cloneEntity.GetAllComponents()) {
            buffer.Set(copiedEntity,c);
        }
        return copiedEntity;
    }



    public static Entity Instantiate(this World world, World assetWorld, Entity prefabEntity)
    {
        var archetype = assetWorld.GetArchetype(prefabEntity);
        var newEntity = world.Create(archetype.Signature);
        // NOTE: can we do this without boxing??
        // TODO is this copy or just reference-> I think this is copy-> but reference are copy pointer
        foreach (var c in assetWorld.GetAllComponents(prefabEntity)) {
            newEntity.Set(c);
        }

        if (!newEntity.Has<CTransform>()) return newEntity;
        ref var transform = ref newEntity.Get<CTransform>();
        //由于复制的引用而不得不更改关系
        List<Entity> children = new List<Entity>(transform.ChildrenCount);
        for (int i = 0; i < transform.ChildrenCount; i++)
        {
            var childPrefab = transform.Children[i];
            Entity newChild = world.Instantiate(assetWorld, childPrefab);
            ref var childTransform = ref newChild.Get<CTransform>();
            childTransform.Parent = newEntity;
            children[i] = newChild;
        }
        transform.ResetChildren(children);
        return newEntity;
    }
}
*/


