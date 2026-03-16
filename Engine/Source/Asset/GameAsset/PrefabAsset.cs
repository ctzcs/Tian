using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Asset;
using Engine.Asset.Pipeline;
using Engine.Components;
using Engine.Core.Extensions;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Serialize;


/// <summary>
/// Prefab 资源：包含本地实体树模板和外部 Prefab 引用列表。
/// </summary>
public class PrefabAsset : GameAsset
{
    public string Name { get; set; } = string.Empty;
    public PrefabEntity EntityRoot { get; set; } = new();
    public List<PrefabExternalReference> ExternalReferences { get; set; } = new();

    public PrefabAsset()
    {
        Type = AssetType.Prefab;
    }

    /// <summary>
    /// 从运行时实体树导出 Prefab 数据。
    /// </summary>
    /// <param name="name">Prefab 名称。</param>
    /// <param name="root">导出的根实体。</param>
    public static PrefabAsset FromEntity(string name, Entity root)
    {
        var asset = new PrefabAsset { Name = name };
        asset.EntityRoot = BuildPrefabEntity(root, asset.ExternalReferences);
        return asset;
    }

    /// <summary>
    /// 实例化当前 Prefab（两阶段：先本地树，再挂外部引用）。
    /// </summary>
    /// <param name="targetWorld">目标世界。</param>
    /// <returns>实例化后的根实体。</returns>
    public Entity Instantiate(EntityStore targetWorld)
    {
        return Instantiate(targetWorld, AssetDatabase.LoadPrefabByGuid);
    }

    /// <summary>
    /// 使用自定义加载器实例化 Prefab。
    /// </summary>
    /// <param name="targetWorld">目标世界。</param>
    /// <param name="prefabLoader">根据 Guid 加载外部 Prefab。</param>
    /// <returns>实例化后的根实体。</returns>
    public Entity Instantiate(EntityStore targetWorld, Func<Guid, PrefabAsset?> prefabLoader)
    {
        return InstantiateInternal(targetWorld, prefabLoader, new HashSet<Guid>());
    }

    private Entity InstantiateInternal(EntityStore targetWorld, Func<Guid, PrefabAsset?> prefabLoader, HashSet<Guid> guard)
    {
        if (Guid != Guid.Empty && !guard.Add(Guid))
            throw new InvalidOperationException($"Circular prefab reference detected: {Guid}");

        try
        {
            var map = new Dictionary<Guid, Entity>();
            var root = InstantiateNode(targetWorld, EntityRoot, map);
            //从map中根据guid取出parentEntity，将外部节点挂载parentEntity上
            for (int i = 0; i < ExternalReferences.Count; i++)
            {
                var reference = ExternalReferences[i];
                if (reference.TargetGuid == Guid.Empty) continue;

                Entity mountParent = default;
                if (reference.MountEntityGuid != Guid.Empty)
                {
                    map.TryGetValue(reference.MountEntityGuid, out mountParent);
                }

                if (mountParent.IsNull)
                {
                    if (!map.TryGetValue(reference.ParentEntityGuid, out mountParent) || mountParent.IsNull) continue;
                }

                var nested = prefabLoader(reference.TargetGuid);
                if (nested == null) continue;

                var nestedRoot = nested.InstantiateInternal(targetWorld, prefabLoader, guard);
                if (!nestedRoot.IsNull) nestedRoot.SetParent(mountParent);
            }

            return root;
        }
        finally
        {
            if (Guid != Guid.Empty) guard.Remove(Guid);
        }
    }

    private static PrefabEntity BuildPrefabEntity(Entity entity, List<PrefabExternalReference> references)
    {
        var node = new PrefabEntity
        {
            Name = $"Entity_{entity.Id}",
            Entity = entity
        };

        if (!entity.HasComponent<CTransform>()) return node;

        ref var transform = ref entity.GetComponent<CTransform>();
        for (int i = 0; i < transform.ChildrenCount; i++)
        {
            var child = transform.Children[i];
            if (child.IsNull) continue;
            //如果子节点上有PrefabRef说明有Prefab引用
            if (child.HasComponent<PrefabRef>())
            {
                var prefabRef = child.GetComponent<PrefabRef>();
                if (prefabRef.AssetGuid != Guid.Empty)
                {
                    var mountNode = BuildPrefabEntity(child, references);
                    if (string.IsNullOrWhiteSpace(mountNode.Name) && !string.IsNullOrWhiteSpace(prefabRef.MountKey))
                        mountNode.Name = prefabRef.MountKey;

                    node.Children.Add(mountNode);

                    references.Add(new PrefabExternalReference
                    {
                        ParentEntityGuid = node.EntityGuid,
                        MountEntityGuid = mountNode.EntityGuid,
                        MountKey = string.IsNullOrWhiteSpace(prefabRef.MountKey) ? mountNode.Name : prefabRef.MountKey,
                        TargetGuid = prefabRef.AssetGuid
                    });
                    continue;
                }
            }

            node.Children.Add(BuildPrefabEntity(child, references));
        }

        return node;
    }

    //创建本地树，将Node挂到Parent上，同时在map中将Guid和每个Entity对应
    private static Entity InstantiateNode(EntityStore world, PrefabEntity node, Dictionary<Guid, Entity> map)
    {
        var entity = world.CreateEntity();
        node.Entity.CopyEntity(entity);
        if (entity.Tags.Has<Prefab>()) entity.RemoveTag<Prefab>();

        map[node.EntityGuid] = entity;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = InstantiateNode(world, node.Children[i], map);
            child.SetParent(entity);
        }

        return entity;
    }
}

public class PrefabEntity
{
    public Guid EntityGuid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Entity Entity { get; set; } = new();
    public List<PrefabEntity> Children { get; set; } = new();
}


/// <summary>
/// 外部 Prefab 引用记录。
/// </summary>
public class PrefabExternalReference
{
    /// <summary>挂载父节点（Prefab 内稳定实体 Guid）。</summary>
    public Guid ParentEntityGuid { get; set; }
    /// <summary>优先挂载节点（Prefab 内稳定实体 Guid）。</summary>
    public Guid MountEntityGuid { get; set; }
    /// <summary>挂点别名（主要用于可读性与编辑器显示）。</summary>
    public string MountKey { get; set; } = string.Empty;
    /// <summary>被引用外部 Prefab 的 Guid。</summary>
    public Guid TargetGuid { get; set; }
}


public class PrefabAssetJsonConvert : JsonConverter<PrefabAsset>
{
    public override PrefabAsset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject for PrefabAsset.");
        }

        var asset = new PrefabAsset();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                asset.Type = AssetType.Prefab;
                return asset;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end while reading PrefabAsset.");
            }

            switch (propertyName)
            {
                case nameof(PrefabAsset.Guid):
                    asset.Guid = JsonSerializer.Deserialize<Guid>(ref reader, options);
                    break;
                case nameof(PrefabAsset.Path):
                    asset.Path = JsonSerializer.Deserialize<string>(ref reader, options) ?? string.Empty;
                    break;
                case nameof(PrefabAsset.Type):
                    _ = JsonSerializer.Deserialize<AssetType>(ref reader, options);
                    break;
                case nameof(PrefabAsset.Name):
                    asset.Name = JsonSerializer.Deserialize<string>(ref reader, options) ?? string.Empty;
                    break;
                case nameof(PrefabAsset.EntityRoot):
                    asset.EntityRoot = JsonSerializer.Deserialize<PrefabEntity>(ref reader, options) ?? new PrefabEntity();
                    break;
                case nameof(PrefabAsset.ExternalReferences):
                    asset.ExternalReferences = JsonSerializer.Deserialize<List<PrefabExternalReference>>(ref reader, options) ?? new List<PrefabExternalReference>();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Incomplete JSON while reading PrefabAsset.");
    }

    public override void Write(Utf8JsonWriter writer, PrefabAsset value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(PrefabAsset.Guid));
        JsonSerializer.Serialize(writer, value.Guid, options);

        writer.WritePropertyName(nameof(PrefabAsset.Path));
        JsonSerializer.Serialize(writer, value.Path, options);

        writer.WritePropertyName(nameof(PrefabAsset.Type));
        JsonSerializer.Serialize(writer, AssetType.Prefab, options);

        writer.WritePropertyName(nameof(PrefabAsset.Name));
        JsonSerializer.Serialize(writer, value.Name, options);

        writer.WritePropertyName(nameof(PrefabAsset.EntityRoot));
        JsonSerializer.Serialize(writer, value.EntityRoot, options);

        writer.WritePropertyName(nameof(PrefabAsset.ExternalReferences));
        JsonSerializer.Serialize(writer, value.ExternalReferences, options);

        writer.WriteEndObject();
    }
}

public class PrefabEntityJsonConvert : JsonConverter<PrefabEntity>
{
    public override PrefabEntity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return new PrefabEntity();
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject for PrefabEntity.");

        var value = new PrefabEntity();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return value;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = reader.GetString();
            if (!reader.Read()) throw new JsonException("Unexpected end while reading PrefabEntity.");

            switch (propertyName)
            {
                case nameof(PrefabEntity.EntityGuid):
                    value.EntityGuid = JsonSerializer.Deserialize<Guid>(ref reader, options);
                    break;
                case nameof(PrefabEntity.Name):
                    value.Name = JsonSerializer.Deserialize<string>(ref reader, options) ?? string.Empty;
                    break;
                case nameof(PrefabEntity.Entity):
                    value.Entity = DeserializeEntity(ref reader);
                    break;
                case nameof(PrefabEntity.Children):
                    value.Children = JsonSerializer.Deserialize<List<PrefabEntity>>(ref reader, options) ?? [];
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Incomplete JSON while reading PrefabEntity.");
    }

    public override void Write(Utf8JsonWriter writer, PrefabEntity value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(PrefabEntity.EntityGuid));
        JsonSerializer.Serialize(writer, value.EntityGuid, options);

        writer.WritePropertyName(nameof(PrefabEntity.Name));
        JsonSerializer.Serialize(writer, value.Name, options);

        writer.WritePropertyName(nameof(PrefabEntity.Entity));
        WriteEntityJson(writer, value.Entity);

        writer.WritePropertyName(nameof(PrefabEntity.Children));
        JsonSerializer.Serialize(writer, value.Children, options);

        writer.WriteEndObject();
    }

    private static Entity DeserializeEntity(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null) return new Entity();

        string payload;
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text)) return new Entity();
            using var doc = JsonDocument.Parse(text);
            payload = BuildEntityPayload(doc.RootElement);
        }
        else
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            payload = BuildEntityPayload(doc.RootElement);
        }

        return AssetDatabase.PrefabWorld.ReadEntity(payload);
    }

    private static void WriteEntityJson(Utf8JsonWriter writer, Entity entity)
    {
        if (entity.IsNull)
        {
            writer.WriteNullValue();
            return;
        }

        var json = AssetDatabase.EntitySerializer.WriteEntity(entity);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.WriteTo(writer);
    }

    private static string BuildEntityPayload(JsonElement root)
    {
        return root.ValueKind switch
        {
            JsonValueKind.Array => root.GetRawText(),
            JsonValueKind.Object => $"[{root.GetRawText()}]",
            _ => throw new JsonException($"Invalid entity payload kind: {root.ValueKind}")
        };
    }
}

