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
    public const float ERROR_THRESHOLD = 0.15f;
    public const float SAMPLE_INTERVAL = 0.1f;

    public List<GuideKeyframe> Keyframes { get; private set; } = new List<GuideKeyframe>();
    public float Time { get; private set; } = 0.0f;

    private TrackAsset track;

    private void Update()
    {
        UpdateProperties();
        UpdateView();
    }

    public void ResetReferences()
    {
        this.track = null;
    }

    public void Initialize(GuideTrack track)
    {
        this.track = track;
        track.Initialize(this);
    }

    public void UpdateProperties()
    {
        this.Time = (float)TimelineUtilities.Director.time;
        this.Keyframes = Track.GetMarkers().Where(item => item is GuideKeyframe).Select(item => item as GuideKeyframe).ToList();
        this.Keyframes.Sort(KeyframeUtil.KeyframeComparator);
    }

    public void SetPose(Transform pose, float time, JointType type = JointType.Continuous)
    {
        SetPose(pose.position, pose.rotation, pose.lossyScale, time, type);
    }

    public void SetPosition(Vector3 position, float time, JointType type = JointType.Continuous)
    {
        SetPose(position, transform.rotation, transform.lossyScale, time, type);
    }

    public void SetRotation(Quaternion rotation, float time, JointType type = JointType.Continuous)
    {
        SetPose(transform.position, rotation, transform.lossyScale, time, type);
    }

    public void SetScale(Vector3 scale, float time, JointType type = JointType.Continuous)
    {
        SetPose(transform.position, transform.rotation, scale, time, type);
    }


    public void SetPose(Vector3 position, Quaternion rotation, Vector3 scale, float time, JointType type = JointType.Continuous)
    {
        List<GuideKeyframe> waypoints = this.Keyframes;
        if (waypoints == null)
            return;

        GuideKeyframe keyframe = GetKeyframe(waypoints, time);
        if (keyframe == null)
        {
            keyframe = Track.CreateMarker<GuideKeyframe>(time);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
            UpdateProperties();
        }

        bool endpoint = (waypoints.Count == 0) || (time > waypoints[waypoints.Count - 1].time);
        keyframe.JointType = type;
        keyframe.Position = position;
        keyframe.Rotation = rotation;
        keyframe.Scale = scale;

        if (endpoint)
            keyframe.JointType = JointType.Stop;

        UpdateView();
        Undo.RecordObjects(new UnityEngine.Object[] { keyframe }, "Change Guide Keyframe");
    }

    public void UpdateView()
    {
        UpdateTransform(Time);
    }

    public void AddWaypoint()
    {
        SetPose(transform.position, transform.rotation, transform.localScale, Time);
    }

    public void UpdateTransform(float time)
    {
        KeyframeUtil.InterpolationSet<GuideKeyframe> interpolationData = KeyframeUtil.Interpolate(Keyframes, time, true);
        if (interpolationData != null)
        {
            GuideKeyframe firstKeyframe = interpolationData.first;
            GuideKeyframe nextKeyframe = interpolationData.second;

            float linearValue = interpolationData.value;
            float smoothedValue = Mathf.SmoothStep(0, 1, linearValue);

            float startValue = (firstKeyframe.JointType == JointType.Stop) ? smoothedValue : linearValue;
            float endValue = (nextKeyframe.JointType == JointType.Stop) ? smoothedValue : linearValue;
            float value = Mathf.Lerp(startValue, endValue, linearValue);

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

    private GuideKeyframe GetKeyframe(IEnumerable<GuideKeyframe> keyframes, float time)
    {
        foreach (GuideKeyframe keyframe in keyframes)
        {
            if (Mathf.Approximately((float)keyframe.time, time))
                return keyframe;
        }
        return null;
    }

    private void OnDestroy()
    {
        GuideTrack guideTrack = this.track as GuideTrack;
        if (guideTrack != null)
        {
            guideTrack.ResetReferences();
            TimelineUtilities.Timeline.DeleteTrack(guideTrack);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }
    }
    public void RemoveKeyframe(GuideKeyframe keyframe)
    {
        Track.DeleteMarker(keyframe);
        UpdateView();
        UpdateProperties();
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    public void DrawShapeGUI()
    {
        GuideShape shape = GetComponent<GuideShape>();
        shape.DrawSceneGUI();
    }

    public override bool Dynamic => true;

    public void Apply()
    {
        UpdateProperties();
        GuideShape guideShape = GetComponent<GuideShape>();
        List<Crazyflie> drones = guideShape.AttachmentPoints.Select(p => p.Drone).Where(drone => drone != null).ToList();

        int n = Keyframes.Count;
        if (n <= 1)
            return;
        int sampleCount = Mathf.RoundToInt(1.0f / SAMPLE_INTERVAL);

        Dictionary<Crazyflie, List<Vector3>> points = new Dictionary<Crazyflie, List<Vector3>>();
        Dictionary<Crazyflie, List<CubicBezier>> curves = new Dictionary<Crazyflie, List<CubicBezier>>();


        for (int i = 0; i < n - 1; i++)
        {
            GuideKeyframe first = Keyframes[i];
            GuideKeyframe second = Keyframes[i + 1];

            float startTime = (float)first.time;
            float endTime = (float)second.time;

            float timeDiff = (endTime - startTime);
            float timeInterval = SAMPLE_INTERVAL * timeDiff;

            foreach (Crazyflie drone in drones)
                points[drone] = new List<Vector3>();

            for (int j = 0; j <= sampleCount; j++)
            {
                float t = startTime + (j * timeInterval);
                UpdateTransform(t);
                foreach (Crazyflie drone in drones)
                {
                    float attachmentTime = drone.AttachmentPoint.AttachmentTime;
                    if (attachmentTime == -1 || t >= attachmentTime)
                        points[drone].Add(drone.AttachmentPoint.Position);
                }
            }

            foreach (Crazyflie drone in drones)
            {
                //allPoints[drone].Add(points[drone]);
                if (points[drone].Count < 3)
                    continue;

                if (!curves.ContainsKey(drone))
                    curves[drone] = new List<CubicBezier>();

                List<CubicBezier> paths = BezierFitter.Fit(points[drone], ERROR_THRESHOLD, startTime, endTime);
                curves[drone].AddRange(paths);
            }

        }

        foreach (Crazyflie drone in drones)
        {
            List<CubicBezier> paths = curves[drone];

            float start = paths[0].startTime;
            float end = paths[paths.Count - 1].endTime;
            drone.ClearWaypoints(start, end);

            foreach (CubicBezier path in paths)
            {
                Vector3 tangent = path.anchor2 - path.control2;
                drone.AddWaypoint(path.anchor1, path.control1, path.startTime);
                drone.AddWaypoint(path.anchor2, path.anchor2 + tangent, path.endTime);
            }


            //CubicBezier lastCurve = paths[paths.Count - 1];
            //Vector3 tangentVector = lastCurve.anchor2 - lastCurve.control2;
            //drone.AddWaypoint(lastCurve.anchor2, lastCurve.anchor2 + tangentVector, lastCurve.endTime);

            drone.UpdateProperties();
            drone.UpdateView();
            drone.Release();
        }
    }

    public void OnDrawGizmos()
    {
        /*
        if (allPoints != null)
        {
            foreach (var kvPair in allPoints)
            {
                foreach (var pointsList in kvPair.Value)
                {
                    foreach (Vector3 point in pointsList)
                        Gizmos.DrawSphere(point, 0.01f);
                }
            }
        }*/
    }


}
