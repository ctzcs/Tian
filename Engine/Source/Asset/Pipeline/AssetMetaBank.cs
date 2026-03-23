using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Engine.Asset.Pipeline;

public class AssetMetaBank
{
    public string Version = "1";
    public DateTime GeneratedAt;
    public string AssetRootPath = "Assets";
    public List<Entry> AssetIndexFiles = new();
    [JsonIgnore]
    public Dictionary<AssetId, Entry>? AssetIndexFileDic;
    public class Entry
    {
        public AssetId Id;
        public string? Path;
    }

    public Dictionary<AssetId, Entry> GetEntryDic()
    {
        var dic = new Dictionary<AssetId, Entry>();
        foreach (var entry in AssetIndexFiles)
        {
            dic.Add(entry.Id, entry);
        }
        return dic;
    }
    
    public void SetEntries(List<Entry> entries)
    {
        AssetIndexFiles.Clear();
        AssetIndexFiles.AddRange(entries);
    }

    public bool TryGetPath(AssetId assetId, out string? path)
    {
        path = string.Empty;
        AssetIndexFileDic ??= GetEntryDic();

        if (!AssetIndexFileDic.TryGetValue(assetId, out Entry? entry)) return false;
        path = entry.Path;
        return true;
    }


    public void TryLoadAsset(string path)
    {
        
    }
}