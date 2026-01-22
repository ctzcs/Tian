using System.Numerics;
using Foster.Framework;

namespace Content.Test;

public class Resources
{
    public readonly SpriteFont? font;
    public readonly Texture? texture;
    public Target target;
    public Batcher batcher;
    public Vector2 logicSize;
    public Material customMaterial;

    public Resources(Target target,SpriteFont font, Texture texture,Batcher batcher, Vector2 logicSize,Material customMaterial)
    {
        this.target = target;
        this.font = font;
        this.texture = texture;
        this.batcher = batcher;
        this.logicSize = logicSize;
        this.customMaterial = customMaterial;
    }
}