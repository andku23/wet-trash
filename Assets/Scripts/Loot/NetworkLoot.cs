using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkLoot : NetworkBehaviour
{
    [SerializeField] private GameObject instructions;
    [SerializeField] private GameObject dropInstructions;

    private ClientStateMachine _stateMachine;

    enum States
    {
        Default = 0,
        ClosestItem = 1,
        PickedUp = 2
    };
    
    public override void OnNetworkSpawn()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, OnDefaultStateUpdate, OnDefaultStateExit);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, OnClosestItemStateUpdate, OnClosestItemStateExit);
        _stateMachine.AddState((int)States.ClosestItem, closestItemState);
        
        BaseState pickedUpState = new BaseState(OnPickedUpStateEnter, OnPickedUpStateUpdate, OnPickedUpStateExit);
        _stateMachine.AddState((int)States.PickedUp, pickedUpState);
        
        _stateMachine.ChangeState((int)States.Default);
        
        base.OnNetworkSpawn();
    }

    public void TrySetClosestLoot(bool isClosest)
    {
        if (isClosest)
        {
            _stateMachine.ChangeState((int)States.ClosestItem);
        }
        else
        {
            _stateMachine.ChangeState((int)States.Default);
        }
    }
    
    #region States

    private void OnDefaultStateEnter()
    {
        instructions.SetActive(false);
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
    
    #endregion
    
    private void Update()
    {
        _stateMachine.Update();
    }
}
