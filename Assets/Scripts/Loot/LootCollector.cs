using StarterAssets;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class LootCollector : NetworkBehaviour
{
    public static LootCollector Instance;
    
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Transform grabbedLootConnectPoint;
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private ThirdPersonController thirdPersonController;
    
#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif
    
    private StarterAssetsInputs _input;
    private NetworkLoot lastClosestLoot;
    private GameObject heldLoot;
    private const float MAX_DROP_DISTANCE = 1.5f;
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        if(Instance == null) Instance = this;
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#if ENABLE_INPUT_SYSTEM
        _playerInput = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        base.OnNetworkSpawn();
    }
    
    public void AttachToPoint(GameObject loot)
    {
        GameObject go = Instantiate(loot, grabbedLootConnectPoint.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        heldLoot = go;
        thirdPersonController.ToggleCarrying(true);
    }

    public void DestroyHeldObject()
    {
        Destroy(heldLoot);
        heldLoot = null;
        thirdPersonController.ToggleCarrying(false);
    }
    
    void Update()
    {
        if (!IsOwner) return;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2.0f, layerMask);
        float minDistance = Mathf.Infinity;
        Collider closestCollider = null;

        foreach (Collider collider in hitColliders)
        {
            // Optionally, exclude self if the script is on an object with a collider
            if (collider.gameObject == gameObject) continue; 

            float distance = Vector3.Distance(transform.position, collider.transform.position); 

            if (distance < minDistance)
            {
                minDistance = distance;
                closestCollider = collider;
            }
        }

        if (closestCollider != null)
        {
            NetworkLoot loot = closestCollider.GetComponent<ColliderReference>().reference.GetComponent<NetworkLoot>();
            if (loot != null && loot != lastClosestLoot)
            {
                loot.TrySetClosestLoot(true);
                if (lastClosestLoot != null)
                {
                    NetworkLoot lastLoot = lastClosestLoot.GetComponent<NetworkLoot>();
                    lastLoot.TrySetClosestLoot(false);
                }
                lastClosestLoot = loot;
            }
        }
        else
        {
            if (lastClosestLoot != null)
            {
                NetworkLoot lastLoot = lastClosestLoot.GetComponent<NetworkLoot>();
                lastLoot.TrySetClosestLoot(false);
                lastClosestLoot = null;
            }
        }

        if (heldLoot != null)
        {
            if (_input.interact)
            {
                Physics.Raycast(heldLoot.transform.position, -Vector3.up, out RaycastHit hit);
                if (hit.collider != null)
                {
                    if (hit.distance < MAX_DROP_DISTANCE)
                    {
                        LootManager.Instance.RequestDrop(
                            new Vector3(hit.point.x, hit.point.y + 0.3f, hit.point.z), heldLoot.gameObject);
                    }
                    
                }
            }
        }
        else
        {
            if (_input.interact && lastClosestLoot != null)
            {
                LootManager.Instance.RequestPickup(lastClosestLoot);
                lastClosestLoot = null; //Get rid of this if i plan on having it be carried
            }
        }
        
        _input.interact = false;
    }
}
