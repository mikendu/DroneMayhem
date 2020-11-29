using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Linq;

[SelectionBase]
[ExecuteInEditMode]
public class Crazyflie : MonoBehaviour, ITimeControl
{
    [ColorUsage(false, false)]
    public Color LightColor;

    protected Light m_light;
    protected MeshRenderer m_renderer;
    protected MaterialPropertyBlock properties;

    protected PlayableDirector director;
    protected TimelineAsset timeline;
    protected TrackAsset track;


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (properties == null)
            properties = new MaterialPropertyBlock();

        if (m_light == null)
            m_light = GetComponentInChildren<Light>();

        if (m_renderer == null)
            m_renderer = GetComponent<MeshRenderer>();

        m_light.color = LightColor;
        properties.SetColor("_EmissionColor", 4 * LightColor);
        m_renderer.SetPropertyBlock(properties);

    }

    public void SetTime(double time)
    {
        transform.position = GetPosition(time);
        LightColor = GetColor(time);
    }

    public void OnControlTimeStart()
    {
        
    }

    public void OnControlTimeStop()
    {
        
    }


    public void UpdateView()
    {
        double currentTime = Director.time;
        transform.position = GetPosition(currentTime);
        LightColor = GetColor(currentTime);
    }

    private Color GetColor(double time)
    {
        List<ColorKeyframe> keyframes = ColorKeyframes;
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            ColorKeyframe keyframe = keyframes[i];
            ColorKeyframe nextKeyframe = keyframes[i + 1];

            if (time >= keyframe.time && time < nextKeyframe.time)
            {
                double duration = (nextKeyframe.time - keyframe.time);
                double interpolationValue = (time - keyframe.time) / duration;
                return Color.Lerp(keyframe.LightColor, nextKeyframe.LightColor, Mathf.Clamp01((float)interpolationValue));
            }
        }

        if (keyframes.Count > 0)
        {
            if (time < keyframes[0].time)
                return keyframes[0].LightColor;
            else
                return keyframes[keyframes.Count - 1].LightColor;
        }

        return LightColor;
    }

    private Vector3 GetPosition(double time)
    {
        List<PositionKeyframe> keyframes = PositionKeyframes;
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            PositionKeyframe keyframe = keyframes[i];
            PositionKeyframe nextKeyframe = keyframes[i + 1];

            if (time >= keyframe.time && time < nextKeyframe.time)
            {
                double duration = (nextKeyframe.time - keyframe.time);
                double interpolationValue = (time - keyframe.time) / duration;
                return EvaluateBezier(keyframe, nextKeyframe, Mathf.Clamp01((float)interpolationValue));
            }
        }
        
        if (keyframes.Count > 0)
        {
            if (time < keyframes[0].time)
                return keyframes[0].Position;
            else
                return keyframes[keyframes.Count - 1].Position;
        }

        return transform.position;
    }

    
    private Vector3 EvaluateBezier(PositionKeyframe currentKeyframe, PositionKeyframe nextKeyframe, float interpolation)
    {
        bool linearStart = (currentKeyframe.JointType == JointType.Linear);
        bool linearEnd = (nextKeyframe.JointType == JointType.Linear);

        Vector3 startPos = currentKeyframe.Position;
        Vector3 endPos = nextKeyframe.Position;
        Vector3 startTangent = linearStart ? startPos : startPos + currentKeyframe.StartTangent;
        Vector3 endTangent = linearEnd ? endPos : endPos + currentKeyframe.EndTangent;

        Vector3 quadOne = EvaluateQuadratic(startPos, startTangent, endTangent, interpolation);
        Vector3 quadTwo = EvaluateQuadratic(startTangent, endTangent, endPos, interpolation);
        return Vector3.Lerp(quadOne, quadTwo, interpolation);
    }

    private Vector3 EvaluateQuadratic(Vector3 q0, Vector3 q1, Vector3 q2, float segmentTime)
    {
        Vector3 lineOne = Vector3.Lerp(q0, q1, segmentTime);
        Vector3 lineTwo = Vector3.Lerp(q1, q2, segmentTime);
        return Vector3.Lerp(lineOne, lineTwo, segmentTime);
    }

    private void OnValidate()
    {
        UpdateView();
    }

    public PlayableDirector Director
    {
        get
        {
            if (director == null)
                director = FindObjectOfType<PlayableDirector>();

            return director;
        }
    }

    public TimelineAsset Timeline
    {
        get
        {
            if (timeline == null)
                timeline = Director?.playableAsset as TimelineAsset;

            return timeline;
        }
    }

    public TrackAsset Track
    {
        get
        {
            if (track == null)
            {
                var allTracks = Timeline.GetOutputTracks();
                foreach (TrackAsset track in allTracks)
                {
                    var drone = Director.GetGenericBinding(track);
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
            return Track.GetMarkers().Where(item => item is PositionKeyframe).Select(item => item as PositionKeyframe).ToList();
        }
    }

    public List<ColorKeyframe> ColorKeyframes
    {
        get
        {
            return Track.GetMarkers().Where(item => item is ColorKeyframe).Select(item => item as ColorKeyframe).ToList();
        }
    }
    

}
