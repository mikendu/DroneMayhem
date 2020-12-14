using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Linq;

[SelectionBase]
[ExecuteInEditMode]
public class Crazyflie : MonoBehaviour
{

    [ColorUsage(false, false)]
    public Color LightColor;

    protected Light m_light;
    protected MeshRenderer m_renderer;
    protected MaterialPropertyBlock properties;

    protected PlayableDirector director;
    protected TimelineAsset timeline;
    protected TrackAsset track;
    protected bool needsSelect = false;

    void Start()
    {
        
    }



    void LateUpdate()
    {
        if (this.needsSelect)
        {
            Selection.activeTransform = this.transform;
            needsSelect = false;
        }

        if (properties == null)
            properties = new MaterialPropertyBlock();

        if (m_light == null)
            m_light = GetComponentInChildren<Light>();

        if (m_renderer == null)
            m_renderer = GetComponent<MeshRenderer>();

        m_light.color = LightColor;
        properties.SetColor("_EmissionColor", 4 * LightColor);
        m_renderer.SetPropertyBlock(properties);

        UpdateView();
    }


    public void Select()
    {
        this.needsSelect = true;
    }

    public void UpdateView()
    {
        double currentTime = TimelineUtilities.Director.time;
        transform.position = KeyframeUtil.GetPosition(PositionKeyframes, currentTime, transform.position);
        LightColor = KeyframeUtil.GetColor(ColorKeyframes, currentTime, LightColor);
    }


    private void OnValidate()
    {
        SetColorKeyframe();
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
                    var drone = TimelineUtilities.Director.GetGenericBinding(track);
                    if (drone != null && drone == this)
                    {
                        this.track = track;
                        break;
                    }
                }
            }
            return track;
        }
    }


    public List<PositionKeyframe> PositionKeyframes
    {
        get
        {
            if (Track == null)
                return null;

            List<PositionKeyframe> result = Track.GetMarkers().Where(item => item is PositionKeyframe).Select(item => item as PositionKeyframe).ToList();
            result.Sort((x, y) => x.time.CompareTo(y.time));
            return result;
        }
    }

    public List<ColorKeyframe> ColorKeyframes
    {
        get
        {
            if (Track == null)
                return null;

            List<ColorKeyframe> result = Track.GetMarkers().Where(item => item is ColorKeyframe).Select(item => item as ColorKeyframe).ToList();
            result.Sort((x, y) => x.time.CompareTo(y.time));
            return result;
        }
    }

    public DroneKeyframe SetWaypoint()
    {
        float time = (float)TimelineUtilities.Director?.time;
        List<PositionKeyframe> keyframes = PositionKeyframes;
        if (keyframes == null)
            return null;

        PositionKeyframe keyframe = GetKeyframe(keyframes, time) as PositionKeyframe;
        if (keyframe == null)
        {
            keyframe = Track.CreateMarker<PositionKeyframe>(time);
            keyframe.JointType = JointType.Linear;
        }

        float offset = (time > keyframes[keyframes.Count - 1].time) ? 0.1f : 0.0f;
        keyframe.Position = transform.position + new Vector3(0, offset, 0);
        UpdateView();
        EnforceWaypointConstraints();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        return keyframe;
    }

    public DroneKeyframe SetColorKeyframe()
    {
        float time = (float)TimelineUtilities.Director?.time;
        List<ColorKeyframe> keyframes = ColorKeyframes;
        if (keyframes == null || time == 0.0f)
            return null;

        ColorKeyframe keyframe = GetKeyframe(keyframes, time) as ColorKeyframe;
        if (keyframe == null)
            keyframe = Track.CreateMarker<ColorKeyframe>(time);

        keyframe.LightColor = LightColor;
        UpdateView();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        return keyframe;
    }

    public void RemoveWaypoint(PositionKeyframe waypoint)
    {
        Track.DeleteMarker(waypoint);
        UpdateView();
        EnforceWaypointConstraints();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    public void RemoveColorKeyframe(ColorKeyframe keyframe)
    {
        Track.DeleteMarker(keyframe);
        UpdateView();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    public void EnforceWaypointConstraints()
    {
        List<PositionKeyframe> waypoints = this.PositionKeyframes;
        waypoints[0].JointType = JointType.Linear;
        waypoints[waypoints.Count - 1].JointType = JointType.Linear;
    }

    private T GetKeyframe<T>(float time) where T : DroneKeyframe
    {
        if (typeof(T) == typeof(ColorKeyframe))
            return GetKeyframe(ColorKeyframes, time) as T;

        if (typeof(T) == typeof(PositionKeyframe))
            return GetKeyframe(PositionKeyframes, time) as T;

        return null;
    }


    private DroneKeyframe GetKeyframe(IEnumerable<DroneKeyframe> keyframes, float time)
    {
        foreach (DroneKeyframe keyframe in keyframes)
        {
            if (Mathf.Approximately((float)keyframe.time, time))
                return keyframe;
        }
        return null;
    }
}
