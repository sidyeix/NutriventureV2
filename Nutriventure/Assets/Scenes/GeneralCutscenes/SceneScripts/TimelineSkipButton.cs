using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;

public class TimelineSkipButton : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector timelineDirector; // Reference to the Playable Director
    public Button skipButton; // Reference to the skip button

    [Header("Skip Settings")]
    public double skipToTime = 0; // Time in seconds to skip to
    public bool playAfterSkip = true; // Whether to continue playing after skipping
    public bool disableAfterSkip = true; // Whether to disable the button after skipping

    [Header("Audio")]
    public AudioSource audioSource; // Optional: AudioSource for skip sound
    public AudioClip skipSound; // Optional: Sound to play when skipping

    void Start()
    {
        // Get button reference if not assigned
        if (skipButton == null)
        {
            skipButton = GetComponent<Button>();
        }

        // Setup button listener
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        // Get audio source if not assigned
        if (audioSource == null && skipSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }
    }

    // Called when skip button is clicked
    public void OnSkipButtonClicked()
    {
        SkipTimeline();
    }

    // Skip the timeline to the specified time
    public void SkipTimeline()
    {
        if (timelineDirector == null)
        {
            Debug.LogWarning("No Playable Director assigned to TimelineSkipButton!");
            return;
        }

        // Play skip sound if available
        PlaySkipSound();

        // Tell the pause manager to ignore every pause signal whose
        // timeline.time is at or before the skip destination.
        if (TimelinePauseManager.Instance != null)
        {
            TimelinePauseManager.Instance.SetIgnoreSignalsUpToTime(skipToTime);
        }

        // Skip to the specified time
        timelineDirector.time = skipToTime;

        // Resume playback if specified
        if (playAfterSkip)
        {
            timelineDirector.Play();
        }
        else
        {
            timelineDirector.Pause();
            timelineDirector.Evaluate(); // Update the timeline state
        }

        Debug.Log($"Timeline skipped to {skipToTime} seconds");

        // Safety net: force-resume after a couple of frames in case
        // a crossed signal still managed to pause the director.
        if (playAfterSkip)
        {
            StartCoroutine(ForceResumeAfterSkip());
        }

        // Disable button if specified
        if (disableAfterSkip && skipButton != null)
        {
            skipButton.interactable = false;
        }
    }

    private IEnumerator ForceResumeAfterSkip()
    {
        // Wait a few frames for any deferred signal processing to finish
        yield return null;
        yield return null;
        yield return null;

        // If the director is somehow not playing, force it
        if (timelineDirector != null && timelineDirector.state != PlayState.Playing)
        {
            // Force-clear any pause state the manager might hold
            if (TimelinePauseManager.Instance != null && TimelinePauseManager.Instance.IsTimelinePaused())
            {
                TimelinePauseManager.Instance.ResumeTimeline();
            }

            timelineDirector.Play();
            Debug.Log("Timeline force-resumed after skip (safety net triggered)");
        }
    }

    // Optional: Play skip sound
    private void PlaySkipSound()
    {
        if (audioSource != null && skipSound != null)
        {
            audioSource.PlayOneShot(skipSound);
        }
    }

    // Public method to set skip time dynamically
    public void SetSkipTime(double newTime)
    {
        skipToTime = newTime;
    }

    // Public method to enable/disable the skip button
    public void SetSkipButtonEnabled(bool enabled)
    {
        if (skipButton != null)
        {
            skipButton.interactable = enabled;
        }
    }

    // Reset the skip button (enable it again)
    public void ResetSkipButton()
    {
        if (skipButton != null)
        {
            skipButton.interactable = true;
        }
    }

    // Optional: Add a check to automatically skip when timeline reaches a certain point
    void Update()
    {
        // You can add automatic skipping logic here if needed
        // Example: Skip automatically after X seconds
        // if (timelineDirector != null && timelineDirector.time > autoSkipTime) {
        //     SkipTimeline();
        // }
    }

    void OnDestroy()
    {
        // Clean up button listener
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
    }
}