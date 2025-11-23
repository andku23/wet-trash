using UnityEngine;

public class SoundItemModel : HeldItemModel
{
    [SerializeField] private PlayerAudioSource.SoundType soundType;
    
    public override void UseItem(PlayerController player)
    {
        UsableItemManager.Instance.RequestPlaySound(transform.position, soundType);
    }
}
