using Unity.Netcode;
using UnityEngine;

public class AttachmentAirSuit : NetworkBehaviour, IInteractable, IAttachment
{
    [SerializeField] private WorldspaceInstruction worldspaceInstruction;
    [SerializeField] private float breathPerPump = 1;
    
    private ClientStateMachine _stateMachine;
    private NetworkVariable<int> _networkedState = new NetworkVariable<int>(0);
    
    private NetworkVariable<bool> isPlayerWearing = new NetworkVariable<bool>();
    private NetworkVariable<bool> isPlayerPumping = new NetworkVariable<bool>();
    private NetworkVariable<ulong> playerWearing = new NetworkVariable<ulong>();
    private NetworkVariable<ulong> playerPumping = new NetworkVariable<ulong>();

    public bool IsPlayerWearing { get => isPlayerWearing.Value; private set => isPlayerWearing.Value = value; }
    public bool IsPlayerPumping { get => isPlayerPumping.Value; private set => isPlayerPumping.Value = value; }
    public ulong PlayerWearing { get => playerWearing.Value; private set => playerWearing.Value = value; }
    public ulong PlayerPumping { get => playerPumping.Value; private set => playerPumping.Value = value; }
    
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

    public bool IsPersistentInteractable
    {
        get
        {
            return State == (int) States.Wearing;
        }
        set
        {
        }
    }

    public void Interact(ulong networkPlayerID, InteractionButtonType buttonType)
    {
        if (State == (int)States.Default)
        {
            if (buttonType == InteractionButtonType.Interact)
            {
                MountSuit_ServerRpc(NetworkManager.LocalClientId);
            }
        }
        else if(State == (int)States.Wearing)
        {
            if (buttonType == InteractionButtonType.Interact)
            {
                InteractWearing_ServerRpc(NetworkManager.LocalClientId);
            }
        }
        else if(State == (int)States.Pumping)
        {
            if (buttonType == InteractionButtonType.Interact)
            {
                InteractPumping(NetworkManager.LocalClientId);
            } else if (buttonType == InteractionButtonType.Use)
            {
                PumpAir_ServerRpc(NetworkManager.LocalClientId);
            }
        }
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        switch ((States)State)
        {
            case States.Pumping:
                worldspaceInstruction.SetVisible(false);
                return false;
            case States.Wearing:
                if (NetworkManager.LocalClientId != PlayerWearing)
                {
                    worldspaceInstruction.SetVisible(true);
                    return true;
                }
                else
                {
                    worldspaceInstruction.SetVisible(false);
                    return false;
                }
            default:
                worldspaceInstruction.SetVisible(true);
                return true;
        }
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
        if (networkPlayerID == PlayerWearing)
        {
            UnmountSuit_ServerRpc(networkPlayerID);
        }
        else
        {
            MountPump_ServerRpc(networkPlayerID);
        }
    }
    
    public void InteractPumping(ulong networkPlayerID)
    {
        if (networkPlayerID == PlayerWearing)
        {
            UnmountSuit_ServerRpc(networkPlayerID);
        }
        else if (networkPlayerID == PlayerPumping)
        {
            UnmountPump_ServerRpc(networkPlayerID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void MountSuit_ServerRpc(ulong networkPlayerID)
    {
        if (State != (int)States.Default) return;
        PlayerWearing = networkPlayerID;
        IsPlayerWearing = true;
        _networkedState.Value = (int)States.Wearing;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void UnmountSuit_ServerRpc(ulong networkPlayerID)
    {
        if (IsPlayerWearing)
        {
            IsPlayerWearing = false;
        }
        
        if(IsPlayerPumping)
            UnmountPump_ServerRpc(PlayerPumping);
        
        _networkedState.Value = (int)States.Default;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void MountPump_ServerRpc(ulong networkPlayerID)
    {
        if (State != (int)States.Wearing) return;
        PlayerPumping = networkPlayerID;
        IsPlayerPumping = true;
        _networkedState.Value = (int)States.Pumping;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void UnmountPump_ServerRpc(ulong networkPlayerID)
    {
        if (State != (int)States.Pumping) return;
        if(PlayerPumping != networkPlayerID) return;
        IsPlayerPumping = false;
        _networkedState.Value = (int)States.Wearing;
        //TODO disconnect logic here
        UnmountPump_ClientRpc(PlayerPumping);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void UnmountPump_ClientRpc(ulong targetPlayerID)
    {
        if(NetworkManager.LocalClientId != targetPlayerID) return;
        InteractionController.Instance.DisconnectFromPersistentInteractable();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PumpAir_ServerRpc(ulong networkPlayerID)
    {
        PumpAir_ClientRpc(PlayerWearing);
    }
    
    [ClientRpc]
    public void PumpAir_ClientRpc(ulong targetPlayer)
    {
        if(targetPlayer != NetworkManager.LocalClientId) return;
        NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerStateController>().ChangeBreathByAmount(breathPerPump);
    }
    
    #endregion
    
    #region States

    private void OnDefaultState_Enter()
    {
        worldspaceInstruction.SetVisible(true);
        worldspaceInstruction.SetText("'E' to wear");
    }
    
    private void OnWearingState_Enter()
    {
        if (PlayerWearing == NetworkManager.LocalClientId)
        {
            worldspaceInstruction.SetVisible(false);
        }
        else
        {
            worldspaceInstruction.SetVisible(true);
            worldspaceInstruction.SetText("'E' to start pumping");
        }
        
    }
    
    private void OnPumpingState_Enter()
    {
        worldspaceInstruction.SetVisible(false);
    }
    
    #endregion

    // Update is called once per frame
    private void Update()
    {
        _stateMachine.Update();
    }
}
