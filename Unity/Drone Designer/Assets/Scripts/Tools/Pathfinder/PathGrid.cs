using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



public class PathGrid
{
    public static float GridSize = 0.0625f;
    private static readonly Vector3 TargetDimensions = new Vector3(GridSize, GridSize, GridSize);

    private Vector3Int divisions;
    private Vector3 intervals;
    private Bounds bounds;
    private int[,,] obstacles;

    public PathGrid(List<GameObject> drones, List<Vector3> targetPositions)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach(GameObject drone in drones) 
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

        Vector3 center = 0.5f * (min + max);
        Vector3 size = max - min;
        bounds = new Bounds(center, size);

        int divX = Mathf.CeilToInt(size.x / TargetDimensions.x);
        int divY = Mathf.CeilToInt(size.y / TargetDimensions.y);
        int divZ = Mathf.CeilToInt(size.z / TargetDimensions.z);

        divisions = new Vector3Int(divX, divY, divZ);
        intervals = new Vector3(size.x / divX, size.y / divY, size.z / divZ);
        obstacles = new int[divX, divY, divZ];

        /*
        foreach(GameObject drone in drones)
        {
            Vector3Int gridPos = GetGridPosition(drone.transform.position);
            obstacles[gridPos.x, gridPos.y, gridPos.z] = true;
        }

        foreach (Vector3 position in targetPositions)
        {
            Vector3Int gridPos = GetGridPosition(position);
            obstacles[gridPos.x, gridPos.y, gridPos.z] = true;
        }*/
    }



    public void OccupyVolume(Vector3Int pos, GameObject drone)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    SetOccupancy(pos + new Vector3Int(i, j, k), drone);
                }
            }
        }
    }

    public bool IsVolumeAvailable(Vector3Int pos, GameObject drone)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    if (!IsAvailable(pos + new Vector3Int(i, j, k), drone))
                        return false;
                }
            }
        }

        return true;
    }

    public void SetOccupancy(int x, int y, int z, GameObject drone)
    {
        if ((x >= 0 && x < obstacles.GetLength(0))
            && (y >= 0 && y < obstacles.GetLength(1))
            && (z >= 0 && z < obstacles.GetLength(2)))
        {
            obstacles[x, y, z] = drone.GetInstanceID();
        }
    }

    public void SetOccupancy(Vector3Int pos, GameObject drone)
    {
        SetOccupancy(pos.x, pos.y, pos.z, drone);
    }

    public int GetGridPosition(float position, GridAxis axis)
    {
        float interval = GetSize(axis) / GetDivisions(axis);
        float relativePosition = position - GetBoundsMin(axis);
        return Mathf.FloorToInt(relativePosition / interval);
    }

    public Vector3Int GetGridPosition(Vector3 position)
    {
        int x = GetGridPosition(position.x, GridAxis.X);
        int y = GetGridPosition(position.y, GridAxis.Y);
        int z = GetGridPosition(position.z, GridAxis.Z);

        return new Vector3Int(x, y, z);
    }

    public bool IsAvailable(int x, int y, int z, GameObject drone)
    {
        return (x >= 0 && x < obstacles.GetLength(0))
            && (y >= 0 && y < obstacles.GetLength(1))
            && (z >= 0 && z < obstacles.GetLength(2))
            && (obstacles[x, y, z] == 0 || obstacles[x, y, z] == drone.GetInstanceID());
    }
    public bool IsAvailable(Vector3Int pos, GameObject drone)
    {
        return IsAvailable(pos.x, pos.y, pos.z, drone);
    }

    private float GetBoundsMin(GridAxis axis)
    {
        switch (axis)
        {
            case GridAxis.X:
                return bounds.min.x;
            case GridAxis.Y:
                return bounds.min.y;
            case GridAxis.Z:
                return bounds.min.z;
        }
        return float.MinValue;
    }

    private float GetSize(GridAxis axis)
    {
        switch (axis)
        {
            case GridAxis.X:
                return bounds.size.x;
            case GridAxis.Y:
                return bounds.size.y;
            case GridAxis.Z:
                return bounds.size.z;
        }
        return float.MinValue;
    }

    private int GetDivisions(GridAxis axis)
    {
        switch (axis)
        {
            case GridAxis.X:
                return divisions.x;
            case GridAxis.Y:
                return divisions.y;
            case GridAxis.Z:
                return divisions.z;
        }
        return int.MinValue;
    }

    public Vector3 GetMin(int i, int j, int k)
    {
        float x = intervals.x * i;
        float y = intervals.y * j;
        float z = intervals.z * k;

        return bounds.min + new Vector3(x, y, z);
    }

    public Vector3 GetMin(Vector3Int gridPos)
    {
        return GetMin(gridPos.x, gridPos.y, gridPos.z);
    }

    public Vector3 GetCenter(int i, int j, int k)
    {
        return GetMin(i, j, k) + (0.5f * intervals);
    }

    public Vector3 GetCenter(Vector3Int pos)
    {
        return GetCenter(pos.x, pos.y, pos.z);
    }

    public Vector3 GetRelative(Vector3 position)
    { 
        Vector3 min = GetMin(GetGridPosition(position));
        Vector3 offset = position - min;
        offset.Scale(new Vector3(1.0f / intervals.x, 1.0f / intervals.y, 1.0f / intervals.z));
        return offset;
    }

    public Vector3Int GetAdjacencyDirections(Vector3 position)
    {
        Vector3 relative = GetRelative(position);
        Vector3 direction = relative - (0.5f * Vector3.one);
        int dirX = Mathf.RoundToInt(direction.x / 0.5f);
        int dirY = Mathf.RoundToInt(direction.y / 0.5f);
        int dirZ = Mathf.RoundToInt(direction.z / 0.5f);

        Vector3Int result = new Vector3Int(dirX, dirY, dirZ);
        return result;
    }

    public void ClearOccupancy()
    {
        obstacles = new int[divisions.x, divisions.y, divisions.z];
    }

    public int TotalOccupancy
    {
        get
        {
            int total = 0;
            for (int i = 0; i < obstacles.GetLength(0); i++)
            {
                for (int j = 0; j < obstacles.GetLength(1); j++)
                {
                    for (int k = 0; k < obstacles.GetLength(2); k++)
                    {
                        if (obstacles[i, j, k] != 0)
                        {
                            total += 1;
                        }
                    }
                }
            }

            return total;
        }
    }

    public int Capacity
    {
        get
        {
            return obstacles.Length;
        }
    }

    public void Draw(bool drawInternal = false)
    {
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        //if (drawInternal)
        // DrawGrid();

        Color previousColor = Gizmos.color;

        for (int i = 0; i < obstacles.GetLength(0); i++)
        {
            for (int j = 0; j < obstacles.GetLength(1); j++)
            {
                for (int k = 0; k < obstacles.GetLength(2); k++) 
                { 
                    if (obstacles[i, j, k] != 0)
                    {
                        Gizmos.color = InstanceIdToColor(obstacles[i, j, k]);
                        Vector3 position = GetCenter(i, j, k);
                        Gizmos.DrawCube(position, intervals);
                    }
                }
            }
        }
        Gizmos.color = previousColor;
    }

    private Color InstanceIdToColor(int instanceID)
    {
        float hue = Mathf.Repeat((float)instanceID / 100.0f, 1.0f);
        Color baseColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
        return new Color(baseColor.r, baseColor.g, baseColor.b, 0.1f);
    }

    private void DrawGrid()
    {
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.025f);
        Gizmos.matrix = Matrix4x4.identity;

        float intervalX = bounds.size.x / divisions.x;
        float intervalY = bounds.size.y / divisions.y;
        float intervalZ = bounds.size.z / divisions.z;

        float startX = bounds.min.x;
        float startY = bounds.min.y;
        float startZ = bounds.min.z;

        float endX = bounds.max.x;
        float endY = bounds.max.y;
        float endZ = bounds.max.z;

        for (int i = 0; i <= divisions.y; i++)
        {
            float y = startY + (i * intervalY);
            for (int j = 0; j <= divisions.x; j++)
            {
                float x = startX + (j * intervalX);
                Gizmos.DrawLine(new Vector3(x, y, startZ), new Vector3(x, y, endZ));
            }

            for (int k = 0; k <= divisions.z; k++)
            {
                float z = startZ + (k * intervalZ);
                Gizmos.DrawLine(new Vector3(startX, y, z), new Vector3(endX, y, z));
            }
        }

        for (int i = 0; i <= divisions.z; i++)
        {
            float z = startZ + (i * intervalZ);
            for (int j = 0; j <= divisions.x; j++)
            {
                float x = startX + (j * intervalX);
                Gizmos.DrawLine(new Vector3(x, startY, z), new Vector3(x, endY, z));
            }
        }

        Gizmos.color = previousColor;
        Gizmos.matrix = previousMatrix;
    }

    private Vector3 ExpandMin(Vector3 min, Vector3 position)
    {
        min.x = Mathf.Min(min.x, position.x);
        min.y = Mathf.Min(min.y, position.y);
        min.z = Mathf.Min(min.z, position.z);
        return min;
    }

    private Vector3 ExpandMax(Vector3 max, Vector3 position)
    {
        max.x = Mathf.Max(max.x, position.x);
        max.y = Mathf.Max(max.y, position.y);
        max.z = Mathf.Max(max.z, position.z);
        return max;
    }
}