using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class StandardHarvestable : BaseHarvestableObject, IInteractable
{
    [SerializeField] private PlayerAudioSource _audioSource;
    [SerializeField] private Animator _animator;
    [SerializeField] private WorldspaceInstruction interactionInstruction;
    
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
    
    public override void DoDamage(ItemInteractionData interactionData)
    {
        if (!ContainsInteractableType(interactionData.interactionType)) return;
        _animator.SetTrigger(_animID_Hit);
        _audioSource.PlaySound(PlayerAudioSource.SoundType.RockHit);
        DoDamage_ServerRpc(interactionData.interactionType, interactionData.damage);
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        string instructionText = "Use: ";
        Sprite[] sprites = new Sprite[interactableTypes.Length];
        for (int i = 0; i < interactableTypes.Length; i++)
        {
            sprites[i] = EnumAssetManager.Instance.itemInteractionLookup[interactableTypes[i]];
        }
        interactionInstruction.SetSpriteList(instructionText, sprites);
        interactionInstruction.SetVisible(true);
        return true;
    }
    
    public void DisableInteractable()
    {
        interactionInstruction.SetVisible(false);
    }
    
    protected override async Awaitable DoDeath()
    {
        _audioSource.PlaySound(PlayerAudioSource.SoundType.RockBreak);
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
