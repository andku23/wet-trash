using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkedBoat : NetworkBehaviour
{
    [SerializeField] private GameObject _driverSeat;
    [SerializeField] private Rigidbody _rb;
    
    private NetworkVariable<bool> _hasDriver = new NetworkVariable<bool>(false);
    private NetworkVariable<ulong> _driverID = new NetworkVariable<ulong>(0);
    
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
    }

    private void FixedUpdate()
    {
        if (IsOwner && _hasDriver.Value)
        {
            //transform.position += new Vector3(_input.move.x * Time.deltaTime * 3.0f, 0.0f, _input.move.y * Time.deltaTime * 3.0f);
            _rb.AddForce(_input.move.x * 3f, 0.0f, _input.move.y * 3f);
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
        NetworkClient requestedDrivePlayer = NetworkManager.Singleton.ConnectedClients[playerNetworkObjectId];
        requestedDrivePlayer.PlayerObject.SynchronizeTransform = false;
        requestedDrivePlayer.PlayerObject.GetComponent<ThirdPersonController>().ToggleDriving(true);
        requestedDrivePlayer.PlayerObject.GetComponent<CopyTransform>().target = _driverSeat;
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void RequestToUndrive_ClientRpc(ulong playerNetworkObjectId)
    {
        NetworkClient requestedDrivePlayer = NetworkManager.Singleton.ConnectedClients[playerNetworkObjectId];
        requestedDrivePlayer.PlayerObject.SynchronizeTransform = true;
        requestedDrivePlayer.PlayerObject.GetComponent<ThirdPersonController>().ToggleDriving(false);
        requestedDrivePlayer.PlayerObject.GetComponent<CopyTransform>().target = null;
    }
}
