using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class GradientTexture
{
    public static Texture2D Generate(Gradient gradient, int sampleCount = 16, bool invert = false)
    {
        Texture2D texture = new Texture2D(sampleCount, 1, TextureFormat.ARGB32, false);
        UpdateTexture(gradient, ref texture, sampleCount, invert);
        return texture;
    }

    public static void UpdateTexture(Gradient gradient, ref Texture2D texture, int sampleCount = 16, bool invert = false)
    {
        if (texture == null || texture.width != sampleCount)
            texture = new Texture2D(sampleCount, 1, TextureFormat.ARGB32, false);

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        sampleCount = Math.Max(sampleCount, 2);
        float interval = 1.0f / (sampleCount - 1);

        for (int i = 0; i < sampleCount; i++)
        {
            float index = invert ? (1.0f - (i * interval)) : (i * interval);
            Color color = gradient.Evaluate(index);
            texture.SetPixel(i, 0, color);
        }

        texture.Apply();
    }
}