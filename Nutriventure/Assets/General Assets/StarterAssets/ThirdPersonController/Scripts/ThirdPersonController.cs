using UnityEngine;

#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 26.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 56.335f;

        [Tooltip("Crawl speed of the character in m/s")]
        public float CrawlSpeed = 1.5f;

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

        [Tooltip("Crawl jump height (reduced)")]
        public float CrawlJumpHeight = 0.8f;

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
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Crawling")]
        [Tooltip("Crawl height as percentage of normal height")]
        [Range(0.1f, 1f)]
        public float CrawlHeightRatio = 0.5f;
        [Tooltip("How fast the character transitions between standing and crawling")]
        public float HeightChangeSpeed = 5f;
        [Tooltip("Character controller radius when crawling")]
        public float CrawlRadius = 0.2f;
        [Tooltip("Camera Y offset when crawling")]
        public float CrawlCameraYOffset = -0.5f;
        [Tooltip("Smooth transition speed for camera offset")]
        public float CameraOffsetSmoothSpeed = 5f;
        [Tooltip("Allow jumping while crawling")]
        public bool AllowJumpWhileCrawling = true;
        [Tooltip("Automatically stand up when jumping from crawl")]
        public bool AutoStandAfterCrawlJump = true;
        [Tooltip("Delay before auto-stand after crawl jump (seconds)")]
        public float AutoStandDelay = 0.5f;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private Vector3 _originalCameraLocalPosition;
        private Vector3 _targetCameraLocalPosition;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _autoStandTimer = 0f;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDCrawl;

        // crawling variables
        [SerializeField] private bool _isCrawling = false;
        private float _originalHeight;
        private float _originalRadius;
        private Vector3 _originalCenter;
        private float _targetHeight;
        private float _currentCameraYOffset = 0f;

        // Simple toggle tracking
        private bool _previousCrawlInput = false;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
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
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // Store original character controller values
            _originalHeight = _controller.height;
            _originalCenter = _controller.center;
            _originalRadius = _controller.radius;
            _targetHeight = _originalHeight;

            // Store original camera position
            if (CinemachineCameraTarget != null)
            {
                _originalCameraLocalPosition = CinemachineCameraTarget.transform.localPosition;
                _targetCameraLocalPosition = _originalCameraLocalPosition;
            }

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            HandleAutoStandTimer();
            HandleCrawlingInput();
            JumpAndGravity();
            GroundedCheck();
            Move();
            UpdateCameraOffset();
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
            _animIDCrawl = Animator.StringToHash("Crawl");
        }

        private void HandleAutoStandTimer()
        {
            if (_autoStandTimer > 0f)
            {
                _autoStandTimer -= Time.deltaTime;
                if (_autoStandTimer <= 0f && AutoStandAfterCrawlJump)
                {
                    SetCrawling(false);
                }
            }
        }

        private void HandleCrawlingInput()
        {
            // Check for keyboard input (C key)
            bool crawlInput = _input.crawl;

            // Detect button press (not hold)
            bool crawlButtonPressed = crawlInput && !_previousCrawlInput;

            // Update previous state
            _previousCrawlInput = crawlInput;

            // Toggle crawl on button press
            if (crawlButtonPressed && Grounded)
            {
                ToggleCrawl();
            }

            // Always update the crawling state
            UpdateCrawlingState();
        }

        private void UpdateCrawlingState()
        {
            // Calculate target height based on crawl state
            _targetHeight = _isCrawling ? _originalHeight * CrawlHeightRatio : _originalHeight;

            // Smoothly transition to target height
            _controller.height = Mathf.Lerp(_controller.height, _targetHeight, Time.deltaTime * HeightChangeSpeed);

            // Adjust controller center to keep feet on ground
            float heightDifference = _originalHeight - _controller.height;
            Vector3 newCenter = _originalCenter;
            newCenter.y -= heightDifference / 2f;
            _controller.center = newCenter;

            // Adjust radius when crawling
            _controller.radius = _isCrawling ? CrawlRadius : _originalRadius;

            // Update target camera position
            if (CinemachineCameraTarget != null)
            {
                _targetCameraLocalPosition = _originalCameraLocalPosition;
                if (_isCrawling)
                {
                    _targetCameraLocalPosition.y += CrawlCameraYOffset;
                }
            }

            // Update animator with crawl state
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDCrawl, _isCrawling);
            }
        }

        private void UpdateCameraOffset()
        {
            if (CinemachineCameraTarget != null)
            {
                // Smoothly interpolate camera position
                Vector3 currentPosition = CinemachineCameraTarget.transform.localPosition;
                Vector3 newPosition = Vector3.Lerp(currentPosition, _targetCameraLocalPosition,
                    Time.deltaTime * CameraOffsetSmoothSpeed);
                CinemachineCameraTarget.transform.localPosition = newPosition;
            }
        }

        private void GroundedCheck()
        {
            // Adjust grounded check based on crawling state
            float currentGroundedOffset = _isCrawling ? GroundedOffset * 0.5f : GroundedOffset;

            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - currentGroundedOffset,
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
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = 0f;

            if (_isCrawling)
            {
                targetSpeed = CrawlSpeed;
            }
            else if (_input.sprint)
            {
                targetSpeed = SprintSpeed;
            }
            else
            {
                targetSpeed = MoveSpeed;
            }

            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
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

                // Jump - check if allowed while crawling
                if (_input.jump && _jumpTimeoutDelta <= 0.0f && (!_isCrawling || AllowJumpWhileCrawling))
                {
                    // Calculate jump height based on crawl state
                    float currentJumpHeight = _isCrawling ? CrawlJumpHeight : JumpHeight;

                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(currentJumpHeight * -2f * Gravity);

                    // If jumping while crawling and auto-stand is enabled, start timer
                    if (_isCrawling && AutoStandAfterCrawlJump)
                    {
                        _autoStandTimer = AutoStandDelay;
                    }

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
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
                _input.jump = false;
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

        // Public method to check if player is crawling
        public bool IsCrawling()
        {
            return _isCrawling;
        }

        // Public method to set crawl state
        public void SetCrawling(bool crawl)
        {
            if (Grounded || !crawl)
            {
                _isCrawling = crawl;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDCrawl, _isCrawling);
                }

                if (_isCrawling)
                {
                    _input.sprint = false;
                }
            }
        }

        // Public method to toggle crawl - CALL THIS FROM UI BUTTON
        public void ToggleCrawl()
        {
            if (Grounded)
            {
                _isCrawling = !_isCrawling;
                Debug.Log("ToggleCrawl called. New state: " + _isCrawling);

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDCrawl, _isCrawling);
                }

                if (_isCrawling)
                {
                    _input.sprint = false;
                }
            }
        }
    }
}