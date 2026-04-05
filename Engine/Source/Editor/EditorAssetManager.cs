using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Engine.Asset;
using Engine.Asset.Pipeline;
using Engine.Asset.Source;

namespace Engine.Editor;

public sealed class EditorAssetManager
{
    public string AssetsRootPath { get; private set; } = string.Empty;
    public DirectoryAssetSource? Source { get; private set; }
    public bool IsInitialized => !string.IsNullOrWhiteSpace(AssetsRootPath);
    public AssetCatalog Catalog { get; } = new();

    public void Initialize(string assetsRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetsRootPath);

        AssetsRootPath = Path.GetFullPath(assetsRootPath);
        Source = new DirectoryAssetSource(AssetsRootPath);
    }

    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(AssetsRootPath))
            Initialize(Assets.EditorAssetsPath);
        else
            Source = new DirectoryAssetSource(AssetsRootPath);
    }

    public bool Exists(string relativePath)
    {
        EnsureInitialized();
        return File.Exists(GetFullPath(relativePath));
    }

    public bool HasMeta(string relativePath)
    {
        EnsureInitialized();
        return File.Exists(GetMetaPath(relativePath));
    }

    public string GetFullPath(string relativePath)
    {
        EnsureInitialized();
        return Path.Combine(AssetsRootPath, Normalize(relativePath));
    }

    public string GetMetaPath(string relativePath)
    {
        return GetFullPath(relativePath) + ".meta";
    }

    public IReadOnlyList<string> EnumerateFiles()
    {
        EnsureInitialized();

        return Directory.GetFiles(AssetsRootPath, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .Where(path => !string.Equals(Path.GetFileName(path), AssetDatabase.AssetMetaBankFile, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(AssetsRootPath, path).Replace('\\', '/'))
            .OrderBy(path => path)
            .ToArray();
    }

    public IReadOnlyList<AssetMeta> EnumerateMetas()
    {
        EnsureInitialized();

        return EnumerateFiles()
            .Select(path => TryGetMeta(path, out var meta) ? meta : null)
            .Where(meta => meta != null)
            .Cast<AssetMeta>()
            .ToArray();
    }

    public bool TryGetMeta(string relativePath, out AssetMeta? meta)
    {
        EnsureInitialized();

        var metaPath = GetMetaPath(relativePath);
        if (!File.Exists(metaPath))
        {
            meta = null;
            return false;
        }

        var json = File.ReadAllText(metaPath);
        meta = JsonSerializer.Deserialize<AssetMeta>(json, AssetDatabase.JsonOptions);
        return meta != null;
    }

    public void GenerateMeta()
    {
        EnsureInitialized();
        AssetDatabase.GenerateMetaAsset(AssetsRootPath);
    }

    public void GenerateCatalog()
    {
        EnsureInitialized();
        AssetDatabase.GenerateAssetIndexFile(AssetsRootPath);
        Catalog.Load(AssetsRootPath);
    }

    public bool LoadCatalog()
    {
        EnsureInitialized();
        return Catalog.Load(AssetsRootPath);
    }

    void EnsureInitialized()
    {
        if (string.IsNullOrWhiteSpace(AssetsRootPath))
            Initialize(Assets.EditorAssetsPath);
    }

    static string Normalize(string relativePath)
    {
        return relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
    }
}