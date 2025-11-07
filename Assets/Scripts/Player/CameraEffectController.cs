using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class CameraEffectController : MonoBehaviour
{
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private InWaterChecker waterChecker;

    private int waterColliderStack = 0;

    private void Start()
    {
        waterChecker.OnWaterStackUpdated += OnWaterStackUpdated;
    }

    private void OnDestroy()
    {
        waterChecker.OnWaterStackUpdated -= OnWaterStackUpdated;
    }

    private void OnWaterStackUpdated()
    {
        if (waterChecker.IsInWater)
        {
            RenderSettings.fog = true;
            postProcessVolume.weight = 1.0f;
        }
        else
        {
            RenderSettings.fog = false;
            postProcessVolume.weight = 0.0f;
        }
        
    }
}
