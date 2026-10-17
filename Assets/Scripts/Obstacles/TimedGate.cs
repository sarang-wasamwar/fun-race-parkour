// TimedGate.cs
// Opens and closes on a configurable period. Closed = solid wall (not a kill zone).
// Implements IPredictableHazard.

using UnityEngine;

namespace FunRaceParkour
{
    public class TimedGate : MonoBehaviour, IResettableObstacle, IPredictableHazard
    {
        [Header("Timed Gate")]
        [SerializeField, Range(0.2f, 10f)] private float openTime   = 2f;
        [SerializeField, Range(0.2f, 10f)] private float closedTime = 2f;
        [SerializeField] private float phaseOffset = 0f;

        [SerializeField] private GameObject gateGeometry;  // the wall object
        [SerializeField] private Collider   gateCollider;

        [SerializeField] private Color openColor   = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color closedColor = new Color(1f, 0f, 0f, 0.8f);

        private Renderer _renderer;
        private bool _isOpen;

        private void Awake()
        {
            if (gateGeometry == null) gateGeometry = gameObject;
            if (gateCollider == null) gateCollider = GetComponent<Collider>();
            _renderer = gateGeometry.GetComponent<Renderer>();
        }

        private void Update()
        {
            bool shouldBeOpen = GateIsOpen(Time.time);
            if (shouldBeOpen != _isOpen) SetOpen(shouldBeOpen);
        }

        private bool GateIsOpen(float t)
        {
            float period = openTime + closedTime;
            float phase  = ((t + phaseOffset) % period);
            return phase < openTime;
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (gateCollider != null) gateCollider.enabled = !open;
            if (gateGeometry != null) gateGeometry.SetActive(!open || _renderer != null); // keep visible but non-solid
            if (_renderer   != null) _renderer.material.color = open ? openColor : closedColor;
        }

        // ── IPredictableHazard ────────────────────────────────────────────
        public bool IsSafeAt(float xPosition, float timeFromNow, float crossDuration)
        {
            float now  = Time.time;
            float step = 0.05f;
            for (float dt = timeFromNow; dt <= timeFromNow + crossDuration; dt += step)
                if (!GateIsOpen(now + dt)) return false;
            return true;
        }

        public float NextSafeWindow(float xPosition, float crossDuration, float searchHorizon = 10f)
        {
            float step      = 0.05f;
            float now       = Time.time;
            float safeStart = -1f;
            for (float t = 0f; t <= searchHorizon; t += step)
            {
                bool safe = GateIsOpen(now + t);
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
        public void ResetState()
        {
            _isOpen = false;
            SetOpen(GateIsOpen(Time.time));
        }
    }
}
