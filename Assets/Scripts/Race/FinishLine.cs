// FinishLine.cs
// Trigger at the end of the course. First crossing per racer records finish time.

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(Collider))]
    public class FinishLine : MonoBehaviour
    {
        private void Reset() => GetComponent<Collider>().isTrigger = true;
        private void Awake()  => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            var rm = RaceManager.Instance;
            if (rm == null) return;
            rm.OnRacerFinished(other.gameObject);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.84f, 0f, 0.5f);
            Gizmos.DrawCube(transform.position, new Vector3(8f, 3f, 0.3f));
        }
    }
}
