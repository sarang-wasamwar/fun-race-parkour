
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector3 movementDirection = Vector3.right;
    public float movementDistance = 1f;
    public float speed = 2f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 previousPosition;

    void Start()
    {
        startPosition = transform.position;

        movementDirection.Normalize();
        endPosition = startPosition
                    + movementDirection * movementDistance;

        previousPosition = transform.position;
    }

    void Update()
    {
        // Move toward the opposite endpoint
        float distanceToEnd =
            Vector3.Distance(transform.position, endPosition);

        Vector3 target = distanceToEnd < 0.05f
                       ? startPosition
                       : endPosition;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        // Calculate actual movement speed in units/second
        float currentSpeed =
            Vector3.Distance(transform.position, previousPosition)
            / Time.deltaTime;

        previousPosition = transform.position;

        Debug.Log("Platform Speed: " + currentSpeed.ToString("F2")
                  + " units/sec");
    }
}