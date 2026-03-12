using Content.Source.Test_UI2;
using Engine.Core;
using ImGuiNET;

namespace Game.Editor;

public sealed class TestUI2Editor : GameEditor
{
    private int clickCount;
    private bool showStats = true;

    public override string Name => "Test_UI2";

    protected override void Register()
    {
        RegisterWindow("Test_UI2 Tools", DrawWindow);
    }

    private void DrawWindow()
    {
        ImGui.Text($"Editor: {Name}");
        ImGui.Checkbox("Show Stats", ref showStats);

        if (ImGui.Button("Ping"))
            clickCount++;

        ImGui.SameLine();
        ImGui.Text($"Clicks: {clickCount}");

        var content = CurrentContent as Test_UI2;
        if (content == null)
        {
            ImGui.Text("Current Content is not Test_UI2");
            return;
        }

        if (showStats)
        {
            ImGui.Text($"LogicResolution: {content.LogicResolution.X} x {content.LogicResolution.Y}");
        }
    }
}