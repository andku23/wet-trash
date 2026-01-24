using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class BaseEnemy : NetworkBehaviour, IDamagable
{
    
    [Header("Internal Components")]
    [SerializeField] protected Animator _animator_c;
    [SerializeField] protected PlayerAudioSource _audioSource_c;
    
    [Space(10)]
    [Header("Raycast Masks")]
    [SerializeField] protected LayerMask _playerMask;
    
    [Space(10)]
    [Header("Data")]
    [SerializeField] protected List<int> _lootDroppedOnDeath_c = new List<int>();
    
    // Position Data
    protected Vector3 targetPosition_s;
    protected Vector3 lastPosition_s;
    protected Vector3 initialPosition_s;
    protected Quaternion nextRotation_s;
    
    // Health
    protected NetworkVariable<int> _health = new NetworkVariable<int>(0);
    public int Health => _health.Value;
    
    // Spawn Area
    protected SpawnAreaType _spawnArea_s;
    public SpawnAreaType SpawnArea_S {get => _spawnArea_s; set => _spawnArea_s = value; }
    
    // State Machine
    protected NetworkVariable<int> _networkState = new NetworkVariable<int>(0);
    protected ClientStateMachine _stateMachine;

    public enum SpawnAreaType
    {
        Overworld,
        Dungeon
    }
    
    public override void OnNetworkSpawn()
    {
        InitializeStateMachine();
        
        if (IsServer)
        {
            InitializeServerValues_S();
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
            foreach (var loot in _lootDroppedOnDeath_c)
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
    
    public virtual void InitializeServerValues_S()
    {
        initialPosition_s = transform.position;
    }

    [ServerRpc(RequireOwnership = false)]
    protected void ChangeState_ServerRpc(int newState)
    {
        _networkState.Value = newState;
    }
    
    protected IEnumerator InflictLocalPlayerDamage(int damage)
    {
        yield return new WaitForSeconds(1.5f);
        var playerState = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerStateData>();
        GameManager.Instance.ChangeHealth(NetworkManager.Singleton.LocalClientId, playerState.Health.Value - damage);
    }
    
    [ServerRpc(RequireOwnership = false)]
    protected void DoAttack_ServerRpc(ulong networkPlayerID)
    {
        DoAttack_ClientRpc(networkPlayerID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    protected void DoAttack_ClientRpc(ulong networkPlayerID)
    {
        _animator_c.SetTrigger("Attack");
        _audioSource_c.PlaySound(PlayerAudioSource.SoundType.EnemyDoDamage);
        if (NetworkManager.Singleton.LocalClientId == networkPlayerID)
        {
            StartCoroutine(InflictLocalPlayerDamage(2));
        }
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

    protected void FindClosestPlayer(out NetworkClient closestPlayer, out float closestDistance)
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
    
    protected void FindClosestMovingPlayer(float maxSafeSpeed, out NetworkClient closestPlayer, out float closestDistance)
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

    protected void FindClosestHoldingPlayer(out NetworkClient closestPlayer, out float closestDistance)
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
    
    protected NetworkObject FindPlayersInLOS(Vector3 startPoint, float radius, float distance)
    {
        Vector3 start = startPoint;
        NetworkObject foundPlayer = null;
        if (Physics.SphereCast(start, radius, transform.forward, out var hit, distance, _playerMask))
        {
            foundPlayer = hit.transform.gameObject.GetComponent<NetworkObject>();
        }
        return foundPlayer;
    }

    #endregion
}
