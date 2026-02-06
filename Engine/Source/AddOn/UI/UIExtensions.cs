using System;
using System.Numerics;
using Engine.Core;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Engine.UI;

public static class UIFluentExtensions
{
    /// <summary>
    /// 设置背景颜色并返回自身，便于链式调用。
    /// </summary>
    public static T WithBackgroundColor<T>(this T element, Color color)
        where T : UIElement
    {
        if (element is UIImage image)
            image.BackgroundColor = color;
        else if (element is Button button)
            button.BackgroundColor = color;
        return element;
    }

    /// <summary>
    /// 使用子纹理设置背景（九宫格 / 拉伸等），返回自身。
    /// </summary>
    public static T WithBackgroundImage<T>(
        this T element,
        Subtexture subtex,
        ElementImageFillMode fillMode = ElementImageFillMode.Stretch,
        Vector4 nineSliceBorder = default)
        where T : UIElement
    {
        if (element is UIImage image)
            image.SetBackgroundImage(subtex, fillMode, nineSliceBorder);
        else if (element is Button button)
            button.SetBackgroundImage(subtex, fillMode, nineSliceBorder);
        return element;
    }

    /// <summary>
    /// 使用整张 Texture 作为背景，返回自身。
    /// </summary>
    public static T WithBackgroundImage<T>(
        this T element,
        Texture texture,
        ElementImageFillMode fillMode = ElementImageFillMode.Stretch,
        Vector4 nineSliceBorder = default)
        where T : UIElement
    {
        if (element is UIImage image)
            image.SetBackgroundImage(texture, fillMode, nineSliceBorder);
        else if (element is Button button)
            button.SetBackgroundImage(texture, fillMode, nineSliceBorder);
        return element;
    }

    /// <summary>
    /// 修改 LayoutConfig，并返回自身。configure 收到的是当前 Layout，返回修改后的 Layout。
    /// </summary>
    public static T WithLayout<T>(
        this T group,
        Func<LayoutConfig, LayoutConfig> configure)
        where T : UILayoutGroup
    {
        var cfg = group.Layout;
        cfg = configure(cfg);
        group.Layout = cfg;
        return group;
    }

    public static T WithPadding<T>(this T group, float left, float top, float right, float bottom)
        where T : UILayoutGroup
    {
        var cfg = group.Layout;
        cfg.PaddingLeft = left;
        cfg.PaddingTop = top;
        cfg.PaddingRight = right;
        cfg.PaddingBottom = bottom;
        group.Layout = cfg;
        return group;
    }

    public static T WithPadding<T>(this T group, float all)
        where T : UILayoutGroup
        => group.WithPadding(all, all, all, all);

    public static T WithChildGap<T>(this T group, float gap)
        where T : UILayoutGroup
    {
        var cfg = group.Layout;
        cfg.ChildGap = gap;
        group.Layout = cfg;
        return group;
    }

    public static T WithAlign<T>(this T group, HorizontalAlignment x, VerticalAlignment y)
        where T : UILayoutGroup
    {
        var cfg = group.Layout;
        cfg.AlignX = x;
        cfg.AlignY = y;
        group.Layout = cfg;
        return group;
    }

    public static T WithAutoSize<T>(this T group, bool autoWidth = false, bool autoHeight = true)
        where T : UILayoutGroup
    {
        var cfg = group.Layout;
        cfg.AutoWidth = autoWidth;
        cfg.AutoHeight = autoHeight;
        group.Layout = cfg;
        return group;
    }

    public static T WithViewportRatio<T>(this T element, Rect normalizedRect)
        where T : UIElement
    {
        element.SizeMode = UISizeMode.ViewportRatio;
        element.NormalizedRect = normalizedRect;
        return element;
    }

    public static TParent WithChildren<TParent>(this TParent parent, params UIElement[] children)
        where TParent : UIElement
    {
        for (int i = 0; i < children.Length; i++)
            parent.AddChild(children[i]);
        return parent;
    }

    /// <summary>
    /// 配置布局动画：时间和缓动曲线，返回自身方便链式调用。
    /// </summary>
    public static T WithLayoutAnimation<T>(
        this T element,
        float duration,
        Transition transition = Transition.EaseOut)
        where T : UIElement
    {
        element.LayoutTweenDuration = duration;
        element.LayoutTransition = transition;
        return element;
    }

    public static T WithRect<T>(this T element, Rect rect)
        where T : UIElement
    {
        element.Rect = rect;
        return element;
    }

    public static T WithRotation<T>(this T element, float rotation)
        where T : UIElement
    {
        element.Rotation = rotation;
        return element;
    }

    public static T WithRotationPivot<T>(this T element, Vector2 pivot)
        where T : UIElement
    {
        element.RotationPivot = pivot;
        return element;
    }

    public static T WithTextColor<T>(this T element, Color color)
        where T : UIElement
    {
        if (element is UIText text)
            text.TextColor = color;
        else if (element is Button button)
            button.TextColor = color;
        return element;
    }

    public static T WithTextAlign<T>(this T element, Vector2 align)
        where T : UIElement
    {
        if (element is UIText text)
            text.TextAlign = align;
        else if (element is Button button)
            button.TextAlign = align;
        return element;
    }

    public static T WithTextSize<T>(this T element, float size)
        where T : UIElement
    {
        if (element is UIText text)
            text.TextSize = size;
        else if (element is Button button)
            button.TextSize = size;
        return element;
    }

    public static T WithTextOverflow<T>(this T element, ElementTextOverflowMode mode)
        where T : UIElement
    {
        if (element is UIText text)
            text.TextOverflow = mode;
        else if (element is Button button)
            button.TextOverflow = mode;
        return element;
    }

    public static T WithTextStyle<T>(
        this T element,
        Func<ElementTextStyle, ElementTextStyle> configure)
        where T : UIElement
    {
        if (element is UIText text)
            text.ConfigureTextStyle(configure);
        else if (element is Button button)
            button.ConfigureTextStyle(configure);
        return element;
    }

    public static T WithText<T>(this T element, string content)
        where T : UIElement
    {
        if (element is UIText text)
            text.Text = content;
        else if (element is Button button)
            button.Text = content;
        return element;
    }

    public static T WithWidthRatioToParent<T>(this T element, float ratio)
        where T : UIElement
    {
        element.WidthRatioToParent = ratio;
        return element;
    }

    public static T WithHeightRatioToParent<T>(this T element, float ratio)
        where T : UIElement
    {
        element.HeightRatioToParent = ratio;
        return element;
    }

    public static T WithXRatioToParent<T>(this T element, float ratio)
        where T : UIElement
    {
        element.XRatioToParent = ratio;
        return element;
    }

    public static T WithYRatioToParent<T>(this T element, float ratio)
        where T : UIElement
    {
        element.YRatioToParent = ratio;
        return element;
    }

    /// <summary>
    /// 抢剩余空间的权重
    /// </summary>
    /// <param name="element"></param>
    /// <param name="grow"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T WithGrowX<T>(this T element, float grow)
        where T : UIElement
    {
        element.GrowX = grow;
        return element;
    }

    public static T WithGrowY<T>(this T element, float grow)
        where T : UIElement
    {
        element.GrowY = grow;
        return element;
    }

    public static T WithMinWidth<T>(this T element, float value)
        where T : UIElement
    {
        element.MinWidth = value;
        return element;
    }

    public static T WithMaxWidth<T>(this T element, float value)
        where T : UIElement
    {
        element.MaxWidth = value;
        return element;
    }

    public static T WithMinHeight<T>(this T element, float value)
        where T : UIElement
    {
        element.MinHeight = value;
        return element;
    }

    public static T WithMaxHeight<T>(this T element, float value)
        where T : UIElement
    {
        element.MaxHeight = value;
        return element;
    }

    /// <summary>
    /// 添加子元素并返回父节点，方便链式 AddChild。
    /// </summary>
    public static TParent WithChild<TParent>(
        this TParent parent,
        UIElement child)
        where TParent : UIElement
    {
        parent.AddChild(child);
        return parent;
    }
}