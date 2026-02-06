using ImGuiNET;

namespace Editor;

public class EditorWindowManager(EditorData data)
{
    private readonly List<EditorWindow> windows = new List<EditorWindow>();

    public void Update()
    {
        foreach (var window in windows)
        {
            if (window.IsOpen)
                window.Update();
        }
    }

    public void Render()
    {
        foreach (var window in windows)
        {
            if (window.IsOpen)
                window.Render();
        }
    }
    
    
    
    public void AddWindow(EditorWindow window){
        windows.Add(window);
        (window as IEditorWindow).OnAddWindow(data);
    }


    public void SwitchWindowVisual<T>()
    {
        foreach (var window in windows)
        {
            if (window is T)
                window.IsOpen=!window.IsOpen;
        }
    }

    public void RemoveWindow(EditorWindow window)
    {
        windows.Remove(window);
    }
}