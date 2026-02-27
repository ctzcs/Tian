using System.Text.Json;
using Foster.Framework;

namespace Engine.Core;

public class ProjectConfig
{
    public string GameAssembly;
    public string GameName;
    public string EditorName;
    public static string ProjectConfigFile => "ProjectConfig.json";
}


public static class ProjectConfigUtils
{
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
}