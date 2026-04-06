using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Engine.Asset;
using Engine.Asset.Pipeline;
using Engine.Asset.Source;
using Engine.Core;

namespace Engine.Editor;

/// <summary>
/// 编辑器侧资源入口。
/// 用于基于 ProjectConfig 定位编辑器 Assets 目录，并执行文件枚举、meta 读写、catalog 生成与查询。
/// 常见用法：
/// 1. 编辑器启动后调用 InitializeFromProjectConfig() 或 Refresh()。
/// 2. 需要生成 .meta / AssetMetaBank 时调用 GenerateMeta() / GenerateCatalog()。
/// 3. 需要通过 AssetId 反查资源路径时调用 TryGetEntry() / TryGetPath()。
/// 不负责运行时图集、字体、纹理流读取；这些能力应使用 GameContent.AssetManager。
/// </summary>
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

    public void InitializeFromProjectConfig()
    {
        Initialize(ProjectConfigUtils.ResolveEditorAssetsRootPath());
    }

    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(AssetsRootPath))
            InitializeFromProjectConfig();
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

    public bool TryGetEntry(AssetId assetId, out AssetCatalogEntry? entry)
    {
        if (!Catalog.IsLoaded)
            LoadCatalog();

        return Catalog.TryGetEntry(assetId, out entry);
    }

    public bool TryGetPath(AssetId assetId, out string path)
    {
        if (!Catalog.IsLoaded)
            LoadCatalog();

        return Catalog.TryGetPath(assetId, out path);
    }

    public void RefreshCatalog()
    {
        GenerateCatalog();
    }

    void EnsureInitialized()
    {
        if (string.IsNullOrWhiteSpace(AssetsRootPath))
            InitializeFromProjectConfig();
    }

    static string Normalize(string relativePath)
    {
        return relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
    }
}