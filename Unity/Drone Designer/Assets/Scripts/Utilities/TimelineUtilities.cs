using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEditor.SceneManagement;
using System.IO;
using System.Reflection;
using System.Linq;

[InitializeOnLoad]
public class TimelineUtilities : MonoBehaviour
{

    private static PlayableDirector director;
    private static TimelineAsset timeline;
    private static System.Type TimelineWindowType;
    private static bool initialized = false;

    private static GameObject DroneTemplate;
    private static GameObject CameraTemplate;
    private static GameObject EnvironmentTemplate;
    private static GameObject TimelineTemplate;

    static TimelineUtilities()
    {
        RefreshReferences();
        TimelineWindowType = FindWindowType();
        initialized = false;

        DroneTemplate = Resources.Load<GameObject>("Prefabs/crazyflie");
        CameraTemplate = Resources.Load<GameObject>("Prefabs/Camera");
        EnvironmentTemplate = Resources.Load<GameObject>("Prefabs/Environment");
        TimelineTemplate = Resources.Load<GameObject>("Prefabs/Timeline");

        EditorSceneManager.sceneClosing -= OnPreClose;
        EditorSceneManager.sceneClosing += OnPreClose;

        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void RefreshReferences()
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

    private static void OnPreClose(Scene scene, bool removing)
    {
        UnlockTimeline();
        Crazyflie[] allDrones = FindObjectsOfType<Crazyflie>();
        foreach (Crazyflie drone in allDrones)
            drone.TrackLocked = true;

    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        RefreshReferences();
        if (mode == OpenSceneMode.Single)
        {
            ShowTimeline();
        }
    }







    [MenuItem("Drone Tools/New Sequence", false, 0)]
    static void CreateSequence()
    {
        // Choose folder for new sequence
        string path = EditorUtility.SaveFilePanel("Create New Sequence", Application.dataPath + "/Sequences", "New Sequence", "unity");
        if (string.IsNullOrEmpty(path))
            return;

        string sequenceName = Path.GetFileNameWithoutExtension(path);
        string parentFolder = Path.GetDirectoryName(path);
        string folder = Path.Combine(parentFolder, sequenceName);

        if (Directory.Exists(path))
        {
            bool nonEmpty = Directory.EnumerateFileSystemEntries(path).Any();
            if (nonEmpty)
            {
                string message = $"Directory {folder} exists is non-empty!";
                EditorUtility.DisplayDialog("Error", message, "OK");
                return;
            }
        }
        else
        {
            Directory.CreateDirectory(folder);
        }

        // Create & open a new scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Populate scene
        GameObject environmentPrefab = (GameObject)PrefabUtility.InstantiatePrefab(EnvironmentTemplate);
        GameObject cameraPrefab = (GameObject)PrefabUtility.InstantiatePrefab(CameraTemplate);
        GameObject timelinePrefab = (GameObject)PrefabUtility.InstantiatePrefab(TimelineTemplate);
        PlayableDirector director = timelinePrefab.GetComponent<PlayableDirector>();

        // Create timeline playable next to scene
        Uri timelineFullPath = new Uri(Path.Combine(folder, $"{sequenceName}.playable"));
        Uri assetPathRoot = new Uri(Application.dataPath);
        string timelineAssetPath = UnityWebRequest.UnEscapeURL(assetPathRoot.MakeRelativeUri(timelineFullPath).ToString());

        TimelineAsset timeline = TimelineAsset.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, timelineAssetPath);
        director.playableAsset = timeline;

        // Lighting Settings
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.HSVToRGB(0, 0, 0.35f);

        RefreshReferences();
        ShowTimeline();
        CreateDrone();

        // Save scene asset
        string scenePath = Path.Combine(folder, $"{sequenceName}.unity");
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

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

        string operationName = "Create Drone";
        Undo.RecordObject(Director, operationName);
        Undo.RecordObject(Timeline, operationName);

        int droneCount = FindObjectsOfType<Crazyflie>().Length;
        GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(DroneTemplate);
        drone.name = $"Drone {droneCount}";
        drone.transform.position = new Vector3(0, 0.5f, 0);
        drone.transform.SetAsLastSibling();

        CrazyflieTrack track = Timeline.CreateTrack<CrazyflieTrack>(drone.name + " Track");
        Crazyflie crazyflie = drone.GetComponent<Crazyflie>();
        crazyflie?.Initialize(track);
        crazyflie?.SetColorKeyframe(Color.black, 0.0f);
        crazyflie?.SetWaypoint(drone.transform.position, 0.0f);
        Director.SetGenericBinding(track, crazyflie);

        AssetDatabase.Refresh();
        EditorUtility.SetDirty(Timeline);
        EditorUtility.SetDirty(track);
        EditorUtility.SetDirty(Director);
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

    static void UnlockTimeline()
    {
        if (TimelineWindowType == null)
        {
            return;
        }

        PropertyInfo instanceProperty = TimelineWindowType.GetProperty("instance");
        object windowInstance = instanceProperty.GetValue(null);
        TimelineWindowType.InvokeMember(
            "locked",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
            System.Type.DefaultBinder,
            windowInstance,
            new object[] { false }
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


        try
        {
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
        catch (TargetException)
        {

        }
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
