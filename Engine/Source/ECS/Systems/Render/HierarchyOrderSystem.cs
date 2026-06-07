using Engine.Components;
using Engine.Render;
using Engine.Utility;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.ECS;

public class HierarchyOrderSystem : QuerySystem
{
    private ArchetypeQuery<CTransform, SortingOrder> visibleSpriteQuery;

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        visibleSpriteQuery = store.Query<CTransform, SortingOrder>()
            .AllTags(Tags.Get<InsiderView>());
    }

    protected override void OnUpdate()
    {
        visibleSpriteQuery.ForEachEntity((ref CTransform transform, ref SortingOrder sortingOrder, Entity entity) =>
        {
            if (entity.IsNull)
                return;

            int depth = 0;
            Entity root = entity;
            Entity current = entity;

            for (int guard = 0; guard < 1024; guard++)
            {
                ref var tr = ref current.GetComponent<CTransform>();
                var parent = tr.Parent;

                if (parent.IsNull || !parent.HasComponent<CTransform>())
                {
                    root = current;
                    break;
                }
                
                current = parent;
                depth++;
            }

            ref var rootTransform = ref root.GetComponent<CTransform>();
            var rootY = rootTransform.GetWorldPosition().Y;
            //最终是升序编码所以Y越小越靠前，这里翻转是因为y
            uint group = Mathf.FloatToSortable(-rootY);
            uint index = Mathf.FloatToSortable(-transform.localPosition.Y);

            if (entity.HasComponent<HierarchyOrder>())
            {
                ref var order = ref entity.GetComponent<HierarchyOrder>();
                order.group = group;
                order.depth = depth;
                order.index = index;
            }
            else
            {
                CommandBuffer.AddComponent(entity.Id, new HierarchyOrder
                {
                    group = group,
                    depth = depth,
                    index = index
                });
            }
        });
    }
}