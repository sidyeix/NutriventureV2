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

        [Header("Pushing")]
        [Tooltip("Push speed of the character in m/s")]
        public float PushSpeed = 1.5f;
        [Tooltip("How close the player needs to be to push an object")]
        public float PushRange = 1.5f;
        [Tooltip("How much force is applied to pushable objects")]
        public float PushForce = 5f;
        [Tooltip("Angle threshold for pushing (degrees)")]
        public float PushAngleThreshold = 45f;

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
        private int _animIDIsPushing;

        // crawling variables
        [SerializeField] private bool _isCrawling = false;
        private float _originalHeight;
        private float _originalRadius;
        private Vector3 _originalCenter;
        private float _targetHeight;
        private float _currentCameraYOffset = 0f;

        // pushing variables
        [SerializeField] private bool _isPushing = false;
        private GameObject _currentPushableObject;
        private Vector3 _pushDirection;
        private bool _wasPushingLastFrame = false;

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
            HandlePushing();
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
            _animIDIsPushing = Animator.StringToHash("isPushing");
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

        private void HandlePushing()
        {
            // If push button is pressed
            if (_input.push)
            {
                if (!_isPushing)
                {
                    // Try to find a pushable object in front
                    CheckForPushableObject();
                }

                if (_isPushing && _currentPushableObject != null)
                {
                    // Check if player is still facing the object
                    Vector3 toObject = _currentPushableObject.transform.position - transform.position;
                    toObject.y = 0;
                    float angle = Vector3.Angle(transform.forward, toObject.normalized);

                    if (angle > PushAngleThreshold || Vector3.Distance(transform.position, _currentPushableObject.transform.position) > PushRange + 1f)
                    {
                        // Player is no longer facing the object or too far away
                        StopPushing();
                        return;
                    }

                    // Update push direction based on player's forward direction
                    _pushDirection = transform.forward;

                    // Apply force to the pushable object if player is moving forward
                    if (_input.move.y > 0.1f)
                    {
                        Rigidbody rb = _currentPushableObject.GetComponent<Rigidbody>();
                        if (rb != null && rb.isKinematic)
                        {
                            // Force disable kinematic if we're supposed to be pushing
                            rb.isKinematic = false;
                            Debug.Log("Disabled kinematic on pushable object: " + _currentPushableObject.name);
                        }

                        if (rb != null && !rb.isKinematic)
                        {
                            Vector3 force = _pushDirection * PushForce * Time.deltaTime * 60f * Mathf.Clamp01(_input.move.y);
                            rb.AddForce(force, ForceMode.Force);
                        }
                    }
                    else
                    {
                        // Player is not moving forward - stop the object's movement
                        Rigidbody rb = _currentPushableObject.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }
                }
            }
            else if (_isPushing)
            {
                // Stop pushing when button is released
                StopPushing();
            }
        }

        private void CheckForPushableObject()
        {
            if (_isCrawling) return; // Can't push while crawling

            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;

            // Use a spherecast for better detection
            if (Physics.SphereCast(rayStart, 0.3f, transform.forward, out hit, PushRange))
            {
                // Check if the object has the "Pushable" tag AND has a Rigidbody
                if (hit.collider.CompareTag("Pushable"))
                {
                    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        _currentPushableObject = hit.collider.gameObject;
                        StartPushing();
                        return;
                    }
                }
            }

            // Also check for objects in front using OverlapSphere
            Collider[] colliders = Physics.OverlapSphere(rayStart + transform.forward * (PushRange * 0.5f), 0.5f);
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Pushable"))
                {
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        _currentPushableObject = col.gameObject;
                        StartPushing();
                        return;
                    }
                }
            }
        }

        private void StartPushing()
        {
            _isPushing = true;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDIsPushing, true);
            }

            // Disable other movement states
            _input.sprint = false;
            SetCrawling(false);

            Debug.Log("Started pushing: " + (_currentPushableObject != null ? _currentPushableObject.name : "null"));
        }

        public void StopPushing()
        {
            _isPushing = false;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDIsPushing, false);
            }

            // Re-enable kinematic on the pushable object
            if (_currentPushableObject != null)
            {
                Rigidbody rb = _currentPushableObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true; // Stop physics interactions
                    rb.linearVelocity = Vector3.zero; // Stop any remaining movement
                    rb.angularVelocity = Vector3.zero;
                    Debug.Log("Re-enabled kinematic on pushable object: " + _currentPushableObject.name);
                }
            }

            _currentPushableObject = null;

            Debug.Log("Stopped pushing");
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
            // If pushing, limit movement speed and direction
            float targetSpeed;

            if (_isPushing)
            {
                // When pushing, use push speed
                targetSpeed = PushSpeed;

                // Check conditions for pushing movement:
                bool canMoveWhilePushing = _input.push &&
                                           _currentPushableObject != null &&
                                           _input.move.y > 0.1f;

                if (!canMoveWhilePushing)
                {
                    targetSpeed = 0f;
                }
                else
                {
                    // Scale speed based on forward input
                    targetSpeed *= Mathf.Clamp01(_input.move.y);
                }
            }
            else if (_isCrawling)
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

                // Jump - check if allowed while crawling or pushing
                if (_input.jump && _jumpTimeoutDelta <= 0.0f &&
                    (!_isCrawling || AllowJumpWhileCrawling) &&
                    !_isPushing) // Can't jump while pushing
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

                    // Stop pushing if jumping
                    if (_isPushing)
                    {
                        StopPushing();
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

            // Draw push range gizmo
            Gizmos.color = Color.blue;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawLine(rayStart, rayStart + transform.forward * PushRange);
            Gizmos.DrawWireSphere(rayStart, 0.3f);
            Gizmos.DrawWireSphere(rayStart + transform.forward * (PushRange * 0.5f), 0.5f);
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

        // Public method to check if player can push
        public bool CanPush()
        {
            if (_isCrawling) return false;

            // Check if there's a pushable object in front using tag
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;

            if (Physics.SphereCast(rayStart, 0.3f, transform.forward, out hit, PushRange))
            {
                return hit.collider.CompareTag("Pushable");
            }

            return false;
        }

        // Public method to get push state
        public bool IsPushing()
        {
            return _isPushing;
        }

        // Public method to get current pushable object
        public GameObject GetCurrentPushableObject()
        {
            return _currentPushableObject;
        }

        public bool GetPushInput()
        {
            return _input.push;
        }
    }
}