using System.Diagnostics;
using Friflo.Engine.ECS;

namespace Engine.Components;

public struct FrameCounter:IComponent
{
    public int FPS;
    public int Frames;
    public Stopwatch sw; // = Stopwatch.StartNew
}

public struct RenderBatchStats:IComponent
{
    public int BatchCount;
}


/*/// <summary>
///     Simple utility to count frames in last second
/// </summary>
public class FrameCounter
{
    public int FPS;
    public int Frames;
    public Stopwatch sw = Stopwatch.StartNew();

    public void Update()
    {
        Frames++;
        var elapsed = sw.Elapsed.TotalSeconds;
        if (elapsed > 1)
        {
            sw.Restart();
            FPS = Frames;
            Frames = 0;
        }
    }
}*/