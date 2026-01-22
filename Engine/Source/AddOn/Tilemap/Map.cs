using System.Numerics;
using Engine.Asset;
using Engine.Core.Structure;
using Foster.Framework;

namespace Engine.Tilemap;



public struct TileAsset
{
    public bool Active;
    public Rect Rect;
    public Color Color;
}

public sealed class TileLayerAsset
{
    public int Id;
    public bool Visible = true;
    public Dictionary<Vector2Int, TileAsset> Tiles { get; } = new();
}

public sealed class TilemapAsset:GameAsset
{
    
    public string TilesetId = string.Empty;
    public int TileSize;
    public int ChunkSize;
    public Vector2 Origin;
    public List<TileLayerAsset> Layers { get; } = new();


    public TilemapAsset()
    {
        Guid = Guid.NewGuid();
        Type = AssetType.Tilemap;
    }
    
    public TileLayerAsset GetOrCreateLayer(int layerId)
    {
        for (int i = 0; i < Layers.Count; i++)
        {
            if (Layers[i].Id == layerId)
                return Layers[i];
        }

        var layer = new TileLayerAsset { Id = layerId };
        Layers.Add(layer);
        return layer;
    }

    public void SetTile(int layerId, Vector2Int tileIndex, Rect rect, Color color, bool active = true)
    {
        var layer = GetOrCreateLayer(layerId);
        layer.Tiles[tileIndex] = new TileAsset
        {
            Active = active,
            Rect = rect,
            Color = color
        };
    }

    public void ClearTile(int layerId, Vector2Int tileIndex)
    {
        var layer = GetOrCreateLayer(layerId);
        layer.Tiles.Remove(tileIndex);
    }

    public Tilemap BuildRuntime(GraphicsDevice device, Texture texture)
    {
        var map = new Tilemap(device, texture, TileSize, ChunkSize)
        {
            Origin = Origin
        };

        for (int i = 0; i < Layers.Count; i++)
        {
            var layerAsset = Layers[i];
            foreach (var kv in layerAsset.Tiles)
            {
                var tile = kv.Value;
                map.SetTile(layerAsset.Id, kv.Key, tile.Rect, tile.Color, tile.Active);
            }

            var runtimeLayer = map.GetLayer(layerAsset.Id);
            if (runtimeLayer != null)
                runtimeLayer.Visible = layerAsset.Visible;
        }

        return map;
    }
}

public struct Tile
{
    public bool Active;
    public Rect Rect;
    public Color Color;
}

public sealed class TileChunk : IDisposable
{
    public readonly Vector2Int Index;
    public readonly int Width;
    public readonly int Height;
    public readonly int TileSize;
    public readonly Tile[] Tiles;

    private readonly Mesh<PosTexColVertex> mesh;
    private readonly PosTexColVertex[] vertices;
    private readonly int[] indices;

    private int tileCount;
    private bool dirty;

    public TileChunk(GraphicsDevice device, Vector2Int index, int width, int height, int tileSize)
    {
        Index = index;
        Width = width;
        Height = height;
        TileSize = tileSize;
        Tiles = new Tile[width * height];
        mesh = new Mesh<PosTexColVertex>(device);
        vertices = new PosTexColVertex[width * height * 4];
        indices = new int[width * height * 6];

        var vertexCount = 0;
        for (var i = 0; i < indices.Length; i += 6)
        {
            indices[i + 0] = vertexCount + 0;
            indices[i + 1] = vertexCount + 1;
            indices[i + 2] = vertexCount + 2;
            indices[i + 3] = vertexCount + 0;
            indices[i + 4] = vertexCount + 2;
            indices[i + 5] = vertexCount + 3;
            vertexCount += 4;
        }

        if (indices.Length > 0)
            mesh.SetIndices(indices);

        tileCount = 0;
        dirty = true;
    }

    public void SetTile(int x, int y, in Tile tile)
    {
        var index = y * Width + x;
        Tiles[index] = tile;
        dirty = true;
    }

    public void ClearTile(int x, int y)
    {
        var index = y * Width + x;
        Tiles[index].Active = false;
        dirty = true;
    }

    public void UpdateVertices(Texture texture, Vector2 worldOrigin)
    {
        if (!dirty)
            return;

        tileCount = 0;

        var texWidth = (float)texture.Width;
        var texHeight = (float)texture.Height;

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var idx = y * Width + x;
                var tile = Tiles[idx];
                if (!tile.Active)
                    continue;

                var basePos = worldOrigin + new Vector2(x * TileSize, y * TileSize);

                var rect = tile.Rect;
                var tx0 = rect.X / texWidth;
                var ty0 = rect.Y / texHeight;
                var tx1 = rect.Right / texWidth;
                var ty1 = rect.Bottom / texHeight;

                var v = tileCount * 4;

                vertices[v + 0].Pos = basePos;
                vertices[v + 0].Tex = new Vector2(tx0, ty0);
                vertices[v + 0].Col = tile.Color;

                vertices[v + 1].Pos = basePos + new Vector2(rect.Width, 0);
                vertices[v + 1].Tex = new Vector2(tx1, ty0);
                vertices[v + 1].Col = tile.Color;

                vertices[v + 2].Pos = basePos + new Vector2(rect.Width, rect.Height);
                vertices[v + 2].Tex = new Vector2(tx1, ty1);
                vertices[v + 2].Col = tile.Color;

                vertices[v + 3].Pos = basePos + new Vector2(0, rect.Height);
                vertices[v + 3].Tex = new Vector2(tx0, ty1);
                vertices[v + 3].Col = tile.Color;

                tileCount++;
            }
        }

        if (tileCount > 0)
            mesh.SetVertices(vertices.AsSpan(0, tileCount * 4));

        dirty = false;
    }

    public void Render(GraphicsDevice device, Target target, Material material)
    {
        if (tileCount <= 0)
            return;

        var command = new DrawCommand(target, mesh, material)
        {
            BlendMode = BlendMode.Premultiply,
            IndexOffset = 0,
            IndexCount = tileCount * 6
        };

        command.Submit(device);
    }

    public void Dispose()
    {
        mesh.Dispose();
    }
}

public sealed class TileLayer : IDisposable
{
    private readonly GraphicsDevice device;
    private readonly Texture texture;
    private readonly int tileSize;
    private readonly int chunkSize;
    private readonly Dictionary<Vector2Int, TileChunk> chunks = new();

    public readonly int Id;
    public bool Visible = true;

    public TileLayer(GraphicsDevice device, Texture texture, int tileSize, int chunkSize, int id)
    {
        this.device = device;
        this.texture = texture;
        this.tileSize = tileSize;
        this.chunkSize = chunkSize;
        Id = id;
    }

    private TileChunk GetOrCreateChunk(Vector2Int chunkIndex)
    {
        if (!chunks.TryGetValue(chunkIndex, out var chunk))
        {
            chunk = new TileChunk(device, chunkIndex, chunkSize, chunkSize, tileSize);
            chunks.Add(chunkIndex, chunk);
        }

        return chunk;
    }

    public void SetTile(Vector2Int tileIndex, Rect rect, Color color, bool active = true)
    {
        var chunkIndex = new Vector2Int(tileIndex.X / chunkSize, tileIndex.Y / chunkSize);
        var localX = tileIndex.X % chunkSize;
        var localY = tileIndex.Y % chunkSize;

        var tile = new Tile
        {
            Active = active,
            Rect = rect,
            Color = color
        };

        var chunk = GetOrCreateChunk(chunkIndex);
        chunk.SetTile(localX, localY, tile);
    }

    public void ClearTile(Vector2Int tileIndex)
    {
        var chunkIndex = new Vector2Int(tileIndex.X / chunkSize, tileIndex.Y / chunkSize);
        var localX = tileIndex.X % chunkSize;
        var localY = tileIndex.Y % chunkSize;

        if (chunks.TryGetValue(chunkIndex, out var chunk))
            chunk.ClearTile(localX, localY);
    }

    public bool TryGetTile(Vector2Int tileIndex, out Tile tile)
    {
        var chunkIndex = new Vector2Int(tileIndex.X / chunkSize, tileIndex.Y / chunkSize);
        var localX = tileIndex.X % chunkSize;
        var localY = tileIndex.Y % chunkSize;

        if (!chunks.TryGetValue(chunkIndex, out var chunk))
        {
            tile = default;
            return false;
        }

        var indexInChunk = localY * chunk.Width + localX;
        tile = chunk.Tiles[indexInChunk];
        return tile.Active;
    }

    public bool TryGetTileAtWorldPosition(Vector2 worldPosition, Vector2 origin, out Vector2Int tileIndex, out Tile tile)
    {
        var local = worldPosition - origin;
        if (local.X < 0 || local.Y < 0)
        {
            tileIndex = default;
            tile = default;
            return false;
        }

        var x = (int)MathF.Floor(local.X / tileSize);
        var y = (int)MathF.Floor(local.Y / tileSize);
        tileIndex = new Vector2Int(x, y);
        return TryGetTile(tileIndex, out tile);
    }

    public void WriteToAsset(TileLayerAsset assetLayer)
    {
        assetLayer.Visible = Visible;
        assetLayer.Tiles.Clear();

        foreach (var pair in chunks)
        {
            var chunkIndex = pair.Key;
            var chunk = pair.Value;
            var width = chunk.Width;
            var height = chunk.Height;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = y * width + x;
                    var tile = chunk.Tiles[idx];
                    if (!tile.Active)
                        continue;

                    var globalIndex = new Vector2Int(chunkIndex.X * width + x, chunkIndex.Y * height + y);
                    assetLayer.Tiles[globalIndex] = new TileAsset
                    {
                        Active = tile.Active,
                        Rect = tile.Rect,
                        Color = tile.Color
                    };
                }
            }
        }
    }

    public void Render(Target target, Material material, Vector2 origin)
    {
        if (!Visible)
            return;

        foreach (var pair in chunks)
        {
            var index = pair.Key;
            var chunk = pair.Value;

            var chunkOrigin = origin + new Vector2(index.X * chunkSize * tileSize, index.Y * chunkSize * tileSize);

            chunk.UpdateVertices(texture, chunkOrigin);
            chunk.Render(device, target, material);
        }
    }

    public void Dispose()
    {
        foreach (var chunk in chunks.Values)
            chunk.Dispose();

        chunks.Clear();
    }
}

public sealed class Tilemap : IDisposable
{
    private readonly GraphicsDevice device;
    private readonly Texture texture;
    private readonly Material material;
    private readonly SortedDictionary<int, TileLayer> layers = new();

    public readonly int TileSize;
    public readonly int ChunkSize;
    public Vector2 Origin;

    public const int DefaultLayer = 0;

    public Tilemap(GraphicsDevice device, Texture texture, int tileSize, int chunkSize)
    {
        this.device = device;
        this.texture = texture;
        TileSize = tileSize;
        ChunkSize = chunkSize;

        material = device.Defaults.TexturedMaterial;
    }

    private TileLayer GetOrCreateLayer(int layerId)
    {
        if (!layers.TryGetValue(layerId, out var layer))
        {
            layer = new TileLayer(device, texture, TileSize, ChunkSize, layerId);
            layers.Add(layerId, layer);
        }

        return layer;
    }

    public TileLayer? GetLayer(int layerId)
    {
        layers.TryGetValue(layerId, out var layer);
        return layer;
    }

    public void SetTile(Vector2Int tileIndex, Rect rect, Color color, bool active = true)
    {
        SetTile(DefaultLayer, tileIndex, rect, color, active);
    }

    public void SetTile(int layerId, Vector2Int tileIndex, Rect rect, Color color, bool active = true)
    {
        var layer = GetOrCreateLayer(layerId);
        layer.SetTile(tileIndex, rect, color, active);
    }

    public void ClearTile(Vector2Int tileIndex)
    {
        ClearTile(DefaultLayer, tileIndex);
    }

    public void ClearTile(int layerId, Vector2Int tileIndex)
    {
        if (layers.TryGetValue(layerId, out var layer))
            layer.ClearTile(tileIndex);
    }

    public bool TryGetTile(Vector2Int tileIndex, out Tile tile)
    {
        return TryGetTile(DefaultLayer, tileIndex, out tile);
    }

    public bool TryGetTile(int layerId, Vector2Int tileIndex, out Tile tile)
    {
        if (layers.TryGetValue(layerId, out var layer))
            return layer.TryGetTile(tileIndex, out tile);

        tile = default;
        return false;
    }

    public bool TryGetTileAtWorldPosition(Vector2 worldPosition, out Vector2Int tileIndex, out Tile tile)
    {
        return TryGetTileAtWorldPosition(DefaultLayer, worldPosition, out tileIndex, out tile);
    }

    public bool TryGetTileAtWorldPosition(int layerId, Vector2 worldPosition, out Vector2Int tileIndex, out Tile tile)
    {
        if (layers.TryGetValue(layerId, out var layer))
            return layer.TryGetTileAtWorldPosition(worldPosition, Origin, out tileIndex, out tile);

        tileIndex = default;
        tile = default;
        return false;
    }

    public TilemapAsset ToAsset(string tilesetId)
    {
        var asset = new TilemapAsset
        {
            TilesetId = tilesetId,
            TileSize = TileSize,
            ChunkSize = ChunkSize,
            Origin = Origin
        };

        foreach (var pair in layers)
        {
            var layerId = pair.Key;
            var runtimeLayer = pair.Value;
            var assetLayer = asset.GetOrCreateLayer(layerId);
            runtimeLayer.WriteToAsset(assetLayer);
        }

        return asset;
    }

    public void Render(Target target, in Matrix4x4 matrix)
    {
        material.Vertex.SetUniformBuffer(matrix);
        material.Fragment.Samplers[0] = new Material.BoundSampler(texture, new TextureSampler(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));

        foreach (var pair in layers)
        {
            var layer = pair.Value;
            layer.Render(target, material, Origin);
        }
    }

    public void Dispose()
    {
        foreach (var layer in layers.Values)
            layer.Dispose();

        layers.Clear();
    }
}