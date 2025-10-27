using System.Runtime.CompilerServices;
using StarterAssets;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
*/


[RequireComponent(typeof(CharacterController))]

public class ThirdPersonController : NetworkBehaviour
{
    /*[Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float playerState.MoveSpeed = 2.0f;
    
    [Tooltip("Max Swimming Speed in m/s")]
    public float playerState.MaxSwimmingSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float playerState.SprintSpeed = 5.335f;
    
    [Tooltip("Sprint Swim speed of the character in m/s")]
    public float playerState.SprintSwimSpeed = 4.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float playerState.RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float playerState.SpeedChangeRate = 10.0f;*/

    public PlayerState playerState;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    /*[Space(10)]
    [Tooltip("The height the player can jump")]
    public float playerState.JumpHeight = 1.2f;
    */

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    /*[Header("Player playerState.Grounded")]
    [Tooltip("If the character is playerState.Grounded or not. Not part of the CharacterController built in playerState.Grounded check")]
    public bool playerState.Grounded = true;
    
    public bool playerState.VehicleParented = false;
    
    [Header("In Water")]
    public bool playerState.InWater = true;
    
    [Header("Is Driving")]
    public bool playerState.IsDriving = false;
    
    [Header("Is Dead")]
    public bool playerState.IsDead = false;
    
    [Header("If youre on the water surface")]
    public bool playerState.InWaterOnSurface = false;*/


    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the playerState.Grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("The radius of the water check. Should match the radius of the CharacterController")]
    public float WaterRadius = 0.1f;

    [Tooltip("Center of water check")]
    public GameObject WaterCheckCenter;
    
    [Tooltip("Used to check if your head is out of the water")]
    public GameObject WaterCheckTop;
    
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;
    
    [Tooltip("What layers the character uses as water")]
    public LayerMask WaterLayers;

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
    
    public NetworkHandleParenting NetworkHandleParenting;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

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

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDIsSwimming;
    private int _animIDIsCarrying;
    private int _animIDIsDriving;

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
        
    }

    public override void OnNetworkSpawn()
    {

        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#if ENABLE_INPUT_SYSTEM
        _playerInput = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        Debug.Log(_playerInput);
#else
		Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        if (!IsOwner) return;

        _hasAnimator = TryGetComponent(out _animator);

        
        JumpAndGravity();
        GroundedCheck();
        InWaterCheck();
        if (!playerState.IsDead)
        {
            Move();
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDIsSwimming = Animator.StringToHash("IsSwimming");
        _animIDIsCarrying = Animator.StringToHash("IsCarrying");
        _animIDIsDriving = Animator.StringToHash("IsDriving");
    }

    private void GroundedCheck()
    {
        
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        
        Collider[] hitColliders = Physics.OverlapSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        playerState.Grounded = hitColliders.Length > 0;
        
        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, playerState.Grounded);
        }

        Collider boatColliderInRange = null;
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].CompareTag("Boat"))
            {
                boatColliderInRange = hitColliders[i];
            }
        }
    
        // Don't do anything if its already in the same state
        if (playerState.VehicleParented == (boatColliderInRange != null)) return;
        playerState.VehicleParented = (boatColliderInRange != null);
        if (playerState.VehicleParented)
        {
            GameObject parent = boatColliderInRange.GetComponent<ColliderReference>().reference;
            NetworkHandleParenting.RequestParentTo(NetworkManager.Singleton.LocalClientId, parent.GetComponent<NetworkTransform>().NetworkObjectId);
        }
        else
        {
            NetworkHandleParenting.RequestUnparentTo(NetworkManager.Singleton.LocalClientId);
        }
    }
    
    private void InWaterCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = WaterCheckCenter.transform.position;
        playerState.InWater = Physics.CheckSphere(spherePosition, WaterRadius, WaterLayers,
            QueryTriggerInteraction.Collide);

        if (playerState.InWater)
        {
            Vector3 topSpherePosition = WaterCheckTop.transform.position;
            playerState.InWaterOnSurface = !Physics.CheckSphere(topSpherePosition, 0.01f, WaterLayers,
                QueryTriggerInteraction.Collide);
            playerState.Grounded = false;
        }
        else
        {
            playerState.InWaterOnSurface = false;
        }
        
        // update animator if using character
        if (_hasAnimator && (playerState.InWater != _animator.GetBool(_animIDIsSwimming)))
        {
            _animator.SetBool(_animIDIsSwimming, playerState.InWater);
            _animator.SetBool(_animIDGrounded, playerState.Grounded);
        }
    }

    public void ToggleCarrying(bool isCarrying)
    {
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDIsCarrying, isCarrying);
        }
    }
    
    public void ToggleDriving(bool IsDriving)
    {
        playerState.IsDriving = IsDriving;
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDIsDriving, playerState.IsDriving);
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
        if (playerState.IsDriving) return;
        if (playerState.InWater)
        {
            MoveWater();
        }
        else
        {
            MoveLand();
        }
    }

    private void MoveWater()
    {
        float targetSpeed = _input.sprint ? playerState.SprintSwimSpeed : playerState.MoveSpeed;
        Vector3 cameraForward = CinemachineCameraTarget.transform.forward;
        float currentSpeed = new Vector3(_controller.velocity.x, _controller.velocity.y, _controller.velocity.z).magnitude;
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
        
        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
        
        targetSpeed *= playerState.SwimWeightMultiplier;
        
        if (currentSpeed < targetSpeed - speedOffset ||
            currentSpeed > targetSpeed + speedOffset)
        {
            
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * playerState.SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            
            _speed = targetSpeed;
        }

        _speed = Mathf.Clamp(_speed, 0, playerState.SprintSwimSpeed);
        
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                playerState.RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 cameraEuler = CinemachineCameraTarget.transform.forward;

       Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
       
        // move the player
        _controller.Move(targetDirection.normalized * (inputDirection.magnitude * (_speed * Time.deltaTime)) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        
        if (_hasAnimator)
        {
            float dampTime = 0.2f;
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, _speed, dampTime, Time.deltaTime);
        }
    }
    private void MoveLand()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _input.sprint ? playerState.SprintSpeed : playerState.MoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        targetSpeed *= playerState.SprintWeightMultiplier;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * playerState.SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * playerState.SpeedChangeRate);
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
                playerState.RotationSmoothTime);

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
        if (playerState.InWater)
        {
            if (_input.jump)
            {
                if (playerState.InWaterOnSurface)
                {
                    _verticalVelocity = playerState.WaterSurfaceJumpHeight;
                }
                else
                {
                    _verticalVelocity = 5.0f;
                    
                }
            }
            else if (_input.descend)
            {
                _verticalVelocity = -5.0f;
            }
            else
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = 0.0f;
            }
        }
        else if (playerState.Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when playerState.Grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(playerState.JumpHeight * -2f * Gravity);

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

            // if we are not playerState.Grounded, do not jump
            _input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity && !playerState.InWater)
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

        if (playerState.Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the playerState.Grounded collider
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
                //AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            //AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }
}
