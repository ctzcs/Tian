using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Core;
using Foster.Framework;

namespace Engine.UI_2;


public class UIElement
{
    public UIElement? Parent { get; private set; }
    public List<UIElement> Children { get; } = new();

    public LayoutStyle Layout;
    public Rect LayoutRect { get; private set; }
    public Rect ContentRect { get; private set; }

    public Rect TargetRect { get; private set; }

    public float Rotation { get; set; }
    public Vector2 RotationPivot { get; set; } = new Vector2(0.5f, 0.5f);

    public Matrix3x2 WorldMatrix { get; protected set; } = Matrix3x2.Identity;
    public Matrix3x2 InverseWorldMatrix { get; protected set; } = Matrix3x2.Identity;

    public bool Visible { get; set; } = true;
    public bool Display { get; set; } = true;

    public Color BackgroundColor { get; set; }
    public bool BackgroundEnabled { get; set; }

    public bool ClipChildren { get; set; }

    public bool Interactable { get; set; }
    public bool PointerPassThrough { get; set; }
    public object? UserData { get; set; }

    public Action<Ui2PointerEvent>? OnPointerEnter;
    public Action<Ui2PointerEvent>? OnPointerExit;
    public Action<Ui2PointerEvent>? OnPointerDown;
    public Action<Ui2PointerEvent>? OnPointerMove;
    public Action<Ui2PointerEvent>? OnPointerUp;
    public Action<Ui2PointerEvent>? OnClick;

    public bool AnimateLayout { get; set; } = true;
    public float LayoutTweenDuration { get; set; } = 0.15f;
    public Transition LayoutTransition { get; set; } = Transition.EaseOut;

    static readonly Func<Vector2, Vector2, float, Vector2> Vector2Lerp = Vector2.Lerp;
    Interpolated<Vector2> posTween;
    Interpolated<Vector2> sizeTween;
    bool targetDirty;
    bool initialized;

    public UIElement()
    {
    }

    public void AddChild(UIElement child)
    {
        if (child.Parent == this)
            return;

        child.Parent?.RemoveChild(child);
        child.Parent = this;
        Children.Add(child);
    }

    public void RemoveChild(UIElement child)
    {
        if (!Children.Remove(child))
            return;

        if (child.Parent == this)
            child.Parent = null;
    }

    public virtual Vector2 Measure(Vector2 availableSize)
    {
        float width = Layout.Width > 0f ? Layout.Width : availableSize.X;
        float height = Layout.Height > 0f ? Layout.Height : availableSize.Y;

        if (Layout.MinWidth > 0f && width < Layout.MinWidth)
            width = Layout.MinWidth;
        if (Layout.MaxWidth > 0f && width > Layout.MaxWidth)
            width = Layout.MaxWidth;
        if (Layout.MinHeight > 0f && height < Layout.MinHeight)
            height = Layout.MinHeight;
        if (Layout.MaxHeight > 0f && height > Layout.MaxHeight)
            height = Layout.MaxHeight;

        return new Vector2(width, height);
    }

    void ApplyContentRect(Rect rect)
    {
        float innerX = rect.X + Layout.PaddingLeft;
        float innerY = rect.Y + Layout.PaddingTop;
        float innerW = rect.Width - Layout.PaddingLeft - Layout.PaddingRight;
        float innerH = rect.Height - Layout.PaddingTop - Layout.PaddingBottom;

        ContentRect = new Rect(innerX, innerY, innerW, innerH);
    }

    void ArrangeChildrenDefault()
    {
        if (Parent == null || Layout.LayoutType != LayoutType.None)
            return;

        float innerX = Layout.PaddingLeft;
        float innerY = Layout.PaddingTop;
        float innerW = LayoutRect.Width - Layout.PaddingLeft - Layout.PaddingRight;
        float innerH = LayoutRect.Height - Layout.PaddingTop - Layout.PaddingBottom;
        if (innerW < 0f) innerW = 0f;
        if (innerH < 0f) innerH = 0f;

        foreach (var child in Children)
        {
            if (!child.Display || !child.Visible)
                continue;

            var style = child.Layout;
            float marginLeft = style.MarginLeft;
            float marginRight = style.MarginRight;
            float marginTop = style.MarginTop;
            float marginBottom = style.MarginBottom;

            float availableW = innerW - marginLeft - marginRight;
            float availableH = innerH - marginTop - marginBottom;
            if (availableW < 0f) availableW = 0f;
            if (availableH < 0f) availableH = 0f;

            float width = style.Width > 0f ? style.Width : availableW;
            float height = style.Height > 0f ? style.Height : availableH;

            if (style.MinWidth > 0f && width < style.MinWidth) width = style.MinWidth;
            if (style.MaxWidth > 0f && width > style.MaxWidth) width = style.MaxWidth;
            if (style.MinHeight > 0f && height < style.MinHeight) height = style.MinHeight;
            if (style.MaxHeight > 0f && height > style.MaxHeight) height = style.MaxHeight;

            if (width > availableW) width = availableW;
            if (height > availableH) height = availableH;

            float x;
            switch (style.AlignX)
            {
                case HorizontalAlignment.End:
                    x = innerX + innerW - marginRight - width;
                    break;
                case HorizontalAlignment.Center:
                    x = innerX + marginLeft + (availableW - width) * 0.5f;
                    break;
                case HorizontalAlignment.Stretch:
                case HorizontalAlignment.Start:
                default:
                    x = innerX + marginLeft;
                    break;
            }

            float y;
            switch (style.AlignY)
            {
                case VerticalAlignment.End:
                    y = innerY + innerH - marginBottom - height;
                    break;
                case VerticalAlignment.Center:
                    y = innerY + marginTop + (availableH - height) * 0.5f;
                    break;
                case VerticalAlignment.Stretch:
                case VerticalAlignment.Start:
                default:
                    y = innerY + marginTop;
                    break;
            }

            child.Arrange(new Rect(x, y, width, height));
        }
    }

    // 布局矩形改变时调用
    protected virtual void OnLayoutRectChanged(Rect rect)
    {
    }

    // 安排元素的布局矩形
    // 子类可以重写此方法来实现自定义的布局逻辑
    // 默认实现只是将目标矩形赋值给布局矩形
    public virtual void Arrange(Rect rect)
    {
        if (!TargetRect.Equals(rect))
        {
            TargetRect = rect;
            targetDirty = true;
        }
    }

    void EnsureInitialized(float time)
    {
        if (initialized)
            return;

        var pos = new Vector2(TargetRect.X, TargetRect.Y);
        var size = new Vector2(TargetRect.Width, TargetRect.Height);
        posTween = new Interpolated<Vector2>(pos, pos, time, Transition.None, Vector2Lerp);
        sizeTween = new Interpolated<Vector2>(size, size, time, Transition.None, Vector2Lerp);
        LayoutRect = TargetRect;
        ApplyContentRect(LayoutRect);
        OnLayoutRectChanged(LayoutRect);
        initialized = true;
        targetDirty = false;
    }

    public void Update(float time)
    {
        if (!initialized)
        {
            EnsureInitialized(time);
        }
        else
        {
            if (targetDirty)
            {
                var currentPos = new Vector2(LayoutRect.X, LayoutRect.Y);
                var currentSize = new Vector2(LayoutRect.Width, LayoutRect.Height);
                var targetPos = new Vector2(TargetRect.X, TargetRect.Y);
                var targetSize = new Vector2(TargetRect.Width, TargetRect.Height);

                if (!AnimateLayout || LayoutTweenDuration <= 0f)
                {
                    LayoutRect = TargetRect;
                    ApplyContentRect(LayoutRect);
                    ArrangeChildrenDefault();
                    OnLayoutRectChanged(LayoutRect);
                }
                else
                {
                    posTween.SetValue(currentPos, targetPos, time, LayoutTransition, Vector2Lerp);
                    sizeTween.SetValue(currentSize, targetSize, time, LayoutTransition, Vector2Lerp);
                    posTween.SetDuration(LayoutTweenDuration);
                    sizeTween.SetDuration(LayoutTweenDuration);
                }

                targetDirty = false;
            }

            if (AnimateLayout && LayoutTweenDuration > 0f)
            {
                var pos = posTween.GetValue(time);
                var size = sizeTween.GetValue(time);
                LayoutRect = new Rect(pos.X, pos.Y, size.X, size.Y);
                ApplyContentRect(LayoutRect);
                ArrangeChildrenDefault();
                OnLayoutRectChanged(LayoutRect);
            }
            else
            {
                LayoutRect = TargetRect;
                ApplyContentRect(LayoutRect);
                ArrangeChildrenDefault();
                OnLayoutRectChanged(LayoutRect);
            }
        }

        foreach (var child in Children)
            child.Update(time);
    }

    public Rect GetWorldRect()
    {
        if (Parent == null)
            return LayoutRect;

        var parentRect = Parent.GetWorldRect();
        return new Rect(parentRect.X + LayoutRect.X, parentRect.Y + LayoutRect.Y, LayoutRect.Width, LayoutRect.Height);
    }

    public Vector2 WorldToLocal(Vector2 world)
    {
        return Vector2.Transform(world, InverseWorldMatrix);
    }

    public Vector2 LocalToWorld(Vector2 local)
    {
        return Vector2.Transform(local, WorldMatrix);
    }

    public virtual bool HitTest(Vector2 position)
    {
        if (!Interactable)
            return false;

        var size = new Vector2(LayoutRect.Width, LayoutRect.Height);
        if (size.X <= 0f || size.Y <= 0f)
            return false;

        var local = Vector2.Transform(position, InverseWorldMatrix);
        return local.X >= 0f && local.X <= size.X &&
               local.Y >= 0f && local.Y <= size.Y;
    }

    public UIElement? Hit(Vector2 position)
    {
        if (!Visible || !Display)
            return null;

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            var child = Children[i];
            var hit = child.Hit(position);
            if (hit != null)
                return hit;
        }

        if (Interactable && HitTest(position))
            return this;

        return null;
    }

    public void HitAll(Vector2 position, List<UIElement> results)
    {
        if (!Visible || !Display)
            return;

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            var child = Children[i];
            child.HitAll(position, results);
        }

        if (HitTest(position))
            results.Add(this);
    }

    protected Matrix3x2 ComputeLocalMatrix()
    {
        var size = new Vector2(LayoutRect.Width, LayoutRect.Height);
        var pivot = RotationPivot * size;
        var position = new Vector2(LayoutRect.X + pivot.X, LayoutRect.Y + pivot.Y);

        return Transform.CreateMatrix(position, pivot, Vector2.One, Rotation);
    }

    public virtual void UpdateWorldMatrix(Matrix3x2 parentMatrix)
    {
        var local = ComputeLocalMatrix();
        WorldMatrix = parentMatrix * local;

        if (!Matrix3x2.Invert(WorldMatrix, out var inv))
            inv = Matrix3x2.Identity;

        InverseWorldMatrix = inv;

        foreach (var child in Children)
            child.UpdateWorldMatrix(WorldMatrix);
    }

    public virtual void CollectDrawCommands(List<Ui2DrawCommand> commands, int depth)
    {
        if (!Visible || !Display)
            return;

        var rect = new Rect(0f, 0f, LayoutRect.Width, LayoutRect.Height);
        var matrix = WorldMatrix;

        if (BackgroundEnabled)
            commands.Add(new Ui2DrawCommand(Ui2DrawCommandType.Background, rect, BackgroundColor, depth, matrix: matrix));

        if (ClipChildren)
            commands.Add(new Ui2DrawCommand(Ui2DrawCommandType.ClipPush, GetWorldRect(), default, depth));

        int nextDepth = depth + 1;
        foreach (var child in Children)
            child.CollectDrawCommands(commands, nextDepth);

        if (ClipChildren)
            commands.Add(new Ui2DrawCommand(Ui2DrawCommandType.ClipPop, GetWorldRect(), default, depth));
    }

    public void ForceLayoutRect(Rect rect)
    {
        if (!initialized)
        {
            var pos = new Vector2(rect.X, rect.Y);
            var size = new Vector2(rect.Width, rect.Height);
            posTween = new Interpolated<Vector2>(pos, pos, 0f, Transition.None, Vector2Lerp);
            sizeTween = new Interpolated<Vector2>(size, size, 0f, Transition.None, Vector2Lerp);
            TargetRect = rect;
            LayoutRect = rect;
            ApplyContentRect(LayoutRect);
            ArrangeChildrenDefault();
            OnLayoutRectChanged(LayoutRect);
            initialized = true;
            targetDirty = false;
        }
        else
        {
            var pos = new Vector2(rect.X, rect.Y);
            var size = new Vector2(rect.Width, rect.Height);
            posTween.SetValue(pos, pos, 0f, Transition.None, Vector2Lerp);
            sizeTween.SetValue(size, size, 0f, Transition.None, Vector2Lerp);
            posTween.SetDuration(0f);
            sizeTween.SetDuration(0f);
            TargetRect = rect;
            LayoutRect = rect;
            ApplyContentRect(LayoutRect);
            ArrangeChildrenDefault();
            OnLayoutRectChanged(LayoutRect);
            targetDirty = false;
        }
    }
}

public static class UI2FluentExtensions
{
    public static T WithBackgroundColor<T>(this T element, Color color)
        where T : UIElement
    {
        element.BackgroundEnabled = true;
        element.BackgroundColor = color;
        return element;
    }

    public static T WithLayoutAnimation<T>(this T element, float duration, Transition transition = Transition.EaseOut)
        where T : UIElement
    {
        element.LayoutTweenDuration = duration;
        element.LayoutTransition = transition;
        return element;
    }

    public static T WithPadding<T>(this T element, float left, float top, float right, float bottom)
        where T : UIElement
    {
        var layout = element.Layout;
        layout.PaddingLeft = left;
        layout.PaddingTop = top;
        layout.PaddingRight = right;
        layout.PaddingBottom = bottom;
        element.Layout = layout;
        return element;
    }

    public static T WithPadding<T>(this T element, float all)
        where T : UIElement
    {
        return element.WithPadding(all, all, all, all);
    }

    public static T WithMargin<T>(this T element, float left, float top, float right, float bottom)
        where T : UIElement
    {
        var layout = element.Layout;
        layout.MarginLeft = left;
        layout.MarginTop = top;
        layout.MarginRight = right;
        layout.MarginBottom = bottom;
        element.Layout = layout;
        return element;
    }

    public static T WithMargin<T>(this T element, float all)
        where T : UIElement
    {
        return element.WithMargin(all, all, all, all);
    }

    public static T WithGrow<T>(this T element, float grow)
        where T : UIElement
    {
        var layout = element.Layout;
        layout.Grow = grow;
        element.Layout = layout;
        return element;
    }

    public static T WithSize<T>(this T element, float width, float height)
        where T : UIElement
    {
        var layout = element.Layout;
        layout.Width = width;
        layout.Height = height;
        element.Layout = layout;
        return element;
    }

    public static T WithViewportRatio<T>(this T element, Rect viewportRatio)
        where T : UIElement
    {
        var layout = element.Layout;
        layout.SizeMode = LayoutSizeMode.ViewportRatio;
        layout.ViewportRatio = viewportRatio;
        element.Layout = layout;
        return element;
    }

    public static T WithAlign<T>(this T element, HorizontalAlignment x, VerticalAlignment y)
        where T : UIElement
    {
        var layout = element.Layout;
        layout.AlignX = x;
        layout.AlignY = y;
        element.Layout = layout;
        return element;
    }

    public static T WithUserData<T>(this T element, object userData)
        where T : UIElement
    {
        element.UserData = userData;
        return element;
    }

    public static TParent WithChild<TParent>(this TParent parent, UIElement child)
        where TParent : UIElement
    {
        parent.AddChild(child);
        return parent;
    }

    public static TParent WithChildren<TParent>(this TParent parent, params UIElement[] children)
        where TParent : UIElement
    {
        for (int i = 0; i < children.Length; i++)
            parent.AddChild(children[i]);
        return parent;
    }

    public static T WithRotation<T>(this T element, float rotation, Vector2? pivot = null)
        where T : UIElement
    {
        element.Rotation = rotation;
        if (pivot.HasValue)
            element.RotationPivot = pivot.Value;
        return element;
    }

    public static T WithRotationPivot<T>(this T element, Vector2 pivot)
        where T : UIElement
    {
        element.RotationPivot = pivot;
        return element;
    }
}

public class Ui2Navigator
{
    readonly UIElement root;

    public UIElement Current { get; private set; }

    public Ui2Navigator(UIElement root)
    {
        this.root = root;
    }

    public void SetFocus(UIElement element)
    {
        Current = element;
    }

    public void MoveUp()
    {
        Move(new Vector2(0f, -1f));
    }

    public void MoveDown()
    {
        Move(new Vector2(0f, 1f));
    }

    public void MoveLeft()
    {
        Move(new Vector2(-1f, 0f));
    }

    public void MoveRight()
    {
        Move(new Vector2(1f, 0f));
    }

    public void ClickCurrent()
    {
        if (Current == null)
            return;

        var wr = Current.GetWorldRect();
        var center = new Vector2(wr.X + wr.Width * 0.5f, wr.Y + wr.Height * 0.5f);
        var e = new Ui2PointerEvent
        {
            Target = Current,
            Current = Current,
            Position = center
        };
        Current.OnClick?.Invoke(e);
    }

    void Move(Vector2 direction)
    {
        if (Current == null)
        {
            Current = FindFirstFocusable(root);
            return;
        }

        var next = FindNext(Current, direction);
        if (next != null)
            Current = next;
    }

    UIElement FindFirstFocusable(UIElement node)
    {
        if (IsFocusable(node))
            return node;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            var found = FindFirstFocusable(child);
            if (found != null)
                return found;
        }

        return null;
    }

    UIElement FindNext(UIElement from, Vector2 direction)
    {
        var candidates = new List<UIElement>();
        CollectFocusable(root, candidates);

        var fromRect = from.GetWorldRect();
        var fromCenter = new Vector2(fromRect.X + fromRect.Width * 0.5f, fromRect.Y + fromRect.Height * 0.5f);

        if (direction.LengthSquared() <= 0f)
            direction = new Vector2(0f, 1f);
        direction = Vector2.Normalize(direction);

        UIElement best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < candidates.Count; i++)
        {
            var element = candidates[i];
            if (element == from)
                continue;

            var rect = element.GetWorldRect();
            var center = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
            var to = center - fromCenter;
            if (to.LengthSquared() <= 0f)
                continue;

            var toDir = Vector2.Normalize(to);
            float dot = Vector2.Dot(direction, toDir);
            if (dot <= 0.2f)
                continue;

            float distance = to.Length();
            float score = dot * 10f - distance;

            if (score > bestScore)
            {
                bestScore = score;
                best = element;
            }
        }

        return best;
    }

    void CollectFocusable(UIElement node, List<UIElement> results)
    {
        if (IsFocusable(node))
            results.Add(node);

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            CollectFocusable(child, results);
        }
    }

    bool IsFocusable(UIElement element)
    {
        if (!element.Visible || !element.Display)
            return false;

        if (!element.Interactable)
            return false;

        var rect = element.GetWorldRect();
        if (rect.Width <= 0f || rect.Height <= 0f)
            return false;

        return true;
    }
}
