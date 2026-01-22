// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

#if NETCOREAPP3_0_OR_GREATER
    using System.Runtime.Intrinsics;
#endif

// ReSharper disable ConvertToAutoProperty
namespace Friflo.Engine.ECS.Utils;

/// <summary>
/// Support a bit set currently limited to 256 bits.<br/>
/// If need an additional Vector256Long[] could be added be added for arbitrary length.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public partial struct BitSet
{
#if NETCOREAPP3_0_OR_GREATER
    [FieldOffset(00)] internal  Vector256<long> value;      // 32
#endif
    [FieldOffset(00)] internal  long            l0;         // (8)
    [FieldOffset(08)] internal  long            l1;         // (8)
    [FieldOffset(16)] internal  long            l2;         // (8)
    [FieldOffset(24)] internal  long            l3;         // (8)

    [FieldOffset(32)] internal  long            l4;         // (8)
    [FieldOffset(40)] internal  long            l5;         // (8)
    [FieldOffset(48)] internal  long            l6;         // (8)
    [FieldOffset(56)] internal  long            l7;         // (8)
    
    // Could extend with Vector256Long[] if 256 components are not enough
    // private readonly  Vector256Long[]   values;

    public readonly          BitSetEnumerator    GetEnumerator() => new BitSetEnumerator(this);
    public readonly override string              ToString()      => AppendString(new StringBuilder()).ToString();
    
    public BitSet(int[] indices) {
        foreach (var index in indices) {
            SetBit(index);
        }
    }
    
    // hash distribution is probably not good. But executes fast. Leave it for now.
    public readonly int HashCode()
    {
        var l = l0 ^ l1  ^ l2 ^ l3  ^ l4 ^ l5 ^ l6 ^ l7;
        return unchecked((int)l) ^ (int)(l >> 32);
    }
        
    public void SetBit(int index)
    {
        switch (index) {
            case < 64:      l0 |= 1L <<  index;          return;
            case < 128:     l1 |= 1L << (index - 64);    return;
            case < 192:     l2 |= 1L << (index - 128);   return;
            /* default:        l3 |= 1L << (index - 192);   return; */
            case < 256:     l3 |= 1L << (index - 192);   return;
            case < 320:     l4 |= 1L << (index - 256);   return;
            case < 384:     l5 |= 1L << (index - 320);   return;
            case < 448:     l6 |= 1L << (index - 384);   return;
            default:        l7 |= 1L << (index - 448);   return;
        }
    }
    
    public void ClearBit(int index)
    {
        switch (index) {
            case < 64:      l0 &= ~(1L <<  index);          return;
            case < 128:     l1 &= ~(1L << (index - 64));    return;
            case < 192:     l2 &= ~(1L << (index - 128));   return;
            //default:        l3 &= ~(1L << (index - 192));   return;
            case < 256:     l3 &= ~(1L << (index - 192));   return;
            case < 320:     l4 &= ~(1L << (index - 256));   return;
            case < 384:     l5 &= ~(1L << (index - 320));   return;
            case < 448:     l6 &= ~(1L << (index - 384));   return;
            default:        l7 &= ~(1L << (index - 448));   return;
        }
    }
    
    public readonly bool Has(int index)
    {
        switch (index) {
            case < 64:      return (l0 & (1L <<  index))        != 0;
            case < 128:     return (l1 & (1L << (index -  64))) != 0;
            case < 192:     return (l2 & (1L << (index - 128))) != 0;
            //default:        return (l3 & (1L << (index - 192))) != 0;
            case < 256:     return (l3 & (1L << (index - 192))) != 0;
            case < 320:     return (l4 & (1L << (index - 256))) != 0;
            case < 384:     return (l5 & (1L << (index - 320))) != 0;
            case < 448:     return (l6 & (1L << (index - 384))) != 0;
            default:        return (l7 & (1L << (index - 448))) != 0;
        }
    }
    
    private readonly StringBuilder AppendString(StringBuilder sb) {
        if (l7 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16} {l3:x16} {l4:x16} {l5:x16} {l6:x16} {l7:x16}");
        } else if (l6 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16} {l3:x16} {l4:x16} {l5:x16} {l6:x16}");
        } else if (l5 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16} {l3:x16} {l4:x16} {l5:x16}");
        } else if (l4 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16} {l3:x16} {l4:x16}");
        } else if (l3 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16} {l3:x16}");
        } else if (l2 != 0) {
            sb.Append($"{l0:x16} {l1:x16} {l2:x16}");
        } else if (l1 != 0) {
            sb.Append($"{l0:x16} {l1:x16}");
        } else {
            sb.Append($"{l0:x16}");
        }
        return sb;
    }
}
