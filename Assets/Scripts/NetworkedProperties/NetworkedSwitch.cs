using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NetworkedSwitch : NetworkBehaviour, IInteractable
{

    [SerializeField] private GameObject instructions;
    [SerializeField] private UnityEvent onInteract;
    
    private ClientStateMachine _stateMachine;

    enum States
    {
        Default = 0,
        ClosestItem = 1
    };
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, OnDefaultStateUpdate, OnDefaultStateExit);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, OnClosestItemStateUpdate, OnClosestItemStateExit);
        _stateMachine.AddState((int)States.ClosestItem, closestItemState);
        
        _stateMachine.ChangeState((int)States.Default);
    }

    public void Interact()
    {
        onInteract.Invoke();
    }
    
    #region States

    private void OnDefaultStateEnter()
    {
        instructions.SetActive(false);
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
    
    #endregion

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
}
