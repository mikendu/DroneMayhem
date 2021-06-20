using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;


[CustomEditor(typeof(GuideKeyframe))]
public class GuideKeyframeEditor : CustomEditor<GuideKeyframe>
{
    protected override void OnEnable()
    {
        base.OnEnable();
        TimelineUtilities.Director.time = Target.time;
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
    }

    protected override void OnDrawScene(SceneView scene)
    {
        GuideKeyframe keyframe = Target;
        DynamicGuide guide = keyframe.Guide;
        DynamicGuideEditor.Draw(guide);

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            guide.RemoveKeyframe(Target);

        DrawGUI(keyframe);
    }

    public static void DrawGUI(GuideKeyframe keyframe)
    {
        Rect toolsRect = new Rect(340, 20, 300, 180);
        CustomGUI.Window(toolsRect, "Guide Keyframe", DrawWaypointTools, keyframe);
    }

    private static void DrawWaypointTools(GuideKeyframe keyframe)
    {
        DynamicGuide guide = keyframe.Guide;

        EditorGUI.BeginChangeCheck();
        JointType updatedJointType = (JointType)EditorGUILayout.EnumPopup("Joint Type", keyframe.JointType);
        EditorGUILayout.Space(10);


        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(keyframe, "Change Guide Keyframe");
            keyframe.JointType = updatedJointType;
            guide.UpdateView();
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        if (GUILayout.Button("Delete"))
            guide.RemoveKeyframe(keyframe);
    }
}