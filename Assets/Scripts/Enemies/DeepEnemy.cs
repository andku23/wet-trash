using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DeepEnemy : BaseEnemy
{
    private NetworkClient _closestPlayer;
    private PlayerState _closestPlayerState;
    private float startTime;
    
    [SerializeField] private float travelTime = 2.0f;
    [SerializeField] private float minimumFollowDistance = 10.0f;
    [SerializeField] private float minimumAttackDistance = 1.0f;
    [SerializeField] private float attackCooldownTime = 1.0f;
    [SerializeField] private float swimSpeed = 3.5f;
    [SerializeField] private float maxSafeSwimSpeed = 0.1f;
    
    private Collider _currentWaterBody;
    
    enum ServerStates
    {
        Idle = 0,
        FollowingPlayer = 1,
        AttackingPlayer = 2,
        Death = 3
    }

    public override void InitializeServerValues()
    {
        base.InitializeServerValues();
        _currentWaterBody = base.GetCurrentWaterBody();
    }

    public override void InitializeStateMachine()
    {
        base.InitializeStateMachine();
        
        BaseState idle = new BaseState(Idle_OnEnter, Idle_Update, null);
        _stateMachine.AddState((int)ServerStates.Idle, idle);
        
        BaseState followingPlayer = new BaseState(FollowingPlayer_OnEnter, FollowingPlayer_Update, null);
        _stateMachine.AddState((int)ServerStates.FollowingPlayer, followingPlayer);
        
        BaseState attackingPlayer = new BaseState(AttackingPlayer_OnEnter, AttackingPlayer_Update, null);
        _stateMachine.AddState((int)ServerStates.AttackingPlayer, attackingPlayer);
        
        BaseState death = new BaseState(Death_OnEnter, null, null);
        _stateMachine.AddState((int)ServerStates.Death, death);
    }
    
    protected override IEnumerator DoDeath()
    {
        _animator.SetBool("IsDead", true);
        _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
        if (IsServer)
        {
            ChangeState_ServerRpc((int)ServerStates.Death);
            yield return new WaitForSeconds(1.5f);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    
    protected override void OnHealthUpdated(int prev, int next)
    {
        base.OnHealthUpdated(prev, next);
        _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
    }
    
    #region States

    private void Idle_OnEnter()
    {
        if (!IsServer) return;
        nextPosition = transform.position;
        lastPosition = transform.position;
    }
    
    private void Idle_Update()
    {
        if (!IsServer) return;
        SetClosestMovingPlayer(maxSafeSwimSpeed ,out var closestPlayer, out var closestDistance);
        _closestPlayer = closestPlayer;

        if (_closestPlayer != null && closestDistance < minimumFollowDistance) 
        {
            _closestPlayerState = _closestPlayer.PlayerObject.GetComponent<PlayerState>();
            if (_closestPlayerState.Health.Value > 0 && _currentWaterBody != null &&
                _currentWaterBody.bounds.Contains(_closestPlayer.PlayerObject.transform.position))
            {
                ChangeState_ServerRpc((int)ServerStates.FollowingPlayer);
            }
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
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (_closestPlayerState.Velocity <= maxSafeSwimSpeed)
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (_currentWaterBody != null && !_currentWaterBody.bounds.Contains(_closestPlayer.PlayerObject.transform.position))
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (_closestPlayerState.Health.Value <= 0)
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (distanceToPlayer < minimumAttackDistance)
        {
            ChangeState_ServerRpc((int)ServerStates.AttackingPlayer);
        }
        else if (distanceToPlayer > minimumFollowDistance)
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
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
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (_closestPlayerState.Velocity <= maxSafeSwimSpeed)
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (_closestPlayerState.Health.Value <= 0)
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (distanceToPlayer > minimumAttackDistance)
        {
            ChangeState_ServerRpc((int)ServerStates.FollowingPlayer);
        }
        else if (Time.time - _lastAttackTime > attackCooldownTime)
        {
            _lastAttackTime = Time.time;
            DoAttack_ServerRpc(_closestPlayer.ClientId);
        }
    }
    
    private void Death_OnEnter()
    {
        
    }
    
    #endregion

    [ServerRpc(RequireOwnership = false)]
    private void DoAttack_ServerRpc(ulong networkPlayerID)
    {
        DoAttack_ClientRpc(networkPlayerID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void DoAttack_ClientRpc(ulong networkPlayerID)
    {
        _animator.SetTrigger("Attack");
        _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyDoDamage);
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
}
