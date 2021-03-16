using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GlobalTransform : MonoBehaviour
{
    public static Transform Current { get; private set; } = null;

    public static Vector3 Transfomed(Vector3 position)
    {
        if (Current == null)
            return position;

        return Current.TransformPoint(position);
    }

    public static Vector3 InverseTransfomed(Vector3 position)
    {
        if (Current == null)
            return position;

        return Current.InverseTransformPoint(position);
    }


    public void Start() { Current = transform; }
    public void Update() { Current = transform; }

    public void OnEnable()
    {
        Current = transform;
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void OnDisable()
    {
        Current = null;
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void OnDestroy()
    {
        Current = null;
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void Apply()
    {
        Crazyflie[] drones = GameObject.FindObjectsOfType<Crazyflie>();
        foreach (Crazyflie drone in drones)
            drone.ApplyGlobalTransform();

        Undo.DestroyObjectImmediate(gameObject);
    }
}



[CustomEditor(typeof(GlobalTransform))]
public class GlobalTransformEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space(15);
        if (Button("Apply"))
        {
            ((GlobalTransform)target).Apply();
        }
    }

    private bool Button(string text)
    {
        bool result = false;
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(text, GUILayout.Width(200)))
            result = true;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        return result;
    }
}