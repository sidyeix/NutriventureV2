using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;

public class KartTrigger : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector destinationDirector;

    public GameObject playerUI;
    public GameObject driveUI;
    public GameObject kartDrivingUI;
    public TextMeshProUGUI destinationText;
    
    [Header("Countdown Settings")]
    public GameObject countdownUI; // Assign a UI GameObject with TextMeshProUGUI component
    public TextMeshProUGUI countdownText;
    public float countdownTime = 3f;
    public AudioClip countdownBeepSound;
    public AudioClip countdownGoSound;
    
    [Header("Horse Sound Effects")]
    public AudioClip horseIdleSound; // Sound when player is near the kart/horse
    public AudioClip horseRunningSound; // Sound when kart/horse is moving
    [Range(0f, 1f)]
    public float idleVolume = 0.5f;
    [Range(0f, 1f)]
    public float runningVolume = 0.7f;
    public float fadeInOutDuration = 0.5f; // Duration for crossfading sounds
    
    [Header("Player UI Elements")]
    public GameObject[] playerUIElementsToHide;

    public KartController kartController;
    public Transform kartSeatPosition;

    public Transform[] destinations;
    private int currentDestinationIndex = 0;

    private GameObject player;
    private bool playerInside = false;
    private bool isDriving = false;
    private bool isCountingDown = false;

    private Dictionary<GameObject, bool> playerUIElementStates = new Dictionary<GameObject, bool>();
    private AudioSource audioSource;
    private AudioSource horseIdleSource;
    private AudioSource horseRunningSource;
    private Coroutine countdownCoroutine;
    private Coroutine idleFadeCoroutine;
    private Coroutine runningFadeCoroutine;

    // Store player's original position and rotation for cancellation
    private Vector3 playerOriginalPosition;
    private Quaternion playerOriginalRotation;
    private Transform playerOriginalParent;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Setup main audio source for countdown sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup separate audio sources for horse sounds (for better control)
        horseIdleSource = gameObject.AddComponent<AudioSource>();
        horseIdleSource.spatialBlend = 1.0f; // Make it 3D sound
        horseIdleSource.rolloffMode = AudioRolloffMode.Linear;
        horseIdleSource.minDistance = 5f;
        horseIdleSource.maxDistance = 20f;
        horseIdleSource.loop = true;
        
        horseRunningSource = gameObject.AddComponent<AudioSource>();
        horseRunningSource.spatialBlend = 1.0f; // Make it 3D sound
        horseRunningSource.rolloffMode = AudioRolloffMode.Linear;
        horseRunningSource.minDistance = 5f;
        horseRunningSource.maxDistance = 30f;
        horseRunningSource.loop = true;
        
        // Configure horse idle sound if assigned
        if (horseIdleSound != null)
        {
            horseIdleSource.clip = horseIdleSound;
            horseIdleSource.volume = 0f; // Start with zero volume
        }
        
        // Configure horse running sound if assigned
        if (horseRunningSound != null)
        {
            horseRunningSource.clip = horseRunningSound;
            horseRunningSource.volume = 0f; // Start with zero volume
        }

        if (playerUIElementsToHide != null)
        {
            foreach (GameObject uiElement in playerUIElementsToHide)
            {
                if (uiElement != null)
                {
                    playerUIElementStates[uiElement] = uiElement.activeSelf;
                }
            }
        }

        driveUI?.SetActive(false);
        kartDrivingUI?.SetActive(false);
        if (countdownUI != null) countdownUI.SetActive(false);
    }

    private bool hasPlayedTimeline = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (playerInside && !isDriving && !isCountingDown)
            {
                StartDriveSequence();
            }
            else if (isDriving && !kartController.HasArrived)
            {
                ExitKart();
            }
        }
    }

    public void StartDriveSequence()
    {
        if (!playerInside || isDriving || isCountingDown) return;
        
        // Fade out idle sound
        FadeOutHorseIdleSound();
        
        // Hide player UI and drive UI immediately
        HidePlayerUIElements();
        driveUI?.SetActive(false);
        
        // Show kart driving UI at the start of sequence
        kartDrivingUI?.SetActive(true);
        
        // Position player to kart seat
        PreparePlayerForDriving();
        
        // Start countdown
        StartCountdown();
    }

    private void PreparePlayerForDriving()
    {
        // Store original player transform for cancellation
        playerOriginalPosition = player.transform.position;
        playerOriginalRotation = player.transform.rotation;
        playerOriginalParent = player.transform.parent;

        // Position player to kart seat
        player.transform.SetParent(kartSeatPosition);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        // Disable player controllers
        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
        if (cc) cc.enabled = false;
        if (tpc) tpc.enabled = false;
    }

    public void StartCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        isCountingDown = true;
        
        // Show countdown UI
        if (countdownUI != null) countdownUI.SetActive(true);
        
        // Countdown from 3 to 1
        for (int i = (int)countdownTime; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.color = Color.yellow;
            }
            
            // Play beep sound
            if (countdownBeepSound != null)
            {
                audioSource.PlayOneShot(countdownBeepSound);
            }
            
            // Optional: Add animation or scaling effect
            yield return StartCoroutine(ScaleCountdownText());
            
            yield return new WaitForSeconds(1f);
        }
        
        // "GO!" display
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.color = Color.green;
        }
        
        // Play GO sound
        if (countdownGoSound != null)
        {
            audioSource.PlayOneShot(countdownGoSound);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Hide countdown UI
        if (countdownUI != null) countdownUI.SetActive(false);
        
        isCountingDown = false;
        
        // Start driving (kartDrivingUI is already active from StartDriveSequence)
        StartDriving();
    }

    private IEnumerator ScaleCountdownText()
    {
        if (countdownText == null) yield break;
        
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 originalScale = countdownText.transform.localScale;
        Vector3 targetScale = originalScale * 1.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        countdownText.transform.localScale = originalScale;
    }

    public void StartDriving()
    {
        if (!playerInside || player == null) return;

        isDriving = true;

        // Start horse running sound
        StartHorseRunningSound();

        // KartDrivingUI is already active from StartDriveSequence
        // Just enable the kart controller
        if (kartController != null)
        {
            kartController.SetControllable(true);
            UpdateDestinationUI();
        }
    }

    // Start horse idle sound with fade in
    private void StartHorseIdleSound()
    {
        if (horseIdleSource == null || horseIdleSound == null) return;
        
        horseIdleSource.Play();
        
        // Start fade in coroutine
        if (idleFadeCoroutine != null)
        {
            StopCoroutine(idleFadeCoroutine);
        }
        idleFadeCoroutine = StartCoroutine(FadeAudioSource(horseIdleSource, 0f, idleVolume, fadeInOutDuration));
    }

    // Fade out horse idle sound
    private void FadeOutHorseIdleSound()
    {
        if (horseIdleSource == null || !horseIdleSource.isPlaying) return;
        
        if (idleFadeCoroutine != null)
        {
            StopCoroutine(idleFadeCoroutine);
        }
        idleFadeCoroutine = StartCoroutine(FadeAudioSource(horseIdleSource, horseIdleSource.volume, 0f, fadeInOutDuration, true));
    }

    // Start horse running sound with fade in
    private void StartHorseRunningSound()
    {
        if (horseRunningSource == null || horseRunningSound == null) return;
        
        horseRunningSource.Play();
        
        // Start fade in coroutine
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
        }
        runningFadeCoroutine = StartCoroutine(FadeAudioSource(horseRunningSource, 0f, runningVolume, fadeInOutDuration));
    }

    // Fade out horse running sound
    private void FadeOutHorseRunningSound()
    {
        if (horseRunningSource == null || !horseRunningSource.isPlaying) return;
        
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
        }
        runningFadeCoroutine = StartCoroutine(FadeAudioSource(horseRunningSource, horseRunningSource.volume, 0f, fadeInOutDuration, true));
    }

    // Generic audio fade coroutine
    private IEnumerator FadeAudioSource(AudioSource source, float startVolume, float endVolume, float duration, bool stopAfterFade = false)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(startVolume, endVolume, t);
            yield return null;
        }
        
        source.volume = endVolume;
        
        if (stopAfterFade && endVolume <= 0f)
        {
            source.Stop();
        }
    }

    // Modified Update method for driving state
    private void LateUpdate()
    {
        if (isDriving)
        {
            UpdateDestinationUI();

            if (kartController.HasArrived && !hasPlayedTimeline)
            {
                PlayDestinationTimeline();
            }
            
            // Optionally adjust running sound based on kart speed
            if (kartController != null && horseRunningSource != null)
            {
                // You can adjust volume or pitch based on speed if desired
                // Example: horseRunningSource.pitch = Mathf.Lerp(0.8f, 1.2f, kartController.CurrentSpeed / kartController.maxSpeed);
            }
        }
    }

    void PlayDestinationTimeline()
    {
        hasPlayedTimeline = true;

        // Fade out running sound
        FadeOutHorseRunningSound();

        // Disable kart control
        if (kartController != null)
            kartController.SetControllable(false);

        // Hide driving UI
        kartDrivingUI?.SetActive(false);

        // Play timeline
        if (destinationDirector != null)
        {
            destinationDirector.stopped += OnTimelineFinished;
            destinationDirector.Play();
        }
        else
        {
            // Fallback if no timeline assigned
            AutoExitKart();
        }
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        director.stopped -= OnTimelineFinished;

        AutoExitKart();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (!isCountingDown && !isDriving)
            {
                driveUI?.SetActive(true);
                // Start horse idle sound when player approaches
                StartHorseIdleSound();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            driveUI?.SetActive(false);
            
            // Fade out horse idle sound when player leaves
            FadeOutHorseIdleSound();
            
            // Cancel countdown if player leaves during countdown
            if (isCountingDown)
            {
                StopCountdown();
            }
        }
    }

    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        isCountingDown = false;
        
        // Hide all UI elements
        if (countdownUI != null) countdownUI.SetActive(false);
        kartDrivingUI?.SetActive(false);
        
        // Reset player to original position
        ResetPlayerAfterCancellation();
        
        // Show drive UI again if player is still inside
        if (playerInside)
        {
            driveUI?.SetActive(true);
            // Restart idle sound
            StartHorseIdleSound();
        }
    }

    private void ResetPlayerAfterCancellation()
    {
        // Reset player to original transform
        player.transform.SetParent(playerOriginalParent);
        player.transform.position = playerOriginalPosition;
        player.transform.rotation = playerOriginalRotation;

        // Re-enable player controller
        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();
        if (tpc) tpc.enabled = true;
        if (cc) cc.enabled = true;
        
        // Show player UI again
        ShowPlayerUIElements();
    }

    public void ExitKart()
    {
        if (!isDriving) return;

        isDriving = false;
        playerInside = false;

        // Fade out running sound
        FadeOutHorseRunningSound();

        ShowPlayerUIElements();

        driveUI?.SetActive(false);
        kartDrivingUI?.SetActive(false);

        // Reset player parent and position
        player.transform.SetParent(null);
        // You might want to position player near the kart instead of original position
        player.transform.position = kartController.transform.position + Vector3.right * 2f;

        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

        if (tpc) tpc.enabled = true;
        if (cc) cc.enabled = true;

        if (kartController != null)
            kartController.SetControllable(false);
    }

    public void AutoExitKart()
    {
        if (!isDriving) return;

        isDriving = false;
        kartDrivingUI?.SetActive(false);

        Invoke("CompleteAutoExit", 1.5f);
    }

    void CompleteAutoExit()
    {
        playerInside = false;
        hasPlayedTimeline = false;

        ShowPlayerUIElements();
        driveUI?.SetActive(false);

        player.transform.SetParent(null);
        // Position player near the kart at destination
        if (kartController != null && kartController.CurrentDestination != null)
        {
            player.transform.position = kartController.CurrentDestination.position + Vector3.right * 2f;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        ThirdPersonController tpc = player.GetComponent<ThirdPersonController>();

        if (tpc) tpc.enabled = true;
        if (cc) cc.enabled = true;

        GoToNextDestination();
    }

    void GoToNextDestination()
    {
        currentDestinationIndex++;

        if (currentDestinationIndex >= destinations.Length)
        {
            currentDestinationIndex = 0;
        }

        if (kartController != null && destinations.Length > 0)
        {
            kartController.SetDestination(destinations[currentDestinationIndex]);
            UpdateDestinationUI();
        }
    }

    void UpdateDestinationUI()
    {
        if (destinationText != null && kartController != null && kartController.CurrentDestination != null)
        {
            float distance = Vector3.Distance(
                kartController.transform.position,
                kartController.CurrentDestination.position
            );

            destinationText.text =
                $"Destination: {kartController.CurrentDestination.name}\nDistance: {distance:F1}m";

            destinationText.color =
                distance <= kartController.autoBrakeDistance ? Color.yellow : Color.white;
        }
        else if (destinationText != null)
        {
            destinationText.text = "Destination: None";
        }
    }

    public void SetNextDestination()
    {
        if (destinations == null || destinations.Length == 0) return;

        currentDestinationIndex = (currentDestinationIndex + 1) % destinations.Length;

        if (kartController != null)
        {
            kartController.SetDestination(destinations[currentDestinationIndex]);
            UpdateDestinationUI();
        }
    }

    public void ClearDestination()
    {
        if (kartController != null)
        {
            kartController.ClearDestination();
            UpdateDestinationUI();
        }
    }

    public void SetDestinationByIndex(int index)
    {
        if (destinations == null || index < 0 || index >= destinations.Length) return;

        currentDestinationIndex = index;

        if (kartController != null)
        {
            kartController.SetDestination(destinations[currentDestinationIndex]);
            UpdateDestinationUI();
        }
    }

    private void HidePlayerUIElements()
    {
        if (playerUIElementsToHide != null)
        {
            foreach (GameObject uiElement in playerUIElementsToHide)
            {
                if (uiElement != null)
                {
                    if (!playerUIElementStates.ContainsKey(uiElement))
                    {
                        playerUIElementStates[uiElement] = uiElement.activeSelf;
                    }
                    uiElement.SetActive(false);
                }
            }
        }
    }

    private void ShowPlayerUIElements()
    {
        if (playerUIElementsToHide != null)
        {
            foreach (GameObject uiElement in playerUIElementsToHide)
            {
                if (uiElement != null && playerUIElementStates.ContainsKey(uiElement))
                {
                    uiElement.SetActive(playerUIElementStates[uiElement]);
                }
            }
        }
    }

    public void SetPlayerUIElementActive(GameObject uiElement, bool active)
    {
        if (uiElement != null &&
            playerUIElementsToHide != null &&
            System.Array.Exists(playerUIElementsToHide, element => element == uiElement))
        {
            uiElement.SetActive(active);

            if (playerUIElementStates.ContainsKey(uiElement))
            {
                playerUIElementStates[uiElement] = active;
            }
        }
    }
    
    // Clean up coroutines when destroyed
    private void OnDestroy()
    {
        if (idleFadeCoroutine != null)
        {
            StopCoroutine(idleFadeCoroutine);
        }
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
        }
    }
}