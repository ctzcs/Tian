using Engine.Core;
using Engine.Editor;
using Foster.Framework;
using Friflo.Engine.ECS;

namespace Editor;

public class EditorData:IDisposable
{
    public Renderer ImRenderer;
    public IContent? currentContent;
    public Entity selectedEntity = default;
    public App app;
    public EditorWindowManager? WindowManager;
    
    
    public void Dispose()
    {
        ImRenderer?.Dispose();
        ImRenderer = null;
        currentContent = null;
    }
}