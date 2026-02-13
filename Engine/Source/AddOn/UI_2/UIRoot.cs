using System.Collections.Generic;
using System.Numerics;
using Engine.Core;
using Foster.Framework;

namespace Engine.UI_2;

public class UIRoot
{
    readonly App app;
    readonly Vector2Int logicResolution;
    readonly List<UICanvas> canvases = new();
    readonly Dictionary<string, UICanvas> canvasById = new();

    public IReadOnlyList<UICanvas> Canvases => canvases;

    public bool Enabled { get; set; } = true;

    public UIRoot(App app, Vector2Int logicResolution)
    {
        this.app = app;
        this.logicResolution = logicResolution;
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

    public void Update()
    {
        if (!Enabled)
            return;

        var viewport = new Rect(0, 0, logicResolution.X, logicResolution.Y);
        float time = (float)app.Time.Seconds;

        foreach (var canvas in canvases)
        {
            var canvasViewport = canvas.ClipRect ?? viewport;
            canvas.Layout(canvasViewport);
            canvas.Update(time);
        }

        var mouse = app.Input.Mouse;
        var pointer = Core.Input.Cursor.GetScreenPosition(logicResolution);

        foreach (var canvas in canvases)
            canvas.UpdateInput(pointer, mouse.LeftPressed, mouse.LeftReleased);
    }

    public void Render(Batcher batcher)
    {
        if (!Enabled)
            return;

        var viewport = new Rect(0, 0, logicResolution.X, logicResolution.Y);

        foreach (var canvas in canvases)
        {
            var canvasViewport = canvas.ClipRect ?? viewport;
            canvas.Render(batcher, canvasViewport);
        }
    }
}
