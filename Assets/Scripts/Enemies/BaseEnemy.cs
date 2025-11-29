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
    
    #region Utility Functions

    protected Collider GetCurrentWaterBody()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.1f);
        Collider collider = null;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].CompareTag("Water"))
            {
                collider = colliders[i];
            }
        }

        return collider;
    }

    protected void SetClosestPlayer(out NetworkClient closestPlayer, out float closestDistance)
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        closestDistance = float.MaxValue;
        closestPlayer = null;
        foreach (var client in connectedClients)
        {
            float currentDistance = Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position);
            if (currentDistance < closestDistance)
            {
                closestDistance = currentDistance;
                closestPlayer = client.Value;
            }
        }
    }

    #endregion
}
