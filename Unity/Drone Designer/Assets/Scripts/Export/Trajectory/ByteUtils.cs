using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ByteUtils
{
    public static void Pack(List<byte> bytes, short data, bool littleEndian = true)
    {
        byte lsb = (byte)(data & 0xFF);
        byte msb = (byte)((data >> 8) & 0xFF);

        if (littleEndian)
        {
            bytes.Add(lsb);
            bytes.Add(msb);
        }
        else
        {
            bytes.Add(msb);
            bytes.Add(lsb);
        }
    }

    public static byte Coalesce(byte a, byte b, byte c, byte d) 
    {
        byte isolator = 0x03;
        byte shiftedA = (byte)((a & isolator) << 6);
        byte shiftedB = (byte)((b & isolator) << 4);
        byte shiftedC = (byte)((c & isolator) << 2);
        byte shiftedD = (byte)(d & isolator);

        return (byte)(shiftedA | shiftedB | shiftedC | shiftedD);
    }
}