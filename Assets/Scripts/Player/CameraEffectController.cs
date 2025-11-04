using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class CameraEffectController : MonoBehaviour
{
    [SerializeField] private Volume postProcessVolume;

    private int waterColliderStack = 0;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            waterColliderStack++;
        }
        
        if(waterColliderStack > 0)
            postProcessVolume.weight = 1.0f;
        else
            postProcessVolume.weight = 0.0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            waterColliderStack--;
        }
        if(waterColliderStack > 0)
            postProcessVolume.weight = 1.0f;
        else
            postProcessVolume.weight = 0.0f;
    }
}
