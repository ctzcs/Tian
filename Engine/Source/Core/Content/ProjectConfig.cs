using System.Text.Json;
using Foster.Framework;

namespace Engine.Core;

public class ProjectConfig
{
    public string GameAssembly;
    public string GameEditorAssembly;
    public string GameName;
    public string EditorName;
    public static string ProjectConfigFile => "ProjectConfig.json";
}


public static class ProjectConfigUtils
{
    private static string? cachedProjectConfigPath;

    public static string? ResolveProjectConfigPath()
    {
        if (!string.IsNullOrWhiteSpace(cachedProjectConfigPath) && File.Exists(cachedProjectConfigPath))
            return cachedProjectConfigPath;

        var configured = LoadEditorProjectConfigPath();
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            cachedProjectConfigPath = configured;
            return configured;
        }

        var cwdCandidate = Path.Combine(Environment.CurrentDirectory, ProjectConfig.ProjectConfigFile);
        if (File.Exists(cwdCandidate))
            return cwdCandidate;

        var baseCandidate = Path.Combine(AppContext.BaseDirectory, ProjectConfig.ProjectConfigFile);
        if (File.Exists(baseCandidate))
            return baseCandidate;

        return null;
    }

    public static string GetProjectDirectory(string configPath)
    {
        var dir = Path.GetDirectoryName(configPath);
        return string.IsNullOrWhiteSpace(dir) ? Environment.CurrentDirectory : dir;
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