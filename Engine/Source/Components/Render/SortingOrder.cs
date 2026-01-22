using System.Numerics;
using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;

namespace Engine.Render;


public struct SortingOrder : IComponent
{
    public int layerMask; // 1<<31
    public int depth; // 这里注意，如果有不同的material插入，应该预留一个连续的depth区间，保证合批正确
}

/// <summary>
/// 用于Hierarchy
/// </summary>
public struct HierarchyOrder : IComponent
{
    public uint group;
    public int depth;
    public uint index;
}

public static class SortingOrderExtensions
{
    public static int LayerMaskToIndex(int layerMask)
    {
        if (layerMask == 0)
            return 0;

        return BitOperations.TrailingZeroCount(unchecked((uint)layerMask));
    }

    public static int IndexToLayerMask(int layerIndex)
    {
        if ((uint)layerIndex >= 32u)
            return 0;

        return unchecked((int)(1u << layerIndex));
    }
    
    // 16 bit 层数 index
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort NormalizeLayer16(int layerMask)
    {
        if (layerMask == 0)
            return 0;
        uint u = unchecked((uint)layerMask);
        return (ushort)BitOperations.TrailingZeroCount(u);
    }

    // 16 bit 深度index
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort NormalizeDepth16(int depth)
    {
        if (depth <= 0)
            return 0;
        if ((uint)depth > ushort.MaxValue)
            return ushort.MaxValue;
        return (ushort)depth;
    }
}

