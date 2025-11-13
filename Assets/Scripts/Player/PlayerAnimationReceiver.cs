using UnityEngine;

public class PlayerAnimationReceiver : MonoBehaviour
{
    [SerializeField] private PlayerAudioSource _playerAudioSource;
    
    private void OnFootstep(AnimationEvent animationEvent)
    {
        _playerAudioSource.PlaySound(PlayerAudioSource.SoundType.FootStep);
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        _playerAudioSource.PlaySound(PlayerAudioSource.SoundType.Landing);
    }
    
    private void OnSplash(AnimationEvent animationEvent)
    {
        _playerAudioSource.PlaySound(PlayerAudioSource.SoundType.WaterSplash);
    }
    
    private void OnSwim(AnimationEvent animationEvent)
    {
        _playerAudioSource.PlaySound(PlayerAudioSource.SoundType.Swim);
    }
}
