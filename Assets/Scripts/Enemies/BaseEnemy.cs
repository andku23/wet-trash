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
    
    // Position Data for next position
    protected Vector3 targetPosition_s;
    protected Vector3 lastPosition_s;
    protected Vector3 initialPosition_s;
    
    // Time Data for next position
    protected float startTime_s;
    protected float currentTime_s;
    protected float speed_s = 1.0f;
    protected float expectedTime_s;
    
    protected float lastAttackTime_s;
    
    // Current target state of enemy
    protected NetworkObject _targetPlayer_s;
    protected PlayerStateData _targetPlayerState_s;
    protected Collider _currentWaterBody_s;
    
    protected float MAX_FOLLOW_DISTANCE = 10.0f;
    protected float MAX_ATTACK_DISTANCE = 1.0f;
    protected float ATTACK_COOLDOWN_TIME = 2.0f;
    protected float SWIM_SPEED_IDLE = 3.5f;
    protected float SWIM_SPEED_CHASING = 6.0f;
    
    // Health
    protected NetworkVariable<int> _health = new NetworkVariable<int>(0);
    public int Health => _health.Value;
    protected Coroutine _deathCoroutine;
    
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

    public virtual void OnNetworkStateUpdated(int prev, int next)
    {
        _stateMachine.ChangeState(next);
    }
    
    public virtual void InitializeStateMachine()
    {
        _stateMachine = new ClientStateMachine();
    }
    
    public virtual void InitializeServerValues_S()
    {
        initialPosition_s = transform.position;
        _currentWaterBody_s = GetCurrentWaterBody();
    }

    [ServerRpc(RequireOwnership = false)]
    protected void ChangeState_ServerRpc(int newState)
    {
        _networkState.Value = newState;
    }
    
    #region Enemy Take Damage
    
    public void DoDamage(ItemInteractionData interactionData)
    {
        ReceiveDamage_ServerRpc(interactionData.damage);
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void ReceiveDamage_ServerRpc(int damage)
    {
        _health.Value -= damage;
    }
    
    protected virtual void OnHealthUpdated(int prev, int next)
    {
        if (_deathCoroutine != null) return;
        if (Health <= 0)
        {
            _deathCoroutine = StartCoroutine(DoDeath());
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
    
    #endregion
    
    #region Player Damage
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
        _animator_c.SetTrigger("DoAttack");
        _audioSource_c.PlaySound(PlayerAudioSource.SoundType.EnemyDoDamage);
        if (NetworkManager.Singleton.LocalClientId == networkPlayerID)
        {
            StartCoroutine(InflictLocalPlayerDamage(2));
        }
    }
    
    #endregion
    
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
    
    #region Behaviours Functions

    protected void SetNextStaticTargetPosition(Vector3 nextPosition)
    {
        startTime_s = Time.time;
        currentTime_s = Time.time;
        lastPosition_s = transform.position;
        targetPosition_s = nextPosition;
        expectedTime_s = Vector3.Distance(lastPosition_s, targetPosition_s) / speed_s;
    }

    protected void SetTargetPlayer(NetworkObject player)
    {
        _targetPlayer_s = player;
        _targetPlayerState_s = player.GetComponent<PlayerStateData>();
    }

    protected void SwimToNextStaticTargetPosition()
    {
        currentTime_s += Time.deltaTime;
        transform.position = Vector3.Lerp(lastPosition_s, targetPosition_s,
            (currentTime_s - startTime_s)/ expectedTime_s);
        
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetPosition_s - transform.position), 0.3f);
    }
    
    protected void SwimAtTargetPlayer()
    {
        if (_targetPlayer_s == null) return;
        
        float distance = Vector3.Distance(transform.position, _targetPlayer_s.transform.position);
        float expectedDistanceTraveled = Time.deltaTime * speed_s;
        transform.position = Vector3.Lerp(transform.position, _targetPlayer_s.transform.position,
            expectedDistanceTraveled/distance);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(_targetPlayer_s.transform.position - transform.position), 0.3f);
    }

    protected bool IsTargetPlayerUnreachable(float disengageDistance, out float distanceToPlayer)
    {
        float distance = Vector3.Distance(transform.position, _targetPlayer_s.transform.position);
        distanceToPlayer = distance;
        
        Debug.Log("distance: " + distance + " disengage: " + disengageDistance);
        
        if (_targetPlayer_s == null) return true;
        if (distance > disengageDistance) return true;
        if (_currentWaterBody_s != null &&
            !_currentWaterBody_s.bounds.Contains(_targetPlayer_s.transform.position)) return true;
        if (_targetPlayerState_s.Health.Value <= 0) return true;
        
        return false;
    }
    
    #endregion
    
    #region Behaviour States

    protected void DefaultAttackBehaviour()
    {
        if (Time.time - lastAttackTime_s > ATTACK_COOLDOWN_TIME)
        {
            Debug.Log("attack");
            lastAttackTime_s = Time.time;
            DoAttack_ServerRpc(_targetPlayer_s.OwnerClientId); // Sort of a hack to find client id, but should work
        }
    }
    #endregion
    
    protected virtual void Update()
    {
        if(_stateMachine != null)
            _stateMachine.Update();
    }
}
