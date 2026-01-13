using UnityEngine;

public class EnerlingsSFX : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Battle Sounds")]
    [Space(10)]
    public AudioClip basicAttack;
    public AudioClip hitImpact;
    public AudioClip magicCast;
    public AudioClip dodge;

    [Header("Skill Sounds")]
    [Space(10)]
    public AudioClip skill1;
    public AudioClip skill2;
    public AudioClip skill3;

    [Header("Character Sounds")]
    [Space(10)]
    public AudioClip footstep;
    public AudioClip jump;
    public AudioClip land;
    public AudioClip voiceGrunt;

    [Header("Result Sounds")]
    [Space(10)]
    public AudioClip winSound;
    public AudioClip deathSound;

    [Header("Optional Sounds")]
    [Space(10)]
    public AudioClip special1;
    public AudioClip special2;
    public AudioClip weaponDraw;

    void Start()
    {
        InitializeAudioSource();
    }

    void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    // Battle Sounds
    #region Battle Methods
    public void PlayBasicAttack()
    {
        PlayClip(basicAttack, "basicAttack");
    }

    public void PlayHitImpact()
    {
        PlayClip(hitImpact, "hitImpact");
    }

    public void PlayMagicCast()
    {
        PlayClip(magicCast, "magicCast");
    }

    public void PlayDodge()
    {
        PlayClip(dodge, "dodge");
    }
    #endregion

    // Skill Sounds
    #region Skill Methods
    public void PlaySkill1()
    {
        PlayClip(skill1, "skill1");
    }

    public void PlaySkill2()
    {
        PlayClip(skill2, "skill2");
    }

    public void PlaySkill3()
    {
        PlayClip(skill3, "skill3");
    }
    #endregion

    // Character Sounds
    #region Character Methods
    public void PlayFootstep()
    {
        PlayClip(footstep, "footstep");
    }

    public void PlayJump()
    {
        PlayClip(jump, "jump");
    }

    public void PlayLand()
    {
        PlayClip(land, "land");
    }

    public void PlayVoiceGrunt()
    {
        PlayClip(voiceGrunt, "voiceGrunt");
    }
    #endregion

    // Result Sounds
    #region Result Methods
    public void PlayWinSound()
    {
        PlayClip(winSound, "winSound");
    }

    public void PlayDeathSound()
    {
        PlayClip(deathSound, "deathSound");
    }
    #endregion

    // Optional Sounds
    #region Optional Methods
    public void PlaySpecial1()
    {
        PlayClip(special1, "special1");
    }

    public void PlaySpecial2()
    {
        PlayClip(special2, "special2");
    }

    public void PlayWeaponDraw()
    {
        PlayClip(weaponDraw, "weaponDraw");
    }
    #endregion

    // Helper method to play clips with error checking
    void PlayClip(AudioClip clip, string clipName)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null! Cannot play " + clipName);
            return;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning(clipName + " is not assigned in the inspector!");
        }
    }

    // Optional: Volume control methods
    public void PlayBasicAttackWithVolume(float volume = 1.0f)
    {
        PlayClipWithVolume(basicAttack, "basicAttack", volume);
    }

    public void PlaySkill1WithVolume(float volume = 1.0f)
    {
        PlayClipWithVolume(skill1, "skill1", volume);
    }

    // Generic volume control helper
    void PlayClipWithVolume(AudioClip clip, string clipName, float volume)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null! Cannot play " + clipName);
            return;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else
        {
            Debug.LogWarning(clipName + " is not assigned in the inspector!");
        }
    }
}