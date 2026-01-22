
using Engine.Components;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

public partial class TransformSystem : QuerySystem
{
    EntityStore world;
    ArchetypeQuery<CTransform> leafTransformQuery;
    ArchetypeQuery<CTransform,CheckBox> checkBoxQuery;
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
        leafTransformQuery = world.Query<CTransform>();
        checkBoxQuery = world.Query<CTransform,CheckBox>();
    }

    /*/// <summary>
    /// 所有根节点操作
    /// </summary>
    /// <param name="transform"></param>
    [Query]
    [All<Transform>, None<ChildOf>]
    public void RootTransform(ref Transform transform)
    {
        Transform.CalculateWorldPosition(ref transform);
    }*/

    
    protected override void OnUpdate()
    {
        leafTransformQuery.ForEachEntity((ref transform, entity) =>
        {
            if (!transform.HasChildren)
            {
                transform.UpdateTransform();
            }
        });
        
        checkBoxQuery.ForEachEntity((ref transform, ref checkBox, entity) =>
        {
            switch (checkBox.Pivot)
            {
                case RectPivot.BottomCenter:
                    //一般都是底部中心是Transform.position
                    checkBox.rect.BottomCenter = transform.position;
                    break;
                default:
                    checkBox.rect.Center = transform.position;
                    break;
            }
            
        });
    }
}