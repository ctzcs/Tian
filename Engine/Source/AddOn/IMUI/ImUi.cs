using Foster.Framework;

namespace Engine.IMUI;

public static class ImUi
{
    public static ImUiContext? Current { get; private set; }

    public static void SetCurrent(ImUiContext context)
    {
        Current = context;
    }

    public static void BeginFrame()
    {
        Current?.BeginFrame();
    }

    public static void EndFrame()
    {
        Current?.EndFrame();
    }

    public static bool BeginWindow(string title, Rect rect)
    {
        return Current != null && Current.BeginWindow(title, rect);
    }

    public static void EndWindow()
    {
        Current?.EndWindow();
    }

    public static void Label(string text)
    {
        Current?.Label(text);
    }

    public static bool Button(string text, float width = 0f, float height = -1f)
    {
        return Current != null && Current.Button(text, width, height);
    }

    public static void Render(Batcher batcher)
    {
        Current?.Render(batcher);
    }
}