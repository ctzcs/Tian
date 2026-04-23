using System;
using System.Numerics;
using Engine.Core;
using Engine.UI_2;
using Engine.Utility;
using Foster.Framework;
using ImGuiNET;

namespace Editor;

public sealed class UI2DebugWindow : EditorWindow
{
    private Batcher? batcher;
    private readonly UIDebugger fallbackDebugger = new();
    private UIElement? selectedElement;
    private string selectedCanvasName = string.Empty;
    private bool showUiTree = true;
    private bool showLayout = true;
    private bool showOnlyVisible = false;

    protected override void OnAddWindow()
    {
        base.OnAddWindow();
        batcher = new Batcher(Data!.app.GraphicsDevice);
        IsOpen = true;
    }

    public override void Update()
    {
        if (ImGui.Begin("UI2 Debug"))
        {
            var content = Data?.currentContent;
            if (content == null)
            {
                ImGui.Text("Current Content is null");
            }
            else
            {
                var uiRoot = ReflectionUtility.TryGetObject<UIRoot>(content);
                if (uiRoot == null)
                {
                    ImGui.Text("No UIRoot found on current content");
                }
                else
                {
                    var reflectedDebugger = ReflectionUtility.TryGetObject<UIDebugger>(content);
                    var debugger = reflectedDebugger ?? fallbackDebugger;

                    var enabled = debugger.Enabled;
                    if (ImGui.Checkbox("Enable UIDebug", ref enabled))
                        debugger.Enabled = enabled;

                    ImGui.Checkbox("Show UI Tree", ref showUiTree);
                    ImGui.Checkbox("Show Layout", ref showLayout);
                    ImGui.Checkbox("Only Visible", ref showOnlyVisible);

                    ImGui.Text($"Scale: {uiRoot.Scale:0.00}x");
                    ImGui.Text($"Canvas Count: {uiRoot.Canvases.Count}");
                    ImGui.Text(reflectedDebugger != null ? "Debugger Source: Content" : "Debugger Source: Editor");
                    SyncSelectedElement(uiRoot, debugger);
                    DrawSelectedInfo();
                    DrawHoveredInfo(uiRoot);

                    if (showUiTree)
                    {
                        ImGui.Separator();

                        for (int i = 0; i < uiRoot.Canvases.Count; i++)
                        {
                            var canvas = uiRoot.Canvases[i];
                            var title = string.IsNullOrWhiteSpace(canvas.Id) ? $"Canvas {i}" : canvas.Id!;
                            var canvasOpened = ImGui.TreeNodeEx($"{title}##canvas-{i}", ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth);
                            if (!canvasOpened)
                                continue;

                            DrawElementNode(canvas.Root, canvas.DebugHovered, title, "Root", $"canvas-{i}-root");
                            ImGui.TreePop();
                        }
                    }
                }
            }
        }

        ImGui.End();
    }

    public override void Render()
    {
        var content = Data?.currentContent;
        if (content == null || batcher == null)
            return;

        var uiRoot = ReflectionUtility.TryGetObject<UIRoot>(content);
        if (uiRoot == null)
            return;

        var reflectedDebugger = ReflectionUtility.TryGetObject<UIDebugger>(content);
        if (reflectedDebugger != null)
            return;

        if (!fallbackDebugger.Enabled)
            return;

        var target = content.Target;
        if (target == null || target.IsDisposed)
            return;

        uiRoot.RenderDebug(batcher, fallbackDebugger);

        batcher.Render(target);
        batcher.Clear();
    }

    private void DrawElementNode(UIElement element, UIElement? hovered, string canvasName, string label, string path)
    {
        if (showOnlyVisible && (!element.Visible || !element.Display))
            return;

        var rect = element.GetWorldRect();
        var text = $"{label}: {element.GetType().Name}";

        if (element == hovered)
            text += "  [Hovered]";
        if (element == selectedElement)
            text += "  [Selected]";
        if (!element.Visible)
            text += "  [Hidden]";
        if (!element.Display)
            text += "  [NoDisplay]";
        if (element.Interactable)
            text += "  [Interactable]";
        if (element.UserData != null)
            text += $"  User:{element.UserData}";
        if (showLayout)
            text += $"  [{rect.X:0},{rect.Y:0},{rect.Width:0}x{rect.Height:0}]";

        if (element == hovered)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.9f, 0.2f, 1f));

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (element.Children.Count == 0)
            flags |= ImGuiTreeNodeFlags.Leaf;
        if (element == selectedElement)
            flags |= ImGuiTreeNodeFlags.Selected;

        var opened = ImGui.TreeNodeEx($"{text}##{path}", flags);
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
        {
            selectedElement = element;
            selectedCanvasName = canvasName;
        }

        if (element == hovered)
            ImGui.PopStyleColor();

        if (!opened)
            return;

        for (int i = 0; i < element.Children.Count; i++)
            DrawElementNode(element.Children[i], hovered, canvasName, $"Child {i}", $"{path}-{i}");

        ImGui.TreePop();
    }

    private void DrawSelectedInfo()
    {
        ImGui.Separator();
        ImGui.Text("Selected");
        if (selectedElement == null)
        {
            ImGui.Text("Selected: None");
            return;
        }

        if (ImGui.Button("Clear Selection"))
        {
            selectedElement = null;
            selectedCanvasName = string.Empty;
            return;
        }

        DrawElementInfo(selectedElement, selectedCanvasName, "Selected");
    }

    private static void DrawHoveredInfo(UIRoot uiRoot)
    {
        UIElement? hovered = null;
        string canvasName = string.Empty;

        for (int i = uiRoot.Canvases.Count - 1; i >= 0; i--)
        {
            var canvas = uiRoot.Canvases[i];
            if (canvas.DebugHovered == null)
                continue;

            hovered = canvas.DebugHovered;
            canvasName = string.IsNullOrWhiteSpace(canvas.Id) ? $"Canvas {i}" : canvas.Id!;
            break;
        }

        ImGui.Separator();
        if (hovered == null)
        {
            ImGui.Text("Hovered: None");
            return;
        }

        DrawElementInfo(hovered, canvasName, "Hovered");
    }

    private void SyncSelectedElement(UIRoot uiRoot, UIDebugger debugger)
    {
        if (selectedElement != null && !ContainsElement(uiRoot, selectedElement))
        {
            selectedElement = null;
            selectedCanvasName = string.Empty;
        }

        debugger.SelectedElement = selectedElement;
    }

    private static bool ContainsElement(UIRoot uiRoot, UIElement target)
    {
        for (int i = 0; i < uiRoot.Canvases.Count; i++)
        {
            if (ContainsElement(uiRoot.Canvases[i].Root, target))
                return true;
        }

        return false;
    }

    private static bool ContainsElement(UIElement element, UIElement target)
    {
        if (ReferenceEquals(element, target))
            return true;

        for (int i = 0; i < element.Children.Count; i++)
        {
            if (ContainsElement(element.Children[i], target))
                return true;
        }

        return false;
    }

    private static void DrawElementInfo(UIElement element, string canvasName, string prefix)
    {
        var rect = element.GetWorldRect();
        var layout = element.Layout;
        var childrenLayout = element.ChildrenLayout;
        ImGui.Text($"{prefix}: {element.GetType().Name}");
        ImGui.Text($"Canvas: {canvasName}");
        ImGui.Text($"Rect: {rect.X:0}, {rect.Y:0}, {rect.Width:0} x {rect.Height:0}");
        ImGui.Text($"Interactable: {(element.Interactable ? "true" : "false")}");
        ImGui.Text($"Visible: {(element.Visible ? "true" : "false")}    Display: {(element.Display ? "true" : "false")}");
        ImGui.Text($"UserData: {element.UserData ?? "null"}");
        ImGui.Text($"Grow: {layout.Grow:0.##}");
        ImGui.Text($"Min: {layout.MinWidth:0}, {layout.MinHeight:0}    Max: {layout.MaxWidth:0}, {layout.MaxHeight:0}");
        ImGui.Text($"Padding: {childrenLayout.PaddingLeft:0}, {childrenLayout.PaddingTop:0}, {childrenLayout.PaddingRight:0}, {childrenLayout.PaddingBottom:0}");
    }
}
