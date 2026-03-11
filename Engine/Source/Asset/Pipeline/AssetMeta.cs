using ImGuiNET;

namespace Engine.Asset.Pipeline;
//序列化数据(Hash, ImporterID, Dependencies)
public class AssetMeta
{
    public AssetId Id;
    public string Name;
    public string Ext;
    public string Path;
    public int Tags; // tag id

    public AssetMeta(string name,string ext,string path, int tags)
    {
        Id = AssetId.New();
        Name = name;
        Path = path;
        Tags = tags;
        Ext = ext;
    }
}