using System.Text.Json;
using Engine.Asset;
using Engine.Core;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Serialize;

namespace Engine.Asset.Pipeline;

public class AssetDatabase
{

    public static string AssetIndexFile => "AssetIndex.json";

    public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    public static EntityStore PrefabWorld = new EntityStore();
    public static EntitySerializer EntitySerializer = new EntitySerializer();
    public static EntityConverter EntityConverter = new EntityConverter();
    public static List<DataEntity> DataEntities = new List<DataEntity>();
    
    
    private static readonly Dictionary<AssetId, string> RuntimeIndex = new();
    private static readonly Dictionary<Guid, string> PrefabPathCache = new();
    private static JsonSerializerOptions? PrefabJsonOptions;
    private static string RuntimeAssetRootPath = string.Empty;

    public static bool LoadIndex(string srcPath)
    {
        var indexFile = Path.Combine(srcPath, AssetIndexFile);
        if (!File.Exists(indexFile))
        {
            RuntimeIndex.Clear();
            RuntimeAssetRootPath = string.Empty;
            return false;
        }

        var indexFileText = File.ReadAllText(indexFile);
        var index = JsonSerializer.Deserialize<AssetIndex>(indexFileText, JsonOptions);
        if (index == null)
        {
            RuntimeIndex.Clear();
            RuntimeAssetRootPath = string.Empty;
            return false;
        }

        RuntimeAssetRootPath = string.IsNullOrWhiteSpace(index.AssetRootPath) ? srcPath : index.AssetRootPath;
        if (!Path.IsPathRooted(RuntimeAssetRootPath))
        {
            RuntimeAssetRootPath = Path.GetFullPath(Path.Combine(srcPath, RuntimeAssetRootPath));
        }

        RuntimeIndex.Clear();
        foreach (var entry in index.AssetIndexFiles)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
                continue;
            RuntimeIndex[entry.Id] = entry.Path;
        }

        return true;
    }

    public static bool TryGetRelativePath(AssetId id, out string path)
    {
        path = string.Empty;
        if (!RuntimeIndex.TryGetValue(id, out var relativePath))
            return false;

        path = relativePath;
        return true;
    }

    public static bool TryGetPath(AssetId id, out string path)
    {
        path = string.Empty;
        if (!TryGetRelativePath(id, out var relativePath))
            return false;

        path = Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(RuntimeAssetRootPath, relativePath);
        return true;
    }

    public static void GenerateMetaAsset(string srcPath)
    {
        if (!Directory.Exists(srcPath))
        {
            Log.Error($"Directory {srcPath} does not exist");
            return;
        }
        
        var files = Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .Where(p => !string.Equals(Path.GetFileName(p), AssetIndexFile, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        foreach (var file in files)
        {
            var dir = Path.GetDirectoryName(file)!;
            string ext = Path.GetExtension(file);
            string fileName = Path.GetFileName(file);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            string metaFileName = fileName + ".meta";
            string metaFilePath = Path.Combine(dir, metaFileName);
            string relativePath = Path.GetRelativePath(srcPath, file);
            string metaJsonTxt;
            bool isDirty = false;
            if (!File.Exists(metaFilePath))
            {
                //TODO 不同文件类型的Meta文件的不同创建
                
                //
                AssetMeta meta = new AssetMeta(fileNameWithoutExtension, ext, relativePath, 0);
                metaJsonTxt = JsonSerializer.Serialize(meta,JsonOptions);
                isDirty = true;
            }
            else
            {
                metaJsonTxt = File.ReadAllText(metaFilePath);
                AssetMeta meta = JsonSerializer.Deserialize<AssetMeta>(metaJsonTxt, JsonOptions);
                if (meta == null)
                {
                    Log.Error($"Meta File {metaFilePath} JsonTxt {metaJsonTxt} is null");
                }
                else if (meta.Path != relativePath)
                {
                    meta.Path = relativePath;
                    metaJsonTxt = JsonSerializer.Serialize(meta, JsonOptions);
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                File.WriteAllText(metaFilePath, metaJsonTxt);
            }
        }
    }
    
    
    public static void GenerateAssetIndexFile(string srcPath)
    {
        var indexFile = Path.Combine(srcPath, AssetIndexFile);
        AssetIndex? assetIndexFile = CreateOrGetAssetIndexFile(indexFile);
        if (assetIndexFile == null)
        {
            Log.Error($"AssetIndexFile {srcPath} Not Found");
            return;
        }
        //扫描所有meta文件
        var metaFiles = Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        var entries = new List<AssetIndex.Entry>();
        foreach (var metaFile in metaFiles)
        { 
            var metaFileTxt = File.ReadAllText(metaFile);
            var meta = JsonSerializer.Deserialize<AssetMeta>(metaFileTxt,JsonOptions);
            if (meta == null)
            {
                Log.Error($"Meta File {metaFile} JsonTxt {metaFileTxt} is null");
                continue;
            }
            
            //移除仅剩meta文件无文件的
            var fullPath = Path.Combine(srcPath, meta.Path);
            if (!File.Exists(fullPath))
            {
                Log.Info($"File {fullPath} Not Found");
                File.Delete(metaFile);
                continue;
            }
            
            entries.Add(new AssetIndex.Entry()
            {
                Id = meta.Id,
                Path = meta.Path,
            });
        }
        assetIndexFile.SetEntries(entries);
        assetIndexFile.GeneratedAt = DateTime.Now;
        assetIndexFile.AssetRootPath = "."; //srcPath如果是Assets，那么这里就是相对根目录
        string assetIndexJson = JsonSerializer.Serialize(assetIndexFile, JsonOptions);
        File.WriteAllText(indexFile,assetIndexJson);
    }
    
    public static void UpdateAssetIndexFile(string srcPath){}

    /// <summary>
    /// 获取 Prefab 专用序列化配置（含 PrefabAsset/PrefabEntity 转换器）。
    /// </summary>
    public static JsonSerializerOptions GetPrefabJsonOptions()
    {
        if (PrefabJsonOptions != null) return PrefabJsonOptions;

        PrefabJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
        };
        PrefabJsonOptions.Converters.Add(new PrefabEntityJsonConvert());
        PrefabJsonOptions.Converters.Add(new PrefabAssetJsonConvert());
        return PrefabJsonOptions;
    }

    /// <summary>
    /// 按绝对路径加载 Prefab。
    /// </summary>
    /// <param name="fullPath">Prefab 文件绝对路径。</param>
    public static PrefabAsset? LoadPrefabByPath(string fullPath)
    {
        if (!File.Exists(fullPath)) return null;
        var text = File.ReadAllText(fullPath);
        return JsonSerializer.Deserialize<PrefabAsset>(text, GetPrefabJsonOptions());
    }

    /// <summary>
    /// 按 Guid 加载 Prefab（运行时推荐入口）。
    /// </summary>
    /// <param name="prefabGuid">Prefab 资源 Guid。</param>
    public static PrefabAsset? LoadPrefabByGuid(Guid prefabGuid)
    {
        var assetsRoot = ResolveMetaAssetsPath();

        if (PrefabPathCache.TryGetValue(prefabGuid, out var cachedPath) && File.Exists(cachedPath))
        {
            return LoadPrefabByPath(cachedPath);
        }

        var files = Directory.GetFiles(assetsRoot, "*.prefab", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            var prefab = LoadPrefabByPath(files[i]);
            if (prefab == null) continue;

            if (!PrefabPathCache.ContainsKey(prefab.Guid))
                PrefabPathCache[prefab.Guid] = files[i];

            if (prefab.Guid == prefabGuid)
            {
                if (string.IsNullOrWhiteSpace(prefab.Path))
                    prefab.Path = Path.GetRelativePath(assetsRoot, files[i]);
                return prefab;
            }
        }

        return null;
    }

    public static AssetIndex? CreateOrGetAssetIndexFile(string indexFile)
    {
        AssetIndex? assetIndexFile = null;
        if (!File.Exists(indexFile))
        {
            assetIndexFile = new AssetIndex();
        }
        else
        {
            var indexFileText = File.ReadAllText(indexFile);
            assetIndexFile = JsonSerializer.Deserialize<AssetIndex>(indexFileText, JsonOptions);
        }
        return assetIndexFile;
    }
    
    /// <summary>
    /// 解析当前项目的资产根目录（优先 ProjectConfig.ContentAssetsDir）。
    /// </summary>
    public static string ResolveMetaAssetsPath()
    {
        var projectConfigPath = ProjectConfigUtils.ResolveProjectConfigPath();
        if (!string.IsNullOrWhiteSpace(projectConfigPath))
        {
            var gameDir = ProjectConfigUtils.GetProjectDirectory(projectConfigPath);
            var config = ProjectConfigUtils.LoadProjectConfig(projectConfigPath);
            var contentAssetsDir = string.IsNullOrWhiteSpace(config?.ContentAssetsDir)
                ? Path.Combine("Content", "Assets")
                : config.ContentAssetsDir;

            var sourceAssetsPath = Path.IsPathRooted(contentAssetsDir)
                ? Path.GetFullPath(contentAssetsDir)
                : Path.GetFullPath(Path.Combine(gameDir, contentAssetsDir));
            if (Directory.Exists(sourceAssetsPath))
                return sourceAssetsPath;
        }

        return Assets.EditorAssetsPath;
    }
}