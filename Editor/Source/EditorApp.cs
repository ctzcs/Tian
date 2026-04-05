using System;
using System.IO;
using System.Numerics;
using Engine.Asset;
using Engine.Asset.Pipeline;
using Engine.Asset.v1;
using Engine.Components;
using Engine.Core;
using Engine.Core.Extensions;
using Engine.Editor;
using Foster.Framework;
using Friflo.Engine.ECS;
using ImGuiNET;

namespace Editor;

public class EditorApp : App
{
	private EditorData _data;
	private EditorWindowManager _editorWindowManager;
	// 动态内容管理

	private bool _themeApplied = false;
	private bool _dockLayoutInitialized = false;
	private string _dockLayoutIniPath = string.Empty;
	private float _dockLayoutAutoSaveSeconds = 0f;

	public EditorApp() : base(new AppConfig()
	{
		ApplicationName = "Tian Engine",
		WindowTitle = "Tian Editor",
		Width = Const._1080P.X,
		Height = Const._1080P.Y,
		Resizable = true,
        UpdateMode = UpdateMode.FixedStep(60,false)
	})
	{
		_data = new EditorData()
		{
			app = this,
			ImRenderer = new Renderer(this, EditorStyle.GetEditorFontPath(0)),
		};
        
        InspectorReflection.RegisterAssemblies();
        
		// 默认从输出目录加载 Content.dll
		//StartEditorSetting();
		_editorWindowManager = new EditorWindowManager(_data);
		_data.WindowManager = _editorWindowManager;
		_editorWindowManager.AddWindow(new ContentSelectorWindow());
		_editorWindowManager.AddWindow(new HierarchyWindow());
		_editorWindowManager.AddWindow(new InspectorWindow());
		_editorWindowManager.AddWindow(new SystemWindow());
		_editorWindowManager.AddWindow(new EditorModeWindow());
		_editorWindowManager.AddWindow(new PerformanceWindow());
		_editorWindowManager.AddWindow(new UI2DebugWindow());

		_dockLayoutIniPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"TianEngine",
			"Editor",
			"imgui_layout.ini");
		var dir = Path.GetDirectoryName(_dockLayoutIniPath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);
	}

	protected override void Startup()
	{
		// 构造中已启动默认内容，这里无需重复
        AssetMetaGenerate();
    }

	protected override void Shutdown()
	{
		if (!string.IsNullOrEmpty(_dockLayoutIniPath))
		{
			var imRenderer = _data.ImRenderer;
			imRenderer.BeginLayout();
			ImGui.SaveIniSettingsToDisk(_dockLayoutIniPath);
			imRenderer.EndLayout();
		}
		_data.Dispose();
	}

	protected override void Update()
	{
		// 确保第一帧建立上下文后再设置主题/字体
		//ThemeUpdate

		var imRenderer = _data.ImRenderer;
		if (!_themeApplied)
		{
			imRenderer.BeginLayout();
			EditorStyle.ApplyImGuiTheme(Assets.EditorAssetsPath +"/Fonts/SmileySans-Oblique.ttf",5f,1f );
			imRenderer.EndLayout();
			_themeApplied = true;
		}

		
		// Content Update
		try
		{
			_data.currentContent?.Update();
		}
		catch (Exception e)
		{
			HandleContentRuntimeException("Update", e);
		}
		//Inspector Update
		imRenderer.BeginLayout();
		UpdateEditorSetting();
		UpdateMainMenuBar();
		_editorWindowManager.Update();
		AutoSaveDockLayout();
		imRenderer.EndLayout();
	}

	protected override void Render()
	{
		Window.Clear(Color.Black);
		try
		{
			_data.currentContent?.Render();
		}
		catch (Exception e)
		{
			HandleContentRuntimeException("Render", e);
		}
        _editorWindowManager.Render();
		_data.ImRenderer.Render();
	}

	

	private void HandleContentRuntimeException(string phase, Exception e)
	{
		Log.Error($"Content {phase} failed: {e}");
		try
		{
			_data.currentContent?.Destroy();
		}
		catch (Exception destroyError)
		{
			Log.Error($"Destroy content after {phase} failure failed: {destroyError}");
		}
		finally
		{
			_data.currentContent = null;
		}
	}

	void UpdateEditorSetting()
	{
		var viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowPos(viewport.Pos);
		ImGui.SetNextWindowSize(viewport.Size);
		ImGui.SetNextWindowViewport(viewport.ID);

		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

		var hostFlags = ImGuiWindowFlags.NoDocking |
			ImGuiWindowFlags.NoCollapse |
			ImGuiWindowFlags.NoResize |
			ImGuiWindowFlags.NoMove |
			ImGuiWindowFlags.NoBringToFrontOnFocus |
			ImGuiWindowFlags.NoNavFocus |
			ImGuiWindowFlags.NoBackground;

		ImGui.Begin("##DockSpaceHost", hostFlags);
		ImGui.PopStyleVar(3);

		var dockspaceId = ImGui.GetID("TianEditorDockSpace");
		LoadDockLayoutOnce();
		ImGui.DockSpace(dockspaceId, Vector2.Zero);
		ImGui.End();

		if (_data.ImRenderer.WantsTextInput)
			Window.StartTextInput();
		else
			Window.StopTextInput();
	}

	void LoadDockLayoutOnce()
	{
		if (_dockLayoutInitialized)
			return;

		if (!string.IsNullOrEmpty(_dockLayoutIniPath) && File.Exists(_dockLayoutIniPath))
			ImGui.LoadIniSettingsFromDisk(_dockLayoutIniPath);

		_dockLayoutInitialized = true;
	}

	void AutoSaveDockLayout()
	{
		if (!_dockLayoutInitialized)
			return;

		_dockLayoutAutoSaveSeconds += Time.Delta;
		if (_dockLayoutAutoSaveSeconds < 2f)
			return;
		_dockLayoutAutoSaveSeconds = 0f;

		if (!string.IsNullOrEmpty(_dockLayoutIniPath))
			ImGui.SaveIniSettingsToDisk(_dockLayoutIniPath);
	}

	void UpdateMainMenuBar()
	{
		if (ImGui.BeginMainMenuBar())
		{
			if (ImGui.BeginMenu("Edit"))
			{
				
				ImGui.EndMenu();
			}
            
			if (ImGui.BeginMenu("Panels"))
			{
				if (ImGui.MenuItem("ContentSelector"))
				{
					_editorWindowManager.SwitchWindowVisual<ContentSelectorWindow>();
				}
				
				if (ImGui.MenuItem("Inspector"))
				{
					_editorWindowManager.SwitchWindowVisual<InspectorWindow>();
				}
				
				if (ImGui.MenuItem("System"))
				{
					_editorWindowManager.SwitchWindowVisual<SystemWindow>();
				}
				
				if (ImGui.MenuItem("Hierarchy"))
				{
					_editorWindowManager.SwitchWindowVisual<HierarchyWindow>();
				}
				
				if (ImGui.MenuItem("Viewport"))
				{
					_editorWindowManager.SwitchWindowVisual<EditorModeWindow>();
				}
				
				if (ImGui.MenuItem("Performance"))
				{
					_editorWindowManager.SwitchWindowVisual<PerformanceWindow>();
				}
				
				if (ImGui.MenuItem("UI2 Debug"))
				{
					_editorWindowManager.SwitchWindowVisual<UI2DebugWindow>();
				}
				ImGui.EndMenu();
			}


            if (ImGui.BeginMenu("Assets"))
            {
                if (ImGui.MenuItem("BuildAssets(GameMode)"))
                {
                    //TODO 这里打包的会是Editor中不改变的Assets,而真实的应该是Content里面的Assets,只有重新编译后再打包才生效，所以应该直接从Content读取
                    AssetsV1.Pack(Assets.ContentAssetsPath,"pack.zip");
                }
            
                if (ImGui.MenuItem("Save"))
                {
                    FileSystem.SaveFileDialog((path, result) =>
                    {
                        if (result == FileSystem.DialogResult.Success && path.Length > 0)
                        {
                            _data.currentContent.World.SaveEntityGz<Prefab>(path);
                        }
                    },[],Assets.EditorAssetsPath);
                    //_data.currentContent.World.SaveEntity<EditorTag>("entity-store.json");
                
                }
                
                if (ImGui.MenuItem("Load"))
                {
                    //_data.currentContent.World.LoadEntity("entity-store.json");
                    //_data.currentContent.World.LoadEntityGz("entity-store.gz");
                    FileSystem.OpenFileDialog((path, result) =>
                    {
                        if (result == FileSystem.DialogResult.Success && path.Length > 0)
                        {
                            //TODO 这里只能加载一个Store，因为加载多个zip可能会覆盖，感觉不应该这样
                            // 这些分批写入的实体，应该只能加载到各自的EntityStore中，一起加载会有覆盖问题
                            string? directoryName = Path.GetFileName(Path.GetDirectoryName(path[0]));
                            string fileName = Path.GetFileName(path[0]);
                            Log.Info($"Loading {fileName} from {directoryName}");
                            //NOTE 必须Build之后才能用这个

                            var entityStore = new EntityStore();
                            entityStore.LoadEntityGzCache("pack.zip", $"{directoryName}/{fileName}",false);
                            _data.currentContent.World.InstantiateRoots(entityStore.Entities);
                        }
                    }, [],Assets.EditorAssetsPath);
               
                }
                ImGui.EndMenu();
            }
            
            // MenuBar 会自动横向排版，不会自动继承上一个元素的 Y 轴偏移
            // 如果要统一控制按钮的 Y 轴偏移，最简单的做法是在每个按钮绘制前调整，或者修改 FramePadding
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
            if (ImGui.Button("Play",  new Vector2(64,26))){}
            
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
            if (ImGui.Button("Pause", new Vector2(64,26))){}
            
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
            if (ImGui.Button("Stop",  new Vector2(64,26))){}
            
			ImGui.EndMainMenuBar();
		}
	}


    void AssetMetaGenerate()
    {
        var assetsPath = ProjectConfigUtils.ResolveAssetsRootPath();
        AssetDatabase.GenerateMetaAsset(assetsPath);
        AssetDatabase.GenerateAssetIndexFile(assetsPath);
    }
    
    
    
    /*TODO :
        1. 这里应该有一个Play模式，和一个编辑模式
        2. 编资产管理: 辑模式可以导入资产，创建资产依赖，编辑关卡，保存关卡
        3. Gizmos
        4. ImGui的使用问题太大了，有很多API不理解，绘制模式也不理解
    */
}