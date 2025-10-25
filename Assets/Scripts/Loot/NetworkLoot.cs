using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkLoot : NetworkBehaviour, IInteractable
{
    [SerializeField] private GameObject instructions;
    [SerializeField] private GameObject dropInstructions;
    [SerializeField] private GameObject heavyInstructions;
    
    public NetworkVariable<int> lootIndex;
    private ClientStateMachine _stateMachine;

    enum States
    {
        Default = 0,
        ClosestItem = 1,
        PickedUp = 2,
        TooHeavy = 3
    };
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        GetComponent<LootInstanceData>().LoadLootNetwork(lootIndex.Value);
        
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, OnDefaultStateUpdate, OnDefaultStateExit);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, OnClosestItemStateUpdate, OnClosestItemStateExit);
        _stateMachine.AddState((int)States.ClosestItem, closestItemState);
        
        BaseState pickedUpState = new BaseState(OnPickedUpStateEnter, OnPickedUpStateUpdate, OnPickedUpStateExit);
        _stateMachine.AddState((int)States.PickedUp, pickedUpState);
        
        BaseState heavyState = new BaseState(OnHeavyStateEnter, OnHeavyStateUpdate, OnHeavyStateExit);
        _stateMachine.AddState((int)States.TooHeavy, heavyState);
        
        _stateMachine.ChangeState((int)States.Default);
    }

    public void SetAsInteractable(bool isInteractable)
    {
        if (isInteractable)
        {
            _stateMachine.ChangeState((int)States.ClosestItem);
        }
        else
        {
            _stateMachine.ChangeState((int)States.Default);
        }
    }

    public void SetAsTooHeavy()
    {
        _stateMachine.ChangeState((int)States.TooHeavy);
    }
    
    #region States

    private void OnDefaultStateEnter()
    {
        instructions.SetActive(false);
        dropInstructions.SetActive(false);
        dropInstructions.SetActive(false);
    }
    
    private void OnDefaultStateUpdate()
    {
        
    }
    
    private void OnDefaultStateExit()
    {
        
    }
    
    private void OnClosestItemStateEnter()
    {
        instructions.SetActive(true);
    }
    
    private void OnClosestItemStateUpdate()
    {
    }
    
    private void OnClosestItemStateExit()
    {
        instructions.SetActive(false);
    }
    
    private void OnPickedUpStateEnter()
    {
        dropInstructions.SetActive(true);
    }
    
    private void OnPickedUpStateUpdate()
    {
    }
    
    private void OnPickedUpStateExit()
    {
        dropInstructions.SetActive(false);
    }
    
    private void OnHeavyStateEnter()
    {
        heavyInstructions.SetActive(true);
    }
    
    private void OnHeavyStateUpdate()
    {
    }
    
    private void OnHeavyStateExit()
    {
        heavyInstructions.SetActive(false);
    }
    
    #endregion
    
    private void Update()
    {
        _stateMachine.Update();
    }
}
