
using UnityEngine;
using System.Collections.Generic;

public class MovingPlatform : MonoBehaviour, FunRaceParkour.IResettableObstacle
{
    [SerializeField, Tooltip("Direction the platform travels in local world space.")]
    private Vector3 movementDirection = Vector3.right;
    [SerializeField, Tooltip("Distance travelled from the starting point.")]
    private float movementDistance = 1f;
    [SerializeField, Tooltip("Platform travel speed in units per second.")]
    private float speed = 2f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 previousPosition;
    private readonly List<CharacterController> riders = new List<CharacterController>();

    public void AddRider(CharacterController rider)
    {
        if (!riders.Contains(rider)) riders.Add(rider);
    }

    public void RemoveRider(CharacterController rider)
    {
        riders.Remove(rider);
    }

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

        Vector3 frameDelta = transform.position - previousPosition;
        foreach (CharacterController rider in riders)
        {
            if (rider != null && rider.enabled)
                rider.Move(frameDelta);
        }

        previousPosition = transform.position;
    }

    public void ResetState()
    {
        riders.Clear();
        transform.position = startPosition;
        previousPosition = startPosition;
    }
}
