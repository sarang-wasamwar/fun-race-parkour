// SwingingHammer.cs
// Pivot-based swinging hammer. Kinematic rigidbody, moved in FixedUpdate.
// Implements IPredictableHazard so AI can predict safe crossing windows.
// Swing is a pure function of Time.fixedTime so AI and human see the same cycle.

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(Rigidbody))]
    public class SwingingHammer : HazardBase, IPredictableHazard
    {
        [Header("Swinging Hammer")]
        [SerializeField, Tooltip("Maximum angle from rest position in degrees.")]
        [Range(10f, 90f)] private float maxAngle = 60f;

        [SerializeField, Tooltip("Full oscillation time in seconds (period).")]
        [Range(0.5f, 10f)] private float period = 3f;

        [SerializeField, Tooltip("Phase offset in seconds (shifts timing).")]
        private float phaseOffset = 0f;

        [SerializeField, Tooltip("Pause duration at each swing end (seconds).")]
        [Range(0f, 1f)] private float pauseAtEnds = 0.0f;

        [SerializeField, Tooltip("Axis around which the pivot rotates (local space).")]
        private Vector3 swingAxis = Vector3.forward;

        // ── Runtime ────────────────────────────────────────────────────────
        private Rigidbody _rb;
        private Quaternion _initialRotation;
        private float _startTime;

        // Danger arc: pivot angle range considered dangerous (degrees from neutral)
        [SerializeField, Tooltip("Hammer is dangerous when |angle| > this threshold (degrees).")]
        [Range(0f, 90f)] private float dangerThreshold = 20f;

        // ── Hit children ──────────────────────────────────────────────────
        // The hammer head might not be the pivot. Assign the head collider here.
        [SerializeField, Tooltip("Collider on the hammer head (not the pivot). If null, uses first child collider.")]
        private Collider hammerHeadCollider;

        protected override void OnTriggerEnter(Collider other) => TryHitRacer(other.gameObject, impactImpulse);

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _initialRotation = transform.rotation;
            _startTime = 0f; // will sync to time base in FixedUpdate
        }

        private void Start()
        {
            _startTime = Time.time;
            if (hammerHeadCollider == null)
                hammerHeadCollider = GetComponentInChildren<Collider>();
        }

        private void FixedUpdate()
        {
            float angle = ComputeAngle(Time.fixedTime);
            _rb.MoveRotation(_initialRotation * Quaternion.AngleAxis(angle, swingAxis));
        }

        private void OnCollisionEnter(Collision col)
        {
            TryHitRacer(col.gameObject, impactImpulse);
        }

        // ── Angle function (deterministic) ────────────────────────────────
        private float ComputeAngle(float t)
        {
            float phase = ((t + phaseOffset) % period) / period; // [0,1]

            if (pauseAtEnds > 0f)
            {
                float halfPause = pauseAtEnds / period;
                float activePhase = 1f - 2f * halfPause;
                if (phase < halfPause)                           return  maxAngle;
                if (phase > 1f - halfPause)                      return -maxAngle;
                float t2 = (phase - halfPause) / activePhase;   // [0,1] in active window
                return maxAngle * Mathf.Cos(t2 * Mathf.PI);
            }

            return maxAngle * Mathf.Cos(phase * Mathf.PI * 2f);
        }

        // ── IPredictableHazard ────────────────────────────────────────────
        public bool IsSafeAt(float xPosition, float timeFromNow, float crossDuration)
        {
            // Sample the angle at regular intervals over [now+timeFromNow, now+timeFromNow+crossDuration]
            float startT  = Time.time + timeFromNow;
            float endT    = startT + crossDuration;
            float step    = 0.05f;
            for (float t = startT; t <= endT; t += step)
            {
                float angle = Mathf.Abs(ComputeAngle(t));
                if (angle > dangerThreshold) return false;
            }
            return true;
        }

        public float NextSafeWindow(float xPosition, float crossDuration, float searchHorizon = 10f)
        {
            float step      = 0.05f;
            float safeStart = -1f;
            float now       = Time.time;

            for (float t = 0f; t <= searchHorizon; t += step)
            {
                bool safe = Mathf.Abs(ComputeAngle(now + t)) <= dangerThreshold;
                if (safe && safeStart < 0f) safeStart = t;
                else if (!safe && safeStart >= 0f)
                {
                    float windowLength = t - safeStart;
                    if (windowLength >= crossDuration) return safeStart;
                    safeStart = -1f;
                }
            }
            if (safeStart >= 0f && (searchHorizon - safeStart) >= crossDuration) return safeStart;
            return -1f;
        }

        // ── IResettableObstacle ───────────────────────────────────────────
        public override void ResetState()
        {
            _startTime = Time.time;
            if (_rb != null) _rb.MoveRotation(_initialRotation);
        }

        // ── Gizmo ─────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position,
                transform.position + Quaternion.AngleAxis(maxAngle, swingAxis) * transform.up * 2f);
            Gizmos.DrawLine(transform.position,
                transform.position + Quaternion.AngleAxis(-maxAngle, swingAxis) * transform.up * 2f);
        }
    }
}
