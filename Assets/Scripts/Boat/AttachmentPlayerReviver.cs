using Unity.Netcode;
using UnityEngine;

public class AttachmentPlayerReviver : NetworkBehaviour, IInteractable, IAttachment
{
    [SerializeField] private WorldspaceInstruction worldspaceInstruction;
 
    private ClientStateMachine _stateMachine;
    private NetworkVariable<int> _networkedState = new NetworkVariable<int>(0);
    
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
        
        BaseState defaultState = new BaseState(OnDefaultState_Enter, OnDefaultState_Update, OnDefaultState_Exit);
        _stateMachine.AddState((int)States.Default, defaultState);
       
        
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
    
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        worldspaceInstruction.SetVisible(true);
        return true;
    }
    
    public void DisableInteractable()
    {
        worldspaceInstruction.SetVisible(false);
    }

    public void OnDefaultState_Enter()
    {
        
    }
    
    public void OnDefaultState_Update()
    {
        
    }
    
    public void OnDefaultState_Exit()
    {
        
    }
    
    private void Update()
    {
        _stateMachine.Update();
    }
}
