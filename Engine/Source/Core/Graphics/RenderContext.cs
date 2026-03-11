using Foster.Framework;

namespace Engine.Core.Graphics;
// 得到渲染的输出目标
// 一个视图一个RenderContext （多目标）
// 一个RenderContext 多个RenderPass (多相机同目标)
public class RenderContext : IDisposable
{
    public GraphicsDevice GraphicsDevice { get; }
    public Target Target { get; private set; }
    public Batcher Batcher { get; }

    public RenderContext(GraphicsDevice graphicsDevice, int width, int height)
    {
        GraphicsDevice = graphicsDevice;
        Target = new Target(graphicsDevice, width, height);
        Batcher = new Batcher(graphicsDevice);
    }

    public void Resize(int width, int height)
    {
        Target.Dispose();
        Target = new Target(GraphicsDevice, width, height);
    }

    public void Clear(Color color)
    {
        Target.Clear(color);
    }

    public void Render()
    {
        Batcher.Render(Target);
        Batcher.Clear();
    }

    public void Dispose()
    {
        Batcher.Dispose();
        Target.Dispose();
    }
}