using System.Numerics;
using Engine;
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using ImGuiNET;
using Cursor = Engine.Core.Input.Cursor;

namespace Editor;

public class GameModeWindow:EditorWindow
{
	// 视口缩放
	private float viewScale = 1f;

	protected override void OnAddWindow()
	{
		base.OnAddWindow();
		IsOpen = true;
	}

	public override void Update()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		
		//TODO 这里的问题是，当在编辑器中视口失去焦点的时候，由于Content直接读取的引擎的输入设备帧数据，导致无法在editor层屏蔽
		if (ImGui.Begin("GameScene", ImGuiWindowFlags.MenuBar))
		{
			if (ImGui.BeginMenuBar())
			{
				ImGui.SetNextItemWidth(80);
				ImGui.SliderFloat("##ScaleSlider", ref viewScale, 0.5f, 6.0f);
				if (ImGui.IsItemHovered())
				{
					ImGui.BeginTooltip();
					ImGui.Text("ScaleRate");
					ImGui.EndTooltip();
				}

                var content = Data?.currentContent;
                if (content != null)
                {
                    var world = content.World;
                    if (world.HasUniqueEntity(Id.Coordinate))
                    {
                        var coordinate = world.GetUniqueEntity(Id.Coordinate).GetComponent<Coordinate>();
                        ImGui.Text($"[{coordinate.MouseCoordinates.X}, {coordinate.MouseCoordinates.Y}]");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("Mouse Coordinates");
                            ImGui.EndTooltip();
                        }
                    }
                }
			}
			ImGui.EndMenuBar();
			
            
            
            
			var size = ImGui.GetContentRegionAvail();
			if (Data.ImRenderer.BeginBatch(size, out var batch, out var bounds))
			{
				if (Data.currentContent != null)
				{
					var wsize = size;
					var center = wsize / 2;
					var screenTarget = Data.currentContent.Target;
					var scale = Calc.Min(
						wsize.X / (float)screenTarget.Width,
						wsize.Y / (float)screenTarget.Height
						) * viewScale;
	
					batch.PushSampler(new(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
					batch.Image(screenTarget, center, screenTarget.Bounds.Size / 2, Vector2.One * scale, 0, Color.White);
					batch.PopSampler();
				}
			}
			Data.ImRenderer.EndBatch();
		}
		ImGui.End();
        ImGui.PopStyleVar(1);
    }
}