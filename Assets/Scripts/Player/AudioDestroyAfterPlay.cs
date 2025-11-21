using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class AudioDestroyAfterPlay : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    
    public void Play(AudioClip audioClip, float volume = 1.0f)
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.Play();
            StartCoroutine(WaitForAudioCompletion(audioClip.length));
        }
    }
    
    private IEnumerator WaitForAudioCompletion(float seconds)
    {
        
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}
