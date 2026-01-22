/*using System.Numerics;
using Engine.Core.Structure;
using Foster.Framework;
using Friflo.Engine.ECS;
using Engine.Asset;
using Engine.Components;
using Engine.Core.Extensions;
using Engine.Render;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;
public struct TilemapDesc : IComponent
{
    public string TilesetId;
    public int TileSize;
    public int ChunkSize;
    public Vector2 Origin;
    public bool Visible;
}

public struct TilemapRequest : ILinkRelation
{
    public Entity target;
    public TilemapRequestType Type;
    public int LayerId;
    public Vector2Int TileIndex;
    public Rect Rect;
    public Color Color;
    public Entity GetRelationKey() => target;
}

public enum TilemapRequestType
{
    SetTile,
    ClearTile,
}


public class TilemapSystem : QuerySystem
{
    private readonly App app;
    private readonly Target target;
    private GraphicsDevice device;
    private EntityStore world;

    private ArchetypeQuery<CTransform, TilemapDesc, SortingOrder> mapQuery;
    private readonly Dictionary<Entity, Tilemap.Tilemap> runtime = new();

    public TilemapSystem(App app, Target target)
    {
        this.app = app;
        this.target = target;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        world = store;
        device = app.GraphicsDevice;
        mapQuery = store.Query<CTransform, TilemapDesc, SortingOrder>();
    }

    protected override void OnUpdate()
    {
        if (!world.HasUniqueEntity("MainCamera"))
            return;

        var camEntity = world.GetUniqueEntity("MainCamera");
        ref var camTransform = ref camEntity.GetComponent<CTransform>();
        ref var camera = ref camEntity.GetComponent<Camera2D>();

        var cam3x2 = CameraUtils.GetCameraMatrix(camTransform, camera);
        var cam4x4 = To4x4(cam3x2);
        var ortho = Matrix4x4.CreateOrthographicOffCenter(0, target.Width, target.Height, 0, 0.1f, 1000.0f);
        var uniform = Matrix4x4.Multiply(ortho, cam4x4);

        mapQuery.ForEachEntity((ref transform, ref desc, ref order, entity) =>
        {
            var tex = Assets.Atlas;
            if (tex == null)
                return;

            if (!runtime.TryGetValue(entity, out var map))
            {
                map = new Tilemap.Tilemap(device, tex, desc.TileSize, desc.ChunkSize);
                runtime[entity] = map;
            }

            map.Origin = transform.position + desc.Origin;

            foreach (var src in entity.GetIncomingLinks<TilemapRequest>())
            {
                var rels = src.Entity.GetRelations<TilemapRequest>();
                for (int i = 0; i < rels.Length; i++)
                {
                    var rel = rels[i];
                    if (rel.target != entity) continue;

                    if (rel.Type == TilemapRequestType.SetTile)
                        map.SetTile(rel.LayerId, rel.TileIndex, rel.Rect, rel.Color, true);
                    else
                        map.ClearTile(rel.LayerId, rel.TileIndex);

                    src.Entity.RemoveRelation<TilemapRequest>(entity);
                }
            }

            if (desc.Visible)
                map.Render(target, uniform);
        });
    }

    
    
    
    private static Matrix4x4 To4x4(Matrix3x2 m)
    {
        return new Matrix4x4(
            m.M11, m.M12, 0, 0,
            m.M21, m.M22, 0, 0,
            0,     0,     1, 0,
            m.M31, m.M32, 0, 1
        );
    }
}*/