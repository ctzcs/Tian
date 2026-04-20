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

    /// <summary>
    /// 设置九宫格边框，参数顺序为 (left, top, right, bottom)，单位是源贴图像素。
    /// 例如：new Vector4(7f, 7f, 7f, 7f) 表示四边各保留 7 像素不拉伸。
    /// 仅在 FillMode 为 <see cref="Ui2ImageFillMode.NineSlice"/> 时生效。
    /// </summary>
    public static UIImage WithNineSliceBorder(this UIImage imageElement, Vector4 border)
    {
        imageElement.NineSliceBorder = border;
        return imageElement;
    }
}
