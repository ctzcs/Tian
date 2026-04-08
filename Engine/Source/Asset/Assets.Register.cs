using System.Numerics;
using System.Text.Json;
using Engine.Core;
using Engine.Core.Extensions;
using Engine.Asset.Pipeline;
using Engine.Components;
using Foster.Framework;
using Friflo.Engine.ECS;
using Rect = Foster.Framework.Rect;

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
        string assetsPath = ProjectConfigUtils.ResolveEditorAssetsRootPath();
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

    public static void TestPrefabTwoStageInstantiate()
    {
        // 1) 构造“主 Prefab”源数据：一个本地子节点 + 一个外部 Prefab 引用节点
        var sourceWorld = new EntityStore();

        var root = sourceWorld.CreateEntity();
        root.AddComponent<CTransform>(new CTransform(default, Vector2.Zero, 0f, Vector2.One));

        var localChild = sourceWorld.CreateEntity();
        localChild.AddComponent<CTransform>(new CTransform(default, Vector2.Zero, 0f, Vector2.One));
        localChild.AddComponent<MetaGroup>(new MetaGroup { GroupName = "Local", SubGroupName = "Inline" });
        localChild.SetParent(root);

        var nestedPrefabGuid = Guid.NewGuid();
        var nestedRefHolder = sourceWorld.CreateEntity();
        nestedRefHolder.AddComponent<CTransform>(new CTransform(default, Vector2.Zero, 0f, Vector2.One));
        nestedRefHolder.AddComponent<PrefabRef>(new PrefabRef { AssetGuid = nestedPrefabGuid, MountKey = "SocketA" });
        nestedRefHolder.SetParent(root);

        // 2) 导出主 Prefab：FromEntity 会把 PrefabRef 子节点提取到 ExternalReferences
        var mainPrefab = PrefabAsset.FromEntity("Main", root);
        mainPrefab.Guid = Guid.NewGuid();

        if (mainPrefab.EntityRoot.Children.Count != 1)
            throw new Exception($"Expected inline child count = 1, actual: {mainPrefab.EntityRoot.Children.Count}");
        if (mainPrefab.ExternalReferences.Count != 1)
            throw new Exception($"Expected external refs count = 1, actual: {mainPrefab.ExternalReferences.Count}");

        // 3) 准备被引用的“外部 Prefab”
        var nestedSourceWorld = new EntityStore();
        var nestedRoot = nestedSourceWorld.CreateEntity();
        nestedRoot.AddComponent<CTransform>(new CTransform(default, Vector2.Zero, 0f, Vector2.One));
        nestedRoot.AddComponent<MetaGroup>(new MetaGroup { GroupName = "Nested", SubGroupName = "External" });

        var nestedPrefab = PrefabAsset.FromEntity("Nested", nestedRoot);
        nestedPrefab.Guid = nestedPrefabGuid;

        var assetsRoot = AssetDatabase.ResolveMetaAssetsPath();
        var prefabDir = Path.Combine(assetsRoot, "Prefab", "TwoStage");
        Directory.CreateDirectory(prefabDir);

        var mainPath = Path.Combine(prefabDir, "Main.prefab");
        var nestedPath = Path.Combine(prefabDir, "Nested.prefab");

        mainPrefab.Path = Path.GetRelativePath(assetsRoot, mainPath);
        nestedPrefab.Path = Path.GetRelativePath(assetsRoot, nestedPath);

        var prefabOptions = AssetDatabase.GetPrefabJsonOptions();
        File.WriteAllText(mainPath, JsonSerializer.Serialize(mainPrefab, prefabOptions));
        File.WriteAllText(nestedPath, JsonSerializer.Serialize(nestedPrefab, prefabOptions));

        var loadedMain = AssetDatabase.LoadPrefabByPath(mainPath);
        if (loadedMain == null) throw new Exception("LoadPrefabByPath failed for Main.prefab.");

        // 4) 运行时实例化（两阶段）：先本地树，再挂外部引用
        var runtimeWorld = new EntityStore();
        var instanceRoot = loadedMain.Instantiate(runtimeWorld, AssetDatabase.LoadPrefabByGuid);

        if (instanceRoot.IsNull) throw new Exception("Instantiate failed: root is null.");
        if (!instanceRoot.HasComponent<CTransform>()) throw new Exception("Instantiate failed: root has no CTransform.");

        // 5) 校验结果：root 下应该同时存在 Local 与 Nested 两个子节点
        ref var transform = ref instanceRoot.GetComponent<CTransform>();
        if (transform.ChildrenCount != 2)
            throw new Exception($"Expected runtime root children = 2, actual: {transform.ChildrenCount}");

        var foundLocal = ContainsMetaGroup(instanceRoot, "Local");
        var foundNested = ContainsMetaGroup(instanceRoot, "Nested");

        if (!foundLocal) throw new Exception("Runtime tree missing local inline child.");
        if (!foundNested) throw new Exception("Runtime tree missing mounted external prefab child.");

        // 6) 打印运行时实体树，便于观察加载结果
        LogEntityTree(instanceRoot, "RuntimeRoot");
    }

    private static void LogEntity(string label, Entity entity)
    {
        if (entity.IsNull)
        {
            Log.Info($"{label}: Null");
            return;
        }

        var metaText = "NoMetaGroup";
        if (entity.HasComponent<MetaGroup>())
        {
            var meta = entity.GetComponent<MetaGroup>();
            metaText = $"MetaGroup={meta.GroupName}/{meta.SubGroupName}";
        }

        Log.Info($"{label}: Id={entity.Id}, Pid={entity.Pid}, {metaText}");
    }

    private static void LogEntityTree(Entity root, string label)
    {
        if (root.IsNull)
        {
            Log.Info($"{label}: NullRoot");
            return;
        }

        LogEntity(label, root);
        if (!root.HasComponent<CTransform>()) return;

        ref var transform = ref root.GetComponent<CTransform>();
        for (int i = 0; i < transform.ChildrenCount; i++)
        {
            var child = transform.Children[i];
            LogEntity($"{label}.Child[{i}]", child);
        }
    }

    private static bool ContainsMetaGroup(Entity root, string groupName)
    {
        if (root.IsNull) return false;
        if (root.HasComponent<MetaGroup>())
        {
            var meta = root.GetComponent<MetaGroup>();
            if (meta.GroupName == groupName) return true;
        }

        if (!root.HasComponent<CTransform>()) return false;

        ref var transform = ref root.GetComponent<CTransform>();
        for (int i = 0; i < transform.ChildrenCount; i++)
        {
            if (ContainsMetaGroup(transform.Children[i], groupName)) return true;
        }

        return false;
    }

}



//