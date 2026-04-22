using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public class Ui2DraggableContainer : VContainer
{
    public HContainer Header { get; }
    public Ui2DragHandle Handle { get; }
    public UIText Title { get; }
    public VContainer Content { get; }

    Vector2 dragOffset;
    bool dragging;

    public Ui2DraggableContainer()
    {
        Gap = 4f;
        ClipChildren = true;
        BackgroundEnabled = true;

        var layout = Layout;
        layout.LayoutType = LayoutType.Absolute;
        layout.AlignX = HorizontalAlignment.Start;
        layout.AlignY = VerticalAlignment.Start;
        Layout = layout;

        Header = new HContainer
        {
            Gap = 4f
        };

        var headerLayout = Header.Layout;
        headerLayout.Width = 0f;
        headerLayout.Height = 24f;
        Header.Layout = headerLayout;

        Handle = new Ui2DragHandle(this)
            .WithSize(24f, 0f)
            .WithBackgroundColor(new Color(0.3f, 0.3f, 0.45f, 1f));

        Title = new UIText();
        Title.TextColor = Color.White;
        Title.TextSize = 14f;
        Title.Align = new Vector2(0f, 0.5f);

        Header.WithChildren(Handle, Title);

        Content = new VContainer
        {
            Gap = 4f
        };

        var contentLayout = Content.Layout;
        contentLayout.Width = 0f;
        contentLayout.Height = 0f;
        Content.Layout = contentLayout;

        var contentChildrenLayout = Content.ChildrenLayout;
        contentChildrenLayout.AlignX = HorizontalAlignment.Stretch;
        contentChildrenLayout.AlignY = VerticalAlignment.Start;
        Content.ChildrenLayout = contentChildrenLayout;

        AddChild(Header);
        AddChild(Content);

        Handle.DragStarted += HandleOnDragStarted;
        Handle.DragUpdated += HandleOnDragUpdated;
        Handle.DragEnded += HandleOnDragEnded;
    }

    void HandleOnDragStarted(Ui2DragHandle handle, Vector2 pos)
    {
        var rect = GetWorldRect();
        dragOffset = pos - new Vector2(rect.X, rect.Y);
        dragging = true;
    }

    void HandleOnDragUpdated(Ui2DragHandle handle, Vector2 pos)
    {
        if (!dragging)
            return;

        var topLeft = pos - dragOffset;
        var layout = Layout;
        layout.MarginLeft = topLeft.X;
        layout.MarginTop = topLeft.Y;
        Layout = layout;
    }

    void HandleOnDragEnded(Ui2DragHandle handle, Vector2 pos)
    {
        dragging = false;
    }

    public Ui2DraggableContainer WithTitle(string text)
    {
        Title.Text = text;
        return this;
    }
}
