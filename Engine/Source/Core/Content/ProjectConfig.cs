using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Engine.Asset;
using Foster.Framework;

namespace Engine.Core;

/// <summary>
/// 游戏项目配置模型，对应 Game/ProjectConfig.json。
/// </summary>
public class ProjectConfig
{
    public string GameAssembly;
    public string GameEditorAssembly;
    public string GameName;
    public string EditorName;
    public string BuildOutputDir;
    public string ContentAssetsDir;
    public string PublishedAssetsDir;
    public string PublishedAssetsZip;
    public static string ProjectConfigFile => "ProjectConfig.json";
}

///
///TODO 这里修改的时候考虑三个问题 1. Editor在Build中如何获取项目中的Assets文件夹 2. Editor在Build中如何获取Build中的Assets文件夹 3. 游戏中如何获取Zip文件
/// 
/// <summary>
/// 项目配置与路径解析工具：负责定位 ProjectConfig、项目根目录、资源目录与程序集输出目录。
/// </summary>
public static class ProjectConfigUtils
{
    private static string? cachedProjectConfigPath;


    #region EditorOnly
    /// <summary>
    /// 解析项目根目录：优先由 ProjectConfig 推导，失败时回退到当前工作目录。
    /// </summary>
    public static string ResolveProjectRootPath()
    {
        var projectConfigPath = ResolveProjectConfigPath();
        if (!string.IsNullOrWhiteSpace(projectConfigPath))
            return GetProjectDirectory(projectConfigPath);
        return Path.GetFullPath(Environment.CurrentDirectory);
    }

    /// <summary>
    /// TODO 这个方法有问题，Editor中应该可以定位，Runner中应该是直接从zip获取 
    /// 
    /// 这里是直接寻找文件夹的方式不打zip包，目前这里默认文件夹应该是Game文件夹，
    /// 解析资源根目录。优先获取ProjectConfig中的ContentAssetsDir，其次从运行目录向上探测 Assets 或 Content/Assets。
    /// </summary>
    public static string ResolveEditorAssetsRootPath()
    {
        if (TryResolveProjectAssetsRootPath(out var path))
            return path;
        if (TryResolvePublishedAssetsRootPath(out path))
            return path;
        throw new DirectoryNotFoundException("Cannot resolve editor assets root path.");
    }

    public static string ResolveContentAssetsRootPath()
    {
        if (TryResolvePublishedAssetsRootPath(out var path))
            return path;
        if (TryResolveProjectAssetsRootPath(out path))
            return path;
        throw new DirectoryNotFoundException("Cannot resolve content assets root path.");
    }

    public static string? ResolveContentAssetsPackagePath()
    {
        var config = GetResolvedProjectConfig();
        var relativePath = string.IsNullOrWhiteSpace(config?.PublishedAssetsZip) ? "pack.zip" : config.PublishedAssetsZip;
        return TryResolveFromSearchRoots(relativePath, File.Exists, out var path) ? path : null;
    }
    
    
    /// <summary>
    /// Editor Only
    /// 按缓存、本地编辑器记录、当前目录、运行目录、祖先目录等顺序解析可用的 ProjectConfig.json 路径。
    /// 找不到时返回 null。
    ///
    /// Environment.CurrentDirectory由当前所在的目录决定，Rider里运行通常是工作目录
    /// 靠锚点文件ProjectConfig.json来获取根目录
    /// </summary>
    public static string? ResolveProjectConfigPath()
    {
        if (TryGetValidProjectConfigPath(cachedProjectConfigPath, out var cached))
            return cached;
        
        var fromBase = TryFindProjectConfigFromAncestors(AppContext.BaseDirectory);
        if (TryGetValidProjectConfigPath(fromBase, out var baseAncestor))
            return baseAncestor;

        var fromCurrent = TryFindProjectConfigFromAncestors(Environment.CurrentDirectory);
        if (TryGetValidProjectConfigPath(fromCurrent, out var currentAncestor))
            return currentAncestor;

        var baseCandidate = Path.Combine(AppContext.BaseDirectory, ProjectConfig.ProjectConfigFile);
        if (TryGetValidProjectConfigPath(baseCandidate, out var basePath))
            return basePath;

        var cwdCandidate = Path.Combine(Environment.CurrentDirectory, ProjectConfig.ProjectConfigFile);
        if (TryGetValidProjectConfigPath(cwdCandidate, out var cwd))
            return cwd;

        return null;
    }
    
    #endregion
    
    /// <summary>
    /// 根据 ProjectConfig.json 文件路径获取项目根目录。
    /// </summary>
    public static string GetProjectDirectory(string configPath)
    {
        var dir = Path.GetDirectoryName(configPath);
        return string.IsNullOrWhiteSpace(dir) ? Environment.CurrentDirectory : dir;
    }
    
    /// <summary>
    /// 解析构建输出目录绝对路径。
    /// buildOutputDir 为空时默认使用 Build。
    /// </summary>
    public static string ResolveBuildOutputRootPath(string projectDir, string? buildOutputDir = null)
    {
        var normalizedBuildOutputDir = string.IsNullOrWhiteSpace(buildOutputDir)
            ? "Build"
            : buildOutputDir.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        var outputDir = Path.IsPathRooted(normalizedBuildOutputDir)
            ? normalizedBuildOutputDir
            : Path.Combine(projectDir, normalizedBuildOutputDir);

        return Path.GetFullPath(outputDir);
    }

   
    /// <summary>
    /// 解析程序集文件路径。
    /// 先在构建输出目录递归查找，再尝试项目根目录直接路径。
    /// 找不到时返回 null。
    /// </summary>
    /// <param name="projectDir">项目文件夹</param>
    /// <param name="assemblyName">程序集文件夹</param>
    /// <param name="buildOutputDir">项目自定义输出路径，不传时走默认 Build</param>
    /// <returns></returns>
    public static string? ResolveAssemblyPath(string projectDir, string assemblyName, string? buildOutputDir = null)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return null;

        if (Path.IsPathRooted(assemblyName))
            return File.Exists(assemblyName) ? Path.GetFullPath(assemblyName) : null;

        if (string.IsNullOrWhiteSpace(projectDir))
            return null;

        var outputDir = ResolveBuildOutputRootPath(projectDir, buildOutputDir);
        if (Directory.Exists(outputDir))
        {
            var match = Directory.EnumerateFiles(outputDir, assemblyName, SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(match))
                return match;
        }

        var direct = Path.Combine(projectDir, assemblyName);
        if (File.Exists(direct))
            return direct;

        return null;
    }

    /// <summary>
    /// 从磁盘加载并反序列化 ProjectConfig。
    /// 文件不存在或格式错误时返回 null。
    /// </summary>
    public static ProjectConfig? LoadProjectConfig(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { IncludeFields = true,PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ProjectConfig>(json, options);
        }
        catch (Exception e)
        {
            Log.Info($"Failed to load ProjectConfig.json: {e.Message}");
            return null;
        }
    }

    #region Private

    private static ProjectConfig? GetResolvedProjectConfig()
    {
        var configPath = ResolveProjectConfigPath();
        return string.IsNullOrWhiteSpace(configPath) ? null : LoadProjectConfig(configPath);
    }

    private static bool TryResolveProjectAssetsRootPath(out string resolved)
    {
        resolved = string.Empty;
        var configPath = ResolveProjectConfigPath();
        if (string.IsNullOrWhiteSpace(configPath))
            return false;

        var config = LoadProjectConfig(configPath);
        var projectDir = GetProjectDirectory(configPath);
        var relativePath = string.IsNullOrWhiteSpace(config?.ContentAssetsDir) ? Path.Combine("Content", "Assets") : config.ContentAssetsDir;
        return TryResolveFromRoot(projectDir, relativePath, Directory.Exists, out resolved);
    }

    private static bool TryResolvePublishedAssetsRootPath(out string resolved)
    {
        var config = GetResolvedProjectConfig();
        var relativePath = string.IsNullOrWhiteSpace(config?.PublishedAssetsDir) ? "Assets" : config.PublishedAssetsDir;
        return TryResolveFromSearchRoots(relativePath, Directory.Exists, out resolved);
    }

    private static bool TryResolveFromSearchRoots(string relativePath, Func<string, bool> exists, out string resolved)
    {
        if (TryResolveFromRoot(AppContext.BaseDirectory, relativePath, exists, out resolved))
            return true;
        if (TryResolveFromRoot(Environment.CurrentDirectory, relativePath, exists, out resolved))
            return true;
        resolved = string.Empty;
        return false;
    }

    private static bool TryResolveFromRoot(string rootPath, string relativePath, Func<string, bool> exists, out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.IsPathRooted(normalized) ? Path.GetFullPath(normalized) : Path.GetFullPath(Path.Combine(rootPath, normalized));
        if (!exists(candidate))
            return false;

        resolved = candidate;
        return true;
    }

    /// <summary>
    /// 验证候选 ProjectConfig 路径是否可用，并输出规范化后的绝对路径。
    /// </summary>
    private static bool TryGetValidProjectConfigPath(string? path, out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        var config = LoadProjectConfig(path);
        if (config == null)
            return false;

        if (string.IsNullOrWhiteSpace(config.GameAssembly) || string.IsNullOrWhiteSpace(config.GameEditorAssembly))
            return false;

        resolved = Path.GetFullPath(path);
        return true;
    }

    /// <summary>
    /// 从给定目录开始向上查找 Game/ProjectConfig.json。
    /// </summary>
    private static string? TryFindProjectConfigFromAncestors(string startDir)
    {
        if (string.IsNullOrWhiteSpace(startDir))
            return null;

        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Game", ProjectConfig.ProjectConfigFile);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    #endregion
    
    
}