using UnityEngine;

public class SoundItemModel : HeldItemModel
{
    [SerializeField] private PlayerAudioSource.SoundType soundType;
    
    public override void UseItem(InteractionController interactionController, PlayerState playerState)
    {
        UsableItemManager.Instance.RequestPlaySound(transform.position, soundType);
    }
}
