using Cinemachine;
using UnityEngine;

public class StaticCameraControl : MonoBehaviour, ICameraControl
{

    private GameObject _cinemachineCameraTarget;
    private CinemachineVirtualCamera _followCamera;

    public void UpdateCameraRotation()
    {
    }

    public void SetupCinemachineCamera()
    {
        var _followCameras = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _followCamera = _followCameras[0];
        for (int i = 0; i < _followCameras.Length; i++)
        {
            if (_followCameras[i].CompareTag("StaticCamera"))
            {
                _followCamera = _followCameras[i];
            }
        }

        if (_followCamera != null)
        {
            _followCamera.Priority += 1;
        }
    }
    
    public void DesetupCinemachineCamera()
    {
        if (_followCamera != null)
        {
            _followCamera.Priority -= 1;
        }
    }

    public GameObject CinemachineCameraTarget =>_cinemachineCameraTarget;
}
