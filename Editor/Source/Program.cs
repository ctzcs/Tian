

namespace Editor;

public class Program
{
    public static void Main(string[] args)
    {
        using var editor = new EditorApp();
        editor.Run();
    }
}