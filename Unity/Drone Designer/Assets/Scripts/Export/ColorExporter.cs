using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ColorExporter
{
    public static byte[] Process(List<ColorKeyframe> keyframes)
    {
        if (keyframes.Count == 0)
            return new byte[0];

        List<byte> results = new List<byte>();
        ProcessStart(results, keyframes);

        for(int i = 0; i < keyframes.Count - 1; i++)
            ProcessKeyframe(results, keyframes[i], keyframes[i + 1]);

        // end signal
        results.AddRange(new byte[] { 0, 0, 0, 0 });
        return results.ToArray();
    }

    private static void ProcessStart(List<byte> bytes, List<ColorKeyframe> keyframes)
    {
        if (keyframes.Count == 0)
            return;

        ColorKeyframe firstKeyframe = keyframes[0];
        AddTiming(bytes, 0.01f, firstKeyframe.LightColor);
    }

    private static void ProcessKeyframe(List<byte> bytes,  ColorKeyframe current, ColorKeyframe next)
    {
        float duration = (float)(next.time - current.time);
        AddTiming(bytes, duration, next.LightColor);
    }

    private static void AddTiming(List<byte> bytes, float duration, Color color)
    {
        // Convert duration from seconds to multiples of 1/100th of a second
        // This gets capped at 65,535 (two bytes)
        short multiples = (short)(Mathf.RoundToInt(100 * duration) & 0xFFFF);
        byte timeUpper = (byte)(multiples >> 8);
        byte timeLower = (byte)(multiples & 0xFF);

        byte r = ToByte(color.r);
        byte g = ToByte(color.g);
        byte b = ToByte(color.b);

        /* In order to fit all the LEDs in one radio packet RGB565 is used
         * to compress the colors. The calculations below converts 3 bytes
         * RGB into 2 bytes RGB565. Then shifts the value of each color to
         * LSB, applies the intensity and shifts them back for correct
         * alignment on 2 bytes. 
         */
        short led = GetLedBytes(r, g, b);
        byte ledUpper = (byte)(led >> 8);
        byte ledLower = (byte)(led & 0xFF);
        bytes.AddRange(new byte[] { timeUpper, timeLower, ledUpper, ledLower });
    }

    private static short GetLedBytes(byte r, byte g, byte b)
    {
        uint r5 = (((((uint)r) * 249) + 1014) >> 11);
        uint g6 = (((((uint)g) * 253) + 505) >> 10);
        uint b5 = (((((uint)b) * 249) + 1014) >> 11);

        uint combined = ((r5 << 11) | (g6 << 5) | (b5 << 0));
        return (short)(combined & 0xFFFF);
    }

    private static byte ToByte(float colorValue)
    {
        return (byte)(Mathf.RoundToInt(255 * colorValue) & 0xFF);
    }
}