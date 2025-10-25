using Unity.Netcode;
using UnityEngine;

public class AttachmentCrane : NetworkBehaviour, IInteractable
{
    [SerializeField] private GameObject useInstructions;
    [SerializeField] private GameObject craneHookParent;
    
    [SerializeField] private GameObject craneHookPrefab;
    
    private ClientStateMachine _stateMachine;
    
    enum States
    {
        Default = 0,
        ClosestItem = 1,
        HookHeld = 2
    };
    
    public override void OnNetworkSpawn()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, OnDefaultStateUpdate, OnDefaultStateExit);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, OnClosestItemStateUpdate, OnClosestItemStateExit);
        _stateMachine.AddState((int)States.ClosestItem, closestItemState);
        
        BaseState hookHeldState = new BaseState(OnHookHeldStateEnter, OnHookHeldStateUpdate, OnHookHeldStateExit);
        _stateMachine.AddState((int)States.HookHeld, hookHeldState);
        
        _stateMachine.ChangeState((int)States.Default);
        
        Instantiate(craneHookPrefab, craneHookParent.transform);
    }

    public void Interact(ulong networkPlayerID)
    {
        if (_stateMachine.currentStateEnum == (int)States.ClosestItem)
        {
            _stateMachine.ChangeState((int)States.HookHeld);
            Interact_ServerRpc(networkPlayerID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void Interact_ServerRpc(ulong networkPlayerID)
    {
        Interact_ClientRpc(networkPlayerID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void Interact_ClientRpc(ulong networkPlayerID)
    {
        craneHookParent.SetActive(false);
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[networkPlayerID].PlayerObject;
        InteractionController interactionController = playerObject.GetComponent<InteractionController>();
        if (interactionController != null)
        {
            GameObject craneHookInstantiated = interactionController.AttachToPoint(craneHookPrefab);
            IHoldable heldObject = craneHookInstantiated.GetComponent<IHoldable>();
            if (heldObject != null)
            {
                heldObject.ConnectedParent = gameObject;
            }
        }
    }

    public void DropCraneHook(ulong networkPlayerID)
    {
        DropCraneHook_ServerRpc(networkPlayerID);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void DropCraneHook_ServerRpc(ulong networkPlayerID)
    {
        DropCraneHook_ClientRpc(networkPlayerID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void DropCraneHook_ClientRpc(ulong networkPlayerID)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[networkPlayerID].PlayerObject;
        InteractionController interactionController = playerObject.GetComponent<InteractionController>();
        _stateMachine.ChangeState((int)States.Default);
        craneHookParent.SetActive(true);
        if (interactionController != null)
        {
            interactionController.DestroyHeldObject();
        }
    }

    public void SetAsInteractable(bool isInteractable)
    {
        if(_stateMachine.currentStateEnum == (int)States.HookHeld) return;
        if (isInteractable)
        {
            _stateMachine.ChangeState((int)States.ClosestItem);
        }
        else
        {
            _stateMachine.ChangeState((int)States.Default);
        }
    }
    
    private void OnDefaultStateEnter()
    {
        useInstructions.SetActive(false);
    }
    
    private void OnDefaultStateUpdate()
    {
        
    }
    
    private void OnDefaultStateExit()
    {
        
    }
    
    private void OnClosestItemStateEnter()
    {
        useInstructions.SetActive(true);
    }
    
    private void OnClosestItemStateUpdate()
    {
    }
    
    private void OnClosestItemStateExit()
    {
        useInstructions.SetActive(false);
    }
    
    private void OnHookHeldStateEnter()
    {
        useInstructions.SetActive(false);
    }
    
    private void OnHookHeldStateUpdate()
    {
    }
    
    private void OnHookHeldStateExit()
    {
    }
}
