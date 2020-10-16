using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;


public class TimelineUtilities : MonoBehaviour
{
    [MenuItem("Drone Tools/Simulate Sequence")]
    public static void SimulateSequence()
    {

    }

    [MenuItem("Drone Tools/Run Sequence")]
    public static void RunSequence()
    {

    }

    [MenuItem("Drone Tools/Save Sequence")]
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
}
