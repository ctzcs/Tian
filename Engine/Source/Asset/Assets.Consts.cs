namespace Engine.Asset;

public partial class Assets
{
    private const string contentAssetFolderName = "Assets";
    private static string? path = null;
    private static string? engineAssetsPath;

    //Editor中往往是相对Bin
    public static string EditorAssetsPath
    {
        get
        {
            // during development we search up from the build directory to find the Assets folder
            // (instead of copying all the Assets to the build directory).
            if (path == null)
            {
                var up = "";
                while (!Directory.Exists(Path.Join(up, contentAssetFolderName)) && up.Length < 10)
                    up = Path.Join(up, "..");
                path = Path.Join(up, contentAssetFolderName);
            }

            return path ?? throw new Exception("Unable to find Assets path");
        }
    }


    public static string ContentAssetsPath
    {
        get
        {
            var up = "";
            for (int i = 0; i < 12; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, up, "Content", "Assets"));
                if (Directory.Exists(candidate))
                    return candidate;

                up = Path.Combine(up, "..");
            }
            throw new DirectoryNotFoundException("Cannot find Content\\Assets from AppContext.BaseDirectory");
        }
    }
    
    
    public static string GetContentAssetsPath()
    {
        var up = "";
        for (int i = 0; i < 12; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, up, "Content", "Assets"));
            if (Directory.Exists(candidate))
                return candidate;

            up = Path.Combine(up, "..");
        }

        throw new DirectoryNotFoundException("Cannot find Content\\Assets from AppContext.BaseDirectory");
    }
}