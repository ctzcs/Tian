using System;
using System.Numerics;
using Engine.UI;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Content.Source.Test_Ui;

public sealed class UiTestRotationPanel
{
    private readonly UIElement rotationDemo;
    private readonly UIElement rotationStatus;

    private bool rotationAuto = true;
    private float rotationSpeed = 1.6f;
    private float rotationAmplitude = 0.6f;
    private float rotationBase;
    private Vector2 rotationPivot = new(0.5f, 0.5f);

    public VerticalGroup Root { get; }

    public UiTestRotationPanel(UIRoot uiRoot)
    {
        
        //从子元素向父元素构建
        var title = new UIElement(new Rect(0, 0, 0, 40))
            .WithBackgroundColor(Rgb(45, 45, 55))
            .WithText("Rotation Demo")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(22);

        
        var btnAuto = new Button(new Rect(0, 0, 0, 36))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(70, 70, 90))
            .WithText("Auto")
            .WithClick((b) => rotationAuto = !rotationAuto);

        var btnSpeedDown = new Button(new Rect(0, 0, 0, 36))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(55, 55, 65))
            .WithText("Speed-")
            .WithClick((b) => rotationSpeed = MathF.Max(0f, rotationSpeed - 0.3f));

        var btnSpeedUp = new Button(new Rect(0, 0, 0, 36))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(55, 55, 65))
            .WithText("Speed+")
            .WithClick((b) => rotationSpeed += 0.3f);
        
        var controls1 = new HorizontalGroup()
            .WithRect(new Rect(0, 0, 0, 40))
            .WithChildGap(8)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Middle)
            .WithAutoSize(autoWidth: false, autoHeight: true)
            .WithChildren(btnAuto, btnSpeedDown, btnSpeedUp);
        
        var btnPivotTL = new Button(new Rect(0, 0, 0, 36))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(55, 55, 65))
            .WithText("Pivot TL")
            .WithClick((b) => rotationPivot = new Vector2(0f, 0f));

        var btnPivotCenter = new Button(new Rect(0, 0, 0, 36))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(55, 55, 65))
            .WithText("Pivot C")
            .WithClick((b) => rotationPivot = new Vector2(0.5f, 0.5f));

        var btnPivotBR = new Button(new Rect(0, 0, 0, 36))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(55, 55, 65))
            .WithText("Pivot BR")
            .WithClick((b) => rotationPivot = new Vector2(1f, 1f));

        var controls2 = new HorizontalGroup()
            .WithRect(new Rect(0, 0, 0, 40))
            .WithChildGap(8)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Middle)
            .WithAutoSize(autoWidth: false, autoHeight: true)
            .WithChildren(btnPivotTL, btnPivotCenter, btnPivotBR);
        

        rotationDemo = new UIElement(new Rect(0, 0, 320, 220))
            .WithBackgroundColor(Rgb(34, 34, 40));

        var demoText = new UIElement(new Rect(0, 0, 320, 220))
            .WithText("Parent rotates → child should follow")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(20);

        var childBadge = new UIElement(new Rect(18, 18, 160, 46))
            .WithBackgroundColor(Rgb(120, 70, 50))
            .WithText("Child")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(18);

        var nestedDot = new UIElement(new Rect(6, 6, 18, 18))
            .WithBackgroundColor(Rgb(240, 220, 90));

        childBadge.WithChild(nestedDot);
        rotationDemo.WithChildren(demoText, childBadge);

        rotationStatus = new UIElement(new Rect(0, 0, 0, 32))
            .WithBackgroundColor(Rgb(40, 40, 48))
            .WithTextColor(Rgb(230, 230, 230))
            .WithTextAlign(new Vector2(0.0f, 0.5f))
            .WithTextSize(16);

        Root = new VerticalGroup()
            .WithRect(new Rect(0, 0, 360, 0))
            .WithBackgroundColor(Rgb(26, 26, 30))
            .WithPadding(12)
            .WithChildGap(10)
            .WithAutoSize(autoWidth: false, autoHeight: false)
            .WithChildren(title, controls1, controls2, rotationDemo, rotationStatus);
    }

    public void Update(float timeSeconds)
    {
        var rotation = rotationAuto
            ? MathF.Sin(timeSeconds * rotationSpeed) * rotationAmplitude + rotationBase
            : rotationBase;

        rotationDemo.Rotation = rotation;
        rotationDemo.RotationPivot = rotationPivot;

        rotationStatus.Text = $"Rotation: {rotation:0.00} rad | Pivot: {rotationPivot.X:0.00},{rotationPivot.Y:0.00} | Auto: {(rotationAuto ? "On" : "Off")}";
    }

    private static Color Rgb(byte r, byte g, byte b)
        => new Color(r, g, b, 255);
}