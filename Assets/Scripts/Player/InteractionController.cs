using System;
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
    
    [SerializeField] private LayerMask interactableLayerMask;
    [SerializeField] private LayerMask buildingLayerMask;
    [SerializeField] private Transform grabbedLootConnectPoint;
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerState playerState;

    private float rotationPlaceOffset = 0.0f;
    
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
    private IInteractable persistentInteractable;
    private IHoldable heldObject;
    private BoatAttachment placingBoatAttachment;
    private const float MAX_DROP_DISTANCE = 1.5f;
    private const float MAX_INTERACTION_DISTANCE = 5.0f;
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
        GameObject go = Instantiate(loot, playerController.CameraControl.gameObject.GetComponent<ControlModeData>().lootConnectPoint.transform);
        go.transform.localRotation = Quaternion.identity;
        if (playerController.ControlMode == PlayerController.ControlModeEnum.FirstPerson)
        {
            go.transform.localPosition = new Vector3(0, 0.4f, 0);
            go.transform.localScale = Vector3.one * 0.3f;
        }
        else
        {
            go.transform.localPosition = new Vector3(0, -0.3f, -0.1f);
            go.transform.localScale = Vector3.one * 0.6f;
        }
        heldObject = go.GetComponent<IHoldable>();
        playerController.ToggleCarrying(true);
        DisableCurrentInteractable();
        return go;
    }

    public void DestroyHeldObject()
    {
        Destroy(heldObject.gameObject);
        heldObject = null;
        playerController.ToggleCarrying(false);
        DisableCurrentInteractable();
    }
    
    private void FixedUpdate()
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

        if (heldObject != null)
        {
            playerState.SwimWeightMultiplier = heldObject.GetWeightMultiplier();
            playerState.SprintWeightMultiplier = heldObject.GetWeightMultiplier();
        }
        else
        {
            playerState.SwimWeightMultiplier = 1.0f;
            playerState.SprintWeightMultiplier = 1.0f;
        }
    }

    private void DoInteractionStandard()
    {
        if (!IsOwner) return;

        Collider[] hitColliders = Array.Empty<Collider>();
        if (playerController.ControlMode == PlayerController.ControlModeEnum.ThirdPerson)
        {
            hitColliders = Physics.OverlapSphere(transform.position, 2.0f, interactableLayerMask);
        } else if (playerController.ControlMode == PlayerController.ControlModeEnum.FirstPerson)
        {
            Physics.Raycast(
                playerController.MainCamera.transform.position, 
                playerController.MainCamera.transform.forward,
                out RaycastHit raycastHit,
                MAX_INTERACTION_DISTANCE,
                interactableLayerMask);
            
            if (raycastHit.collider != null)
            {
                hitColliders = new Collider[1];
                hitColliders[0] = raycastHit.collider;
            }
            else
            {
                hitColliders = Array.Empty<Collider>();
            }
        }
        
       
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

        if (closestCollider != null && minDistance < MAX_INTERACTION_DISTANCE)
        {
            GameObject parentHitObject = closestCollider.gameObject;
            // Expects collider reference
            ColliderReference colliderReference = closestCollider.GetComponent<ColliderReference>();
            if (colliderReference != null)
            {
                if (colliderReference.reference != null)
                {
                    parentHitObject = colliderReference.reference;
                }
            }
            IInteractable interactable = parentHitObject.GetComponent<IInteractable>();
            if (interactable != null && interactable != lastClosestInteractable)
            {
                bool isInteractable = interactable.EnableInteractable(heldObject);

                if (persistentInteractable == null)
                {
                    if (isInteractable)
                    {
                        if (lastClosestInteractable != null)
                        {
                            lastClosestInteractable.DisableInteractable();
                        }
                
                        lastClosestInteractable = interactable;
                    }
                }
            }
        }
        else
        {
            DisableCurrentInteractable();
        }
       

        if (_input.interact)
        {
            //Turn it off immediately so we don't get double events
            _input.interact = false;

            if (persistentInteractable != null)
            {
                
                persistentInteractable.Interact(NetworkManager.Singleton.LocalClientId);
                persistentInteractable = null;
                DisableCurrentInteractable();
            }
            else
            {
                NetworkLoot loot = null;
                LootDeposit deposit = null;
                
                if (lastClosestInteractable != null)
                {
                    loot = lastClosestInteractable.gameObject.GetComponent<NetworkLoot>();
                    deposit = lastClosestInteractable.gameObject.GetComponent<LootDeposit>();
                }
                
                if (heldObject != null)
                {
                    if (heldObject.HeldObjectType == HeldObjectType.CraneHook)
                    {
                        AttachmentCrane crane = heldObject.ConnectedParent.GetComponent<AttachmentCrane>();
                        if (loot != null)
                        {
                            crane.AttachCraneHook(NetworkManager.Singleton.LocalClientId, loot.transform, loot.NetworkObject);
                        }
                        else
                        {
                            crane.DropCraneHook(NetworkManager.Singleton.LocalClientId);
                        }
                    }
                    else if (heldObject.HeldObjectType == HeldObjectType.Loot)
                    {
                        if (deposit != null)
                        {
                            LootManager.Instance.RequestDeposit(deposit, heldObject.gameObject);
                            deposit.DisableInteractable();
                        }
                        else
                        {
                            Physics.Raycast(heldObject.gameObject.transform.position, -Vector3.up, out RaycastHit hit);
                            LootManager.Instance.RequestDrop(
                                heldObject.gameObject.transform.position, heldObject.gameObject);
                        }
                    } 
                }
                else
                {
                    if (loot != null)
                    {
                        LootData data = LootManager.Instance.LootIndextoData(loot.lootIndex.Value);
                        if (data.lootType == LootType.Heavy)
                        {
                            loot.SetAsTooHeavy();
                        }
                        else
                        {
                            DisableCurrentInteractable();
                            LootManager.Instance.RequestPickup(loot);
                        }
                    }
                    else
                    {
                        if (lastClosestInteractable != null)
                        {
                            if (lastClosestInteractable.IsPersistentInteractable)
                            {
                                
                                persistentInteractable = lastClosestInteractable;
                            }
                            
                            lastClosestInteractable.Interact(NetworkManager.Singleton.LocalClientId);
                            
                        }
                    }
                }
            }
        }
    }

    private void DisableCurrentInteractable()
    {
        if (lastClosestInteractable != null)
        {
            lastClosestInteractable.DisableInteractable();
            lastClosestInteractable = null;
        }
    }

    private void DoInteractionBuilding()
    {
        if (!IsOwner) return;

        Collider[] hitColliders = Array.Empty<Collider>();
        if (playerController.ControlMode == PlayerController.ControlModeEnum.ThirdPerson)
        {
            hitColliders = Physics.OverlapSphere(transform.position, 2.0f, interactableLayerMask);
        } else if (playerController.ControlMode == PlayerController.ControlModeEnum.FirstPerson)
        {
            Physics.Raycast(
                playerController.MainCamera.transform.position, 
                playerController.MainCamera.transform.forward,
                out RaycastHit raycastHit,
                MAX_INTERACTION_DISTANCE,
                buildingLayerMask);
            
            if (raycastHit.collider != null)
            {
                hitColliders = new Collider[1];
                hitColliders[0] = raycastHit.collider;
            }
            else
            {
                hitColliders = Array.Empty<Collider>();
            }
            
        }
        
        float minDistance = Mathf.Infinity;
        Collider closestCollider = null;

        //Calculate closest interactable
        foreach (Collider collider in hitColliders)
        {
            // Exclude self if the script is on an object with a collider
            if (collider.gameObject == gameObject) continue;
            if (heldObject != null && heldObject.gameObject == collider.gameObject) continue;

            float distance = Vector3.Distance(transform.position, collider.transform.position); 

            if (distance < minDistance && collider.CompareTag("AttachmentPoint"))
            {
                minDistance = distance;
                closestCollider = collider;
            }
        }

        if (closestCollider != null)
        {
            Debug.Log(closestCollider.ToString());
            
            GameObject parentHitObject = closestCollider.gameObject;
            ColliderReference colliderReference = closestCollider.GetComponent<ColliderReference>();
            // Expects collider reference
            if (colliderReference != null)
            {
                if(colliderReference.reference != null)
                    parentHitObject = closestCollider.GetComponent<ColliderReference>().reference;
            }
            
            BoatAttachmentPoint boatAttachmentPoint = parentHitObject.GetComponentInChildren<BoatAttachmentPoint>();
            if (boatAttachmentPoint != null && boatAttachmentPoint.heldItem.Value == 0)
            {
                placingBoatAttachment.transform.position = boatAttachmentPoint.transform.position;
                placingBoatAttachment.transform.rotation = boatAttachmentPoint.transform.rotation;
                placingBoatAttachment.transform.Rotate(boatAttachmentPoint.transform.up, rotationPlaceOffset);
                placingBoatAttachment.gameObject.SetActive(true);
                
                if (_input.interact)
                {
                    // TODO dont allow you to place if theres something already attached
                    Destroy(placingBoatAttachment.gameObject);
                    GameManager.Instance.PlaceAttachmentPoint(_shopItemIndex, boatAttachmentPoint, rotationPlaceOffset);
                    placingBoatAttachment = null;
                    CurrentInteractionMode = InteractionMode.Default;
                    _input.interact = false;
                }

                if (_input.respawn)
                {
                    rotationPlaceOffset += 90;
                    rotationPlaceOffset = rotationPlaceOffset % 360;
                    _input.respawn = false;
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
        rotationPlaceOffset = 0.0f;
    }
}
