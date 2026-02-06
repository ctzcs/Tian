using Engine.Core;
using Foster.Framework;
using ImGuiNET;

namespace Editor;

public class ContentSelectorWindow:EditorWindow
{
	private ContentManager contentManager;
    private int curIndex = -1;
    
	protected override void OnAddWindow()
	{
		contentManager = new ContentManager();
        IsOpen = true;
		var contentDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content.dll");
		if (File.Exists(contentDll))
		{
			TryLoadAssemblyAndDefaultContent(contentDll);
		}
		else
		{
			// 回退：走本地引用，避免空白
			/*Data.currentContent = new TestSample(Data.app);
			Data.currentContent.Start();*/
            Log.Info($"Loading content failed");
		}
	}

    public override void Update()
    {
	    UpdateContentSelector();
    }
    
    
    void UpdateContentSelector()
	{
		if (ImGui.Begin("Content Selector"))
		{
			var contentDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content.dll");
            
			ImGui.Separator();
			ImGui.Text(File.Exists(contentDll) ? $"Loaded: {contentDll}" : "Content.dll not found in output folder");

			ImGui.Separator();
			ImGui.Text("Available Types:");
            //切换
            var contents = contentManager.GetAvailableContentTypes("Content").ToArray();
            if (contents.Length > 0)
            {
                if (curIndex < 0 || curIndex >= contents.Length) {curIndex = 0;}
                int idx = curIndex;
                if (ImGui.Combo("##content_type", ref idx, contents, contents.Length))
                {
                    curIndex = idx;
                    LoadContentByName(contents[idx]);
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

	private void TryLoadAssemblyAndDefaultContent(string contentDll)
	{
		try
		{
			contentManager.Clear();
			contentManager.LoadContentAssembly("Content", contentDll);

			// 优先尝试 TestSample（简单名），否则取第一个可用类型
			var types = contentManager.GetAvailableContentTypes("Content").ToArray();
			if (types.Length == 0)
			{
				// 无可用类型，回退
				Log.Info("No content types found");
				return;
			}
            //默认加载0
			var defaultName = types[0];
			var content = contentManager.CreateAndSetCurrent("Content", defaultName, Data.app);
			Data.currentContent = content;
		}
		catch
		{
			// 出错回退
			Log.Info("Loading content failed");
		}
	}

	private void LoadContentByName(string typeName)
	{
		try
		{
			var content = contentManager.CreateAndSetCurrent("Content", typeName, Data.app);
			Data.currentContent = content;
            GC.Collect();
        }
		catch(Exception e)
		{
			// 加载失败则保持原样
            Log.Info($"Loading content {typeName} failed {e}");
		}
	}
}