using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class Morton
{
    public static ulong encode(int x, int y, int z)
    {
        return encode((uint)x, (uint)y, (uint)z);
    }

    public static ulong encode(uint x, uint y, uint z)
    {
        ulong answer = 0;
        answer |= splitBy3(x) | splitBy3(y) << 1 | splitBy3(z) << 2;
        return answer;
    }
    
    private static ulong splitBy3(uint a)
    {
        ulong x = a & 0x1fffff; // we only look at the first 21 bits
        x = (x | x << 32) & 0x1f00000000ffff; // shift left 32 bits, OR with self, and 00011111000000000000000000000000000000001111111111111111
        x = (x | x << 16) & 0x1f0000ff0000ff; // shift left 32 bits, OR with self, and 00011111000000000000000011111111000000000000000011111111
        x = (x | x << 8) & 0x100f00f00f00f00f; // shift left 32 bits, OR with self, and 0001000000001111000000001111000000001111000000001111000000000000
        x = (x | x << 4) & 0x10c30c30c30c30c3; // shift left 32 bits, OR with self, and 0001000011000011000011000011000011000011000011000011000100000000
        x = (x | x << 2) & 0x1249249249249249;
        return x;
    }

    private static uint compact(ulong w)
    {
        w &= 0x1249249249249249;
        w = (w ^ (w >> 2)) & 0x30c30c30c30c30c3;
        w = (w ^ (w >> 4)) & 0xf00f00f00f00f00f;
        w = (w ^ (w >> 8)) & 0x00ff0000ff0000ff;
        w = (w ^ (w >> 16)) & 0x00ff00000000ffff;
        w = (w ^ (w >> 32)) & 0x00000000001fffff;
        return (uint)w;
    }

    public static Vector3Int decode(ulong Z_code)
    {
        uint x = compact(Z_code);
        uint y = compact(Z_code >> 1);
        uint z = compact(Z_code >> 2);
        return new Vector3Int((int)x, (int)y, (int)z);
    }


    public static uint nextPowerOfTwo(uint v)
    {
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
    }
}