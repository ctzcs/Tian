using System.Numerics;
using Engine.Core;
using ImGuiNET;

namespace Editor;

public static class EditorStyle
{
	public static string GetEditorFontPath(int index)
	{
		var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
		var editorAssetsPath = ProjectConfigUtils.ResolveEditorAssetsRootPath();
		var candidates = new[]
		{
            Path.Combine(editorAssetsPath,"Fonts", "JetBrainsMono-Regular.ttf"),
            Path.Combine(editorAssetsPath,"Fonts","monogram.ttf"),
			Path.Combine(fontsDir, "arial.ttf"),
			Path.Combine(fontsDir, "segoeui.ttf"),
		};
		if (File.Exists(candidates[index]))
		{
			return candidates[index];
		}

		for (int i = 0; i < candidates.Length; i++)
		{
			if (File.Exists(candidates[i]))
			{
				return candidates[i];
			}
		}
		return string.Empty;
	}

	//TODO 目前不能设置字体
    public static void ApplyImGuiTheme(string? ttfPath = null, float fontSize = 16f, float globalScale = 1.0f)
	{
		var io = ImGui.GetIO();
		if (globalScale != 1.0f)
			io.FontGlobalScale *= globalScale;

		// 深色主题（现代化）
		var style = ImGui.GetStyle();
		var colors = style.Colors;

		Vector4 bg			= new(0.18f, 0.18f, 0.18f, 1.00f);
		Vector4 panel		= new(0.22f, 0.22f, 0.22f, 1.00f);
		Vector4 panelHover	= new(0.26f, 0.26f, 0.26f, 1.00f);
		Vector4 panelActive	= new(0.30f, 0.30f, 0.30f, 1.00f);
		Vector4 text		= new(0.86f, 0.86f, 0.86f, 1.00f);
		Vector4 textDisabled= new(0.55f, 0.55f, 0.55f, 1.00f);
		Vector4 border		= new(0.12f, 0.12f, 0.12f, 1.00f);

		Vector4 accent		= new(0.24f, 0.49f, 0.90f, 1.00f);
		Vector4 accentHover = new(0.30f, 0.56f, 0.95f, 1.00f);
		Vector4 accentActive= new(0.20f, 0.44f, 0.82f, 1.00f);

		// 背景/文本
		colors[(int)ImGuiCol.Text]                 = text;
		colors[(int)ImGuiCol.TextDisabled]         = textDisabled;
		colors[(int)ImGuiCol.WindowBg]             = bg;
		colors[(int)ImGuiCol.ChildBg]              = new(0,0,0,0);
		colors[(int)ImGuiCol.PopupBg]              = panel;
		colors[(int)ImGuiCol.Border]               = border;
		colors[(int)ImGuiCol.BorderShadow]         = new(0,0,0,0);

		// Frame（输入框、按钮等）
		colors[(int)ImGuiCol.FrameBg]              = panelHover;
		colors[(int)ImGuiCol.FrameBgHovered]       = panelActive;
		colors[(int)ImGuiCol.FrameBgActive]        = new(0.34f, 0.34f, 0.34f, 1.00f);

		// 标题栏
		colors[(int)ImGuiCol.TitleBg]              = bg;
		colors[(int)ImGuiCol.TitleBgActive]        = panel;
		colors[(int)ImGuiCol.TitleBgCollapsed]     = bg;

		// 菜单/滚动条
		colors[(int)ImGuiCol.MenuBarBg]            = panel;
		colors[(int)ImGuiCol.ScrollbarBg]          = panel;
		colors[(int)ImGuiCol.ScrollbarGrab]        = new(0.30f, 0.32f, 0.34f, 1.00f);
		colors[(int)ImGuiCol.ScrollbarGrabHovered] = new(0.36f, 0.38f, 0.40f, 1.00f);
		colors[(int)ImGuiCol.ScrollbarGrabActive]  = new(0.44f, 0.46f, 0.48f, 1.00f);

		// 选中/高亮（表格、树节点选中）
		colors[(int)ImGuiCol.Header]               = panel;
		colors[(int)ImGuiCol.HeaderHovered]        = panelHover;
		colors[(int)ImGuiCol.HeaderActive]         = panelActive;

		// 按钮
		colors[(int)ImGuiCol.Button]               = panel;
		colors[(int)ImGuiCol.ButtonHovered]        = panelHover;
		colors[(int)ImGuiCol.ButtonActive]         = panelActive;

		// 分割线、resize handle
		colors[(int)ImGuiCol.Separator]            = border;
		colors[(int)ImGuiCol.SeparatorHovered]     = accentHover;
		colors[(int)ImGuiCol.SeparatorActive]      = accentActive;
		colors[(int)ImGuiCol.ResizeGrip]           = new(0.50f, 0.50f, 0.50f, 0.25f);
		colors[(int)ImGuiCol.ResizeGripHovered]    = accentHover;
		colors[(int)ImGuiCol.ResizeGripActive]     = accentActive;

		// 选项卡
		colors[(int)ImGuiCol.Tab]                  = bg;
		colors[(int)ImGuiCol.TabHovered]           = panelHover;
		/*colors[(int)ImGuiCol.TabActive]            = panel;
		colors[(int)ImGuiCol.TabUnfocused]         = bg;
		colors[(int)ImGuiCol.TabUnfocusedActive]   = panel;*/

		// 滑条/进度/勾选
		colors[(int)ImGuiCol.CheckMark]            = accent;
		colors[(int)ImGuiCol.SliderGrab]           = accent;
		colors[(int)ImGuiCol.SliderGrabActive]     = accentActive;
		colors[(int)ImGuiCol.PlotLines]            = accent;
		colors[(int)ImGuiCol.PlotHistogram]        = accent;

		// 超链接/可点击文本（可选）
		/*colors[(int)ImGuiCol.NavHighlight]         = accent;*/

		// 圆角/留白/边距
		style.WindowRounding   = 8f;
		style.ChildRounding    = 6f;
		style.FrameRounding    = 6f;
		style.GrabRounding     = 6f;
		style.TabRounding      = 6f;

		style.WindowBorderSize = 1f;
		style.FrameBorderSize  = 1f;
		style.PopupBorderSize  = 1f;

		style.WindowPadding    = new(12f, 10f);
		style.FramePadding     = new(10f, 6f);
		style.ItemSpacing      = new(10f, 8f);
		style.ItemInnerSpacing = new(6f, 4f);
		style.IndentSpacing    = 18f;

		// 标题栏高度 & 菜单高度
		style.WindowTitleAlign = new(0.02f, 0.5f); // 左对齐一点点
		style.ScrollbarSize    = 14f;

		// 可选：强调色应用在可点击元素
		colors[(int)ImGuiCol.TextSelectedBg]       = new(accent.X, accent.Y, accent.Z, 0.35f);
		colors[(int)ImGuiCol.DragDropTarget]       = accentActive;
	}

	public static void PushInspectorComponentHeaderTheme()
	{
		ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.20f, 0.60f, 0.28f, 1.00f));
		ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.25f, 0.70f, 0.33f, 1.00f));
		ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.18f, 0.52f, 0.24f, 1.00f));
	}

	public static void PopInspectorComponentHeaderTheme()
	{
		ImGui.PopStyleColor(3);
	}

	public static void PushInspectorTagsHeaderTheme()
	{
		ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.20f, 0.20f, 0.20f, 1.00f));
		ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.24f, 0.24f, 0.24f, 1.00f));
		ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.28f, 0.28f, 0.28f, 1.00f));
	}

	public static void PopInspectorTagsHeaderTheme()
	{
		ImGui.PopStyleColor(3);
	}

	public static void BeginInspectorComponentBox(string header)
	{
		float margin = 8f;
		ImGui.Indent(margin);
		ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.22f, 0.22f, 0.22f, 1.00f));
		ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.18f, 0.52f, 0.24f, 1.00f));
		var width = ImGui.GetContentRegionAvail().X - margin;
		if (width < 0) width = 0;
		ImGui.BeginChild($"{header}_box", new Vector2(width, 0), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY);
	}

	public static void EndInspectorComponentBox()
	{
		ImGui.EndChild();
		ImGui.PopStyleColor(2);
		ImGui.Unindent(8f);
	}
}