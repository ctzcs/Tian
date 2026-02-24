namespace Editor;

public abstract class EditorWindow:IEditorWindow
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