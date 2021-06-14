using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class OctreeTest : MonoBehaviour
{
    public bool DrawGrid = false;
    public bool DrawObstacles = false;

    [Range(2.0f, 8.0f)]
    public float Size = 4;

    [Range(0.01f, 0.5f)]
    public float GridSize = 0.1f;

    public List<MeshRenderer> Obstacles = new List<MeshRenderer>();

    // private SparseVoxelOctree octree = null;
    private SearchGrid searchGrid;

    private void OnDrawGizmos()
    {
        if (searchGrid != null)
        {
            if (DrawGrid)
                searchGrid.DrawVoxelGrid();

            if (DrawObstacles)
                searchGrid.DrawObstacles(0);
        }
    }

    public void Update()
    {
    }

    public void Refresh()
    {
        List<Bounds> obstacles = Obstacles.Select(mesh => mesh.bounds).ToList();
        // searchGrid = SearchGrid.Generate(obstacles, Size, GridSize);
        EditorApplication.QueuePlayerLoopUpdate();
    }
}



[CustomEditor(typeof(OctreeTest))]
public class OctreeTestEditor : Editor
{

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Refresh"))
            ((OctreeTest)target).Refresh();

        EditorGUILayout.Space(30.0f);
        DrawDefaultInspector();
    }
}