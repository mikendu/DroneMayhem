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

        UpdateView();

    }

    public void SetTime(double time)
    {
        //transform.position = KeyframeUtil.GetPosition(PositionKeyframes, time, transform.position);
        //LightColor = KeyframeUtil.GetColor(ColorKeyframes, time, LightColor);
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
        transform.position = KeyframeUtil.GetPosition(PositionKeyframes, currentTime, transform.position);
        LightColor = KeyframeUtil.GetColor(ColorKeyframes, currentTime, LightColor);
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
