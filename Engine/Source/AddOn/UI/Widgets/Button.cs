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
    ButtonImage backgroundElement;
    ButtonText textElement;

    public Button(bool maskable, bool selectable, bool visible, Rect rect, UIElement? parent = null)
        : base(maskable, selectable, visible, rect, parent)
    {
        var childRect = new Rect(0, 0, rect.Width, rect.Height);
        backgroundElement = new ButtonImage(this, childRect);
        textElement = new ButtonText(this, childRect);
        textElement.TextColor = Color.White;
        textElement.TextAlign = new Vector2(0.5f, 0.5f);
        textElement.TextOverflow = ElementTextOverflowMode.ShrinkAndWrap;
        AddChild(backgroundElement);
        AddChild(textElement);
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
        base.CopyToClone(target, false);

        if (target is Button btn)
        {
            btn.backgroundElement.Background = backgroundElement.Background;
            btn.textElement.TextStyle = textElement.TextStyle;
            btn._mat = _mat;
            btn.Enter = Enter;
            btn.Click = Click;
            btn.Exit = Exit;
            btn.Hover = Hover;

            if (cloneChildren)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child == backgroundElement || child == textElement)
                        continue;
                    var childClone = child.Clone(true);
                    btn.AddChild(childClone);
                }
            }
        }
    }

    protected override void UpdateLayout()
    {
        base.UpdateLayout();
        var childRect = new Rect(0, 0, rect.Width, rect.Height);
        if (backgroundElement.Rect != childRect)
            backgroundElement.Rect = childRect;
        if (textElement.Rect != childRect)
            textElement.Rect = childRect;
    }

    public ElementBackgroundStyle Background
    {
        get => backgroundElement.Background;
        set => backgroundElement.Background = value;
    }

    public Color BackgroundColor
    {
        get => backgroundElement.BackgroundColor;
        set => backgroundElement.BackgroundColor = value;
    }

    public void SetBackgroundImage(Subtexture subtex, ElementImageFillMode fillMode, Vector4 nineSliceBorder = default)
    {
        backgroundElement.SetBackgroundImage(subtex, fillMode, nineSliceBorder);
    }

    public void SetBackgroundImage(Texture texture, ElementImageFillMode fillMode, Vector4 nineSliceBorder = default)
    {
        backgroundElement.SetBackgroundImage(texture, fillMode, nineSliceBorder);
    }

    public ElementTextStyle TextStyle
    {
        get => textElement.TextStyle;
        set => textElement.TextStyle = value;
    }

    public string Text
    {
        get => textElement.Text;
        set => textElement.Text = value;
    }

    public Color TextColor
    {
        get => textElement.TextColor;
        set => textElement.TextColor = value;
    }

    public Vector2 TextAlign
    {
        get => textElement.TextAlign;
        set => textElement.TextAlign = value;
    }

    public float TextSize
    {
        get => textElement.TextSize;
        set => textElement.TextSize = value;
    }

    public ElementTextOverflowMode TextOverflow
    {
        get => textElement.TextOverflow;
        set => textElement.TextOverflow = value;
    }

    public void ConfigureTextStyle(Func<ElementTextStyle, ElementTextStyle> configure)
    {
        textElement.TextStyle = configure(textElement.TextStyle);
    }

    public void OnPointerEnter(UiFrame state)
    {
        _mouseOver = true;
        BackgroundColor = Color.Red;
        InvokeEnter();
    }
    
    public void OnPointerExit(UiFrame state)
    {
        _mouseOver = false;
        BackgroundColor = Color.White;
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

    sealed class ButtonImage : UIImage
    {
        readonly Button owner;

        public ButtonImage(Button owner, Rect rect)
            : base(rect)
        {
            this.owner = owner;
        }

        protected override void RenderBackground(Batcher batcher)
        {
            if (owner._mat == null)
            {
                base.RenderBackground(batcher);
                return;
            }

            batcher.PushMaterial(owner._mat);
            base.RenderBackground(batcher);
            batcher.PopMaterial();
        }
    }

    sealed class ButtonText : UIText
    {
        readonly Button owner;

        public ButtonText(Button owner, Rect rect)
            : base(rect)
        {
            this.owner = owner;
        }

        protected override void RenderText(Batcher batcher)
        {
            if (owner._mat == null)
            {
                base.RenderText(batcher);
                return;
            }

            batcher.PushMaterial(owner._mat);
            base.RenderText(batcher);
            batcher.PopMaterial();
        }
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