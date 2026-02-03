using System.Numerics;
using Engine.Core;
using Foster.Framework;

namespace Content.Test;

public class Resources
{
    public readonly SpriteFont? font;
    public readonly Texture? texture;
    public Target target;
    public Batcher batcher;
    public Vector2Int logicSize;
    public Material customMaterial;

    public Resources(Target target,SpriteFont font, Texture texture,Batcher batcher, Vector2Int logicSize,Material customMaterial)
    {
        this.target = target;
        this.font = font;
        this.texture = texture;
        this.batcher = batcher;
        this.logicSize = logicSize;
        this.customMaterial = customMaterial;
    }
}