
using System;
using Engine.Components;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Core.Graphics;

public enum RenderPassSpace
{
    Screen,
    World,
}

public class RenderPass
{
    public Batcher Batcher { get; }
    public Target? OutputTarget { get; set; }
    public SystemRoot RenderGroup { get; }
    public Entity CameraEntity { get; }
    public Color ClearColor { get; set; }
    public Action<UpdateTick>? PostUpdate { get; set; }
    public RenderPassSpace Space { get; set; }
    public bool ShouldClear { get; set; }

    public RenderPass(Batcher batcher, SystemRoot renderGroup, Entity cameraEntity, Color clearColor, Target? outputTarget = null, Action<UpdateTick>? postUpdate = null, RenderPassSpace space = RenderPassSpace.Screen, bool shouldClear = true)
    {
        Batcher = batcher;
        RenderGroup = renderGroup;
        CameraEntity = cameraEntity;
        ClearColor = clearColor;
        OutputTarget = outputTarget;
        PostUpdate = postUpdate;
        Space = space;
        ShouldClear = shouldClear;
    }

    public RenderPass(RenderContext context, SystemRoot renderGroup, Entity cameraEntity, Color clearColor, Action<UpdateTick>? postUpdate = null, RenderPassSpace space = RenderPassSpace.Screen, bool shouldClear = true)
        : this(context.Batcher, renderGroup, cameraEntity, clearColor, context.Target, postUpdate, space, shouldClear)
    {
    }

    public void Render(UpdateTick tick, Target? fallbackTarget)
    {
        var target = OutputTarget ?? fallbackTarget;
        if (target == null)
            throw new InvalidOperationException("RenderPass requires an output target.");

        if (ShouldClear)
            target.Clear(ClearColor);

        var hasCameraMatrix = false;
        if (Space == RenderPassSpace.World)
        {
            if (CameraEntity.IsNull || !CameraEntity.HasComponent<Camera2D>() || !CameraEntity.HasComponent<CTransform>())
                throw new InvalidOperationException("World RenderPass requires a valid CameraEntity with Camera2D and CTransform.");

            ref var camera = ref CameraEntity.GetComponent<Camera2D>();
            ref var transform = ref CameraEntity.GetComponent<CTransform>();
            var matrix = camera.worldToScreenMatrix;
            if (matrix == default)
            {
                matrix = CameraUtils.GetCameraMatrix(transform, camera);
                camera.worldToScreenMatrix = matrix;
            }
            Batcher.PushMatrix(matrix);
            hasCameraMatrix = true;
        }

        try
        {
            RenderGroup.Update(tick);
            PostUpdate?.Invoke(tick);
        }
        finally
        {
            if (hasCameraMatrix)
            {
                try
                {
                    Batcher.PopMatrix();
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        Batcher.Render(target);
        Batcher.Clear();
    }
}




/*
//TODO 
- RenderSystem = RenderPass 执行器（读取 Camera + Batcher + Target）
- Before/AfterRenderWorldSystem 只保留结构或者直接移除
- Coordinate/Selectable/Debug 作为 RenderPass 中的一部分，绑定具体相机
*/