using System.Numerics;
using Engine.Asset;
using Engine.Core;
using Engine.UI_2;
using Engine.Utility;
using Foster.Framework;

namespace Content.Source.Test_UI2;

public class MainCanvasDemo
{
    public UICanvas Canvas { get; }

    private RowGroup topRow;
    private ColumnGroup bottomRow;
    private bool layoutToggled;

    public MainCanvasDemo(UIRoot uiRoot, Vector2Int logicResolution, App app)
    {
        Canvas = uiRoot.CreateCanvas("Main");
        Build(logicResolution, app);
    }

    private void Build(Vector2Int logicResolution, App app)
    {
        Canvas.Root.Children.Clear();

        topRow = new RowGroup
        {
            Gap = 12f
        }
        .WithLayoutAnimation(0.35f, Transition.EaseInOut)
        .WithPadding(24f, 24f, 24f, 24f)
        .WithAlign(HorizontalAlignment.Start, VerticalAlignment.Stretch)
        .WithViewportRatio(new Rect(0f, 0f, 1f, 0.5f));

        Canvas.Root.WithChild(topRow);

        bottomRow = new ColumnGroup
        {
            Gap = 16f
        }
        .WithLayoutAnimation(0.35f, Transition.EaseInOut)
        .WithPadding(24f, 12f, 24f, 24f)
        .WithAlign(HorizontalAlignment.Start, VerticalAlignment.Center)
        .WithViewportRatio(new Rect(0f, 0.5f, 1f, 0.5f));

        Canvas.Root.WithChild(bottomRow);

        var topLeft = CreateBox(150f, 80f, Rgba(0.3f, 0.6f, 1f, 1f));
        topLeft.OnClick = e => Log.Info("[UI2 Test] TopLeft Click pos=" + e.Position);

        var centerRow = new RowGroup
        {
            Gap = 8f
        }
        .WithSize(0f, 80f)
        .WithGrow(1f);

        var centerLeft = CreateBox(80f, 80f, Rgba(0.8f, 0.3f, 0.3f, 1f));
        var centerRight = CreateBox(80f, 80f, Rgba(0.3f, 0.8f, 0.3f, 1f));

        centerRow.WithChildren(centerLeft, centerRight);

        var topRight = new ColumnGroup
        {
            Gap = 4f
        }
        .WithSize(180f, 80f)
        .WithBackgroundColor(Rgba(0.25f, 0.25f, 0.35f, 1f));

        var label = new UIText()
            .WithText("UI_2 Layout Test")
            .WithTextColor(Color.White)
            .WithTextSize(18f)
            .WithTextAlign(new Vector2(0.5f, 0.5f));

        topRight.WithChild(label);
        topRow.AddChild(topLeft);
        topRow.AddChild(centerRow);
        topRow.AddChild(topRight);

        var bottomLeft = new ColumnGroup
        {
            Gap = 4f
        }
        .WithSize(0f, 120f)
        .WithGrow(2f)
        .WithBackgroundColor(Rgba(0.2f, 0.4f, 0.9f, 1f));

        var longText = "This is a long text to test overflow behavior in UI_2.";

        var textShrink = new UIText()
            .WithText("ShrinkToFit: " + longText)
            .WithTextColor(Color.White)
            .WithTextSize(32f)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextOverflow(Ui2TextOverflowMode.ShrinkToFit)
            .WithViewportRatio(new Rect(0f, 0f, 1f, 1f / 3f));

        var textWrap = new UIText()
            .WithText("Wrap: " + longText)
            .WithTextColor(Color.White)
            .WithTextSize(18f)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextOverflow(Ui2TextOverflowMode.Wrap)
            .WithViewportRatio(new Rect(0f, 1f / 3f, 1f, 1f / 3f));

        var textShrinkWrap = new UIText()
            .WithText("ShrinkAndWrap: " + longText)
            .WithTextColor(Color.White)
            .WithTextSize(18f)
            .WithTextAlign(new Vector2(0.5f, 0.5f))
            .WithTextOverflow(Ui2TextOverflowMode.ShrinkAndWrap)
            .WithViewportRatio(new Rect(0f, 2f / 3f, 1f, 1f / 3f));

        var shrinkRow = new RowGroup
        {
            Gap = 4f
        }
        .WithSize(0f, 40f)
        .WithBackgroundColor(Rgba(0.15f, 0.15f, 0.25f, 1f));

        {
            var layout = shrinkRow.Layout;
            layout.Width = 220f;
            layout.MarginTop = 4f;
            shrinkRow.Layout = layout;
        }

        var shrinkBox1 = CreateBox(120f, 40f, Rgba(0.9f, 0.3f, 0.3f, 1f))
            .WithUserData("ShrinkBox1");
        var layout1 = shrinkBox1.Layout;
        layout1.Shrink = 1f;
        shrinkBox1.Layout = layout1;

        var shrinkBox2 = CreateBox(120f, 40f, Rgba(0.3f, 0.9f, 0.3f, 1f))
            .WithUserData("ShrinkBox2");
        var layout2 = shrinkBox2.Layout;
        layout2.Shrink = 3f;
        shrinkBox2.Layout = layout2;

        var shrinkBox3 = CreateBox(120f, 40f, Rgba(0.3f, 0.3f, 0.9f, 1f))
            .WithUserData("ShrinkBox3");
        var layout3 = shrinkBox3.Layout;
        layout3.Shrink = 0f;
        shrinkBox3.Layout = layout3;

        shrinkRow.WithChildren(shrinkBox1, shrinkBox2, shrinkBox3);

        bottomLeft.WithChildren(textShrink, textWrap, textShrinkWrap, shrinkRow);

        var buttonRow = new RowGroup
        {
            Gap = 8f
        }
        .WithSize(0f, 40f);

        var testButton = new Ui2Button()
            .WithSize(120f, 40f)
            .WithText("UI2 Button");
        testButton.Enabled = true;
        testButton.Clicked += b =>
        {
            Log.Info("[UI2 Test] Button Click");
        };

        buttonRow.AddChild(testButton);

        var bottomCenter = new ColumnGroup
        {
            Gap = 6f
        }
        .WithSize(0f, 80f)
        .WithGrow(2f)
        .WithBackgroundColor(Rgba(0.2f, 0.8f, 0.4f, 1f))
        .WithUserData("BottomCenter");
        bottomCenter.Interactable = true;
        bottomCenter.OnPointerDown = e => Log.Info("[UI2 Test] bottomCenter Down pos=" + e.Position);
        bottomCenter.OnPointerUp = e => Log.Info("[UI2 Test] bottomCenter Up pos=" + e.Position);
        var bottomCenterLabel = new UIText()
            .WithText("Click to toggle text")
            .WithTextColor(Color.White)
            .WithTextSize(64f)
            .WithTextOverflow(Ui2TextOverflowMode.ShrinkAndWrap);
        bottomCenter.WithChild(bottomCenterLabel);
        bool bottomCenterToggle = false;
        bottomCenter.OnClick = e =>
        {
            bottomCenterToggle = !bottomCenterToggle;
            bottomCenterLabel.Text = bottomCenterToggle
                ? "Longer content in bottom center to test dynamic text updates."
                : "Click to toggle text";
            Log.Info("[UI2 Test] bottomCenter Click target=" + e.Target.UserData + " current=" + e.Current.UserData + " pos=" + e.Position);
        };

        var sliderRow = new RowGroup
        {
            Gap = 8f
        }
        .WithSize(0f, 40f)
        .WithGrow(1f)
        .WithBackgroundColor(Rgba(0.15f, 0.25f, 0.15f, 1f));

        var slider = new Ui2Slider
        {
            Min = 0f,
            Max = 100f,
            ThumbSize = 12f
        }
        .WithSize(0f, 20f)
        .WithGrow(1f);

        slider.ValueChanged += (s, v) =>
        {
            Log.Info("[UI2 Test] Slider Value=" + v);
        };

        sliderRow.AddChild(slider);

        bottomCenter.AddChild(sliderRow);

        var hpRow = new RowGroup
        {
            Gap = 8f
        }
        .WithSize(0f, 20f)
        .WithGrow(1f);

        var hpLabel = new UIText()
            .WithText("HP")
            .WithTextColor(Color.White)
            .WithTextSize(14f)
            .WithSize(30f, 0f);

        var hpBar = new Ui2Slider
            {
                Min = 0f,
                Max = 100f,
                ThumbSize = 0f,
                TrackColor = Rgba(0.2f, 0.05f, 0.05f, 1f),
                FillColor = Rgba(0.9f, 0.15f, 0.15f, 1f),
                ThumbColor = Rgba(0.2f, 0.05f, 0.05f, 1f),
                Interactable = false
            }
            .WithSize(0f, 14f)
            .WithGrow(1f);
            hpBar.SetValue(75f, false);
        

        hpRow.WithChildren(hpLabel, hpBar);
        bottomCenter.AddChild(hpRow);

        var bottomRight = new RowGroup
        {
            Gap = 8f
        }
        .WithSize(0f, 60f)
        .WithGrow(1f)
        .WithBackgroundColor(Rgba(0.9f, 0.6f, 0.2f, 1f));
        bottomRight.ClipChildren = true;

        var panelTexture = Assets.GetSubtexture("test_ui_rect/1");

        var imgStretch = new UIImage()
            .WithImageTint(Color.White)
            .WithImageFillMode(Ui2ImageFillMode.Stretch)
            .WithImageSubtexture(panelTexture)
            .WithSize(64f, 64f);

        var imgFit = new UIImage()
            .WithImageTint(Color.White)
            .WithImageFillMode(Ui2ImageFillMode.Fit)
            .WithImageSubtexture(panelTexture)
            .WithSize(64f, 64f);

        var imgNineSlice = new UIImage()
            .WithImageTint(Color.White)
            .WithImageFillMode(Ui2ImageFillMode.NineSlice)
            .WithNineSliceBorder(new Vector4(7f, 7f, 7f, 7f))
            .WithImageSubtexture(panelTexture)
            .WithSize(96f, 48f);

        bottomRight.WithChildren(imgStretch, imgFit, imgNineSlice);

        var gridGroup = new GridGroup
        {
            Gap = 4f,
            Columns = 4,
            CellWidth = 32f,
            CellHeight = 32f
        }
        .WithSize(0f, 120f)
        .WithGrow(1f)
        .WithBackgroundColor(Rgba(0.15f, 0.15f, 0.2f, 1f))
        .WithAlign(HorizontalAlignment.Center,VerticalAlignment.Center)
        .WithUserData("GridGroup");

        for (int i = 0; i < 8; i++)
        {
            var cell = CreateBox(32f, 32f, Rgba(0.4f + 0.05f * i, 0.7f, 1f - 0.05f * i, 1f))
                .WithUserData("GridCell" + i);
            gridGroup.AddChild(cell);
        }

        if (gridGroup.Children.Count > 0)
        {
            var firstCell = gridGroup.Children[0];
            var layout = firstCell.Layout;
            layout.MinWidth = 24f;
            layout.MaxWidth = 64f;
            layout.MinHeight = 24f;
            layout.MaxHeight = 64f;
            layout.MarginLeft = 4f;
            layout.MarginRight = 4f;
            layout.MarginTop = 2f;
            layout.MarginBottom = 2f;
            layout.Shrink = 1f;
            firstCell.Layout = layout;
        }

        int gridDynamicIndex = 8;
        bool gridAddMode = true;
        gridGroup.Interactable = true;
        gridGroup.OnClick = e =>
        {
            if (gridAddMode)
            {
                int i = gridDynamicIndex;
                float r = 0.4f + 0.03f * (i % 20);
                float b = 1f - 0.03f * (i % 20);
                var cell = CreateBox(32f, 32f, Rgba(r, 0.8f, b, 1f))
                    .WithUserData("GridCellDynamic" + i);
                gridGroup.AddChild(cell);
                gridDynamicIndex++;
            }
            else
            {
                int count = gridGroup.Children.Count;
                if (count > 0)
                {
                    var last = gridGroup.Children[count - 1];
                    gridGroup.RemoveChild(last);
                }
            }

            gridAddMode = !gridAddMode;
            Log.Info("[UI2 Test] gridGroup Click children=" + gridGroup.Children.Count);
        };

        var scrollView = new Ui2ScrollView()
            .WithSize(0f, 120f)
            .WithGrow(1f);

        scrollView.Background = Rgba(0.1f, 0.1f, 0.1f, 1f);

        var scrollContent = new ColumnGroup
        {
            Gap = 4f
        }
        .WithSize(0f, 0f);

        scrollView.Content.AddChild(scrollContent);

        for (int i = 0; i < 20; i++)
        {
            var row = new RowGroup
            {
                Gap = 4f
            }
            .WithSize(0f, 24f)
            .WithBackgroundColor(Rgba(0.15f + 0.02f * i, 0.3f, 0.5f, 1f));

            var itemLabel = new UIText()
                .WithText("Item " + i)
                .WithTextColor(Color.White)
                .WithTextSize(14f)
                .WithTextAlign(new Vector2(0f, 0.5f));

            row.WithChild(itemLabel);
            scrollContent.AddChild(row);
        }

        var eventRow = new RowGroup
        {
            Gap = 4f
        }
        .WithSize(0f, 60f)
        .WithGrow(1f)
        .WithBackgroundColor(Rgba(0.15f, 0.15f, 0.15f, 1f))
        .WithUserData("EventRow");

        var eventItem1 = CreateBox(0f, 40f, Rgba(0.4f, 0.7f, 1f, 1f))
            .WithGrow(1f)
            .WithUserData("EventItem1");
        eventItem1.Interactable = true;
        eventItem1.PointerPassThrough = true;
        eventItem1.OnClick = e =>
        {
            Log.Info("[UI2 Test] EventItem1 Click target=" + e.Target.UserData + " current=" + e.Current.UserData + " pos=" + e.Position);
        };

        var eventItem2 = CreateBox(0f, 40f, Rgba(0.4f, 1f, 0.7f, 1f))
            .WithGrow(1f)
            .WithUserData("EventItem2");
        eventItem2.Interactable = true;
        eventItem2.PointerPassThrough = true;
        eventItem2.OnClick = e =>
        {
            Log.Info("[UI2 Test] EventItem2 Click target=" + e.Target.UserData + " current=" + e.Current.UserData + " pos=" + e.Position);
        };

        var eventItem3 = CreateBox(0f, 40f, Rgba(1f, 0.7f, 0.4f, 1f))
            .WithGrow(1f)
            .WithUserData("EventItem3");
        eventItem3.Interactable = true;
        eventItem3.PointerPassThrough = true;
        eventItem3.OnClick = e =>
        {
            Log.Info("[UI2 Test] EventItem3 Click target=" + e.Target.UserData + " current=" + e.Current.UserData + " pos=" + e.Position);
        };

        eventRow.PointerPassThrough = false;
        eventRow.OnClick = e =>
        {
            Log.Info("[UI2 Test] EventRow Click target=" + e.Target.UserData + " current=" + e.Current.UserData + " pos=" + e.Position);
        };

        eventRow.WithChildren(eventItem1, eventItem2, eventItem3);

        var overlay = new UIElement()
            .WithBackgroundColor(Rgba(1f, 1f, 1f, 0.1f))
            .WithSize(120f, 32f);

        {
            var layout = overlay.Layout;
            layout.LayoutType = LayoutType.Absolute;
            layout.MarginRight = 8f;
            layout.MarginBottom = 8f;
            layout.AlignX = HorizontalAlignment.End;
            layout.AlignY = VerticalAlignment.End;
            overlay.Layout = layout;
        }

        var overlayText = new UIText()
            .WithText("ABS")
            .WithTextColor(Color.Black)
            .WithTextSize(14f)
            .WithTextAlign(new Vector2(0.5f, 0.5f));
        overlay.WithChild(overlayText);

        buttonRow.WithGrow(1f)
            .WithBackgroundColor(Rgba(0.15f, 0.25f, 0.35f, 1f));

        var bottomRowTop = new RowGroup
        {
            Gap = 16f
        }
        .WithSize(0f, 0f)
        .WithGrow(1f);

        bottomRowTop.WithChildren(bottomLeft, bottomCenter, buttonRow);

        var bottomRowBottom = new RowGroup
        {
            Gap = 16f
        }
        .WithSize(0f, 0f)
        .WithGrow(1f);

        bottomRowBottom.WithChildren(gridGroup, bottomRight, scrollView, eventRow);

        bottomRow.WithChildren(bottomRowTop, bottomRowBottom, overlay);
    }

    public void ToggleLayout()
    {
        layoutToggled = !layoutToggled;

        if (topRow == null || bottomRow == null)
            return;

        if (layoutToggled)
        {
            topRow.Layout.ViewportRatio = new Rect(0f, 0f, 1f, 0.4f);
            bottomRow.Layout.ViewportRatio = new Rect(0f, 0.4f, 1f, 0.6f);
        }
        else
        {
            topRow.Layout.ViewportRatio = new Rect(0f, 0f, 1f, 0.5f);
            bottomRow.Layout.ViewportRatio = new Rect(0f, 0.5f, 1f, 0.5f);
        }
    }

    private UIElement CreateBox(float width, float height, Color color)
    {
        return new UIElement()
            .WithSize(width, height)
            .WithBackgroundColor(color);
    }

    private static Color Rgba(float r, float g, float b, float a)
    {
        return new Color(r, g, b, a);
    }
}

