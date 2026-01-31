using System;
using System.Numerics;
using Rect = Foster.Framework.Rect;
namespace Engine.UI;

/// <summary>
/// 拖拽手柄（Handle）。
/// </summary>
/// <remarks>
/// 用于将某个 <see cref="UIElement"/>（通常是一项/元素）在同一个 <see cref="UILayoutGroup"/> 内进行拖拽重排。
/// 该类型本身只负责把输入事件转发给 <see cref="UiDragController"/>；重排逻辑由 Controller 统一管理。
/// </remarks>
public sealed class UiDragHandle(Rect rect, UiDragController controller, UIElement item)
    : UIElement(true, true, true, rect), IInputListener
{
    private bool dragging;

    public bool OnPointerDown(UiFrame state)
    {
        dragging = controller.BeginDrag(item, state.targetPosition);
        return dragging;
    }

    public bool OnRightPointerDown(UiFrame state) => false;

    public void OnPointerMoved(UiFrame state)
    {
        if (!dragging)
            return;

        controller.UpdateDrag(state.targetPosition);
    }

    public void OnPointerUp(UiFrame state)
    {
        if (!dragging)
            return;

        dragging = false;
        controller.EndDrag();
    }
}

/// <summary>
/// Item本体移动版
/// </summary>
/// <param name="rect"></param>
/// <param name="controller"></param>
public class UiDragItem : UIElement, IInputListener
{
    protected readonly UiDragController controller;
    private bool dragging;

    public UiDragItem(Rect rect, UiDragController controller)
        : base(true, true, true, rect)
    {
        this.controller = controller;
    }

    public bool OnPointerDown(UiFrame state)
    {
        dragging = controller.BeginDrag(this, state.targetPosition);
        return dragging;
    }

    public bool OnRightPointerDown(UiFrame state) => false;

    public void OnPointerMoved(UiFrame state)
    {
        if (!dragging)
            return;

        controller.UpdateDrag(state.targetPosition);
    }

    public void OnPointerUp(UiFrame state)
    {
        if (!dragging)
            return;

        dragging = false;
        controller.EndDrag();
    }

    protected override UIElement CreateCloneInstance()
    {
        return new UiDragItem(rect, controller);
    }
}

/// <summary>
/// 携带数据的本体移动版
/// </summary>
/// <param name="rect"></param>
/// <param name="controller"></param>
/// <typeparam name="T"></typeparam>
public sealed class UiDragItem<T> : UiDragItem
{
    public T BindData;

    public UiDragItem(Rect rect, UiDragController controller)
        : base(rect, controller)
    {
    }

    public UiDragItem<T> WithData(T data)
    {
        BindData = data;
        return this;
    }

    protected override UIElement CreateCloneInstance()
    {
        return new UiDragItem<T>(rect, controller);
    }

    protected override void CopyToClone(UIElement target, bool cloneChildren)
    {
        base.CopyToClone(target, cloneChildren);

        if (target is UiDragItem<T> item)
            item.BindData = BindData;
    }
}

/// <summary>
/// 容器内拖拽重排控制器：支持将同一个 <see cref="UILayoutGroup"/> 中的子元素按指针位置拖拽并重新插入。
/// </summary>
/// <remarks>
/// 用法（典型）：
/// <list type="number">
/// <item><description>为目标 <see cref="UILayoutGroup"/> 创建一个 <see cref="UiDragController"/>。</description></item>
/// <item><description>为每个可拖拽的 row 添加一个 <see cref="UiDragHandle"/>（或在你自己的 IInputListener 里调用 BeginDrag/UpdateDrag/EndDrag）。</description></item>
/// <item><description>订阅 <see cref="Reordered"/> 以在发生实际换位时更新数据源。</description></item>
/// </list>
/// Controller 在拖拽期间会：
/// <list type="bullet">
/// <item><description>从 group 中临时移除被拖拽的 row，并放入一个占位元素（placeholder）。</description></item>
/// <item><description>在 UI 根节点下创建一个不可交互的视觉克隆（ghost）跟随指针移动。</description></item>
/// <item><description>根据指针位置更新 placeholder 在 group.Children 中的插入索引。</description></item>
/// </list>
/// </remarks>
public sealed class UiDragController
{
    private readonly UIRoot uiRoot;
    private UILayoutGroup group;

    private UIElement? dragItem;
    private UIElement? dragGhost;
    private UIElement? placeholder;
    private Vector2 grabOffset;
    private int startIndex = -1;
    private int placeholderIndex = -1;

    /// <summary>
    /// 当前是否处于拖拽中。
    /// </summary>
    public bool IsDragging => dragItem != null;

    /// <summary>
    /// 当拖拽结束且位置发生变化时触发。
    /// </summary>
    /// <remarks>
    /// 参数依次为：item、原始索引（from）、目标索引（to）。
    /// 只有 from 与 to 都有效且不相等时才会触发。
    /// </remarks>
    public Action<UIElement, int, int>? Reordered;

    /// <summary>
    /// 创建一个绑定到指定 UI 根节点与目标布局容器的重排控制器。
    /// </summary>
    /// <param name="uiRoot">用于挂载拖拽 ghost 的 UI 根节点。</param>
    /// <param name="group">允许重排的目标容器。</param>
    public UiDragController(UIRoot uiRoot, UILayoutGroup group)
    {
        this.uiRoot = uiRoot;
        this.group = group;
    }

    /// <summary>
    /// 切换当前控制的目标容器。
    /// </summary>
    /// <remarks>
    /// 如果正在拖拽，会先 <see cref="Cancel"/> 当前拖拽，再切换到新的容器。
    /// </remarks>
    /// <param name="group">新的目标容器。</param>
    public void SetGroup(UILayoutGroup group)
    {
        if (IsDragging)
            Cancel();
        this.group = group;
    }

    /// <summary>
    /// 开始拖拽指定的 item。
    /// </summary>
    /// <param name="item">要被重排的子元素（必须属于当前 <see cref="UILayoutGroup"/>）。</param>
    /// <param name="pointerPos">指针的世界坐标位置。</param>
    /// <returns>成功开始拖拽返回 true；否则返回 false。</returns>
    public bool BeginDrag(UIElement item, Vector2 pointerPos)
    {
        if (dragItem != null)
            return false;

        var idx = group.Children.IndexOf(item);
        if (idx < 0)
            return false;

        var wr = item.WorldRect;
        grabOffset = pointerPos - wr.Position;

        startIndex = idx;
        placeholderIndex = idx;

        dragItem = item;

        group.RemoveChild(item);
        item.Parent = null;

        placeholder = CreatePlaceholder(wr, group.Layout.Direction);
        InsertChildAt(group, idx, placeholder);

        dragGhost = item.Clone(true);
        dragGhost.Interactable = false;
        dragGhost.Selectable = false;
        dragGhost.AnimateLayout = false;
        dragGhost.Rect = new Rect(wr.X, wr.Y, wr.Width, wr.Height);
        uiRoot.Root.AddChild(dragGhost);

        return true;
    }

    /// <summary>
    /// 更新拖拽（通常在指针移动时调用）。
    /// </summary>
    /// <param name="pointerPos">指针的世界坐标位置。</param>
    public void UpdateDrag(Vector2 pointerPos)
    {
        if (dragItem == null || dragGhost == null || placeholder == null)
            return;

        var gr = dragGhost.Rect;
        dragGhost.Rect = new Rect(
            pointerPos.X - grabOffset.X,
            pointerPos.Y - grabOffset.Y,
            gr.Width,
            gr.Height);

        var targetIndex = GetDropIndex(pointerPos, group, placeholder);
        if (targetIndex == placeholderIndex)
            return;

        group.Children.Remove(placeholder);

        if (targetIndex < 0) targetIndex = 0;
        if (targetIndex > group.Children.Count) targetIndex = group.Children.Count;

        group.Children.Insert(targetIndex, placeholder);
        placeholder.Parent = group;
        group.InvalidateLayout();

        placeholderIndex = targetIndex;
    }

    /// <summary>
    /// 结束拖拽并将 item 插入到当前 placeholder 所在位置。
    /// </summary>
    /// <remarks>
    /// 通常在指针抬起时调用。该方法会清理 ghost/placeholder，并在发生实际换位时触发 <see cref="Reordered"/>。
    /// </remarks>
    public void EndDrag()
    {
        if (dragItem == null || placeholder == null)
            return;

        Rect ghostWorldRect = default;
        var hasGhost = false;

        if (dragGhost != null)
        {
            ghostWorldRect = dragGhost.Rect;
            hasGhost = true;

            uiRoot.Root.RemoveChild(dragGhost);
            dragGhost.Parent = null;
            dragGhost = null;
        }

        var idx = group.Children.IndexOf(placeholder);
        if (idx < 0)
            idx = group.Children.Count;

        group.Children.Remove(placeholder);
        placeholder.Parent = null;
        placeholder = null;

        if (hasGhost)
        {
            var groupWr = group.WorldRect;
            dragItem.Rect = new Rect(
                ghostWorldRect.X - groupWr.X,
                ghostWorldRect.Y - groupWr.Y,
                ghostWorldRect.Width,
                ghostWorldRect.Height);
        }

        var from = startIndex;
        var to = idx;

        InsertChildAt(group, idx, dragItem);

        var item = dragItem;

        dragItem = null;
        grabOffset = default;
        startIndex = -1;
        placeholderIndex = -1;

        group.InvalidateLayout();

        if (from >= 0 && to >= 0 && from != to)
            Reordered?.Invoke(item, from, to);
    }

    /// <summary>
    /// 取消当前拖拽：尽量将 item 放回开始拖拽时的索引位置。
    /// </summary>
    public void Cancel()
    {
        if (dragItem == null)
            return;

        placeholderIndex = startIndex >= 0 ? startIndex : placeholderIndex;
        EndDrag();
    }

    UIElement CreatePlaceholder(Rect draggedWorldRect, LayoutDirection direction)
    {
        var main = direction == LayoutDirection.Row ? draggedWorldRect.Width : draggedWorldRect.Height;
        if (main <= 0f)
            main = 42f;

        Rect rect;
        switch (direction)
        {
            case LayoutDirection.Row:
                rect = new Rect(0, 0, main, 0);
                break;
            case LayoutDirection.Grid:
            {
                var size = MathF.Max(draggedWorldRect.Width, draggedWorldRect.Height);
                if (size <= 0f)
                    size = 42f;
                rect = new Rect(0, 0, size, size);
                break;
            }
            default:
                rect = new Rect(0, 0, 0, main);
                break;
        }

        var p = new UIElement(rect)
        {
            AnimateLayout = false,
            Interactable = false,
            Selectable = false
        };

        if (direction == LayoutDirection.Row)
            p.GrowY = 1;
        else if (direction == LayoutDirection.Column)
            p.GrowX = 1;

        return p;
    }

    static int GetDropIndex(Vector2 pointerPos, UILayoutGroup group, UIElement placeholder)
    {
        var dir = group.Layout.Direction;
        if (dir == LayoutDirection.Grid)
            return GetGridDropIndex(pointerPos, group, placeholder);

        var index = 0;

        for (int i = 0; i < group.Children.Count; i++)
        {
            var child = group.Children[i];
            if (ReferenceEquals(child, placeholder) || !child.Visible)
                continue;

            var wr = child.WorldRect;
            var mid = dir == LayoutDirection.Row
                ? wr.X + wr.Width * 0.5f
                : wr.Y + wr.Height * 0.5f;

            var cursor = dir == LayoutDirection.Row ? pointerPos.X : pointerPos.Y;
            if (cursor < mid)
                return index;

            index++;
        }

        return index;
    }

    static int GetGridDropIndex(Vector2 pointerPos, UILayoutGroup group, UIElement placeholder)
    {
        var cfg = group.Layout;
        var groupWr = group.WorldRect;

        var innerX = cfg.PaddingLeft;
        var innerY = cfg.PaddingTop;
        var innerW = groupWr.Width - cfg.PaddingLeft - cfg.PaddingRight;
        var innerH = groupWr.Height - cfg.PaddingTop - cfg.PaddingBottom;

        var cellW = cfg.GridCellWidth > 0f ? cfg.GridCellWidth : 42f;
        var cellH = cfg.GridCellHeight > 0f ? cfg.GridCellHeight : 42f;
        var stepX = cellW + cfg.ChildGap;
        var stepY = cellH + cfg.ChildGap;

        var cols = cfg.GridColumns;
        if (cols <= 0)
        {
            cols = innerW > 0f && cellW > 0f
                ? (int)((innerW + cfg.ChildGap) / (cellW + cfg.ChildGap))
                : 1;
        }

        if (cols < 1)
            cols = 1;

        var local = pointerPos - groupWr.Position;
        var x = local.X - innerX;
        var y = local.Y - innerY;

        var col = stepX > 0f ? (int)MathF.Floor((x + stepX * 0.5f) / stepX) : 0;
        var row = stepY > 0f ? (int)MathF.Floor((y + stepY * 0.5f) / stepY) : 0;

        if (col < 0) col = 0;
        if (col > cols - 1) col = cols - 1;
        if (row < 0) row = 0;

        var targetIndex = row * cols + col;

        var visibleCount = 0;
        for (int i = 0; i < group.Children.Count; i++)
        {
            var child = group.Children[i];
            if (ReferenceEquals(child, placeholder) || !child.Visible)
                continue;
            visibleCount++;
        }

        if (targetIndex < 0) targetIndex = 0;
        if (targetIndex > visibleCount) targetIndex = visibleCount;

        return targetIndex;
    }

    static void InsertChildAt(UIElement parent, int index, UIElement child)
    {
        if (index < 0) index = 0;
        if (index > parent.Children.Count) index = parent.Children.Count;

        parent.Children.Insert(index, child);
        child.Parent = parent;
        parent.InvalidateLayout();
    }
}

