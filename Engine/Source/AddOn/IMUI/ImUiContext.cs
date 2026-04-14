using System.Collections.Generic;
using System.Numerics;
using Engine.Core;
using Foster.Framework;

namespace Engine.IMUI;

public sealed class ImUiContext
{
    private readonly App app;
    private Vector2Int outputResolution;
    private readonly List<ImUiDrawCommand> commands = new();
    private readonly Stack<uint> idStack = new();

    private WindowState currentWindow;
    private bool hasWindow;

    public ImUiStyle Style { get; } = new();
    public SpriteFont? Font { get; set; }
    public bool Enabled { get; set; } = true;
    public Rect? ViewportRect { get; set; }
    public float Scale { get; set; } = 1f;

    public Vector2 Pointer { get; private set; }
    public bool PointerDown { get; private set; }
    public bool PointerPressed { get; private set; }
    public bool PointerReleased { get; private set; }

    public uint HotId { get; private set; }
    public uint ActiveId { get; private set; }

    private struct WindowState
    {
        public uint Id;
        public Rect Rect;
        public Vector2 Cursor;
        public float ContentWidth;
        public int ItemIndex;
    }

    public ImUiContext(App app, Vector2Int outputResolution)
    {
        this.app = app;
        this.outputResolution = outputResolution;
    }

    public void OnResize(int width, int height)
    {
        outputResolution = new Vector2Int(width, height);
    }

    public void BeginFrame()
    {
        if (!Enabled)
            return;

        commands.Clear();
        hasWindow = false;
        HotId = 0;

        Pointer = GetPointerPosition();
        var mouse = app.Input.Mouse;
        PointerDown = mouse.LeftDown;
        PointerPressed = mouse.LeftPressed;
        PointerReleased = mouse.LeftReleased;

        if (PointerReleased && ActiveId != 0)
            ActiveId = 0;
    }

    public void EndFrame()
    {
    }

    public bool BeginWindow(string title, Rect rect)
    {
        if (!Enabled)
            return false;

        var id = HashId(title);
        currentWindow = new WindowState
        {
            Id = id,
            Rect = rect,
            Cursor = new Vector2(rect.X + Style.WindowPadding, rect.Y + Style.TitleBarHeight + Style.WindowPadding),
            ContentWidth = rect.Width - Style.WindowPadding * 2f,
            ItemIndex = 0
        };
        hasWindow = true;

        var titleRect = new Rect(rect.X, rect.Y, rect.Width, Style.TitleBarHeight);
        commands.Add(ImUiDrawCommand.MakeRect(rect, Style.WindowBg));
        commands.Add(ImUiDrawCommand.MakeRect(titleRect, Style.TitleBg));
        commands.Add(ImUiDrawCommand.MakeText(title, new Vector2(rect.X + Style.WindowPadding, rect.Y + 5f), Style.Text));

        return true;
    }

    public void EndWindow()
    {
        hasWindow = false;
    }

    public void PushID(int id)
    {
        idStack.Push(unchecked((uint)id));
    }

    public void PushID(string id)
    {
        idStack.Push(unchecked((uint)id.GetHashCode()));
    }

    public void PopID()
    {
        if (idStack.Count > 0)
            idStack.Pop();
    }

    public void Label(string text, Color? color = null)
    {
        if (!hasWindow)
            return;

        var pos = currentWindow.Cursor;
        commands.Add(ImUiDrawCommand.MakeText(text, pos, color ?? Style.Text));
        AdvanceCursor(Style.FontSize + 4f);
    }

    public bool Button(string text, float width = 0f, float height = -1f)
    {
        if (!hasWindow)
            return false;

        var w = width <= 0f ? currentWindow.ContentWidth : width;
        var h = height <= 0f ? Style.ButtonHeight : height;
        var rect = new Rect(currentWindow.Cursor.X, currentWindow.Cursor.Y, w, h);

        var id = NextItemId("button", text);
        var hovered = rect.Contains(Pointer);

        if (hovered)
            HotId = id;

        if (PointerPressed && hovered)
            ActiveId = id;

        var clicked = PointerReleased && hovered && ActiveId == id;

        var color = Style.Button;
        if (ActiveId == id && PointerDown)
            color = Style.ButtonActive;
        else if (hovered)
            color = Style.ButtonHover;

        commands.Add(ImUiDrawCommand.MakeRect(rect, color));
        commands.Add(ImUiDrawCommand.MakeText(text, new Vector2(rect.X + 10f, rect.Y + (h - Style.FontSize) * 0.5f), Style.Text));

        AdvanceCursor(h + Style.ItemSpacing);
        return clicked;
    }

    public void Render(Batcher batcher)
    {
        if (!Enabled)
            return;

        for (int i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            if (cmd.Kind == ImUiDrawKind.Rect)
            {
                batcher.Rect(cmd.Rect, cmd.Color);
                continue;
            }

            if (Font != null)
                batcher.Text(Font, cmd.Text, cmd.Position, cmd.Color);
            else
                batcher.Text(cmd.Text, cmd.Position, Style.FontSize, cmd.Color);
        }
    }

    private void AdvanceCursor(float y)
    {
        currentWindow.Cursor = new Vector2(currentWindow.Cursor.X, currentWindow.Cursor.Y + y);
    }

    private uint NextItemId(string kind, string label)
    {
        var id = HashId($"{kind}:{label}:{currentWindow.ItemIndex}");
        currentWindow.ItemIndex++;
        return id;
    }

    private uint HashId(string label)
    {
        var hash = new HashCode();
        hash.Add(currentWindow.Id);
        foreach (var id in idStack)
            hash.Add(id);
        hash.Add(label);
        return unchecked((uint)hash.ToHashCode());
    }

    private Rect GetOutputViewport() => ViewportRect ?? new Rect(0, 0, outputResolution.X, outputResolution.Y);

    private Rect GetLayoutViewport()
    {
        var outputViewport = GetOutputViewport();
        var s = Scale <= 0f ? 1f : Scale;
        return new Rect(outputViewport.X, outputViewport.Y, outputViewport.Width / s, outputViewport.Height / s);
    }

    private Vector2 GetPointerPosition()
    {
        var pointer = Engine.Core.Input.Cursor.GetScreenPosition(outputResolution);
        var outputViewport = GetOutputViewport();
        if (!outputViewport.Contains(pointer) || outputViewport.Width <= 0f || outputViewport.Height <= 0f)
            return new Vector2(-1f, -1f);

        var layoutViewport = GetLayoutViewport();
        return new Vector2(
            layoutViewport.X + (pointer.X - outputViewport.X) / outputViewport.Width * layoutViewport.Width,
            layoutViewport.Y + (pointer.Y - outputViewport.Y) / outputViewport.Height * layoutViewport.Height);
    }
}