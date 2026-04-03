
using System;
using System.Numerics;
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

public enum ViewportFitMode
{
    Stretch,
    Fit,
    Fill,
    IntegerFit,
    OneToOne,
    ExpandWidthKeepHeight,
    ExpandHeightKeepWidth,
}

public sealed class ContentViewport
{
    public Vector2Int SourceSize { get; set; }
    public ViewportFitMode FitMode { get; set; } = ViewportFitMode.Fit;
    public TextureFilter Filter { get; set; } = TextureFilter.Linear;
    public Vector2 Align { get; set; } = new(0.5f, 0.5f);

    public Vector2Int ResolveSourceSize(RectInt outputBounds)
    {
        int sw = Math.Max(1, SourceSize.X);
        int sh = Math.Max(1, SourceSize.Y);
        float ow = Math.Max(1, outputBounds.Width);
        float oh = Math.Max(1, outputBounds.Height);
        return FitMode switch
        {
            ViewportFitMode.ExpandWidthKeepHeight => new Vector2Int((int)MathF.Ceiling(sh * ow / oh), sh),
            ViewportFitMode.ExpandHeightKeepWidth => new Vector2Int(sw, (int)MathF.Ceiling(sw * oh / ow)),
            _ => new Vector2Int(sw, sh),
        };
    }

    public Rect Resolve(RectInt outputBounds)
    {
        float sw = Math.Max(1, SourceSize.X);
        float sh = Math.Max(1, SourceSize.Y);
        float ow = outputBounds.Width;
        float oh = outputBounds.Height;
        if (FitMode == ViewportFitMode.Stretch || FitMode == ViewportFitMode.ExpandWidthKeepHeight || FitMode == ViewportFitMode.ExpandHeightKeepWidth)
            return new Rect(outputBounds.X, outputBounds.Y, ow, oh);
        float scale = FitMode switch
        {
            ViewportFitMode.Fill => MathF.Max(ow / sw, oh / sh),
            ViewportFitMode.OneToOne => 1f,
            ViewportFitMode.IntegerFit => Calc.Min(ow / sw, oh / sh) >= 1f ? MathF.Floor(Calc.Min(ow / sw, oh / sh)) : Calc.Min(ow / sw, oh / sh),
            _ => Calc.Min(ow / sw, oh / sh),
        };
        float w = sw * scale;
        float h = sh * scale;
        float x = outputBounds.X + (ow - w) * Align.X;
        float y = outputBounds.Y + (oh - h) * Align.Y;
        return new Rect(x, y, w, h);
    }
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
    public ContentViewport? Viewport { get; set; }
    public Target? SourceTarget { get; set; }

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
            transform.EnsureWorldTransform();
            CameraUtils.UpdateCachedMatrices(ref camera, in transform);
            Batcher.PushMatrix(camera.worldToScreenMatrix);
            hasCameraMatrix = true;
        }

        try
        {
            RenderGroup.Update(tick);
            if (SourceTarget != null)
            {
                var viewport = Viewport;
                var dest = viewport?.Resolve(target.Bounds) ?? new Rect(0, 0, target.Width, target.Height);
                var center = new Vector2(dest.X + dest.Width / 2f, dest.Y + dest.Height / 2f);
                var origin = new Vector2(SourceTarget.Width, SourceTarget.Height) / 2f;
                var scale = new Vector2(dest.Width / SourceTarget.Width, dest.Height / SourceTarget.Height);
                Batcher.PushSampler(new(viewport?.Filter ?? TextureFilter.Linear, TextureWrap.Clamp, TextureWrap.Clamp));
                Batcher.Image(SourceTarget, center, origin, scale, 0, Color.White);
                Batcher.PopSampler();
            }
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