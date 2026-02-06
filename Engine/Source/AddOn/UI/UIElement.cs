using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Asset;
using Engine.Core;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Engine.UI;

public enum ElementTextOverflowMode
{
    None,
    ShrinkToFit,
    Wrap,
    WrapAutoHeight,
    ShrinkAndWrap
}

public struct ElementTextStyle
{
    public bool Enabled;
    public string Content;
    public Color Color;
    public Vector2 Align;
    public float Size; // 字号（<=0 表示使用默认字号）
    public ElementTextOverflowMode OverflowMode;
}

public enum ElementBackgroundMode
{
    None,
    Color,
    Image
}

public enum ElementImageFillMode
{
    Original,
    Stretch,
    Fit,
    NineSlice
}

public struct ElementBackgroundStyle
{
    public bool Enabled;
    public ElementBackgroundMode Mode;

    public Color Color;

    public Subtexture? Subtex;
    public Texture? Texture;

    public ElementImageFillMode ImageFillMode;
    public Vector4 NineSliceBorder;
}

public enum UISizeMode
{
    Pixel,
    ViewportRatio
}

/// <summary>
/// UI的基本单位
/// </summary>
/// <param name="maskable"></param>
/// <param name="selectable"></param>
/// <param name="visible"></param>
/// <param name="rect"></param>
/// <param name="parent"></param>
public class UIElement(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
{
    protected bool isDisabled = false;
    protected bool layoutDirty = true;

    float rotation;
    Vector2 rotationPivot = new(0.5f, 0.5f);

    /// <summary>
    /// 可遮罩的，用于剪裁和Hit
    /// </summary>
    protected bool maskable = maskable;
    protected bool selectable = selectable;
    protected bool visible = visible;
    protected Rect rect = rect;
    protected Rect targetRect = rect;
    protected Rect worldRect;
    protected LayoutConfig layout;

    public UISizeMode SizeMode;
    public Rect NormalizedRect;

    float widthRatioToParent;
    float heightRatioToParent;
    float xRatioToParent;
    float yRatioToParent;
    float growX;
    float growY;
    float minWidth;
    float maxWidth;
    float minHeight;
    float maxHeight;

    bool targetDirty;
    bool animateLayout = true;
    float layoutTweenDuration = 0.15f;
    Transition layoutTransition = Transition.EaseOut;

    static readonly Func<Vector2, Vector2, float, Vector2> Vector2Lerp = Vector2.Lerp;

    Interpolated<Vector2> posTween = new(rect.Position, rect.Position, 0f, Transition.None, Vector2Lerp);
    Interpolated<Vector2> sizeTween = new(rect.Size, rect.Size, 0f, Transition.None, Vector2Lerp);

    public LayoutConfig Layout
    {
        get => layout;
        set
        {
            if (layout.Equals(value))
                return;
            layout = value;
            InvalidateLayout();
        }
    }
    
    protected UIElement? parent = parent;
    protected List<UIElement> children = new();
    
    public UIElement? Parent
    {
        get { return parent; }
        set { parent = value; }
    }
    
    public List<UIElement> Children => children;

    public UIElement() : this(new Rect(0, 0, 0, 0))
    {
    }

    public UIElement(Rect rect, UIElement? parent = null)
        : this(maskable: true, selectable: false, visible: true, rect: rect, parent: parent)
    {
    }

    public bool Maskable
    {
        get => maskable;
        set => maskable = value;
    }

    public UIElement ClearChildren()
    {
        for (int i = 0; i < children.Count; i++)
            children[i].Parent = null;

        children.Clear();
        InvalidateLayout();
        return this;
    }

    public Rect Rect
    {
        get => rect;
        set
        {
            rect = value;
            targetRect = value;
            targetDirty = false;
            ResetLayoutTweens();
            InvalidateLayout();
        }
    }

    public Rect TargetRect => targetRect;

    public Rect WorldRect => worldRect;

    public float WidthRatioToParent
    {
        get => widthRatioToParent;
        set => SetLayoutFloat(ref widthRatioToParent, value);
    }

    public float HeightRatioToParent
    {
        get => heightRatioToParent;
        set => SetLayoutFloat(ref heightRatioToParent, value);
    }

    public float XRatioToParent
    {
        get => xRatioToParent;
        set => SetLayoutFloat(ref xRatioToParent, value);
    }

    public float YRatioToParent
    {
        get => yRatioToParent;
        set => SetLayoutFloat(ref yRatioToParent, value);
    }

    public float GrowX
    {
        get => growX;
        set => SetLayoutFloat(ref growX, value);
    }

    public float GrowY
    {
        get => growY;
        set => SetLayoutFloat(ref growY, value);
    }

    public float MinWidth
    {
        get => minWidth;
        set => SetLayoutFloat(ref minWidth, value);
    }

    public float MaxWidth
    {
        get => maxWidth;
        set => SetLayoutFloat(ref maxWidth, value);
    }

    public float MinHeight
    {
        get => minHeight;
        set => SetLayoutFloat(ref minHeight, value);
    }

    public float MaxHeight
    {
        get => maxHeight;
        set => SetLayoutFloat(ref maxHeight, value);
    }

    /// <summary>
    /// 创建克隆实例的方法
    /// </summary>
    /// <returns></returns>
    protected virtual UIElement CreateCloneInstance()
    {
        return new UIElement(maskable, selectable, visible, rect);
    }
    
    /// <summary>
    /// 普通的克隆拷贝
    /// </summary>
    /// <param name="target"></param>
    /// <param name="cloneChildren"></param>
    protected virtual void CopyToClone(UIElement target, bool cloneChildren)
    {
        target.Rect = rect;
        target.visible = visible;
        target.selectable = selectable;
        target.isDisabled = isDisabled;
        target.maskable = maskable;
        target.layout = layout;
        target.SizeMode = SizeMode;
        target.NormalizedRect = NormalizedRect;
        target.widthRatioToParent = widthRatioToParent;
        target.heightRatioToParent = heightRatioToParent;
        target.xRatioToParent = xRatioToParent;
        target.yRatioToParent = yRatioToParent;
        target.growX = growX;
        target.growY = growY;
        target.minWidth = minWidth;
        target.maxWidth = maxWidth;
        target.minHeight = minHeight;
        target.maxHeight = maxHeight;
        target.animateLayout = animateLayout;
        target.layoutTweenDuration = layoutTweenDuration;
        target.layoutTransition = layoutTransition;
        target.rotation = rotation;
        target.rotationPivot = rotationPivot;

        if (cloneChildren)
        {
            foreach (var child in children)
            {
                var childClone = child.Clone(true);
                target.AddChild(childClone);
            }
        }
    }

    /// <summary>
    /// 克隆方法
    /// </summary>
    /// <param name="cloneChildren"></param>
    /// <returns></returns>
    public virtual UIElement Clone(bool cloneChildren = true)
    {
        var instance = CreateCloneInstance();
        CopyToClone(instance, cloneChildren);
        return instance;
    }

    public float Rotation
    {
        get => rotation;
        set => rotation = value;
    }

    public Vector2 RotationPivot
    {
        get => rotationPivot;
        set => rotationPivot = value;
    }

    /// <summary>
    /// Animate动画，默认开启
    /// </summary>
    public bool AnimateLayout
    {
        get => animateLayout;
        set => animateLayout = value;
    }

    /// <summary>
    /// Tween的时间
    /// </summary>
    public float LayoutTweenDuration
    {
        get => layoutTweenDuration;
        set => layoutTweenDuration = value;
    }

    /// <summary>
    /// 布局转换函数
    /// </summary>
    public Transition LayoutTransition
    {
        get => layoutTransition;
        set => layoutTransition = value;
    }

    /// <summary>
    /// 用来设置目标Rect, 这样做会有动画
    /// </summary>
    /// <param name="value"></param>
    public void SetTargetRect(Rect value)
    {
        if (targetRect == value)
            return;

        targetRect = value;
        targetDirty = true;
        InvalidateLayout();
    }

    
    /// <summary>
    /// 是否可选
    /// </summary>
    public bool Selectable
    {
        get => selectable;
        set => selectable = value;
    }

    /// <summary>
    /// 是否可视
    /// </summary>
    public bool Visible
    {
        get => visible;
        set => visible = value;
    }

    /// <summary>
    /// 是否可交互
    /// </summary>
    public bool Interactable
    {
        get => !isDisabled;
        set => isDisabled = !value;
    }

    /// <summary>
    /// 布局是否改变
    /// </summary>
    public bool IsLayoutDirty => layoutDirty;

    public UIElement AddChild(UIElement child)
    {
        children.Add(child);
        child.Parent = this;
        InvalidateLayout();
        return this;
    }

    public UIElement RemoveChild(UIElement child)
    {
        children.Remove(child);
        InvalidateLayout();
        return this;
    }


    public void RemoveSelf()
    {
        if (parent != null)
        {
            parent.RemoveChild(this);
        }
    }
    
    
    public void InvalidateLayout()
    {
        if (layoutDirty)
            return;
        layoutDirty = true;
        parent?.InvalidateLayout();
    }

    /// <summary>
    /// 强制更新布局
    /// </summary>
    /// <param name="recursive"></param>
    public void UpdateLayoutNow(bool recursive = true)
    {
        EnsureLayoutUpToDate();

        if (recursive)
        {
            foreach (var child in children)
                child.UpdateLayoutNow(true);
        }
    }

    //先自己 UpdateLayout，再让子节点 Apply ，是一个「自上而下」的顺序。
    public virtual void Apply()
    {
        EnsureLayoutUpToDate();

        foreach (var child in children)
            child.Apply();
    }

    public void Update(float time)
    {
        if (!visible)
            return;

        EnsureLayoutUpToDate();

        if (targetDirty)
        {
            ApplyTarget(time);
            targetDirty = false;
        }

        if (animateLayout)
        {
            rect.Position = posTween.GetValue(time);
            rect.Size = sizeTween.GetValue(time);
        }

        foreach (var child in children)
        {
            child.Update(time);
        }
    }
    
    public void CollectDrawCommands(List<UIDrawCommand> commands, int depth = 0)
    {
        CollectDrawCommandsInternal(commands, depth, 0, Matrix3x2.Identity);
    }

    public void CollectDrawCommands(List<UIDrawCommand> commands, int depth, int group)
    {
        CollectDrawCommandsInternal(commands, depth, group, Matrix3x2.Identity);
    }

    protected internal virtual void CollectDrawCommandsInternal(List<UIDrawCommand> commands, int depth, int group, Matrix3x2 parentMatrix)
    {
        if (!TryPrepareCollectDrawCommands(parentMatrix, out var matrix))
            return;

        CollectDrawCommandsForChildren(commands, depth, group, matrix);
    }

    protected bool TryPrepareCollectDrawCommands(Matrix3x2 parentMatrix, out Matrix3x2 matrix)
    {
        if (!visible)
        {
            matrix = parentMatrix;
            return false;
        }

        worldRect = GetWorldRect();

        matrix = parentMatrix;
        if (rotation != 0f)
        {
            var basePivot = worldRect.Position + rotationPivot * worldRect.Size;
            var pivot = Vector2.Transform(basePivot, parentMatrix);
            var local = Matrix3x2.CreateTranslation(-pivot) *
                        Matrix3x2.CreateRotation(rotation) *
                        Matrix3x2.CreateTranslation(pivot);
            matrix = parentMatrix * local;
        }

        return true;
    }

    protected void CollectDrawCommandsForChildren(List<UIDrawCommand> commands, int depth, int group, Matrix3x2 matrix)
    {
        foreach (var child in children)
            child.CollectDrawCommandsInternal(commands, depth, group, matrix);
    }

    public void CollectDrawCommandsAsRoot(List<UIDrawCommand> commands)
    {
        var group = 0;
        foreach (var child in children)
        {
            child.CollectDrawCommands(commands, 0, group);
            group++;
        }
    }
    
    
    public virtual UIElement? Hit(Vector2 point)
    {
        if (isDisabled)
            return null;

        if (!visible)
            return null;

        for (var i = children.Count - 1; i >= 0; i--)
        {
            var child = children[i];
            if (!child.Visible)
                continue;

            var hit = child.Hit(point);
            if (hit != null)
                return hit;
        }

        if (!selectable)
            return null;

        var wr = GetWorldRect();
        if (wr.Contains(point))
            return this;

        return null;
    }
    
    protected virtual Vector2 Measure(Vector2 availableSize)
    {
        // 默认：用当前 Rect.Size，当成 Fixed
        return rect.Size;
    }

    protected virtual void Arrange(Rect bounds)
    {
        // 默认：自己就占这个 Box
        targetRect = bounds;
    }

    protected virtual void UpdateLayout()
    {
        if (parent == null)
            return;

        if (widthRatioToParent <= 0f && heightRatioToParent <= 0f &&
            xRatioToParent <= 0f && yRatioToParent <= 0f)
            return;

        var parentRect = parent.TargetRect;
        var r = rect;

        if (xRatioToParent > 0f)
            r.X = parentRect.Width * xRatioToParent;
        if (yRatioToParent > 0f)
            r.Y = parentRect.Height * yRatioToParent;

        if (widthRatioToParent > 0f)
            r.Width = parentRect.Width * widthRatioToParent;
        if (heightRatioToParent > 0f)
            r.Height = parentRect.Height * heightRatioToParent;

        if (minWidth > 0f && r.Width < minWidth)
            r.Width = minWidth;
        if (maxWidth > 0f && r.Width > maxWidth)
            r.Width = maxWidth;
        if (minHeight > 0f && r.Height < minHeight)
            r.Height = minHeight;
        if (maxHeight > 0f && r.Height > maxHeight)
            r.Height = maxHeight;

        if (r != rect)
        {
            rect = r;
            targetRect = r;
        }
    }

    bool SetLayoutFloat(ref float field, float value)
    {
        if (field == value)
            return false;
        field = value;
        InvalidateLayout();
        return true;
    }

    void EnsureLayoutUpToDate()
    {
        if (!layoutDirty)
            return;
        UpdateLayout();
        layoutDirty = false;
    }

    void ResetLayoutTweens()
    {
        posTween = new Interpolated<Vector2>(rect.Position, rect.Position, 0f, Transition.None, Vector2Lerp);
        sizeTween = new Interpolated<Vector2>(rect.Size, rect.Size, 0f, Transition.None, Vector2Lerp);
    }

    protected void DrawBackgroundInternal(Batcher batcher, ElementBackgroundStyle background)
    {
        if (!background.Enabled)
            return;

        switch (background.Mode)
        {
            case ElementBackgroundMode.Color:
                batcher.Quad(new Quad(worldRect), background.Color);
                break;

            case ElementBackgroundMode.Image:
                DrawBackgroundImage(batcher, background);
                break;
        }
    }

    protected void DrawTextInternal(Batcher batcher, ElementTextStyle textStyle)
    {
        if (!(textStyle.Enabled && !string.IsNullOrEmpty(textStyle.Content) && Assets.Font != null))
            return;

        var boxPos = worldRect.Position;
        var align = textStyle.Align;
        var anchor = new Vector2(boxPos.X + worldRect.Width * align.X,
                                 boxPos.Y + worldRect.Height * align.Y);

        if (textStyle.OverflowMode == ElementTextOverflowMode.WrapAutoHeight)
        {
            var boxW = worldRect.Width;
            if (boxW > 0)
            {
                var size = textStyle.Size > 0f ? textStyle.Size : Assets.Font.Size;
                var sizeScale = size / Assets.Font.Size;
                ApplyTextWrapAutoHeight(textStyle, boxW, sizeScale, align, ref anchor);
            }
        }

        switch (textStyle.OverflowMode)
        {
            case ElementTextOverflowMode.ShrinkToFit:
                DrawElementTextShrinkToFit(textStyle, batcher, anchor, align);
                break;
            case ElementTextOverflowMode.Wrap:
                DrawElementTextWrap(textStyle, batcher, anchor, align);
                break;
            case ElementTextOverflowMode.WrapAutoHeight:
                DrawElementTextWrap(textStyle, batcher, anchor, align);
                break;
            case ElementTextOverflowMode.ShrinkAndWrap:
                DrawElementTextShrinkAndWrap(textStyle, batcher, anchor, align);
                break;
            default:
                var text = textStyle.Content.AsSpan();
                var size = textStyle.Size;
                if (size > 0f)
                    batcher.Text(Assets.Font, text, anchor, align, size, textStyle.Color);
                else
                    batcher.Text(Assets.Font, text, anchor, align, textStyle.Color);
                break;
        }
    }

    void ApplyTextWrapAutoHeight(ElementTextStyle textStyle, float boxW, float sizeScale, Vector2 justify, ref Vector2 anchor)
    {
        var content = textStyle.Content ?? string.Empty;
        var lines = Assets.Font.WrapText(content.AsSpan(), boxW);
        var lineCount = lines.Count;

        float height = 0;
        if (lineCount > 0)
            height = Assets.Font.Height * lineCount + Assets.Font.LineGap * (lineCount - 1);

        height *= sizeScale;

        rect.Height = height;
        targetRect.Height = height;
        worldRect = GetWorldRect();
        anchor.Y = worldRect.Y + worldRect.Height * justify.Y;
    }

    void DrawBackgroundImage(Batcher batcher, ElementBackgroundStyle background)
    {
        var tex = background.Subtex?.Texture ?? background.Texture;
        if (tex == null)
            return;

        var rect = worldRect;

        if (background.Subtex.HasValue)
        {
            var sub = background.Subtex.Value;
            switch (background.ImageFillMode)
            {
                case ElementImageFillMode.Original:
                    batcher.Image(sub, rect.Position, background.Color);
                    break;
                case ElementImageFillMode.Stretch:
                    batcher.ImageStretch(sub, rect, background.Color);
                    break;
                case ElementImageFillMode.Fit:
                    batcher.ImageFit(sub, rect, new Vector2(0.5f, 0.5f), background.Color, false, false);
                    break;
                case ElementImageFillMode.NineSlice:
                    DrawNineSlice(batcher, sub, rect, background.NineSliceBorder, background.Color);
                    break;
            }
        }
        else
        {
            var sub = new Subtexture(tex);
            switch (background.ImageFillMode)
            {
                case ElementImageFillMode.Original:
                    batcher.Image(tex, rect.Position, background.Color);
                    break;
                case ElementImageFillMode.Stretch:
                    batcher.ImageStretch(sub, rect, background.Color);
                    break;
                case ElementImageFillMode.Fit:
                    batcher.ImageFit(sub, rect, new Vector2(0.5f, 0.5f), background.Color, false, false);
                    break;
                case ElementImageFillMode.NineSlice:
                    DrawNineSlice(batcher, sub, rect, background.NineSliceBorder, background.Color);
                    break;
            }
        }
    }

    void DrawNineSlice(Batcher batcher, Subtexture subtex, Rect dst, Vector4 border, Color color)
    {
        // border: (left, top, right, bottom) in pixels of source texture
        var tex = subtex.Texture;
        if (tex == null)
            return;

        float left = border.X;
        float top = border.Y;
        float right = border.Z;
        float bottom = border.W;

        float srcW = subtex.Width;
        float srcH = subtex.Height;

        // 最小保护：如果目标太小，直接拉伸整图
        if (dst.Width <= left + right || dst.Height <= top + bottom)
        {
            batcher.ImageStretch(subtex, dst, color);
            return;
        }

        float x0 = dst.X;
        float x1 = dst.X + left;
        float x2 = dst.X + dst.Width - right;
        float x3 = dst.X + dst.Width;

        float y0 = dst.Y;
        float y1 = dst.Y + top;
        float y2 = dst.Y + dst.Height - bottom;
        float y3 = dst.Y + dst.Height;

        float u0 = subtex.TexCoords[0].X;
        float v0 = subtex.TexCoords[0].Y;
        float u3 = subtex.TexCoords[2].X;
        float v3 = subtex.TexCoords[2].Y;

        float duL = (left / srcW) * (u3 - u0);
        float duR = (right / srcW) * (u3 - u0);
        float dvT = (top / srcH) * (v3 - v0);
        float dvB = (bottom / srcH) * (v3 - v0);

        float u1 = u0 + duL;
        float u2 = u3 - duR;
        float v1 = v0 + dvT;
        float v2 = v3 - dvB;

        // 9 宫格绘制（从左上到右下）
        // 左上
        batcher.Quad(tex,
            new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1),
            new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1),
            color);
        // 上中
        batcher.Quad(tex,
            new Vector2(x1, y0), new Vector2(x2, y0), new Vector2(x2, y1), new Vector2(x1, y1),
            new Vector2(u1, v0), new Vector2(u2, v0), new Vector2(u2, v1), new Vector2(u1, v1),
            color);
        // 右上
        batcher.Quad(tex,
            new Vector2(x2, y0), new Vector2(x3, y0), new Vector2(x3, y1), new Vector2(x2, y1),
            new Vector2(u2, v0), new Vector2(u3, v0), new Vector2(u3, v1), new Vector2(u2, v1),
            color);
        // 左中
        batcher.Quad(tex,
            new Vector2(x0, y1), new Vector2(x1, y1), new Vector2(x1, y2), new Vector2(x0, y2),
            new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v2), new Vector2(u0, v2),
            color);
        // 中
        batcher.Quad(tex,
            new Vector2(x1, y1), new Vector2(x2, y1), new Vector2(x2, y2), new Vector2(x1, y2),
            new Vector2(u1, v1), new Vector2(u2, v1), new Vector2(u2, v2), new Vector2(u1, v2),
            color);
        // 右中
        batcher.Quad(tex,
            new Vector2(x2, y1), new Vector2(x3, y1), new Vector2(x3, y2), new Vector2(x2, y2),
            new Vector2(u2, v1), new Vector2(u3, v1), new Vector2(u3, v2), new Vector2(u2, v2),
            color);
        // 左下
        batcher.Quad(tex,
            new Vector2(x0, y2), new Vector2(x1, y2), new Vector2(x1, y3), new Vector2(x0, y3),
            new Vector2(u0, v2), new Vector2(u1, v2), new Vector2(u1, v3), new Vector2(u0, v3),
            color);
        // 下中
        batcher.Quad(tex,
            new Vector2(x1, y2), new Vector2(x2, y2), new Vector2(x2, y3), new Vector2(x1, y3),
            new Vector2(u1, v2), new Vector2(u2, v2), new Vector2(u2, v3), new Vector2(u1, v3),
            color);
        // 右下
        batcher.Quad(tex,
            new Vector2(x2, y2), new Vector2(x3, y2), new Vector2(x3, y3), new Vector2(x2, y3),
            new Vector2(u2, v2), new Vector2(u3, v2), new Vector2(u3, v3), new Vector2(u2, v3),
            color);
    }

    void DrawElementTextShrinkToFit(ElementTextStyle textStyle, Batcher batcher, Vector2 anchor, Vector2 justify)
    {
        var content = textStyle.Content ?? string.Empty;
        var textSize = Assets.Font.SizeOf(content.AsSpan());
        var boxW = worldRect.Width;
        var boxH = worldRect.Height;

        if (boxW <= 0 || textSize.X <= 0 || textSize.Y <= 0)
        {
            batcher.Text(Assets.Font, content.AsSpan(), anchor, justify, textStyle.Color);
            return;
        }

        var scaleX = boxW / textSize.X;
        var scale = scaleX;

        if (boxH > 0)
        {
            var scaleY = boxH / textSize.Y;
            if (scaleY < scale)
                scale = scaleY;
        }

        if (scale >= 1f)
        {
            batcher.Text(Assets.Font, content.AsSpan(), anchor, justify, textStyle.Color);
            return;
        }

        var baseSize = textStyle.Size > 0f ? textStyle.Size : Assets.Font.Size;
        var scaledSize = baseSize * scale;
        if (scaledSize <= 0f)
            return;

        batcher.Text(Assets.Font, content.AsSpan(), anchor, justify, scaledSize, textStyle.Color);
    }

    void DrawElementTextWrap(ElementTextStyle textStyle, Batcher batcher, Vector2 anchor, Vector2 justify)
    {
        var content = textStyle.Content ?? string.Empty;
        var boxW = worldRect.Width;

        if (boxW <= 0)
        {
            batcher.Text(Assets.Font, content.AsSpan(), anchor, justify, textStyle.Color);
            return;
        }

        var size = textStyle.Size > 0f ? textStyle.Size : Assets.Font.Size;
        batcher.TextWrapped(Assets.Font, content.AsSpan(), boxW, anchor, justify, size, textStyle.Color);
    }

    void DrawElementTextShrinkAndWrap(ElementTextStyle textStyle, Batcher batcher, Vector2 anchor, Vector2 justify)
    {
        var content = textStyle.Content ?? string.Empty;
        var boxW = worldRect.Width;
        var boxH = worldRect.Height;

        if (boxW <= 0 || boxH <= 0)
        {
            batcher.Text(Assets.Font, content.AsSpan(), anchor, justify, textStyle.Color);
            return;
        }

        var lines = Assets.Font.WrapText(content.AsSpan(), boxW);
        var lineCount = lines.Count;

        if (lineCount == 0)
        {
            batcher.Text(Assets.Font, content.AsSpan(), anchor, justify, textStyle.Color);
            return;
        }

        var totalHeight = Assets.Font.Height * lineCount + Assets.Font.LineGap * (lineCount - 1);

        if (totalHeight <= boxH)
        {
            batcher.TextWrapped(Assets.Font, content.AsSpan(), boxW, anchor, justify, textStyle.Color);
            return;
        }

        var scale = boxH / totalHeight;
        if (scale <= 0f)
            return;

        batcher.PushMatrix(anchor, Vector2.Zero, Vector2.One * scale, 0f);
        batcher.TextWrapped(Assets.Font, content.AsSpan(), boxW, Vector2.Zero, justify, textStyle.Color);
        batcher.PopMatrix();
    }
    void ApplyTarget(float time)
    {
        if (!animateLayout)
        {
            rect = targetRect;
            posTween = default;
            sizeTween = default;
            return;
        }

        posTween.SetValue(rect.Position,targetRect.Position, time,layoutTransition,Vector2.Lerp);
        sizeTween.SetValue(rect.Size, targetRect.Size, time, layoutTransition, Vector2Lerp);
        posTween.SetDuration(layoutTweenDuration);
        sizeTween.SetDuration(layoutTweenDuration);
    }


    Rect GetWorldRect()
    {
        if (parent == null)
            return rect;

        var p = parent.GetWorldRect();
        return new Rect(p.X + rect.X, p.Y + rect.Y, rect.Width, rect.Height);
    }
}

public class UIImage : UIElement
{
    ElementBackgroundStyle background;

    public ElementBackgroundStyle Background
    {
        get => background;
        set => background = value;
    }

    public Color BackgroundColor
    {
        get => background.Color;
        set
        {
            background.Enabled = true;
            if (background.Mode == ElementBackgroundMode.None)
                background.Mode = ElementBackgroundMode.Color;
            background.Color = value;
        }
    }

    public void SetBackgroundImage(Subtexture subtex, ElementImageFillMode fillMode, Vector4 nineSliceBorder = default)
    {
        SetBackgroundImageInternal(subtex, null, fillMode, nineSliceBorder);
    }

    public void SetBackgroundImage(Texture texture, ElementImageFillMode fillMode, Vector4 nineSliceBorder = default)
    {
        SetBackgroundImageInternal(null, texture, fillMode, nineSliceBorder);
    }

    void SetBackgroundImageInternal(Subtexture? subtex, Texture? texture, ElementImageFillMode fillMode, Vector4 nineSliceBorder)
    {
        background.Enabled = true;
        background.Mode = ElementBackgroundMode.Image;
        background.Subtex = subtex;
        background.Texture = texture;
        background.ImageFillMode = fillMode;
        background.NineSliceBorder = nineSliceBorder;
        if (background.Color.A == 0)
            background.Color = Color.White;
    }

    public UIImage(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
        : base(maskable, selectable, visible, rect, parent)
    {
        Selectable = false;
    }

    public UIImage(Rect rect, UIElement? parent = null)
        : this(maskable: true, selectable: true, visible: true, rect: rect, parent: parent)
    {
    }

    public UIImage() : this(new Rect(0, 0, 0, 0))
    {
    }

    protected override UIElement CreateCloneInstance()
    {
        return new UIImage(maskable, selectable, visible, rect);
    }

    protected override void CopyToClone(UIElement target, bool cloneChildren)
    {
        base.CopyToClone(target, cloneChildren);

        if (target is UIImage image)
            image.background = background;
    }

    protected internal override void CollectDrawCommandsInternal(List<UIDrawCommand> commands, int depth, int group, Matrix3x2 parentMatrix)
    {
        if (!TryPrepareCollectDrawCommands(parentMatrix, out var matrix))
            return;

        var rendered = background.Enabled && background.Mode != ElementBackgroundMode.None;
        if (rendered)
            commands.Add(new UIDrawCommand(UIDrawCommandType.Background, RenderBackground, depth, group, matrix));

        var nextDepth = rendered ? depth + 1 : depth;
        CollectDrawCommandsForChildren(commands, nextDepth, group, matrix);
    }

    protected virtual void RenderBackground(Batcher batcher)
    {
        DrawBackgroundInternal(batcher, background);
    }
}

public class UIText : UIElement
{
    ElementTextStyle textStyle;

    public ElementTextStyle TextStyle
    {
        get => textStyle;
        set => textStyle = value;
    }

    public string Text
    {
        get => textStyle.Content;
        set
        {
            textStyle.Enabled = !string.IsNullOrEmpty(value);
            textStyle.Content = value ?? string.Empty;
        }
    }

    public Color TextColor
    {
        get => textStyle.Color;
        set
        {
            textStyle.Enabled = true;
            textStyle.Color = value;
        }
    }

    public Vector2 TextAlign
    {
        get => textStyle.Align;
        set
        {
            textStyle.Enabled = true;
            textStyle.Align = value;
        }
    }

    public float TextSize
    {
        get => textStyle.Size;
        set
        {
            textStyle.Enabled = true;
            textStyle.Size = value;
        }
    }

    public ElementTextOverflowMode TextOverflow
    {
        get => textStyle.OverflowMode;
        set
        {
            textStyle.Enabled = true;
            textStyle.OverflowMode = value;
        }
    }

    public void ConfigureTextStyle(Func<ElementTextStyle, ElementTextStyle> configure)
    {
        textStyle = configure(textStyle);
    }

    public UIText(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
        : base(maskable, selectable, visible, rect, parent)
    {
        Selectable = false;
    }

    public UIText(Rect rect, UIElement? parent = null)
        : this(maskable: true, selectable: false, visible: true, rect: rect, parent: parent)
    {
    }

    public UIText() : this(new Rect(0, 0, 0, 0))
    {
    }

    protected override UIElement CreateCloneInstance()
    {
        return new UIText(maskable, selectable, visible, rect);
    }

    protected override void CopyToClone(UIElement target, bool cloneChildren)
    {
        base.CopyToClone(target, cloneChildren);

        if (target is UIText text)
            text.textStyle = textStyle;
    }

    protected internal override void CollectDrawCommandsInternal(List<UIDrawCommand> commands, int depth, int group, Matrix3x2 parentMatrix)
    {
        if (!TryPrepareCollectDrawCommands(parentMatrix, out var matrix))
            return;

        var rendered = textStyle.Enabled && !string.IsNullOrEmpty(textStyle.Content) && Assets.Font != null;
        if (rendered)
            commands.Add(new UIDrawCommand(UIDrawCommandType.Text, RenderText, depth, group, matrix));

        var nextDepth = rendered ? depth + 1 : depth;
        CollectDrawCommandsForChildren(commands, nextDepth, group, matrix);
    }

    protected virtual void RenderText(Batcher batcher)
    {
        DrawTextInternal(batcher, textStyle);
    }
}