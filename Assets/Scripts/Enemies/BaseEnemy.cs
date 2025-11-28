using Unity.Netcode;
using UnityEngine;

public abstract class BaseEnemy : NetworkBehaviour
{
    [SerializeField] protected Animator _animator;
    [SerializeField] protected PlayerAudioSource _audioSource;
    
    protected Vector3 nextPosition;
    protected Vector3 lastPosition;
    protected Vector3 initialPosition;
    protected Quaternion nextRotation;
    
    protected NetworkVariable<int> _networkState = new NetworkVariable<int>(0);
    protected ClientStateMachine _stateMachine;
    
    public override void OnNetworkSpawn()
    {
        InitializeStateMachine();
        
        if (IsServer)
        {
            InitializeServerValues();
        }

        _networkState.OnValueChanged += OnNetworkStateUpdated;
        
        //Sync network state to whatever server is on
        OnNetworkStateUpdated(0, _networkState.Value);
    }

    public virtual void OnNetworkStateUpdated(int prev, int next)
    {
        _stateMachine.ChangeState(next);
    }

    public virtual void InitializeStateMachine()
    {
        _stateMachine = new ClientStateMachine();
    }
    
    public virtual void InitializeServerValues()
    {
        initialPosition = transform.position;
    }

    [ServerRpc(RequireOwnership = false)]
    protected void ChangeState_ServerRpc(int newState)
    {
        _networkState.Value = newState;
    }
    
    protected virtual void Update()
    {
        if(_stateMachine != null)
            _stateMachine.Update();
    }
}
