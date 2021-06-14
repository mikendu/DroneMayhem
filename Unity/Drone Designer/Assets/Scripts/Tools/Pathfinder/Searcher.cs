using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;


struct Node
{
    public Vector3Int position;
    public float heuristic;

    public Node(Vector3Int position, float heuristic)
    {
        this.position = position;
        this.heuristic = heuristic;
    }

    public override bool Equals(object obj)
    {
        if (obj is Node)
            return ((Node)obj).position.Equals(position);

        return false;
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

}

public class Searcher
{
    public static bool DEBUG_PRINT = false;

    private static SortedSet<Node> openSet;
 
    private static HashSet<Vector3Int> visited;
    private static Dictionary<Vector3Int, float> costMap;
    private static Dictionary<Vector3Int, Vector3Int> parentMap;
    private static Dictionary<Vector3Int, float> timeMap;

    private static Vector3Int start;
    private static Vector3Int goal;


    public static List<Vector3> FindPath(SearchGrid grid, GameObject drone, Vector3 target)
    {
        openSet = new SortedSet<Node>(new FuncComparer<Node>((x, y) => x.heuristic.CompareTo(y.heuristic)));
        costMap = new Dictionary<Vector3Int, float>();
        visited = new HashSet<Vector3Int>();
        parentMap = new Dictionary<Vector3Int, Vector3Int>();
        timeMap = new Dictionary<Vector3Int, float>();

        start = grid.GetGridPosition(drone.transform.position);
        goal = grid.GetGridPosition(target);

        Node startNode = new Node(start, Heuristic(start, goal));
        timeMap[start] = 0;
        costMap[start] = 0.0f;

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = openSet.Min;
            openSet.Remove(current);

            if (current.position == goal)
                return ReconstructPath(grid, drone, target);

            visited.Add(current.position);
            List<Vector3Int> neighbors = GetNeightbors(current.position);

            float currentTime = timeMap.ContainsKey(current.position) ? timeMap[current.position] : 0;

            foreach (Vector3Int neighbor in neighbors)
            {
                float travelTime = GetTravelTime(grid, current.position, neighbor);
                if (!visited.Contains(neighbor) && grid.IsAvailable(neighbor, currentTime + travelTime))
                {
                    UpdateVertex(grid, current.position, neighbor);
                }
            }
        }

        Reset();
        return new List<Vector3>();
    }


    private static void UpdateVertex(SearchGrid grid, Vector3Int current, Vector3Int neighbor)
    {
        bool hasParent = parentMap.ContainsKey(current);

        if (hasParent && HasLineOfSight(grid, parentMap[current], neighbor))
        {
            Vector3Int parent = parentMap[current];

            float score = costMap[parent] + Cost(parent, neighbor);
            float time = timeMap[parent] + GetTravelTime(grid, parent, neighbor);

            if (!costMap.ContainsKey(neighbor) || score < costMap[neighbor])
            {
                costMap[neighbor] = score;
                timeMap[neighbor] = time;
                parentMap[neighbor] = parent;
                
                Node newNode = new Node(neighbor, score + Heuristic(neighbor, goal));

                if (!openSet.Contains(newNode))
                    openSet.Add(newNode);
            }

        }
        else
        {
            float score = costMap[current] + Cost(current, neighbor);
            float time = timeMap[current] + GetTravelTime(grid, current, neighbor);

            if (!costMap.ContainsKey(neighbor) || score < costMap[neighbor])
            {
                costMap[neighbor] = score;
                parentMap[neighbor] = current;
                timeMap[neighbor] = time;

                Node newNode = new Node(neighbor, score + Heuristic(neighbor, goal));

                if (!openSet.Contains(newNode))
                    openSet.Add(newNode);
            }
        }
    }


    private static bool HasLineOfSight(SearchGrid grid, Vector3Int from, Vector3Int to)
    {
        Vector3 start = grid.GetCenter(from);
        Vector3 end = grid.GetCenter(to);
        Vector3 direction = (end - start).normalized;

        Vector3Int currentGridPos = from;

        do
        {
            float time = timeMap[from] + GetTravelTime(grid, from, currentGridPos);
            if (!grid.IsAvailable(currentGridPos, time))
            {
                return false;
            }

            Bounds currentBounds = new Bounds(grid.GetCenter(currentGridPos), grid.CellSize);
            Tuple<Vector3, Vector3> intersection = BoxIntersector.Intersect(start, direction, currentBounds);
            
            Vector3 outPosition = intersection.Item2 + (0.01f * direction);
            currentGridPos = grid.GetGridPosition(outPosition);

        } while (currentGridPos != to);

        return true;
    }

    private static float GetTravelTime(SearchGrid grid, Vector3Int start, Vector3Int end)
    {
        float physicalDistance = Vector3.Distance(grid.GetCenter(start), grid.GetCenter(end));
        return Pathfinder.GetTime(physicalDistance);
    }

    private static List<Vector3> ReconstructPath(SearchGrid searchGrid, GameObject drone, Vector3 target)
    {
        List<Vector3> results = new List<Vector3>();
        results.Add(target);

        Vector3Int current = goal;
        while(parentMap.ContainsKey(current))
        {
            Vector3Int parentPos = parentMap[current];
            results.Add(searchGrid.GetCenter(parentPos));
            current = parentPos;
        }

        results.Reverse();
        results[0] = drone.transform.position;

        Reset();
        return results;
    }

    private static float Cost(Vector3Int from, Vector3Int to)
    {
        return Vector3Int.Distance(from, to);
    }

    private static float Heuristic(Vector3Int node, Vector3Int goal)
    {
        return Vector3.Distance(node, goal);
    }

    private static List<Vector3Int> GetNeightbors(Vector3Int position)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    if (i == 0 && j == 0 && k == 0)
                        continue;

                    Vector3Int neighbor = position + new Vector3Int(i, j, k);
                    neighbors.Add(neighbor);
                }
            }
        }

        return neighbors;
    }

    private static void Reset()
    {
        openSet = null;
        visited = null;
        costMap = null;
        timeMap = null;
        parentMap = null;
    }
}