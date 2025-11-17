using Cinemachine;
using UnityEngine;

public class FirstPersonCameraControl : MonoBehaviour, ICameraControl
{
    
    private GameObject _mainCamera;
    
    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject cinemachineCameraTarget;

    public GameObject CinemachineCameraTarget { get => cinemachineCameraTarget; }

    private CinemachineVirtualCamera _followCamera;

    private void Start()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }
    
    public void SetupCinemachineCamera()
    {
        var _followCameras = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _followCamera = _followCameras[0];
        for (int i = 0; i < _followCameras.Length; i++)
        {
            if (_followCameras[i].CompareTag("FirstPersonCamera"))
            {
                _followCamera = _followCameras[i];
            }
        }
        _followCamera.Follow = CinemachineCameraTarget.transform;
        _followCamera.Priority += 1;
    }
    
    public void DesetupCinemachineCamera()
    {
        _followCamera.Priority -= 1;
    }
    
    public void UpdateCameraRotation()
    {
    }
}
