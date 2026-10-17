// MovingPlatformExtended.cs
// Replaces/extends Assets/MovingPlatform.cs with:
//   • Rider-carrying via PlatformRider helper (delta applied to CharacterController)
//   • Deterministic position for IPredictableHazard
//   • Per-frame debug log REMOVED
//   • IResettableObstacle support

using System.Collections.Generic;
using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovingPlatformExtended : MonoBehaviour, IResettableObstacle
    {
        [Header("Movement")]
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField, Range(0.5f, 20f)] private float speed = 2f;
        [SerializeField, Range(0f, 2f)] private float pauseAtEnds = 0.3f;

        private Rigidbody  _rb;
        private Vector3    _startA;
        private Vector3    _startB;
        private float      _period;
        private Vector3    _previousPos;

        // Riders: CharacterControllers standing on this platform
        private readonly List<CharacterController> _riders = new();

        // Expose delta for PlatformRider
        public Vector3 FrameDelta { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic   = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Start()
        {
            if (pointA == null || pointB == null)
            {
                // Fall back to local offsets
                _startA = transform.position;
                _startB = transform.position + Vector3.right * 3f;
            }
            else
            {
                _startA = pointA.position;
                _startB = pointB.position;
            }

            float dist = Vector3.Distance(_startA, _startB);
            _period = dist > 0f ? (dist / speed) * 2f + pauseAtEnds * 2f : 4f;
            _previousPos = _startA;
        }

        private void FixedUpdate()
        {
            Vector3 newPos = ComputePosition(Time.fixedTime);
            FrameDelta     = newPos - _previousPos;
            _rb.MovePosition(newPos);
            _previousPos = newPos;

            // Apply delta to all riders
            foreach (var cc in _riders)
            {
                if (cc != null && cc.enabled)
                    cc.Move(FrameDelta);
            }
        }

        // ── Deterministic position ────────────────────────────────────────
        private Vector3 ComputePosition(float t)
        {
            if (_period <= 0f) return _startA;
            float phase     = (t % _period) / _period;
            float pauseFrac = _period > 0f ? pauseAtEnds / _period : 0f;
            float halfPause = pauseFrac * 0.5f;
            float active    = (1f - 2f * halfPause) * 0.5f;

            float t01;
            if (phase < halfPause)              return _startA;
            if (phase < 0.5f)                  { t01 = (phase - halfPause) / active; return Vector3.Lerp(_startA, _startB, t01); }
            if (phase < 0.5f + halfPause)      return _startB;
            t01 = (phase - 0.5f - halfPause) / active;
            return Vector3.Lerp(_startB, _startA, t01);
        }

        // ── Rider registration ─────────────────────────────────────────────
        public void AddRider(CharacterController cc)
        {
            if (!_riders.Contains(cc)) _riders.Add(cc);
        }

        public void RemoveRider(CharacterController cc)
        {
            _riders.Remove(cc);
        }

        // ── IResettableObstacle ───────────────────────────────────────────
        public void ResetState()
        {
            _riders.Clear();
            FrameDelta   = Vector3.zero;
            _previousPos = _startA;
            if (_rb != null) _rb.MovePosition(_startA);
        }

        private void OnDrawGizmosSelected()
        {
            if (pointA != null && pointB != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pointA.position, pointB.position);
            }
        }
    }
}
