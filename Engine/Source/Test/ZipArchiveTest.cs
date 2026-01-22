using System.IO.Compression;

namespace Engine.Test;

public static class ZipArchiveTest
{



    public static void Pack()
    {
        string path = "test";
        using FileStream fileStream = new FileStream(path,FileMode.Create);
        ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);
        //zipArchive.CreateEntry();
        //创建压缩条目
        
    }

    public static void Unpack()
    {
        //ZipFile.CreateFromDirectory("test", "test.zip");
    }
}