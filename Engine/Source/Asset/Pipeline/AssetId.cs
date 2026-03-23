using System;

namespace Engine.Asset.Pipeline;


//GUID封装 - 每一个引擎的资源类型都应该有一个Id 用于引用
public record struct AssetId(Guid Value)
{
    public static AssetId New() => new AssetId(Guid.NewGuid());
}