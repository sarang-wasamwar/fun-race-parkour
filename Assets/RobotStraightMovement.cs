using UnityEngine;

public class RobotStraightMovement : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        // Move forward along the robot's local forward direction
        transform.Translate(
            Vector3.forward * speed * Time.deltaTime,
            Space.Self
        );
    }
}
