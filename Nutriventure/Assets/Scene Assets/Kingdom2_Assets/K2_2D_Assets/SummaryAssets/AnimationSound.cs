using UnityEngine;

public class AnimationSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip star;
    public AudioClip swoosh;

    public void PlayStarSFX()
    {
        audioSource.PlayOneShot(star);
    }

    public void PlayWhoosh()
    {
        audioSource.PlayOneShot(swoosh);
    }
}
