using Unity.Netcode;
using UnityEngine;

public class AttachmentElevator : NetworkBehaviour, IInteractable, IAttachment
{
    [SerializeField] private WorldspaceInstruction worldspaceInstruction;
    [SerializeField] private GameObject platform;
    
    [SerializeField] private float speed;
    
    private ClientStateMachine _stateMachine;
    private NetworkVariable<int> _networkedState = new NetworkVariable<int>(0);
    
    public int State {get {return _networkedState.Value;}}
    
    public enum States
    {
        Default = 0,
        Descending = 1,
        Down = 2,
        Ascending = 3,
    };
    
    private void Start()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(Default_Enter, Default_Update, null);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState descendingState = new BaseState(Descending_Enter, Descending_Update, null);
        _stateMachine.AddState((int)States.Descending, descendingState);
        
        BaseState downState = new BaseState(Down_Enter, Down_Update, null);
        _stateMachine.AddState((int)States.Down, downState);
        
        BaseState ascendingState = new BaseState(Ascending_Enter, Ascending_Update, null);
        _stateMachine.AddState((int)States.Ascending, ascendingState);
       
        _stateMachine.ChangeState(_networkedState.Value);
        _networkedState.OnValueChanged += OnNetworkStateUpdated;
        
        _upHeight = platform.transform.position.y;
    }
    
    public void OnNetworkStateUpdated(int prev, int next)
    {
        if (prev != next)
        {
            _stateMachine.ChangeState(next);
        }
    }
    
    #region Interactable
    
    public void Interact(ulong networkPlayerID, InteractionButtonType buttonType)
    {
        switch ((States)State)
        {
            case States.Default:
                ChangeState_ServerRpc(NetworkManager.LocalClientId, States.Descending);
                break;
            case States.Down:
                ChangeState_ServerRpc(NetworkManager.LocalClientId, States.Ascending);
                break;
            case States.Ascending:
            case States.Descending:
            default:
                break;
        }
    }

    public bool EnableInteractable(IHoldable heldObject)
    {
        bool isInteractable = false;
        switch ((States)State)
        {
            case States.Default:
                worldspaceInstruction.SetText("'E' to send down");
                isInteractable = true;
                break;
            case States.Down:
                worldspaceInstruction.SetText("'E' to send up");
                isInteractable = true;
                break;
            case States.Ascending:
            case States.Descending:
            default:
                worldspaceInstruction.SetText("Wait until finished");
                isInteractable = false;
                break;
        }
        worldspaceInstruction.SetVisible(isInteractable);
        return isInteractable;
    }
    
    public void DisableInteractable()
    {
        worldspaceInstruction.SetVisible(false);
    }

    #endregion
    
    #region RPC
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeState_ServerRpc(ulong networkPlayerID, States state)
    {
        _networkedState.Value = (int)state;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncPlatformPosition_ServerRpc(float height)
    {
        SyncPlatformPosition_ClientRpc(height);
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void SyncPlatformPosition_ClientRpc(float height)
    {
        Vector3 pos = platform.transform.position;
        pos.y = height;
        platform.transform.position = pos;
    }
    
    #endregion
    
    #region States

    private float _upHeight;
    private float _downHeight;

    private void Default_Enter()
    {
        if (IsServer)
        {
            SyncPlatformPosition_ServerRpc(_upHeight);
        }
    }
    
    private void Default_Update(){}

    private void Descending_Enter()
    {
        //_downHeight = platform.transform.position.y;
    }

    private void Descending_Update()
    {
        if (IsServer)
        {
            if (platform.transform.position.y <= -10f)
            {
                _networkedState.Value = (int)States.Down;
            }
        }

        platform.transform.Translate(0, -Time.deltaTime * speed, 0);
    }
    
    private void Ascending_Enter(){}

    private void Ascending_Update()
    {
        if (IsServer)
        {
            if (platform.transform.position.y >= _upHeight)
            {
                _networkedState.Value = (int)States.Default;
            }
        }

        platform.transform.Translate(0, Time.deltaTime * speed, 0);
    }

    private void Down_Enter()
    {
        if (IsServer)
        {
            SyncPlatformPosition_ServerRpc(platform.transform.position.y);
        }
    }
    
    private void Down_Update(){}
    
    #endregion

    private void Update()
    {
        _stateMachine.Update();
    }
}
