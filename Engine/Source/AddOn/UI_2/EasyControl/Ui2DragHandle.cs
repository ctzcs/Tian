using System;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public class Ui2DragHandle : UIElement
{
    public UIElement Target { get; }

    public bool Dragging { get; private set; }

    public event Action<Ui2DragHandle, Vector2>? DragStarted;
    public event Action<Ui2DragHandle, Vector2>? DragUpdated;
    public event Action<Ui2DragHandle, Vector2>? DragEnded;

    Vector2 startPosition;

    public Ui2DragHandle(UIElement target)
    {
        Target = target;
        Interactable = true;

        OnPointerDown += HandlePointerDown;
        OnPointerMove += HandlePointerMove;
        OnPointerUp += HandlePointerUp;
    }

    void HandlePointerDown(Ui2PointerEvent e)
    {
        var wr = Target.GetWorldRect();
        startPosition = new Vector2(wr.X, wr.Y);
        Dragging = true;
        DragStarted?.Invoke(this, e.Position);
    }

    void HandlePointerMove(Ui2PointerEvent e)
    {
        if (!Dragging)
            return;

        DragUpdated?.Invoke(this, e.Position);
    }

    void HandlePointerUp(Ui2PointerEvent e)
    {
        if (!Dragging)
            return;

        Dragging = false;
        DragEnded?.Invoke(this, e.Position);
    }
}

public class Ui2ReorderableColumn : ColumnGroup
{
    UIElement draggingItem;
    UIElement dragPlaceholder;
    UIElement dragGhost;
    float ghostWidth;
    float ghostHeight;
    Vector2 dragOffsetLocal = Vector2.Zero;
    int originalIndex = -1;
    bool dragStarted;
    bool dragVisualActive;
    Vector2 dragStartWorld = Vector2.Zero;

    public Ui2ReorderableColumn()
    {
        Gap = 2f;
    }

    int FindIndex(UIElement item)
    {
        return Children.IndexOf(item);
    }

    void UpdateOrder(Vector2 pointerWorld)
    {
        if (dragPlaceholder == null)
            return;

        var local = WorldToLocal(pointerWorld);
        float y = local.Y;

        int insertIndex = 0;
        int count = Children.Count;

        for (int i = 0; i < count; i++)
        {
            var child = Children[i];
            var rect = child.LayoutRect;
            float mid = rect.Y + rect.Height * 0.5f;
            if (y > mid)
                insertIndex = i + 1;
        }

        int currentIndex = FindIndex(dragPlaceholder);
        if (currentIndex < 0)
            return;

        if (insertIndex == currentIndex || insertIndex == currentIndex + 1)
            return;

        Children.RemoveAt(currentIndex);

        if (insertIndex > currentIndex)
            insertIndex--;

        if (insertIndex < 0)
            insertIndex = 0;
        if (insertIndex > Children.Count)
            insertIndex = Children.Count;

        Children.Insert(insertIndex, dragPlaceholder);
    }

    public void AddItem(string text)
    {
        var row = new RowGroup
        {
            Gap = 4f
        }
        .WithSize(0f, 20f)
        .WithBackgroundColor(new Color(0.18f, 0.2f, 0.26f, 1f));

        var handle = new Ui2DragHandle(row)
            .WithSize(16f, 0f)
            .WithBackgroundColor(new Color(0.3f, 0.3f, 0.45f, 1f));

        var label = new UIText()
            .WithText(text)
            .WithTextColor(Color.White)
            .WithTextSize(12f)
            .WithTextAlign(new Vector2(0f, 0.5f));

        row.WithChildren(handle, label);
        AddChild(row);

        handle.DragStarted += (h, pos) =>
        {
            if (dragGhost != null)
            {
                var parent = dragGhost.Parent;
                if (parent != null)
                    parent.RemoveChild(dragGhost);
                dragGhost = null;
            }

            if (dragPlaceholder != null)
            {
                int placeholderIndex = FindIndex(dragPlaceholder);
                if (placeholderIndex >= 0)
                    Children.RemoveAt(placeholderIndex);
                dragPlaceholder = null;
            }

            draggingItem = row;
            dragStarted = true;
            dragVisualActive = false;
            dragStartWorld = pos;

            originalIndex = FindIndex(row);
            if (originalIndex < 0)
                originalIndex = 0;
        };

        handle.DragUpdated += (h, pos) =>
        {
            if (!dragStarted)
                return;

            float distance = (pos - dragStartWorld).Length();
            const float activateThreshold = 3f;

            if (!dragVisualActive)
            {
                if (distance < activateThreshold)
                    return;

                dragVisualActive = true;

                var worldRect = row.GetWorldRect();
                ghostWidth = worldRect.Width;
                ghostHeight = worldRect.Height;

                var listLocalPos = WorldToLocal(pos);
                var rowLocalRect = row.LayoutRect;
                dragOffsetLocal = listLocalPos - new Vector2(rowLocalRect.X, rowLocalRect.Y);

                RemoveChild(row);

                dragPlaceholder = new UIElement()
                    .WithSize(ghostWidth, ghostHeight);

                Children.Insert(originalIndex, dragPlaceholder);

                var ghostRow = new RowGroup
                {
                    Gap = row.Gap
                }
                .WithSize(ghostWidth, ghostHeight)
                .WithBackgroundColor(row.BackgroundColor);

                var ghostLabel = new UIText()
                    .WithText(text)
                    .WithTextColor(Color.White)
                    .WithTextSize(12f)
                    .WithTextAlign(new Vector2(0f, 0.5f));

                ghostRow.WithChild(ghostLabel);
                ghostRow.AnimateLayout = false;
                ghostRow.Interactable = false;
                ghostRow.PointerPassThrough = true;

                var ghostLayout = ghostRow.Layout;
                ghostLayout.LayoutType = LayoutType.Absolute;
                ghostLayout.AlignX = HorizontalAlignment.Start;
                ghostLayout.AlignY = VerticalAlignment.Start;
                ghostLayout.Width = ghostWidth;
                ghostLayout.Height = ghostHeight;

                var ghostLocalTopLeft = listLocalPos - dragOffsetLocal;
                var ghostWorldTopLeft = LocalToWorld(ghostLocalTopLeft);
                ghostLayout.MarginLeft = ghostWorldTopLeft.X;
                ghostLayout.MarginTop = ghostWorldTopLeft.Y;

                ghostRow.Layout = ghostLayout;

                dragGhost = ghostRow;

                UIElement root = this;
                while (root.Parent != null)
                    root = root.Parent;
                root.AddChild(dragGhost);
            }

            if (dragGhost != null)
            {
                var listLocalPos = WorldToLocal(pos);
                var ghostLocalTopLeft = listLocalPos - dragOffsetLocal;
                var ghostWorldTopLeft = LocalToWorld(ghostLocalTopLeft);

                var ghostLayout = dragGhost.Layout;
                ghostLayout.MarginLeft = ghostWorldTopLeft.X;
                ghostLayout.MarginTop = ghostWorldTopLeft.Y;
                dragGhost.Layout = ghostLayout;
            }

            UpdateOrder(pos);
        };

        handle.DragEnded += (h, pos) =>
        {
            if (!dragStarted)
                return;

            if (dragVisualActive)
            {
                if (dragGhost != null && draggingItem != null)
                {
                    var ghostRect = dragGhost.GetWorldRect();
                    var ghostTopLeft = new Vector2(ghostRect.X, ghostRect.Y);
                    var listLocalTopLeft = WorldToLocal(ghostTopLeft);
                    var animRect = new Rect(listLocalTopLeft.X, listLocalTopLeft.Y, ghostRect.Width, ghostRect.Height);
                    draggingItem.AnimateLayout = true;
                    draggingItem.ForceLayoutRect(animRect);
                }

                if (dragGhost != null)
                {
                    var parent = dragGhost.Parent;
                    if (parent != null)
                        parent.RemoveChild(dragGhost);
                    dragGhost = null;
                }

                if (dragPlaceholder != null && draggingItem != null)
                {
                    int placeholderIndex = FindIndex(dragPlaceholder);
                    Children.Remove(dragPlaceholder);
                    dragPlaceholder = null;

                    int insertIndex = placeholderIndex;
                    if (insertIndex < 0)
                        insertIndex = originalIndex;
                    if (insertIndex < 0)
                        insertIndex = 0;
                    if (insertIndex > Children.Count)
                        insertIndex = Children.Count;

                    Children.Insert(insertIndex, draggingItem);
                }
            }

            draggingItem = null;
            originalIndex = -1;
            dragStarted = false;
            dragVisualActive = false;
        };
    }
}
