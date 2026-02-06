using Foster.Framework;
using Friflo.Engine.ECS;

namespace Content.Test.Test;

public struct ArrayUnit:IComponent
{
    public List<Layer> Layers;
}

public struct Tile
{
    public bool Active;
    public Rect Rect;
    public Color Color;
}

public struct Layer
{
    public List<Chunk> chunks;
}

public struct Chunk
{
    public List<Tile> tiles;
}