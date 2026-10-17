// RaceCheckpoint.cs
// Placed along the track. Each racer records the last checkpoint it crossed.
// Racer respawns at that position, not the start.

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(Collider))]
    public class RaceCheckpoint : MonoBehaviour
    {
        [Tooltip("Sequential index (0 = first checkpoint after start).")]
        public int checkpointIndex;

        [Tooltip("Respawn position for a racer that activates this checkpoint.")]
        public Transform respawnPoint;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var rm = RaceManager.Instance;
            if (rm == null) return;
            var data = rm.GetRacerData(other.gameObject);
            if (data == null) return;
            if (data.checkpointIndex >= checkpointIndex) return; // already passed

            data.checkpointIndex  = checkpointIndex;
            data.lastCheckpointPos = respawnPoint != null ? respawnPoint.position : transform.position;
            data.lastCheckpointRot = respawnPoint != null ? respawnPoint.rotation : transform.rotation;

            rm.OnCheckpointReached(data, checkpointIndex);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawCube(transform.position, new Vector3(6f, 2f, 0.2f));
        }
    }
}
