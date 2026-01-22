using System.Collections;
using System.Numerics;
using Editor;
using ImGuiNET;

namespace Engine.Editor.Drawer;

public sealed class BuiltinListDrawer : IInspectorDrawer
    {

        public int Order => 0;

        public bool Supports(Type type) =>
            typeof(IList).IsAssignableFrom(type) && type.IsGenericType;

        public bool Draw(string label, Type type, ref object? val)
        {
            var elemType = type.GetGenericArguments()[0];
            val ??= Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType))!;
            var list = (IList)val;
            var changed = false;
            var opened = ImGui.TreeNodeEx($"{label} [{list.Count}]", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!opened) return changed;

            ImGui.PushID(label);
            var style = ImGui.GetStyle();
            float scale = 0.85f;
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(style.FramePadding.X * scale, style.FramePadding.Y * scale));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(style.ItemSpacing.X * scale, style.ItemSpacing.Y * scale));
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(style.CellPadding.X * scale, style.CellPadding.Y * scale));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);

            float upWidth = ImGui.CalcTextSize("Up").X + style.FramePadding.X * 2f;
            float downWidth = ImGui.CalcTextSize("Down").X + style.FramePadding.X * 2f;
            float removeWidth = ImGui.CalcTextSize("Remove").X + style.FramePadding.X * 2f;
            float actionWidth = upWidth + downWidth + removeWidth + style.ItemSpacing.X * 2f;
            if (ImGui.BeginTable($"{label}_table", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.NoPadOuterX))
            {
                ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, actionWidth);

                for (int i = 0, count = list.Count; i < count; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.PushID(i);
                    object? item = list[i];
                    var itemChanged = InspectorUtil.DrawField($"{label}[{i}]", elemType, ref item);
                    ImGui.TableSetColumnIndex(1);
                    bool acted = false;
                    if (i > 0)
                    {
                        if (ImGui.Button($"Up##{label}_{i}"))
                        {
                            var tmp = list[i - 1];
                            list[i - 1] = list[i];
                            list[i] = tmp;
                            changed = true;
                            acted = true;
                        }
                    }
                    ImGui.SameLine();
                    if (i < count - 1)
                    {
                        if (ImGui.Button($"Down##{label}_{i}"))
                        {
                            var tmp = list[i + 1];
                            list[i + 1] = list[i];
                            list[i] = tmp;
                            changed = true;
                            acted = true;
                        }
                    }
                    if (i == count - 1)
                    {
                        ImGui.SameLine();
                        if (ImGui.Button($"Remove##{label}_{i}"))
                        {
                            list.RemoveAt(count - 1);
                            changed = true;
                            acted = true;
                        }
                    }
                    if (itemChanged)
                    {
                        list[i] = item!;
                        changed = true;
                    }
                    ImGui.PopID();
                    if (acted) { break; }
                }
                ImGui.EndTable();
            }

            var addWidth = ImGui.CalcTextSize("Add").X + style.FramePadding.X * 2f;
            var avail = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, avail - addWidth));
            if (ImGui.Button($"Add##{label}"))
            {
                list.Add(Activator.CreateInstance(elemType)!);
                changed = true;
            }
            ImGui.PopStyleVar(4);
            ImGui.PopID();
            ImGui.TreePop();

            return changed;
        }
    }
    