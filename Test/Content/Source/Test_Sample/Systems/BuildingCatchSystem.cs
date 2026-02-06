
using Engine.Asset;
using Engine.Components;
using Engine.Core.Extensions;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Color = Foster.Framework.Color;

using Vector2 = System.Numerics.Vector2;

namespace Content.Test;

public class BuildingCatchSystem : QuerySystem<CTransform,Unit>
{
    private EntityStore world;
    private Resources resources;
    
    public BuildingCatchSystem(EntityStore world,Resources res)
    {
        Filter.AllComponents(ComponentTypes.Get<Worker>());
        this.world = world;
        this.resources = res;
    }
    
    List<Entity> checkEntities = [];
    private EntityList _triggerEntity = new();
    private List<Entity> record = new();

    void GetAllChild(List<Entity> child,in Entity root)
    {
        if (root.IsNull || !root.HasComponent<CTransform>())
        {
            return;
        }
        
        var children = root.GetComponent<CTransform>().Children;
        if (children?.Count <= 0) return;
        for (int i = 0; i < children?.Count; i++)
        {
            child.Add(children[i]);
            GetAllChild(child,children[i]);
        }
    }

    protected override void OnUpdate()
    {
        record.Clear();
        Query.ForEachEntity((ref transform,ref unit,entity) =>
        {
            if (transform.HasParent)
            {
                return;
            }

            if (Vector2.DistanceSquared(transform.position, Vector2.Zero) < 64)
            {
                if (transform.HasChildren) return;
                record.Add(entity);
            }
            
            else if (Vector2.DistanceSquared(transform.position, new Vector2(0, resources.logicSize.Y / 2)) < 64)
            {
                checkEntities.Clear();
                GetAllChild(checkEntities, entity);
                for (int i = 0; i < checkEntities.Count; i++)
                {
                    if (!checkEntities[i].IsNull)
                    {
                        // 调试输出
                        ref var childTransform = ref checkEntities[i].GetComponent<CTransform>();
                        if (childTransform.Parent != default) checkEntities[i].SetParent(default);
                        if (!checkEntities[i].HasComponent<NoActive>()) checkEntities[i].Add(new NoActive());
                    }
                }
            }
            
        });
        foreach (var entity in record)
        {
            var child = TestExt.CreateFrogCarrier(
                world,
                new Vector2(0, 1),
                0,
                Vector2.One,
                "frog/0",
                Color.Blue,
                4,
                4);
            child.SetParent(entity);
        }
        
        //方法一：有随机访问
        /*_triggerEntity.Clear();
        Query.Entities.ToEntityList(_triggerEntity);
        //当遍历的过程种有东西改变了那个Store的情况
        foreach (var entity in _triggerEntity)
        {
            ref var transform = ref entity.GetComponent<CTransform>();
            if (transform.HasParent)
            {
                return;
            }

            if (Vector2.DistanceSquared(transform.position, Vector2.Zero) < 64)
            {
                if (transform.HasChildren) return;
                var child = TestExt.CreateFrogCarrier(
                    world,
                    new Vector2(0, -1),
                    0,
                    Vector2.One,
                    "frog/0",
                    Color.Blue,
                    4,
                    4);
                child.SetParent(entity);
            }


            else if (Vector2.DistanceSquared(transform.position, new Vector2(0, resources.logicSize.Y / 2)) < 64)
            {
                checkEntities.Clear();
                GetAllChild(checkEntities, entity);
                for (int i = 0; i < checkEntities.Count; i++)
                {
                    if (!checkEntities[i].IsNull)
                    {
                        // 调试输出
                        ref var childTransform = ref checkEntities[i].GetComponent<CTransform>();
                        if (childTransform.Parent != default) checkEntities[i].SetParent(default);
                        if (!checkEntities[i].HasComponent<NoActive>()) checkEntities[i].Add(new NoActive());
                    }

                }
            }
        }
        */

    }
}