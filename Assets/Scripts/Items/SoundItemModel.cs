using UnityEngine;

public class SoundItemModel : HeldItemModel
{
    [SerializeField] private PlayerAudioSource audioPlayer;
    
    public override void UseItem()
    {
        audioPlayer.PlaySound(0);
    }
}
