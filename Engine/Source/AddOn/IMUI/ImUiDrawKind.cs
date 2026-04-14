using System.Numerics;
using Foster.Framework;

namespace Engine.IMUI;

internal enum ImUiDrawKind
{
    Rect,
    Text
}

internal readonly struct ImUiDrawCommand
{
    public readonly ImUiDrawKind Kind;
    public readonly Rect Rect;
    public readonly Vector2 Position;
    public readonly Color Color;
    public readonly string Text;

    private ImUiDrawCommand(ImUiDrawKind kind, Rect rect, Vector2 position, Color color, string text)
    {
        Kind = kind;
        Rect = rect;
        Position = position;
        Color = color;
        Text = text;
    }

    public static ImUiDrawCommand MakeRect(Rect rect, Color color)
        => new(ImUiDrawKind.Rect, rect, default, color, string.Empty);

    public static ImUiDrawCommand MakeText(string text, Vector2 position, Color color)
        => new(ImUiDrawKind.Text, default, position, color, text);
}