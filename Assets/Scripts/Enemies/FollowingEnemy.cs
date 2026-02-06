using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class FollowingEnemy : BaseEnemy
{
    [SerializeField] private float travelTime = 2.0f;
    [SerializeField] private float minimumFollowDistance = 10.0f;
    [SerializeField] private float minimumAttackDistance = 1.0f;
    [SerializeField] private float attackCooldownTime = 1.0f;
    [SerializeField] private float swimSpeed = 3.5f;
    
    enum ServerStates
    {
        Idle = 0,
        FollowingPlayer = 1,
        AttackingPlayer = 2,
        Death = 3
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
        _animator_c.SetBool("IsDead", true);
        _audioSource_c.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
        if (IsServer)
        {
            ChangeState_ServerRpc((int)ServerStates.Death);
            DropLoot_S();
            yield return new WaitForSeconds(1.5f);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    
    protected override void OnHealthUpdated(int prev, int next)
    {
        base.OnHealthUpdated(prev, next);
        _audioSource_c.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
    }
    
    #region States

    private void Idle_OnEnter()
    {
        if (!IsServer) return;
        targetPosition_s = transform.position;
        lastPosition_s = transform.position;
    }
    
    private void Idle_Update()
    {
        if (!IsServer) return;
        FindClosestPlayer(out var closestPlayer, out var closestDistance);
        _targetPlayer_s = closestPlayer.PlayerObject;
        _targetPlayerState_s = _targetPlayer_s.GetComponent<PlayerStateData>();

        if (_targetPlayer_s != null && closestDistance < minimumFollowDistance * GameManager.Instance.gameData.MONSTER_DETECTION_RANGE_MULTIPLIER && 
            _targetPlayerState_s.Health.Value > 0 && _currentWaterBody_s != null &&
            _currentWaterBody_s.bounds.Contains(_targetPlayer_s.transform.position))
        {
            ChangeState_ServerRpc((int)ServerStates.FollowingPlayer);
        }
        else if (Vector3.Distance(gameObject.transform.position, targetPosition_s) <= 0.1f)
        {
            lastPosition_s = targetPosition_s;
            targetPosition_s = new Vector3(
                Random.Range(-3, 3) + initialPosition_s.x,
                Random.Range(-3, 3) + initialPosition_s.y,
                Random.Range(-3, 3) + initialPosition_s.z
            );
            startTime_s = Time.time;
        }
        else
        {
            transform.position = Vector3.Lerp(lastPosition_s, targetPosition_s, (Time.time - startTime_s)/travelTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetPosition_s - transform.position), 3.0f * Time.deltaTime);
        }
    }
    
    private void FollowingPlayer_OnEnter()
    {
    }
    
    private void FollowingPlayer_Update()
    {
        if (!IsServer) return;
        float disengageDistance =
            minimumFollowDistance * GameManager.Instance.gameData.MONSTER_DETECTION_RANGE_MULTIPLIER;
        bool isPlayerUnreachable = IsTargetPlayerUnreachable(disengageDistance, out var distanceToPlayer);

        if (isPlayerUnreachable)
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else
        {
            if (distanceToPlayer < minimumAttackDistance)
            {
                ChangeState_ServerRpc((int)ServerStates.AttackingPlayer);
            }
            else
            {
                SwimAtTargetPlayer();
            }
        }
    }
    
    private void AttackingPlayer_OnEnter()
    {
        
    }

    private float _lastAttackTime = 0f;
    
    private void AttackingPlayer_Update()
    {
        if (!IsServer) return;
        float disengageDistance =
            minimumAttackDistance * GameManager.Instance.gameData.MONSTER_DETECTION_RANGE_MULTIPLIER;
        bool isPlayerUnreachable = IsTargetPlayerUnreachable(disengageDistance, out var distanceToPlayer);

        if (isPlayerUnreachable)
        {
            ChangeState_ServerRpc((int)ServerStates.Idle);
        }
        else if (Time.time - _lastAttackTime > attackCooldownTime)
        {
            _lastAttackTime = Time.time;
            DoAttack_ServerRpc(_targetPlayer_s.OwnerClientId); // Sort of a hack to find client id, but should work
        }
    }
    
    private void Death_OnEnter()
    {
        
    }
    
    #endregion
    
}
