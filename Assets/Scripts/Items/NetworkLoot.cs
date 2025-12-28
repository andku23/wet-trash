using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkLoot : NetworkBehaviour, IInteractable, ICranable
{
    [SerializeField] private WorldspaceInstruction instruction;
    
    public NetworkVariable<int> lootIndex;
    public NetworkVariable<ulong> spawnedPlayerID;
    public NetworkVariable<bool> isInteractionLocked;
    private ClientStateMachine _stateMachine;

    [HideInInspector] public ItemInstance itemInstance;

    private string priceText;
    
    public bool IsInteractionLocked
    {
        get => isInteractionLocked.Value;
        set { isInteractionLocked.Value = value; }
    }

    enum LocalStates
    {
        Default = 0,
        ClosestItem = 1,
        PickedUp = 2,
        TooHeavy = 3,
        ClosestItemCrane = 4
    };

    private void Start()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, null, null);
        _stateMachine.AddState((int)LocalStates.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, null, OnClosestItemStateExit);
        _stateMachine.AddState((int)LocalStates.ClosestItem, closestItemState);
        
        BaseState pickedUpState = new BaseState(OnPickedUpStateEnter, null, OnPickedUpStateExit);
        _stateMachine.AddState((int)LocalStates.PickedUp, pickedUpState);
        
        BaseState heavyState = new BaseState(OnHeavyStateEnter, null, OnHeavyStateExit);
        _stateMachine.AddState((int)LocalStates.TooHeavy, heavyState);
        
        BaseState cranePickupableState = new BaseState(OnCranePickupStateEnter, null, OnCranePickupStateExit);
        _stateMachine.AddState((int)LocalStates.ClosestItemCrane, cranePickupableState);
        
        _stateMachine.ChangeState((int)LocalStates.Default);

        isInteractionLocked.OnValueChanged += OnInteractionLockedChanged;
    }

    void OnDestroy()
    {
        isInteractionLocked.OnValueChanged -= OnInteractionLockedChanged;
        _stateMachine = null;
    }

    private void OnInteractionLockedChanged(bool prev, bool next)
    {
        if (prev != next)
        {
            if (next == false)
            {
                DisableInteractable();
            }
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        itemInstance = GetComponent<ItemInstance>();
        itemInstance.LoadNetwork(lootIndex.Value, spawnedPlayerID.Value);
        var itemData = LootManager.Instance.ItemList.itemDatas[lootIndex.Value];
        string text = "E to Pickup\n";
        foreach (var valueType in itemData.valueRange)
        {
            text += valueType.CurrencyType.ToString() + ": " + valueType.MaxValue + "\n";
        }
        priceText = text;
    }

    public bool EnableInteractable(IHoldable heldObject)
    {
        bool isInteractable = false;
        if (isInteractionLocked.Value) return false;
        if (heldObject != null)
        {
            if (heldObject.HeldObjectType == HeldObjectType.TemporaryHold)
            {
                _stateMachine.ChangeState((int)LocalStates.ClosestItemCrane);
                isInteractable = true;
            }
        } else if (heldObject == null)
        {
            _stateMachine.ChangeState((int)LocalStates.ClosestItem);
            isInteractable = true;
        }
        
        return isInteractable;
    }
    
    public void DisableInteractable()
    {
        _stateMachine.ChangeState((int)LocalStates.Default);
    }

    public void SetAsTooHeavy()
    {
        _stateMachine.ChangeState((int)LocalStates.TooHeavy);
    }
    
    #region States

    private void OnDefaultStateEnter()
    {
        instruction.SetVisible(false);
    }
    
    private void OnClosestItemStateEnter()
    {
        instruction.SetText(priceText);
        instruction.SetVisible(true);
    }
    
    private void OnClosestItemStateExit()
    {
        instruction.SetVisible(false);
    }
    
    private void OnPickedUpStateEnter()
    {
    }
    
    private void OnPickedUpStateExit()
    {
    }
    
    private void OnHeavyStateEnter()
    {
        instruction.SetText("Too Heavy");
        instruction.SetVisible(true);
    }
    
    private void OnHeavyStateExit()
    {
        instruction.SetVisible(false);
    }
    
    private void OnCranePickupStateEnter()
    {
        instruction.SetText("E to attach crane");
        instruction.SetVisible(true);
    }
    
    private void OnCranePickupStateExit()
    {
        instruction.SetVisible(false);
    }
    
    #endregion
    
    private void Update()
    {
        _stateMachine.Update();
    }

    public Vector3 GetAttachPoint()
    {
        if (itemInstance != null && itemInstance.Model != null &&
            itemInstance.Model.CraneAttachPoint != null)
        {
            return itemInstance.Model.CraneAttachPoint.transform.position;
        }
        else
        {
            Vector3 attachPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            attachPoint.y += 1.5f;
            return attachPoint;
        }
    }

    public GameObject GetLocalModel()
    {
        return LootManager.Instance.LootIndextoData(lootIndex.Value).model;
    }
}
