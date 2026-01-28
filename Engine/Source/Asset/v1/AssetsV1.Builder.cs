using System.IO.Compression;
using Foster.Framework;

namespace Engine.Asset.v1;

public static partial class AssetsV1
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