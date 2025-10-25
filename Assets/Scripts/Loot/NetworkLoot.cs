using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkLoot : NetworkBehaviour, IInteractable
{
    [SerializeField] private GameObject instructions;
    [SerializeField] private GameObject dropInstructions;
    [SerializeField] private GameObject heavyInstructions;
    [SerializeField] private GameObject craneHookInstructions;
    
    public NetworkVariable<int> lootIndex;
    private ClientStateMachine _stateMachine;

    enum LocalStates
    {
        Default = 0,
        ClosestItem = 1,
        PickedUp = 2,
        TooHeavy = 3,
        ClosestItemCrane = 4
    };
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        GetComponent<LootInstanceData>().LoadLootNetwork(lootIndex.Value);
        
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
    }

    public bool EnableInteractable(IHoldable heldObject)
    {
        bool isInteractable = false;
        if (heldObject != null)
        {
            if (heldObject.HeldObjectType == HeldObjectType.CraneHook)
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
        instructions.SetActive(false);
        dropInstructions.SetActive(false);
        dropInstructions.SetActive(false);
    }
    
    private void OnClosestItemStateEnter()
    {
        instructions.SetActive(true);
    }
    
    private void OnClosestItemStateExit()
    {
        instructions.SetActive(false);
    }
    
    private void OnPickedUpStateEnter()
    {
        dropInstructions.SetActive(true);
    }
    
    private void OnPickedUpStateExit()
    {
        dropInstructions.SetActive(false);
    }
    
    private void OnHeavyStateEnter()
    {
        heavyInstructions.SetActive(true);
    }
    
    private void OnHeavyStateExit()
    {
        heavyInstructions.SetActive(false);
    }
    
    private void OnCranePickupStateEnter()
    {
        craneHookInstructions.SetActive(true);
    }
    
    private void OnCranePickupStateExit()
    {
        craneHookInstructions.SetActive(false);
    }
    
    #endregion
    
    private void Update()
    {
        _stateMachine.Update();
    }
}
