// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Engine.ECS.Utils;

public struct BitSetEnumerator
{
    private readonly    long    l0;     //  8
    private readonly    long    l1;     //  8
    private readonly    long    l2;     //  8
    private readonly    long    l3;     //  8
    private readonly    long    l4;     //  8
    private readonly    long    l5;     //  8
    private readonly    long    l6;     //  8
    private readonly    long    l7;     //  8
    
    private             long    lng;    //  8   - 64 bits
    private             int     lngPos; //  4   - range: [0, 1, 2, 3, 4] - higher values are not assigned
    private             int     curPos; //  4   - range: [0, ..., 255]
    
    internal BitSetEnumerator(in BitSet bitSet) {
        l0  = bitSet.l0;
        l1  = bitSet.l1;
        l2  = bitSet.l2;
        l3  = bitSet.l3;
        l4  = bitSet.l4;
        l5  = bitSet.l5;
        l6  = bitSet.l6;
        l7  = bitSet.l7;
        lng = l0;
    }
    
    /// <returns>the index of the current bit == 1. The index range is [0, ... , 255]</returns>
    public readonly int Current => curPos;
    
    // --- IEnumerator
    public void Reset() {
        lng     = l0;
        lngPos  = 0;
        curPos  = 0;
    }
    
    public bool MoveNext()
    {
        while (true)
        {
            if (lng != 0) {
#if NETCOREAPP3_0_OR_GREATER
                var bitPos  = System.Numerics.BitOperations.TrailingZeroCount(lng);
#else
                var bitPos  = BitSet.TrailingZeroCount(lng);
#endif
                lng        ^= 1L << bitPos;
                curPos      = (lngPos << 6) + bitPos;
                return true;
            }
            switch (++lngPos) {
            //  case 0      not possible: lngPos > 0
                case 1:     lng = l1;   break;  // use break instead of continue to reach scope end for test coverage
                case 2:     lng = l2;   break;
                case 3:     lng = l3;   break;
                case 4:     lng = l4;   break;
                case 5:     lng = l5;   break;
                case 6:     lng = l6;   break;
                case 7:     lng = l7;   break;
                default:    return false;
            }
        }
    }
}
