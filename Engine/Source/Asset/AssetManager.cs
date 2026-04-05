using System;
using Engine.Asset.Pipeline;
using Engine.Asset.Source;
using Foster.Framework;

namespace Engine.Asset;

public sealed class AssetManager : IDisposable
{
    public IAssetSource? Source { get; private set; }
    public AssetCatalog? Catalog { get; private set; }
    public bool IsInitialized => Source != null;
    public bool HasCatalog => Catalog != null;
    public SpriteFont? DefaultFont => Assets.Font;

    public void Initialize(IAssetSource source, AssetCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (Source is IDisposable disposable && !ReferenceEquals(Source, source))
            disposable.Dispose();

        Source = source;
        Catalog = catalog;

        if (source is ZipAssetSource zipAssetSource)
            zipAssetSource.Initialize();
    }

    public void AttachCatalog(AssetCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Catalog = catalog;
    }

    public void LoadSpriteAtlas(GraphicsDevice graphicsDevice, string spriteSubDirectory = "Sprites")
    {
        ArgumentNullException.ThrowIfNull(Source);
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        if (Source is ZipAssetSource)
        {
            Assets.LoadSpritesFromGz(graphicsDevice, spriteSubDirectory);
            return;
        }

        if (Source is DirectoryAssetSource)
        {
            Assets.Load(graphicsDevice);
            return;
        }

        throw new NotSupportedException($"Unsupported asset source: {Source.GetType().Name}");
    }

    public void SetDefaultFont(SpriteFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        Assets.SetFont(font);
    }

    public bool ContainsSprite(string name) => Assets.Sprites.ContainsKey(name);

    public bool ContainsSubtexture(string name) => Assets.Subtextures.ContainsKey(name);

    public bool TryGetSprite(string name, out Sprite? sprite)
    {
        return Assets.Sprites.TryGetValue(name, out sprite);
    }

    public bool TryGetSubtexture(string name, out Subtexture subtexture)
    {
        return Assets.Subtextures.TryGetValue(name, out subtexture);
    }

    public PrefabAsset? LoadPrefab(Guid prefabGuid)
    {
        return AssetDatabase.LoadPrefabByGuid(prefabGuid);
    }

    public bool TryLoadPrefab(Guid prefabGuid, out PrefabAsset? prefab)
    {
        prefab = LoadPrefab(prefabGuid);
        return prefab != null;
    }

    public PrefabAsset? LoadPrefab(AssetId assetId)
    {
        if (Catalog == null || !Catalog.TryGetPath(assetId, out var path))
            return null;

        return AssetDatabase.LoadPrefabByPath(path);
    }

    public bool TryLoadPrefab(AssetId assetId, out PrefabAsset? prefab)
    {
        prefab = LoadPrefab(assetId);
        return prefab != null;
    }

    public bool TryGetPath(AssetId assetId, out string path)
    {
        if (Catalog == null)
        {
            path = string.Empty;
            return false;
        }

        return Catalog.TryGetPath(assetId, out path);
    }

    public bool TryGetEntry(AssetId assetId, out AssetCatalogEntry? entry)
    {
        if (Catalog == null)
        {
            entry = null;
            return false;
        }

        return Catalog.TryGetEntry(assetId, out entry);
    }

    public void UnloadAtlas()
    {
        Assets.DeleteCache();
    }

    public void Clear()
    {
        UnloadAtlas();
    }

    public void Reset()
    {
        Clear();

        if (Source is IDisposable disposable)
            disposable.Dispose();

        Source = null;
        Catalog = null;
    }

    public void Dispose()
    {
        Reset();
    }
}