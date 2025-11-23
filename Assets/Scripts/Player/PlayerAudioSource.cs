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
        Swim,
        Thump,
        EnemyAttack,
        Trumpet
    }

    public void PlaySound(int index)
    {
        if (index >= soundGroups.Count) return;
        PlaySound(soundGroups[index].soundType);
    }
    
    public void PlaySound(SoundType soundType)
    {
        SoundGroup soundGroup = GetSoundGroup(soundType);
        if (soundGroup == null) return;
        PlaySound(soundGroup, transform.position);
    }
    
    public void PlaySoundAtPosition(SoundType soundType, Vector3 position)
    {
        SoundGroup soundGroup = GetSoundGroup(soundType);
        if (soundGroup == null) return;
        PlaySound(soundGroup, position);
    }

    private SoundGroup GetSoundGroup(SoundType soundType)
    {
        SoundGroup soundGroup = null;
        for (int i = 0; i < soundGroups.Count; i++)
        {
            if (soundType == soundGroups[i].soundType)
            {
                soundGroup = soundGroups[i];
                break;
            }
        }

        return soundGroup;
    }

    private void PlaySound(SoundGroup soundGroup, Vector3 position)
    {
        AudioClip clip = soundGroup.clips[Random.Range(0, soundGroup.clips.Length)];
        if (soundGroup.isInstantiatedAudio)
        {
            AudioDestroyAfterPlay audioDestroyAfterPlay = Instantiate(instantiatedAudioPrefab).GetComponent<AudioDestroyAfterPlay>();
            audioDestroyAfterPlay.transform.position = position;
            audioDestroyAfterPlay.Play(clip, soundGroup.volume);
        }
        else
        {
            audioSource.volume = soundGroup.volume;
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
