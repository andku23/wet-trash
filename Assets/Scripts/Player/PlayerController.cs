using System.Runtime.CompilerServices;
using StarterAssets;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
*/

[RequireComponent(typeof(CharacterController))]

public class PlayerController : NetworkBehaviour
{
    public PlayerState playerState;
    

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

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

    public bool ForceThirdPerson;
    public GameObject ControlModeThirdPerson;
    public GameObject ControlModeFirstPerson;
    public GameObject ControlModeStatic;
    public ControlModeEnum ControlMode;
    
    public OwnerNetworkAnimator OwnerNetworkAnimator;

    private ICameraControl _cameraControl;
    private bool _isCameraAndMovementLocked;
    
    public NetworkHandleParenting NetworkHandleParenting;

    public enum ControlModeEnum
    {
        FirstPerson,
        ThirdPerson,
        StaticControl
    }

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
    private int _animIDVerticalLookAmount;

    private float firstPersonPitch;
    private float firstPersonYaw;

#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerAudioSource _playerAudioSource;
    private CharacterController _controller;
    private StarterAssetsInputs _input;
    public GameObject MainCamera;
    private GameObject _selectedControlMode;

    private bool _hasAnimator;
    public ICameraControl CameraControl {get{return _cameraControl;}}

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

    public override void OnNetworkSpawn()
    {
        //Set control to first person if youre the controller
        if (IsOwner && !ForceThirdPerson)
        {
            ChangeControlMode(ControlModeEnum.FirstPerson);
        }
        else
        {
            ChangeControlMode(ControlModeEnum.ThirdPerson);
        }
        
        // Disable whichever version isnt being used
        
        if (IsOwner)
        {
            _controller = GetComponent<CharacterController>();
            _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#if ENABLE_INPUT_SYSTEM
            _playerInput = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#endif
            UI.Instance.OnUIOpened += LockCamera;
            UI.Instance.OnUIClosed += UnlockCamera;
        }
    }

    private void OnDestroy()
    {
        if (IsOwner)
        {
            UI.Instance.OnUIOpened -= LockCamera;
            UI.Instance.OnUIClosed -= UnlockCamera;
        }
    }

    public void ChangeControlMode(ControlModeEnum controlMode)
    {
        ControlMode = controlMode;
        if (_cameraControl != null)
        {
            _cameraControl.DesetupCinemachineCamera();
        }
        
        ControlModeFirstPerson.SetActive(controlMode == ControlModeEnum.FirstPerson);
        ControlModeThirdPerson.SetActive(controlMode == ControlModeEnum.ThirdPerson);
        ControlModeStatic.SetActive(controlMode == ControlModeEnum.StaticControl);
        
        switch (controlMode)
        {
            case ControlModeEnum.FirstPerson:
                _selectedControlMode = ControlModeFirstPerson;
                break;
            case ControlModeEnum.ThirdPerson:
                _selectedControlMode = ControlModeThirdPerson;
                break;
            case ControlModeEnum.StaticControl:
                _selectedControlMode = ControlModeStatic;
                break;
        }
        
        OwnerNetworkAnimator.Animator = _selectedControlMode.GetComponent<Animator>();
        GetComponent<PlayerDeath>().Animator = _selectedControlMode.GetComponent<Animator>();
        _animator = _selectedControlMode.GetComponent<Animator>();
        _cameraControl = _selectedControlMode.GetComponent<ICameraControl>();

        if (IsOwner)
        {
            if (MainCamera == null) MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _cameraControl.SetupCinemachineCamera();
            _hasAnimator = _animator != null;
            AssignAnimationIDs();
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            if (_animator != null)
            {
                _animator.SetFloat(_animIDVerticalLookAmount, 0.5f);
            }
        }
    }

    private void LockCamera()
    {
        Debug.Log("LockCamera");
        _isCameraAndMovementLocked = true;
    }
    
    private void UnlockCamera()
    {
        _isCameraAndMovementLocked = false;
    }

    private void Update()
    {
        if (!IsOwner) return;
        _hasAnimator = _animator != null;

        JumpAndGravity();
        GroundedCheck();
        InWaterCheck();
        if (!playerState.IsDead && !_isCameraAndMovementLocked)
        {
            Move();
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        _cameraControl.UpdateCameraRotation();

        if (_input.debug)
        {
            _input.debug = false;
            if (_selectedControlMode == ControlModeFirstPerson)
            {
                ChangeControlMode(ControlModeEnum.ThirdPerson);
            }
            else if (_selectedControlMode == ControlModeThirdPerson)
            {
                ChangeControlMode(ControlModeEnum.StaticControl);
            }
            else
            {
                ChangeControlMode(ControlModeEnum.FirstPerson);
            }
        }
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
        _animIDVerticalLookAmount = Animator.StringToHash("VerticalLookAmount");
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
        
        bool isNextInWater = Physics.CheckSphere(spherePosition, WaterRadius, WaterLayers,
            QueryTriggerInteraction.Collide);

        
        playerState.InWater = isNextInWater;

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
        float currentSpeed = new Vector3(_controller.velocity.x, _controller.velocity.y, _controller.velocity.z).magnitude;
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
        
        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
        
        targetSpeed *= 1/(playerState.SwimWeightMultiplier * playerState.WeightCarried + 1);
        
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
        
        
            if (ControlMode == ControlModeEnum.ThirdPerson)
            {
                if (_input.move != Vector2.zero)
                {
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                      MainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                        playerState.RotationSmoothTime);
                    // rotate to face input direction relative to camera position
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
       
                // move the player
                _controller.Move(targetDirection.normalized * (inputDirection.magnitude * (_speed * Time.deltaTime)) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }
            else if (ControlMode == ControlModeEnum.FirstPerson)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                firstPersonPitch += _input.look.y * deltaTimeMultiplier;
                firstPersonYaw += _input.look.x * deltaTimeMultiplier;
            
                firstPersonYaw = ThirdPersonCameraControl.ClampAngle(firstPersonYaw, float.MinValue, float.MaxValue);
                firstPersonPitch = ThirdPersonCameraControl.ClampAngle(firstPersonPitch, -89, 89);
                
                transform.rotation = Quaternion.Euler(0.0f,
                    firstPersonYaw, 0.0f);
                
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDVerticalLookAmount, (firstPersonPitch + 89) / (89 + 89));
                    _cameraControl.CinemachineCameraTarget.transform.localEulerAngles = new Vector3(firstPersonPitch, 0, 0);
                }

                _targetRotation = transform.rotation.eulerAngles.y;
            
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                Vector3 leftDirection = Quaternion.Euler(0, _targetRotation + 90, 0) * Vector3.forward ;

                // move the player
                _controller.Move(targetDirection.normalized * (inputDirection.z * (_speed * Time.deltaTime)) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime +
                                 leftDirection.normalized * (inputDirection.x * (_speed * Time.deltaTime)));
            }
            
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

        targetSpeed *= 1/(playerState.SprintWeightMultiplier * playerState.WeightCarried + 1);;

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
        if (ControlMode == ControlModeEnum.ThirdPerson)
        {
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  MainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    playerState.RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }
        else if (ControlMode == ControlModeEnum.FirstPerson)
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            Debug.Log(_input.look.y);
            firstPersonPitch += _input.look.y * deltaTimeMultiplier;
            firstPersonYaw += _input.look.x * deltaTimeMultiplier;
            
            firstPersonYaw = ThirdPersonCameraControl.ClampAngle(firstPersonYaw, float.MinValue, float.MaxValue);
            firstPersonPitch = ThirdPersonCameraControl.ClampAngle(firstPersonPitch, -89, 89);
                
            transform.rotation = Quaternion.Euler(0.0f,
                firstPersonYaw, 0.0f);

            if (_hasAnimator)
            {
                float remapPitchAngle = ((firstPersonPitch + 89) / (89 + 89));
                _animator.SetFloat(_animIDVerticalLookAmount, remapPitchAngle);
                _cameraControl.CinemachineCameraTarget.transform.localEulerAngles = new Vector3(firstPersonPitch, 0, 0);
            }

            _targetRotation = transform.rotation.eulerAngles.y;
            
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            Vector3 leftDirection = Quaternion.Euler(0, _targetRotation + 90, 0) * Vector3.forward ;

            // move the player
            _controller.Move(targetDirection.normalized * (inputDirection.z * (_speed * Time.deltaTime)) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime +
                leftDirection.normalized * (inputDirection.x * (_speed * Time.deltaTime)));
        }

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
                    _verticalVelocity = playerState.WaterVerticalSwimSpeed;
                    
                }
            }
            else if (_input.descend)
            {
                _verticalVelocity = -playerState.WaterVerticalSwimSpeed;
            }
            else
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = 0.0f;
            }
            _verticalVelocity *= 1/(playerState.SwimWeightMultiplier*playerState.WeightCarried + 1);
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
}
