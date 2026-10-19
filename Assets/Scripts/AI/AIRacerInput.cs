// AIRacerInput.cs
// IRacerInput implementation for AI racers.
// Uses sensor + prediction approach. Never changes speed, jump height or gravity.
// All hazard prediction uses IPredictableHazard (same information the human sees).
// AI may only move forward/back and jump.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(CharacterController))]
    public class AIRacerInput : MonoBehaviour, IRacerInput
    {
        // ── Profile ────────────────────────────────────────────────────────
        [SerializeField, Tooltip("Personality profile (reaction time, error ranges, etc.)")]
        private AIRacerProfile profile;

        public void SetProfile(AIRacerProfile value) => profile = value;

        // ── IRacerInput ────────────────────────────────────────────────────
        public float Move       { get; private set; }
        public bool  Jump       { get; private set; }
        public bool  InputLocked
        {
            get => _inputLocked;
            set { _inputLocked = value; if (value) { Move = 0f; Jump = false; } }
        }

        public void ConsumeJump() => Jump = false;

        // ── Runtime state ──────────────────────────────────────────────────
        private ThirdPersonControllerExtended _ext;
        private StarterAssets.ThirdPersonController _tpc;
        private bool _inputLocked;

        private float MotorSpeed => _ext != null ? _ext.MoveSpeed : (_tpc != null ? _tpc.MoveSpeed : 2f);
        private float MotorJump => _ext != null ? _ext.JumpHeight : (_tpc != null ? _tpc.JumpHeight : 1.2f);
        private float MotorGravity => _ext != null ? _ext.Gravity : (_tpc != null ? _tpc.Gravity : -15f);
        private bool MotorGrounded => _ext != null ? _ext.Grounded : (_tpc != null && _tpc.Grounded);
        private LayerMask MotorGround => _ext != null ? _ext.GroundLayers : (_tpc != null ? _tpc.GroundLayers : ~0);
        private float MotorPathStartX => _ext != null ? _ext.PathStartX : (_tpc != null ? _tpc.PathStartX : 0f);
        private float MotorPathEndX => _ext != null ? _ext.PathEndX : (_tpc != null ? _tpc.PathEndX : 1f);

        // Navigation state machine
        private enum NavState { MovingForward, WaitingForHazard, JumpApproach, Jumping, StuckRecovery }
        private NavState _navState = NavState.MovingForward;

        // Physics constants (read from controller at start — same as player)
        private float _moveSpeed;
        private float _jumpHeight;
        private float _gravity;

        // Derived jump math (cached, recomputed if controller values change)
        private float _jumpDuration;    // total air time
        private float _jumpReach;       // horizontal distance covered during jump

        // Sensors
        private float          _groundCheckDist   = 0.6f;   // gap detection ahead
        private float          _hazardCheckRadius  = 6f;    // sphere for nearby hazards
        private Collider[]     _overlapBuffer      = new Collider[16];

        // Stuck detection
        private float _lastX;
        private float _stuckTimer;
        private int   _stuckJumpCount;

        // Hazard wait
        private IPredictableHazard _currentHazard;
        private float              _hazardX;
        private float              _crossDuration;
        private float              _waitTimer;

        // Jump approach
        private float _jumpTriggerX;

        // ── Gap validator math (also used by editor validator) ─────────────
        /// <summary>
        /// Compute the safe maximum jump gap in X given controller settings.
        /// reach = MoveSpeed × jumpDuration × (1 - safetyMargin)
        /// </summary>
        public static float ComputeJumpReach(float moveSpeed, float jumpHeight, float gravity, float safetyMargin)
        {
            // time to peak: v0 = sqrt(h*-2*g), tPeak = v0 / |g|
            float v0       = Mathf.Sqrt(jumpHeight * -2f * gravity);
            float tPeak    = v0 / Mathf.Abs(gravity);
            float duration = tPeak * 2f;
            return moveSpeed * duration * (1f - safetyMargin);
        }

        public static float ComputeJumpDuration(float jumpHeight, float gravity)
        {
            float v0    = Mathf.Sqrt(jumpHeight * -2f * gravity);
            float tPeak = v0 / Mathf.Abs(gravity);
            return tPeak * 2f;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _ext = GetComponent<ThirdPersonControllerExtended>();
            _tpc = GetComponent<StarterAssets.ThirdPersonController>();
        }

        private void Start()
        {
            CachePhysicsConstants();
            _lastX = transform.position.x;

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AIRacerProfile>();
                Debug.LogWarning($"[AIRacerInput] No profile on {name}. Using defaults.");
            }
        }

        private void CachePhysicsConstants()
        {
            _moveSpeed  = MotorSpeed;
            _jumpHeight = MotorJump;
            _gravity    = MotorGravity;

            _jumpDuration = ComputeJumpDuration(_jumpHeight, _gravity);
            _jumpReach    = _moveSpeed * _jumpDuration * (1f - (profile != null ? profile.safetyMargin : 0.15f));
        }

        // ── Update ─────────────────────────────────────────────────────────
        private void Update()
        {
            if (_inputLocked) { Move = 0f; Jump = false; return; }

            // Re-cache if controller values have changed
            if (!Mathf.Approximately(MotorSpeed, _moveSpeed) ||
                !Mathf.Approximately(MotorJump, _jumpHeight))
                CachePhysicsConstants();

            DetectStuck();
            RunNavState();
        }

        // ── Navigation state machine ───────────────────────────────────────
        private void RunNavState()
        {
            switch (_navState)
            {
                case NavState.MovingForward:
                    Move = 1f;
                    Jump = false;
                    CheckForHazardsAhead();
                    CheckForGapsAhead();
                    break;

                case NavState.WaitingForHazard:
                    Move = 0f;
                    Jump = false;
                    WaitForHazardWindow();
                    break;

                case NavState.JumpApproach:
                    Move = 1f;
                    Jump = false;
                    CheckJumpTrigger();
                    break;

                case NavState.Jumping:
                    Move = 1f;
                    Jump = true; // held until consumed by controller
                    // Transition back once in the air
                    if (!MotorGrounded) _navState = NavState.MovingForward;
                    break;

                case NavState.StuckRecovery:
                    // Try jumping first, then back-step
                    RunStuckRecovery();
                    break;
            }
        }

        // ── Sensors ────────────────────────────────────────────────────────
        private void CheckForHazardsAhead()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _hazardCheckRadius,
                _overlapBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var hazard = _overlapBuffer[i].GetComponentInParent<IPredictableHazard>();
                if (hazard == null) continue;

                float hx = _overlapBuffer[i].transform.position.x;
                // Only care about hazards ahead of us
                if (hx <= transform.position.x) continue;

                // Estimate cross time (how long to walk through the hazard)
                float crossDist   = _overlapBuffer[i].bounds.size.x + 0.5f;
                float crossDur    = crossDist / _moveSpeed;
                float windowMul = profile != null
                    ? Mathf.Lerp(profile.safeWindowMultiplier, 1.1f, profile.riskTolerance)
                    : 1.5f;
                float requiredDur = crossDur * windowMul;
                float distanceToHazard = hx - transform.position.x;
                float timeToArrive     = distanceToHazard / _moveSpeed;

                if (!hazard.IsSafeAt(hx, timeToArrive + (profile?.reactionTime ?? 0.15f), requiredDur))
                {
                    // Stop before the hazard at a standoff distance
                    _currentHazard = hazard;
                    _hazardX       = hx - _overlapBuffer[i].bounds.extents.x - 0.3f;
                    _crossDuration = requiredDur;
                    _navState      = NavState.WaitingForHazard;
                    return;
                }
            }
        }

        private void CheckForGapsAhead()
        {
            // Forward raycast to detect gap (no ground ahead)
            float checkStep = 0.5f;
            float checkDist = _jumpReach * 1.5f;
            float direction = MotorPathEndX > MotorPathStartX ? 1f : -1f;

            Vector3 startPos = transform.position + Vector3.right * direction * 0.5f;
            for (float d = checkStep; d <= checkDist; d += checkStep)
            {
                Vector3 checkPos = startPos + Vector3.right * direction * d;
                bool hasGround = Physics.Raycast(checkPos + Vector3.up * 0.2f, Vector3.down,
                    _groundCheckDist + 0.5f, MotorGround, QueryTriggerInteraction.Ignore);

                if (!hasGround)
                {
                    // Gap found at distance d. Plan jump.
                    // Trigger jump when we are (jumpReach - safetyBuffer) before the edge.
                    float edgeX   = transform.position.x + direction * d;
                    float errorMod = profile != null ? Random.Range(0f, profile.jumpTimingError) : 0.05f;
                    _jumpTriggerX = edgeX - direction * (_jumpReach - errorMod);
                    _navState     = NavState.JumpApproach;
                    return;
                }
            }
        }

        private void CheckJumpTrigger()
        {
            float direction = MotorPathEndX > MotorPathStartX ? 1f : -1f;
            float dx = (transform.position.x - _jumpTriggerX) * direction;
            if (dx >= 0f)
            {
                // We've reached the trigger point — jump!
                _navState = NavState.Jumping;
            }
        }

        private void WaitForHazardWindow()
        {
            // Walk up to the standoff X
            float direction = MotorPathEndX > MotorPathStartX ? 1f : -1f;
            float distToHazardEdge = (_hazardX - transform.position.x) * direction;
            if (distToHazardEdge > 0.3f)
            {
                Move = 1f;
                return;
            }

            Move = 0f;

            // Poll every pollInterval
            _waitTimer -= Time.deltaTime;
            if (_waitTimer > 0f) return;
            _waitTimer = profile?.pollInterval ?? 0.1f;

            if (_currentHazard == null) { _navState = NavState.MovingForward; return; }

            float reactionDelay = profile?.reactionTime ?? 0.15f;
            float window = _currentHazard.NextSafeWindow(_hazardX, _crossDuration);
            if (window >= 0f && window <= reactionDelay)
            {
                // Safe window starting now (within reaction time)
                _navState = NavState.MovingForward;
                _currentHazard = null;
            }
        }

        // ── Stuck detection ────────────────────────────────────────────────
        private void DetectStuck()
        {
            if (_navState == NavState.WaitingForHazard)
            {
                _stuckTimer = 0f;
                _lastX = transform.position.x;
                return;
            }

            float dx = Mathf.Abs(transform.position.x - _lastX);
            if (dx < 0.02f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > (profile?.stuckTimeout ?? 3f) && _navState != NavState.StuckRecovery)
                {
                    _navState      = NavState.StuckRecovery;
                    _stuckJumpCount = 0;
                }
            }
            else
            {
                _stuckTimer     = 0f;
                _stuckJumpCount = 0;
                if (_navState == NavState.StuckRecovery) _navState = NavState.MovingForward;
            }
            _lastX = transform.position.x;
        }

        private float _recoveryTimer;
        private bool  _backingUp;

        private void RunStuckRecovery()
        {
            _recoveryTimer -= Time.deltaTime;

            if (!_backingUp)
            {
                // Try a jump
                if (MotorGrounded && _stuckJumpCount < (profile?.maxJumpRetries ?? 2))
                {
                    Jump = true;
                    _stuckJumpCount++;
                    _recoveryTimer = 0.5f;
                }
                else if (_recoveryTimer <= 0f)
                {
                    // Back up briefly
                    _backingUp     = true;
                    _recoveryTimer = profile != null ? profile.recoveryDelay : 0.8f;
                }
                Move = 1f;
            }
            else
            {
                Move = -1f; // back up
                Jump = false;
                if (_recoveryTimer <= 0f)
                {
                    _backingUp      = false;
                    _stuckTimer     = 0f;
                    _stuckJumpCount = 0;
                    _navState       = NavState.MovingForward;
                }
            }
        }
    }
}
