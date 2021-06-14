using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct OccupancyRange
{
    public float start;
    public float end;

    public OccupancyRange(float time, float timestep)
    {
        start = time;
        end = time + timestep;
    }

    public void ExtendTo(float time, float timestep)
    {
        end = Mathf.Max(time + timestep, end);
    }

    public bool Contains(float time)
    {
        return (time >= start && time <= end);
    }

    public override string ToString()
    {
        return $"({start.ToString("n3")} -> {end.ToString("n3")})";
    }
}


public class SearchGrid
{
    private int divisions;
    private Bounds bounds;

    public float Spacing { get; private set; }
    public Vector3 CellSize { get; private set; }

    private HashSet<ulong> staticObstacles = new HashSet<ulong>();
    private Dictionary<ulong, List<OccupancyRange>> dynamicObstacles = new Dictionary<ulong, List<OccupancyRange>>();

    public SearchGrid(float size, float targetGridSize)
    {
        uint targetDivisions = (uint)Mathf.CeilToInt(size / targetGridSize);
        divisions = (int)Morton.nextPowerOfTwo(targetDivisions);
        bounds = new Bounds(new Vector3(0, size / 2.0f, 0), size * Vector3.one);
        
        Spacing = size / divisions;
        CellSize = new Vector3(Spacing, Spacing, Spacing);
    }
    
    public void ClearObstacles()
    {
        this.staticObstacles = new HashSet<ulong>();
        this.dynamicObstacles = new Dictionary<ulong, List<OccupancyRange>>();
    }
    
    public void AddStaticObstacles(List<Bounds> obstacles)
    {
        foreach(Bounds bounds in obstacles)
        {
            Vector3Int min = GetGridPosition(bounds.min);
            Vector3Int max = GetGridPosition(bounds.max);

            /*
            // Fill in faces only to save time and memory

            // Z Faces
            for(int i = min.x; i <= max.x; i++)
            {
                for(int j = min.y; j <= max.y; j++)
                {
                    this.staticObstacles.Add(Morton.encode(i, j, min.z));
                    this.staticObstacles.Add(Morton.encode(i, j, max.z));
                }
            }
            
            // Y Faces
            for (int i = min.x; i <= max.x; i++)
            {
                for (int k = min.z; k <= max.z; k++)
                {
                    this.staticObstacles.Add(Morton.encode(i, min.y, k));
                    this.staticObstacles.Add(Morton.encode(i, max.y, k));
                }
            }
            
            // X Faces
            for (int j = min.y; j <= max.y; j++)
            {
                for (int k = min.z; k <= max.z; k++)
                {
                    this.staticObstacles.Add(Morton.encode(min.x, j, k));
                    this.staticObstacles.Add(Morton.encode(max.x, j, k));
                }
            }*/

            for (int i = min.x; i <= max.x; i++)
            {
                for (int j = min.y; j <= max.y; j++)
                {
                    for (int k = min.z; k <= max.z; k++)
                    {
                        this.staticObstacles.Add(Morton.encode(i, j, k));
                    }
                }
            }
        }
    }

    public void AddDynamicObstacles(List<Bounds> obstacles, float time, float timestep)
    {
        // Debug.Log("Adding dynamic obstacles for time: " + time);
        foreach (Bounds bounds in obstacles)
        {
            Vector3Int min = GetGridPosition(bounds.min);
            Vector3Int max = GetGridPosition(bounds.max);

            /* Causes "tunneling" issues
            // Fill in faces only to save time and memory

            // Z Faces
            for (int i = min.x; i <= max.x; i++)
            {
                for (int j = min.y; j <= max.y; j++)
                {
                    AddDynamicObstacle(Morton.encode(i, j, min.z), time, timestep);
                    AddDynamicObstacle(Morton.encode(i, j, max.z), time, timestep);
                }
            }

            // Y Faces
            for (int i = min.x; i <= max.x; i++)
            {
                for (int k = min.z; k <= max.z; k++)
                {
                    AddDynamicObstacle(Morton.encode(i, min.y, k), time, timestep);
                    AddDynamicObstacle(Morton.encode(i, max.y, k), time, timestep);
                }
            }

            // X Faces
            for (int j = min.y; j <= max.y; j++)
            {
                for (int k = min.z; k <= max.z; k++)
                {
                    AddDynamicObstacle(Morton.encode(min.x, j, k), time, timestep);
                    AddDynamicObstacle(Morton.encode(max.x, j, k), time, timestep);
                }
            }*/
            for (int i = min.x; i <= max.x; i++)
            {
                for (int j = min.y; j <= max.y; j++)
                {
                    for (int k = min.z; k <= max.z; k++)
                    {
                        AddDynamicObstacle(Morton.encode(i, j, k), time, timestep);
                    }
                }
            }
        }
    }

    private void AddDynamicObstacle(ulong block, float time, float timestep)
    {
        if (!dynamicObstacles.ContainsKey(block) || dynamicObstacles[block] == null)
            dynamicObstacles[block] = new List<OccupancyRange>();

        float minTimestep = Pathfinder.MinTimestep(CellSize.x);
        List<OccupancyRange> ranges = dynamicObstacles[block];
        if (ranges.Count == 0)
        {
            ranges.Add(new OccupancyRange(time, timestep));
            return;
        }

        OccupancyRange latest = ranges[ranges.Count - 1];
        if (latest.end >= time || (time - latest.end) <= minTimestep)
        {
            latest.ExtendTo(time, timestep);
            ranges[ranges.Count - 1] = latest;
        }
        else
        {
            ranges.Add(new OccupancyRange(time, timestep));
        }
    }


    public bool IsAvailable(Vector3Int position, float time)
    {
        ulong code = Morton.encode(position.x, position.y, position.z);
        bool staticOcclusion = staticObstacles.Contains(code);
        bool dynamicOcclusion = false;

        if (dynamicObstacles.ContainsKey(code))
            dynamicOcclusion = dynamicObstacles[code].Any(range => range.Contains(time));

        return !(staticOcclusion || dynamicOcclusion);
    }

    public Vector3Int GetGridPosition(Vector3 position)
    {
        int x = CalculateGridIndex(position.x, bounds.min.x);
        int y = CalculateGridIndex(position.y, bounds.min.y);
        int z = CalculateGridIndex(position.z, bounds.min.z);
        return new Vector3Int(x, y, z);
    }

    private int CalculateGridIndex(float position, float min)
    {
        float interval = bounds.size.x / divisions;
        float relativePosition = position - min;
        return Mathf.Clamp(Mathf.FloorToInt(relativePosition / interval), 0, divisions - 1);
    }

    public Vector3 GetCenter(Vector3Int pos)
    {
        float x = bounds.min.x + (CellSize.x * pos.x) + (0.5f * CellSize.x);
        float y = bounds.min.y + (CellSize.y * pos.y) + (0.5f * CellSize.y);
        float z = bounds.min.z + (CellSize.z * pos.z) + (0.5f * CellSize.z);
        return new Vector3(x, y, z);
    }

    public void DrawVoxelGrid()
    {
        PathGizmos.DrawGrid(bounds, new Vector3Int(divisions, divisions, divisions), new Color(1.0f, 1.0f, 1.0f, 0.15f));
    }

    public void DrawObstacles(float time)
    {
        Color previousColor = Gizmos.color;
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.25f);

        foreach (ulong morton in staticObstacles)
        {
            Vector3Int gridPostion = Morton.decode(morton);
            Vector3 position = GetCenter(gridPostion);
            Gizmos.DrawCube(position, CellSize);
        }


        Gizmos.color = new Color(0.0f, 0.5f, 1.0f, 0.25f);
        foreach (var kvPair in dynamicObstacles)
        {
            bool occluded = kvPair.Value.Any(r => r.Contains(time));
            if (occluded)
            {
                Vector3Int gridPostion = Morton.decode(kvPair.Key);
                Vector3 position = GetCenter(gridPostion);
                Gizmos.DrawCube(position, CellSize);
            }
        }


        Gizmos.color = previousColor;
    }

    public void DebugPrint()
    {
        foreach(ulong block in dynamicObstacles.Keys)
        {
            string[] ranges = dynamicObstacles[block].Select(range => range.ToString()).ToArray();
            Debug.Log("Block:" + block + ", ranges: [" + string.Join(", ", ranges) + "]");
        }
    }

    public static SearchGrid Generate(List<GameObject> drones, List<Vector3> targetPositions, float targetGridSize = 0.1f)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (GameObject drone in drones)
        {
            Vector3 position = drone.transform.position;
            min = ExpandMin(min, position);
            max = ExpandMax(max, position);
        }

        foreach (Vector3 position in targetPositions)
        {
            min = ExpandMin(min, position);
            max = ExpandMax(max, position);
        }

        min -= (0.75f * Vector3.one);
        max += (0.75f * Vector3.one);
        min.y = 0;

        Vector3 size = (max - min);
        float sideLength = Mathf.Max(size.x, size.y, size.z);

        SearchGrid grid = new SearchGrid(sideLength, targetGridSize);
        return grid;
    }



    private static Vector3 ExpandMin(Vector3 min, Vector3 position)
    {
        min.x = Mathf.Min(min.x, position.x);
        min.y = Mathf.Min(min.y, position.y);
        min.z = Mathf.Min(min.z, position.z);
        return min;
    }

    private static Vector3 ExpandMax(Vector3 max, Vector3 position)
    {
        max.x = Mathf.Max(max.x, position.x);
        max.y = Mathf.Max(max.y, position.y);
        max.z = Mathf.Max(max.z, position.z);
        return max;
    }

}