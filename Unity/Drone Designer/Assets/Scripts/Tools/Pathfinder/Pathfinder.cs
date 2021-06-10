using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;



public struct Node
{
    public Vector3Int gridPosition;
    public Vector3 position;
    public float time;

    public Node(Vector3Int pos, Vector3 position, float t)
    {
        this.gridPosition = pos;
        this.position = position;
        this.time = t;
    }
}

public class PathData
{
    public GameObject drone;
    public Vector3 currentPosition;
    public Vector3 target;
    public Vector3Int targetGridPos;
    public float distance;
    public float currentDistance;

    public float staticPenalty = 0.0f;
    public HashSet<Vector3Int> visitedGridPositions = new HashSet<Vector3Int>();
    public List<Node> nodes = new List<Node>();
    public List<CubicBezier> curves = new List<CubicBezier>();

    public bool complete = false;
}

public class PathDataComparer : IComparer<PathData>
{
    public int Compare(PathData x, PathData y)
    {
        return x.currentDistance.CompareTo(y.currentDistance);
    }
}

public enum RowMode
{
    DEPTH, HEIGHT
}


[ExecuteInEditMode]
public class Pathfinder: MonoBehaviour
{
    private static readonly float DRONE_RADIUS = 0.15f;
    private const float MAX_VELOCITY = 0.25f;
    private const float TIMESTEP = 0.5f;

    [Range(1, 4)]
    public int Rows = 1;
    public RowMode RowStyle = RowMode.DEPTH;

    [Range(0, 10000)]
    public int MaxIterations = 1;

    public bool DrawMatch = true;
    public bool DrawDiscrete = true;
    public bool DrawContinuous = true;
    public bool DrawGrid = true;

    public GameObject DroneHolder;
    public GameObject TargetHolder;


    private List<GameObject> drones = new List<GameObject>();
    private List<Vector3> targets = new List<Vector3>();
    private List<PathData> pathData = new List<PathData>();
    
    // private AStarGrid gridArea = null;
    private PathGrid grid = null;
    private float totalTime = 0.0f;

    public void Update()
    {
    }

    public void OnDrawGizmos()
    {
        if (pathData == null)
            return;

        Color color = Gizmos.color;
        foreach (PathData path in pathData)
        {
            if (path == null) 
                continue;

            if (DrawMatch)
            {
                Gizmos.color = new Color(1, 1, 1, 0.05f);
                Gizmos.DrawLine(path.drone.transform.position, path.target);
            }


            if (DrawDiscrete)
            {
                for (int i = 1; i < path.nodes.Count; i++)
                {
                    Gizmos.color = new Color(1, 1, 1, 0.25f);
                    Vector3 start = path.nodes[i - 1].position;
                    Vector3 end = path.nodes[i].position;
                    Gizmos.DrawLine(start, end);

                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(end, 0.0075f);
                    Handles.Label(end + (0.02f * Vector3.right), path.nodes[i].time.ToString("n2"), CustomGUI.LabelStyle);
                }
            }

            if (DrawContinuous)
            {
                for (int i = 0; i < path.curves.Count; i++)
                {
                    CubicBezier bezier = path.curves[i];
                    Handles.DrawBezier(bezier.anchor1,
                                        bezier.anchor2,
                                        bezier.control1,
                                        bezier.control2,
                                        Color.white,
                                        null,
                                        2.0f);
                }
            }
        }
        Gizmos.color = color;

        if (grid != null && DrawGrid)
        {
            grid.Draw();
        }
    }
    public void SmoothPaths()
    {
        foreach(PathData data in pathData)
        {
            List<Vector3> points = data.nodes.Select(x => x.position).ToList();
            data.curves = BezierInterpolator.Interpolate(points);

            int n = data.curves.Count - 1;
            data.curves[0].SetControl1(data.curves[0].anchor1);
            data.curves[n].SetControl2(data.curves[n].anchor2);
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void FindPaths()
    {
        grid = new PathGrid(Drones, Targets);
        pathData.Sort((a, b) => a.distance.CompareTo(b.distance));

        totalTime = 0.0f;
        foreach (PathData path in pathData)
            StartPath(path, grid);


        totalTime += TIMESTEP;
        grid.ClearOccupancy();
        int completeCount = 0;
        int iterationCount = 0;

        while(completeCount < pathData.Count)
        {
            completeCount = StepPathfinding();
            iterationCount += 1;
            if (iterationCount >= MaxIterations)
                break;
        }
        MergeNodes();
        Debug.Log("Complete count: " + completeCount + " / " + pathData.Count);

        EditorApplication.QueuePlayerLoopUpdate();
    }


    private void MergeNodes()
    {
        foreach (PathData path in pathData)
        {
            List<Node> merged = new List<Node>();
            if (path.nodes.Count == 0)
                continue;

            Node currentNode = path.nodes[0];
            if (path.nodes.Count == 1)
                continue;

            for (int i = 1; i < path.nodes.Count; i++)
            {
                Node node = path.nodes[i];

                if (node.gridPosition != currentNode.gridPosition)                
                    merged.Add(currentNode);

                currentNode = node;                
            }
            merged.Add(currentNode);

            // Advanced Merge
            List<Node> superMerged = new List<Node>();
            if (merged.Count < 3)
                continue;

            Vector3 direction = merged[1].position - merged[0].position;
            path.nodes = superMerged;
        }
    }

    public void StartPathfinding()
    {
        grid = new PathGrid(Drones, Targets);
        pathData.Sort((a, b) => a.distance.CompareTo(b.distance));

        totalTime = 0.0f;
        foreach (PathData path in pathData)
            StartPath(path, grid);


        totalTime += TIMESTEP;
        grid.ClearOccupancy();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public int StepPathfinding()
    {
        int completeCount = pathData.Count(item => item.complete);
        if (completeCount == pathData.Count)
            return completeCount;

        grid.ClearOccupancy();
        Step(grid, TIMESTEP, totalTime);
        //Debug.Log($"Grid occupancy: {grid.TotalOccupancy} / {grid.Capacity}");
        totalTime += TIMESTEP;
        EditorApplication.QueuePlayerLoopUpdate();
        return completeCount;
    }

    private void OccupyPhysicalPosition(Vector3 position, GameObject drone)
    {
        Vector3Int gridPos = grid.GetGridPosition(position);
        grid.OccupyVolume(gridPos, drone);
    }

    private void AddPositionToPath(PathData data, Vector3 pos, float time)
    {
        Vector3Int gridPos = grid.GetGridPosition(pos);
        data.currentPosition = pos;
        data.currentDistance = Vector3.Distance(data.currentPosition, data.target);
        data.nodes.Add(new Node(gridPos, pos, time));
        data.visitedGridPositions.Add(gridPos);
    }

    private void AddNodeToPath(PathData data, Vector3Int node, float time)
    {
        AddPositionToPath(data, grid.GetCenter(node), time);
    }


    private void Step(PathGrid grid, float deltaTime, float time)
    {
        pathData.Sort((x, y) => x.currentDistance.CompareTo(y.currentDistance));
        foreach (PathData path in pathData)
            StepForward(path, grid, deltaTime, time);
    }

    private void StartPath(PathData path, PathGrid grid)
    {
        GameObject drone = path.drone;
        path.currentPosition = drone.transform.position;
        path.currentDistance = Vector3.Distance(path.currentPosition, path.target);
        path.targetGridPos = grid.GetGridPosition(path.target);
        AddPositionToPath(path, drone.transform.position, 0.0f);
    }

    private void StepForward(PathData pathData, PathGrid grid, float deltaTime, float totalTime)
    {
        OccupyPhysicalPosition(pathData.currentPosition, pathData.drone);
        if (pathData.complete)
            return;

        Vector3Int newGridPos = GetTargetPosition(pathData, grid, deltaTime, totalTime);
        if (newGridPos == grid.GetGridPosition(pathData.currentPosition))
        {
            //Debug.Log("CHECK 2 | not moving during this time step, couldn't find valid position!!");
        }
        if (newGridPos == pathData.targetGridPos)
        {
            pathData.complete = true;
            AddPositionToPath(pathData, pathData.target, totalTime);
            return;
        }
        else
        {
            AddNodeToPath(pathData, newGridPos, totalTime);
        }
        
    }

    private Vector3Int GetTargetPosition(PathData path, PathGrid grid, float delta, float totalTime)
    {
        Vector3 toTarget = (path.target - path.currentPosition);
        Vector3 direction = toTarget.normalized;
        Vector3Int startGridPosition = grid.GetGridPosition(path.currentPosition);
        Vector3Int targetGridPos = startGridPosition;

        float moveAmount = Mathf.Min(MAX_VELOCITY, toTarget.magnitude);
        int maxBoxesPerStep = Mathf.Max(1, Mathf.CeilToInt((MAX_VELOCITY * delta) / PathGrid.GridSize));
        float interval = moveAmount / maxBoxesPerStep;

        for (int i = 1; i <= maxBoxesPerStep; i++)
        {
            float amt = i * interval;
            Vector3 physicalPos = path.currentPosition + (amt * direction);
            Vector3Int gridPos = grid.GetGridPosition(physicalPos);
            bool available = (gridPos == path.targetGridPos || grid.IsVolumeAvailable(gridPos, path.drone));
            if (gridPos != targetGridPos && available)
            {
                targetGridPos = gridPos;
                grid.OccupyVolume(gridPos, path.drone);
                path.visitedGridPositions.Add(gridPos);

                if (gridPos == path.targetGridPos)
                    return targetGridPos;
            }
        }

        if (targetGridPos == startGridPosition)
        {
            float minHeuristic = SearchHeuristic(path, targetGridPos, direction) + path.staticPenalty;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector3Int potentialPosition = startGridPosition + new Vector3Int(i, j, k);
                        if (grid.IsVolumeAvailable(potentialPosition, path.drone) && !path.visitedGridPositions.Contains(potentialPosition))
                        {
                            float currentHeuristic = SearchHeuristic(path, potentialPosition, direction);
                            if (currentHeuristic < minHeuristic)
                            {
                                targetGridPos = potentialPosition;
                                minHeuristic = currentHeuristic;
                            }
                        }
                    }
                }
            }
        }

        if (targetGridPos == startGridPosition)
        {
            path.staticPenalty += 0.05f;
            Debug.Log($"Drone not moving during this time step (totalTime: {totalTime}) couldn't find valid position!! Penalty: {path.staticPenalty}");
        }
        else
        {
            path.staticPenalty = 0.0f;
        }

        path.visitedGridPositions.Add(targetGridPos);
        grid.OccupyVolume(targetGridPos, path.drone);
        return targetGridPos;

    }

    private float SearchHeuristic(PathData path, Vector3Int gridPosition, Vector3 targetDirection)
    {
        Vector3 direction = ((Vector3)(path.targetGridPos - gridPosition)).normalized;
        float targetAlignment = 1.0f - Vector3.Dot(direction, targetDirection);
        return Vector3Int.Distance(gridPosition, path.targetGridPos) + targetAlignment;
    }

    public void Match()
    {
        drones.Clear();
        targets.Clear();
        pathData.Clear();
        List<Tuple<int, int>> assignments = HungarianSolver.FindAssignments(Drones, Targets);

        foreach(Tuple<int, int> assignment in assignments)
        {
            PathData path = new PathData();
            path.drone = Drones[assignment.Item1];
            path.target = Targets[assignment.Item2];
            path.distance = Vector3.Distance(path.drone.transform.position, path.target);
            pathData.Add(path);
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }


    public void Shuffle()
    {
        drones.Clear();
        targets.Clear();
        pathData.Clear();

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

    public void LineUp()
    {
        drones.Clear();
        targets.Clear();
        pathData.Clear();

        int dronesPerRow = Mathf.CeilToInt((float)Drones.Count / Rows);

        float interval = (2.0f * DRONE_RADIUS);
        float startZ = -0.75f;
        float startY = 0.25f;



        for(int i = 0; i < Drones.Count; i++)
        {
            int row = i / dronesPerRow;
            int col = i % dronesPerRow;
            int numDronesInRow = Math.Min(dronesPerRow, Drones.Count - (row * dronesPerRow));
            float totalLength = (numDronesInRow - 1) * interval;
            float startX = -totalLength / 2.0f;

            GameObject drone = Drones[i];
            float x = startX + (col * interval);
            float y = startY + (row * 0.25f);
            float z = startZ - (row * interval);
            Vector3 position = Vector3.zero;
            if (RowStyle == RowMode.HEIGHT)
                position = new Vector3(x, y, startZ);
            else
                position = new Vector4(x, startY, z);
            drone.transform.position = position;
        }


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

}


[CustomEditor(typeof(Pathfinder))]
public class PathfinderEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(30.0f);

        if (GUILayout.Button("Shuffle"))
            ((Pathfinder)target).Shuffle();

        if (GUILayout.Button("Line Up"))
            ((Pathfinder)target).LineUp();


        EditorGUILayout.Space(30.0f);

        if (GUILayout.Button("Match"))
            ((Pathfinder)target).Match();

        if (GUILayout.Button("Full Pathfinding"))
            ((Pathfinder)target).FindPaths();

        if (GUILayout.Button("Bezier Interpolate"))
            ((Pathfinder)target).SmoothPaths();



        EditorGUILayout.Space(30.0f);

        if (GUILayout.Button("Start Pathfinding"))
            ((Pathfinder)target).StartPathfinding();

        if (GUILayout.Button("Step Pathfinding"))
            ((Pathfinder)target).StepPathfinding();

        EditorGUILayout.Space(30.0f);
    }
}