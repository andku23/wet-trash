using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioSource : MonoBehaviour
{
    
    [SerializeField] private List<SoundGroup> soundGroups;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject instantiatedAudioPrefab;
    
    public enum SoundType
    {
        FootStep,
        Landing,
        WaterSplash,
        Swim
    }
    
    public void PlaySound(SoundType soundType)
    {
        SoundGroup soundGroup = soundGroups[(int)soundType];
        AudioClip clip = soundGroup.clips[Random.Range(0, soundGroup.clips.Length)];
        float volume = soundGroup.volume;
        
        if (soundGroup.isInstantiatedAudio)
        {
            AudioDestroyAfterPlay audioDestroyAfterPlay = Instantiate(instantiatedAudioPrefab).GetComponent<AudioDestroyAfterPlay>();
            audioDestroyAfterPlay.transform.position = transform.position;
            audioDestroyAfterPlay.Play(clip);
        }
        else
        {
            audioSource.volume = volume;
            audioSource.clip = clip;
            audioSource.Play();
        }
        
        
    }
}

[System.Serializable]
public class SoundGroup
{
    public PlayerAudioSource.SoundType soundType;
    public AudioClip[] clips;
    public float volume;
    public bool isInstantiatedAudio;
}
