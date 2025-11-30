using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class NetworkedBoat : NetworkBehaviour
{
    [SerializeField] private InteractableSteeringWheel _steeringWheel;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private CinemachineVirtualCamera _boatVirtualCamera;
    [SerializeField] private float RotationSmoothTime = 0.12f;
    [SerializeField] public NetworkBoatState BoatState;
    [SerializeField] private List<GameObject> initialBoatParts;
    
    public List<BoatAttachmentPoint> BoatAttachmentPoints = new List<BoatAttachmentPoint>();
    public Dictionary<ulong, BoatPart> BoatParts = new Dictionary<ulong, BoatPart>(); //ALl the realtime added boat parts 
    public Transform DriverSeat; // transform thats baked into the prefab that gets moved to wherever the steering wheel is when boat part is added
    
    
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    
    public bool HasDriver => BoatState.HasDriver.Value;
    public ulong DriverID => BoatState.DriverID.Value;
    
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
        BoatManager.Instance.Boat = this;
        
        //Initialize all the boat logic so it can be spawned in
        if (IsServer)
        {
            StartCoroutine(InitializeBoatParts());
        }
        
    }

    public IEnumerator InitializeBoatParts()
    {
        yield return null;
        foreach (GameObject boatPartPrefab in initialBoatParts)
        {
            GameObject go = Instantiate(boatPartPrefab);
            NetworkObject no = go.GetComponent<NetworkObject>();
            no.transform.position = transform.position;
            no.transform.rotation = transform.rotation;
            
            no.Spawn();
            no.transform.parent = transform;
            
            BoatPart boatPart = no.GetComponent<BoatPart>();

            if (boatPart.SteeringWheel != null)
            {
                _steeringWheel = boatPart.SteeringWheel;
                MoveDriverSeat_ServerRpc(boatPart.SteeringWheel.transform.position,
                    boatPart.SteeringWheel.transform.rotation);
            }
                
            BoatManager.Instance.RegisterBoatPartServer(no, boatPart);
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner && HasDriver)
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
                
                _rb.AddTorque(transform.up * BoatState.TurnSpeed.Value * _input.move.x); 
                
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * transform.forward;

                if (_input.move.y != 0.0f)
                {
                    _rb.AddForce(targetDirection.normalized * BoatState.MoveSpeed.Value, ForceMode.Impulse);
                }
            }
        }
    }

    [ServerRpc]
    private void MoveDriverSeat_ServerRpc(Vector3 position, Quaternion rotation)
    {
        MoveDriverSeat_ClientRpc(position, rotation);
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void MoveDriverSeat_ClientRpc(Vector3 position, Quaternion rotation)
    {
        DriverSeat.position = position;
        DriverSeat.rotation = rotation;
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
        if (HasDriver) return;
        BoatState.HasDriver.Value = true;
        BoatState.DriverID.Value = playerNetworkObjectId;
        GetComponent<NetworkObject>().ChangeOwnership(playerNetworkObjectId);
        GetComponent<NetworkTransformFixed>().ForceApplyAuthoritativeState();
        RequestToDrive_ClientRpc(playerNetworkObjectId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestToUndrive_ServerRpc(ulong playerNetworkObjectId)
    {
        if (!HasDriver) return;
        if (playerNetworkObjectId != DriverID) return;
        BoatState.HasDriver.Value = false;
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
        requestedDrivePlayer.PlayerObject.GetComponent<CopyTransform>().target = DriverSeat.gameObject;
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

