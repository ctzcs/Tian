using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI_2;

public class UIImage : UIElement
{
    public Subtexture? Subtexture { get; set; }
    public Color Tint { get; set; } = Color.White;
    public Ui2ImageFillMode FillMode { get; set; } = Ui2ImageFillMode.Stretch;
    public Vector4 NineSliceBorder { get; set; }

    public override void CollectDrawCommands(List<Ui2DrawCommand> commands, int depth)
    {
        if (!Visible || !Display)
            return;

        var rect = new Rect(0f, 0f, LayoutRect.Width, LayoutRect.Height);
        var matrix = WorldMatrix;

        if (BackgroundEnabled)
            commands.Add(new Ui2DrawCommand(Ui2DrawCommandType.Background, rect, BackgroundColor, depth, matrix: matrix));

        if (Subtexture.HasValue)
            commands.Add(new Ui2DrawCommand(
                Ui2DrawCommandType.Image,
                rect,
                Tint,
                depth,
                subtexture: Subtexture,
                imageFillMode: FillMode,
                nineSliceBorder: NineSliceBorder,
                matrix: matrix));

        int nextDepth = depth + 1;
        foreach (var child in Children)
            child.CollectDrawCommands(commands, nextDepth);
    }
}

public static class UIImageExtensions
{
    public static UIImage WithImageSubtexture(this UIImage imageElement, Subtexture subtexture)
    {
        imageElement.Subtexture = subtexture;
        return imageElement;
    }

    public static UIImage WithImageTint(this UIImage imageElement, Color tint)
    {
        imageElement.Tint = tint;
        return imageElement;
    }

    public static UIImage WithImageFillMode(this UIImage imageElement, Ui2ImageFillMode fillMode)
    {
        imageElement.FillMode = fillMode;
        return imageElement;
    }

    public static UIImage WithNineSliceBorder(this UIImage imageElement, Vector4 border)
    {
        imageElement.NineSliceBorder = border;
        return imageElement;
    }
}
