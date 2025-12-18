using UnityEngine;

public class SoundItemModel : HeldItemModel
{
    [SerializeField] private PlayerAudioSource.SoundType soundType;
    
    public override void UseItem(InteractionController interactionController, PlayerStateData playerState)
    {
        UsableItemManager.Instance.RequestPlaySound(transform.position, soundType);
    }
}
