using System.Numerics;
using Engine.UI;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Content.Source.Test_Ui;

public sealed class UiTestSliderPanel
{
    public VerticalGroup Root { get; }

    public UiTestSliderPanel(UIRoot uiRoot)
    {
        Root = new VerticalGroup()
            .WithRect(new Rect(0, 0, 0, 0))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(26, 26, 30))
            .WithPadding(12)
            .WithChildGap(12)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Top)
            .WithAutoSize(autoWidth: false, autoHeight: true);
        Root.AnimateLayout = false;
        Root.Maskable = false;

        var title = new UIElement(new Rect(0, 0, 0, 40))
            .WithBackgroundColor(Rgb(45, 45, 55))
            .WithText("Slider Panel")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(22);

        var sliderHLabel = new UIElement(new Rect(0, 0, 110, 32))
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithText("Horizontal")
            .WithTextColor(Rgb(220, 220, 220))
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(16);

        var sliderHValueLabel = new UIElement(new Rect(0, 0, 120, 32))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithText("Value: 35.00")
            .WithTextColor(Rgb(220, 220, 220))
            .WithTextAlign(new Vector2(0.1f, 0.5f))
            .WithTextSize(16);

        var sliderH = new SliderBar(new Rect(0, 0, 0, 20))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(34, 34, 40));
        sliderH.Min = 0f;
        sliderH.Max = 100f;
        sliderH.ThumbSize = 24f;
        sliderH.Track.BackgroundColor = Rgb(60, 60, 72);
        sliderH.Fill.BackgroundColor = Rgb(120, 170, 255);
        sliderH.Thumb.BackgroundColor = Rgb(230, 230, 240);
        sliderH.SetValue(35f, notify: false);
        sliderH.ValueChanged += (_, v) => sliderHValueLabel.Text = $"Value: {v:0.00}";

        var sliderHRow = new HorizontalGroup()
            .WithRect(new Rect(0, 0, 0, 36))
            .WithGrowX(1)
            .WithChildGap(12)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Middle)
            .WithAutoSize(autoWidth: false, autoHeight: false)
            .WithChildren(sliderHLabel, sliderH, sliderHValueLabel);
        sliderHRow.AnimateLayout = false;

        var sliderVLabel = new UIElement(new Rect(0, 0, 140, 32))
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithText("Vertical")
            .WithTextColor(Rgb(220, 220, 220))
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(16);

        var sliderVValueLabel = new UIElement(new Rect(0, 0, 140, 32))
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithText("Value: 0.60")
            .WithTextColor(Rgb(220, 220, 220))
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(16);

        var sliderV = new SliderBar(new Rect(0, 0, 28, 160))
            .WithBackgroundColor(Rgb(34, 34, 40));
        sliderV.Direction = SliderDirection.Vertical;
        sliderV.Min = 0f;
        sliderV.Max = 1f;
        sliderV.ThumbSize = 24f;
        sliderV.Track.BackgroundColor = Rgb(60, 60, 72);
        sliderV.Fill.BackgroundColor = Rgb(120, 170, 255);
        sliderV.Thumb.BackgroundColor = Rgb(230, 230, 240);
        sliderV.SetValue(0.6f, notify: false);
        sliderV.ValueChanged += (_, v) => sliderVValueLabel.Text = $"Value: {v:0.00}";

        var sliderVColumn = new VerticalGroup()
            .WithRect(new Rect(0, 0, 180, 0))
            .WithChildGap(8)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Top)
            .WithAutoSize(autoWidth: false, autoHeight: true)
            .WithChildren(sliderVLabel, sliderV, sliderVValueLabel);
        sliderVColumn.AnimateLayout = false;

        var content = new VerticalGroup()
            .WithRect(new Rect(0, 0, 0, 0))
            .WithGrowX(1)
            .WithPadding(16)
            .WithChildGap(18)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Top)
            .WithAutoSize(autoWidth: false, autoHeight: true)
            .WithBackgroundColor(Rgb(32, 32, 38))
            .WithChildren(sliderHRow, sliderVColumn);
        content.AnimateLayout = false;
        content.Maskable = false;

        Root.WithChildren(title, content);
    }

    private static Color Rgb(byte r, byte g, byte b)
        => new Color(r, g, b, 255);
}