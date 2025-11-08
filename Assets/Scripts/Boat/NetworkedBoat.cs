using System.Collections.Generic;
using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkedBoat : NetworkBehaviour
{
    [SerializeField] private GameObject _driverSeat;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private CinemachineVirtualCamera _boatVirtualCamera;
    [SerializeField] private float RotationSmoothTime = 0.12f;
    [SerializeField] private float BoatSpeed = 2.0f;
    
    public List<BoatAttachmentPoint> BoatAttachmentPoints = new List<BoatAttachmentPoint>();
    
    private NetworkVariable<bool> _hasDriver = new NetworkVariable<bool>(false);
    private NetworkVariable<ulong> _driverID = new NetworkVariable<ulong>(0);
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    
    public bool HasDriver => _hasDriver.Value;
    public ulong DriverID => _driverID.Value;
    
#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif
    private StarterAssetsInputs _input;
    
    public override void OnNetworkSpawn()
    {
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#if ENABLE_INPUT_SYSTEM
        _playerInput = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#else
#endif
        GameManager.Instance.Boat = this;
        
        //Initialize all the boat logic so it can be spawned in
        
    }

    private void FixedUpdate()
    {
        if (IsOwner && _hasDriver.Value)
        {
            //transform.position += new Vector3(_input.move.x * Time.deltaTime * 3.0f, 0.0f, _input.move.y * Time.deltaTime * 3.0f);
            if (_input.move != Vector2.zero)
            {
                Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                //transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                
                float torqueMagnitude = 500f; // Adjust this value for desired rotational speed
                _rb.AddTorque(transform.up * torqueMagnitude * _input.move.x); 
                
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * transform.forward;

                if (_input.move.y != 0.0f)
                {
                    _rb.AddForce(targetDirection.normalized * BoatSpeed, ForceMode.Impulse);
                }
            }
        }
    }

    public void RequestToDrive(bool isDriving)
    {
        if (isDriving)
        {
            RequestToDrive_ServerRpc(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            RequestToUndrive_ServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestToDrive_ServerRpc(ulong playerNetworkObjectId)
    {
        if (_hasDriver.Value) return;
        _hasDriver.Value = true;
        _driverID.Value = playerNetworkObjectId;
        GetComponent<NetworkObject>().ChangeOwnership(playerNetworkObjectId);
        GetComponent<NetworkTransformFixed>().ForceApplyAuthoritativeState();
        RequestToDrive_ClientRpc(playerNetworkObjectId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestToUndrive_ServerRpc(ulong playerNetworkObjectId)
    {
        if (!_hasDriver.Value) return;
        if (playerNetworkObjectId != _driverID.Value) return;
        _hasDriver.Value = false;
        GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.ServerClientId);
        RequestToUndrive_ClientRpc(playerNetworkObjectId);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void RequestToDrive_ClientRpc(ulong playerNetworkObjectId)
    {
        if (playerNetworkObjectId == NetworkManager.Singleton.LocalClientId)
        {
            _boatVirtualCamera.Priority = 20;
        }

        // if (IsOwner)
        // {
        //     GetComponent<NetworkTransformFixed>().ForceApplyAuthoritativeState();
        // }
        
        NetworkClient requestedDrivePlayer = NetworkManager.Singleton.ConnectedClients[playerNetworkObjectId];
        requestedDrivePlayer.PlayerObject.SynchronizeTransform = false;
        requestedDrivePlayer.PlayerObject.GetComponent<PlayerController>().ToggleDriving(true);
        requestedDrivePlayer.PlayerObject.GetComponent<CopyTransform>().target = _driverSeat;
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void RequestToUndrive_ClientRpc(ulong playerNetworkObjectId)
    {
        if (playerNetworkObjectId == NetworkManager.Singleton.LocalClientId)
        {
            _boatVirtualCamera.Priority = 0;
        }
        
        NetworkClient requestedDrivePlayer = NetworkManager.Singleton.ConnectedClients[playerNetworkObjectId];
        requestedDrivePlayer.PlayerObject.SynchronizeTransform = true;
        requestedDrivePlayer.PlayerObject.GetComponent<PlayerController>().ToggleDriving(false);
        requestedDrivePlayer.PlayerObject.GetComponent<CopyTransform>().target = null;
    }
}
