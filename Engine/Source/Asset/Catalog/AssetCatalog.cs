using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Engine.Asset.Pipeline;

namespace Engine.Asset;

public sealed class AssetCatalog
{
    readonly List<AssetCatalogEntry> entries = new();

    public string RootPath { get; private set; } = string.Empty;
    public bool IsLoaded { get; private set; }
    public IReadOnlyList<AssetCatalogEntry> Entries => entries;

    public bool Load(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        Clear();
        RootPath = Path.GetFullPath(rootPath);
        IsLoaded = AssetDatabase.LoadIndex(RootPath);
        if (!IsLoaded)
            return false;

        var indexFile = Path.Combine(RootPath, AssetDatabase.AssetMetaBankFile);
        if (!File.Exists(indexFile))
            return true;

        var json = File.ReadAllText(indexFile);
        var bank = JsonSerializer.Deserialize<AssetMetaBank>(json, AssetDatabase.JsonOptions);
        if (bank == null)
            return true;

        entries.AddRange(bank.AssetIndexFiles
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Path))
            .Select(entry => new AssetCatalogEntry
            {
                Id = entry.Id,
                Name = Path.GetFileNameWithoutExtension(entry.Path!),
                Extension = Path.GetExtension(entry.Path!),
                RelativePath = entry.Path!.Replace('\\', '/'),
                FullPath = Path.GetFullPath(Path.Combine(RootPath, entry.Path!))
            }));
        return true;
    }

    public bool TryGetPath(AssetId id, out string path)
    {
        if (!IsLoaded)
        {
            path = string.Empty;
            return false;
        }

        return AssetDatabase.TryGetPath(id, out path);
    }

    public bool TryGetEntry(AssetId id, out AssetCatalogEntry? entry)
    {
        entry = entries.FirstOrDefault(it => it.Id == id);
        if (entry != null)
            return true;

        if (!TryGetPath(id, out var fullPath))
            return false;

        var relativePath = Path.IsPathRooted(fullPath) && !string.IsNullOrWhiteSpace(RootPath)
            ? Path.GetRelativePath(RootPath, fullPath)
            : fullPath;

        entry = new AssetCatalogEntry
        {
            Id = id,
            Name = Path.GetFileNameWithoutExtension(relativePath),
            Extension = Path.GetExtension(relativePath),
            RelativePath = relativePath.Replace('\\', '/'),
            FullPath = fullPath
        };
        return true;
    }

    public bool Contains(AssetId id) => TryGetEntry(id, out _);

    public string? GetPathOrNull(AssetId id)
    {
        return TryGetPath(id, out var path) ? path : null;
    }

    public void Clear()
    {
        entries.Clear();
        RootPath = string.Empty;
        IsLoaded = false;
    }
}