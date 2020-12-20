using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class Palette
{
    public static readonly Color SemiTranslucent = new Color(1, 1, 1, 0.775f);
    public static readonly Color Translucent = new Color(1, 1, 1, 0.35f);
    public static readonly Color UltraTranslucent = new Color(1, 1, 1, 0.175f);

    private const float HandleAlpha = 0.45f;
    public static readonly Color TranslucentRed = new Color(1.0f, 0.0f, 0.0f, HandleAlpha);
    public static readonly Color TranslucentGreen = new Color(0.0f, 1.0f, 0.0f, HandleAlpha);
    public static readonly Color TranslucentBlue = new Color(0.325f, 0.65f, 1.0f, HandleAlpha);

    private const float GuiBrightness = 0.25f;
    public static readonly Color Lightener = new Color(0.25f, 0.25f, 0.25f);
    public static readonly Color GuiBackground = new Color(GuiBrightness, GuiBrightness, GuiBrightness, 0.775f);
}