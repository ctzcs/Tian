namespace Engine.Asset;

public partial class Assets
{
    private const string contentAssetFolderName = "Assets";
    private static string? path = null;
    private static string? engineAssetsPath;

    //Editor中往往是相对Bin
    public static string EditorAssetsPath => AssetsPath;

    public static string ContentAssetsPath => AssetsPath;

    public static string AssetsPath => ResolveAssetsPath();

    public static string GetContentAssetsPath() => AssetsPath;

    // 统一开发/发布两种目录结构，避免不同入口使用不同路径策略
    private static string ResolveAssetsPath()
    {
        if (engineAssetsPath != null)
            return engineAssetsPath;

        var baseDir = AppContext.BaseDirectory;
        var local = Path.GetFullPath(Path.Combine(baseDir, contentAssetFolderName));
        if (Directory.Exists(local))
        {
            engineAssetsPath = local;
            path = local;
            return local;
        }

        var up = "";
        for (int i = 0; i < 12; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(baseDir, up, contentAssetFolderName));
            if (Directory.Exists(candidate))
            {
                engineAssetsPath = candidate;
                path = candidate;
                return candidate;
            }

            var contentCandidate = Path.GetFullPath(Path.Combine(baseDir, up, "Content", contentAssetFolderName));
            if (Directory.Exists(contentCandidate))
            {
                engineAssetsPath = contentCandidate;
                path = contentCandidate;
                return contentCandidate;
            }

            up = Path.Combine(up, "..");
        }

        throw new DirectoryNotFoundException("Cannot find Assets path from AppContext.BaseDirectory");
    }

    // 旧逻辑保留，避免丢失历史上下文
    // public static string EditorAssetsPath
    // {
    //     get
    //     {
    //         // during development we search up from the build directory to find the Assets folder
    //         // (instead of copying all the Assets to the build directory).
    //         if (path == null)
    //         {
    //             var up = "";
    //             while (!Directory.Exists(Path.Join(up, contentAssetFolderName)) && up.Length < 10)
    //                 up = Path.Join(up, "..");
    //             path = Path.Join(up, contentAssetFolderName);
    //         }

    //         return path ?? throw new Exception("Unable to find Assets path");
    //     }
    // }


    // public static string ContentAssetsPath
    // {
    //     get
    //     {
    //         var up = "";
    //         for (int i = 0; i < 12; i++)
    //         {
    //             var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, up, "Content", "Assets"));
    //             if (Directory.Exists(candidate))
    //                 return candidate;

    //             up = Path.Combine(up, "..");
    //         }
    //         throw new DirectoryNotFoundException("Cannot find Content\\Assets from AppContext.BaseDirectory");
    //     }
    // }
    
    
    // public static string GetContentAssetsPath()
    // {
    //     var up = "";
    //     for (int i = 0; i < 12; i++)
    //     {
    //         var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, up, "Content", "Assets"));
    //         if (Directory.Exists(candidate))
    //             return candidate;

    //         up = Path.Combine(up, "..");
    //     }

    //     throw new DirectoryNotFoundException("Cannot find Content\\Assets from AppContext.BaseDirectory");
    // }
}