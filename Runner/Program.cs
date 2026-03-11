using Engine.Core;
using Foster.Framework;
using Runner;

internal static class Program
{
    public static void Main(string[] args)
    {
        //这里设置的是窗口大小
        using var gameContent = new GameApp(new AppConfig(
            "Game",
            "Game",
            Const._2K.X,
            Const._2K.Y,Flags:AppFlags.GraphicsDebugging));
        gameContent.Run();

    }
}

