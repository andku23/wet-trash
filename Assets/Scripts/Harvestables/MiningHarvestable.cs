using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MiningHarvestable : BaseHarvestableObject
{
    enum ServerStates
    {
        Idle = 0
    }
    
    protected override void InitializeStateMachine()
    {
        base.InitializeStateMachine();
        
        BaseState idle = new BaseState(null, Idle_Update, null);
        _stateMachine.AddState((int)ServerStates.Idle, idle);
    }
    
    protected override async Awaitable DoDeath()
    {
        //_animator.SetBool("IsDead", true);
        //_audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
        if (IsServer)
        {
            //ChangeState_ServerRpc((int)ServerStates.Death);
            DropLoot_S();
            await Awaitable.WaitForSecondsAsync(1.5f);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void Idle_Update()
    {
    }
}
