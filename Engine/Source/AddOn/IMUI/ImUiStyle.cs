using Foster.Framework;

namespace Engine.IMUI;

public sealed class ImUiStyle
{
    public float WindowPadding { get; set; } = 10f;
    public float ItemSpacing { get; set; } = 8f;
    public float TitleBarHeight { get; set; } = 28f;
    public float FontSize { get; set; } = 18f;
    public float ButtonHeight { get; set; } = 32f;

    public Color WindowBg { get; set; } = new Color(20, 22, 28, 230);
    public Color TitleBg { get; set; } = new Color(42, 46, 58, 255);
    public Color Text { get; set; } = Color.White;
    public Color Button { get; set; } = new Color(64, 70, 88, 255);
    public Color ButtonHover { get; set; } = new Color(82, 90, 112, 255);
    public Color ButtonActive { get; set; } = new Color(98, 108, 136, 255);
}