using System;
using ImGuiNET;

namespace Editor;

public abstract class EditorWindow : IEditorWindow
{
    public bool IsOpen { get; set; }
    
    protected EditorData? Data { get; set; }
    
    public virtual void Update(){}
    
    public virtual void Render(){}
    
    void IEditorWindow.OnAddWindow(EditorData data)
    {
        Data = data;
        OnAddWindow();
    }
    
    protected virtual void OnAddWindow(){}
    
}

public interface IEditorWindow
{
    void OnAddWindow(EditorData data);
}

public sealed class CallbackEditorWindow : EditorWindow
{
    private readonly string _title;
    private readonly Action _draw;

    public CallbackEditorWindow(string title, Action draw)
    {
        _title = title;
        _draw = draw;
        IsOpen = true;
    }

    public override void Update()
    {
        if (ImGui.Begin(_title))
            _draw();

        ImGui.End();
    }
}