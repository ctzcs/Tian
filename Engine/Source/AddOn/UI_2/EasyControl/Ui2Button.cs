using System;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public class Ui2Button : UIElement
{
    public UIText Label { get; }
    public UIImage Image { get; }

    public Color NormalColor { get; set; }
    public Color HoverColor { get; set; }
    public Color PressedColor { get; set; }
    public Color DisabledColor { get; set; }

    public bool Enabled
    {
        get => Interactable;
        set
        {
            Interactable = value;
            UpdateVisual();
        }
    }

    public event Action<Ui2Button>? Clicked;

    bool isHovered;
    bool isPressed;

    public Ui2Button()
    {
        Interactable = true;
        BackgroundEnabled = true;

        NormalColor = new Color(0x4A4A4A);
        HoverColor = new Color(0x5A5A5A);
        PressedColor = new Color(0x3A3A3A);
        DisabledColor = new Color(0x333333);
        BackgroundColor = NormalColor;

        Image = new UIImage
        {
            Interactable = false
        };
        Image.PointerPassThrough = true;
        AddChild(Image);

        Label = new UIText
        {
            Interactable = false
        };
        Label.Align = new Vector2(0.5f, 0.5f);
        Label.TextColor = Color.White;
        Label.TextSize = 16f;
        Label.PointerPassThrough = true;
        AddChild(Label);

        OnPointerEnter += HandlePointerEnter;
        OnPointerExit += HandlePointerExit;
        OnPointerDown += HandlePointerDown;
        OnPointerUp += HandlePointerUp;
        OnClick += HandleClick;

        Image.OnPointerEnter += HandlePointerEnter;
        Image.OnPointerExit += HandlePointerExit;

        Label.OnPointerEnter += HandlePointerEnter;
        Label.OnPointerExit += HandlePointerExit;
    }

    public override void Arrange(Rect rect)
    {
        base.Arrange(rect);

        var contentRect = new Rect(0f, 0f, rect.Width, rect.Height);
        Image.Arrange(contentRect);
        Label.Arrange(contentRect);
    }

    void HandlePointerEnter(Ui2PointerEvent e)
    {
        isHovered = true;
        UpdateVisual();
    }

    void HandlePointerExit(Ui2PointerEvent e)
    {
        isHovered = false;
        isPressed = false;
        UpdateVisual();
    }

    void HandlePointerDown(Ui2PointerEvent e)
    {
        if (!Enabled)
            return;

        isPressed = true;
        UpdateVisual();
    }

    void HandlePointerUp(Ui2PointerEvent e)
    {
        isPressed = false;
        UpdateVisual();
    }

    void HandleClick(Ui2PointerEvent e)
    {
        if (!Enabled)
            return;

        Clicked?.Invoke(this);
    }

    void UpdateVisual()
    {
        if (!Enabled)
            BackgroundColor = DisabledColor;
        else if (isPressed)
            BackgroundColor = PressedColor;
        else if (isHovered)
            BackgroundColor = HoverColor;
        else
            BackgroundColor = NormalColor;
    }

    public Ui2Button WithText(string text)
    {
        Label.Text = text;
        return this;
    }

    public Ui2Button WithTextColor(Color color)
    {
        Label.TextColor = color;
        return this;
    }

    public Ui2Button WithImageSubtexture(Subtexture subtexture)
    {
        Image.Subtexture = subtexture;
        return this;
    }

    public Ui2Button WithImageTint(Color color)
    {
        Image.Tint = color;
        return this;
    }

    public Ui2Button WithClick(Action<Ui2Button> handler)
    {
        Clicked += handler;
        return this;
    }

    public Ui2Button WithColors(Color normal, Color hover, Color pressed, Color disabled)
    {
        NormalColor = normal;
        HoverColor = hover;
        PressedColor = pressed;
        DisabledColor = disabled;
        UpdateVisual();
        return this;
    }
}
