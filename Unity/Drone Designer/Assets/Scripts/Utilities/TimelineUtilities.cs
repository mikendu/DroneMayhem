using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor.Timeline;
using System.IO;
using System.Reflection;

[InitializeOnLoad]
public class TimelineUtilities : MonoBehaviour
{

    private static PlayableDirector director;
    private static TimelineAsset timeline;

    static TimelineUtilities()
    {
        director = FindObjectOfType<PlayableDirector>();
        timeline = director?.playableAsset as TimelineAsset;
    }

    public static PlayableDirector Director
    {
        get
        {
            if (director == null)
                director = FindObjectOfType<PlayableDirector>();

            return director;
        }
    }

    public static TimelineAsset Timeline
    {
        get
        {
            if (timeline == null)
                timeline = Director?.playableAsset as TimelineAsset;

            return timeline;
        }
    }




    [MenuItem("Drone Tools/Show Timeline %t", false, 0)]
    static void ShowTimeline()
    {
        //https://answers.unity.com/questions/1237463/how-do-i-get-a-reference-to-the-default-editor-win.html
        var allWindowTypes = GetAllEditorWindowTypes();
        System.Type desiredWindowType = null;
        foreach (System.Type windowType in allWindowTypes)
        {
            if (windowType.FullName.Contains("Timeline"))
            {
                desiredWindowType = windowType;
                break;
            }
        }

        if (desiredWindowType == null)
            return;

        PlayableDirector director = FindObjectOfType<PlayableDirector>();
        TimelineAsset timelineAsset = director?.playableAsset as TimelineAsset;
        if (timelineAsset == null)
        {
            EditorUtility.DisplayDialog("Error", "No timeline found in scene!", "OK");
            return;
        }

        EditorWindow timelineWindow = EditorWindow.GetWindow(desiredWindowType);
        MethodInfo setTimelineMethod = desiredWindowType.GetMethod("SetCurrentTimeline", new System.Type[] { typeof(PlayableDirector), typeof(TimelineClip) });
        setTimelineMethod.Invoke(timelineWindow, new object[] { director, null });
        desiredWindowType.InvokeMember(
            "locked", 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, 
            System.Type.DefaultBinder, 
            timelineWindow, 
            new object[] { true }
        );
    }

    [MenuItem("Drone Tools/Export Sequence %&s", false, 1)]
    public static void SaveSequence()
    {
        PlayableDirector director = FindObjectOfType<PlayableDirector>();
        TimelineAsset timeline = director?.playableAsset as TimelineAsset;
        if (timeline == null)
        {
            EditorUtility.DisplayDialog("Error", "No timeline found in scene!", "OK");
            return;
        }

        string data = TimelineExporter.ExportTimeline(timeline);
        string path = EditorUtility.SaveFilePanel("Save Sequence", "", "New Sequence.json", "json");
        if (!string.IsNullOrEmpty(path))
            File.WriteAllText(path, data);
    }


    [MenuItem("Drone Tools/Insert Drone %#d", false, 15)]
    static void CreateDrone()
    {
        PlayableDirector director = FindObjectOfType<PlayableDirector>();
        TimelineAsset timeline = director?.playableAsset as TimelineAsset;
        if (timeline == null)
        {
            EditorUtility.DisplayDialog("Error", "No timeline found in scene!", "OK");
            return;
        }

        Debug.Log("Num Tracks: " + timeline.outputTrackCount);
        Debug.Log("Track [0]: " + timeline.GetOutputTrack(0));
        Debug.Log("Track [1]: " + timeline.GetOutputTrack(1));
        Debug.Log("Track [2]: " + timeline.GetOutputTrack(2));

        TrackAsset track = timeline.GetOutputTrack(2);
        Debug.Log("Markers count: " + track.GetMarkerCount());
        Debug.Log("Track[2].Markers[0]: " + track.GetMarker(0));

        // Get Drone By Track
        var drone = director.GetGenericBinding(track);
        Debug.Log("Result: " + drone);

        // Add Marker
        //track.CreateMarker

        // Edit Marker
        track.GetMarker(0).time = 3.5;

        // Add Track
        //timeline.CreateTrack
    }



    private static System.Type[] GetAllEditorWindowTypes()
    {
        var result = new System.Collections.Generic.List<System.Type>();
        System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
        System.Type editorWindow = typeof(EditorWindow);
        foreach (var A in AS)
        {
            System.Type[] types = A.GetTypes();
            foreach (var T in types)
            {
                if (T.IsSubclassOf(editorWindow))
                    result.Add(T);
            }
        }
        return result.ToArray();
    }

}
