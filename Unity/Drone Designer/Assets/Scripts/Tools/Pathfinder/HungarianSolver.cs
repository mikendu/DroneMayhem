using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/*
public struct GridInfo
{
    public bool marked;
    public bool assigned;
}

public class GridMetadata
{
    private GridInfo[] rows;
    private GridInfo[] columns;
    public float Size { get; private set; }

    public GridMetadata(int size)
    {
        this.rows = new GridInfo[size];
        this.columns = new GridInfo[size];
        Size = size;
    }

    public bool IsMarked(int row, int column) { return rows[row].marked || columns[column].marked; }
    public bool IsAssigned(int row, int column) { return rows[row].assigned || columns[column].assigned; }
    public bool IsRowMarked(int row) { return rows[row].marked; }
    public bool IsColumnMarked(int column) { return columns[column].marked; }
    public bool IsRowAssigned(int row) { return rows[row].assigned; }
    public bool IsColumnAssigned(int column) { return columns[column].assigned; }
    public void MarkRow(int row) { rows[row].marked = true; }
    public void MarkColumn(int column) { columns[column].marked = true; }
    public void AssignRow(int row) { rows[row].assigned = true; }
    public void AssignColumn(int column) { columns[column].assigned = true; }

    public void Assign(int row, int column)
    {
        AssignRow(row);
        AssignColumn(column);
    }

    public int TotalMarkerCount()
    {
        return rows.Count(x => x.marked) + columns.Count(x => x.marked);
    }
}
*/

public static class HungarianSolver
{

    public static List<Tuple<int, int>> FindAssignments(List<GameObject> drones, List<Vector3> targets)
    {
        int count = drones.Count;
        int[,] costMatrix = new int[count, count];

        for (int i = 0; i < count; i++)
        {
            GameObject drone = drones[i];
            for (int j = 0; j < count; j++)
            {
                Vector3 target = targets[j];
                costMatrix[i, j] = Mathf.RoundToInt(1000.0f * Vector3.Distance(drone.transform.position, target));
            }
        }

        int[] assignments = HungarianAlgorithm.HungarianAlgorithm.FindAssignments(costMatrix);
        List<Tuple<int, int>> result = new List<Tuple<int, int>>();
        for(int i = 0; i < assignments.Length; i++)
        {
            result.Add(new Tuple<int, int>(i, assignments[i]));
        }

        // PrintAssignments(result);
        return result;
    }
    /*

    public static List<Tuple<int, int>> FindAssignments(int count, List<GameObject> drones, List<Vector3> targets)
    {
        //int count = drones.Count;
        float[,] matrix = new float[count, count];
        GridMetadata gridMetadata = new GridMetadata(count);


        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                matrix[i, j] = 25 + (Mathf.RoundToInt(UnityEngine.Random.value * 300));
            }
        }

        // Get all distances
        // Rows = targets
        // Columns = drones

        for (int i = 0; i < count; i++)
        {
            Vector3 target = targets[i];
            for (int j = 0; j < count; j++)
            {
                GameObject drone = drones[j];
                matrix[i, j] = Vector3.Distance(target, drone.transform.position);
            }
        }

        PrintMatrix(matrix);

        // Step 1
        for (int i = 0; i < count; i++)
        {
            float min = MinFromRow(matrix, i);
            for (int j = 0; j < count; j++)
                matrix[i, j] -= min;
        }

        PrintMatrix(matrix);

        // Step 2
        for (int i = 0; i < count; i++)
        {
            float min = MinFromCol(matrix, i);
            for (int j = 0; j < count; j++)
                matrix[j, i] -= min;
        }

        PrintMatrix(matrix);

        // Step 3
        MarkZeros(matrix, gridMetadata);
        PrintMarked(matrix, gridMetadata);

        int markerCount = gridMetadata.TotalMarkerCount();
        int failsafe = 0;
        while (markerCount < count && failsafe < 1500)
        {
            float min = MinUnmarkedValue(matrix, gridMetadata);

            // Subtract from unmarked rows
            for (int i = 0; i < count; i++)
            {
                if (!gridMetadata.IsRowMarked(i))
                    AddToRow(matrix, i, -min);
            }
            PrintMatrix(matrix);

            // Add to marked columns
            for (int i = 0; i < count; i++)
            {
                if (gridMetadata.IsColumnMarked(i))
                    AddToCol(matrix, i, min);
            }
            PrintMatrix(matrix);


            MarkZeros(matrix, gridMetadata);
            PrintMarked(matrix, gridMetadata);
            markerCount = gridMetadata.TotalMarkerCount();
            Debug.Log("Marker count: " + markerCount);

            failsafe += 1;
        }

        // Get Assignments
        List<Tuple<int, int>> assignments = new List<Tuple<int, int>>();
        failsafe = 0;
        while (assignments.Count < count && failsafe < 2500)
        {
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    float value = matrix[i, j];
                    if (gridMetadata.IsAssigned(i, j))
                        continue;

                    if (!Mathf.Approximately(value, 0.0f))
                        continue;

                    int rowCount = ZeroCountRow(matrix, i, gridMetadata, true);
                    int colCount = ZeroCountCol(matrix, j, gridMetadata, true);
                    if (rowCount == 1 || colCount == 1)
                    {
                        assignments.Add(new Tuple<int, int>(i, j));
                        gridMetadata.Assign(i, j);
                    }
                }
            }
            failsafe += 1;
        }

        PrintAssignments(assignments);
        return assignments;
    }


    private static void MarkZeros(float[,] matrix, GridMetadata gridMetadata)
    {
        int count = matrix.GetLength(0);
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                float value = matrix[i, j];
                if (!Mathf.Approximately(value, 0.0f))
                    continue;

                int rowCount = ZeroCountRow(matrix, i, gridMetadata);
                int colCount = ZeroCountCol(matrix, j, gridMetadata);
                bool marked = gridMetadata.IsMarked(i, j);

                if (!marked && (rowCount > 0 || colCount > 0))
                {
                    if (rowCount >= colCount)
                        gridMetadata.MarkRow(i);
                    else
                        gridMetadata.MarkColumn(j);
                }
            }
        }
    }



    private static float MinUnmarkedValue(float[,] matrix, GridMetadata gridMetadata)
    {
        int count = matrix.GetLength(0);
        float min = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                bool marked = gridMetadata.IsMarked(i, j);
                if (marked)
                    continue;

                min = Mathf.Min(min, matrix[i, j]);
            }
        }

        if (min >= float.MaxValue)
            throw new IndexOutOfRangeException("Did not find any unmarked values!!");

        return min;
    }


    private static void AddToRow(float[,] matrix, int row, float value)
    {
        if (row < 0 || row >= matrix.GetLength(0))
            throw new InvalidOperationException("Row index out of bounds");

        for (int i = 0; i < matrix.GetLength(1); i++)
            matrix[row, i] += value;

    }

    private static void AddToCol(float[,] matrix, int col, float value)
    {
        if (col < 0 || col >= matrix.GetLength(1))
            throw new InvalidOperationException("Col index out of bounds");

        for (int i = 0; i < matrix.GetLength(0); i++)
            matrix[i, col] += value;

    }


    private static float MinFromRow(float[,] matrix, int row)
    {
        if (row < 0 || row >= matrix.GetLength(0))
            throw new InvalidOperationException("Row index out of bounds");

        float min = float.MaxValue;
        for (int i = 0; i < matrix.GetLength(1); i++)
            min = Mathf.Min(min, matrix[row, i]);

        return min;
    }

    private static float MinFromCol(float[,] matrix, int col)
    {
        if (col < 0 || col >= matrix.GetLength(1))
            throw new InvalidOperationException("Col index out of bounds");

        float min = float.MaxValue;
        for (int i = 0; i < matrix.GetLength(0); i++)
            min = Mathf.Min(min, matrix[i, col]);

        return min;
    }

    private static int ZeroCountRow(float[,] matrix, int row, GridMetadata gridMetadata, bool ignoreAssigned = false)
    {
        if (row < 0 || row >= matrix.GetLength(0))
            throw new InvalidOperationException("Row index out of bounds");

        int count = 0;
        for (int i = 0; i < matrix.GetLength(1); i++)
        {
            if (gridMetadata.IsAssigned(row, i))
                continue;

            if (Mathf.Approximately(matrix[row, i], 0.0f))
                count += 1;
        }
        return count;
    }
    private static int ZeroCountCol(float[,] matrix, int col, GridMetadata gridMetadata, bool ignoreAssigned = false)
    {
        if (col < 0 || col >= matrix.GetLength(1))
            throw new InvalidOperationException("Col index out of bounds");

        int count = 0;
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            if (gridMetadata.IsAssigned(i, col))
                continue;

            if (Mathf.Approximately(matrix[i, col], 0.0f))
                count += 1;
        }

        return count;
    }

    private static void PrintMatrix(float[,] matrix)
    {
        int count = matrix.GetLength(0);
        string message = "";
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                message += ("" + matrix[i, j].ToString("0.##") + "\t");
            }
            message += "\n";
        }

        Debug.Log("Matrix:\n\n" + message);
    }


    private static void PrintMarked(float[,] matrix, GridMetadata gridMetadata)
    {
        int count = matrix.GetLength(0);
        string message = "";
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                bool marked = gridMetadata.IsMarked(i, j);
                string value = (marked) ? "XXX" : ("" + matrix[i, j].ToString("0.##"));
                message += (value + "\t");
            }
            message += "\n";
        }

        Debug.Log("Marked Entries:\n\n" + message);
    }*/

    private static void PrintAssignments(List<Tuple<int, int>> assignments)
    {
        string message = "Assignments: { " +
            String.Join(", ", assignments.Select(x => x.Item1 + " => " + x.Item2).ToArray()) + " }\n";

        List<int> rows = assignments.Select(x => x.Item1).ToList();
        List<int> columns = assignments.Select(x => x.Item2).ToList();

        rows.Sort();
        columns.Sort();

        message += "\n - Rows: [ " + String.Join(", ", rows.ToArray()) + " ]";
        message += "\n - Cols: [ " + String.Join(", ", columns.ToArray()) + " ]\n\n";
        Debug.Log(message);
    }

}