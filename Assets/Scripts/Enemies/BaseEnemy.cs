using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseEnemy : NetworkBehaviour, IDamagable
{
    [SerializeField] protected Animator _animator;
    [SerializeField] protected PlayerAudioSource _audioSource;
    [SerializeField] protected List<int> _lootDroppedOnDeath = new List<int>();
    
    protected Vector3 nextPosition;
    protected Vector3 lastPosition;
    protected Vector3 initialPosition;
    protected Quaternion nextRotation;
    protected NetworkVariable<int> _health = new NetworkVariable<int>(0);
    
    
    protected NetworkVariable<int> _networkState = new NetworkVariable<int>(0);
    public int Health => _health.Value;
    protected ClientStateMachine _stateMachine;
    
    public override void OnNetworkSpawn()
    {
        InitializeStateMachine();
        
        if (IsServer)
        {
            InitializeServerValues();
        }

        _networkState.OnValueChanged += OnNetworkStateUpdated;
        _health.OnValueChanged += OnHealthUpdated;
        
        //Sync network state to whatever server is on
        OnNetworkStateUpdated(0, _networkState.Value);
    }

    protected virtual void OnHealthUpdated(int prev, int next)
    {
        if (Health <= 0)
        {
            StartCoroutine(DoDeath());
        }
    }

    protected virtual IEnumerator DoDeath()
    {
        DropLoot_S();
        yield return null;
    }

    protected virtual void DropLoot_S()
    {
        if (IsServer)
        {
            foreach (var loot in _lootDroppedOnDeath)
            {
                LootManager.Instance.SpawnAndLoadLoot_S(transform.position, loot);
            }
        }
        
    }

    public virtual void OnNetworkStateUpdated(int prev, int next)
    {
        _stateMachine.ChangeState(next);
    }
    
    public void DoDamage(ItemInteractionData interactionData)
    {
        ReceiveDamage_ServerRpc(interactionData.damage);
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void ReceiveDamage_ServerRpc(int damage)
    {
        _health.Value -= damage;
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
    
    protected void SetClosestMovingPlayer(float maxSafeSpeed, out NetworkClient closestPlayer, out float closestDistance)
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        closestDistance = float.MaxValue;
        closestPlayer = null;
        foreach (var client in connectedClients)
        {
            var player = client.Value.PlayerObject;
            PlayerStateData playerState  = player.GetComponent<PlayerStateData>();
            //Debug.Log(playerState.Velocity);
            if (playerState == null) continue;
            if (playerState.Velocity <= maxSafeSpeed) continue;
            float currentDistance = Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position);
            if (currentDistance < closestDistance)
            {
                closestDistance = currentDistance;
                closestPlayer = client.Value;
            }
        }
    }

    protected void SetClosestHoldingPlayer(out NetworkClient closestPlayer, out float closestDistance)
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        closestDistance = float.MaxValue;
        closestPlayer = null;
        foreach (var client in connectedClients)
        {
            var player = client.Value.PlayerObject;
            var playerState = player.GetComponent<PlayerStateData>();
            if (playerState == null || !playerState.DisplayingHeldObject) continue;
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
