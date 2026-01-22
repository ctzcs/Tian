using Engine.Core.Extensions;
using Engine.Core.Structure;
using Engine.Tilemap;
using Foster.Framework;

namespace Engine.Asset;

public partial class Assets
{
    private static Dictionary<Guid, GameAsset>  _allAssets = new ();
    private static Dictionary<Type,List<GameAsset>> _assetTypeGroup = new();
    
    /// <summary>
    /// 先将所有的文件头注册进来
    /// </summary>
    public static void RegisterAll()
    {
        _allAssets.Clear();
        _assetTypeGroup.Clear();
        string assetsPath = AssetsPath;
        var directories = Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories);
        foreach (var directory in directories)
        {
            var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Log.Info($"Loading assets from {file}");
            }
            //文件类型处理
            //fft
            //ase->打包成动画
            //hlsl
            //json->反序列化
        }
    }
    
}

/// <summary>
/// 
/// </summary>
public class WorldRegister
{
    //
    public List<GameAsset> FindAllWorldAsset()
    {
        return null;
    }
}

/// <summary>
/// 
/// </summary>
public class TilemapRegister
{
    //从Type中找到所有的

    public List<GameAsset> FindAllTilemapAsset()
    {
        return null;
    }
}




public static class AssetTest
{
    public static void Test()
    {
        Assets.RegisterAll();
    }



    public static void TestWrite()
    {
        TilemapAsset map = new TilemapAsset();
        map.Path = "Tilemap.asset";
        map.SetTile(0,new Vector2Int(0,0),new Rect(1,1),Color.Blue,true);
        SerializeExtensions.SaveFile("Tilemap.asset",map,false);
    }
    
}



//