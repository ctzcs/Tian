using System;
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Cursor = Engine.Core.Input.Cursor;

namespace Engine.Systems.Editor;


public struct Selector : IComponent
{
    public Entity SelectTarget;
}

public class SelectableSystem:QuerySystem<CTransform,CheckBox>
{
    private App ctx;
    private EntityStore _world;
    private float distanceSq;
    public static readonly string Selector = nameof(Selector);
    
    public SelectableSystem(App ctx)
    {
        this.ctx = ctx;
    }
    
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        _world = store;
        store.CreateEntity(new UniqueEntity("Selector"),
            new MetaGroup()
            {
                GroupName = "Unique",
                SubGroupName = "BuildIn"
            },
            new Selector());
        Filter.AllTags(Tags.Get<InsiderView>());
    }

    protected override void OnUpdate()
    {
        distanceSq = Single.MaxValue;
        if (!_world.HasUniqueEntity("Selector") || !_world.HasUniqueEntity("MainCamera"))
        {
            return;
        }
        
        if (!Cursor.CanGameUse())
            return;
        
        ref var selector = ref _world.GetUniqueEntity("Selector").GetComponent<Selector>();
        var cameraE = _world.GetUniqueEntity("MainCamera");
        ref var cameraTransform = ref cameraE.GetComponent<CTransform>();
        ref var camera = ref cameraE.GetComponent<Camera2D>();
        var screenSize = new System.Numerics.Vector2(camera.viewRect.Width, camera.viewRect.Height);
		var screenPosition = CameraUtils.ViewportToLogicScreen(Cursor.ViewportPosition, screenSize);
		var pos = CameraUtils.ScreenToWorld(screenPosition, cameraTransform, camera);
        Entity lastSelectTarget = selector.SelectTarget;
        Entity selectTarget = default;
        Query.ForEachEntity((ref transform,ref checkBox, entity) =>
        {
            /*if (Mathf.IsInsideCircle(pos,transform.position,1,out var sqDistance))
            {
                if (sqDistance < distanceSq)
                {
                    selectTarget = entity;
                }
            }*/
            if(!selectTarget.IsNull) return;
            if (!checkBox.Contains(transform,pos)) return;
            selectTarget = entity;
        });
        selector.SelectTarget = selectTarget;
        
        if (!lastSelectTarget.IsNull && lastSelectTarget.HasComponent<CheckBox>())
        {
            lastSelectTarget.GetComponent<CheckBox>().IsEnable = false;
        }
        
        if (!selectTarget.IsNull && selectTarget.HasComponent<CheckBox>())
        {
            selectTarget.GetComponent<CheckBox>().IsEnable = true;
        }
    }
}