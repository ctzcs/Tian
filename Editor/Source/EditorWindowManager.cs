using ImGuiNET;

namespace Editor;

public class EditorWindowManager(EditorData data)
{
    private readonly List<EditorWindow> windows = new List<EditorWindow>();

    public void Update()
    {
        var snapshot = windows.ToArray();
        foreach (var window in snapshot)
        {
            if (windows.Contains(window) && window.IsOpen)
                window.Update();
        }
    }

    public void Render()
    {
        var snapshot = windows.ToArray();
        foreach (var window in snapshot)
        {
            if (windows.Contains(window) && window.IsOpen)
                window.Render();
        }
    }
    
    
    
    public void AddWindow(EditorWindow window){
        windows.Add(window);
        (window as IEditorWindow).OnAddWindow(data);
    }


    public void SwitchWindowVisual<T>()
    {
        var snapshot = windows.ToArray();
        foreach (var window in snapshot)
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
