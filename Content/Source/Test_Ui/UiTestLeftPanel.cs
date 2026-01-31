using System;
using System.Numerics;
using Engine.Asset;
using Engine.UI;
using Foster.Framework;
using Rect = Foster.Framework.Rect;

namespace Content.Source.Test_Ui;

public sealed class UiTestLeftPanel
{
    private readonly UIRoot uiRoot;
    private readonly Action onRebuild;

    private readonly VerticalGroup listGroup;
    private readonly UiDragController reorderController;
    private readonly UIElement statusBar;

    private int itemId;

    public VerticalGroup Root { get; }

    public UiTestLeftPanel(UIRoot uiRoot, Action onRebuild)
    {
        this.uiRoot = uiRoot;
        this.onRebuild = onRebuild;

        Root = new VerticalGroup()
            .WithRect(new Rect(0, 0, 520, 0))
            .WithBackgroundColor(Rgb(30, 30, 36))
            .WithPadding(12)
            .WithChildGap(10)
            .WithAutoSize(autoWidth: false, autoHeight: false);

        var title = new UIElement(new Rect(0, 0, 0, 54))
            .WithBackgroundColor(Rgb(45, 45, 55))
            .WithText("UI Test Scene")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(28);

        var controlsRow = new HorizontalGroup()
            .WithRect(new Rect(0, 0, 0, 44))
            .WithChildGap(8)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Middle)
            .WithAutoSize(autoWidth: false, autoHeight: true);

        var btnAdd = new Button(new Rect(0, 0, 0, 40))
            .WithGrowX(1)
            .WithText("Add Item (P)")
            .WithBackgroundImage(Assets.GetSubtexture("test_ui_rect/0"), ElementImageFillMode.Stretch)
            .WithClick((b) => AddItem());

        var btnRemove = new Button(new Rect(0, 0, 0, 40))
            .WithGrowX(1)
            .WithText("Remove Last (L)")
            .WithBackgroundImage(Assets.GetSubtexture("test_ui_rect/1"), ElementImageFillMode.Stretch)
            .WithClick((b) => RemoveLastItem());

        var btnRebuild = new Button(new Rect(0, 0, 0, 40))
            .WithGrowX(1)
            .WithText("Rebuild (R)")
            .WithBackgroundColor(Rgb(70, 70, 90))
            .WithClick((b) => onRebuild());

        controlsRow.WithChildren(btnAdd, btnRemove, btnRebuild);

        listGroup = new VerticalGroup()
            .WithRect(new Rect(0, 0, 0, 0))
            .WithGrowY(1)
            .WithBackgroundColor(Rgb(22, 22, 26))
            .WithPadding(10)
            .WithChildGap(8)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Top)
            .WithAutoSize(autoWidth: false, autoHeight: false);

        reorderController = new UiDragController(uiRoot, listGroup);
        reorderController.Reordered += (_, _, _) => UpdateStatus();

        statusBar = new UIElement(new Rect(0, 0, 0, 36))
            .WithBackgroundColor(Rgb(40, 40, 48))
            .WithText("Items: 0 | Toggle UI: U | Debug: O")
            .WithTextColor(Rgb(230, 230, 230))
            .WithTextAlign(new Vector2(0.0f, 0.5f))
            .WithTextSize(18);

        Root.WithChildren(title, controlsRow, listGroup, statusBar);

        UpdateStatus();
    }

    public bool IsDragging => reorderController.IsDragging;

    public void CancelDrag()
    {
        reorderController.Cancel();
    }

    public void SeedItems(int count)
    {
        for (int i = 0; i < count; i++)
            AddItem();
    }

    public void AddItem()
    {
        itemId++;

        var row = new HorizontalGroup()
            .WithRect(new Rect(0, 0, 0, 42))
            .WithChildGap(8)
            .WithAlign(HorizontalAlignment.Left, VerticalAlignment.Middle)
            .WithAutoSize(autoWidth: false, autoHeight: true);

        var label = new UIElement(new Rect(0, 0, 0, 34))
            .WithGrowX(1)
            .WithBackgroundColor(Rgb(50, 50, 58))
            .WithText($"Item #{itemId}")
            .WithTextColor(Color.White)
            .WithTextAlign(new Vector2(0.1f, 0.5f))
            .WithTextSize(18);

        var dragHandle = new UiDragHandle(new Rect(0, 0, 34, 34), reorderController, row)
            .WithBackgroundColor(Rgb(70, 70, 78))
            .WithText("≡")
            .WithTextColor(Rgb(230, 230, 230))
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextSize(18);

        var btnX = new Button(new Rect(0, 0, 44, 34))
            .WithText("X")
            .WithBackgroundColor(Rgb(120, 50, 50))
            .WithClick((b) =>
            {
                row.RemoveSelf();
                UpdateStatus();
            });

        row.WithChildren(dragHandle, label, btnX);

        listGroup.WithChild(row);
        UpdateStatus();
    }

    public void RemoveLastItem()
    {
        if (reorderController.IsDragging)
            return;

        if (listGroup.Children.Count == 0)
            return;

        var last = listGroup.Children[^1];
        listGroup.RemoveChild(last);
        last.Parent = null;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        statusBar.Text = $"Items: {listGroup.Children.Count} | Toggle UI: U | Debug: O";
    }

    private static Color Rgb(byte r, byte g, byte b)
        => new Color(r, g, b, 255);
}