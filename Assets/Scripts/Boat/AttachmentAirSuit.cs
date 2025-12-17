using Unity.Netcode;
using UnityEngine;

public class AttachmentAirSuit : NetworkBehaviour, IInteractable, IAttachment
{
    [SerializeField] private WorldspaceInstruction worldspaceInstruction;
    
    private ClientStateMachine _stateMachine;
    private NetworkVariable<int> _networkedState = new NetworkVariable<int>(0);
    
    private bool isPlayerWearing_S;
    private bool isPlayerPumping_S;
    private ulong playerWearing_S;
    private ulong playerPumping_S;
    
    public int State {get {return _networkedState.Value;}}
    
    public enum States
    {
        Default = 0,
        Wearing = 1,
        Pumping = 2
    };
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultState_Enter, null, null);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState equippedState = new BaseState(OnWearingState_Enter, null, null);
        _stateMachine.AddState((int)States.Wearing, equippedState);
        
        BaseState pumpingState = new BaseState(OnPumpingState_Enter, null, null);
        _stateMachine.AddState((int)States.Pumping, pumpingState);
        
        _stateMachine.ChangeState(_networkedState.Value);
        _networkedState.OnValueChanged += OnNetworkStateUpdated;
    }
    
    public void OnNetworkStateUpdated(int prev, int next)
    {
        if (prev != next)
        {
            _stateMachine.ChangeState(next);
        }
    }
    
    #region Interactable
    public bool IsPersistentInteractable { get => false; set {} }
    public void Interact(ulong networkPlayerID)
    {
        Debug.Log("intearcting");
        if (State == (int)States.Default)
        {
            MountSuit_ServerRpc(NetworkManager.LocalClientId);
        }
        else if(State == (int)States.Wearing)
        {
            InteractWearing_ServerRpc(NetworkManager.LocalClientId);
        }
        else if(State == (int)States.Pumping)
        {
            InteractPumping_ServerRpc(NetworkManager.LocalClientId);
        }
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        if (State == (int)States.Pumping)
        {
            worldspaceInstruction.SetVisible(false);
            return false;
        }
        else
        {
            worldspaceInstruction.SetVisible(true);
        }

       
        return true;
    }
    
    public void DisableInteractable()
    {
        worldspaceInstruction.SetVisible(false);
    }
    
    #endregion
    
    #region Server Logic

    [ServerRpc(RequireOwnership = false)]
    public void InteractWearing_ServerRpc(ulong networkPlayerID)
    {
        if (networkPlayerID == playerWearing_S)
        {
            UnmountSuit_ServerRpc(networkPlayerID);
        }
        else
        {
            MountPump_ServerRpc(networkPlayerID);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void InteractPumping_ServerRpc(ulong networkPlayerID)
    {
        if (networkPlayerID == playerWearing_S)
        {
            UnmountSuit_ServerRpc(networkPlayerID);
        }
        else if (networkPlayerID == playerPumping_S)
        {
            UnmountPump_ServerRpc(networkPlayerID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void MountSuit_ServerRpc(ulong networkPlayerID)
    {
        if (State != (int)States.Default) return;
        playerWearing_S = networkPlayerID;
        isPlayerWearing_S = true;
        _networkedState.Value = (int)States.Wearing;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void UnmountSuit_ServerRpc(ulong networkPlayerID)
    {
        if (isPlayerWearing_S)
        {
            isPlayerWearing_S = false;
        }
        
        if(isPlayerPumping_S)
            UnmountPump_ServerRpc(playerPumping_S);
        
        _networkedState.Value = (int)States.Default;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void MountPump_ServerRpc(ulong networkPlayerID)
    {
        if (State != (int)States.Wearing) return;
        playerPumping_S = networkPlayerID;
        isPlayerPumping_S = true;
        _networkedState.Value = (int)States.Pumping;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void UnmountPump_ServerRpc(ulong networkPlayerID)
    {
        if (State != (int)States.Pumping) return;
        if(playerPumping_S != networkPlayerID) return;
        isPlayerPumping_S = false;
        _networkedState.Value = (int)States.Wearing;
        //TODO disconnect logic here
    }
    
    #endregion
    
    #region States

    private void OnDefaultState_Enter()
    {
        worldspaceInstruction.SetText("'E' to wear");
    }
    
    private void OnWearingState_Enter()
    {
        worldspaceInstruction.SetText("'E' to start pumping");
    }
    
    private void OnPumpingState_Enter()
    {
        
    }
    
    #endregion

    // Update is called once per frame
    private void Update()
    {
        _stateMachine.Update();
    }
}
