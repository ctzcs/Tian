using System.Numerics;
using Engine.Asset.Source;
using Engine.Asset.v1;
using Foster.Framework;

namespace Engine.Asset;


/// <summary>
/// 最早写的版本
/// </summary>
public static partial class Assets
{
	public static SpriteFont? Font { get; private set; }
	public static Texture? Atlas { get; internal set; }
	public static readonly Dictionary<string, Sprite> Sprites = new();
	public static readonly Dictionary<string, Subtexture> Subtextures = new();
    
	public static void SetFont(SpriteFont? font)
	{
		// 加载主要字体文件
		Font = font; 
	}
	
	public static void LoadRuntime(GraphicsDevice gfx, string spriteSubDirectory = "Sprites")
	{
		ApplyLoadResult(SpriteAtlasLoader.Load(gfx, AssetManager.CreateRuntimeSource(), spriteSubDirectory));
	}

	public static void Load(GraphicsDevice gfx)
	{
		ApplyLoadResult(SpriteAtlasLoader.Load(gfx, new DirectoryAssetSource(EditorAssetsPath)));
    }

    public static void LoadSpritesFromGz(GraphicsDevice gfx, string spriteSubDirectory = "Sprites")
    {
        if (string.IsNullOrWhiteSpace(ContentAssetsPackagePath))
            throw new FileNotFoundException("Runtime asset package not found.");

        ApplyLoadResult(SpriteAtlasLoader.Load(gfx, new ZipAssetSource(ContentAssetsPackagePath), spriteSubDirectory));
    }
    
    static void ApplyLoadResult(SpriteAtlasLoadResult result)
    {
        DeleteCache();
        Atlas = result.Atlas;

        foreach (var (name, sprite) in result.Sprites)
            Sprites[name] = sprite;

        foreach (var (name, subtexture) in result.Subtextures)
            Subtextures[name] = subtexture;
    }
    
	/// <summary>
	/// 卸载资源
	/// </summary>
	public static void DeleteCache()
	{
		Atlas?.Dispose();
		Atlas = null;
		Font = null;

		Sprites.Clear();
		Subtextures.Clear();
		
	}

	/// <summary>
	/// 获取Sprite
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public static Sprite? GetSprite(string name)
	{
		if (Sprites.TryGetValue(name, out var value))
			return value;
		return null;
	}
	

	/// <summary>
	/// 获取子纹理
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public static Subtexture GetSubtexture(string name)
    {
        return Subtextures.TryGetValue(name, out var value) ? value : new();
    }

}