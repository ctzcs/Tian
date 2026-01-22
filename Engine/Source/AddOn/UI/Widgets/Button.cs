using System;
using System.Numerics;
using Engine.Asset;
using Foster.Framework;

namespace Engine.UI;

public class Button(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null) 
    : UIElement(maskable, selectable, visible, rect, parent),IInputListener
{
    /// <summary>
    /// 是否在UI上
    /// </summary>
    protected bool _mouseOver;
    /// <summary>
    /// 是否按下
    /// </summary>
    protected bool _mouseDown;
    bool _textStyleInitialized;
    public Material? _mat;
    public object? BindData;
    public event Action<Button,object>? Enter;
    public event Action<Button,object>? Click;
    public event Action<Button,object>? Exit;
    public event Action<Button,object>? Hover;

    protected override UIElement CreateCloneInstance()
    {
        return new Button(maskable, selectable, visible, rect);
    }

    protected override void CopyToClone(UIElement target, bool cloneChildren)
    {
        base.CopyToClone(target, cloneChildren);

        if (target is Button btn)
        {
            btn._textStyleInitialized = _textStyleInitialized;
            btn._mat = _mat;
            btn.BindData = BindData;
            btn.Enter = Enter;
            btn.Click = Click;
            btn.Exit = Exit;
            btn.Hover = Hover;
        }
    }

    public string Text
    {
        get => textStyle.Content;
        set
        {
            textStyle.Enabled = !string.IsNullOrEmpty(value);
            textStyle.Content = value ?? string.Empty;
            if (!_textStyleInitialized)
            {
                textStyle.Color = Color.White;
                textStyle.Align = new Vector2(0.5f, 0.5f);
                textStyle.OverflowMode = ElementTextOverflowMode.ShrinkAndWrap;
                _textStyleInitialized = true;
            }
        }
    }
    
    public void OnPointerEnter(UiFrame state)
    {
        _mouseOver = true;
        base.BackgroundColor = Color.Red;
        Enter?.Invoke(this,BindData);
        Log.Info("OnPointerEnter");
    }
    
    public void OnPointerExit(UiFrame state)
    {
        _mouseOver = false;
        base.BackgroundColor = Color.White;
        Exit?.Invoke(this,BindData);
        Log.Info("OnPointerExit");
    }

    public void OnPointerHover(UiFrame state)
    {
        if (_mouseOver)
            Hover?.Invoke(this,BindData);
    }


    public Button WithData(object? data){
        BindData = data;
        return this;
    }

    /// <summary>
    /// 同时设置 BindData 并注册 Click 回调。
    /// </summary>
    public Button WithClick(Action<Button, object?> handler)
    {
    
        Click += handler;
        return this;
    }

    /// <summary>
    /// 同时设置 BindData 并注册 Click 回调。
    /// </summary>
    public Button WithHover(Action<Button, object?> handler)
    {
        Click += handler;
        return this;
    }

    public Button WithExit(Action<Button, object?> handler)
    {
        Exit += handler;
        return this;
    }

    public Button WithEnter(Action<Button, object?> handler)
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
                Click?.Invoke(this,BindData);

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