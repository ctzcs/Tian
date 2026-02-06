using System;
using System.Collections.Generic;
using Engine.Components;
using Friflo.Engine.ECS;
using ImGuiNET;

namespace Editor;

public class HierarchyWindow:EditorWindow
{
    private enum FilterMode
    {
        Smart = 0,
        Id,
        Unique,
        Component,
        Tag,
    }

    private const uint SearchMaxLength = 256;

    private readonly struct FilterTerm
    {
        public readonly FilterMode Mode;
        public readonly string Query;

        public FilterTerm(FilterMode mode, string query)
        {
            Mode = mode;
            Query = query;
        }
    }

    private string _search = string.Empty;
    private readonly Dictionary<int, bool> _visibleCache = new();
    private readonly List<FilterTerm> _filters = new();

    protected override void OnAddWindow()
    {
        base.OnAddWindow();
        IsOpen = true;
    }

    public override void Update()
    {
        var world = Data.currentContent?.World;
        if (world == null)
            return;

        if (ImGui.Begin("Hierarchy"))
        {
            DrawFilterBar();

            ParseFilters(_search, _filters);
            bool filterActive = _filters.Count > 0;

            _visibleCache.Clear();

            DrawRootEntities(world.Entities.ToEntityList(), filterActive, _filters);
        }

        ImGui.End();
    }

    private void DrawFilterBar()
    {
        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##HierarchySearch", "Search (space=AND, id:/u:/c:/tag:)", ref _search, SearchMaxLength);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort))
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Gramme: space=AND, support \"\" include space");
            ImGui.Separator();
            ImGui.TextUnformatted("prefix: id:<num> | u:<text> | c:<component name> | tag:<tag>");
            ImGui.TextUnformatted("eg: id:7  c:Transform  tag:Enemy");
            ImGui.TextUnformatted("eg: \"Main Camera\"  u:Player");
            ImGui.EndTooltip();
        }
        ImGui.PopItemWidth();

        var hasSearch = (_search ?? string.Empty).Trim().Length > 0;
        if (hasSearch && ImGui.SmallButton("Clear"))
            _search = string.Empty;

        ImGui.Spacing();
    }

    private static string GetEntityDisplayName(Entity entity)
    {
        if (entity.HasComponent<UniqueEntity>())
        {
            ref var unique = ref entity.GetComponent<UniqueEntity>();
            return $"Unique:{unique.uid}";
        }

        return "entity";
    }

    private static void ParseFilters(string raw, List<FilterTerm> dst)
    {
        dst.Clear();

        var s = (raw ?? string.Empty).Trim();
        if (s.Length == 0)
            return;

        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            if (i >= s.Length) break;

            string token;
            if (s[i] == '"')
            {
                int start = ++i;
                while (i < s.Length && s[i] != '"') i++;
                token = s.Substring(start, i - start);
                if (i < s.Length && s[i] == '"') i++;
            }
            else
            {
                int start = i;
                while (i < s.Length && !char.IsWhiteSpace(s[i])) i++;
                token = s.Substring(start, i - start);
            }

            token = token.Trim();
            if (token.Length == 0)
                continue;

            if (TryParseTerm(token, out var term))
                dst.Add(term);
        }
    }

    private static bool TryParseTerm(string token, out FilterTerm term)
    {
        int colon = token.IndexOf(':');
        if (colon > 0)
        {
            var prefix = token[..colon].Trim();
            var rest = token[(colon + 1)..].Trim();
            if (rest.Length > 0)
            {
                switch (prefix.ToLowerInvariant())
                {
                    case "id":
                        term = new FilterTerm(FilterMode.Id, rest);
                        return true;
                    case "u":
                    case "unique":
                        term = new FilterTerm(FilterMode.Unique, rest);
                        return true;
                    case "c":
                    case "comp":
                    case "component":
                        term = new FilterTerm(FilterMode.Component, rest);
                        return true;
                    case "t":
                    case "tag":
                        term = new FilterTerm(FilterMode.Tag, rest);
                        return true;
                }
            }
        }

        term = new FilterTerm(FilterMode.Smart, token);
        return true;
    }

    private static bool EntityMatchesAll(Entity entity, List<FilterTerm> terms)
    {
        for (int i = 0; i < terms.Count; i++)
        {
            var term = terms[i];
            if (!EntityMatches(entity, term.Query, term.Mode))
                return false;
        }

        return true;
    }

    private static bool EntityMatches(Entity entity, string query, FilterMode mode)
    {
        if (query.Length == 0)
            return true;

        switch (mode)
        {
            case FilterMode.Id:
                if (int.TryParse(query, out int parsedId))
                    return entity.Id == parsedId;
                return entity.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);

            case FilterMode.Unique:
                if (!entity.HasComponent<UniqueEntity>())
                    return false;
                ref var unique = ref entity.GetComponent<UniqueEntity>();
                return unique.uid.Contains(query, StringComparison.OrdinalIgnoreCase);

            case FilterMode.Component:
                foreach (var ct in entity.Archetype.ComponentTypes)
                {
                    var type = ct.Type;
                    if (type.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (type.FullName != null && type.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;

            case FilterMode.Tag:
                foreach (var tagType in entity.Tags)
                {
                    var tagText = tagType.TagName;
                    if (!string.IsNullOrWhiteSpace(tagText) && tagText.Contains(query, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;

            case FilterMode.Smart:
            default:
                if (entity.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (entity.HasComponent<UniqueEntity>())
                {
                    ref var u = ref entity.GetComponent<UniqueEntity>();
                    if (u.uid.Contains(query, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                foreach (var tagType in entity.Tags)
                {
                    var tagText = tagType.TagName;
                    if (!string.IsNullOrWhiteSpace(tagText) && tagText.Contains(query, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                foreach (var ct in entity.Archetype.ComponentTypes)
                {
                    var type = ct.Type;
                    if (type.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
        }
    }

    private bool IsVisible(Entity entity, List<FilterTerm> terms, int depth)
    {
        if (_visibleCache.TryGetValue(entity.Id, out bool cached))
            return cached;

        bool visible = EntityMatchesAll(entity, terms);

        if (!visible && depth < 1024 && entity.HasComponent<CTransform>())
        {
            ref var transform = ref entity.GetComponent<CTransform>();
            var children = transform.Children;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.IsNull)
                    continue;
                if (IsVisible(child, terms, depth + 1))
                {
                    visible = true;
                    break;
                }
            }
        }

        _visibleCache[entity.Id] = visible;
        return visible;
    }

    private const string UngroupedName = "Ungrouped";

    private const ImGuiTreeNodeFlags GroupNodeFlagsBase = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.FramePadding;
    private const ImGuiTreeNodeFlags SubGroupNodeFlagsBase = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.FramePadding;

    private static readonly System.Numerics.Vector4 GroupTextColor = new(0.95f, 0.85f, 0.45f, 1f);
    private static readonly System.Numerics.Vector4 GroupHeaderColor = new(0.25f, 0.22f, 0.12f, 1f);
    private static readonly System.Numerics.Vector4 GroupHeaderHoveredColor = new(0.35f, 0.30f, 0.16f, 1f);
    private static readonly System.Numerics.Vector4 GroupHeaderActiveColor = new(0.35f, 0.30f, 0.16f, 1f);

    private static readonly System.Numerics.Vector4 SubGroupTextColor = new(0.65f, 0.85f, 1f, 1f);
    private static readonly System.Numerics.Vector4 SubGroupHeaderColor = new(0.10f, 0.18f, 0.24f, 1f);
    private static readonly System.Numerics.Vector4 SubGroupHeaderHoveredColor = new(0.14f, 0.25f, 0.33f, 1f);
    private static readonly System.Numerics.Vector4 SubGroupHeaderActiveColor = new(0.14f, 0.25f, 0.33f, 1f);

    private static void PushGroupNodeStyle()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, GroupTextColor);
        ImGui.PushStyleColor(ImGuiCol.Header, GroupHeaderColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GroupHeaderHoveredColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, GroupHeaderActiveColor);
    }

    private static void PopGroupNodeStyle() => ImGui.PopStyleColor(4);

    private static void PushSubGroupNodeStyle()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, SubGroupTextColor);
        ImGui.PushStyleColor(ImGuiCol.Header, SubGroupHeaderColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, SubGroupHeaderHoveredColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, SubGroupHeaderActiveColor);
    }

    private static void PopSubGroupNodeStyle() => ImGui.PopStyleColor(4);

    private static string FormatGroupLabel(string name, int count) => $"{name}  ({count})";

    private void DrawRootEntities(EntityList entities, bool filterActive, List<FilterTerm> terms)
    {
        var groups = new Dictionary<string, Dictionary<string, List<Entity>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            if (entity.IsNull)
                continue;

            if (entity.HasComponent<CTransform>())
            {
                ref var transform = ref entity.GetComponent<CTransform>();
                if (transform.Parent != default)
                    continue;
            }

            if (filterActive && !IsVisible(entity, terms, 0))
                continue;

            string groupName = "Ungrouped";
            string subGroupName = "Ungrouped";

            if (entity.HasComponent<MetaGroup>())
            {
                ref var mg = ref entity.GetComponent<MetaGroup>();

                var g = (mg.GroupName ?? string.Empty).Trim();
                if (g.Length > 0)
                    groupName = g;

                var sg = (mg.SubGroupName ?? string.Empty).Trim();
                if (sg.Length > 0)
                    subGroupName = sg;
            }

            if (!groups.TryGetValue(groupName, out var subGroups))
            {
                subGroups = new Dictionary<string, List<Entity>>(StringComparer.OrdinalIgnoreCase);
                groups[groupName] = subGroups;
            }

            if (!subGroups.TryGetValue(subGroupName, out var list))
            {
                list = new List<Entity>();
                subGroups[subGroupName] = list;
            }

            list.Add(entity);
        }

        var groupNames = new List<string>(groups.Keys);
        groupNames.Sort((a, b) =>
        {
            bool au = string.Equals(a, "Ungrouped", StringComparison.OrdinalIgnoreCase);
            bool bu = string.Equals(b, "Ungrouped", StringComparison.OrdinalIgnoreCase);
            if (au != bu)
                return au ? 1 : -1;
            return StringComparer.OrdinalIgnoreCase.Compare(a, b);
        });

        for (int gi = 0; gi < groupNames.Count; gi++)
        {
            var groupName = groupNames[gi];
            var subGroups = groups[groupName];
            int groupCount = 0;
            foreach (var kv in subGroups)
                groupCount += kv.Value.Count;

            ImGui.PushID(groupName);
            var groupFlags = GroupNodeFlagsBase | (filterActive ? ImGuiTreeNodeFlags.DefaultOpen : 0);

            PushGroupNodeStyle();
            bool groupOpen = ImGui.TreeNodeEx(FormatGroupLabel(groupName, groupCount), groupFlags);
            PopGroupNodeStyle();

            if (groupOpen)
            {
                bool showSubGroups = subGroups.Count > 1;
                if (!showSubGroups && subGroups.Count == 1)
                {
                    foreach (var kv in subGroups)
                    {
                        if (!string.Equals(kv.Key, "Ungrouped", StringComparison.OrdinalIgnoreCase))
                            showSubGroups = true;
                        break;
                    }
                }

                if (!showSubGroups)
                {
                    foreach (var kv in subGroups)
                    {
                        var list = kv.Value;
                        list.Sort((a, b) => a.Id.CompareTo(b.Id));

                        for (int i = 0; i < list.Count; i++)
                        {
                            var e = list[i];
                            ImGui.PushID(e.Id);
                            DrawEntityNode(e, filterActive, terms);
                            ImGui.PopID();
                        }

                        break;
                    }
                }
                else
                {
                    var subGroupNames = new List<string>(subGroups.Keys);
                    subGroupNames.Sort((a, b) =>
                    {
                        bool au = string.Equals(a, "Ungrouped", StringComparison.OrdinalIgnoreCase);
                        bool bu = string.Equals(b, "Ungrouped", StringComparison.OrdinalIgnoreCase);
                        if (au != bu)
                            return au ? 1 : -1;
                        return StringComparer.OrdinalIgnoreCase.Compare(a, b);
                    });

                    for (int sgi = 0; sgi < subGroupNames.Count; sgi++)
                    {
                        var subGroupName = subGroupNames[sgi];
                        var list = subGroups[subGroupName];
                        list.Sort((a, b) => a.Id.CompareTo(b.Id));

                        ImGui.PushID(subGroupName);
                        var subFlags = SubGroupNodeFlagsBase | (filterActive ? ImGuiTreeNodeFlags.DefaultOpen : 0);

                        PushSubGroupNodeStyle();
                        bool subOpen = ImGui.TreeNodeEx(FormatGroupLabel(subGroupName, list.Count), subFlags);
                        PopSubGroupNodeStyle();

                        if (subOpen)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                var e = list[i];
                                ImGui.PushID(e.Id);
                                DrawEntityNode(e, filterActive, terms);
                                ImGui.PopID();
                            }

                            ImGui.TreePop();
                        }

                        ImGui.PopID();
                    }
                }

                ImGui.TreePop();
            }

            ImGui.PopID();
        }
    }

    private void DrawEntityNode(Entity entity, bool filterActive, List<FilterTerm> terms)
    {
        if (filterActive && !IsVisible(entity, terms, 0))
            return;

        string name = GetEntityDisplayName(entity);

        var isSelected = entity == Data.selectedEntity;
        var flags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow
                                                     | (isSelected ? ImGuiTreeNodeFlags.Selected : 0)
                                                     | (filterActive ? ImGuiTreeNodeFlags.DefaultOpen : 0);

        var open = ImGui.TreeNodeEx($"{name} [{entity.Id}]", flags);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsItemToggledOpen())
            Data.selectedEntity = entity;

        if (!open)
            return;

        if (entity.HasComponent<CTransform>())
        {
            ref var transform = ref entity.GetComponent<CTransform>();
            var children = transform.Children;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.IsNull)
                    continue;

                if (filterActive && !IsVisible(child, terms, 0))
                    continue;

                ImGui.PushID(child.Id);
                DrawEntityNode(child, filterActive, terms);
                ImGui.PopID();
            }
        }

        ImGui.TreePop();
    }
}