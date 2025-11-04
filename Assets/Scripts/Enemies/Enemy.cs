using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    private Vector3 nextPosition;
    private Vector3 lastPosition;
    private Vector3 initialPosition;
    private Quaternion nextRotation;
    private NetworkClient _closestPlayer;
    private PlayerState _closestPlayerState;

    private float startTime;
    private float travelTime = 2.0f;
    private float minimumFollowDistance = 10.0f;
    private float minimumAttackDistance = 1.0f;
    private float attackCooldownTime = 1.0f;
    private float swimSpeed = 3.5f;
    private ClientStateMachine _serverStateMachine;
    private NetworkVariable<int> _networkState = new NetworkVariable<int>(0);
    private Collider _currentWaterBody;
    
    [SerializeField] private Animator _animator;
    
    enum ServerStates
    {
        Idle = 0,
        FollowingPlayer = 1,
        AttackingPlayer = 2
    };

    public override void OnNetworkSpawn()
    {
        _serverStateMachine = new ClientStateMachine();
        
        BaseState idle = new BaseState(Idle_OnEnter, Idle_Update, null);
        _serverStateMachine.AddState((int)ServerStates.Idle, idle);
        
        BaseState followingPlayer = new BaseState(FollowingPlayer_OnEnter, FollowingPlayer_Update, null);
        _serverStateMachine.AddState((int)ServerStates.FollowingPlayer, followingPlayer);
        
        BaseState attackingPlayer = new BaseState(AttackingPlayer_OnEnter, AttackingPlayer_Update, null);
        _serverStateMachine.AddState((int)ServerStates.AttackingPlayer, attackingPlayer);

        ChangeState(ServerStates.Idle);

        if (IsServer)
        {
            initialPosition = transform.position;
            Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].CompareTag("Water"))
                {
                    _currentWaterBody = colliders[i];
                }
            }
        }
    }
    
    #region States
    
    private void ChangeState(ServerStates newState)
    {
        _serverStateMachine.ChangeState((int)newState);
        _networkState.Value = (int)newState;
    }

    private void Idle_OnEnter()
    {
        if (!IsServer) return;
        nextPosition = transform.position;
        lastPosition = transform.position;
    }
    
    private void Idle_Update()
    {
        if (!IsServer) return;
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        _closestPlayer = null;
        float closestDistance = float.MaxValue;
        foreach (var client in connectedClients)
        {
            float currentDistance = Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position);
            if (currentDistance < closestDistance)
            {
                closestDistance = currentDistance;
                _closestPlayer = client.Value;
                _closestPlayerState = client.Value.PlayerObject.GetComponent<PlayerState>();
            }
        }

        if (_closestPlayer != null && closestDistance < minimumFollowDistance && _closestPlayerState.Health.Value > 0)
        {
            ChangeState(ServerStates.FollowingPlayer);
        }
        else if (Vector3.Distance(gameObject.transform.position, nextPosition) <= 0.1f)
        {
            lastPosition = nextPosition;
            nextPosition = new Vector3(
                Random.Range(-3, 3) + initialPosition.x,
                Random.Range(-3, 3) + initialPosition.y,
                Random.Range(-3, 3) + initialPosition.z
            );
            startTime = Time.time;
        }
        else
        {
            transform.position = Vector3.Lerp(lastPosition, nextPosition, (Time.time - startTime)/travelTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(nextPosition - transform.position), 3.0f * Time.deltaTime);
        }
    }
    
    private void FollowingPlayer_OnEnter()
    {
        
    }
    
    private void FollowingPlayer_Update()
    {
        if (!IsServer) return;
        float distanceToPlayer = Vector3.Distance(transform.position, _closestPlayer.PlayerObject.transform.position);

        if (_closestPlayer == null)
        {
            ChangeState(ServerStates.Idle);
        }
        else if (!_currentWaterBody.bounds.Contains(_closestPlayer.PlayerObject.transform.position))
        {
            ChangeState(ServerStates.Idle);
        }
        else if (_closestPlayerState.Health.Value <= 0)
        {
            ChangeState(ServerStates.Idle);
        }
        else if (distanceToPlayer < minimumAttackDistance)
        {
            ChangeState(ServerStates.AttackingPlayer);
        }
        else if (distanceToPlayer > minimumFollowDistance)
        {
            ChangeState(ServerStates.Idle);
        }
        else
        {
            float distanceToTravel = swimSpeed * Time.deltaTime;
            float distanceTotal = Vector3.Distance(transform.position, _closestPlayer.PlayerObject.transform.position);
            transform.position = Vector3.Lerp(transform.position, _closestPlayer.PlayerObject.transform.position, distanceToTravel/distanceTotal);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(_closestPlayer.PlayerObject.transform.position - transform.position), 0.3f);
        }
    }
    
    private void AttackingPlayer_OnEnter()
    {
        
    }

    private float _lastAttackTime = 0f;
    
    private void AttackingPlayer_Update()
    {
        if (!IsServer) return;
        float distanceToPlayer = Vector3.Distance(transform.position, _closestPlayer.PlayerObject.transform.position);

        if (_closestPlayer == null)
        {
            ChangeState(ServerStates.Idle);
        }
        else if (_closestPlayerState.Health.Value <= 0)
        {
            ChangeState(ServerStates.Idle);
        }
        else if (distanceToPlayer > minimumAttackDistance)
        {
            ChangeState(ServerStates.FollowingPlayer);
        }
        else if (Time.time - _lastAttackTime > attackCooldownTime)
        {
            _lastAttackTime = Time.time;
            DoAttack_ServerRpc(_closestPlayer.ClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoAttack_ServerRpc(ulong networkPlayerID)
    {
        DoAttack_ClientRpc(networkPlayerID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void DoAttack_ClientRpc(ulong networkPlayerID)
    {
        _animator.SetTrigger("Attack");
        if (NetworkManager.Singleton.LocalClientId == networkPlayerID)
        {
            StartCoroutine(InflictDamage(2));
        }
    }

    private IEnumerator InflictDamage(int damage)
    {
        yield return new WaitForSeconds(1.5f);
        var playerState = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerState>();
        GameManager.Instance.ChangeHealth(NetworkManager.Singleton.LocalClientId, playerState.Health.Value - damage);
    }
    
    #endregion
    
    private void Update()
    {
        if(_serverStateMachine != null)
            _serverStateMachine.Update();
    }
}
