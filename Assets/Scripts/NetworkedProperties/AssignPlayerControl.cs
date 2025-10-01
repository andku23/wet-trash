using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class AssignPlayerControl : NetworkBehaviour
{
    [SerializeField] private GameObject _cameraFollowTarget;
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var _followCamera = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            _followCamera.Follow = _cameraFollowTarget.transform;
        }
    }
}
