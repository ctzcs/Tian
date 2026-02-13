using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public class UIText : UIElement
{
    public string Text { get; set; } = string.Empty;
    public Color TextColor { get; set; } = Color.White;
    public float TextSize { get; set; } = 16f;
    public Vector2 Align { get; set; } = Vector2.Zero;
    public Ui2TextOverflowMode OverflowMode { get; set; } = Ui2TextOverflowMode.None;

    public override void CollectDrawCommands(List<Ui2DrawCommand> commands, int depth)
    {
        if (!Visible || !Display)
            return;

        var rect = new Rect(0f, 0f, LayoutRect.Width, LayoutRect.Height);
        var matrix = WorldMatrix;

        if (BackgroundEnabled)
            commands.Add(new Ui2DrawCommand(Ui2DrawCommandType.Background, rect, BackgroundColor, depth, matrix: matrix));

        if (!string.IsNullOrEmpty(Text))
            commands.Add(new Ui2DrawCommand(
                Ui2DrawCommandType.Text,
                rect,
                TextColor,
                depth,
                Text,
                TextSize,
                Align,
                OverflowMode,
                matrix: matrix));

        int nextDepth = depth + 1;
        foreach (var child in Children)
            child.CollectDrawCommands(commands, nextDepth);
    }
}

public static class UITextExtensions
{
    public static UIText WithText(this UIText textElement, string text)
    {
        textElement.Text = text;
        return textElement;
    }

    public static UIText WithTextColor(this UIText textElement, Color color)
    {
        textElement.TextColor = color;
        return textElement;
    }

    public static UIText WithTextSize(this UIText textElement, float size)
    {
        textElement.TextSize = size;
        return textElement;
    }

    public static UIText WithTextAlign(this UIText textElement, Vector2 align)
    {
        textElement.Align = align;
        return textElement;
    }

    public static UIText WithTextOverflow(this UIText textElement, Ui2TextOverflowMode overflowMode)
    {
        textElement.OverflowMode = overflowMode;
        return textElement;
    }
}
