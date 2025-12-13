using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MiningHarvestable : BaseHarvestableObject
{
    [SerializeField] private Animator _animator;
    
    private int _animID_Hit;
    private int _animID_Die;
    
    enum ServerStates
    {
        Idle = 0,
        Die = 1
    }
    
    protected override void InitializeStateMachine()
    {
        base.InitializeStateMachine();
        
        _animID_Hit = Animator.StringToHash("Hit");
        _animID_Die = Animator.StringToHash("Die");
        
        BaseState idle = new BaseState(Idle_OnEnter, null, null);
        _stateMachine.AddState((int)ServerStates.Idle, idle);
        
        BaseState die = new BaseState(Die_OnEnter, null, null);
        _stateMachine.AddState((int)ServerStates.Die, die);
    }
    
    public override void DoDamage(ItemInteractionType interactionType, int amount)
    {
        if (!ContainsInteractableType(interactionType)) return;
        _animator.SetTrigger(_animID_Hit);
        Debug.Log("do hit");
        DoDamage_ServerRpc(interactionType, amount);
    }
    
    protected override async Awaitable DoDeath()
    {
        //_audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
        if (IsServer)
        {
            ChangeState_ServerRpc((int)ServerStates.Die);
            DropLoot_S();
            await Awaitable.WaitForSecondsAsync(2f);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void Idle_OnEnter()
    {
        
    }
    
    private void Die_OnEnter()
    {
        _animator.SetTrigger(_animID_Die);
    }
}
