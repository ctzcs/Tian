// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


// ReSharper disable ConvertToAutoProperty
namespace Friflo.Engine.ECS.Utils;

public partial struct BitSet
{
    // Compare
    // ------------------------------ IsDefault() - Equals() ------------------------------
#if! NETCOREAPP3_0_OR_GREATER
    internal bool IsDefault() {
        return l0 == 0 &&
               l1 == 0 &&
               l2 == 0 &&
               l3 == 0 &&
               l4 == 0 && l5 == 0 && l6 == 0 && l7 == 0;
    }
    
    internal bool Equals (in BitSet other) {
        return l0 == other.l0 &&
               l1 == other.l1 &&
               l2 == other.l2 &&
               l3 == other.l3 &&
               l4 == other.l4 && l5 == other.l5 &&
               l6 == other.l6 && l7 == other.l7;
    }
    
    public readonly int GetBitCount() {
        return
            NumberOfSetBits(l0) +
            NumberOfSetBits(l1) +
            NumberOfSetBits(l2) +
            NumberOfSetBits(l3) +
            NumberOfSetBits(l4) +
            NumberOfSetBits(l5) +
            NumberOfSetBits(l6) +
            NumberOfSetBits(l7);
    }
#endif

    // Compare
    // ------------------------------------ bit operations ------------------------------------
#if !NET7_0_OR_GREATER
    public readonly bool HasAll(in BitSet bitSet)
    {
        return (l0 & bitSet.l0) == bitSet.l0 &&
               (l1 & bitSet.l1) == bitSet.l1 &&
               (l2 & bitSet.l2) == bitSet.l2 &&
               (l3 & bitSet.l3) == bitSet.l3 &&
               (l4 & bitSet.l4) == bitSet.l4 &&
               (l5 & bitSet.l5) == bitSet.l5 &&
               (l6 & bitSet.l6) == bitSet.l6 &&
               (l7 & bitSet.l7) == bitSet.l7;
    }
    
    public readonly bool HasAny(in BitSet bitSet)
    {
        return (l0 & bitSet.l0) != 0 ||
               (l1 & bitSet.l1) != 0 ||
               (l2 & bitSet.l2) != 0 ||
               (l3 & bitSet.l3) != 0 ||
               (l4 & bitSet.l4) != 0 ||
               (l5 & bitSet.l5) != 0 ||
               (l6 & bitSet.l6) != 0 ||
               (l7 & bitSet.l7) != 0;
    }
    
    
    internal void Add (in BitSet right) {
        l0 |= right.l0;
        l1 |= right.l1;
        l2 |= right.l2;
        l3 |= right.l3;
        l4 |= right.l4;
        l5 |= right.l5;
        l6 |= right.l6;
        l7 |= right.l7;
    }
    
    internal void Remove (in BitSet right) {
        l0 &= ~right.l0;
        l1 &= ~right.l1;
        l2 &= ~right.l2;
        l3 &= ~right.l3;
        l4 &= ~right.l4;
        l5 &= ~right.l5;
        l6 &= ~right.l6;
        l7 &= ~right.l7;
    }

    internal static BitSet Add (in BitSet left, in BitSet right) {
        return new BitSet {
            l0 = left.l0 | right.l0,
            l1 = left.l1 | right.l1,
            l2 = left.l2 | right.l2,
            l3 = left.l3 | right.l3,
            l4 = left.l4 | right.l4,
            l5 = left.l5 | right.l5,
            l6 = left.l6 | right.l6,
            l7 = left.l7 | right.l7,
        };
    }
    
    internal static BitSet Remove (in BitSet left, in BitSet right) {
        return new BitSet {
            l0 = left.l0 & ~right.l0,
            l1 = left.l1 & ~right.l1,
            l2 = left.l2 & ~right.l2,
            l3 = left.l3 & ~right.l3,
            l4 = left.l4 & ~right.l4,
            l5 = left.l5 & ~right.l5,
            l6 = left.l6 & ~right.l6,
            l7 = left.l7 & ~right.l7,
        };
    }
    
    internal static BitSet Added (in BitSet left, in BitSet right) {
        return new BitSet {
            l0 = ~left.l0 & right.l0,
            l1 = ~left.l1 & right.l1,
            l2 = ~left.l2 & right.l2,
            l3 = ~left.l3 & right.l3,
            l4 = ~left.l4 & right.l4,
            l5 = ~left.l5 & right.l5,
            l6 = ~left.l6 & right.l6,
            l7 = ~left.l7 & right.l7,
        };
    }
    
    internal static BitSet Removed (in BitSet left, in BitSet right) {
        return new BitSet {
            l0 = left.l0 & ~right.l0,
            l1 = left.l1 & ~right.l1,
            l2 = left.l2 & ~right.l2,
            l3 = left.l3 & ~right.l3,
            l4 = left.l4 & ~right.l4,
            l5 = left.l5 & ~right.l5,
            l6 = left.l6 & ~right.l6,
            l7 = left.l7 & ~right.l7,
        };
    }
    
    internal static BitSet Changed (in BitSet left, in BitSet right) {
        return new BitSet {
            l0 = left.l0 ^ right.l0,
            l1 = left.l1 ^ right.l1,
            l2 = left.l2 ^ right.l2,
            l3 = left.l3 ^ right.l3,
            l4 = left.l4 ^ right.l4,
            l5 = left.l5 ^ right.l5,
            l6 = left.l6 ^ right.l6,
            l7 = left.l7 ^ right.l7,
        };
    }
    
    internal static BitSet Intersect (in BitSet left, in BitSet right) {
        return new BitSet {
            l0 = left.l0 & right.l0,
            l1 = left.l1 & right.l1,
            l2 = left.l2 & right.l2,
            l3 = left.l3 & right.l3,
            l4 = left.l4 & right.l4,
            l5 = left.l5 & right.l5,
            l6 = left.l6 & right.l6,
            l7 = left.l7 & right.l7,
        };
    }
#endif

    internal static int NumberOfSetBits(long i)
    {
        i -= ((i >> 1) & 0x5555555555555555);
        i = (i & 0x3333333333333333) + ((i >> 2) & 0x3333333333333333);
        return (int)((((i + (i >> 4)) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56);
    }
    
    internal static int TrailingZeroCount(long i) {
        long y;
        if (i == 0) return 64;
        int n = 63;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse - no clue why
        y = i << 32; if (y != 0) { n = n -32; i = y; }
        y = i << 16; if (y != 0) { n = n -16; i = y; }
        y = i << 8;  if (y != 0) { n = n - 8; i = y; }
        y = i << 4;  if (y != 0) { n = n - 4; i = y; }
        y = i << 2;  if (y != 0) { n = n - 2; i = y; }
        return (int)(n - ((i << 1) >>> 63));
    }
}


