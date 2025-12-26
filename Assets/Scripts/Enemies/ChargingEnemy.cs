using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ChargingEnemy : BaseEnemy
{
    [SerializeField] private Rigidbody _rigidbody;
    
    private NetworkClient _targetPlayer;
    private PlayerStateData _closestPlayerState;
    private Collider _currentWaterBody;
    private float startTime;
    private float idleDistance;
    private bool isChargingUp = false;
    private bool isDoingAttack = false;
    private bool hasDoneDamage = false;

    [SerializeField] private float AGRO_RANGE = 5f;
    [SerializeField] private float IDLE_SPEED = 2.0f;
    [SerializeField] private float ATTACK_COOLDOWN_TIME = 1.0f;
    [SerializeField] private float SWIM_SPEED = 3.5f;
    [SerializeField] private float CHARGE_TIME = 2.0f;
    [SerializeField] private float CHARGE_FORCE = 1.0f;
    [SerializeField] private float COLLISION_RADIUS = 0.5f;
    [SerializeField] private float DAMAGE = 2f;
    
    enum ServerStates
    {
        Idle = 0,
        AttackingPlayer = 1,
        Dead = 2
    }

    public override void InitializeServerValues()
    {
        base.InitializeServerValues();
        _currentWaterBody = base.GetCurrentWaterBody();
    }
    
    protected override IEnumerator DoDeath()
    {
        _animator.SetBool("IsDead", true);
        _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
        if (IsServer)
        {
            ChangeState_ServerRpc((int)ServerStates.Dead);
            DropLoot_S();
            yield return new WaitForSeconds(1.5f);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    
    protected override void OnHealthUpdated(int prev, int next)
    {
        base.OnHealthUpdated(prev, next);
        _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
    }
    
    public override void InitializeStateMachine()
    {
        base.InitializeStateMachine();
        
        BaseState idle = new BaseState(Idle_OnEnter, Idle_Update, null);
        _stateMachine.AddState((int)ServerStates.Idle, idle);
        
        BaseState attackingPlayer = new BaseState(AttackingPlayer_OnEnter, AttackingPlayer_Update, null);
        _stateMachine.AddState((int)ServerStates.AttackingPlayer, attackingPlayer);
        
        BaseState dead = new BaseState(Dead_OnEnter, null, null);
        _stateMachine.AddState((int)ServerStates.Dead, dead);
    }

    private void CheckDamage()
    {
        if (hasDoneDamage) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, COLLISION_RADIUS);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == NetworkManager.Singleton.LocalClient.PlayerObject.gameObject)
            {
                PlayerStateController playerStateController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerStateController>();
                hasDoneDamage = true;
                playerStateController.DoDamage_ServerRpc(DAMAGE);
                _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyDoDamage);
            }
        }
    }
    
    #region States

    private void Idle_OnEnter()
    {
        if (!IsServer) return;
        nextPosition = transform.position;
        lastPosition = transform.position;
        _animator.SetTrigger("DoSwim");
    }
    
    private void Idle_Update()
    {
        if (!IsServer) return;
        SetClosestHoldingPlayer(out var closestPlayer, out var closestDistance);
        _targetPlayer = closestPlayer;

        if (_targetPlayer != null && closestDistance < AGRO_RANGE)
        {
            _closestPlayerState = _targetPlayer.PlayerObject.GetComponent<PlayerStateData>();
            if (_closestPlayerState.Health.Value > 0 && _currentWaterBody != null &&
                _currentWaterBody.bounds.Contains(_targetPlayer.PlayerObject.transform.position))
            {
                ChangeState_ServerRpc((int)ServerStates.AttackingPlayer);
                _animator.SetTrigger("DoCharge");
            }
        }
        else if (Vector3.Distance(gameObject.transform.position, nextPosition) <= 0.1f)
        {
            lastPosition = nextPosition;
            nextPosition = WorldManager.Instance.GetRandomPointNearHotspot();
            startTime = Time.time;
            idleDistance = Vector3.Distance(lastPosition, nextPosition);
        }
        else
        {
            transform.position = Vector3.Lerp(lastPosition, nextPosition, ((Time.time - startTime)*IDLE_SPEED)/idleDistance);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(nextPosition - transform.position), 3.0f * Time.deltaTime);
        }
    }
    
    private void AttackingPlayer_OnEnter()
    {
        isChargingUp = true;
        isDoingAttack = false;
        startTime = Time.time;
        hasDoneDamage = false;
    }
    
    private void AttackingPlayer_Update()
    {
        if (IsServer)
        {
            if (_closestPlayerState.Health.Value <= 0)
            {
                ChangeState_ServerRpc((int)ServerStates.Idle);
            }
            else if (_currentWaterBody != null && !_currentWaterBody.bounds.Contains(_targetPlayer.PlayerObject.transform.position))
            {
                ChangeState_ServerRpc((int)ServerStates.Idle);
            }
            else if (_closestPlayerState.DisplayingHeldObject == false)
            {
                ChangeState_ServerRpc((int)ServerStates.Idle);
            }
            else if (isChargingUp)
            {
                if (Time.time - startTime >= CHARGE_TIME)
                {
                    isChargingUp = false;
                    isDoingAttack = true;
                    hasDoneDamage = false;
                    nextPosition = _targetPlayer.PlayerObject.transform.position;
                }
                else
                {
                    Vector3 directionToTarget = _targetPlayer.PlayerObject.transform.position - transform.position;
                    Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, 3.0f * Time.deltaTime);
                }
            }
            else
            {
                if (isDoingAttack)
                {
                    _rigidbody.AddForce(transform.forward * CHARGE_FORCE, ForceMode.Impulse);
                    _animator.SetTrigger("DoAttack");
                    isDoingAttack = false;
                    hasDoneDamage = false;
                    startTime = Time.time;
                }
                else
                {
                    if (Time.time - startTime >= 0.3f &&
                        _rigidbody.linearVelocity.magnitude < 0.3f)
                    {
                        _animator.SetTrigger("DoCharge");
                        isChargingUp = true;
                        startTime = Time.time;
                    }
                }
                CheckDamage();
            }
        }
    }
    
    private void Dead_OnEnter()
    {
        
    }
    
    #endregion
}
