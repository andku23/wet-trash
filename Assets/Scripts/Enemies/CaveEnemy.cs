using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CaveEnemy : BaseEnemy
{
    private NetworkClient _targetPlayer_s;
    private PlayerStateData _closestPlayerState_s;
    private Collider _currentWaterBody_s;
    private CaveRoom _currentRoom_s;
    private CaveRoom _targetRoom_s;
    
    public CaveRoom CurrentRoom_S => _currentRoom_s;
    
    enum ServerStates
    {
        Idle = 0,
        AttackingPlayer = 1,
        Dead = 2
    }

    public override void InitializeServerValues()
    {
        base.InitializeServerValues();
        _currentWaterBody_s = base.GetCurrentWaterBody();
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
        
        BaseState attackingPlayer = new BaseState(null, null, null);
        _stateMachine.AddState((int)ServerStates.AttackingPlayer, attackingPlayer);
        
        BaseState dead = new BaseState(null, null, null);
        _stateMachine.AddState((int)ServerStates.Dead, dead);
    }
    
    #region States

    private void Idle_OnEnter()
    {
        
    }
    
    private void Idle_Update()
    {
        
        
    }
    
    #endregion
}
