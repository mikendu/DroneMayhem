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

    public List<ColorKeyframe> ColorKeyframes { get; private set; } = new List<ColorKeyframe>();
    public List<Waypoint> Waypoints { get; private set; } = new List<Waypoint>();
    public float Time { get; private set; } = 0.0f;

    protected Color previousColor;

    void Start()
    {
        
    }



    void Update()
    {
        TimelineUtilities.Initialize();

        if (properties == null)
            properties = new MaterialPropertyBlock();

        if (m_light == null)
            m_light = GetComponentInChildren<Light>();

        if (m_renderer == null)
            m_renderer = GetComponent<MeshRenderer>();

        m_light.color = LightColor;
        properties.SetColor("_EmissionColor", 4 * LightColor);
        m_renderer.SetPropertyBlock(properties);

        UpdateProperties();
        UpdateView();
    }

    private void OnDestroy()
    {
        if (Track != null)
            GameObject.DestroyImmediate(Track, true);
    }

    public void UpdateProperties()
    {
        this.Time = (float)TimelineUtilities.Director.time;

        this.ColorKeyframes = Track.GetMarkers()
            .Where(item => item is ColorKeyframe)
            .Select((item, index) =>
            {
                ColorKeyframe keyframe = item as ColorKeyframe;
                keyframe.MarkerIndex = index;
                return keyframe;
            }).ToList();

        this.ColorKeyframes.Sort((x, y) => x.time.CompareTo(y.time));

        this.Waypoints = Track.GetMarkers().Where(item => item is Waypoint).Select(item => item as Waypoint).ToList();
        this.Waypoints.Sort((x, y) => x.time.CompareTo(y.time));
    }

    public void UpdateView()
    {
        previousColor = LightColor;
        transform.position = KeyframeUtil.GetPosition(Waypoints, Time, transform.position);
        LightColor = KeyframeUtil.GetColor(ColorKeyframes, Time, LightColor);
    }


    private void OnValidate()
    {
        if (this.track != null && LightColor != previousColor)
        {
            SetColorKeyframe(LightColor, this.Time);
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
                    Crazyflie drone = TimelineUtilities.Director.GetGenericBinding(track) as Crazyflie;
                    if (drone != null && drone.gameObject == this.gameObject)
                    {
                        this.track = track;
                        break;
                    }
                }
            }

            return track;
        }
    }


    public void SetWaypoint(Vector3 position, float time)
    {
        List<Waypoint> waypoints = this.Waypoints;
        if (waypoints == null)
            return;

        Waypoint keyframe = GetKeyframe(waypoints, time) as Waypoint;
        if (keyframe == null)
        {
            Vector3 tangent = 0.125f *  KeyframeUtil.GetTangent(waypoints, time, true, true);
            keyframe = Track.CreateMarker<Waypoint>(time);
            keyframe.JointType = JointType.Continuous;
            keyframe.Tangent = tangent;

            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
            UpdateProperties();
            EnforceWaypointConstraints();
        }

        bool pastEnd = (time > waypoints[waypoints.Count - 1].time);
        Undo.RecordObjects(new Object[] { keyframe }, "Change Waypoints");
        keyframe.Position = position;

        if (pastEnd)
        {
            keyframe.JointType = JointType.Linear;
        }

        UpdateView();
        //EnforceWaypointConstraints();
        //TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    public void SetColorKeyframe(Color lightColor, float time)
    {
        List<ColorKeyframe> keyframes = this.ColorKeyframes;
        if (keyframes == null)
            return;

        ColorKeyframe keyframe = GetKeyframe(keyframes, time) as ColorKeyframe;
        if (keyframe == null)
        {
            keyframe = Track.CreateMarker<ColorKeyframe>(time);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
            UpdateProperties();
        }

        keyframe.LightColor = lightColor;
        //UpdateView();
        //TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    public void RemoveWaypoint(Waypoint waypoint)
    {
        Track.DeleteMarker(waypoint);
        UpdateView();
        UpdateProperties();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        EnforceWaypointConstraints();
    }

    public void RemoveColorKeyframe(ColorKeyframe keyframe)
    {
        Track.DeleteMarker(keyframe);
        UpdateView();
        UpdateProperties();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    public void EnforceWaypointConstraints()
    {
        List<Waypoint> waypoints = this.Waypoints;
        Waypoint firstWaypoint = waypoints[0];
        Waypoint lastWaypoint = waypoints[waypoints.Count - 1];
        Undo.RecordObjects(new Object[] { firstWaypoint, lastWaypoint }, "Change Waypoints");

        firstWaypoint.JointType = JointType.Linear;
        lastWaypoint.JointType = JointType.Linear;
    }

    private T GetKeyframe<T>(float time) where T : DroneKeyframe
    {
        if (typeof(T) == typeof(ColorKeyframe))
            return GetKeyframe(ColorKeyframes, time) as T;

        if (typeof(T) == typeof(Waypoint))
            return GetKeyframe(Waypoints, time) as T;

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

    public bool IsEndpoint(Waypoint waypoint)
    {
        List<Waypoint> waypoints = this.Waypoints;
        Waypoint firstWaypoint = waypoints[0];
        Waypoint lastWaypoint = waypoints[waypoints.Count - 1];
        return (waypoint == firstWaypoint || waypoint == lastWaypoint);
    }
}
