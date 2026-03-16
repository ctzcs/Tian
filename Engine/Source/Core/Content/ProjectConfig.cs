using System.Text.Json;
using Engine.Asset;
using Foster.Framework;

namespace Engine.Core;

public class ProjectConfig
{
    public string GameAssembly;
    public string GameEditorAssembly;
    public string GameName;
    public string EditorName;
    public string BuildOutputDir;
    public string ContentAssetsDir;
    public static string ProjectConfigFile => "ProjectConfig.json";
}


public static class ProjectConfigUtils
{
    private static string? cachedProjectConfigPath;

    public static string? ResolveProjectConfigPath()
    {
        if (TryGetValidProjectConfigPath(cachedProjectConfigPath, out var cached))
            return cached;

        var configured = LoadEditorProjectConfigPath();
        if (TryGetValidProjectConfigPath(configured, out var cfg))
        {
            cachedProjectConfigPath = cfg;
            return cfg;
        }

        var cwdCandidate = Path.Combine(Environment.CurrentDirectory, ProjectConfig.ProjectConfigFile);
        if (TryGetValidProjectConfigPath(cwdCandidate, out var cwd))
            return cwd;

        var baseCandidate = Path.Combine(AppContext.BaseDirectory, ProjectConfig.ProjectConfigFile);
        if (TryGetValidProjectConfigPath(baseCandidate, out var basePath))
            return basePath;

        var fromCurrent = TryFindProjectConfigFromAncestors(Environment.CurrentDirectory);
        if (TryGetValidProjectConfigPath(fromCurrent, out var currentAncestor))
            return currentAncestor;

        var fromBase = TryFindProjectConfigFromAncestors(AppContext.BaseDirectory);
        if (TryGetValidProjectConfigPath(fromBase, out var baseAncestor))
            return baseAncestor;

        return null;
    }

    public static string GetProjectDirectory(string configPath)
    {
        var dir = Path.GetDirectoryName(configPath);
        return string.IsNullOrWhiteSpace(dir) ? Environment.CurrentDirectory : dir;
    }

    public static string? ResolveAssemblyPath(string projectDir, string assemblyName, string? buildOutputDir = null)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return null;

        if (Path.IsPathRooted(assemblyName))
            return File.Exists(assemblyName) ? Path.GetFullPath(assemblyName) : null;

        if (string.IsNullOrWhiteSpace(projectDir))
            return null;

        if (!string.IsNullOrWhiteSpace(buildOutputDir))
        {
            var normalizedBuildOutputDir = buildOutputDir
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            var outputDir = Path.IsPathRooted(normalizedBuildOutputDir)
                ? normalizedBuildOutputDir
                : Path.Combine(projectDir, normalizedBuildOutputDir);
            if (Directory.Exists(outputDir))
            {
                var match = Directory.EnumerateFiles(outputDir, assemblyName, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(match))
                    return match;
            }
        }

        var direct = Path.Combine(projectDir, assemblyName);
        if (File.Exists(direct))
            return direct;

        var buildDir = Path.Combine(projectDir, "Build");
        if (Directory.Exists(buildDir))
        {
            var match = Directory.EnumerateFiles(buildDir, assemblyName, SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(match))
                return match;
        }

        return null;
    }

    public static bool SetProjectConfigPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            return false;

        cachedProjectConfigPath = fullPath;
        SaveEditorProjectConfigPath(fullPath);
        return true;
    }

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

    private static string GetEditorProjectConfigPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TianEngine",
            "Editor",
            "project.json");
    }

    private static string? LoadEditorProjectConfigPath()
    {
        var path = GetEditorProjectConfigPath();
        if (!File.Exists(path))
            return null;
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { IncludeFields = true,PropertyNameCaseInsensitive = true };
            var cfg = JsonSerializer.Deserialize<EditorProjectConfig>(json, options);
            return cfg?.ProjectConfigPath;
        }
        catch (Exception e)
        {
            Log.Info($"Failed to load editor project config: {e.Message}");
            return null;
        }
    }

    private static void SaveEditorProjectConfigPath(string projectConfigPath)
    {
        try
        {
            var path = GetEditorProjectConfigPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var payload = new EditorProjectConfig { ProjectConfigPath = projectConfigPath };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Log.Info($"Failed to save editor project config: {e.Message}");
        }
    }

    private sealed class EditorProjectConfig
    {
        public string ProjectConfigPath;
    }
}