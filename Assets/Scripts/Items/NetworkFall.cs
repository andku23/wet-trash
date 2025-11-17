using Unity.Netcode;
using UnityEngine;

public class NetworkFall : NetworkBehaviour
{
    [SerializeField] private InWaterChecker inWaterChecker;
    [SerializeField] private LayerMask layerMask;
    
    public float FallSpeedAir = 0.01f;
    
    public float FallSpeedWater = 0.01f;
    

    private void FixedUpdate()
    {
        if (!IsServer) return;
        float fallSpeed = (inWaterChecker.IsInWater) ? FallSpeedWater : FallSpeedAir;
        if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit raycastHit, fallSpeed, layerMask))
        {
            transform.position = raycastHit.point;
            enabled = false;
        }
        else
        {
            transform.position += Vector3.down * fallSpeed;
        }
    }
}
