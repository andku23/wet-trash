using Unity.Netcode;
using UnityEngine;

public class AttachmentCrane : NetworkBehaviour, IInteractable
{
    [SerializeField] private GameObject useInstructions;
    [SerializeField] private GameObject reelInstructions;
    [SerializeField] private GameObject craneHookParent;
    [SerializeField] private GameObject craneHookPrefab;
    
    private ClientStateMachine _stateMachine;
    private NetworkVariable<int> _networkedState = new NetworkVariable<int>(0);
    private NetworkObject _hookedLoot = null;
    private GameObject _craneHookOnLoot = null;
    private GameObject _localHookedLoot = null;
    
    enum LocalStates
    {
        Default = 0,
        ClosestItem = 1,
        HookHeld = 2,
        ClosestItemCrane = 6
    };

    enum NetworkStates
    {
        Default = 0,
        HookHeld = 2,
        AttachedToLoot = 3,
        ReelingIn = 4,
        ReelFinished = 5
    }

    private void Start()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, null, null);
        _stateMachine.AddState((int)LocalStates.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, null, OnClosestItemStateExit);
        _stateMachine.AddState((int)LocalStates.ClosestItem, closestItemState);
        
        BaseState hookHeldState = new BaseState(OnHookHeldStateEnter, null, OnHookHeldStateExit);
        _stateMachine.AddState((int)LocalStates.HookHeld, hookHeldState);
        
        BaseState attachedToLootState = new BaseState(OnHookAttachedEnter, null, null);
        _stateMachine.AddState((int)NetworkStates.AttachedToLoot, attachedToLootState);
        
        BaseState reelingInState = new BaseState(OnReelingInStateEnter, OnReelingInStateUpdate, null);
        _stateMachine.AddState((int)NetworkStates.ReelingIn, reelingInState);
        
        BaseState reelFinishedState = new BaseState(OnReelFinishedStateEnter, null, null);
        _stateMachine.AddState((int)NetworkStates.ReelFinished, reelFinishedState);
        
        BaseState closestItemCraneState = new BaseState(OnClosestItemCraneStateEnter, null, OnClosestItemCraneStateExit);
        _stateMachine.AddState((int)LocalStates.ClosestItemCrane, closestItemCraneState);
        
        _stateMachine.ChangeState(_networkedState.Value);
        _networkedState.OnValueChanged += OnNetworkStateUpdated;
        
        
    }

    public override void OnNetworkSpawn()
    {
        Instantiate(craneHookPrefab, craneHookParent.transform);
        GameManager.Instance.TimeFinishedEvent.AddListener(ResetCrane);
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
            HoldHook_ServerRpc(networkPlayerID);
        }
        else if(_stateMachine.currentStateEnum == (int)LocalStates.ClosestItemCrane)
        {
            ReelIn_ServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void HoldHook_ServerRpc(ulong networkPlayerID)
    {
        _networkedState.Value = (int)NetworkStates.HookHeld;
        HoldHook_ClientRpc(networkPlayerID);
    }

    [ClientRpc(RequireOwnership = false)]
    public void HoldHook_ClientRpc(ulong networkPlayerID)
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

    public void AttachCraneHook(ulong networkPlayerID, Transform attachPoint, NetworkObject networkLoot)
    {
        
        AttachCraneHook_ServerRpc(networkPlayerID, attachPoint.position, networkLoot.NetworkObjectId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void AttachCraneHook_ServerRpc(ulong networkPlayerID, Vector3 attachPoint, ulong lootNetworkObjectID)
    {
        _networkedState.Value = (int)NetworkStates.AttachedToLoot;
        _hookedLoot = NetworkManager.Singleton.SpawnManager.SpawnedObjects[lootNetworkObjectID];
        IInteractable interactableObject = _hookedLoot.GetComponent<IInteractable>();
        if (interactableObject != null)
        {
            interactableObject.IsInteractionLocked = true;
            interactableObject.DisableInteractable();
        }
        
        AttachCraneHook_ClientRpc(networkPlayerID, attachPoint, lootNetworkObjectID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void AttachCraneHook_ClientRpc(ulong networkPlayerID, Vector3 attachPoint, ulong lootNetworkObjectID)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[networkPlayerID].PlayerObject;
        InteractionController interactionController = playerObject.GetComponent<InteractionController>();
        NetworkObject hookedLoot = NetworkManager.Singleton.SpawnManager.SpawnedObjects[lootNetworkObjectID];
        if (interactionController != null)
        {
            interactionController.DestroyHeldObject();
        }
        
        ICranable cranableObject = hookedLoot.GetComponent<ICranable>();
        if (cranableObject != null)
        {
            _craneHookOnLoot = Instantiate(craneHookPrefab, hookedLoot.transform);
            _craneHookOnLoot.transform.position = cranableObject.GetAttachPoint();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ReelIn_ServerRpc()
    {
        _networkedState.Value = (int)NetworkStates.ReelingIn;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ReelFinished_ServerRpc()
    {
        _networkedState.Value = (int)NetworkStates.ReelFinished;
        _hookedLoot.transform.position = craneHookParent.transform.position;
        int lootIndex = _hookedLoot.GetComponent<NetworkLoot>().lootIndex.Value;
        LootManager.Instance.DespawnLoot_Server(_hookedLoot);
        MoneyManager.Instance.AddCash(LootManager.Instance.LootIndextoData(lootIndex).price);
        ReelFinished_ClientRpc(lootIndex);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void ReelFinished_ClientRpc(int lootIndex)
    {
        LootData lootData = LootManager.Instance.LootIndextoData(lootIndex);
        _localHookedLoot = Instantiate(lootData.model, craneHookParent.transform);
    }
    
    public void ResetCrane()
    {
        _stateMachine.ChangeState((int)LocalStates.Default);
        if(IsServer) _networkedState.Value = (int)NetworkStates.Default;
        if (_localHookedLoot != null)
        {
            Destroy(_localHookedLoot.gameObject);
        }
    }

    public bool EnableInteractable(IHoldable heldObject)
    {
        if (heldObject != null) return false;
        if(_networkedState.Value == (int)NetworkStates.HookHeld) return false;
        else if(_networkedState.Value == (int)NetworkStates.ReelingIn) return false;
        else if(_networkedState.Value == (int)NetworkStates.ReelFinished) return false;
        if (_networkedState.Value == (int)NetworkStates.AttachedToLoot)
        {
            _stateMachine.ChangeState((int)LocalStates.ClosestItemCrane);
        }
        else
        {
            _stateMachine.ChangeState((int)LocalStates.ClosestItem);
        }
        
        return true;
    }
    
    public void DisableInteractable()
    {
        if (_stateMachine.currentStateEnum == (int)LocalStates.ClosestItem)
        {
            _stateMachine.ChangeState((int)LocalStates.Default);
        } else if (_stateMachine.currentStateEnum == (int)LocalStates.ClosestItemCrane)
        {
            _stateMachine.ChangeState((int)NetworkStates.AttachedToLoot);
        }
    }
    
    private void OnDefaultStateEnter()
    {
        useInstructions.SetActive(false);
        craneHookParent.SetActive(true);
    }

    private void OnHookAttachedEnter()
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
        
    }
    
    private void OnClosestItemCraneStateEnter()
    {
        reelInstructions.SetActive(true);
    }
    
    private void OnClosestItemCraneStateExit()
    {
        reelInstructions.SetActive(false);
    }

    private float timeStart;
    private float totalDistance;
    private const float pullRate = 2.0f;
    private Vector3 pullStartPosition = new Vector3();
    private void OnReelingInStateEnter()
    {
        if (IsServer)
        {
            timeStart = Time.time;
            totalDistance = Vector3.Distance(craneHookParent.transform.position, _hookedLoot.transform.position);
            pullStartPosition = _hookedLoot.transform.position;
        }
    }
    
    private void OnReelingInStateUpdate()
    {
        if (IsServer)
        {
            // Debug.Log(((Time.time - timeStart) * pullRate) / totalDistance);
            _hookedLoot.transform.position = Vector3.Lerp(pullStartPosition, craneHookParent.transform.position, ((Time.time - timeStart) * pullRate) / totalDistance);
            if (Vector3.Distance(_hookedLoot.transform.position, craneHookParent.transform.position) < 0.01f)
            {
                ReelFinished_ServerRpc();
            }
        }
    }
    
    private void OnReelFinishedStateEnter()
    {
        craneHookParent.SetActive(true);
    }

    private void Update()
    {
        _stateMachine.Update();
    }
}
