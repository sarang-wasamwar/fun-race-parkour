// HazardBase.cs
// Base class for all hazards. Handles collision detection, impact delivery,
// warning visuals, and IResettableObstacle. Never instant-kills or teleports.

using UnityEngine;

namespace FunRaceParkour
{
    public enum HazardType { Knockback, Stun, FallThrough }

    public abstract class HazardBase : MonoBehaviour, IResettableObstacle
    {
        [Header("Hazard Settings")]
        [SerializeField, Tooltip("What happens when a racer is hit.")]
        protected HazardType hazardType = HazardType.Knockback;

        [SerializeField, Tooltip("Impulse applied on hit (X = lateral, Y = upward pop).")]
        protected Vector3 impactImpulse = new Vector3(3f, 3f, 0f);

        [SerializeField, Tooltip("Seconds the racer's input is suppressed after impact.")]
        [Range(0f, 2f)] protected float stunDuration = 0.3f;

        [Header("Warning Visual")]
        [SerializeField, Tooltip("Optional renderer that flashes warning colour.")]
        protected Renderer warningRenderer;

        [SerializeField] protected Color warningColor  = new Color(1f, 0.5f, 0f);
        [SerializeField] protected Color normalColor   = Color.white;
        [SerializeField, Range(0.1f, 2f)]
        protected float warningFlashHz = 2f;

        protected bool _isFlashing;
        private float _flashTimer;
        private bool  _flashState;

        // ── Contact detection (CharacterController) ────────────────────────
        // CharacterController does not fire OnCollisionEnter. We use OnControllerColliderHit
        // on the racer side, OR a trigger on the hazard side (whichever fits the geometry).
        // HazardBase uses a trigger by default. Override for kinematic-body sweepers.

        protected virtual void OnTriggerEnter(Collider other)
        {
            TryHitRacer(other.gameObject, impactImpulse);
        }

        protected void TryHitRacer(GameObject go, Vector3 impulse)
        {
            var receiver = go.GetComponent<IHazardReceiver>();
            if (receiver == null) return;
            receiver.ApplyImpact(impulse, stunDuration);
        }

        // ── Warning flash ─────────────────────────────────────────────────
        protected void StartFlash() => _isFlashing = true;
        protected void StopFlash()
        {
            _isFlashing = false;
            if (warningRenderer != null) warningRenderer.material.color = normalColor;
        }

        protected virtual void Update()
        {
            if (!_isFlashing || warningRenderer == null) return;
            _flashTimer += Time.deltaTime;
            if (_flashTimer >= 1f / warningFlashHz)
            {
                _flashState = !_flashState;
                warningRenderer.material.color = _flashState ? warningColor : normalColor;
                _flashTimer = 0f;
            }
        }

        // ── IResettableObstacle ───────────────────────────────────────────
        public abstract void ResetState();
    }
}
