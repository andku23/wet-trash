using Unity.Netcode;
using UnityEngine;

public class AttachmentCrane : NetworkBehaviour, IInteractable
{
    [SerializeField] private GameObject useInstructions;
    [SerializeField] private GameObject craneHookParent;
    
    [SerializeField] private GameObject craneHookPrefab;
    
    private ClientStateMachine _stateMachine;
    private NetworkVariable<int> _networkedState = new NetworkVariable<int>(0);
    
    enum LocalStates
    {
        Default = 0,
        ClosestItem = 1,
        HookHeld = 2
    };

    enum NetworkStates
    {
        Default = 0,
        HookHeld = 2,
        AttachedToLoot = 3,
        ReelingIn = 4,
        ReelFinished = 5
    }

    public override void OnNetworkSpawn()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, null, null);
        _stateMachine.AddState((int)LocalStates.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, null, OnClosestItemStateExit);
        _stateMachine.AddState((int)LocalStates.ClosestItem, closestItemState);
        
        BaseState hookHeldState = new BaseState(OnHookHeldStateEnter, null, OnHookHeldStateExit);
        _stateMachine.AddState((int)LocalStates.HookHeld, hookHeldState);
        
        BaseState attachedToLootState = new BaseState(null, null, null);
        _stateMachine.AddState((int)NetworkStates.AttachedToLoot, attachedToLootState);
        
        BaseState reelingInState = new BaseState(null, OnReelingInStateUpdate, null);
        _stateMachine.AddState((int)NetworkStates.ReelingIn, reelingInState);
        
        BaseState reelFinishedState = new BaseState(OnReelFinishedStateEnter, null, null);
        _stateMachine.AddState((int)NetworkStates.ReelFinished, reelFinishedState);

        _networkedState.OnValueChanged += OnNetworkStateUpdated;
        
        _stateMachine.ChangeState(_networkedState.Value);
        
        Instantiate(craneHookPrefab, craneHookParent.transform);
    }

    public void OnNetworkStateUpdated(int prev, int next)
    {
        if (prev != next)
        {
            _stateMachine.ChangeState(next);
        }
    }

    public void Interact(ulong networkPlayerID)
    {
        if (_stateMachine.currentStateEnum == (int)LocalStates.ClosestItem)
        {
            _stateMachine.ChangeState((int)LocalStates.HookHeld);
            Interact_ServerRpc(networkPlayerID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void Interact_ServerRpc(ulong networkPlayerID)
    {
        _networkedState.Value = (int)NetworkStates.HookHeld;
        Interact_ClientRpc(networkPlayerID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void Interact_ClientRpc(ulong networkPlayerID)
    {
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
        _networkedState.Value = (int)NetworkStates.Default;
        DropCraneHook_ClientRpc(networkPlayerID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void DropCraneHook_ClientRpc(ulong networkPlayerID)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[networkPlayerID].PlayerObject;
        InteractionController interactionController = playerObject.GetComponent<InteractionController>();
        if (interactionController != null)
        {
            interactionController.DestroyHeldObject();
        }
    }

    public bool EnableInteractable(IHoldable heldObject)
    {
        if (heldObject != null) return false;
        if(_networkedState.Value == (int)NetworkStates.HookHeld) return false;
        _stateMachine.ChangeState((int)LocalStates.ClosestItem);
        return true;
    }
    
    public void DisableInteractable()
    {
        _stateMachine.ChangeState((int)LocalStates.Default);
    }
    
    private void OnDefaultStateEnter()
    {
        useInstructions.SetActive(false);
    }
    
    
    private void OnClosestItemStateEnter()
    {
        useInstructions.SetActive(true);
    }
    
    private void OnClosestItemStateExit()
    {
        useInstructions.SetActive(false);
    }
    
    private void OnHookHeldStateEnter()
    {
        useInstructions.SetActive(false);
        craneHookParent.SetActive(false);
    }
    
    private void OnHookHeldStateExit()
    {
        craneHookParent.SetActive(true);
    }
    
    private void OnReelingInStateUpdate()
    {
        if (IsServer)
        {
            
        }
    }
    
    private void OnReelFinishedStateEnter()
    {
        
    }
}
