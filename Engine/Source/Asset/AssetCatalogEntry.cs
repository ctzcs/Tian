using Engine.Asset.Pipeline;

namespace Engine.Asset;

public sealed class AssetCatalogEntry
{
    public AssetId Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
}