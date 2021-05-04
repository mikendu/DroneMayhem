using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Timeline;
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
    protected TrackAsset track;

    public List<ColorKeyframe> ColorKeyframes { get; private set; } = new List<ColorKeyframe>();
    public List<Waypoint> Waypoints { get; private set; } = new List<Waypoint>();
    public float Time { get; private set; } = 0.0f;

    public bool TrackLocked { get; set; } = false;

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
        CrazyflieTrack crazyflieTrack = this.track as CrazyflieTrack;
        if (crazyflieTrack != null && !TrackLocked)
        {
            crazyflieTrack.ResetReferences();
            TimelineUtilities.Timeline.DeleteTrack(crazyflieTrack);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }
    }

    public void ResetReferences()
    {
        this.track = null;
    }

    public void Initialize(CrazyflieTrack track)
    {
        this.track = track;
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

        this.ColorKeyframes.Sort(KeyframeUtil.KeyframeComparator);

        this.Waypoints = Track.GetMarkers().Where(item => item is Waypoint).Select(item => item as Waypoint).ToList();
        this.Waypoints.Sort(KeyframeUtil.KeyframeComparator);
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

    public void ApplyGlobalTransform()
    {
        List<Waypoint> waypoints = this.Waypoints;
        if (waypoints == null)
            return;

        Undo.RecordObjects(waypoints.ToArray(), "Apply Global Transform");
        foreach (Waypoint waypoint in waypoints)
        {
            Vector3 newPosition = GlobalTransform.Transfomed(waypoint.Position);
            Vector3 newTangent = GlobalTransform.Transfomed(waypoint.WorldTangent);
            waypoint.SetPosition(newPosition);
            waypoint.SetTangent(newTangent);
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

        bool endpoint = (waypoints.Count == 0) || (time > waypoints[waypoints.Count - 1].time);
        Undo.RecordObjects(new Object[] { keyframe }, "Change Waypoints");
        keyframe.Position = position;

        if (endpoint)
        {
            keyframe.JointType = JointType.Linear;
        }

        UpdateView();
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
        UpdateView();
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
        if (waypoints.Count == 0)
            return;

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
