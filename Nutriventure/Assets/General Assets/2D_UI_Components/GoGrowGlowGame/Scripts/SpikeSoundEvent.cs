using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SpikeSoundEvent : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spikeSound;

    private void Awake()
    {
        // Auto-assign AudioSource if not set
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // ?? Call this from the Animation Event
    public void PlaySpikeSound()
    {
        if (audioSource == null || spikeSound == null)
            return;

        audioSource.PlayOneShot(spikeSound);
    }
}
