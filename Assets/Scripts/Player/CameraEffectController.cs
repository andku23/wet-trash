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
        postProcessVolume.weight = (waterChecker.IsInWater) ? 1.0f : 0.0f;
    }
}
