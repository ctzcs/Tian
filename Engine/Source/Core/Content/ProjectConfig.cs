using System;
using System.Collections.Generic;
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
    public string DefaultEditorAssetProfile;
    public string DefaultRuntimeAssetProfile;
    public Dictionary<string, AssetProfile> AssetProfiles;
    public static string ProjectConfigFile => "ProjectConfig.json";
}

public class AssetProfile
{
    public AnchoredPath AssetsRoot;
    public AnchoredPath PackagePath;
    public string RuntimeAssetMode;
}

public class AnchoredPath
{
    public string Anchor;
    public string RelativePath;
}

public sealed class PathContext
{
    public string ProjectConfigPath { get; init; }
    public ProjectConfig ProjectConfig { get; init; }
    public string GameRoot { get; init; }
    public string AssetsRoot { get; init; }
    public string? PackagePath { get; init; }
    public RuntimeAssetMode RuntimeAssetMode { get; init; }
}

public enum RuntimeAssetMode
{
    ZipOnly,
    ZipPreferred,
    DirectoryOnly
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
        if (string.IsNullOrWhiteSpace(projectConfigPath))
            throw new FileNotFoundException("ProjectConfig.json not found.");
        return GetProjectDirectory(projectConfigPath);
    }

    /// <summary>
    /// TODO 这个方法有问题，Editor中应该可以定位，Runner中应该是直接从zip获取 
    /// 
    /// 这里是直接寻找文件夹的方式不打zip包，目前这里默认文件夹应该是Game文件夹，
    /// 解析资源根目录。优先获取ProjectConfig中的ContentAssetsDir，其次从运行目录向上探测 Assets 或 Content/Assets。
    /// </summary>
    public static string ResolveEditorAssetsRootPath() => ResolvePathContext(false).AssetsRoot;

    public static string ResolveContentAssetsRootPath() => ResolvePathContext(true).AssetsRoot;

    public static string? ResolveContentAssetsPackagePath() => ResolvePathContext(true).PackagePath;

    public static RuntimeAssetMode ResolveRuntimeAssetMode() => ResolvePathContext(true).RuntimeAssetMode;

    public static PathContext ResolvePathContext(bool runtime)
    {
        var projectConfigPath = runtime ? ResolveRuntimeProjectConfigPath() : ResolveEditorProjectConfigPath();
        if (string.IsNullOrWhiteSpace(projectConfigPath))
            throw new FileNotFoundException("ProjectConfig.json not found.");

        var projectConfig = LoadProjectConfig(projectConfigPath);
        if (projectConfig == null)
            throw new InvalidOperationException("Failed to load ProjectConfig.json.");

        var gameRoot = GetProjectDirectory(projectConfigPath);
        var appBase = Path.GetFullPath(AppContext.BaseDirectory);
        var profileName = runtime ? projectConfig.DefaultRuntimeAssetProfile : projectConfig.DefaultEditorAssetProfile;
        if (string.IsNullOrWhiteSpace(profileName))
            throw new InvalidOperationException("Default asset profile is not configured.");
        if (projectConfig.AssetProfiles == null || !projectConfig.AssetProfiles.TryGetValue(profileName, out var profile) || profile == null)
            throw new InvalidOperationException($"Asset profile not found: {profileName}");

        var assetsRoot = ResolveAnchoredPath(profile.AssetsRoot, gameRoot, appBase);
        if (!Enum.TryParse<RuntimeAssetMode>(profile.RuntimeAssetMode, true, out var runtimeMode))
            throw new InvalidOperationException($"Invalid runtime asset mode in profile: {profileName}");
        var packagePath = ResolveOptionalAnchoredPath(profile.PackagePath, gameRoot, appBase);

        return new PathContext
        {
            ProjectConfigPath = projectConfigPath,
            ProjectConfig = projectConfig,
            GameRoot = gameRoot,
            AssetsRoot = assetsRoot,
            PackagePath = packagePath,
            RuntimeAssetMode = runtimeMode
        };
    }
    
    
    /// <summary>
    /// Editor Only
    /// 解析编辑器使用的 ProjectConfig.json。
    /// 编辑器允许从 AppBase / CurrentDirectory 向上回溯查找 Game/ProjectConfig.json，
    /// 这样即使从 Build 或 publish 目录启动，也仍然可以回到工程目录操作源资源。
    /// Runtime 不走这套回溯逻辑，只使用 AppBase 下的 ProjectConfig.json，保证发布目录自洽。
    /// 找不到时返回 null。
    /// </summary>
    public static string? ResolveProjectConfigPath() => ResolveEditorProjectConfigPath();

    public static string? ResolveEditorProjectConfigPath()
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

    /// <summary>
    /// Runtime Only
    /// 运行时只认 AppBase 下的 ProjectConfig.json，不回溯工程目录。
    /// 这样发布后的 Game.exe 只依赖发布目录本身的文件布局。
    /// </summary>
    public static string? ResolveRuntimeProjectConfigPath()
    {
        var appBaseCandidate = Path.Combine(AppContext.BaseDirectory, ProjectConfig.ProjectConfigFile);
        return TryGetValidProjectConfigPath(appBaseCandidate, out var appBasePath) ? appBasePath : null;
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

    private static string ResolveAnchoredPath(AnchoredPath path, string gameRoot, string appBase)
    {
        if (path == null)
            throw new InvalidOperationException("Anchored path is not configured.");

        var baseRoot = ResolveAnchorRoot(path.Anchor, gameRoot, appBase);
        if (string.IsNullOrWhiteSpace(path.RelativePath))
            return Path.GetFullPath(baseRoot);

        var relativePath = NormalizeRelativePath(path.RelativePath);
        return Path.IsPathRooted(relativePath)
            ? Path.GetFullPath(relativePath)
            : Path.GetFullPath(Path.Combine(baseRoot, relativePath));
    }

    private static string? ResolveOptionalAnchoredPath(AnchoredPath path, string gameRoot, string appBase)
        => path == null ? null : ResolveAnchoredPath(path, gameRoot, appBase);

    private static string ResolveAnchorRoot(string anchor, string gameRoot, string appBase)
    {
        if (string.Equals(anchor, "GameRoot", StringComparison.OrdinalIgnoreCase))
            return gameRoot;
        if (string.Equals(anchor, "AppBase", StringComparison.OrdinalIgnoreCase))
            return appBase;
        throw new InvalidOperationException($"Unsupported path anchor: {anchor}");
    }

    private static string NormalizeRelativePath(string path) => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

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