// RotatingSweeper.cs
// Horizontal or vertical bar that rotates around a central axis.
// Implements IPredictableHazard.

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(Rigidbody))]
    public class RotatingSweeper : HazardBase, IPredictableHazard
    {
        [Header("Rotating Sweeper")]
        [SerializeField, Tooltip("Rotation speed in degrees per second.")]
        private float degreesPerSecond = 90f;

        [SerializeField, Tooltip("Rotation axis in local space.")]
        private Vector3 axis = Vector3.up;

        [SerializeField, Tooltip("Phase offset in degrees (shifts start angle).")]
        private float phaseOffset = 0f;

        [SerializeField, Tooltip("Optional pause every N degrees (0 = no pause).")]
        [Range(0f, 360f)] private float pauseEveryDegrees = 0f;

        [SerializeField, Tooltip("How long to pause (seconds).")]
        [Range(0f, 2f)] private float pauseDuration = 0f;

        // The sweeper is safe when its bar is pointing along Z (perpendicular to runner lane).
        // We use angle to X-axis: safe when angle ≈ 0 or 180 (bar is across lanes, not along X).
        // "Danger zone": bar is sweeping through the lane (angle between 45 and 135, or 225-315 deg).
        [SerializeField, Tooltip("Angle range (centred on 0°) within which crossing is safe.")]
        [Range(5f, 90f)] private float safeAngleHalfWidth = 30f;

        private Rigidbody _rb;
        private Quaternion _initialRotation;
        private float _startTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic   = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _initialRotation  = transform.rotation;
            _startTime = Time.time;
        }

        private void FixedUpdate()
        {
            float angle = ComputeAngle(Time.time - _startTime);
            _rb.MoveRotation(_initialRotation * Quaternion.AngleAxis(angle, axis));
        }

        protected override void OnCollisionEnter(Collision col)
        {
            base.OnCollisionEnter(col);
        }

        // ── Deterministic angle ───────────────────────────────────────────
        private float ComputeAngle(float t)
        {
            if (pauseEveryDegrees <= 0f || pauseDuration <= 0f || Mathf.Approximately(degreesPerSecond, 0f))
            {
                return (phaseOffset + degreesPerSecond * t) % 360f;
            }

            float speed = Mathf.Abs(degreesPerSecond);
            float sign = Mathf.Sign(degreesPerSecond);
            float moveTime = pauseEveryDegrees / speed;
            float stepDuration = moveTime + pauseDuration;

            float completedSteps = Mathf.Floor(t / stepDuration);
            float stepTime = t - (completedSteps * stepDuration);
            float moveProgress = Mathf.Min(stepTime, moveTime);
            float angleMoved = (completedSteps * pauseEveryDegrees) + (moveProgress * speed);

            return (phaseOffset + sign * angleMoved) % 360f;
        }

        // ── IPredictableHazard ────────────────────────────────────────────
        private bool AngleIsSafe(float angle)
        {
            float a = angle % 360f;
            if (a < 0f) a += 360f;
            // Safe at 0 and 180 (bar faces across lanes), danger at 90 and 270
            float d0   = Mathf.Abs(Mathf.DeltaAngle(a, 0f));
            float d180 = Mathf.Abs(Mathf.DeltaAngle(a, 180f));
            return d0 <= safeAngleHalfWidth || d180 <= safeAngleHalfWidth;
        }

        public bool IsSafeAt(float xPosition, float timeFromNow, float crossDuration)
        {
            float now = Time.time;
            float step = 0.05f;
            for (float dt = timeFromNow; dt <= timeFromNow + crossDuration; dt += step)
                if (!AngleIsSafe(ComputeAngle(now + dt - _startTime))) return false;
            return true;
        }

        public float NextSafeWindow(float xPosition, float crossDuration, float searchHorizon = 10f)
        {
            float step     = 0.05f;
            float now      = Time.time;
            float safeStart = -1f;
            for (float t = 0f; t <= searchHorizon; t += step)
            {
                bool safe = AngleIsSafe(ComputeAngle(now + t - _startTime));
                if (safe && safeStart < 0f) safeStart = t;
                else if (!safe && safeStart >= 0f)
                {
                    if ((t - safeStart) >= crossDuration) return safeStart;
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
    }
}
