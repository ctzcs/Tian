using Engine.Core;
using Foster.Framework;
using ImGuiNET;

namespace Editor;

public class ContentSelectorWindow:EditorWindow
{
	private ContentManager contentManager;
    private int curIndex = -1;
    private ProjectConfig? projectConfig;
    private string? projectConfigPath;
    private string? projectDir;
    private GameEditorBridge? gameEditorBridge;
	protected override void OnAddWindow()
	{
		contentManager = new ContentManager();
        IsOpen = true;
        if (Data?.WindowManager != null)
            gameEditorBridge = new GameEditorBridge(Data, Data.WindowManager, contentManager);

        RefreshProjectConfig();
	}

    public override void Update()
    {
        RefreshProjectConfig();
	    UpdateContentSelector();
    }
    
    
    void UpdateContentSelector()
	{
		if (ImGui.Begin("Content Selector"))
		{
			if (projectConfig == null || string.IsNullOrWhiteSpace(projectDir))
			{
				ImGui.Text("ProjectConfig not loaded");
				ImGui.End();
				return;
			}

			var contentDll = ProjectConfigUtils.ResolveAssemblyPath(projectDir, projectConfig.GameAssembly, projectConfig.BuildOutputDir);
			var hasContentDll = !string.IsNullOrWhiteSpace(contentDll) && File.Exists(contentDll);
			var contentName = hasContentDll ? Path.GetFileNameWithoutExtension(contentDll) : string.Empty;
			ImGui.Separator();
			ImGui.Text(hasContentDll ? $"Loaded: {contentDll}" : "Content.dll not found in output folder");

			ImGui.Separator();
			ImGui.Text("Available Types:");
            var contents = string.IsNullOrWhiteSpace(contentName)
                ? Array.Empty<string>()
                : contentManager.GetAvailableContentTypes(contentName).ToArray();
            if (contents.Length > 0)
            {
                if (curIndex < 0 || curIndex >= contents.Length) {curIndex = 0;}
                int idx = curIndex;
                if (ImGui.Combo("##content_type", ref idx, contents, contents.Length))
                {
                    curIndex = idx;
                    LoadContentByName(contentName,contents[idx]);
                }
                ImGui.SameLine();
            }
            else
            {
                ImGui.Text("No available content types");
            }
		}
		ImGui.End();
	}

	private void TryLoadGameEditors()
	{
		if (projectConfig == null || gameEditorBridge == null)
			return;

		var assemblyName = string.IsNullOrWhiteSpace(projectConfig.GameEditorAssembly)
			? projectConfig.GameAssembly
			: projectConfig.GameEditorAssembly;

		if (string.IsNullOrWhiteSpace(projectDir))
			return;

		var editorDll = ProjectConfigUtils.ResolveAssemblyPath(projectDir, assemblyName, projectConfig.BuildOutputDir);
		gameEditorBridge.LoadEditors(editorDll);
	}

	private void RefreshProjectConfig()
	{
		var resolved = ProjectConfigUtils.ResolveProjectConfigPath();
		if (string.IsNullOrWhiteSpace(resolved))
			return;

		if (resolved == projectConfigPath)
			return;

		projectConfigPath = resolved;
		projectDir = ProjectConfigUtils.GetProjectDirectory(resolved);
		projectConfig = ProjectConfigUtils.LoadProjectConfig(resolved);
		if (projectConfig == null)
		{
			Log.Error("Loading content failed in ContentSelectorWindow");
			return;
		}

		var contentDll = ProjectConfigUtils.ResolveAssemblyPath(projectDir, projectConfig.GameAssembly, projectConfig.BuildOutputDir);
		if (File.Exists(contentDll))
		{
			TryLoadAssemblyAndDefaultContent(contentDll);
		}
		else
		{
			Log.Info($"Loading content failed : content.dll not exist path {contentDll}");
		}
	}

	private void TryLoadAssemblyAndDefaultContent(string contentDll)
	{
        string contentName = Path.GetFileNameWithoutExtension(contentDll);
		try
		{
			contentManager.Clear();
			contentManager.LoadContentAssembly(contentName, contentDll);

			// 优先尝试 TestSample（简单名），否则取第一个可用类型
			var types = contentManager.GetAvailableContentTypes(contentName).ToArray();
			if (types.Length == 0)
			{
				// 无可用类型，回退
				Log.Info("No content types found");
				return;
			}
            //默认加载0
            
			var defaultName = projectConfig?.EditorName != null ? projectConfig.EditorName : types[0];
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == defaultName)
                {
                    curIndex = i;
                    break;
                }
            }
            
			var content = contentManager.CreateAndSetCurrent(contentName, defaultName, Data.app);
			Data.currentContent = content;
			TryLoadGameEditors();
		}
		catch
		{
			// 出错回退
			Log.Info("Loading content failed");
		}
	}

	private void LoadContentByName(string assemblyName,string typeName)
	{
		try
		{
			var content = contentManager.CreateAndSetCurrent(assemblyName, typeName, Data.app);
			Data.currentContent = content;
			TryLoadGameEditors();
            GC.Collect();
        }
		catch(Exception e)
		{
			// 加载失败则保持原样
            Log.Info($"Loading content {typeName} failed {e}");
		}
	}
}