using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;



public struct CompletedPath
{
    public GameObject drone;
    public List<Vector3> discrete;
    public List<CubicBezier> continuous;
}

public struct DroneAssignment
{
    public GameObject drone;
    public Vector3 target;
    public float centroidDistance;
    public float targetHeight;
}

[ExecuteInEditMode]
public class Pathfinder: MonoBehaviour
{
    public const float MAX_VELOCITY = 0.5f;

    [Range(1, 4)]
    public int Rows = 1;
    public ArrangmentRowType RowStyle = ArrangmentRowType.DEPTH;

    public bool DrawMatch = true;
    public bool DrawDiscrete = true;
    public bool DrawContinuous = true;
    public bool DrawTangents = true;
    public bool DrawGrid = true;

    public GameObject DroneHolder;
    public GameObject TargetHolder;

    [Range(0, 150.0f)]
    public float PathTime = 0.0f;

    [Range(1.0f, 5.0f)]
    public float DelayPerLevel = 1.0f;

    [Range(0, 50)]
    public int SolutionSize = 1;

    public bool Playing = false;

    private List<GameObject> drones = new List<GameObject>();
    private List<Vector3> targets = new List<Vector3>();

    private List<DroneAssignment> assignments = new List<DroneAssignment>();
    private List<CompletedPath> finishedPaths = new List<CompletedPath>();

    private SearchGrid searchGrid;
    private float totalTime = 0.0f;


    public void Update()
    {
        if (Playing)
        {
            PathTime = Mathf.Clamp(PathTime + Time.deltaTime, 0.0f, 150.0f);
        }

        if (Application.isPlaying)
        {
            foreach(CompletedPath path in finishedPaths)
            {
                path.drone.transform.position = BezierEvaluator.FindPosition(path.continuous, PathTime);
            }
        }
    }

    public void OnDrawGizmos()
    {
        if (searchGrid != null && DrawGrid)
        {
            //searchGrid.DrawVoxelGrid();
            searchGrid.DrawObstacles(PathTime);
        }


        if (DrawMatch)
        {
            foreach (DroneAssignment assignment in assignments)
            {
                Gizmos.color = new Color(1, 1, 1, 0.15f);
                Gizmos.DrawLine(assignment.drone.transform.position, assignment.target);
            }
        }

        Color color = Gizmos.color;
        foreach (CompletedPath path in finishedPaths )
        {
            if (DrawDiscrete)
            {
                for (int i = 1; i < path.discrete.Count;  i++)
                {
                    Gizmos.color = new Color(1, 1, 1, 0.25f);
                    Vector3 start = path.discrete[i - 1];
                    Vector3 end = path.discrete[i];
                    Gizmos.DrawLine(start, end);

                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(end, 0.0075f);
                }
            }

            if (DrawContinuous)
            {
                for (int i = 0; i < path.continuous.Count; i++)
                {
                    CubicBezier bezier = path.continuous[i];
                    Handles.DrawBezier(bezier.anchor1,
                                        bezier.anchor2,
                                        bezier.control1,
                                        bezier.control2,
                                        Color.white,
                                        null,
                                        2.0f);

                    if (DrawTangents)
                    {
                        Gizmos.DrawLine(bezier.anchor1, bezier.control1);
                        Gizmos.DrawLine(bezier.anchor2, bezier.control2);

                        Gizmos.DrawSphere(bezier.control1, 0.0025f);
                        Gizmos.DrawSphere(bezier.control2, 0.0025f);
                    }

                    Handles.Label(bezier.anchor2 + new Vector3(0.025f, 0, 0), bezier.endTime.ToString("n2"), CustomGUI.LabelStyle);
                }
            }
        }
        Gizmos.color = color;
    }

    public void Refresh()
    {
        drones.Clear();
        targets.Clear();
        assignments.Clear();
        finishedPaths.Clear();
        searchGrid = null;
        totalTime = 0.0f;
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void Rearrange(bool shuffle = true)
    {
        Refresh();
        if (shuffle)
            DroneArranger.Shuffle(Drones);
        else
            DroneArranger.LineUp(Drones, Rows, RowStyle);

        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void FindMatching()
    {
        this. assignments.Clear();
        finishedPaths.Clear();

        HungarianSolver.FindAssignments(Drones, Targets)
            .ForEach(assignment =>
            {
                DroneAssignment droneAssignment = new DroneAssignment();
                droneAssignment.drone = Drones[assignment.Item1];
                droneAssignment.target = Targets[assignment.Item2];
                this.assignments.Add(droneAssignment);
            });

        /*

        Vector3 centroid = GetCentroid();
        for(int i = 0; i < assignments.Count; i++)
        {
            DroneAssignment assignment = assignments[i];
            assignment.centroidDistance = Vector3.Distance(assignment.target, centroid);
            assignment.targetHeight = assignment.target.y;
            assignments[i] = assignment;
        }*/

        EditorApplication.QueuePlayerLoopUpdate();
    }

    private void LayeredPathing()
    {
        SortedDictionary<int, List<CompletedPath>> partitionedPaths = new SortedDictionary<int, List<CompletedPath>>();
        float maxTime = 0.0f;

        foreach(DroneAssignment assignment in assignments)
        {
            CompletedPath path = new CompletedPath();
            path.drone = assignment.drone;
            path.discrete = new List<Vector3>();
            path.continuous = new List<CubicBezier>();

            Vector3 start = path.drone.transform.position;
            Vector3 end = assignment.target;

            CubicBezier curve = new CubicBezier();
            curve.startTime = 0.0f;
            curve.endTime = GetTime(Vector3.Distance(start, end));
            curve.anchor1 = start;
            curve.control1 = start;
            curve.control2 = end;
            curve.anchor2 = end;

            maxTime = Mathf.Max(maxTime, curve.endTime);


            int scaledHeight = Mathf.RoundToInt(assignment.target.y * 10);
            if (!partitionedPaths.ContainsKey(scaledHeight))
                partitionedPaths[scaledHeight] = new List<CompletedPath>();


            partitionedPaths[scaledHeight].Add(path);
            path.continuous.Add(curve);
            finishedPaths.Add(path);
        }

        float delayPerLevel = 0.25f * maxTime;
        float delay = delayPerLevel * (partitionedPaths.Count - 1);


        foreach(var kvPair in partitionedPaths)
        {
            foreach(CompletedPath path in kvPair.Value)
            {
                CubicBezier bezier = path.continuous[0];
                bezier.startTime += delay;
                bezier.endTime += delay;
                path.continuous[0] = bezier;
            }

            delay -= delayPerLevel;
        }
    }

    public void FindPaths()
    {
        finishedPaths.Clear();
        if (this.assignments.Count == 0)
        {
            Debug.LogWarning("No drone assignments have been made yet!");
            return;
        }

        searchGrid = SearchGrid.Generate(Drones, Targets);

        int numSolved = 0;
        while(assignments.Count > 0 && numSolved < SolutionSize)
        {
            if (numSolved == 1)
                Searcher.DEBUG_PRINT = true;
            else
                Searcher.DEBUG_PRINT = false;

            assignments.Sort(this.AssignmentSorter);

            DroneAssignment currentAssignment = assignments[0];
            assignments.RemoveAt(0);

            finishedPaths.Add(FindAndSmoothPath(currentAssignment));
            numSolved += 1;
        }

        AddDynamicObstacles();


        EditorApplication.QueuePlayerLoopUpdate();
    }

    private CompletedPath FindAndSmoothPath(DroneAssignment assignment)
    {
        searchGrid.ClearObstacles();
        AddUnassignedObstacles(assignment);
        AddDynamicObstacles();

        CompletedPath path = new CompletedPath();
        path.drone = assignment.drone;
        path.discrete = Searcher.FindPath(searchGrid, assignment.drone, assignment.target);

        float totalTime = 0.0f;
        path.continuous = new List<CubicBezier>();
        for (int i = 1; i < path.discrete.Count; i++)
        {
            Vector3 previous = path.discrete[i - 1];
            Vector3 current = path.discrete[i];
            CubicBezier bezier = new CubicBezier();
            bezier.anchor1 = previous;
            bezier.control1 = previous;
            bezier.control2 = current;
            bezier.anchor2 = current;
            bezier.startTime = totalTime;

            totalTime += GetTime(Vector3.Distance(previous, current));
            bezier.endTime = totalTime;

            /*
            float multiplier = Mathf.Max(1.0f, BezierEvaluator.RescaleTime(bezier, MAX_VELOCITY));
            float delta = bezier.endTime - bezier.startTime;
            bezier.endTime = bezier.startTime + (multiplier * delta);
            totalTime = bezier.endTime;*/

            path.continuous.Add(bezier);
        }

        

        //path.continuous = BezierInterpolator.Interpolate(path.discrete);

        // Smoothing Pass (Minimizes Jerk)
        /*
        for (int i = 0; i < path.continuous.Count - 1; i++)
        {
            CubicBezier current = path.continuous[i];
            CubicBezier next = path.continuous[i + 1];

            float maxSize = 0.01f * Vector3.Distance(current.anchor1, current.anchor2);
            Vector3 firstTangent = Vector3.ClampMagnitude(current.control1 - current.anchor1, maxSize);
            Vector3 secondTangent = Vector3.ClampMagnitude(current.control2 - current.anchor2, maxSize);

            current.control1 = current.anchor1 + firstTangent;
            current.control2 = current.anchor2 + secondTangent;

            next.control1 = next.anchor1 - secondTangent;

            path.continuous[i] = current;
            path.continuous[i + 1] = next;

            if (i > 0)
            {
                CubicBezier previous = path.continuous[i - 1];
                previous.control2 = previous.anchor2 - firstTangent;
                path.continuous[i - 1] = previous;
            }
        }

        for (int i = path.continuous.Count - 1; i > 0; i--)
        {
            CubicBezier current = path.continuous[i];
            CubicBezier previous = path.continuous[i - 1];

            float maxSize = 0.25f * Vector3.Distance(current.anchor1, current.anchor2);
            Vector3 minimized = ((3 * current.control2) + current.anchor1 - current.anchor2) / 3.0f;

            Vector3 delta = Vector3.ClampMagnitude(minimized - current.anchor1, 100);
            current.control1 = current.anchor1 + delta;
            previous.control2 = previous.anchor2 - delta;

            path.continuous[i] = current;
            path.continuous[i - 1] = previous;
        }

        int n = path.continuous.Count - 1;
        CubicBezier first = path.continuous[0];
        CubicBezier last = path.continuous[n];
       
        first.control1 = first.anchor1;
        last.control2 = last.anchor2;

        path.continuous[0] = first;
        path.continuous[n] = last;


        // Time Assignments

        float totalTime = 0.0f;
        for(int i = 0; i < path.continuous.Count; i++)
        {
            CubicBezier bezier = path.continuous[i];
            float travelTime = BezierEvaluator.Integrate(bezier, MAX_VELOCITY);

            bezier.startTime = totalTime;
            totalTime += travelTime;

            bezier.endTime = totalTime;
            path.continuous[i] = bezier;
        }

        
        /*
        for (int i = 0; i < path.continuous.Count; i++)
        {
            CubicBezier bezier = path.continuous[i];
            Vector3 jerkVector = -bezier.anchor1 + (3 * bezier.control1) - (3 * bezier.control2) + (bezier.anchor2);
            float jerk = 6 * jerkVector.magnitude;
            Debug.Log("REVISED | Curve from " + bezier.startTime + " to " + bezier.endTime + " has herk energy: " + jerk);
        }*/



        /*
        // Smoothing Pass
        for (int i = 0; i < path.continuous.Count - 1; i++)
        {

            CubicBezier current = path.continuous[i];
            CubicBezier next = path.continuous[i + 1];

            float maxSize = 0.75f * Vector3.Distance(current.anchor1, current.anchor2);
            Vector3 firstTangent = Vector3.ClampMagnitude(current.control1 - current.anchor1, maxSize);
            Vector3 secondTangent = Vector3.ClampMagnitude(current.control2 - current.anchor2, maxSize);

            current.control1 = current.anchor1 + firstTangent;
            current.control2 = current.anchor2 + secondTangent;

            next.control1 = next.anchor1 - secondTangent;

            path.continuous[i] = current;
            path.continuous[i + 1] = next;

            if (i > 0)
            {
                CubicBezier previous = path.continuous[i - 1];
                previous.control2 = previous.anchor2 - firstTangent;
                path.continuous[i - 1] = previous;
            }
        }*/

        return path;
    }

    private void AddUnassignedObstacles(DroneAssignment currentAssignment)
    {
        List<Bounds> staticObstacles = new List<Bounds>();
        foreach (DroneAssignment assign in assignments)
        {
            if (assign.drone != currentAssignment.drone)
                staticObstacles.Add(GetDroneBounds(assign.drone.transform.position));
        }

        searchGrid.AddStaticObstacles(staticObstacles);
    }

    private void AddDynamicObstacles()
    {
        float timestep = MinTimestep(searchGrid.CellSize.x);
        float maxTime = MaxTime;

        for (float time = 0.0f; time < maxTime; time += timestep)
        {
            List<Bounds> allBounds = new List<Bounds>();            
            foreach(CompletedPath path in finishedPaths)
            {
                Vector3 position = BezierEvaluator.FindPosition(path.continuous, time);
                allBounds.Add(GetDroneBounds(position));
            }

            searchGrid.AddDynamicObstacles(allBounds, time, timestep);
        }
    }

    private int AssignmentSorter(DroneAssignment a, DroneAssignment b)
    {
        if (Mathf.Approximately(a.centroidDistance, b.centroidDistance))
            return a.targetHeight.CompareTo(b.targetHeight);

        return a.centroidDistance.CompareTo(b.centroidDistance);
    }
    
    private Bounds GetDroneBounds(Vector3 position)
    {
        float radius = DroneArranger.DRONE_RADIUS;
        return new Bounds(position, new Vector3(2 * radius, 4 * radius, 2 * radius));
    }

    public void Solve()
    {
        Refresh();
        FindMatching();
        //FindPaths();
        LayeredPathing();
    }

    private Vector3 GetCentroid()
    {
        Vector3 total = Vector3.zero;
        for (int i = 0; i < assignments.Count; i++)
            total += assignments[i].target;

        return (1.0f / assignments.Count) * total;
    }

    private float MaxTime
    {
        get
        {
            if (finishedPaths.Count == 0)
                return 0.0f;

            return finishedPaths.Max(path => {

                if (path.continuous.Count > 0)
                    return path.continuous.Max((bezier) => bezier.endTime);

                return 0.0f;
            });
        }
    }

    private List<GameObject> Drones
    {
        get
        {
            if (drones == null)
                drones = new List<GameObject>();
            
            if (drones.Count == 0)
            {
                foreach (Transform transform in DroneHolder.transform)
                {
                    if (transform.gameObject.activeInHierarchy)
                    {
                        drones.Add(transform.gameObject);
                    }
                }
            }

            return drones;
        }
    }

    private List<Vector3> Targets
    {
        get
        {
            if (targets == null)
                targets = new List<Vector3>();

            if (targets.Count == 0)
            {
                foreach (Transform transform in TargetHolder.transform)
                {
                    if (transform.gameObject.activeInHierarchy)
                    {
                        targets.Add(transform.position);
                    }
                }
            }

            return targets;
        }
    }

    public static float MinTimestep(float gridSize)
    {
        return GetTime(gridSize);
    }

    public static float GetTime(float distance)
    {
        return (distance / MAX_VELOCITY);
    }
}


[CustomEditor(typeof(Pathfinder))]
public class PathfinderEditor : Editor
{

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Reset"))
            ((Pathfinder)target).Refresh();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Shuffle"))
                ((Pathfinder)target).Rearrange(true);

            if (GUILayout.Button("Line Up"))
                ((Pathfinder)target).Rearrange(false);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(30.0f);

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Find Matching"))
                ((Pathfinder)target).FindMatching();

            if (GUILayout.Button("Find Paths"))
                ((Pathfinder)target).FindPaths();

        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(30.0f);


        if (GUILayout.Button("Solve"))
            ((Pathfinder)target).Solve();


        EditorGUILayout.Space(30.0f);
        DrawDefaultInspector();
    }
}