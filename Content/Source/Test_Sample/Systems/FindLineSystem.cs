using Engine.Components;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;


namespace Content.Test;

public partial class FindLineSystem:QuerySystem
{
    private EntityStore world;
    private Rng rng;
    private float speed = 5;
    private float deltaTime = 0.02f;
    ArchetypeQuery<LineRenderer> lineQuery;
    ArchetypeQuery<CTransform,CheckBox> workerQuery;
    private List<Entity> _lineEntities = new List<Entity>();
    public FindLineSystem(EntityStore world,Rng rng)
    {
        this.world = world;
        this.rng = rng;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        lineQuery = store.Query<LineRenderer>();
        workerQuery = store.Query<CTransform,CheckBox>().AllComponents(ComponentTypes.Get<Worker>());
    }
    
    protected override void OnUpdate()
    {
        
        _lineEntities.Clear();
        lineQuery.ForEachEntity(((ref lineRenderer, entity) =>
        {
            _lineEntities.Add(entity);
        } ));
        
        workerQuery.ForEachEntity(((ref transform, ref box, entity) =>
        {
            if (_lineEntities.Count == 0 || transform.HasParent)
            {
                return;
            }
            if (!entity.IsNull && entity.HasComponent<FollowLine>())
            {
                ref var followLine = ref entity.GetComponent<FollowLine>();
            
                if (!followLine.line.IsNull)
                {
                    ref var lineRenderer = ref followLine.line.GetComponent<LineRenderer>();
                    if (followLine.nextIndex < lineRenderer.line.Count )
                    {
                        var pos = lineRenderer.line[followLine.nextIndex];
                        var dir = (pos - transform.localPosition).Normalized();
                        //可能出现由于速度太快，导致超出线的位置的情况
                        if (!box.rect.Contains(pos) /*Vector2.DistanceSquared(pos,transform.localPosition) > 1*/ ) 
                        {
                            transform.SetLocalPosition(transform.localPosition + deltaTime*dir*speed);
                        }
                        else
                        {
                            followLine.nextIndex++;
                            if (followLine.nextIndex > lineRenderer.line.Count - 1)
                            {
                                var index = rng.Int(0, _lineEntities.Count);
                                followLine.line = _lineEntities[index];
                                followLine.nextIndex = 0;
                            }
                        }
                    }else
                    {
                        var index = rng.Int(0, _lineEntities.Count);
                        followLine.line = _lineEntities[index];
                        followLine.nextIndex = 0;
                    }
                }
            }
            else
            {
                var index =  rng.Int(0, _lineEntities.Count);
                CommandBuffer.AddComponent(entity.Id,new FollowLine()
                {
                    line = _lineEntities[index],
                    nextIndex = 0
                });
                /*entity.AddComponent(new FollowLine()
                {
                    line = _lineEntities[index],
                    nextIndex = 0
                });*/
            }
        } ));
        
        
    }
}