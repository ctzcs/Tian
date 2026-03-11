
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Core.Graphics;

//某个相机渲染一次世界
//计算相机矩阵 → Push → 裁剪 → 绘制 → Pop
public class RenderPass
{
    /// <summary>
    /// 将改相机的视图渲染叠加到RenderContext的Target上
    /// </summary>
    public RenderContext Context { get; }
    /// <summary>
    /// 每个相机可能有不同的渲染的东西
    /// </summary>
    public SystemRoot RenderGroup { get; }
    public Entity CameraEntity { get; }
    public Color ClearColor { get; set; }

    public RenderPass(RenderContext context, SystemRoot renderGroup, Entity cameraEntity, Color clearColor)
    {
        Context = context;
        RenderGroup = renderGroup;
        CameraEntity = cameraEntity;
        ClearColor = clearColor;
    }

    public void Render(UpdateTick tick)
    {
        Context.Clear(ClearColor);
        RenderGroup.Update(tick);
        Context.Render();
    }
    
}




/*
//TODO 
- RenderSystem = RenderPass 执行器（读取 Camera + Batcher + Target）
- Before/AfterRenderWorldSystem 只保留结构或者直接移除
- Coordinate/Selectable/Debug 作为 RenderPass 中的一部分，绑定具体相机
*/