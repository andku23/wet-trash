using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]

public class InWaterChecker : MonoBehaviour
{
    private int waterColliderStack = 0;

    public Action OnWaterStackUpdated;
    public bool IsInWater { get => waterColliderStack > 0; }

    private void Start()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Water"))
            {
                waterColliderStack++;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            waterColliderStack++;
        }
        
        OnWaterStackUpdated?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            waterColliderStack--;
        }
        
        OnWaterStackUpdated?.Invoke();
    }
}
