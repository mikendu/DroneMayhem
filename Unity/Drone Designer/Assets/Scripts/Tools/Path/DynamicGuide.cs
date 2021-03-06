using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEditor.Timeline;


[ExecuteInEditMode]
[RequireComponent(typeof(GuideShape))]
public class DynamicGuide : Guide
{
    public List<GuideKeyframe> Keyframes { get; private set; } = new List<GuideKeyframe>();
    public float Time { get; private set; } = 0.0f;

    private TrackAsset track;

    private void Update()
    {
        UpdateProperties();
        UpdateView();
    }

    public void UpdateProperties()
    {
        this.Time = (float)TimelineUtilities.Director.time;
        this.Keyframes = Track.GetMarkers().Where(item => item is GuideKeyframe).Select(item => item as GuideKeyframe).ToList();
        this.Keyframes.Sort(KeyframeUtil.KeyframeComparator);
    }

    public void UpdateView()
    {
        KeyframeUtil.InterpolationSet<GuideKeyframe> interpolationData = KeyframeUtil.Interpolate(Keyframes, Time, true);
        if (interpolationData != null)
        {
            GuideKeyframe firstKeyframe = interpolationData.first;
            GuideKeyframe nextKeyframe = interpolationData.second;
            float value = interpolationData.value;


            transform.position = Vector3.Lerp(firstKeyframe.Position, nextKeyframe.Position, value);
            transform.rotation = Quaternion.Slerp(firstKeyframe.Rotation, nextKeyframe.Rotation, value);
            transform.localScale = Vector3.Lerp(firstKeyframe.Scale, nextKeyframe.Scale, value);
        }
    }


    public TrackAsset Track
    {
        get
        {
            if (track == null)
            {
                var allTracks = TimelineUtilities.Timeline.GetOutputTracks();
                foreach (TrackAsset track in allTracks)
                {
                    DynamicGuide guide = TimelineUtilities.Director.GetGenericBinding(track) as DynamicGuide;
                    if (guide != null && guide.gameObject == this.gameObject)
                    {
                        this.track = track;
                        break;
                    }
                }
            }

            return track;
        }
    }
}


[CustomEditor(typeof(DynamicGuide))]
public class DynamicGuideEditor: GuideEditor
{

    public override void OnInspectorGUI()
    {
        DrawGuideSelector();
    }
}