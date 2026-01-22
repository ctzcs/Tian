using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Components;
using Engine.Core.Extensions;
using Engine.Render;
using Engine.Utility;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

//AfterBeforeRender
//TODO 尝试优化版本，但使用场景最好是全sprite
public class RenderSystemV1:QuerySystem
{
    /*private const int BatchRenderCount = 32768;*/
    private Batcher batcher;
    private Target renderTarget;
    private int renderCount = 0;
    private readonly SortEntry[] sortEntries = new SortEntry[1_000_000];
    private readonly RenderItem[] renderItems = new RenderItem[1_000_000];
    private int entityCount = 0;
    private Matrix3x2 transformMatrix;
    private EntityStore World;
    private App ctx;
    private ArchetypeQuery<CTransform, LineRenderer, SortingOrder> lineQuery;
    private ArchetypeQuery<CTransform, SpriteRenderer, SortingOrder> spriteQuery;
    
    /// <summary>
    /// 排序Key
    /// </summary>
    public struct SortEntry : IComparer<SortEntry>, IComparable<SortEntry>
    {
        public ulong key;
        public int index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(SortEntry a, SortEntry b) => a.CompareTo(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(SortEntry b)
        {
            ulong a0 = key;
            ulong b0 = b.key;
            if (a0 < b0) return -1;
            if (a0 > b0) return 1;
            return 0;
        }
    }

    //渲染Item
    public struct RenderItem
    {
        public Material? material;
        public Subtexture subtexture;
        public Color color;
        public Vector2 originInPixels;
        public Vector2 position;
        public Vector2 scaleUnits;
        public float rad;
    }
    
    public RenderSystemV1(App app,Batcher batcher,Target renderTarget)
    {
        ctx = app;
        this.batcher = batcher;
        this.renderTarget = renderTarget;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        this.World = store;
        lineQuery = World.Query<CTransform, LineRenderer, SortingOrder>();
        spriteQuery = World.Query<CTransform,SpriteRenderer,SortingOrder>().AllTags(Tags.Get<InsiderView>());
    }

    
    protected override void OnUpdate()
    {
        ref var camera = ref World.GetUniqueEntity(BuildInEntityId.MainCamera).GetComponent<Camera2D>();
        int ppu = camera.pixelsPerUnit;
        //画线
        lineQuery.ForEachEntity((ref transform,ref lineRenderer,ref sortingOrder,entity) =>
        {
            lineRenderer.Draw(batcher, in transform, ppu);
        });
        
        
        //画Sprite
        entityCount = 0;
        //这里不是一定要sr的，目前sr用来刷新
        spriteQuery.ForEachEntity((ref transform,ref sr,ref sortingOrder,entity) =>
        {
            if (entity.IsNull) return;
            sr.InitTexture();

            ushort layer16 = SortingOrderExtensions.NormalizeLayer16(sortingOrder.layerMask);
            ushort depth16 = SortingOrderExtensions.NormalizeDepth16(sortingOrder.depth);

            int idx = entityCount;
            var pos = transform.position;
            float invPpu = 1f / ppu;

            renderItems[idx] = new RenderItem
            {
                subtexture = sr.subtexture,
                material = sr.material,
                color = sr.color,
                originInPixels = sr.originInPixels,
                position = pos,
                scaleUnits = transform.scale * invPpu,
                rad = transform.rad
            };

            sortEntries[idx] = new SortEntry
            {
                key = ((ulong)layer16 << 48) | ((ulong)depth16 << 32) | (uint)Mathf.FloatToSortable(pos.Y),
                index = idx
            };

            entityCount++;
        } );
        
        HandleSpriteRenderList();
        
        
#if DEBUG
        //BugBox
        World.Query<CTransform,CheckBox>().ForEachEntity((ref transform, ref box, entity) =>
        {
            if (!box.IsEnable) return;
            batcher.QuadLine(box.rect.TopLeft,box.rect.TopRight,box.rect.BottomRight,box.rect.BottomLeft,0.1f,Color.Red);
        });
        if (World.HasUniqueEntity(BuildInEntityId.Performance))
        {
            ref var batchStats = ref World.GetUniqueEntity(BuildInEntityId.Performance).GetComponent<RenderBatchStats>();
            batchStats.BatchCount = batcher.BatchCount;
        }
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HandleSpriteRenderList()
    {
            if (entityCount > 1)
            {
                //TODO 也许这里可以使用基数排序优化->但目前就这样吧
                Array.Sort(sortEntries, 0, entityCount);
            }
            
            renderCount = 0;
            Material? currentMaterial = null;
            for (int i = 0; i < entityCount; i++)
            {
                ref readonly var item = ref renderItems[sortEntries[i].index];

                var nextMat = item.material;
                if (nextMat != currentMaterial)
                {
                    if (currentMaterial != null) batcher.PopMaterial();
                    if (nextMat != null) batcher.PushMaterial(nextMat);
                    currentMaterial = nextMat;
                }

                batcher.Image(item.subtexture, item.position, item.originInPixels, item.scaleUnits, item.rad, item.color);
                renderCount++;
            }
            if (currentMaterial != null) batcher.PopMaterial();
        
        
        
    }
}