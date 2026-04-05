using System;
using System.IO;

namespace Engine.Asset.Source;

public sealed class DirectoryAssetSource : IAssetSource
{
    public string RootPath { get; }

    public DirectoryAssetSource(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = Path.GetFullPath(rootPath);
    }

    public bool Exists(string relativePath)
    {
        return File.Exists(GetFullPath(relativePath));
    }

    public bool TryOpen(string relativePath, out Stream? stream)
    {
        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
        {
            stream = null;
            return false;
        }

        stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return true;
    }

    public bool TryReadAllBytes(string relativePath, out byte[] bytes)
    {
        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
        {
            bytes = Array.Empty<byte>();
            return false;
        }

        bytes = File.ReadAllBytes(fullPath);
        return true;
    }

    public string GetFullPath(string relativePath)
    {
        var normalized = Normalize(relativePath);
        return Path.GetFullPath(Path.Combine(RootPath, normalized));
    }

    static string Normalize(string relativePath)
    {
        return relativePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
    }
}