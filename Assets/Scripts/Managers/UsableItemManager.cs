using Unity.Netcode;
using UnityEngine;

// IDK if im gonna keep this permanently, but I just wanted to put all the logic
// for held items in one place
public class UsableItemManager : NetworkBehaviour
{
    
    public static UsableItemManager Instance;

    private void Start()
    {
        Instance = this;
    }
    
    [SerializeField] private PlayerAudioSource audioPlayer;
    
    // Play sound at location for all clients
    public void RequestPlaySound(Vector3 position, PlayerAudioSource.SoundType soundType)
    {
        PlaySound_ServerRpc(position, soundType);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlaySound_ServerRpc(Vector3 position, PlayerAudioSource.SoundType soundType)
    {
        PlaySound_ClientRpc(position, soundType);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void PlaySound_ClientRpc(Vector3 position, PlayerAudioSource.SoundType soundType)
    {
        audioPlayer.PlaySoundAtPosition(soundType, position);
    }
}
