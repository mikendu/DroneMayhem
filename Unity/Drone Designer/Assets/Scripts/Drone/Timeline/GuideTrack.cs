using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;

[TrackColor(13 / 255f, 223 / 255f, 255 / 255f)]
[TrackBindingType(typeof(DynamicGuide))]
public class GuideTrack : TrackAsset
{
    private DynamicGuide guide;
    public DynamicGuide Guide
    {
        get
        {
            if (guide == null)
                guide = TimelineUtilities.Director.GetGenericBinding(this) as DynamicGuide;

            return guide;
        }
    }

    public void Initialize(DynamicGuide guide)
    {
        this.guide = guide;
    }

    private void OnDestroy()
    {
        foreach (IMarker marker in this.GetMarkers())
            this.DeleteMarker(marker);

        if (guide?.gameObject != null)
        {
            guide.ResetReferences();
            Undo.DestroyObjectImmediate(guide.gameObject);
        }
    }

    public void ResetReferences()
    {
        guide = null;
    }
}