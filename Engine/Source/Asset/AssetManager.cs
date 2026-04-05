using System;
using System.Collections.Generic;
using System.IO;
using Engine.Asset.Pipeline;
using Engine.Asset.Source;
using Engine.Core;
using Foster.Framework;

namespace Engine.Asset;

public sealed class AssetManager : IDisposable
{
    readonly Dictionary<string, Sprite> sprites = new();
    readonly Dictionary<string, Subtexture> subtextures = new();
    SpriteFont? defaultFont;
    Texture? atlas;

    public IAssetSource? Source { get; private set; }
    public AssetCatalog? Catalog { get; private set; }
    public bool IsInitialized => Source != null;
    public bool HasCatalog => Catalog != null;
    public string ContentAssetsPath => ProjectConfigUtils.ResolveContentAssetsRootPath();
    public string EditorAssetsPath => ProjectConfigUtils.ResolveEditorAssetsRootPath();
    public string? ContentAssetsPackagePath => ProjectConfigUtils.ResolveContentAssetsPackagePath();
    public SpriteFont? DefaultFont => defaultFont;
    public Texture? Atlas => atlas;

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

    public void InitializeRuntime(AssetCatalog? catalog = null)
    {
        Initialize(CreateRuntimeSource(), catalog);
    }

    public static IAssetSource CreateRuntimeSource()
    {
        var runtimeAssetMode = ProjectConfigUtils.ResolveRuntimeAssetMode();
        var packagePath = ProjectConfigUtils.ResolveContentAssetsPackagePath();

        return runtimeAssetMode switch
        {
            RuntimeAssetMode.ZipOnly => !string.IsNullOrWhiteSpace(packagePath)
                ? new ZipAssetSource(packagePath)
                : throw new FileNotFoundException("Runtime asset package not found."),
            RuntimeAssetMode.DirectoryOnly => new DirectoryAssetSource(ProjectConfigUtils.ResolveContentAssetsRootPath()),
            _ => !string.IsNullOrWhiteSpace(packagePath)
                ? new ZipAssetSource(packagePath)
                : new DirectoryAssetSource(ProjectConfigUtils.ResolveContentAssetsRootPath())
        };
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

        var result = SpriteAtlasLoader.Load(graphicsDevice, Source, spriteSubDirectory);
        UnloadAtlas();
        atlas = result.Atlas;

        foreach (var (name, sprite) in result.Sprites)
            sprites[name] = sprite;

        foreach (var (name, subtexture) in result.Subtextures)
            subtextures[name] = subtexture;
    }

    public void LoadContent(GraphicsDevice graphicsDevice, string spriteSubDirectory = "Sprites")
    {
        if (!IsInitialized)
            InitializeRuntime(Catalog);

        LoadSpriteAtlas(graphicsDevice, spriteSubDirectory);
    }

    public void LoadRuntime(GraphicsDevice graphicsDevice, string spriteSubDirectory = "Sprites")
    {
        LoadContent(graphicsDevice, spriteSubDirectory);
    }

    public bool TryReadAllBytes(string relativePath, out byte[] bytes)
    {
        if (!IsInitialized)
            InitializeRuntime(Catalog);

        return Source!.TryReadAllBytes(relativePath, out bytes);
    }

    public string GetContentPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (Source is DirectoryAssetSource directoryAssetSource)
            return directoryAssetSource.GetFullPath(normalized);

        return Path.GetFullPath(Path.Combine(ContentAssetsPath, normalized));
    }

    public SpriteFont CreateFont(GraphicsDevice graphicsDevice, string fallbackRelativePath, int size, int[]? codepoints = null, params (string imagePath, string dataPath)[] msdfCandidates)
    {
        foreach (var (imagePath, dataPath) in msdfCandidates)
        {
            if (TryReadAllBytes(imagePath, out var imageBytes) && TryReadAllBytes(dataPath, out var dataBytes))
                return new SpriteFont(graphicsDevice, new MsdfFont(new Image(imageBytes), dataBytes));
        }

        if (!IsInitialized)
            InitializeRuntime(Catalog);

        if (Source!.TryOpen(fallbackRelativePath, out var stream) && stream != null)
        {
            using (stream)
            {
                return codepoints == null
                    ? new SpriteFont(graphicsDevice, stream, size)
                    : new SpriteFont(graphicsDevice, stream, size, codepoints);
            }
        }

        throw new FileNotFoundException($"Font not found: {fallbackRelativePath}");
    }

    public Texture CreateTexture(GraphicsDevice graphicsDevice, string relativePath, string? name = null)
    {
        if (!TryReadAllBytes(relativePath, out var bytes))
            throw new FileNotFoundException($"Texture not found: {relativePath}");

        return new Texture(graphicsDevice, new Image(bytes), name);
    }

    public void SetDefaultFont(SpriteFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        defaultFont = font;
        Assets.SetFont(font);
    }

    public bool ContainsSprite(string name) => sprites.ContainsKey(name);

    public bool ContainsSubtexture(string name) => subtextures.ContainsKey(name);

    public Sprite? GetSprite(string name) => sprites.TryGetValue(name, out var sprite) ? sprite : null;

    public Subtexture GetSubtexture(string name) => subtextures.TryGetValue(name, out var subtexture) ? subtexture : new();

    public bool TryGetSprite(string name, out Sprite? sprite)
    {
        return sprites.TryGetValue(name, out sprite);
    }

    public bool TryGetSubtexture(string name, out Subtexture subtexture)
    {
        return subtextures.TryGetValue(name, out subtexture);
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

    public void SetFont(SpriteFont font)
    {
        SetDefaultFont(font);
    }

    public void UnloadAtlas()
    {
        atlas?.Dispose();
        atlas = null;
        sprites.Clear();
        subtextures.Clear();
    }

    public void DeleteCache()
    {
        Clear();
    }

    public void Clear()
    {
        UnloadAtlas();
        defaultFont = null;
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