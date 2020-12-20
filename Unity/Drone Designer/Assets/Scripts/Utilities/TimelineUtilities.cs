using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;
using System.Reflection;

[InitializeOnLoad]
public class TimelineUtilities : MonoBehaviour
{

    private static PlayableDirector director;
    private static TimelineAsset timeline;
    private static System.Type TimelineWindowType;
    private static bool initialized = false;
    private static GameObject DroneTemplate;

    static TimelineUtilities()
    {
        director = FindObjectOfType<PlayableDirector>();
        timeline = director?.playableAsset as TimelineAsset;
        TimelineWindowType = FindWindowType();
        initialized = false;

        DroneTemplate = Resources.Load<GameObject>("Prefabs/crazyflie");
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








    [MenuItem("Drone Tools/New Sequence", false, 0)]
    static void CreateSequence()
    {
        
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









    [MenuItem("Drone Tools/Insert Drone %#d", false, 14)]
    static void CreateDrone()
    {
        if (DroneTemplate == null)
            DroneTemplate = Resources.Load<GameObject>("Prefabs/crazyflie");

        int droneCount = FindObjectsOfType<Crazyflie>().Length;
        GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(DroneTemplate);
        drone.name = $"Drone {droneCount}";
        drone.transform.position = new Vector3(0, 0.5f, 0);
        drone.transform.SetAsLastSibling();

        CrazyflieTrack track = Timeline.CreateTrack<CrazyflieTrack>();
        Crazyflie crazyflie = drone.GetComponent<Crazyflie>();
        crazyflie?.Initialize(track);
        crazyflie?.SetColorKeyframe(Color.black, 0.0f);
        crazyflie?.SetWaypoint(drone.transform.position, 0.0f);
        Director.SetGenericBinding(track, crazyflie);

        AssetDatabase.Refresh();
        UnityEditor.Timeline.TimelineEditor.Refresh(UnityEditor.Timeline.RefreshReason.ContentsAddedOrRemoved);

        Undo.RegisterCreatedObjectUndo(drone, "Create Drone");
        Undo.RegisterCreatedObjectUndo(track, "Create Drone");
        Selection.activeObject = drone;
    }


    [MenuItem("Drone Tools/Show Timeline %t", false, 15)]
    static void ShowTimeline()
    {
        if (TimelineWindowType == null)
        {
            return;
        }

        if (Timeline == null)
        {
            EditorUtility.DisplayDialog("Error", "No timeline found in scene!", "OK");
            return;
        }

        EditorWindow timelineWindow = EditorWindow.GetWindow(TimelineWindowType);
        MethodInfo setTimelineMethod = TimelineWindowType.GetMethod("SetCurrentTimeline", new System.Type[] { typeof(PlayableDirector), typeof(TimelineClip) });
        setTimelineMethod.Invoke(timelineWindow, new object[] { Director, null });
        TimelineWindowType.InvokeMember(
            "locked", 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, 
            System.Type.DefaultBinder, 
            timelineWindow, 
            new object[] { true }
        );
    }






    // -- REFLECTION -- //

    private static System.Type FindWindowType()
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

        return desiredWindowType;
    }

    public static void Initialize()
    {
        if (initialized)
            return;

        PropertyInfo instanceProperty = TimelineWindowType.GetProperty("instance");
        PropertyInfo stateProperty = TimelineWindowType.GetProperty("state");
        object windowInstance = instanceProperty.GetValue(null);
        object windowState = stateProperty.GetValue(windowInstance);
        object masterSequence = windowState.GetType().GetProperty("masterSequence").GetValue(windowState);
        double? time = masterSequence.GetType().GetProperty("time").GetValue(masterSequence) as System.Nullable<double>;

        if (Director != null && time != null)
            Director.time = time.Value;

        initialized = true;
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
