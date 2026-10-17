// MovingBlock.cs
// A solid obstacle that moves back and forth between two endpoints.
// Implements IPredictableHazard. Does NOT carry riders (solid wall only).

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovingBlock : HazardBase, IPredictableHazard
    {
        [Header("Moving Block")]
        [SerializeField, Tooltip("First endpoint of the patrol path.")] private Transform pointA;
        [SerializeField, Tooltip("Second endpoint of the patrol path.")] private Transform pointB;

        [SerializeField, Range(0.5f, 20f), Tooltip("Patrol speed in units per second.")]
        private float speed = 3f;

        [SerializeField, Range(0f, 2f), Tooltip("Pause at each end in seconds.")]
        private float pauseAtEnds = 0f;

        private Rigidbody  _rb;
        private Vector3    _startA;
        private Vector3    _startB;
        private float      _period;     // time for one full A→B→A cycle
        private Vector3    _initialPos;
        private float      _startTime;
        private bool       _initialised;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic   = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _initialPos       = transform.position;
            _startTime        = Time.time;
        }

        private void Start()
        {
            if (pointA == null || pointB == null)
            {
                Debug.LogWarning($"[MovingBlock] {name}: pointA or pointB not assigned.", this);
                enabled = false; return;
            }
            _startA = pointA.position;
            _startB = pointB.position;
            _initialised = true;
            float dist = Vector3.Distance(_startA, _startB);
            if (dist > 0f)
                _period = (dist / speed) * 2f + pauseAtEnds * 2f;
        }

        private void FixedUpdate()
        {
            _rb.MovePosition(ComputePosition(Time.time - _startTime));
        }

        protected override void OnCollisionEnter(Collision col)
        {
            base.OnCollisionEnter(col);
        }

        // ── Deterministic position ────────────────────────────────────────
        private Vector3 ComputePosition(float t)
        {
            if (_period <= 0f) return _startA;
            float phase = (t % _period) / _period; // [0,1]

            float pauseFrac = _period > 0f ? pauseAtEnds / _period : 0f;
            float halfPause = pauseFrac * 0.5f;
            float active    = (1f - 2f * halfPause) * 0.5f; // quarter of cycle (A→B)

            // Phase layout:
            // [0, halfPause)       = at A
            // [halfPause, 0.5)     = moving A→B
            // [0.5, 0.5+halfPause) = at B
            // [0.5+halfPause, 1)   = moving B→A
            float t01;
            if (phase < halfPause)                           return _startA;
            if (phase < 0.5f)                               { t01 = (phase - halfPause) / active; return Vector3.Lerp(_startA, _startB, t01); }
            if (phase < 0.5f + halfPause)                   return _startB;
            t01 = (phase - 0.5f - halfPause) / active;
            return Vector3.Lerp(_startB, _startA, t01);
        }

        private bool IsAtDangerX(float blockX, float targetX)
        {
            // Block is dangerous when its centre is within blockWidth of the racer's X
            float blockWidth = 0.8f;
            return Mathf.Abs(blockX - targetX) < blockWidth;
        }

        // ── IPredictableHazard ────────────────────────────────────────────
        public bool IsSafeAt(float xPosition, float timeFromNow, float crossDuration)
        {
            float now = Time.time;
            float step = 0.05f;
            for (float dt = timeFromNow; dt <= timeFromNow + crossDuration; dt += step)
            {
                Vector3 pos = ComputePosition(now + dt - _startTime);
                if (IsAtDangerX(pos.x, xPosition)) return false;
            }
            return true;
        }

        public float NextSafeWindow(float xPosition, float crossDuration, float searchHorizon = 10f)
        {
            float step      = 0.05f;
            float now       = Time.time;
            float safeStart = -1f;
            for (float t = 0f; t <= searchHorizon; t += step)
            {
                Vector3 pos = ComputePosition(now + t - _startTime);
                bool safe   = !IsAtDangerX(pos.x, xPosition);
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
            if (!_initialised) return;
            _startTime = Time.time;
            if (_rb != null) _rb.MovePosition(_startA);
        }
    }
}
