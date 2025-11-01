using System.Collections;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class AssignPlayerControl : NetworkBehaviour
{
    [SerializeField] private GameObject _cameraFollowTarget;
    
    public void SetupCamera()
    {
        var _followCameras = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var _followCamera = _followCameras[0];
        for (int i = 0; i < _followCameras.Length; i++)
        {
            if (_followCameras[i].Priority > _followCamera.Priority)
            {
                _followCamera = _followCameras[i];
            }
        }
        _followCamera.Follow = _cameraFollowTarget.transform;
    }
}
