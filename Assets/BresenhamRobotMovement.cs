
using UnityEngine;
using System.Collections.Generic;

public class BresenhamRobotMovement : MonoBehaviour
{
    [Header("Bresenham Path")]
    public Transform startPoint;
    public Transform endPoint;
    public float gridSize = 0.25f;

    [Header("Movement")]
    public float speed = 3f;

    private List<Vector3> path = new List<Vector3>();
    private float fixedY;
    private float fixedZ;

    void Start()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("Assign PathStart and PathEnd in Inspector!");
            enabled = false;
            return;
        }

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;

        // The road runs along X. Keep Y and Z fixed.
        fixedY = start.y;
        fixedZ = start.z;

        int steps = Mathf.RoundToInt(
            Mathf.Abs(end.x - start.x) / gridSize
        );

        int direction = end.x >= start.x ? 1 : -1;

        // Bresenham's horizontal line points
        for (int i = 0; i <= steps; i++)
        {
            float x = start.x + direction * i * gridSize;

            path.Add(new Vector3(x, fixedY, fixedZ));
        }

        if (path.Count == 0)
            return;

        // Start at PathStart
        transform.position = path[0];
    }

    void Update()
    {
        float input = 0f;

        if (Input.GetKey(KeyCode.W))
            input = 1f;
        else if (Input.GetKey(KeyCode.S))
            input = -1f;

        if (input == 0f || path.Count == 0)
            return;

        // Find the robot's nearest point along the line.
        float startX = path[0].x;
        float endX = path[path.Count - 1].x;

        float nextX = transform.position.x
                    + input * speed * Time.deltaTime;

        // Never travel beyond the endpoints.
        nextX = Mathf.Clamp(nextX,
            Mathf.Min(startX, endX),
            Mathf.Max(startX, endX));

        transform.position = new Vector3(nextX, fixedY, fixedZ);
    }

    void OnDrawGizmos()
    {
        if (path == null || path.Count < 2)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < path.Count - 1; i++)
            Gizmos.DrawLine(path[i], path[i + 1]);
    }
}
