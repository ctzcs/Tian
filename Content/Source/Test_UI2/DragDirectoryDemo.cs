using System;
using System.Numerics;
using Engine.Core;
using Engine.UI_2;
using Engine.Tweener;
using Foster.Framework;

namespace Content.Source.Test_UI2;

public class DragDirectoryDemo
{
    public UICanvas Canvas { get; }
    private UIElement animatedBox;
    private float timeSeconds;

    public DragDirectoryDemo(UIRoot uiRoot)
    {
        Canvas = uiRoot.CreateCanvas("Drag");
        Build();
    }

    private void Build()
    {
        Canvas.Root.Children.Clear();

        var root = new ColumnGroup
        {
            Gap = 8f
        }
        .WithPadding(24f, 24f, 24f, 24f)
        .WithAlign(HorizontalAlignment.Start, VerticalAlignment.Start)
        .WithViewportRatio(new Rect(0f, 0f, 1f, 1f));

        Canvas.Root.WithChild(root);

        animatedBox = new UIElement()
            .WithSize(80f, 32f)
            .WithBackgroundColor(new Color(0.9f, 0.4f, 0.2f, 1f))
            .WithLayoutAnimation(0.35f, Transition.EaseOut);

        {
            var layout = animatedBox.Layout;
            layout.LayoutType = LayoutType.Absolute;
            layout.AlignX = HorizontalAlignment.Start;
            layout.AlignY = VerticalAlignment.End;
            layout.MarginLeft = 16f;
            layout.MarginBottom = 16f;
            animatedBox.Layout = layout;
        }

        Canvas.Root.AddChild(animatedBox);

        var rowContainer = new RowGroup
        {
            Gap = 8f
        }
        .WithSize(0f, 0f);

        var leftColumn = new ColumnGroup
        {
            Gap = 4f
        }
        .WithSize(120f, 0f);

        var leftButton = new Ui2Button()
            .WithSize(100f, 32f)
            .WithText("Play Animation");

        leftButton.WithClick(b =>
        {
            if (animatedBox == null)
                return;

            var layout = animatedBox.Layout;
            layout.AlignX = HorizontalAlignment.Center;
            layout.AlignY = VerticalAlignment.Center;
            layout.MarginLeft = 0f;
            layout.MarginBottom = 0f;
            animatedBox.Layout = layout;

            animatedBox.Rotation = 0f;
            float delay = 0.5f;
            float duration = 0.6f;

            TweenManager.TweenFloat(
                () => animatedBox.Rotation,
                v => animatedBox.Rotation = v,
                to: MathF.PI / 12f,
                time: timeSeconds + delay,
                duration: duration,
                transition: Transition.EaseOutElastic);
        });

        leftColumn.AddChild(leftButton);

        var dirPanel = new ColumnGroup
        {
            Gap = 4f
        }
        .WithSize(200f, 0f)
        .WithBackgroundColor(new Color(0.12f, 0.12f, 0.16f, 1f));

        var dirTitle = new UIText()
            .WithText("Directories")
            .WithTextColor(Color.White)
            .WithTextSize(14f)
            .WithTextAlign(new Vector2(0f, 0.5f))
            .WithSize(0f, 20f);

        var dirList = new Ui2ReorderableColumn()
            .WithSize(200f, 0f);

        dirPanel.WithChildren(dirTitle, dirList);

        rowContainer.WithChildren(leftColumn, dirPanel);
        root.WithChild(rowContainer);

        string[] dirNames =
        {
            "Assets",
            "Scenes",
            "Scripts",
            "Textures",
            "Audio",
            "Prefabs"
        };

        for (int i = 0; i < dirNames.Length; i++)
        {
            var dirName = dirNames[i];
            dirList.AddItem(dirName);
        }
    }

    public void Update(float deltaTime)
    {
        timeSeconds += deltaTime;
        TweenManager.Update(timeSeconds);
    }
}
