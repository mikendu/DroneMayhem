using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class SparseVoxelOctree
{
    private int divisions;
    private Bounds bounds;

    public float Spacing { get; private set; }
    public Vector3 CellSize { get; private set; }

    private VoxelNode root;
    private HashSet<ulong> filledLeaves;
    private List<Bounds> obstacles;

    public SparseVoxelOctree(float size, float targetGridSize)
    {
        uint targetDivisions = (uint)Mathf.CeilToInt(size / targetGridSize);
        divisions = (int)Morton.nextPowerOfTwo(targetDivisions);
        bounds = new Bounds(new Vector3(0, size / 2.0f, 0), size * Vector3.one);
        
        Spacing = size / divisions;
        CellSize = new Vector3(Spacing, Spacing, Spacing);
    }

    public void SetObstacles(List<Bounds> obstacles)
    {
        filledLeaves = new HashSet<ulong>();
        this.obstacles = obstacles;

        foreach(Bounds bounds in obstacles)
        {
            Vector3Int min = GetGridPosition(bounds.min);
            Vector3Int max = GetGridPosition(bounds.max);

            for (int i = min.x; i <= max.x; i++)
            {
                for (int j = min.y; j <= max.y; j++)
                {
                    for (int k = min.z; k <= max.z; k++)
                    {
                        //voxelGrid[i, j, k] = true;
                        filledLeaves.Add(Morton.encode(i, j, k));
                    }
                }
            }
        }
    }

    public void Compute()
    {
        ulong maxMorton = Morton.encode(divisions, divisions, divisions);
        int maxDepth = Mathf.RoundToInt(Mathf.Log(divisions, 2));

        List<List<VoxelNode>> queues = new List<List<VoxelNode>>();
        for(int i = 0; i < maxDepth; i++)
            queues.Add(new List<VoxelNode>());

        for(ulong i = 0; i < maxMorton; i++)
        {
            int currentDepth = maxDepth - 1;
            VoxelNode leaf = new VoxelNode();
            leaf.gridPosition = Morton.decode(i);
            leaf.filled = filledLeaves.Contains(i);
            leaf.depth = currentDepth;
            //

        }
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

    public void DrawObstacles()
    {
        Color previousColor = Gizmos.color;
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.25f);

        foreach (Bounds bounds in obstacles)
        {
            Vector3Int min = GetGridPosition(bounds.min);
            Vector3Int max = GetGridPosition(bounds.max);

            for (int i = min.x; i <= max.x; i++)
            {
                for (int j = min.y; j <= max.y; j++)
                {
                    for (int k = min.z; k <= max.z; k++)
                    {
                        Vector3 position = GetCenter(new Vector3Int(i, j, k));
                        Gizmos.DrawCube(position, CellSize);
                    }
                }
            }
        }
        Gizmos.color = previousColor;

    }


    public static SparseVoxelOctree Generate(List<Bounds> obstacles, float sideLength, float targetGridSize = 0.1f)
    {
        SparseVoxelOctree octree = new SparseVoxelOctree(sideLength, targetGridSize);
        octree.SetObstacles(obstacles);
        octree.Compute();
        return octree;
    }

}