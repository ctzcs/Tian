
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Asset.Pipeline;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Serialize;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Engine.Core.Extensions;

public static class SerializeExtensions
{
    extension(EntityStore store)
    {
        /// <summary>
        /// Json存储版本
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        public void SaveEntity<T>(string path) where T : struct, ITag
        {
            var serializer = new EntitySerializer();
            var query = store.Query().AllTags(Tags.Get<T>());
            using var writeStream = new FileStream(path, FileMode.Create);
            serializer.WriteEntities(query.Entities, writeStream);
            var ecb = store.GetCommandBuffer();
            foreach (var entity in query.Entities)
            {
                ecb.DeleteEntity(entity.Id);
            }
            ecb.Playback();
            writeStream.Close();
            
        }

        /// <summary>
        /// Json存储版本
        /// </summary>
        /// <param name="path"></param>
        public void LoadEntity(string path)
        {
            var serializer = new EntitySerializer();
            using var readStream = new FileStream(path, FileMode.Open);
            serializer.ReadIntoStore(store,readStream);
            readStream.Close();
        }


        public Entity ReadEntity(string json)
        {
            var entities = AssetDatabase.DataEntities;
            entities.Clear();
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            using var stream = new MemoryStream(bytes);
            var result = AssetDatabase.EntitySerializer.ReadEntities(entities, stream);
            if (result.error != null) throw new JsonException(result.error);
            var converter = AssetDatabase.EntityConverter;
            var entity = converter.DataEntityToEntity(AssetDatabase.DataEntities[0], store, out var error);
            return entity;
        }
        
        
        /// <summary>
        /// Zip压缩版本
        /// </summary>
        /// <param name="path">savePath</param>
        /// <typeparam name="T">Tag标签</typeparam>
        public void SaveEntityGz<T>(string path) where T : struct, ITag
        {
            var serializer = new EntitySerializer();
            var query = store.Query().AllTags(Tags.Get<T>());
            using var fileStream = new FileStream(path, FileMode.Create);
            using var gzip = new GZipStream(fileStream,CompressionLevel.Optimal,true);
            serializer.WriteEntities(query.Entities, gzip);
            var ecb = store.GetCommandBuffer();
            foreach (var entity in query.Entities)
            {
                ecb.DeleteEntity(entity.Id);
            }
            ecb.Playback();
        }
        
        /// <summary>
        /// 直接加载GZip压缩版本
        /// </summary>
        /// <param name="zipPath">Gz文件目录</param>
        public void LoadEntityGz(string zipPath)
        {
            var serializer = new EntitySerializer();
            using var fs = File.OpenRead(zipPath);
            using var gZipStream = new GZipStream(fs,CompressionMode.Decompress, true);
            serializer.ReadIntoStore(store,gZipStream);
        }

        /// <summary>
        /// 从压缩包中加载Gz文件
        /// </summary>
        /// <param name="zipPath">zip路径</param>
        /// <param name="fileName">相对名称包含拓展名，忽略大小写</param>
        public void LoadEntityGz(string zipPath, string fileName)
        {
            var serializer = new EntitySerializer();
            using var reader = Asset.v1.ZipEntryReader.Open(zipPath,fileName);
            if (reader == null) return;
            using var stream = reader.Entry.Open();
            using var gZipStream = new GZipStream(stream,CompressionMode.Decompress, true);
            serializer.ReadIntoStore(store,gZipStream);
        }


        /// <summary>
        /// 从资源压缩包中加载实体Gz文件到Store中
        /// </summary>
        /// <param name="zipPath">zip路径</param>
        /// <param name="fileName">相对名称包含拓展名，忽略大小写</param>
        /// <param name="useCache">重新加载目录</param>
        public void LoadEntityGzCache(string zipPath, string fileName, bool useCache = true)
        {
            var serializer = new EntitySerializer();
            if (useCache)
                Asset.v1.AssetsV1.LazyInitializeCache(zipPath);
            else 
                Asset.v1.AssetsV1.InitializeCache(zipPath);
            Asset.v1.AssetsV1.TryOpenCachedEntry(fileName, out var stream );
            if (stream == null) return;
            using var gZipStream = new GZipStream(stream,CompressionMode.Decompress, true);
            serializer.ReadIntoStore(store,gZipStream);
            stream.Dispose();
        }

        
        
    }
    
    
    [Obsolete("Not using now")]
    public static void SaveFile<T>(string path,T value, bool indented = false)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            IncludeFields = true,
            
        };
        //options.Converters.Add(new Vector2IntKeyConverter());
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var attributes = assembly.GetCustomAttributes<JsonConverterAttribute>();
            foreach (var attribute in attributes)
            {
                if (attribute.ConverterType != null && Activator.CreateInstance(attribute.ConverterType) is JsonConverter converter)
                {
                    options.Converters.Add(converter);
                }
                
            }
        }
        string text = JsonSerializer.Serialize(value,options);
        File.WriteAllText(path, text);
    }

    [Obsolete("Not using now")]
    public static T? LoadFile<T>(string path)
    {
        var options = new JsonSerializerOptions();
        //options.Converters.Add(new Vector2IntKeyConverter());
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var attributes = assembly.GetCustomAttributes<JsonConverterAttribute>();
            foreach (var attribute in attributes)
            {
                if (attribute.ConverterType != null && Activator.CreateInstance(attribute.ConverterType) is JsonConverter converter)
                {
                    options.Converters.Add(converter);
                }
                
            }
        }
        string text = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(text,options);
    }
}