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
    
    private UI _ui;

    private const int INVENTORY_SIZE = 4;
    private int[] _inventory = new int[INVENTORY_SIZE];
    private int _currentInventoryIndex = 0;
    
    private float rotationPlaceOffset;

    public enum InteractionStates
    {
        Standard = 0,
        BoatBuilding = 1,
        PersistentInteractable = 2,
        HoldingInventoryObject = 3,
        HoldingTemporaryObject = 4
    }
    
#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif
    
    private StarterAssetsInputs _input;
    private IInteractable lastClosestInteractable;
    private IInteractable persistentInteractable;
    private IHoldable heldObject;
    private BoatAttachment placingBoatAttachment;
    private int _shopItemIndex;
    private TagHandle _interactableTag;
    private TagHandle _buildingTag;
    private ClientStateMachine _stateMachine;
    private InteractableTypes _currentInteractableTypes;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        if(Instance == null) Instance = this;
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        _playerInput = FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        
        _interactableTag = TagHandle.GetExistingTag("Untagged");
        _buildingTag = TagHandle.GetExistingTag("AttachmentPoint");
        
        _stateMachine = new ClientStateMachine();
        
        _stateMachine.AddState((int)InteractionStates.Standard, 
            new BaseState(null, OnStandardUpdate, null));
        
        _stateMachine.AddState((int)InteractionStates.BoatBuilding, 
            new BaseState(null, OnBoatBuildingUpdate, null));
        
        _stateMachine.AddState((int)InteractionStates.PersistentInteractable, 
            new BaseState(null, OnPersistentInteractableUpdate, null));
        
        _stateMachine.AddState((int)InteractionStates.HoldingInventoryObject, 
            new BaseState(null, OnHeldInventoryItemUpdate, null));
        
        _stateMachine.AddState((int)InteractionStates.HoldingTemporaryObject, 
            new BaseState(null, OnHeldTemporaryItemUpdate, null));
        
        _stateMachine.ChangeState((int)InteractionStates.Standard);

        for (int i = 0; i < _inventory.Length; i++)
        {
            _inventory[i] = -1;
        }
    }
    
    // functions that are called from other players or the server
    // or sometimes just other functions
    #region External Calls
    
    public GameObject PickupTemporaryItemNetwork(GameObject item, ulong heldPlayerID)
    {
        GameObject go = Instantiate(item, playerController.CameraControl.gameObject.GetComponent<ControlModeData>().lootConnectPoint.transform);
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
        heldObject.HeldPlayerID = heldPlayerID;
        playerController.ToggleCarrying(true);
        if (heldPlayerID == NetworkManager.Singleton.LocalClientId)
        {
            _stateMachine.ChangeState((int)InteractionStates.HoldingTemporaryObject);
            DisableCurrentInteractable();
        }
        return go;
    }

    public GameObject PickupItemNetwork(int lootIndex, ulong heldPlayerID)
    {
        ChangeHeldObjectLocal(lootIndex, heldPlayerID);
        
        if (heldPlayerID == NetworkManager.Singleton.LocalClientId)
        {
            IInventorable inventorableItem = heldObject.gameObject.GetComponent<IInventorable>();
            if (inventorableItem != null)
            {
                _inventory[_currentInventoryIndex] = lootIndex;
                playerState.WeightCarried += inventorableItem.GetWeight();
                UI.Instance.AddHotbarItem(_currentInventoryIndex, lootIndex);
            }
        }
        
        return heldObject.gameObject;
    }
    
    public void DropTemporaryItemNetwork(ulong heldPlayerID)
    {
        ChangeHeldObjectLocal(_inventory[_currentInventoryIndex], heldPlayerID);
        //RequestChange_ServerRpc(NetworkManager.Singleton.LocalClientId, _inventory[_currentInventoryIndex]);
    }
    
    public void DropItemNetwork(ulong heldPlayerID)
    {
        if (heldPlayerID == NetworkManager.Singleton.LocalClientId)
        {
            _inventory[_currentInventoryIndex] = -1;
            UI.Instance.RemoveHotbarItem(_currentInventoryIndex);
            IInventorable inventorableItem = heldObject.gameObject.GetComponent<IInventorable>();
            if (inventorableItem != null)
            {
                playerState.WeightCarried -= inventorableItem.GetWeight();
            }
        }
        
        ChangeHeldObjectLocal(-1, heldPlayerID);
    }

    // updateLocal flag is for if you want to ignore updating it if its your own
    // item assuming youve already updated it before sending off the request
    public void ChangeHeldObjectLocal(int lootIndex, ulong heldPlayerID)
    {
        if (heldObject != null)
        {
            DestroyHeldObject();
        }
        
        if (lootIndex != -1)
        {
            
            LoadAndAttachHeldObject(lootIndex, heldPlayerID);
            playerController.ToggleCarrying(true);
            if (heldPlayerID == NetworkManager.Singleton.LocalClientId)
            {
                DisableCurrentInteractable();
                _stateMachine.ChangeState((int)InteractionStates.HoldingInventoryObject);
            }
        }
        else
        {
            if (heldPlayerID == NetworkManager.Singleton.LocalClientId)
            {
                _stateMachine.ChangeState((int)InteractionStates.Standard);
            }
            playerController.ToggleCarrying(false);
        }
        UI.Instance.SetActiveHotbarItem(_currentInventoryIndex);
        
    }
    
    public void ChangeHeldObjectNetwork(int lootIndex, ulong heldPlayerID)
    {
        if (heldPlayerID == NetworkManager.Singleton.LocalClientId) return;

        if (heldObject != null)
        {
            DestroyHeldObject();
        }
        
        if (lootIndex != -1)
        {
            LoadAndAttachHeldObject(lootIndex, heldPlayerID);
            playerController.ToggleCarrying(true);
        }
        else
        {
            playerController.ToggleCarrying(false);
        }
    }
    
    public void ChangeToBuildMode(int shopItemIndex)
    {
        _stateMachine.ChangeState((int)InteractionStates.BoatBuilding);
        _shopItemIndex = shopItemIndex;
        placingBoatAttachment = Instantiate(ShopManager.Instance.shopList.items[_shopItemIndex].placePrefab).GetComponent<BoatAttachment>();
        placingBoatAttachment.gameObject.SetActive(false);
        rotationPlaceOffset = 0.0f;
    }

    public void DestroyHeldObject()
    {
        Destroy(heldObject.gameObject);
        heldObject = null;
        playerController.ToggleCarrying(false);
        
        //Might have to figure these out again when doing crane
        //_stateMachine.ChangeState((int)InteractionStates.Standard);
        //DisableCurrentInteractable();
    }
    
    #endregion
    
    #region Networked Functions

    [ServerRpc(RequireOwnership = false)]
    public void RequestChange_ServerRpc(ulong heldPlayerID, int lootIndex)
    {
        RequestChange_ClientRpc(heldPlayerID, lootIndex);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void RequestChange_ClientRpc(ulong heldPlayerID, int lootIndex)
    {
        ChangeHeldObjectNetwork(lootIndex, heldPlayerID);
    }
    
    #endregion

    // functionality called by interaction controller that are used a lot
    #region Internal Utility

    private void CheckItemScroll()
    {
        if (_input.scroll != 0)
        {
            if (_input.scroll > 0)
            {
                IncrementInventoryIndex(1);
                ChangeHeldObjectLocal(_inventory[_currentInventoryIndex], NetworkManager.Singleton.LocalClientId);
                RequestChange_ServerRpc(NetworkManager.Singleton.LocalClientId, _inventory[_currentInventoryIndex]);
            }
            else
            {
                IncrementInventoryIndex(-1);
                ChangeHeldObjectLocal(_inventory[_currentInventoryIndex], NetworkManager.Singleton.LocalClientId);
                RequestChange_ServerRpc(NetworkManager.Singleton.LocalClientId, _inventory[_currentInventoryIndex]);
            }
            _input.scroll = 0;
        }
    }

    private GameObject LoadAndAttachHeldObject(int lootIndex, ulong heldPlayerID)
    {
        GameObject localLootPrefab = LootManager.Instance.ItemList.localLootPrefab;
        GameObject go = Instantiate(localLootPrefab, playerController.CameraControl.gameObject.GetComponent<ControlModeData>().lootConnectPoint.transform);
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
        heldObject.HeldPlayerID = heldPlayerID;
        
        ItemInstance itemInstance = heldObject.gameObject.GetComponent<ItemInstance>();
        itemInstance.LoadLocal(LootManager.Instance.LootIndextoData(lootIndex), lootIndex);
        return go;
    }

    private void IncrementInventoryIndex(int amount)
    {
        _currentInventoryIndex += amount;
        if (_currentInventoryIndex >= _inventory.Length)
        {
            _currentInventoryIndex %= _inventory.Length;
        } else if (_currentInventoryIndex < 0)
        {
            int remainder = -_currentInventoryIndex%_inventory.Length;
            _currentInventoryIndex = _inventory.Length - remainder;
        }
    }
    
    private void DisableCurrentInteractable()
    {
        if (lastClosestInteractable is MonoBehaviour monoBehaviour && monoBehaviour != null
            && lastClosestInteractable != null)
        {
            lastClosestInteractable.DisableInteractable();
            lastClosestInteractable = null;
        }
    }

    private Collider GetClosestCollider(LayerMask layerMask, TagHandle tag)
    {
        Collider[] hitColliders = Array.Empty<Collider>();
        if (playerController.ControlMode == PlayerController.ControlModeEnum.ThirdPerson)
        {
            hitColliders = Physics.OverlapSphere(transform.position, 2.0f, buildingLayerMask);
        } else if (playerController.ControlMode == PlayerController.ControlModeEnum.FirstPerson)
        {
            Physics.Raycast(
                playerController.MainCamera.transform.position, 
                playerController.MainCamera.transform.forward,
                out RaycastHit raycastHit,
                playerState.MAX_INTERACTION_DISTANCE,
                layerMask);
            
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

            if (distance < minDistance && collider.CompareTag(tag))
            {
                minDistance = distance;
                closestCollider = collider;
            }
        }
        
        return closestCollider;
    }

    private void UpdateCurrentInteractable(Collider closestCollider)
    {
        if (closestCollider == null)
        {
            DisableCurrentInteractable();
            return;
        }
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

            if (isInteractable)
            {
                DisableCurrentInteractable();
                lastClosestInteractable = interactable;
            }
        }
    }

    private void QueryInteractableTypes(IInteractable interactable)
    {
        if (interactable != null)
        {
            _currentInteractableTypes.loot = interactable.gameObject.GetComponent<NetworkLoot>();
            _currentInteractableTypes.deposit = interactable.gameObject.GetComponent<LootDeposit>();
        }
    }
    
    #endregion

    // Interaction logic per state
    #region Interaction States
    private void OnStandardUpdate()
    {
        Collider closestCollider = GetClosestCollider(interactableLayerMask, _interactableTag);
        UpdateCurrentInteractable(closestCollider);
        CheckItemScroll();

        if (_input.interact)
        {
            //Turn it off immediately so we don't get double events
            _input.interact = false;
            
            QueryInteractableTypes(lastClosestInteractable);
            
            if (_currentInteractableTypes.loot != null)
            {
                ItemData data = LootManager.Instance.LootIndextoData(_currentInteractableTypes.loot.lootIndex.Value);
                DisableCurrentInteractable();
                LootManager.Instance.RequestPickup(_currentInteractableTypes.loot);
            }
            else
            {
                if (lastClosestInteractable != null)
                {
                    bool isPersistentInteractable = false;
                    if (lastClosestInteractable.IsPersistentInteractable)
                    {
                        persistentInteractable = lastClosestInteractable;
                        isPersistentInteractable = true;
                    }
                    
                    lastClosestInteractable.Interact(NetworkManager.Singleton.LocalClientId);
                    
                    // Have to set this after because sometimes interacting will make it forget about itself
                    if(isPersistentInteractable) _stateMachine.ChangeState((int)InteractionStates.PersistentInteractable);
                }
            }
        }
    }

    private void OnBoatBuildingUpdate()
    {
        if (!IsOwner) return;

        Collider closestCollider = GetClosestCollider(buildingLayerMask, _buildingTag);

        if (closestCollider != null)
        {
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
                    _stateMachine.ChangeState((int)InteractionStates.Standard);
                    _input.interact = false;
                }

                if (_input.respawn)
                {
                    rotationPlaceOffset += 90;
                    rotationPlaceOffset %= 360;
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

    private void OnHeldInventoryItemUpdate()
    {
        Collider closestCollider = GetClosestCollider(interactableLayerMask, _interactableTag);
        UpdateCurrentInteractable(closestCollider);
        QueryInteractableTypes(lastClosestInteractable);
        CheckItemScroll();

        if (_input.interact)
        {
            //Turn it off immediately so we don't get double events
            _input.interact = false;
            
            heldObject.HeldPlayerID = 0;
            if (heldObject.HeldObjectType == HeldObjectType.Inventorable)
            {
                if (_currentInteractableTypes.deposit != null)
                {
                    LootManager.Instance.RequestDeposit(_currentInteractableTypes.deposit, heldObject.gameObject);
                    _currentInteractableTypes.deposit.DisableInteractable();
                }
                else
                {
                    Physics.Raycast(heldObject.gameObject.transform.position, -Vector3.up, out RaycastHit hit);
                    LootManager.Instance.RequestDrop(
                        heldObject.gameObject.transform.position, heldObject.gameObject);
                }
            }
        }
    }
    
    private void OnHeldTemporaryItemUpdate()
    {
        Collider closestCollider = GetClosestCollider(interactableLayerMask, _interactableTag);
        UpdateCurrentInteractable(closestCollider);
        QueryInteractableTypes(lastClosestInteractable);

        if (_input.interact)
        {
            //Turn it off immediately so we don't get double events
            _input.interact = false;
            
            heldObject.HeldPlayerID = 0;
            if (heldObject.HeldObjectType == HeldObjectType.TemporaryHold)
            {
                AttachmentCrane crane = heldObject.ConnectedParent.GetComponent<AttachmentCrane>();
                if (_currentInteractableTypes.loot != null)
                {
                    crane.AttachCraneHook(NetworkManager.Singleton.LocalClientId, 
                        _currentInteractableTypes.loot.transform, _currentInteractableTypes.loot.NetworkObject);
                }
                else
                {
                    crane.DropCraneHook(NetworkManager.Singleton.LocalClientId);
                }
            }
        }
    }
    
    private void OnPersistentInteractableUpdate()
    {
        if (_input.interact)
        {
            _input.interact = false;

            if (persistentInteractable != null)
            {
                persistentInteractable.Interact(NetworkManager.Singleton.LocalClientId);
                persistentInteractable = null;
                _stateMachine.ChangeState((int)InteractionStates.Standard);
                DisableCurrentInteractable();
            }
        }
    }
    #endregion
    
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            _stateMachine.Update();
        }
    }
}

public struct InteractableTypes
{
    public NetworkLoot loot;
    public LootDeposit deposit;
}
