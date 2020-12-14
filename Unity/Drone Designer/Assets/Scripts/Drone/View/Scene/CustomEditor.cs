using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

public abstract class CustomEditor <T> : Editor where T : UnityEngine.Object
{

    protected T Target { get { return target as T; } }

    protected virtual void OnEnable()
    {
        SceneView.duringSceneGui -= OnDrawScene;

        if (Target != null)
            SceneView.duringSceneGui += OnDrawScene;
    }
    protected virtual void OnDisable()
    {
        SceneView.duringSceneGui -= OnDrawScene;
    }

    protected abstract void OnDrawScene(SceneView scene);
}