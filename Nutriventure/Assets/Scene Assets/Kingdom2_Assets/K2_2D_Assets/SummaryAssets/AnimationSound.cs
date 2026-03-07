using UnityEngine;

public class AnimationSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip star;
    public AudioClip swoosh;

    void Start()
    {
        // Automatically assign AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            // If still null, add AudioSource component
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                Debug.Log("Added AudioSource component to " + gameObject.name);
            }

            Debug.Log("AudioSource assigned: " + audioSource.name);
        }
    }

    public void PlayStarSFX()
    {
        if (audioSource == null)
            TryResolveAudioSource();

        if (audioSource == null)
            return;

        if (star != null)
        {
            audioSource.PlayOneShot(star);
        }
        else
        {
            Debug.LogWarning("Star AudioClip is not assigned!");
        }
    }

    public void PlayWhoosh()
    {
        if (audioSource == null)
            TryResolveAudioSource();

        if (audioSource == null)
            return;

        if (swoosh != null)
        {
            audioSource.PlayOneShot(swoosh);
        }
        else
        {
            Debug.LogWarning("Swoosh AudioClip is not assigned!");
        }
    }

    private void TryResolveAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();
        if (audioSource == null)
            audioSource = FindObjectOfType<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
}