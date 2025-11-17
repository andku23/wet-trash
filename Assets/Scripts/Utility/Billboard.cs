using UnityEngine;


/// <summary>
/// Locks transform rotation to camera view
/// </summary>
[ExecuteInEditMode]
public class Billboard : MonoBehaviour
{
    private Transform _transform;

    protected void Awake() { _transform = transform; }

    protected void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            transform.rotation = Quaternion.LookRotation(camera.transform.forward,
                Vector3.up);
        }
        
        
        var rot = _transform.localRotation;
        _transform.localRotation = rot;
    }
}
