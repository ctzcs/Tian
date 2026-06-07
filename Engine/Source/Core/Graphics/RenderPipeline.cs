using Foster.Framework;
using Friflo.Engine.ECS;

namespace Engine.Core.Graphics;

/// <summary>
/// RenderPipeline
/// 要么切换成不需要ECS,要么挪到ECS part
/// </summary>
public class RenderPipeline : IDisposable
{
    public GraphicsDevice GraphicsDevice { get; }
    public Target Target { get; private set; }
    public Color ClearColor { get; set; } = Color.Transparent;

    private readonly List<RenderPass> renderPasses = new();

    public RenderPipeline(GraphicsDevice graphicsDevice, int width, int height)
    {
        GraphicsDevice = graphicsDevice;
        Target = new Target(graphicsDevice, width, height);
    }

    public void AddPass(RenderPass renderPass)
    {
        if (renderPass == null)
            throw new ArgumentNullException(nameof(renderPass));
        renderPasses.Add(renderPass);
    }

    public bool RemovePass(RenderPass renderPass)
    {
        return renderPasses.Remove(renderPass);
    }

    public void ClearPasses()
    {
        renderPasses.Clear();
    }

    public void Resize(int width, int height)
    {
        Target.Dispose();
        Target = new Target(GraphicsDevice, width, height);
    }

    public void Render(UpdateTick tick)
    {
        if (renderPasses.Count == 0)
            return;

        Target.Clear(ClearColor);
        for (int i = 0; i < renderPasses.Count; i++)
            renderPasses[i].Render(tick, Target);
    }

    public void Dispose()
    {
        Target.Dispose();
    }
}