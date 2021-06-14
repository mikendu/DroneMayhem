using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class VoxelNode
{
    public VoxelNode parent;
    public HashSet<VoxelNode> children = new HashSet<VoxelNode>();

    public int depth;
    public Vector3Int gridPosition;
    public Vector3 min, max;
    public bool filled;
}