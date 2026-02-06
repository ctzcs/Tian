using System.Text.RegularExpressions;
using Friflo.Engine.ECS;
using ImGuiNET;

namespace Editor;

public class InspectorWindow:EditorWindow
{
    private static readonly Regex ComponentNameTailRegex = new(@"(?:(?<=\.)|^)[^.]+$", RegexOptions.Compiled);

    private string _searchText = string.Empty;
    private const uint SearchTextMaxLength = 128;

    protected override void OnAddWindow()
    {
        base.OnAddWindow();
        IsOpen = true;
    }

    public override void Update()
    {
        if (ImGui.Begin("Inspector"))
        {
            if (Data.currentContent == null)
            {
                ImGui.Text("No Content");
                return;
            }

            var entity = Data.selectedEntity;
            if (!entity.IsNull)
            {
                var tags = entity.Tags;
                var components = entity.Components;

                ImGui.PushItemWidth(-1);
                ImGui.InputTextWithHint("##InspectorSearch", "Search Tags / Components", ref _searchText, SearchTextMaxLength);
                ImGui.PopItemWidth();
                //Inspector搜索功能
                var search = _searchText.Trim();
                var hasSearch = search.Length > 0;
                if (hasSearch && ImGui.SmallButton("Clear"))
                {
                    _searchText = string.Empty;
                    search = string.Empty;
                    hasSearch = false;
                }
                ImGui.Spacing();

                if (hasSearch)
                {
                    foreach (var tagType in tags)
                    {
                        var tagText = tagType.TagName;
                        if (!string.IsNullOrWhiteSpace(tagText) && tagText.Contains(search, StringComparison.OrdinalIgnoreCase))
                        {
                            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
                            break;
                        }
                    }
                }
                //绘制Tags
                EditorStyle.PushInspectorTagsHeaderTheme();
                var showTags = ImGui.CollapsingHeader("Tags");
                EditorStyle.PopInspectorTagsHeaderTheme();
                if (showTags)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(6f, 2f));
                    ImGui.SetWindowFontScale(0.85f);
                    ImGui.Indent(10f);

                    var anyTag = false;
                    foreach (var tagType in tags)
                    {
                        var tagText = tagType.TagName;
                        if (string.IsNullOrWhiteSpace(tagText))
                            continue;
                        if (hasSearch && !tagText.Contains(search, StringComparison.OrdinalIgnoreCase))
                            continue;

                        ImGui.TextDisabled(tagText);
                        anyTag = true;
                    }

                    if (!anyTag)
                        ImGui.TextDisabled(hasSearch ? "(No matches)" : "(None)");

                    ImGui.Unindent(10f);
                    ImGui.SetWindowFontScale(1.0f);
                    ImGui.PopStyleVar();
                    ImGui.Spacing();
                }
                
                //Draw Components
                bool anyComponent = false;
                foreach (var component in components)
                {
                    var componentValue = component.Value;
                    var componentHeader = GetComponentHeaderName(componentValue);

                    if (hasSearch)
                    {
                        var typeName = componentValue.GetType().Name;
                        if (!componentHeader.Contains(search, StringComparison.OrdinalIgnoreCase) && !typeName.Contains(search, StringComparison.OrdinalIgnoreCase))
                            continue;
                        ImGui.SetNextItemOpen(true, ImGuiCond.Always);
                    }

                    anyComponent = true;
                    EditorStyle.PushInspectorComponentHeaderTheme();
                    var open = ImGui.CollapsingHeader(componentHeader);
                    EditorStyle.PopInspectorComponentHeaderTheme();
                    if (open)
                    {
                        EditorStyle.BeginInspectorComponentBox(componentHeader);
                        var changed = InspectorUtil.DrawComponentBody(componentValue.GetType(), componentValue);
                        if (changed)
                            EntityUtils.AddEntityComponentValue(Data.selectedEntity, component.Type, componentValue);
                        EditorStyle.EndInspectorComponentBox();
                    }
                }

                if (!anyComponent)
                    ImGui.TextDisabled(hasSearch ? "(No matching components)" : "(No components)");
            }
        }
        ImGui.End();
    }

    private static string GetComponentHeaderName(object componentValue)
    {
        var componentName = componentValue.ToString().TrimEnd('.');
        var m = ComponentNameTailRegex.Match(componentName);
        return m.Success ? m.Value : componentValue.GetType().Name;
    }
}