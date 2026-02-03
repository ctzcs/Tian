using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Components;
using Engine.Core.Extensions;
using Engine.Performance;
using Engine.Render;
using Engine.Utility;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

//AfterBeforeRender
//渲染示例，这里的Render只支持sprite可能不太对。
// TODO 没有做极致的优化，因为这里希望之后可以兼容LineRenderer等各种Renderer，集成排序
public class RenderSystem:QuerySystem
{
    /*private const int BatchRenderCount = 32768;*/
    private Batcher batcher;
    private Target renderTarget;
    private int renderCount = 0;
    private readonly OrderRecord[] entities = new OrderRecord[1_000_000];
    private int entityCount = 0;
    private Matrix3x2 transformMatrix;
    private EntityStore World;
    private App ctx;
    private ArchetypeQuery<CTransform, LineRenderer, SortingOrder> lineQuery;
    private ArchetypeQuery<CTransform,SpriteRenderer,SortingOrder,HierarchyOrder> spriteQuery;
    
    public struct OrderRecord : IComparer<OrderRecord>, IComparable<OrderRecord>
    {
        public Entity entity;
        public ulong key0;
        public ulong key1;
        public uint key2; // 防止抖动
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(OrderRecord a, OrderRecord b) => a.CompareTo(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(OrderRecord b)
        {
            //升序排序
            ulong a0 = key0;
            ulong b0 = b.key0;
            if (a0 < b0) return -1;
            if (a0 > b0) return 1;

            ulong a1 = key1;
            ulong b1 = b.key1;
            if (a1 < b1) return -1;
            if (a1 > b1) return 1;

            uint a2 = key2;
            uint b2 = b.key2;
            if (a2 < b2) return -1;
            if (a2 > b2) return 1;
            return 0;
        }
    }
    
    public RenderSystem(App app,Batcher batcher,Target renderTarget)
    {
        ctx = app;
        this.batcher = batcher;
        this.renderTarget = renderTarget;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        World = store;
        lineQuery = World.Query<CTransform, LineRenderer, SortingOrder>();
        spriteQuery = World.Query<CTransform,SpriteRenderer,SortingOrder,HierarchyOrder>().AllTags(Tags.Get<InsiderView>());
    }

    
    protected override void OnUpdate()
    {
        ref var camera = ref World.GetUniqueEntity(Engine.Id.MainCamera).GetComponent<Camera2D>();
        int ppu = camera.pixelsPerUnit;
        //画线
        //batcher.PushSampler(new TextureSampler(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
        Material? currentLineMaterial = null;
        lineQuery.ForEachEntity((ref transform, ref lineRenderer, ref sortingOrder, entity) =>
        {
            var nextMat = lineRenderer.material;
            if (nextMat != currentLineMaterial)
            {
                if (currentLineMaterial != null) batcher.PopMaterial();
                if (nextMat != null) batcher.PushMaterial(nextMat);
                currentLineMaterial = nextMat;
            }

            lineRenderer.DrawGeometry(batcher, in transform, ppu);
        });
        if (currentLineMaterial != null) batcher.PopMaterial();
        //batcher.PopSampler();
        //画Sprite
        entityCount = 0;
        int spriteCount = spriteQuery.Count;
        if (spriteCount > 30000)
        {
            RenderSpriteDirectly(ref camera);
        }
        else
        {
            spriteQuery.ForEachEntity((ref transform,ref sr,ref sortingOrder, ref hierarchyOrder,entity) =>
            {
                if (entity.IsNull) return;
                sr.InitTexture();

                ushort layer16 = SortingOrderExtensions.NormalizeLayer16(sortingOrder.layerMask);
                ushort depth16 = SortingOrderExtensions.NormalizeDepth16(sortingOrder.depth);

                entities[entityCount++] = new OrderRecord()
                {
                    entity = entity,
                    key0 = ((ulong)layer16 << 48) | ((ulong)depth16 << 32) | hierarchyOrder.group,
                    key1 = ((ulong)(uint)hierarchyOrder.depth << 32) | hierarchyOrder.index,
                    key2 = unchecked((uint)entity.Id),
                };
            } );
        
            HandleSpriteRenderList(ref camera);
        }
        
        
#if DEBUG
        //BugBox
        World.Query<CTransform,CheckBox>().ForEachEntity((ref transform, ref box, entity) =>
        {
            if (!box.IsEnable) return;
            box.Draw(transform,batcher);
        });
#endif
        if (World.HasUniqueEntity(Engine.Id.Performance))
        {
            ref var batchStats = ref World.GetUniqueEntity(Engine.Id.Performance).GetComponent<RenderBatchStats>();
            batchStats.BatchCount = batcher.BatchCount;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HandleSpriteRenderList(ref Camera2D camera)
    {
        int ppu = camera.pixelsPerUnit;
        if (entityCount > 1)
        {
            //TODO 也许这里可以使用基数排序优化->但目前就这样吧
            Array.Sort(entities,0, entityCount);
        }
        renderCount = 0;
        Material? currentMaterial = null;
        for (int i = 0; i < entityCount; i++)
        {
            var entity = entities[i].entity;
            ref var spriteRenderer = ref entity.GetComponent<SpriteRenderer>();
            ref var transform = ref entity.GetComponent<CTransform>();
            var nextMat = spriteRenderer.material;
            if (nextMat != currentMaterial)
            {
                if (currentMaterial != null) batcher.PopMaterial();
                if (nextMat != null) batcher.PushMaterial(nextMat);
                currentMaterial = nextMat;
            }
            spriteRenderer.DrawGeometry(batcher, in transform, ppu);
            renderCount++;
        }
        if (currentMaterial != null) batcher.PopMaterial();
    }


    void RenderSpriteDirectly(ref Camera2D camera)
    {
        Material? currentMaterial = null;
        renderCount = 0;
        int ppu = camera.pixelsPerUnit;
        spriteQuery.ForEachEntity((ref transform, ref sr, ref sortingOrder, ref hierarchyOrder, entity) =>
        {
            if (entity.IsNull) return;
            sr.InitTexture();
            var nextMat = sr.material;
            if (nextMat != currentMaterial)
            {
                if (currentMaterial != null) batcher.PopMaterial();
                if (nextMat != null) batcher.PushMaterial(nextMat);
                currentMaterial = nextMat;
            }
            sr.DrawGeometry(batcher, in transform, ppu);
            renderCount++;
            if (currentMaterial != null) batcher.PopMaterial();
        });
        
    }
}