using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;



public enum ArrangmentRowType
{
    DEPTH, HEIGHT
}

public static class DroneArranger
{
    public const float DRONE_RADIUS = 0.15f;

    public static void Shuffle(List<GameObject> drones)
    {
        Quaternion rotation = Quaternion.Euler(-90, 0, 0);
        foreach (GameObject drone in drones)
        {
            Vector3 position = rotation * Random.insideUnitCircle;
            position.y = 0.25f;
            drone.transform.position = position;
        }

        float minDistance = 1.9f * DRONE_RADIUS;
        float offset = 0.0f;
        do
        {
            offset = Offset(drones);
        } while (offset < minDistance);

    }

    public static void LineUp(List<GameObject> drones, int rows, ArrangmentRowType rowStyle)
    {
        int dronesPerRow = Mathf.CeilToInt((float)drones.Count / rows);

        float interval = (2.0f * DRONE_RADIUS);
        float startZ = -0.75f;
        float startY = 0.25f;

        for (int i = 0; i < drones.Count; i++)
        {
            int row = i / dronesPerRow;
            int col = i % dronesPerRow;
            int numDronesInRow = Math.Min(dronesPerRow, drones.Count - (row * dronesPerRow));
            float totalLength = (numDronesInRow - 1) * interval;
            float startX = -totalLength / 2.0f;

            GameObject drone = drones[i];
            float x = startX + (col * interval);
            float y = startY + (row * 0.25f);
            float z = startZ - (row * interval);
            Vector3 position = Vector3.zero;
            if (rowStyle == ArrangmentRowType.HEIGHT)
                position = new Vector3(x, y, startZ);
            else
                position = new Vector4(x, startY, z);
            drone.transform.position = position;
        }
    }


    public static float Offset(List<GameObject> drones)
    {
        float minDistanceFound = float.MaxValue;
        float minDistance = 2.0f * DRONE_RADIUS;
        for (int i = 0; i < drones.Count - 1; i++)
        {
            GameObject drone1 = drones[i];
            for (int j = i + 1; j < drones.Count; j++)
            {
                GameObject drone2 = drones[j];
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

    private static void OffsetDrone(GameObject drone, Vector3 centroid)
    {
        Vector3 diff = drone.transform.position - centroid;
        Vector3 direction = diff.normalized;
        float offsetAmt = (DRONE_RADIUS - diff.magnitude);
        drone.transform.position += (offsetAmt * direction);
    }

}