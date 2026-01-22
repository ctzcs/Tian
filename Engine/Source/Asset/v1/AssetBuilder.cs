using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Foster.Framework;

namespace Engine.Asset.v1;

public static class AssetBuilder
{
    /// <summary>
    /// TODO 这里可能打包比较大需要改成Async
    /// </summary>
    /// <param name="sourceDir">文件夹路径</param>
    /// <param name="zipPath">输出路径</param>
    public static void Pack(string sourceDir,string zipPath)
    {
        //将路径的所有文件打包成zip,并生成AssetTable
        if (!Directory.Exists(sourceDir)) return;
        var dir = new DirectoryInfo(sourceDir);
        if (File.Exists(zipPath))
        {
            try { File.Delete(zipPath); }
            catch { AssetsV1.DisposeCache(); File.Delete(zipPath); }
        }
        
        using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);
        Log.Info($"Packed {zipPath} Begin");
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            //相对路径
            var rel = Path.GetRelativePath(sourceDir, file).Replace('\\', '/'); // zip 统一使用 '/'
            //压缩等级
            var level = PickLevel(Path.GetExtension(file));
            //创建条目
            var entry = zip.CreateEntry(rel, level);
            using var inStream = File.OpenRead(file);
            using var outStream = entry.Open();
            inStream.CopyTo(outStream);
        }
        Log.Info($"Packed {zipPath} Finished");
    }
    
    
    static CompressionLevel PickLevel(string ext)
    {
        ext = ext.ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".mp3" or ".ogg" or ".mp4" or ".zip" or ".gz"
            ? CompressionLevel.NoCompression
            : CompressionLevel.Optimal;
    }
}


public class AssetsV1
{
    private static FileStream? _fs;
    private static ZipArchive? _zip;
    private static Dictionary<string, ZipArchiveEntry>? _index;
    public static string? CachedZipPath { get; private set; }
    
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


    public static void LazyInitializeCache(string zipPath)
    {
        if (CachedZipPath == zipPath) return;
        InitializeCache(zipPath);
    }

    /// <summary>
    /// 这里拿到的是不用解压的数据流，如果已经是gz格式，那么还需要再解压
    /// </summary>
    /// <param name="relativePath"></param>
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
        AssetBuilder.Pack(Assets.AssetsPath,"pack.zip");
        using var entryReader = ZipEntryReader.Open("pack.zip", "Level/entity-store.gz");
        if (entryReader != null) Log.Info(entryReader.Entry.FullName);
    }
}