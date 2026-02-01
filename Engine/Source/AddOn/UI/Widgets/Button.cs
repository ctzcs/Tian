using System;
using System.Numerics;
using Engine.Asset;
using Foster.Framework;

namespace Engine.UI;

/// <summary>
/// 默认版本，不绑定数据
/// </summary>
/// <param name="maskable"></param>
/// <param name="selectable"></param>
/// <param name="visible"></param>
/// <param name="rect"></param>
/// <param name="parent"></param>
public class Button : UIElement, IInputListener
{
    public Button(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
        : base(maskable, selectable, visible, rect, parent)
    {
        textStyle.Color = Color.White;
        textStyle.Align = new Vector2(0.5f, 0.5f);
        textStyle.OverflowMode = ElementTextOverflowMode.ShrinkAndWrap;
    }

    public Button(Rect rect, UIElement? parent = null)
        : this(maskable: true, selectable: true, visible: true, rect: rect, parent: parent)
    {
    }

    public Button() : this(new Rect(0, 0, 0, 0))
    {
    }

    /// <summary>
    /// 是否在UI上
    /// </summary>
    protected bool _mouseOver;
    /// <summary>
    /// 是否按下
    /// </summary>
    protected bool _mouseDown;
    public Material? _mat;
    public event Action<Button>? Enter;
    public event Action<Button>? Click;
    public event Action<Button>? Exit;
    public event Action<Button>? Hover;

    protected virtual void InvokeEnter() => Enter?.Invoke(this);

    protected virtual void InvokeExit() => Exit?.Invoke(this);

    protected virtual void InvokeHover() => Hover?.Invoke(this);

    protected virtual void InvokeClick() => Click?.Invoke(this);

    protected override UIElement CreateCloneInstance()
    {
        return new Button(maskable, selectable, visible, rect);
    }

    protected override void CopyToClone(UIElement target, bool cloneChildren)
    {
        base.CopyToClone(target, cloneChildren);

        if (target is Button btn)
        {
            btn._mat = _mat;
            btn.Enter = Enter;
            btn.Click = Click;
            btn.Exit = Exit;
            btn.Hover = Hover;
        }
    }

    public void OnPointerEnter(UiFrame state)
    {
        _mouseOver = true;
        base.BackgroundColor = Color.Red;
        InvokeEnter();
    }
    
    public void OnPointerExit(UiFrame state)
    {
        _mouseOver = false;
        base.BackgroundColor = Color.White;
        InvokeExit();
    }

    public void OnPointerHover(UiFrame state)
    {
        if (_mouseOver)
            InvokeHover();
    }


    public Button WithData(object? data)
    {
        return this;
    }

    /// <summary>
    /// 同时设置 BindData 并注册 Click 回调。
    /// </summary>
    public Button WithClick(Action<Button> handler)
    {
    
        Click += handler;
        return this;
    }

    /// <summary>
    /// 同时设置 BindData 并注册 Click 回调。
    /// </summary>
    public Button WithHover(Action<Button> handler)
    {
        Hover += handler;
        return this;
    }

    public Button WithExit(Action<Button> handler)
    {
        Exit += handler;
        return this;
    }

    public Button WithEnter(Action<Button> handler)
    {
        Enter += handler;
        return this;
    }

    bool IInputListener.OnPointerDown(UiFrame state)
    {
        if (isDisabled)
            return false;

        _mouseDown = true;
        return true;
    }

    bool IInputListener.OnRightPointerDown(UiFrame state)
    {
        if (isDisabled)
            return false;

        // 目前仅左键触发 Click，右键按下不改变左键状态
        return false;
    }

    void IInputListener.OnPointerUp(UiFrame state)
    {
        if (isDisabled)
            return;

        if (_mouseDown)
        {
            // 松开时仍在按钮上，认为是一次点击
            if (_mouseOver)
                InvokeClick();

            _mouseDown = false;
        }
    }

    void IInputListener.OnRightPointerUp(UiFrame state)
    {
        // 如需右键点击事件，可以在这里扩展
    }

    protected internal override void DrawBackground(Batcher batcher)
    {
        if (_mat == null)
        {
            base.DrawBackground(batcher);
            return;
        }

        batcher.PushMaterial(_mat);
        base.DrawBackground(batcher);
        batcher.PopMaterial();
    }

    protected internal override void DrawText(Batcher batcher)
    {
        if (_mat == null)
        {
            base.DrawText(batcher);
            return;
        }

        batcher.PushMaterial(_mat);
        base.DrawText(batcher);
        batcher.PopMaterial();
    }
}

/// <summary>
/// 绑定数据版本
/// </summary>
/// <param name="maskable"></param>
/// <param name="selectable"></param>
/// <param name="visible"></param>
/// <param name="rect"></param>
/// <param name="parent"></param>
/// <typeparam name="T"></typeparam>
public sealed class Button<T>(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
    : Button(maskable, selectable, visible, rect, parent)
{
    public T BindData;

    public new event Action<Button<T>, T>? Enter;
    public new event Action<Button<T>, T>? Click;
    public new event Action<Button<T>, T>? Exit;
    public new event Action<Button<T>, T>? Hover;

    public Button(Rect rect, UIElement? parent = null)
        : this(maskable: true, selectable: true, visible: true, rect: rect, parent: parent)
    {
    }

    public Button() : this(new Rect(0, 0, 0, 0))
    {
    }

    public Button<T> WithData(T data)
    {
        BindData = data;
        return this;
    }

    public Button<T> WithClick(Action<Button<T>, T> handler)
    {
        Click += handler;
        return this;
    }

    public Button<T> WithHover(Action<Button<T>, T> handler)
    {
        Hover += handler;
        return this;
    }

    public Button<T> WithExit(Action<Button<T>, T> handler)
    {
        Exit += handler;
        return this;
    }

    public Button<T> WithEnter(Action<Button<T>, T> handler)
    {
        Enter += handler;
        return this;
    }

    protected override void InvokeEnter() => Enter?.Invoke(this, BindData);

    protected override void InvokeExit() => Exit?.Invoke(this, BindData);

    protected override void InvokeHover() => Hover?.Invoke(this, BindData);

    protected override void InvokeClick() => Click?.Invoke(this, BindData);

    protected override UIElement CreateCloneInstance()
    {
        return new Button<T>(maskable, selectable, visible, rect);
    }

    protected override void CopyToClone(UIElement target, bool cloneChildren)
    {
        base.CopyToClone(target, cloneChildren);

        if (target is Button<T> btn)
        {
            btn.BindData = BindData;
            btn.Enter = Enter;
            btn.Click = Click;
            btn.Exit = Exit;
            btn.Hover = Hover;
        }
    }
}