using System;
using Engine.Components;
using Foster.Framework;
using Friflo.Engine.ECS;

namespace Engine.Core.Extensions;

public static class FrifloExtensions
{
    extension(EntityStore world)
    {
        /// <summary>
        /// 实例化实体，这里会自动解析并创建子对象同时解引用
        /// TODO 设置实例化位置和旋转等，目前不能用于Query
        /// </summary>
        /// <param name="srcEntity">src可以来自于不同的Store</param>
        /// <returns></returns>
        public Entity Instantiate(Entity srcEntity)
        {
            var tarEntity = world.CreateEntity();
            srcEntity.CopyEntity(tarEntity);
            if (tarEntity.Tags.Has<Prefab>())
            {
                tarEntity.RemoveTag<Prefab>();
            }
            //重建父子关系
            if (!srcEntity.HasComponent<CTransform>()) return tarEntity;
            ref var srcTransform = ref srcEntity.GetComponent<CTransform>();
            for (int i = 0; i < srcTransform.ChildrenCount; i++)
            {
                var newChild = world.Instantiate(srcTransform.Children[i]);
                newChild.SetParent(tarEntity);
            }
            return tarEntity;
        }

        public bool HasUniqueEntity(string name)
        {
            Entities entities = world.ComponentIndex<UniqueEntity, string>()[name];
            switch (entities.Count)
            {
                case 0:
                    return false;
                case 1:
                    return true;
                default:
                    throw new Exception($"Multiple entities with name {name} {entities.Count}");
            }
        }

        
        public void InstantiateRoots(IEnumerable<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.HasComponent<CTransform>())
                {
                    ref var transform = ref entity.GetComponent<CTransform>();
                    if (!transform.HasParent)
                    {
                        world.Instantiate(entity);
                    }
                }
            }
        }
    }
}