using StarterAssets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InteractionController : NetworkBehaviour
{
    public static InteractionController Instance;
    
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Transform grabbedLootConnectPoint;
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private ThirdPersonController thirdPersonController;
    
    public InteractionMode CurrentInteractionMode;

    public enum InteractionMode
    {
        Default = 0,
        BoatBuilding = 1
    }
    
#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif
    
    private StarterAssetsInputs _input;
    private IInteractable lastClosestInteractable;
    private GameObject heldObject;
    private BoatAttachment placingBoatAttachment;
    private const float MAX_DROP_DISTANCE = 1.5f;
    private int _shopItemIndex;
    
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
        heldObject = go;
        thirdPersonController.ToggleCarrying(true);
        lastClosestInteractable = null;

        return go;
    }

    public void DestroyHeldObject()
    {
        Destroy(heldObject);
        heldObject = null;
        thirdPersonController.ToggleCarrying(false);
        lastClosestInteractable = null;
    }
    
    private void Update()
    {
        switch (CurrentInteractionMode)
        {
            case InteractionMode.Default:
                DoInteractionStandard();
                break;
            case InteractionMode.BoatBuilding:
                DoInteractionBuilding();
                break;
        }
    }

    private void DoInteractionStandard()
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
            if (heldObject != null && heldObject.gameObject == collider.gameObject) continue;

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
                if (interactable.gameObject.GetComponent<LootDeposit>() != null && heldObject != null)
                {
                    interactable.SetAsInteractable(true);
                    if (lastClosestInteractable != null)
                    {
                        lastClosestInteractable.SetAsInteractable(false);
                    }
                    lastClosestInteractable = interactable;
                }
                else if(heldObject == null)
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
            
            
            if (heldObject != null)
            {
                if (deposit != null)
                {
                    LootManager.Instance.RequestDeposit(deposit, heldObject);
                }
                else
                {
                    Physics.Raycast(heldObject.transform.position, -Vector3.up, out RaycastHit hit);
                    if (hit.collider != null)
                    {
                        if (hit.distance < MAX_DROP_DISTANCE)
                        {
                            LootManager.Instance.RequestDrop(
                                new Vector3(hit.point.x, hit.point.y + 0.3f, hit.point.z), heldObject);
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

    private void DoInteractionBuilding()
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
            if (heldObject != null && heldObject.gameObject == collider.gameObject) continue;

            float distance = Vector3.Distance(transform.position, collider.transform.position); 

            if (distance < minDistance)
            {
                minDistance = distance;
                closestCollider = collider;
            }
        }

        if (closestCollider != null && closestCollider.CompareTag("AttachmentPoint"))
        {
            GameObject parentHitObject = closestCollider.gameObject;
            // Expects collider reference
            if (closestCollider.GetComponent<ColliderReference>() != null)
            {
                parentHitObject = closestCollider.GetComponent<ColliderReference>().reference;
            }
            
            BoatAttachmentPoint boatAttachmentPoint = parentHitObject.GetComponentInChildren<BoatAttachmentPoint>();
            if (boatAttachmentPoint != null && boatAttachmentPoint.heldItem.Value == 0)
            {
                placingBoatAttachment.transform.position = boatAttachmentPoint.transform.position;
                placingBoatAttachment.transform.rotation = boatAttachmentPoint.transform.rotation;
                placingBoatAttachment.gameObject.SetActive(true);
                
                if (_input.interact)
                {
                    // TODO dont allow you to place if theres something already attached
                    Destroy(placingBoatAttachment.gameObject);
                    GameManager.Instance.PlaceAttachmentPoint(_shopItemIndex, boatAttachmentPoint);
                    placingBoatAttachment = null;
                    CurrentInteractionMode = InteractionMode.Default;
                    _input.interact = false;
                }
            }
            else
            {
                placingBoatAttachment.gameObject.SetActive(false);
            }
        }
        else
        {
            placingBoatAttachment.gameObject.SetActive(false);
        }
    }

    public void ChangeToBuildMode(int shopItemIndex)
    {
        CurrentInteractionMode = InteractionMode.BoatBuilding;
        _shopItemIndex = shopItemIndex;
        placingBoatAttachment = Instantiate(ShopManager.Instance.shopList.items[_shopItemIndex].placePrefab).GetComponent<BoatAttachment>();
        placingBoatAttachment.gameObject.SetActive(false);
    }
}
