
using Engine.Asset;
using Engine.Components;
using Engine.Core.Extensions;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Color = Foster.Framework.Color;

using Vector2 = System.Numerics.Vector2;

namespace Content.Test;

public partial class BuildingCatchSystem:QuerySystem<CTransform>
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
        Query.ForEachEntity((ref transform, entity) =>
        {
            if (transform.HasParent)
            {
                return;
            }
            if (Vector2.DistanceSquared(transform.position,Vector2.Zero) < 64)
            {
                if (!transform.HasChildren)
                {
                    var child = TestExt.CreateFrogCarrier(
                        world,
                        new Vector2(0,-1),
                        0,
                        Vector2.One,
                        "frog/0",
                        Color.Blue,
                        4,
                        4); 
                    child.SetParent(entity);
                }
            }
        
        
            else if (Vector2.DistanceSquared(transform.position,new Vector2(0,resources.logicSize.Y/2)) < 64)
            {
                checkEntities.Clear();
                GetAllChild(checkEntities,entity);
                for (int i = 0; i < checkEntities.Count; i++)
                {
                    if (!checkEntities[i].IsNull)
                    {
                        // 调试输出
                        ref var childTransform = ref checkEntities[i].GetComponent<CTransform>();
                        if (childTransform.Parent != default) checkEntities[i].SetParent(default);
                        if(!checkEntities[i].HasComponent<NoActive>()) checkEntities[i].Add(new NoActive());
                    }
                    
                }
            }
        });
    }
}