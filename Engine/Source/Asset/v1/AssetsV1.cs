
using System.IO.Compression;
using Foster.Framework;

namespace Engine.Asset.v1;

public static partial class AssetsV1
{
    private static FileStream? _fs;
    private static ZipArchive? _zip;
    private static Dictionary<string, ZipArchiveEntry>? _index;
    private static string? CachedZipPath { get; set; }

    public static ZipArchive Zip => _zip;


    #region LifeTime
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="zipPath"></param>
    public static void InitializeCache(string zipPath)
    {
        DisposeCache();
        if (!File.Exists(zipPath)) return;
        _fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _zip = new ZipArchive(_fs, ZipArchiveMode.Read, leaveOpen: true);
        _index = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in _zip.Entries)
        {
            var key = e.FullName.Replace('\\','/');
            _index[key] = e;
        }
        CachedZipPath = zipPath;
    }

    /// <summary>
    /// 懒加载初始化
    /// </summary>
    /// <param name="zipPath"></param>
    public static void LazyInitializeCache(string zipPath)
    {
        if (CachedZipPath == zipPath) return;
        InitializeCache(zipPath);
    }
    
    public static void DisposeCache()
    {
        _index?.Clear();
        _index = null;
        _zip?.Dispose();
        _zip = null;
        _fs?.Dispose();
        _fs = null;
        CachedZipPath = null;
    }
    

    #endregion
    
    
    /// <summary>
    /// 这里拿到的是不用解压的数据流，如果已经是gz格式，那么还需要再解压
    /// 如果已经是普通格式：
    /// using var ms = new MemoryStream();
    /// stream.CopyTo(ms);
    /// ms.Position = 0;
    /// 如果还需再解压，继续将其放入Gzip流
    ///  using var gZipStream = new GZipStream(stream,CompressionMode.Decompress, true);
    /// </summary>
    /// <param name="relativePath">从Assets作为根目录的相对路径</param>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static bool TryOpenCachedEntry(string relativePath, out Stream? stream)
    {
        stream = null;
        if (_zip == null || _index == null)
        {
            Log.Info("_zip or _index is null");
            return false;
        }
        var key = relativePath.Replace('\\','/');
        if (!_index.TryGetValue(key, out var entry))
        {
            entry = _zip.GetEntry(key);
            if (entry == null)
            {
                Log.Info($"{key} not found");
                return false;
            }
            _index[key] = entry;
        }
        stream = entry.Open();
        return true;
    }
    
    public static bool TryReadCachedBytes(string relativePath, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (!TryOpenCachedEntry(relativePath, out var stream) || stream == null)
            return false;

        using (stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            bytes = ms.ToArray();
        }
        return bytes.Length > 0;
    }

    public static bool TryReadCachedImage(string relativePath, out Image image)
    {
        image = null;
        if (!TryOpenCachedEntry(relativePath, out var stream) || stream == null)
            return false;

        using (stream)
        {
            image = new Image(stream);
            return true;
        }
    }
    
    
    

    
}

/// <summary>
/// TODO 目前可以只有一个Zip文件，然后 AssetManager一直持有这个文件的ZipArchive，省去重复读取导致的速度问题，然后可以删掉这个class
/// </summary>
public sealed class ZipEntryReader : IDisposable
{
    private readonly FileStream _fs;
    private readonly ZipArchive _zip;
    public ZipArchiveEntry Entry { get; }

    private ZipEntryReader(FileStream fs, ZipArchive zip, ZipArchiveEntry stream)
    {
        _fs = fs;
        _zip = zip;
        Entry = stream;
    }

    public static ZipEntryReader? Open(string zipPath, string fileRelativePath)
    {
        var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        //TODO 每次打开都会进行索引，O(n) 所以还是应该自己创建Table,可以一直持有ZipArchive
        var zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);
        var entry = zip.GetEntry(fileRelativePath.Replace('\\','/'));
        if (entry == null)
        {
            zip.Dispose();
            fs.Dispose();
            return null;
        }
        return new ZipEntryReader(fs, zip, entry);
    }

    public void Dispose()
    {
        _zip.Dispose();
        _fs.Dispose();
    }
}


public static class Test
{
    public static void Main()
    {
        AssetsV1.Pack(Assets.EditorAssetsPath,"pack.zip");
        using var entryReader = ZipEntryReader.Open("pack.zip", "Level/entity-store.gz");
        if (entryReader != null) Log.Info(entryReader.Entry.FullName);
    }
}