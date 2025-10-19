using StarterAssets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : NetworkBehaviour
{
    public static InteractionController Instance;
    
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Transform grabbedLootConnectPoint;
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private ThirdPersonController thirdPersonController;
    
#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif
    
    private StarterAssetsInputs _input;
    private IInteractable lastClosestInteractable;
    private GameObject heldLoot;
    private const float MAX_DROP_DISTANCE = 1.5f;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        if(Instance == null) Instance = this;
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#if ENABLE_INPUT_SYSTEM
        _playerInput = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        
    }
    
    public GameObject AttachToPoint(GameObject loot)
    {
        GameObject go = Instantiate(loot, grabbedLootConnectPoint.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        heldLoot = go;
        thirdPersonController.ToggleCarrying(true);
        lastClosestInteractable = null;

        return go;
    }

    public void DestroyHeldObject()
    {
        Destroy(heldLoot);
        heldLoot = null;
        thirdPersonController.ToggleCarrying(false);
        lastClosestInteractable = null;
    }
    
    void Update()
    {
        if (!IsOwner) return;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2.0f, layerMask);
        float minDistance = Mathf.Infinity;
        Collider closestCollider = null;

        //Calculate closest interactable
        foreach (Collider collider in hitColliders)
        {
            // Exclude self if the script is on an object with a collider
            if (collider.gameObject == gameObject) continue;
            if (heldLoot != null && heldLoot.gameObject == collider.gameObject) continue;

            float distance = Vector3.Distance(transform.position, collider.transform.position); 

            if (distance < minDistance)
            {
                minDistance = distance;
                closestCollider = collider;
            }
        }
        
        if (closestCollider != null)
        {
            GameObject parentHitObject = closestCollider.gameObject;
            // Expects collider reference
            if (closestCollider.GetComponent<ColliderReference>() != null)
            {
                parentHitObject = closestCollider.GetComponent<ColliderReference>().reference;
            }
            IInteractable interactable = parentHitObject.GetComponent<IInteractable>();
            if (interactable != null && interactable != lastClosestInteractable)
            {
                if (interactable.gameObject.GetComponent<LootDeposit>() != null && heldLoot != null)
                {
                    interactable.SetAsInteractable(true);
                    if (lastClosestInteractable != null)
                    {
                        lastClosestInteractable.SetAsInteractable(false);
                    }
                    lastClosestInteractable = interactable;
                }
                else if(heldLoot == null)
                {
                    interactable.SetAsInteractable(true);
                    if (lastClosestInteractable != null)
                    {
                        lastClosestInteractable.SetAsInteractable(false);
                    }
                    lastClosestInteractable = interactable;
                }
                
            }
        }
        else
        {
            if (lastClosestInteractable != null)
            {
                lastClosestInteractable.SetAsInteractable(false);
                lastClosestInteractable = null;
            }
        }

        if (_input.interact)
        {
            //Turn it off immediately so we don't get double events
            _input.interact = false;
            
            NetworkLoot loot = null;
            LootDeposit deposit = null;
            
            if (lastClosestInteractable != null)
            {
                loot = lastClosestInteractable.gameObject.GetComponent<NetworkLoot>();
                deposit = lastClosestInteractable.gameObject.GetComponent<LootDeposit>();
            }
            
            
            if (heldLoot != null)
            {
                if (deposit != null)
                {
                    LootManager.Instance.RequestDeposit(deposit, heldLoot);
                }
                else
                {
                    Physics.Raycast(heldLoot.transform.position, -Vector3.up, out RaycastHit hit);
                    if (hit.collider != null)
                    {
                        if (hit.distance < MAX_DROP_DISTANCE)
                        {
                            LootManager.Instance.RequestDrop(
                                new Vector3(hit.point.x, hit.point.y + 0.3f, hit.point.z), heldLoot);
                        }
                    }
                }
            }
            else
            {
                if (loot != null)
                {
                    LootManager.Instance.RequestPickup(loot);
                    lastClosestInteractable = null;
                }
                else
                {
                    if (lastClosestInteractable != null)
                    {
                        lastClosestInteractable.Interact();
                    }
                }
            }
            
        }
    }
}
