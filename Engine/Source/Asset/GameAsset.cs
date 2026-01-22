namespace Engine.Asset;

public class GameAsset
{
    public Guid Guid { get; set; }
    public string Path { get; set; }
    public AssetType Type { get; set; }
}

public enum AssetType
{
    Texture,
    Prefab,
    Sound,
    Tilemap,
}