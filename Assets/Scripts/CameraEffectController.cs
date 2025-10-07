using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraEffectController : MonoBehaviour
{
    [SerializeField] private PostProcessVolume postProcessVolume;
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("collide with water");
        postProcessVolume.weight = 1.0f;
    }

    private void OnTriggerExit(Collider other)
    {
        postProcessVolume.weight = 0.0f;
    }
}
