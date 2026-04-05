using System;
using System.IO;
using Engine.Asset.v1;

namespace Engine.Asset.Source;

public sealed class ZipAssetSource : IAssetSource, IDisposable
{
    readonly string packagePath;

    public string RootPath => packagePath;
    public string PackagePath => packagePath;
    public bool IsInitialized { get; private set; }

    public ZipAssetSource(string packagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        this.packagePath = Path.GetFullPath(packagePath);
    }

    public void Initialize()
    {
        AssetsV1.InitializeCache(packagePath);
        IsInitialized = true;
    }

    public bool Exists(string relativePath)
    {
        EnsureInitialized();

        if (!AssetsV1.TryOpenCachedEntry(Normalize(relativePath), out var stream) || stream == null)
            return false;

        using (stream)
            return true;
    }

    public bool TryOpen(string relativePath, out Stream? stream)
    {
        EnsureInitialized();
        return AssetsV1.TryOpenCachedEntry(Normalize(relativePath), out stream);
    }

    public bool TryReadAllBytes(string relativePath, out byte[] bytes)
    {
        EnsureInitialized();
        return AssetsV1.TryReadCachedBytes(Normalize(relativePath), out bytes);
    }

    public void Dispose()
    {
        if (!IsInitialized)
            return;

        AssetsV1.DisposeCache();
        IsInitialized = false;
    }

    void EnsureInitialized()
    {
        if (!IsInitialized)
            Initialize();
    }

    static string Normalize(string relativePath)
    {
        return relativePath.Replace('\\', '/').TrimStart('/');
    }
}