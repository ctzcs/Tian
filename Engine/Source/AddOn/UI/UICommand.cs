using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.UI
{
    public enum UIDrawCommandType
    {
        Background,
        Text
    }

    public readonly struct UIDrawCommand
    {
        public readonly UIDrawCommandType Type;
        public readonly UIElement Element;
        public readonly int Depth;
        public readonly int Group; // 用于分组渲染，如不同的 UI覆盖
        public readonly Matrix3x2 Matrix;
        public readonly RectInt? Scissor;

        public UIDrawCommand(UIDrawCommandType type, UIElement element, int depth, int group, Matrix3x2 matrix, RectInt? scissor)
        {
            Type = type;
            Element = element;
            Depth = depth;
            Group = group;
            Matrix = matrix;
            Scissor = scissor;
        }
    }

    /// <summary>
    /// 把 UIElement 生成的 DrawCommand 播放到 Batcher 上
    /// TODO 目前采用两遍：先背景，再文字，减少 UIAtlas/FontAtlas 交替。
    /// </summary>
    public static class UIDrawCommandRenderer
    {
        public static void Render(IReadOnlyList<UIDrawCommand> commands, Batcher batcher)
        {
            if (commands.Count == 0)
                return;

            int minDepth = int.MaxValue;
            int maxDepth = int.MinValue;
            int minGroup = int.MaxValue;
            int maxGroup = int.MinValue;

            for (int i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                var d = cmd.Depth;
                var g = cmd.Group;

                if (d < minDepth) minDepth = d;
                if (d > maxDepth) maxDepth = d;
                if (g < minGroup) minGroup = g;
                if (g > maxGroup) maxGroup = g;
            }

            //按Group循环
            for (int group = minGroup; group <= maxGroup; group++)
            {
                // 对于每一个深度：先背景，再文字。
                // 这样同一 depth 的背景仍然可以合批，
                // 并且更深的元素整体会画在浅层元素之后。
                for (int depth = minDepth; depth <= maxDepth; depth++)
                {
                    // 先画该 depth 的所有背景
                    RectInt? currentScissor = null;
                    for (int i = 0; i < commands.Count; i++)
                    {
                        var cmd = commands[i];
                        if (cmd.Group != group || cmd.Depth != depth || cmd.Type != UIDrawCommandType.Background)
                            continue;

                        var nextScissor = cmd.Scissor;
                        if (!Nullable.Equals(currentScissor, nextScissor))
                        {
                            if (currentScissor.HasValue)
                                batcher.PopScissor();
                            if (nextScissor.HasValue)
                                batcher.PushScissor(nextScissor);
                            currentScissor = nextScissor;
                        }

                        var pushed = cmd.Matrix != Matrix3x2.Identity;
                        if (pushed)
                            batcher.PushMatrix(cmd.Matrix, true);

                        cmd.Element.DrawBackground(batcher);

                        if (pushed)
                            batcher.PopMatrix();
                    }

                    if (currentScissor.HasValue)
                        batcher.PopScissor();

                    // 再画该 depth 的所有文字
                    currentScissor = null;
                    for (int i = 0; i < commands.Count; i++)
                    {
                        var cmd = commands[i];
                        if (cmd.Group != group || cmd.Depth != depth || cmd.Type != UIDrawCommandType.Text)
                            continue;

                        var nextScissor = cmd.Scissor;
                        if (!Nullable.Equals(currentScissor, nextScissor))
                        {
                            if (currentScissor.HasValue)
                                batcher.PopScissor();
                            if (nextScissor.HasValue)
                                batcher.PushScissor(nextScissor);
                            currentScissor = nextScissor;
                        }

                        var pushed = cmd.Matrix != Matrix3x2.Identity;
                        if (pushed)
                            batcher.PushMatrix(cmd.Matrix, true);

                        cmd.Element.DrawText(batcher);

                        if (pushed)
                            batcher.PopMatrix();
                    }

                    if (currentScissor.HasValue)
                        batcher.PopScissor();
                }
            }
        }

        public static void RenderDfs(IReadOnlyList<UIDrawCommand> commands, Batcher batcher)
        {
            if (commands.Count == 0)
                return;

            RectInt? currentScissor = null;
            for (int i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];

                var nextScissor = cmd.Scissor;
                if (!Nullable.Equals(currentScissor, nextScissor))
                {
                    if (currentScissor.HasValue)
                        batcher.PopScissor();
                    if (nextScissor.HasValue)
                        batcher.PushScissor(nextScissor);
                    currentScissor = nextScissor;
                }

                var pushed = cmd.Matrix != Matrix3x2.Identity;
                if (pushed)
                    batcher.PushMatrix(cmd.Matrix, true);

                if (cmd.Type == UIDrawCommandType.Background)
                    cmd.Element.DrawBackground(batcher);
                else
                    cmd.Element.DrawText(batcher);

                if (pushed)
                    batcher.PopMatrix();
            }

            if (currentScissor.HasValue)
                batcher.PopScissor();
        }
    }
}