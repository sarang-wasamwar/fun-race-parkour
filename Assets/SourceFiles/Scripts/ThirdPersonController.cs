 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif
using FunRaceParkour;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour, IHazardReceiver
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Header("Bresenham Path")]
        public Transform startPoint;
        public Transform endPoint;
        public float gridSize = 0.25f;

        private float pathStartX;
        private float pathEndX;
        private float pathY;
        private float pathZ;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 12.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = true;

        [Tooltip("Fixed camera yaw angle along the track (90 for +X)")]
        public float FixedCameraYaw = 90.0f;

        public Vector2  LookSensitivity = new Vector2(7.5f, 5.0f);

        // cinemachine
        private float _cinemachineTargetYaw = 90.0f;
        private float _cinemachineTargetPitch = 0.0f;

        // Camera starting position and rotation
        private Vector3 _cameraStartingPosition;
        private Quaternion _cameraStartingRotation;

        // Variable to indicate if we are resetting the camera 
        public bool IsRespawning { get; set; } = false;

        // hazard knockback & stun
        private float _impulseX;
        private float _stunTimer;

        // player
        private float _speed;
        private float _animationBlend;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private IRacerInput _racerInput;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        public float PathStartX => pathStartX;
        public float PathEndX => pathEndX;
        public float PathZ => pathZ;
        public void SetRacerInput(IRacerInput inputSource) => _racerInput = inputSource;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            // Ensure PlatformRider is attached so moving platforms carry the character
            if (!TryGetComponent<PlatformRider>(out _))
            {
                gameObject.AddComponent<PlatformRider>();
            }

            _racerInput = GetComponent<IRacerInput>();
        }

        private void Start()
{
    _cinemachineTargetYaw = FixedCameraYaw;
    _cinemachineTargetPitch = 0.0f;

    _hasAnimator = TryGetComponent(out _animator);
    _controller = GetComponent<CharacterController>();
    _input = GetComponent<StarterAssetsInputs>();
    if (_input != null && LockCameraPosition)
    {
        _input.cursorInputForLook = false;
    }
#if ENABLE_INPUT_SYSTEM 
    _playerInput = GetComponent<PlayerInput>();
#else
	Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
    
    AssignAnimationIDs();

    // Save the starting camera position and rotation locked to track direction (+X)
    if (CinemachineCameraTarget != null)
    {
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(CameraAngleOverride, FixedCameraYaw, 0f);
        _cameraStartingPosition = CinemachineCameraTarget.transform.position;
        _cameraStartingRotation = CinemachineCameraTarget.transform.rotation;
    }

    // reset our timeouts on start
    _jumpTimeoutDelta = JumpTimeout;
    _fallTimeoutDelta = FallTimeout;


    if (startPoint == null || endPoint == null)
    {
        Debug.LogError("Assign PathStart and PathEnd!");
        enabled = false;
        return;
    }

    pathStartX = startPoint.position.x;
    pathEndX = endPoint.position.x;
    pathY = transform.position.y;
    // Keep the spawned lane (RaceManager) rather than forcing startPoint.z.
    pathZ = transform.position.z;

    if (gridSize <= 0f)
    {
        Debug.LogError("Grid Size must be greater than zero.");
        enabled = false;
        return;
    }

    // Tutorial / non-race placement: snap onto the path. Race spawns already have a lane.
    if (_racerInput == null)
    {
        pathZ = startPoint.position.z;
        _controller.enabled = false;
        transform.position = new Vector3(pathStartX, pathY, pathZ);
        _controller.enabled = true;
    }

    float lookScale = PlayerPrefs.GetFloat("FRP_Sensitivity", LookSensitivity.x);
    LookSensitivity = new Vector2(lookScale, lookScale * 0.67f);
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
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
{
    if (CinemachineCameraTarget == null) return;
    if (IsLocked()) return;

    // if respawning, reset to starting position and rotation
    if (IsRespawning)
    {
        _cinemachineTargetYaw = FixedCameraYaw;
        _cinemachineTargetPitch = 0f;

        CinemachineCameraTarget.transform.position = _cameraStartingPosition;
        CinemachineCameraTarget.transform.rotation = _cameraStartingRotation;

        IsRespawning = false;
        return;
    }

    // When camera is locked behind player, do NOT allow mouse-look to rotate the angle
    if (!LockCameraPosition && _input != null && _input.look.sqrMagnitude >= _threshold)
    {
        float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
        _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * LookSensitivity.x;
        _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * LookSensitivity.y;
    }
    else if (LockCameraPosition)
    {
        _cinemachineTargetYaw = FixedCameraYaw;
        _cinemachineTargetPitch = 0.0f;
    }

    _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
    _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

    CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
        _cinemachineTargetPitch + CameraAngleOverride,
        _cinemachineTargetYaw,
        0.0f
    );
}

        //private void Move()
        //{
        //    // set target speed based on move speed, sprint speed and if sprint is pressed
        //    float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        //    // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        //    // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //    // if there is no input, set the target speed to 0
        //    if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        //    // a reference to the players current horizontal velocity
        //    float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        //    float speedOffset = 0.1f;
        //    float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        //    // accelerate or decelerate to target speed
        //    if (currentHorizontalSpeed < targetSpeed - speedOffset ||
        //        currentHorizontalSpeed > targetSpeed + speedOffset)
        //    {
        //        // creates curved result rather than a linear one giving a more organic speed change
        //        // note T in Lerp is clamped, so we don't need to clamp our speed
        //        _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
        //            Time.deltaTime * SpeedChangeRate);

        //        // round speed to 3 decimal places
        //        _speed = Mathf.Round(_speed * 1000f) / 1000f;
        //    }
        //    else
        //    {
        //        _speed = targetSpeed;
        //    }

        //    _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        //    if (_animationBlend < 0.01f) _animationBlend = 0f;

        //    // normalise input direction
        //    Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        //    // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //    // if there is a move input rotate player when the player is moving
        //    if (_input.move != Vector2.zero)
        //    {
        //        _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
        //                          _mainCamera.transform.eulerAngles.y;
        //        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
        //            RotationSmoothTime);

        //        // rotate to face input direction relative to camera position
        //        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        //    }


        //    Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        //    // move the player
        //    _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
        //                     new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        //    // update animator if using character
        //    if (_hasAnimator)
        //    {
        //        _animator.SetFloat(_animIDSpeed, _animationBlend);
        //        _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        //    }
        //}

        private void Move()
        {
            // Handle obstacle knockback / stun state
            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                float impulseMove = _impulseX * Time.deltaTime;
                _impulseX = Mathf.MoveTowards(_impulseX, 0f, 15f * Time.deltaTime);

                _controller.Move(
                    new Vector3(impulseMove, 0f, 0f)
                    + Vector3.up * _verticalVelocity * Time.deltaTime
                );

                Vector3 p = transform.position;
                p.z = pathZ;
                transform.position = p;
                return;
            }

            // Only W/S movement is used.
            // Starter Assets maps W/S to _input.move.y.
            float input = GetMoveInput();

            // Ignore A/D and the sprint input.
            float targetSpeed = Mathf.Abs(input) > 0.01f
                ? MoveSpeed
                : 0f;

            _speed = Mathf.MoveTowards(
                _speed,
                targetSpeed,
                SpeedChangeRate * Time.deltaTime
            );

            _animationBlend = _speed;

            // Keep the robot on the fixed X-axis line.
            float minX = Mathf.Min(pathStartX, pathEndX);
            float maxX = Mathf.Max(pathStartX, pathEndX);

            float direction = Mathf.Sign(pathEndX - pathStartX);
            if (Mathf.Approximately(direction, 0f))
                direction = 1f;

            float nextX = transform.position.x;

            if (Mathf.Abs(input) > 0.01f)
            {
                nextX += input * direction * _speed * Time.deltaTime;
                nextX = Mathf.Clamp(nextX, minX, maxX);

                // Face the direction of travel.
                Vector3 facing = Vector3.right * input * direction;
                transform.rotation = Quaternion.LookRotation(facing);
            }

            // Use CharacterController so gravity and collisions still work.
            float horizontalDelta = nextX - transform.position.x;

            _controller.Move(
                new Vector3(horizontalDelta, 0f, 0f)
                + Vector3.up * _verticalVelocity * Time.deltaTime
            );

            // Correct any small sideways drift.
            Vector3 correctedPosition = transform.position;
            correctedPosition.z = pathZ;
            transform.position = correctedPosition;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(
                    _animIDMotionSpeed,
                    Mathf.Abs(input)
                );
            }
        }


        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (GetJumpInput() && _jumpTimeoutDelta <= 0.0f && _stunTimer <= 0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                    ConsumeJumpInput();
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                ConsumeJumpInput();
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
        private float GetMoveInput()
        {
            if (_racerInput != null)
                return _racerInput.InputLocked ? 0f : _racerInput.Move;
            return _input != null ? _input.move.y : 0f;
        }

        private bool GetJumpInput()
        {
            if (_racerInput != null)
                return !_racerInput.InputLocked && _racerInput.Jump;
            return _input != null && _input.jump;
        }

        private void ConsumeJumpInput()
        {
            if (_racerInput != null)
            {
                _racerInput.ConsumeJump();
                return;
            }
            if (_input != null) _input.jump = false;
        }

        public void SetVerticalVelocity(float v) => _verticalVelocity = v;

        public void ResetForRace(Vector3 position, Quaternion rotation)
        {
            if (_controller != null) _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (_controller != null) _controller.enabled = true;

            pathZ = position.z;
            _verticalVelocity = 0f;
            _speed = 0f;
            _animationBlend = 0f;
            _impulseX = 0f;
            _stunTimer = 0f;
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            IsRespawning = true;
            ResetCameraRotation(90f);
        }

        public void ResetCameraRotation(float targetYaw)
{
    _cinemachineTargetYaw = targetYaw;
    _cinemachineTargetPitch = 0f;

    if (CinemachineCameraTarget != null)
    {
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0f);
    }
}

        // ── IHazardReceiver Implementation ────────────────────────────────
        public void ApplyImpact(Vector3 impulse, float stunSeconds)
        {
            _impulseX = impulse.x;
            _verticalVelocity = Mathf.Max(_verticalVelocity, impulse.y);
            _stunTimer = Mathf.Max(_stunTimer, stunSeconds);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var hazard = hit.collider.GetComponentInParent<HazardBase>();
            if (hazard != null)
            {
                hazard.TryHitRacer(gameObject, Vector3.one * 3f);
            }
        }
    }

    
}
