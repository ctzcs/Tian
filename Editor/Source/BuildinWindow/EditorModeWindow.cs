using System.Numerics;
using Engine;
using Engine.Components;
using Engine.Core.Extensions;
using Foster.Framework;
using ImGuiNET;
using Cursor = Engine.Core.Input.Cursor;

namespace Editor;

public class EditorModeWindow : EditorWindow
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
		if (ImGui.Begin("Viewport", ImGuiWindowFlags.MenuBar))
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
                    if (world != null && world.HasUniqueEntity(Id.Coordinate))
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
			var contentOrigin = ImGui.GetCursorScreenPos();
			var mousePos = ImGui.GetMousePos();
			var local = mousePos - contentOrigin;
			Cursor.ViewportPosition = new Vector2(-1f, -1f);
			Cursor.GameViewportPosition = new Vector2(-1f, -1f);
			if (Data.ImRenderer.BeginBatch(size, out var batch, out var bounds))
			{
				if (Data.currentContent != null)
				{
					var wsize = size;
					var center = wsize / 2;
					var screenTarget = Data.currentContent.Target;
					if (screenTarget == null || screenTarget.IsDisposed)
					{
						Data.ImRenderer.EndBatch();
						ImGui.End();
						ImGui.PopStyleVar(1);
						return;
					}
					var scale = Calc.Min(
						wsize.X / (float)screenTarget.Width,
						wsize.Y / (float)screenTarget.Height
						) * viewScale;
					var imageSize = screenTarget.Bounds.Size * scale;
					var imageOffset = center - imageSize / 2;
					var localInImage = local - imageOffset;
					var insideImage = localInImage.X >= 0f && localInImage.Y >= 0f && localInImage.X <= imageSize.X && localInImage.Y <= imageSize.Y;

					if (insideImage)
					{
						var rate = localInImage / imageSize;
						rate.X = Calc.Clamp(rate.X, 0f, 1f);
						rate.Y = Calc.Clamp(rate.Y, 0f, 1f);
						Cursor.ViewportPosition = rate;
						var screenPos = rate * screenTarget.Bounds.Size;
						var gameRect = Data.currentContent.GameViewportRect;
						if (gameRect.Width > 0f && gameRect.Height > 0f)
						{
							Cursor.GameViewportPosition = new Vector2(
								(screenPos.X - gameRect.X) / gameRect.Width,
								(screenPos.Y - gameRect.Y) / gameRect.Height);
						}
					}
	
					batch.PushSampler(new(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
					batch.Image(screenTarget, center, screenTarget.Bounds.Size / 2, Vector2.One * scale, 0, Color.White);
					batch.PopSampler();
				// //TODO 这里暂时计算的是鼠标在图片中的位置，但是按理来说应该是换算成比例才对
				// 	//内部因为比例裁切导致图片的起点
				// 	var imageOffset = center - screenTarget.Bounds.Size / 2 * scale;
				// 	//绘制当前内容的起点 当前窗口的起点 + 当前鼠标开始绘制的起点
				// 	var startPos = ImGui.GetWindowPos() + ImGui.GetCursorStartPos();
					
				// 	Vector2 curMousePos = CursorUtils.GetMousePositionInContentRect(startPos) - imageOffset;
				// 	Vector2 imageSize = screenTarget.Bounds.Size * scale; 
				// 	Vector2 rate = curMousePos/imageSize;
				// 	Cursor.ViewportPosition = rate;
				// 	/*Log.Info($"mouse rate: {rate}\n" +
				// 		$"Window Pos{ImGui.GetWindowPos() + ImGui.GetCursorStartPos()}\n" +
				// 		$"wsize:{imageOffset} \n " 
				// 	         + $"screenTargetSize:{screenTarget.Bounds.Size}\n"
				// 	         + $"{screenTarget.Bounds.Size * scale / 2}\n"
				// 		+ $"contentMousePos:{curMousePos}");*/
				
				}
			}
			Data.ImRenderer.EndBatch();
		}
		ImGui.End();
        ImGui.PopStyleVar(1);
    }
}