// ThirdPersonControllerExtended.cs
// Drop-in replacement for the original ThirdPersonController that adds:
//   • IRacerInput seam (PlayerRacerInput or AIRacerInput)
//   • IHazardReceiver impulse support
//   • Public API for RaceManager (ResetForRace, SetVerticalVelocity, ApplyImpact)
//   • Expose pathStartX / pathEndX for progress calculation
//
// The original ThirdPersonController is KEPT unchanged in its namespace.
// This script lives in a separate namespace and file so Unity treats them
// independently. AI prefab variants do NOT have PlayerInput and reference
// this component.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;

namespace FunRaceParkour
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonControllerExtended : MonoBehaviour, IHazardReceiver
    {
        // ── Path ───────────────────────────────────────────────────────────
        [Header("Bresenham Path")]
        [Tooltip("Start marker set by RaceManager at spawn time.")]
        public Transform startPoint;
        [Tooltip("End marker (finish line direction).")]
        public Transform endPoint;

        // ── Movement ───────────────────────────────────────────────────────
        [Header("Movement")]
        [Tooltip("Normal move speed in m/s — same value for player and AI.")]
        public float MoveSpeed = 2.0f;
        [Tooltip("Acceleration / deceleration rate.")]
        public float SpeedChangeRate = 10.0f;

        // ── Jump / Gravity ─────────────────────────────────────────────────
        [Header("Jump & Gravity")]
        public float JumpHeight   = 1.2f;
        public float Gravity      = -15.0f;
        public float JumpTimeout  = 0.50f;
        public float FallTimeout  = 0.15f;

        // ── Grounded ───────────────────────────────────────────────────────
        [Header("Grounded")]
        public bool      Grounded        = true;
        public float     GroundedOffset  = -0.14f;
        public float     GroundedRadius  = 0.28f;
        public LayerMask GroundLayers;

        // ── Audio / Anim ───────────────────────────────────────────────────
        [Header("Audio")]
        public AudioClip   LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        // ── Cinemachine (player only — not used by AI) ─────────────────────
        [Header("Cinemachine (Player Only)")]
        public GameObject CinemachineCameraTarget;
        public float      TopClamp           = 70.0f;
        public float      BottomClamp        = -30.0f;
        public float      CameraAngleOverride = 12.0f;
        public bool       LockCameraPosition = true;
        public float      FixedCameraYaw     = 90.0f;
        public Vector2    LookSensitivity    = new Vector2(7.5f, 5.0f);

        // ── Runtime ────────────────────────────────────────────────────────
        private float _pathStartX;
        private float _pathEndX;
        private float _pathZ;
        private float _pathY;

        private float _speed;
        private float _animationBlend;
        private float _verticalVelocity;
        private float _terminalVelocity = 53f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // impulse from hazards
        private float _impulseX;
        private float _stunTimer;

        // camera state
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private Vector3    _cameraStartPos;
        private Quaternion _cameraStartRot;
        public  bool IsRespawning { get; set; }

        // anim IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private bool _hasAnimator;
        private Animator         _animator;
        private CharacterController _controller;

        // Input seam
        private IRacerInput _inputSeam;      // set by RaceManager or Awake
        private bool _isAI;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        // Fallback StarterAssetsInputs (human path without IRacerInput)
        private StarterAssetsInputs _legacyInput;

        // ── Public accessors ───────────────────────────────────────────────
        public float PathStartX => _pathStartX;
        public float PathEndX   => _pathEndX;
        public float PathZ      => _pathZ;
        public float VerticalVelocity => _verticalVelocity;

        private const float _threshold = 0.01f;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            _isAI       = !TryGetComponent<PlayerRacerInput>(out _);
            _inputSeam  = GetComponent<IRacerInput>() as MonoBehaviour as IRacerInput;
            if (_inputSeam == null)
                _inputSeam  = GetComponent<MonoBehaviour>() as IRacerInput; // fallback search

            _legacyInput = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller  = GetComponent<CharacterController>();

            AssignAnimationIDs();

            if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
                _cameraStartPos       = CinemachineCameraTarget.transform.position;
                _cameraStartRot       = CinemachineCameraTarget.transform.rotation;
            }

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            if (startPoint != null && endPoint != null)
                InitPath();

            float lookScale = PlayerPrefs.GetFloat("FRP_Sensitivity", LookSensitivity.x);
            LookSensitivity = new Vector2(lookScale, lookScale * 0.67f);
        }

        private void InitPath()
        {
            _pathStartX = startPoint.position.x;
            _pathEndX   = endPoint.position.x;
            _pathZ      = transform.position.z;
            _pathY      = transform.position.y;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            if (!_isAI)
                CameraRotation();
        }

        // ── Input helpers ──────────────────────────────────────────────────
        private float GetMoveInput()
        {
            if (_inputSeam != null) return _inputSeam.Move;
            if (_legacyInput != null) return _legacyInput.move.y;
            return 0f;
        }

        private bool GetJumpInput()
        {
            if (_inputSeam != null) return _inputSeam.Jump;
            if (_legacyInput != null) return _legacyInput.jump;
            return false;
        }

        private void ConsumeJump()
        {
            if (_inputSeam != null)  { _inputSeam.ConsumeJump(); return; }
            if (_legacyInput != null) _legacyInput.jump = false;
        }

        private bool IsLocked()
        {
            if (_inputSeam != null) return _inputSeam.InputLocked;
            return false;
        }

        // ── Movement ───────────────────────────────────────────────────────
        private void Move()
        {
            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                // During stun, apply impulse X only (no player control)
                float impulseMove = _impulseX * Time.deltaTime;
                _impulseX = Mathf.MoveTowards(_impulseX, 0f, 20f * Time.deltaTime);

                _controller.Move(
                    new Vector3(impulseMove, 0f, 0f)
                    + Vector3.up * _verticalVelocity * Time.deltaTime);

                // Z correction
                Vector3 p = transform.position; p.z = _pathZ; transform.position = p;
                return;
            }

            float input = IsLocked() ? 0f : GetMoveInput();

            float targetSpeed = Mathf.Abs(input) > 0.01f ? MoveSpeed : 0f;
            _speed = Mathf.MoveTowards(_speed, targetSpeed, SpeedChangeRate * Time.deltaTime);
            _animationBlend = _speed;

            float minX = Mathf.Min(_pathStartX, _pathEndX);
            float maxX = Mathf.Max(_pathStartX, _pathEndX);
            float direction = Mathf.Sign(_pathEndX - _pathStartX);
            if (Mathf.Approximately(direction, 0f)) direction = 1f;

            float nextX = transform.position.x;
            if (Mathf.Abs(input) > 0.01f)
            {
                nextX += input * direction * _speed * Time.deltaTime;
                nextX = Mathf.Clamp(nextX, minX, maxX);
                transform.rotation = Quaternion.LookRotation(Vector3.right * input * direction);
            }

            float horizontalDelta = nextX - transform.position.x;
            _controller.Move(
                new Vector3(horizontalDelta, 0f, 0f)
                + Vector3.up * _verticalVelocity * Time.deltaTime);

            // Z lock
            Vector3 cp = transform.position; cp.z = _pathZ; transform.position = cp;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, Mathf.Abs(input));
            }
        }

        // ── Jump & Gravity ─────────────────────────────────────────────────
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;

                bool jumpRequested = GetJumpInput();
                if (jumpRequested && _jumpTimeoutDelta <= 0f && _stunTimer <= 0f && !IsLocked())
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator) _animator.SetBool(_animIDJump, true);
                    ConsumeJump();
                }
                if (_jumpTimeoutDelta >= 0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_fallTimeoutDelta >= 0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                else
                    if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);

                ConsumeJump(); // clear any jump input while airborne
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        // ── Grounded ───────────────────────────────────────────────────────
        private void GroundedCheck()
        {
            Vector3 sphere = new Vector3(transform.position.x,
                                         transform.position.y - GroundedOffset,
                                         transform.position.z);
            Grounded = Physics.CheckSphere(sphere, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        // ── Camera (human only) ─────────────────────────────────────────────
        private bool IsCurrentDeviceMouse
        {
#if ENABLE_INPUT_SYSTEM
            get => _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
            get => false;
#endif
        }

        private void CameraRotation()
        {
            if (CinemachineCameraTarget == null) return;

            if (IsRespawning)
            {
                _cinemachineTargetYaw   = FixedCameraYaw;
                _cinemachineTargetPitch = 0f;
                CinemachineCameraTarget.transform.position = _cameraStartPos;
                CinemachineCameraTarget.transform.rotation = _cameraStartRot;
                IsRespawning = false;
                return;
            }

            if (!LockCameraPosition && _legacyInput != null && _legacyInput.look.sqrMagnitude >= _threshold)
            {
                float dtMul = IsCurrentDeviceMouse ? 1f : Time.deltaTime;
                _cinemachineTargetYaw   += _legacyInput.look.x * dtMul * LookSensitivity.x;
                _cinemachineTargetPitch += _legacyInput.look.y * dtMul * LookSensitivity.y;
            }
            else if (LockCameraPosition)
            {
                _cinemachineTargetYaw = FixedCameraYaw;
                _cinemachineTargetPitch = 0f;
            }

            _cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw,   float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0f);
        }

        public void ResetCameraRotation(float targetYaw = 90f)
        {
            _cinemachineTargetYaw   = targetYaw;
            _cinemachineTargetPitch = 0f;
            if (CinemachineCameraTarget != null)
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        }

        // ── Public Race API ────────────────────────────────────────────────
        /// <summary>Teleport to pos, reset velocity and timers. Called by RaceManager.</summary>
        public void ResetForRace(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.position  = position;
            transform.rotation  = rotation;
            _controller.enabled = true;

            _verticalVelocity = 0f;
            _speed            = 0f;
            _animationBlend   = 0f;
            _impulseX         = 0f;
            _stunTimer        = 0f;
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
                _animator.SetBool(_animIDGrounded, true);
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
                _animator.Play("Grounded", 0, 0f);
            }

            if (!_isAI) ResetCameraRotation(90f);

            // Recalculate path in case Z changed (multi-lane)
            if (startPoint != null && endPoint != null) InitPath();
        }

        public void SetVerticalVelocity(float v) => _verticalVelocity = v;
        public void SetInputSeam(IRacerInput seam) => _inputSeam = seam;

        // ── IHazardReceiver ────────────────────────────────────────────────
        public void ApplyImpact(Vector3 impulse, float stunSeconds)
        {
            // Only X component (Z is locked, Y goes through verticalVelocity)
            _impulseX         = impulse.x;
            _verticalVelocity = Mathf.Max(_verticalVelocity, impulse.y);
            _stunTimer        = Mathf.Max(_stunTimer, stunSeconds);
        }

        // ── Animation events ───────────────────────────────────────────────
        private void AssignAnimationIDs()
        {
            _animIDSpeed       = Animator.StringToHash("Speed");
            _animIDGrounded    = Animator.StringToHash("Grounded");
            _animIDJump        = Animator.StringToHash("Jump");
            _animIDFreeFall    = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                var idx = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[idx],
                    transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
                AudioSource.PlayClipAtPoint(LandingAudioClip,
                    transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }

        // ── Gizmos ────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Grounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        private static float ClampAngle(float a, float min, float max)
        {
            if (a < -360f) a += 360f;
            if (a >  360f) a -= 360f;
            return Mathf.Clamp(a, min, max);
        }
    }
}
