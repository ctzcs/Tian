using System.Numerics;
using Engine.Asset.v1;
using Foster.Framework;

namespace Engine.Asset;


/// <summary>
/// 最早写的版本
/// </summary>
public static partial class Assets
{
	public static SpriteFont? Font { get; private set; }
	public static Texture? Atlas { get; private set; }
	public static readonly Dictionary<string, Sprite> Sprites = new();
	public static readonly Dictionary<string, Subtexture> Subtextures = new();
    
	public static void SetFont(SpriteFont? font)
	{
		// 加载主要字体文件
		Font = font; 
	}
	
	public static void LoadRuntime(GraphicsDevice gfx, string spriteSubDirectory = "Sprites")
	{
		if (HasContentAssetsPackage)
		{
			AssetsV1.LazyInitializeCache(ContentAssetsPackagePath!);
			LoadSpritesFromGz(gfx, spriteSubDirectory);
			return;
		}

		Load(gfx);
	}

	public static void Load(GraphicsDevice gfx)
	{
        DeleteCache();
		var spritesPath = Path.Join(EditorAssetsPath, "Sprites");
		var aseFiles = new Dictionary<string, Aseprite>();
        var imageFiles = new Dictionary<string, Image>();
		// 获取所有的ase/asesprite结尾的sprites文件
		//TODO 这里可以从Zip包中加载，给Aseprite加上一个Load(stream)的拓展方法就好
		foreach (var file in Directory.EnumerateFiles(spritesPath, "*.*", SearchOption.AllDirectories))
		{
            var ext = Path.GetExtension(file);
            if (string.Equals(ext, ".ase", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".asesprite", StringComparison.OrdinalIgnoreCase))
            {
                var name = Path.ChangeExtension(Path.GetRelativePath(spritesPath, file), null);
                Log.Info(name);
                try
                {
                    var ase = new Aseprite(file);
                    if (ase.Frames.Length > 0)
                        aseFiles.Add(name, ase);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else if(string.Equals(ext,".png", StringComparison.OrdinalIgnoreCase))
            {
                var name = Path.ChangeExtension(Path.GetRelativePath(spritesPath, file), null);
                Log.Info(name);
                try
                {
                    var png = new Image(file);
                    imageFiles.Add(name, png);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else
            {
                continue;
            }
			
			
		}
        
		// 打包所有的sprites
		Packer.Output output;
		{
			var packer = new Packer();
            packer.Padding = 4;
			foreach (var (name, ase) in aseFiles)
			{
				var frames = ase.RenderAllFrames();
				for (int i = 0; i < frames.Length; i ++)
					packer.Add($"{name}/{i}", frames[i]);
			}

            foreach (var (name,image) in imageFiles)
            {
                packer.Add(name,image);
            }
            
            
            
			/*foreach (var (name, ase) in tilesetFiles)
			{
				var image = ase.RenderFrame(0);
				var columns = image.Width / Game.TileSize;
				var rows = image.Height / Game.TileSize;

				for (int x = 0; x < columns; x ++)
					for (int y = 0; y < rows; y ++)
						packer.Add($"tilesets/{name}{x}x{y}", image, new RectInt(x, y, 1, 1) * Game.TileSize);
			}*/

			output = packer.Pack();
		}

		// create texture file
		Atlas = new Texture(gfx, output.Pages[0], name: "Atlas");

		// create subtextures
		foreach (var it in output.Entries)
			Subtextures.Add(it.Name, new Subtexture(Atlas, it.Source, it.Frame));

		// create sprite assets
		foreach (var (name, ase) in aseFiles)
		{
			// find origin
			Vector2 origin = Vector2.Zero;
			if (ase.Slices.Count > 0 && ase.Slices[0].Keys.Length > 0 && ase.Slices[0].Keys[0].Pivot.HasValue)
				origin = ase.Slices[0].Keys[0].Pivot!.Value;

			var sprite = new Sprite(name, origin);

			// add frames
			for (int i = 0; i < ase.Frames.Length; i ++)
				sprite.Frames.Add(new(GetSubtexture($"{name}/{i}"), ase.Frames[i].Duration / 1000.0f));

			// add animations
			foreach (var tag in ase.Tags)
			{
				if (!string.IsNullOrEmpty(tag.Name))
					sprite.AddAnimation(tag.Name, tag.From, tag.To - tag.From + 1);
			}

			Sprites.Add(name, sprite);
		}
        
        //png图集？
    }

    public static void LoadSpritesFromGz(GraphicsDevice gfx, string spriteSubDirectory = "Sprites")
    {
        Log.Info("load: sprite from gz");
        DeleteCache();
        var zipArchive= AssetsV1.Zip;
        var aseFiles = new Dictionary<string, Aseprite>();
        var imageFiles = new Dictionary<string, Image>();
        
        var subDir = spriteSubDirectory.Replace('\\', '/').Trim('/');
        var subDirPrefix = subDir + "/";

        foreach (var entry in zipArchive.Entries)
        {
            var relativePath = entry.FullName.Replace('\\', '/');
            if (!relativePath.StartsWith(subDirPrefix, StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith("/", StringComparison.Ordinal))
                continue;

            var ext = Path.GetExtension(relativePath);
            var name = Path.ChangeExtension(relativePath.Substring(subDirPrefix.Length), null)!.Replace('\\', '/');
            Log.Info($"packing: name-{name} full_name-{relativePath}");

            if (!AssetsV1.TryOpenCachedEntry(relativePath, out var stream) || stream == null)
                continue;

            try
            {
                using (stream)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Position = 0;

                    if (string.Equals(ext, ".ase", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ext, ".asesprite", StringComparison.OrdinalIgnoreCase))
                    {
                        var ase = new Aseprite(ms);
                        if (ase.Frames.Length > 0)
                            aseFiles[name] = ase;
                    }
                    else if (string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase))
                    {
                        imageFiles[name] = new Image(ms);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
		// 打包所有的sprites
		Packer.Output output;
		{
			var packer = new Packer();
            packer.Padding = 4;
			foreach (var (name, ase) in aseFiles)
			{
				var frames = ase.RenderAllFrames();
				for (int i = 0; i < frames.Length; i ++)
					packer.Add($"{name}/{i}", frames[i]);
			}

            foreach (var (name,image) in imageFiles)
            {
                packer.Add(name,image);
            }
            
            
            
			/*foreach (var (name, ase) in tilesetFiles)
			{
				var image = ase.RenderFrame(0);
				var columns = image.Width / Game.TileSize;
				var rows = image.Height / Game.TileSize;

				for (int x = 0; x < columns; x ++)
					for (int y = 0; y < rows; y ++)
						packer.Add($"tilesets/{name}{x}x{y}", image, new RectInt(x, y, 1, 1) * Game.TileSize);
			}*/

			output = packer.Pack();
		}

		// create texture file
		Atlas = new Texture(gfx, output.Pages[0], name: "Atlas");

		// create subtextures
		foreach (var it in output.Entries)
			Subtextures.Add(it.Name, new Subtexture(Atlas, it.Source, it.Frame));

		// create sprite assets
		foreach (var (name, ase) in aseFiles)
		{
			// find origin
			Vector2 origin = Vector2.Zero;
			if (ase.Slices.Count > 0 && ase.Slices[0].Keys.Length > 0 && ase.Slices[0].Keys[0].Pivot.HasValue)
				origin = ase.Slices[0].Keys[0].Pivot!.Value;

			var sprite = new Sprite(name, origin);

			// add frames
			for (int i = 0; i < ase.Frames.Length; i ++)
				sprite.Frames.Add(new(GetSubtexture($"{name}/{i}"), ase.Frames[i].Duration / 1000.0f));

			// add animations
			foreach (var tag in ase.Tags)
			{
				if (!string.IsNullOrEmpty(tag.Name))
					sprite.AddAnimation(tag.Name, tag.From, tag.To - tag.From + 1);
			}

			Sprites.Add(name, sprite);
		}
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