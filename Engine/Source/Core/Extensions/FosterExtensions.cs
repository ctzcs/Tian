using System;
using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.Core.Extensions;

public static class SpriteFontExtensions
{
    public readonly ref struct SpriteFontDrawScope
    {
        private readonly Batcher batcher;
        private readonly SpriteFont font;

        public SpriteFontDrawScope(Batcher batcher, SpriteFont font)
        {
            this.batcher = batcher;
            this.font = font;

            if (font.Material != null)
                batcher.PushMaterial(font.Material);

            if (font.Sampler != null)
                batcher.PushSampler(font.Sampler.Value);
        }

        public void Dispose()
        {
            if (font.Sampler != null)
                batcher.PopSampler();

            if (font.Material != null)
                batcher.PopMaterial();
        }
    }

    public static SpriteFontDrawScope BeginSharedDraw(this SpriteFont font, Batcher batcher)
        => new(batcher, font);

    public static void DrawShared(this SpriteFont font, Batcher batcher, ReadOnlySpan<char> text, Vector2 position, Color color)
        => DrawShared(font, batcher, text, position, Vector2.Zero, font.Size, color);

    public static void DrawShared(this SpriteFont font, Batcher batcher, ReadOnlySpan<char> text, Vector2 position, float size, Color color)
        => DrawShared(font, batcher, text, position, Vector2.Zero, size, color);

    public static void DrawShared(this SpriteFont font, Batcher batcher, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
        => DrawShared(font, batcher, text, position, justify, font.Size, color);

    public static void DrawShared(this SpriteFont font, Batcher batcher, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, float size, Color color)
    {
        batcher.PushMatrix(position, Vector2.One * (size / font.Size), 0f);

        var last = 0;
        var at = new Vector2(0, font.Ascent);
        if (justify.X != 0)
            at.X -= justify.X * font.WidthOfLine(text);
        if (justify.Y != 0)
            at.Y -= justify.Y * font.HeightOf(text);

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                at.X = 0;
                if (justify.X != 0 && i < text.Length - 1)
                    at.X -= justify.X * font.WidthOfLine(text[(i + 1)..]);
                at.Y += font.LineHeight;
                last = 0;
                continue;
            }

            if (!font.TryGetCharacter(text[i..], out var ch, out var step))
                continue;

            if (last != 0)
                at.X += font.GetKerning(last, ch.Codepoint);

            if (ch.Subtexture.Texture != null)
                batcher.Image(ch.Subtexture, at + ch.Offset, color);

            last = ch.Codepoint;
            at.X += ch.Advance;
            i += step - 1;
        }

        batcher.PopMatrix();
    }

    public static void DrawWrappedShared(this SpriteFont font, Batcher batcher, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Color color)
        => DrawWrappedShared(font, batcher, text, maxLineWidth, position, Vector2.Zero, font.Size, color);

    public static void DrawWrappedShared(this SpriteFont font, Batcher batcher, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, Color color)
        => DrawWrappedShared(font, batcher, text, maxLineWidth, position, justify, font.Size, color);

    public static void DrawWrappedShared(this SpriteFont font, Batcher batcher, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, float size, Color color)
    {
        List<(int Start, int Length)> lines = font.WrapText(text, maxLineWidth, size);
        batcher.PushMatrix(position, Vector2.One * (size / font.Size), 0f);

        var at = Vector2.Zero;
        if (justify.Y != 0)
            at.Y -= justify.Y * (font.Height * lines.Count + font.LineGap * (lines.Count - 1));

        foreach (var (start, length) in lines)
        {
            DrawShared(font, batcher, text[start..(start + length)], at, new Vector2(justify.X, 0), font.Size, color);
            at.Y += font.LineHeight;
        }

        batcher.PopMatrix();
    }
}