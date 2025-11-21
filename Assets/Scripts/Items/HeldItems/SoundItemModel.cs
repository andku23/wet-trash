using UnityEngine;

public class SoundItemModel : HeldItemModel
{
    [SerializeField] private PlayerAudioSource audioPlayer;
    
    public override void UseItem(PlayerController player)
    {
        audioPlayer.PlaySound(0);
    }
}
