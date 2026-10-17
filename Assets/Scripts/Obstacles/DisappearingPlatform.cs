// DisappearingPlatform.cs
// Stays solid for stayTime after first contact, then disappears for goneTime, then reappears.
// Contact detected via OnControllerColliderHit (fired on racers) → racer calls Register().
// Implements IPredictableHazard.

using System.Collections.Generic;
using UnityEngine;

namespace FunRaceParkour
{
    public class DisappearingPlatform : MonoBehaviour, IResettableObstacle, IPredictableHazard
    {
        [Header("Disappearing Platform")]
        [SerializeField, Range(0.3f, 10f)] private float stayTime  = 1.5f;
        [SerializeField, Range(0.3f, 10f)] private float goneTime  = 2.0f;
        [SerializeField, Range(0.1f, 2f)]  private float warnTime  = 0.4f; // flash before vanish

        [SerializeField] private Renderer platformRenderer;
        [SerializeField] private Collider platformCollider;
        [SerializeField] private Color    warningColor = new Color(1f, 0.3f, 0f);
        [SerializeField] private Color    normalColor  = Color.white;
        [SerializeField] private Color    hiddenColor  = new Color(0.2f, 0.2f, 0.2f, 0.3f);

        private enum PlatformState { Solid, Warning, Gone }
        private PlatformState _state = PlatformState.Solid;
        private float         _timer;
        private float         _firstContactTime = -1f;
        private bool          _hasContact;

        // Track which racers are currently standing on us
        private readonly HashSet<GameObject> _standingRacers = new();

        private void Awake()
        {
            if (platformRenderer == null) platformRenderer = GetComponent<Renderer>();
            if (platformCollider == null) platformCollider = GetComponent<Collider>();
        }

        // Called by PlatformContactReporter on the racer (via OnControllerColliderHit)
        public void RegisterContact(GameObject racer)
        {
            if (racer != null) _standingRacers.Add(racer);
            if (_state == PlatformState.Solid && !_hasContact)
            {
                _hasContact       = true;
                _firstContactTime = Time.time;
                _timer            = stayTime;
            }
        }

        public void RemoveContact(GameObject racer)
        {
            if (racer != null) _standingRacers.Remove(racer);
        }

        private void Update()
        {
            if (!_hasContact && _state == PlatformState.Solid) return;

            _timer -= Time.deltaTime;

            switch (_state)
            {
                case PlatformState.Solid:
                    if (_timer <= warnTime)
                    {
                        _state = PlatformState.Warning;
                        _timer = warnTime;
                        if (platformRenderer != null)
                            platformRenderer.material.color = warningColor;
                    }
                    break;

                case PlatformState.Warning:
                    // Flash warning
                    float flash = Mathf.Sin(Time.time * Mathf.PI * 8f) > 0f ? 1f : 0f;
                    if (platformRenderer != null)
                        platformRenderer.material.color = Color.Lerp(warningColor, normalColor, flash);

                    if (_timer <= 0f)
                    {
                        _state = PlatformState.Gone;
                        _timer = goneTime;
                        SetSolid(false);
                    }
                    break;

                case PlatformState.Gone:
                    if (_timer <= 0f)
                    {
                        _state      = PlatformState.Solid;
                        _hasContact = false;
                        _timer      = 0f;
                        SetSolid(true);
                    }
                    break;
            }
        }

        private void SetSolid(bool solid)
        {
            if (platformCollider != null) platformCollider.enabled = solid;
            if (platformRenderer != null)
                platformRenderer.material.color = solid ? normalColor : hiddenColor;
        }

        // ── IPredictableHazard ────────────────────────────────────────────
        // Platform is "unsafe" when it is Gone or about to go.
        // For AI: "is it safe to step on at timeFromNow for crossDuration?"
        public bool IsSafeAt(float xPosition, float timeFromNow, float crossDuration)
        {
            if (!_hasContact && _state == PlatformState.Solid) return true; // pristine

            float now  = Time.time;
            float when = now + timeFromNow;

            if (_state == PlatformState.Gone)
            {
                float reappearAt = now + _timer;
                // Safe only if the platform reappears before we arrive AND stays up long enough
                float availableTime = stayTime - warnTime;
                return reappearAt <= when && availableTime >= crossDuration;
            }

            if (_state == PlatformState.Solid)
            {
                float timeLeft = _timer - warnTime;
                // Safe if we can cross before it vanishes
                return timeFromNow + crossDuration <= timeLeft;
            }

            return false; // Warning state = about to vanish
        }

        public float NextSafeWindow(float xPosition, float crossDuration, float searchHorizon = 10f)
        {
            if (!_hasContact && _state == PlatformState.Solid) return 0f;

            float now = Time.time;

            if (_state == PlatformState.Gone)
            {
                float reappearIn  = _timer;
                float availableSafe = stayTime - warnTime;
                if (availableSafe >= crossDuration) return reappearIn;
                return -1f; // never safe long enough with one step
            }

            if (_state == PlatformState.Solid)
            {
                float safeRemaining = _timer - warnTime;
                if (safeRemaining >= crossDuration) return 0f; // safe now
                // Wait for next cycle
                float nextCycle = _timer + goneTime;
                float nextSafe  = stayTime - warnTime;
                if (nextSafe >= crossDuration) return nextCycle;
            }

            return -1f;
        }

        // ── IResettableObstacle ───────────────────────────────────────────
        public void ResetState()
        {
            _state            = PlatformState.Solid;
            _hasContact       = false;
            _firstContactTime = -1f;
            _timer            = 0f;
            _standingRacers.Clear();
            SetSolid(true);
        }
    }
}
