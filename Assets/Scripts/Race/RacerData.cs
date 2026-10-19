// RacerData.cs
// Per-racer runtime record managed by RaceManager.

using UnityEngine;

namespace FunRaceParkour
{
    public enum RacerStatus { Racing, Finished, DNF }

    [System.Serializable]
    public class RacerData
    {
        [Tooltip("Display name shown in HUD and results.")]
        public string name;

        [Tooltip("Team colour for material and label.")]
        public Color color;

        /// <summary>Reference to the racer's GameObject in the scene.</summary>
        [System.NonSerialized] public GameObject racerObject;

        /// <summary>Extended controller when present (AI variant).</summary>
        [System.NonSerialized] public ThirdPersonControllerExtended controller;

        /// <summary>Original Starter Assets controller when present (human default).</summary>
        [System.NonSerialized] public StarterAssets.ThirdPersonController starterController;

        [System.NonSerialized] public IRacerInput inputSource;

        // ── progress ──────────────────────────────────────────────────────
        [System.NonSerialized] public int    checkpointIndex = -1;  // last checkpoint reached
        [System.NonSerialized] public float  progress;              // 0-1 (X distance / total)
        [System.NonSerialized] public int    falls;
        [System.NonSerialized] public int    respawns;
        [System.NonSerialized] public RacerStatus status = RacerStatus.Racing;

        // ── timing ────────────────────────────────────────────────────────
        [System.NonSerialized] public float  elapsedTime;     // counts up while Racing
        [System.NonSerialized] public float  finishTime = -1; // only set when finish trigger crossed
        [System.NonSerialized] public int    finishOrder = int.MaxValue;
        [System.NonSerialized] public int    finalRank;       // set after race concludes

        // ── spawn ─────────────────────────────────────────────────────────
        [System.NonSerialized] public Vector3    spawnPosition;
        [System.NonSerialized] public Quaternion spawnRotation;

        // ── checkpoint respawn ────────────────────────────────────────────
        [System.NonSerialized] public Vector3    lastCheckpointPos;
        [System.NonSerialized] public Quaternion lastCheckpointRot;

        public void ResetForRace()
        {
            checkpointIndex  = -1;
            progress         = 0f;
            falls            = 0;
            respawns         = 0;
            status           = RacerStatus.Racing;
            elapsedTime      = 0f;
            finishTime       = -1f;
            finishOrder      = int.MaxValue;
            finalRank        = 0;
            lastCheckpointPos = spawnPosition;
            lastCheckpointRot = spawnRotation;
        }
    }
}
