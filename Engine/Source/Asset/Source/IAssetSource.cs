using System.IO;

namespace Engine.Asset.Source;

public interface IAssetSource
{
    string RootPath { get; }

    bool Exists(string relativePath);
    bool TryOpen(string relativePath, out Stream? stream);
    bool TryReadAllBytes(string relativePath, out byte[] bytes);
}