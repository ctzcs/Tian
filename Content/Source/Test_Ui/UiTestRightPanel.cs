using System.Numerics;
using Engine.UI;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Content.Source.Test_Ui;

public sealed class UiTestRightPanel
{
    public VerticalGroup Root { get; }

    public UiTestRightPanel(UIRoot uiRoot)
    {
        Root = new VerticalGroup()
            .WithRect(new Rect(0, 0, 0, 0))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(26, 26, 30))
            .WithPadding(12)
            .WithChildGap(10)
            .WithAutoSize(autoWidth: false, autoHeight: false);

        var demoTitle = new UIElement(new Rect(0, 0, 0, 40))
            .WithBackgroundColor(Rgb(45, 45, 55))
            .WithText("Text Overflow Demo")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(22);

        var demoRow = new HorizontalGroup()
            .WithRect(new Rect(0, 0, 0, 120))
            .WithChildGap(10)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Top)
            .WithAutoSize(autoWidth: false, autoHeight: true);

        var longText =
            "Wrap / Shrink / ShrinkAndWrap demo.\n" +
            "The quick brown fox jumps over the lazy dog.\n" +
            "你好，测试自动换行与缩放。";

        var boxWrap = new UIElement(new Rect(0, 0, 0, 120))
            .WithGrowX(1)
            .WithMinWidth(180)
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithText(longText)
            .WithTextColor(Rgb(210, 210, 210))
            .WithTextAlign(new Vector2(0f, 0f))
            .WithTextSize(16)
            .WithTextOverflow(ElementTextOverflowMode.Wrap);

        var boxShrink = new UIElement(new Rect(0, 0, 0, 120))
            .WithGrowX(1)
            .WithMinWidth(180)
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithText(longText)
            .WithTextColor(Rgb(210, 210, 210))
            .WithTextAlign(new Vector2(0f, 0f))
            .WithTextSize(16)
            .WithTextOverflow(ElementTextOverflowMode.ShrinkToFit);

        var boxShrinkWrap = new UIElement(new Rect(0, 0, 0, 120))
            .WithGrowX(1)
            .WithMinWidth(180)
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithText(longText)
            .WithTextColor(Rgb(210, 210, 210))
            .WithTextAlign(new Vector2(0f, 0f))
            .WithTextSize(16)
            .WithTextOverflow(ElementTextOverflowMode.ShrinkAndWrap);

        demoRow.WithChildren(boxWrap, boxShrink, boxShrinkWrap);

        var gridTitle = new UIElement(new Rect(0, 0, 0, 40))
            .WithBackgroundColor(Rgb(45, 45, 55))
            .WithText("Grid Demo")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(22);

        var grid = new GridGroup()
            .WithRect(new Rect(0, 0, 0, 240))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(34, 34, 40))
            .WithPadding(10)
            .WithChildGap(8)
            .WithAutoSize(autoWidth: false, autoHeight: false);

        {
            var cfg = grid.Layout;
            cfg.GridColumns = 6;
            cfg.GridCellWidth = 44;
            cfg.GridCellHeight = 44;
            cfg.AlignX = HorizontalAlignment.Center;
            cfg.AlignY = VerticalAlignment.Middle;
            grid.Layout = cfg;
        }

        var reorderController = new UiDragController(uiRoot, grid);

        for (int i = 0; i < 18; i++)
        {
            var cell = new UiDragItem(new Rect(0, 0, 44, 44), reorderController)
                .WithBackgroundColor(Rgb((byte)(50 + i * 6), (byte)(70 + i * 3), (byte)(90 + i * 2)));

            var label = new UIElement(new Rect(0, 0, 44, 44))
                .WithText($"{i + 1}")
                .WithTextColor(Color.White)
                .WithTextAlign(new Vector2(0.5f, 0.5f))
                .WithTextSize(16);

            cell.WithChild(label);

            grid.WithChild(cell);
        }

        Root.WithChildren(demoTitle, demoRow, gridTitle, grid);
    }

    private static Color Rgb(byte r, byte g, byte b)
        => new Color(r, g, b, 255);
}