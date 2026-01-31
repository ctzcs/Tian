using Engine.Core;
using Friflo.Engine.ECS;

namespace Engine.Physics;

public struct SpatialHash:IComponent
{
    public Vector2Int index;
    public Vector2Int chunkIndex;
    public Vector2Int gridIndex;
}