using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Engine.Asset.Source;
using Engine.Asset.v1;
using Foster.Framework;

namespace Engine.Asset;

public static class SpriteAtlasLoader
{
    public static SpriteAtlasLoadResult Load(GraphicsDevice graphicsDevice, IAssetSource source, string spriteSubDirectory = "Sprites")
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(source);

        var aseFiles = new Dictionary<string, Aseprite>();
        var imageFiles = new Dictionary<string, Image>();

        switch (source)
        {
            case DirectoryAssetSource directoryAssetSource:
                LoadFromDirectory(directoryAssetSource, spriteSubDirectory, aseFiles, imageFiles);
                break;
            case ZipAssetSource zipAssetSource:
                LoadFromZip(zipAssetSource, spriteSubDirectory, aseFiles, imageFiles);
                break;
            default:
                throw new NotSupportedException($"Unsupported asset source: {source.GetType().Name}");
        }

        var output = Pack(aseFiles, imageFiles);
        var atlas = new Texture(graphicsDevice, output.Pages[0], name: "Atlas");
        var subtextures = new Dictionary<string, Subtexture>();
        var sprites = new Dictionary<string, Sprite>();

        foreach (var entry in output.Entries)
            subtextures[entry.Name] = new Subtexture(atlas, entry.Source, entry.Frame);

        foreach (var (name, ase) in aseFiles)
        {
            var sprite = new Sprite(name, GetOrigin(ase));

            for (int i = 0; i < ase.Frames.Length; i++)
                sprite.Frames.Add(new(subtextures[$"{name}/{i}"], ase.Frames[i].Duration / 1000.0f));

            foreach (var tag in ase.Tags)
            {
                if (!string.IsNullOrEmpty(tag.Name))
                    sprite.AddAnimation(tag.Name, tag.From, tag.To - tag.From + 1);
            }

            sprites[name] = sprite;
        }

        return new SpriteAtlasLoadResult(atlas, sprites, subtextures);
    }

    static void LoadFromDirectory(
        DirectoryAssetSource source,
        string spriteSubDirectory,
        Dictionary<string, Aseprite> aseFiles,
        Dictionary<string, Image> imageFiles)
    {
        var spritesPath = source.GetFullPath(spriteSubDirectory);
        if (!Directory.Exists(spritesPath))
            throw new DirectoryNotFoundException($"Sprite directory not found: {spritesPath}");

        foreach (var file in Directory.EnumerateFiles(spritesPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            var name = Path.ChangeExtension(Path.GetRelativePath(spritesPath, file), null)!.Replace('\\', '/');

            if (string.Equals(ext, ".ase", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".asesprite", StringComparison.OrdinalIgnoreCase))
            {
                var ase = new Aseprite(file);
                if (ase.Frames.Length > 0)
                    aseFiles[name] = ase;
                continue;
            }

            if (string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase))
                imageFiles[name] = new Image(file);
        }
    }

    static void LoadFromZip(
        ZipAssetSource source,
        string spriteSubDirectory,
        Dictionary<string, Aseprite> aseFiles,
        Dictionary<string, Image> imageFiles)
    {
        if (!source.IsInitialized)
            source.Initialize();

        var subDir = spriteSubDirectory.Replace('\\', '/').Trim('/');
        var subDirPrefix = subDir + "/";

        foreach (var entry in AssetsV1.Zip.Entries)
        {
            var relativePath = entry.FullName.Replace('\\', '/');
            if (!relativePath.StartsWith(subDirPrefix, StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith("/", StringComparison.Ordinal))
                continue;

            var ext = Path.GetExtension(relativePath);
            var name = Path.ChangeExtension(relativePath.Substring(subDirPrefix.Length), null)!.Replace('\\', '/');

            if (!source.TryOpen(relativePath, out var stream) || stream == null)
                continue;

            using (stream)
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Position = 0;

                if (string.Equals(ext, ".ase", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ext, ".asesprite", StringComparison.OrdinalIgnoreCase))
                {
                    var ase = new Aseprite(ms);
                    if (ase.Frames.Length > 0)
                        aseFiles[name] = ase;
                    continue;
                }

                if (string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase))
                    imageFiles[name] = new Image(ms);
            }
        }
    }

    static Packer.Output Pack(Dictionary<string, Aseprite> aseFiles, Dictionary<string, Image> imageFiles)
    {
        var packer = new Packer
        {
            Padding = 4
        };

        foreach (var (name, ase) in aseFiles)
        {
            var frames = ase.RenderAllFrames();
            for (int i = 0; i < frames.Length; i++)
                packer.Add($"{name}/{i}", frames[i]);
        }

        foreach (var (name, image) in imageFiles)
            packer.Add(name, image);

        return packer.Pack();
    }

    static Vector2 GetOrigin(Aseprite ase)
    {
        if (ase.Slices.Count > 0 &&
            ase.Slices[0].Keys.Length > 0 &&
            ase.Slices[0].Keys[0].Pivot.HasValue)
            return ase.Slices[0].Keys[0].Pivot!.Value;

        return Vector2.Zero;
    }
}

public sealed class SpriteAtlasLoadResult
{
    public Texture Atlas { get; }
    public Dictionary<string, Sprite> Sprites { get; }
    public Dictionary<string, Subtexture> Subtextures { get; }

    public SpriteAtlasLoadResult(Texture atlas, Dictionary<string, Sprite> sprites, Dictionary<string, Subtexture> subtextures)
    {
        Atlas = atlas;
        Sprites = sprites;
        Subtextures = subtextures;
    }
}