using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Components;
using Engine.Core;
using Foster.Framework;

namespace Engine.UI_2;

public class UIRoot
{
    readonly App app;
    /// <summary>
    /// 用来获取鼠标位置
    /// </summary>
    Vector2Int outputResolution;
    readonly List<UICanvas> canvases = new();
    readonly Dictionary<string, UICanvas> canvasById = new();
    /// <summary>
    /// 布局的窗口，比如GameViewRect，去掉黑边
    /// </summary>
    public Rect? ViewportRect { get; set; }

    public IReadOnlyList<UICanvas> Canvases => canvases;

    public bool Enabled { get; set; } = true;
    public Action? OnUpdateLayout;

    public UIRoot(App app, Vector2Int outputResolution)
    {
        this.app = app;
        this.outputResolution = outputResolution;
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

    Rect GetLayoutViewport() => ViewportRect ?? new Rect(0, 0, outputResolution.X, outputResolution.Y);

    Rect GetOutputViewport() => GetLayoutViewport();

    Vector2 GetPointerPosition()
    {
        var pointer = Core.Input.Cursor.GetScreenPosition(outputResolution);
        var layoutViewport = GetLayoutViewport();
        if (!layoutViewport.Contains(pointer) || layoutViewport.Width <= 0f || layoutViewport.Height <= 0f)
            return new Vector2(-1f, -1f);

        return pointer;
    }

    public bool TryWorldToUiPx(Vector2 worldPosition, in CTransform cameraTransform, in Camera2D camera, out Vector2 uiPosition)
    {
        var viewport = GetLayoutViewport();
        var normalized = CameraUtils.WorldToViewport(worldPosition, in cameraTransform, in camera);
        if (normalized.X < 0f || normalized.X > 1f || normalized.Y < 0f || normalized.Y > 1f)
        {
            uiPosition = default;
            return false;
        }

        uiPosition = new Vector2(
            viewport.X + normalized.X * viewport.Width,
            viewport.Y + normalized.Y * viewport.Height);
        return true;
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

        OnUpdateLayout?.Invoke();

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
        outputResolution = new Vector2Int(width, height);
    }
}
