using System.Collections.Generic;
using System.Numerics;
using Engine.Core;
using Foster.Framework;

namespace Engine.UI_2;

public class UIRoot
{
    readonly App app;
    Vector2Int logicResolution;
    readonly List<UICanvas> canvases = new();
    readonly Dictionary<string, UICanvas> canvasById = new();

    public Vector2Int ReferenceResolution { get; set; }
    public Rect? ViewportRect { get; set; }

    public IReadOnlyList<UICanvas> Canvases => canvases;

    public bool Enabled { get; set; } = true;

    public UIRoot(App app, Vector2Int logicResolution)
    {
        this.app = app;
        this.logicResolution = logicResolution;
        ReferenceResolution = logicResolution;
    }

    public UICanvas CreateCanvas()
    {
        var canvas = new UICanvas();
        AddCanvas(canvas);
        return canvas;
    }

    public UICanvas CreateCanvas(string id)
    {
        var canvas = new UICanvas
        {
            Id = id
        };
        AddCanvas(canvas);
        return canvas;
    }

    public void AddCanvas(UICanvas canvas)
    {
        if (!canvases.Contains(canvas))
            canvases.Add(canvas);

        if (!string.IsNullOrEmpty(canvas.Id))
            canvasById[canvas.Id] = canvas;
    }

    public void RemoveCanvas(UICanvas canvas)
    {
        canvases.Remove(canvas);

        if (!string.IsNullOrEmpty(canvas.Id) &&
            canvasById.TryGetValue(canvas.Id, out var existing) &&
            ReferenceEquals(existing, canvas))
        {
            canvasById.Remove(canvas.Id);
        }
    }

    public UICanvas? GetCanvas(string id)
    {
        if (canvasById.TryGetValue(id, out var canvas))
            return canvas;
        return null;
    }

    Rect GetLayoutViewport() => new Rect(0, 0, ReferenceResolution.X, ReferenceResolution.Y);

    Rect GetOutputViewport() => ViewportRect ?? new Rect(0, 0, logicResolution.X, logicResolution.Y);

    Vector2 GetPointerPosition()
    {
        var pointer = Core.Input.Cursor.GetScreenPosition(logicResolution);
        var outputViewport = GetOutputViewport();
        if (!outputViewport.Contains(pointer) || outputViewport.Width <= 0f || outputViewport.Height <= 0f)
            return new Vector2(-1f, -1f);

        var layoutViewport = GetLayoutViewport();
        return new Vector2(
            (pointer.X - outputViewport.X) / outputViewport.Width * layoutViewport.Width,
            (pointer.Y - outputViewport.Y) / outputViewport.Height * layoutViewport.Height);
    }

    public void Update()
    {
        if (!Enabled)
            return;

        var viewport = GetLayoutViewport();
        float time = (float)app.Time.Seconds;

        foreach (var canvas in canvases)
        {
            var canvasViewport = canvas.ClipRect ?? viewport;
            canvas.Layout(canvasViewport);
            canvas.Update(time);
        }

        var mouse = app.Input.Mouse;
        var pointer = GetPointerPosition();

        foreach (var canvas in canvases)
            canvas.UpdateInput(pointer, mouse.LeftPressed, mouse.LeftReleased);
    }

    public void Render(Batcher batcher)
    {
        if (!Enabled)
            return;

        var viewport = GetLayoutViewport();
        var outputViewport = GetOutputViewport();

        foreach (var canvas in canvases)
        {
            var canvasViewport = canvas.ClipRect ?? viewport;
            canvas.Render(batcher, canvasViewport, outputViewport);
        }
    }
    

    public void OnResize(int width, int height)
    {
        logicResolution = new Vector2Int(width, height);
    }
}
