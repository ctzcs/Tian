using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Components;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

public class AnimationSystem:QuerySystem
{
    private EntityStore World;
    private ArchetypeQuery<CTransform,Animator,SpriteRenderer> spriteSettingQuery;

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        World = store;
        spriteSettingQuery = store.Query<CTransform,Animator, SpriteRenderer>().AllTags(Tags.Get<InsiderView>());
    }

    protected override void OnUpdate()
    {
        /*if (!World.HasUniqueEntity("MainCamera")) return;
        var cameraEntity = World.GetUniqueEntity("MainCamera");
        var camTransform = cameraEntity.GetComponent<CTransform>();
        var camera = cameraEntity.GetComponent<Camera2D>();

        var viewMinMax = CameraUtils.GetViewMinAndMax(camTransform, camera);*/
        
        spriteSettingQuery.ForEachEntity((ref transform, ref animator,ref sr,entity) =>
        {
            /*if (!CameraUtils.IsVisible(transform,sr,viewMinMax.Item1,viewMinMax.Item2)) return;*/
            animator.millisecond = Tick.time;
            SetAnimToSpriteRenderer(ref animator,ref sr);
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAnimToSpriteRenderer(ref Animator animator, ref SpriteRenderer spriteRenderer)
    {
        var subtexture = animator.GetSubtexture();
        spriteRenderer.subtexture = subtexture;
        var pivot = animator.sprite?.Origin;
        spriteRenderer.originInPixels = pivot??new Vector2(subtexture.Width / 2, subtexture.Height/2);
    }
}