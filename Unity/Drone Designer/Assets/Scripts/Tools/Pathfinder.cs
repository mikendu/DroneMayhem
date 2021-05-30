using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;


public class PathData
{
    public GameObject drone;
    public Vector3 target;
}

public class DistanceData
{
    public int droneIndex;
    public int targetIndex;
    public float distance;
}

public class DistanceComparer : IComparer<DistanceData>
{
    public int Compare(DistanceData d1, DistanceData d2)
    {
        return d1.distance.CompareTo(d2.distance);
    }
}

[ExecuteInEditMode]
public class Pathfinder: MonoBehaviour
{
    private static readonly float DRONE_RADIUS = 0.15f;

    public Transform Target = null;
    public List<GameObject> Drones = new List<GameObject>();

    private List<Vector3> targetPositions = new List<Vector3>();
    private List<PathData> pathData = new List<PathData>();

    public void Update()
    {
        
    }

    public void OnDrawGizmos()
    {
        if (pathData == null)
            return;

        foreach(PathData path in pathData)
        {
            if (path == null) 
                continue;

            Gizmos.DrawLine(path.drone.transform.position, path.target);
        }
        
    }

    public void Solve(bool greedy = false)
    {
        targetPositions.Clear();
        GetTargets();

        pathData.Clear();

        if (greedy)
            GreedySolver();
        else
            Hungarian();
    }

    private void Hungarian()
    {
        List<Tuple<int, int>> assignments = HungarianSolver.FindAssignments(Drones, targetPositions);

        foreach(Tuple<int, int> assignment in assignments)
        {
            PathData path = new PathData();
            path.drone = Drones[assignment.Item1];
            path.target = targetPositions[assignment.Item2];
            pathData.Add(path);
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private void GreedySolver()
    {
        SortedSet<DistanceData> distanceData = new SortedSet<DistanceData>(new DistanceComparer());

        // Get all distances
        for (int i = 0; i < targetPositions.Count; i++)
        {
            Vector3 target = targetPositions[i];
            for (int j = 0; j < Drones.Count; j++)
            {
                GameObject drone = Drones[j];
                DistanceData data = new DistanceData();
                data.targetIndex = i;
                data.droneIndex = j;
                data.distance = Vector3.Distance(target, drone.transform.position);
                distanceData.Add(data);
            }
        }

        HashSet<int> assignedDrones = new HashSet<int>();
        HashSet<int> assignedTargets = new HashSet<int>();

        while (distanceData.Count > 0)
        {
            DistanceData minData = distanceData.Min;
            if (!assignedDrones.Contains(minData.droneIndex) && !assignedTargets.Contains(minData.targetIndex))
            {
                PathData path = new PathData();
                path.drone = Drones[minData.droneIndex];
                path.target = targetPositions[minData.targetIndex];

                assignedDrones.Add(minData.droneIndex);
                assignedTargets.Add(minData.targetIndex);
                pathData.Add(path);
            }
            distanceData.Remove(minData);
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private void GetTargets()
    {
        if (Target == null)
        {
            throw new InvalidOperationException("Target is null!!");
        }

        targetPositions.Add(Target.TransformPoint(new Vector3(-0.5f, -0.5f, -0.5f)));
        targetPositions.Add(Target.TransformPoint(new Vector3( 0.5f, -0.5f, -0.5f)));
        targetPositions.Add(Target.TransformPoint(new Vector3( 0.5f, -0.5f,  0.5f)));
        targetPositions.Add(Target.TransformPoint(new Vector3(-0.5f, -0.5f,  0.5f)));

        targetPositions.Add(Target.TransformPoint(new Vector3(-0.5f,  0.5f, -0.5f)));
        targetPositions.Add(Target.TransformPoint(new Vector3( 0.5f,  0.5f, -0.5f)));
        targetPositions.Add(Target.TransformPoint(new Vector3( 0.5f,  0.5f,  0.5f)));
        targetPositions.Add(Target.TransformPoint(new Vector3(-0.5f,  0.5f,  0.5f)));
    }

    public void Randomize()
    {
        Quaternion rotation = Quaternion.Euler(-90, 0, 0);
        foreach(GameObject drone in Drones)
        {
            Vector3 position = rotation * Random.insideUnitCircle;
            position.y = 0.25f;
            drone.transform.position = position;
        }

        float minDistance = 1.9f * DRONE_RADIUS;
        float offset = 0.0f;
        do
        {
            offset = Offset();
        } while (offset < minDistance);

        EditorApplication.QueuePlayerLoopUpdate();
    }

    public float Offset()
    {
        float minDistanceFound = float.MaxValue;
        float minDistance = 2.0f * DRONE_RADIUS;
        for (int i = 0; i < Drones.Count - 1; i++)
        {
            GameObject drone1 = Drones[i];
            for (int j = i + 1; j < Drones.Count; j++)
            {
                GameObject drone2 = Drones[j];
                float distance = Vector3.Distance(drone1.transform.position, drone2.transform.position);
                minDistanceFound = Mathf.Min(distance, minDistanceFound);

                if (distance < minDistance)
                {
                    Vector3 centroid = (0.5f * (drone1.transform.position + drone2.transform.position));
                    OffsetDrone(drone1, centroid);
                    OffsetDrone(drone2, centroid);
                }
            }
        }

        return minDistanceFound;
    }

    private void OffsetDrone(GameObject drone, Vector3 centroid)
    {
        Vector3 diff = drone.transform.position - centroid;
        Vector3 direction = diff.normalized;
        float offsetAmt = (DRONE_RADIUS - diff.magnitude);
        drone.transform.position += (offsetAmt * direction);
    }

}


[CustomEditor(typeof(Pathfinder))]
public class PathfinderEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(30.0f);


        if (GUILayout.Button("Full Refresh"))
        {
            ((Pathfinder)target).Randomize();
            ((Pathfinder)target).Solve(false);
        }


        if (GUILayout.Button("Randomize"))
            ((Pathfinder)target).Randomize();


        if (GUILayout.Button("Hungarian Solve"))
            ((Pathfinder)target).Solve();

        if (GUILayout.Button("Greedy Solve"))
            ((Pathfinder)target).Solve(true);


        EditorGUILayout.Space(30.0f);
    }
}