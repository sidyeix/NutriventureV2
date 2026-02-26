using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using Cinemachine;

public class KartTrigger : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector destinationDirector;

    public GameObject playerUI;
    public GameObject kartDrivingUI;
    public TextMeshProUGUI destinationText;
    
    [Header("Countdown Settings")]
    public GameObject countdownUI;
    public TextMeshProUGUI countdownText;
    public float countdownTime = 3f;
    public AudioClip countdownBeepSound;
    public AudioClip countdownGoSound;
    
    [Header("Horse Sound Effects")]
    public AudioClip horseIdleSound;
    public AudioClip horseRunningSound;
    [Range(0f, 1f)]
    public float idleVolume = 0.5f;
    [Range(0f, 1f)]
    public float runningVolume = 0.7f;
    public float fadeInOutDuration = 0.5f;
    
    [Header("Player UI Elements")]
    public GameObject[] playerUIElementsToHide;

    [Header("Wagon Blocker")]
    public GameObject wagonBlocker;

    [Header("Camera References")]
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    [SerializeField] private CinemachineVirtualCamera kartFollowCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private float cameraRestoreDelay = 0.5f;

    public KartController kartController;
    public Transform kartSeatPosition;

    public Transform[] destinations;
    private int currentDestinationIndex = 0;

    private GameObject player;
    private bool playerInside = false;
    private bool isDriving = false;
    private bool isCountingDown = false;

    // Flags to control triggering
    private bool hasBeenUsed = false;
    private bool hasTriggeredThisEntry = false;

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

    private bool hasPlayedTimeline = false;
    
    // References for disabling movement/animation
    private StarterAssets.ThirdPersonController thirdPersonController;
    private StarterAssets.StarterAssetsInputs starterAssetsInputs;
    private Animator playerAnimator;
    private CharacterController characterController;

    // Store original camera priorities
    private int originalPlayerCameraPriority = 10;
    private int kartCameraPriority = 15; // Higher than player camera

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Get player components
        if (player != null)
        {
            thirdPersonController = player.GetComponent<StarterAssets.ThirdPersonController>();
            starterAssetsInputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
            playerAnimator = player.GetComponent<Animator>();
            characterController = player.GetComponent<CharacterController>();
        }
        
        // Find camera references if not assigned
        FindCameraReferences();
        
        // Store original player camera priority
        if (playerFollowCamera != null)
        {
            originalPlayerCameraPriority = playerFollowCamera.Priority;
        }
        
        // Setup audio sources
        SetupAudioSources();

        // Store UI element states
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

        // Initialize UI states
        if (kartDrivingUI != null) kartDrivingUI.SetActive(false);
        if (countdownUI != null) countdownUI.SetActive(false);
        
        // Make sure wagon blocker is active at start
        if (wagonBlocker != null)
        {
            wagonBlocker.SetActive(true);
        }
    }

    private void FindCameraReferences()
    {
        // Find player follow camera if not assigned
        if (playerFollowCamera == null)
        {
            CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>();
            foreach (CinemachineVirtualCamera cam in cameras)
            {
                if (cam.gameObject.name.Contains("Player") || cam.Priority > 5)
                {
                    playerFollowCamera = cam;
                    break;
                }
            }
        }

        // Find kart follow camera if not assigned
        if (kartFollowCamera == null)
        {
            CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>();
            foreach (CinemachineVirtualCamera cam in cameras)
            {
                if (cam.gameObject.name.Contains("Kart") || cam.gameObject.name.Contains("Vehicle"))
                {
                    kartFollowCamera = cam;
                    break;
                }
            }
        }

        // Find Cinemachine brain if not assigned
        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        }
    }

    private void SetupAudioSources()
    {
        // Setup main audio source for countdown sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup separate audio sources for horse sounds
        horseIdleSource = gameObject.AddComponent<AudioSource>();
        horseIdleSource.spatialBlend = 1.0f;
        horseIdleSource.rolloffMode = AudioRolloffMode.Linear;
        horseIdleSource.minDistance = 5f;
        horseIdleSource.maxDistance = 20f;
        horseIdleSource.loop = true;
        
        horseRunningSource = gameObject.AddComponent<AudioSource>();
        horseRunningSource.spatialBlend = 1.0f;
        horseRunningSource.rolloffMode = AudioRolloffMode.Linear;
        horseRunningSource.minDistance = 5f;
        horseRunningSource.maxDistance = 30f;
        horseRunningSource.loop = true;
        
        // Configure horse idle sound
        if (horseIdleSound != null)
        {
            horseIdleSource.clip = horseIdleSound;
            horseIdleSource.volume = 0f;
        }
        
        // Configure horse running sound
        if (horseRunningSound != null)
        {
            horseRunningSource.clip = horseRunningSound;
            horseRunningSource.volume = 0f;
        }
    }

    private void Update()
    {
        // Emergency exit with Escape key
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && isDriving)
        {
            ExitKart();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasBeenUsed && !hasTriggeredThisEntry)
        {
            playerInside = true;
            hasTriggeredThisEntry = true;
            
            // Force stop the kart immediately
            ForceStopKart();
            
            // Automatically start the drive sequence
            if (!isCountingDown && !isDriving)
            {
                StartDriveSequence();
            }
            
            // Start horse idle sound
            StartHorseIdleSound();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            
            if (!isDriving && !isCountingDown && !hasBeenUsed)
            {
                hasTriggeredThisEntry = false;
            }
            
            FadeOutHorseIdleSound();
            
            if (isCountingDown)
            {
                StopCountdown();
            }
        }
    }

    public void StartDriveSequence()
    {
        if (!playerInside || isDriving || isCountingDown || hasBeenUsed) return;
        
        FadeOutHorseIdleSound();
        HidePlayerUIElements();
        
        if (kartDrivingUI != null) kartDrivingUI.SetActive(true);
        
        PreparePlayerForDriving();
        StartCountdown();
    }

    private void PreparePlayerForDriving()
    {
        if (player == null) return;
        
        // Store original player transform
        playerOriginalPosition = player.transform.position;
        playerOriginalRotation = player.transform.rotation;
        playerOriginalParent = player.transform.parent;

        // Position player to kart seat
        player.transform.SetParent(kartSeatPosition);
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;

        // Disable player controllers
        ResetPlayerMovement();
        
        if (characterController != null) characterController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;
        if (starterAssetsInputs != null) starterAssetsInputs.enabled = false;
        
        // Set driving animation
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.SetBool("IsDriving", true);
        }
        
        // Switch to kart camera
        SwitchToKartCamera();
    }

    private void SwitchToKartCamera()
    {
        Debug.Log("Switching to KART camera");
        
        // Lower player camera priority
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 5;
            Debug.Log($"Player camera priority set to: {playerFollowCamera.Priority}");
        }
        
        // Raise kart camera priority
        if (kartFollowCamera != null)
        {
            // Make sure kart camera is active
            if (!kartFollowCamera.gameObject.activeSelf)
            {
                kartFollowCamera.gameObject.SetActive(true);
            }
            
            kartFollowCamera.enabled = true;
            kartFollowCamera.Priority = kartCameraPriority;
            Debug.Log($"Kart camera priority set to: {kartFollowCamera.Priority}");
        }
        else
        {
            Debug.LogWarning("Kart follow camera not assigned!");
        }
        
        // Force immediate camera refresh
        ForceCameraRefresh();
        
        // Hard reset after a frame
        StartCoroutine(DelayedHardReset());
    }

    private void SwitchToPlayerCamera()
    {
        Debug.Log("Switching to PLAYER camera");
        
        // Lower kart camera priority
        if (kartFollowCamera != null)
        {
            kartFollowCamera.Priority = 5;
            Debug.Log($"Kart camera priority set to: {kartFollowCamera.Priority}");
        }
        
        // Restore player camera priority
        if (playerFollowCamera != null)
        {
            // Make sure player camera is active
            if (!playerFollowCamera.gameObject.activeSelf)
            {
                playerFollowCamera.gameObject.SetActive(true);
            }
            
            playerFollowCamera.enabled = true;
            playerFollowCamera.Priority = originalPlayerCameraPriority;
            Debug.Log($"Player camera priority restored to: {playerFollowCamera.Priority}");
        }
        
        // Force immediate camera refresh
        ForceCameraRefresh();
        
        // Hard reset after a frame
        StartCoroutine(DelayedHardReset());
    }

    private void ForceCameraRefresh()
    {
        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindObjectOfType<CinemachineBrain>();
            if (cinemachineBrain == null) return;
        }
        
        // Force Cinemachine to update immediately
        cinemachineBrain.ManualUpdate();
        
        if (cinemachineBrain.ActiveVirtualCamera != null)
        {
            Debug.Log($"Active camera after refresh: {cinemachineBrain.ActiveVirtualCamera.Name}");
        }
    }

    private void HardResetCamera()
    {
        Debug.Log("Hard resetting camera...");
        
        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindObjectOfType<CinemachineBrain>();
            if (cinemachineBrain == null) return;
        }
        
        // Store current blend settings
        var defaultBlend = cinemachineBrain.m_DefaultBlend;
        
        // Force a cut blend
        cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
        
        // Force update
        cinemachineBrain.ManualUpdate();
        
        // Restore blend after frame
        StartCoroutine(RestoreBlendAfterFrame(defaultBlend));
    }

    private IEnumerator RestoreBlendAfterFrame(CinemachineBlendDefinition originalBlend)
    {
        yield return new WaitForEndOfFrame();
        
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend = originalBlend;
            cinemachineBrain.ManualUpdate();
        }
    }

    private IEnumerator DelayedHardReset()
    {
        yield return new WaitForEndOfFrame();
        HardResetCamera();
        
        if (cinemachineBrain != null && cinemachineBrain.ActiveVirtualCamera != null)
        {
            Debug.Log($"Active camera after hard reset: {cinemachineBrain.ActiveVirtualCamera.Name}");
        }
    }

    [ContextMenu("Debug Camera State")]
    public void DebugCameraState()
    {
        Debug.Log("=== CAMERA STATE DEBUG ===");
        
        if (playerFollowCamera != null)
        {
            Debug.Log($"Player Camera - Active: {playerFollowCamera.gameObject.activeSelf}, Enabled: {playerFollowCamera.enabled}, Priority: {playerFollowCamera.Priority}");
        }
        
        if (kartFollowCamera != null)
        {
            Debug.Log($"Kart Camera - Active: {kartFollowCamera.gameObject.activeSelf}, Enabled: {kartFollowCamera.enabled}, Priority: {kartFollowCamera.Priority}");
        }
        
        if (cinemachineBrain != null)
        {
            var activeCam = cinemachineBrain.ActiveVirtualCamera;
            Debug.Log($"Cinemachine Brain - Active Camera: {(activeCam != null ? activeCam.Name : "None")}");
            Debug.Log($"Current Blend: {cinemachineBrain.ActiveBlend}");
        }
    }

    public void ForceResetCamera()
    {
        Debug.Log("Force reset camera called on KartTrigger");
        HardResetCamera();
        SwitchToPlayerCamera();
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
        
        if (countdownUI != null) countdownUI.SetActive(true);
        
        for (int i = (int)countdownTime; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.color = Color.yellow;
            }
            
            if (countdownBeepSound != null)
            {
                audioSource.PlayOneShot(countdownBeepSound);
            }
            
            yield return StartCoroutine(ScaleCountdownText());
            yield return new WaitForSeconds(1f);
        }
        
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.color = Color.green;
        }
        
        if (countdownGoSound != null)
        {
            audioSource.PlayOneShot(countdownGoSound);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        if (countdownUI != null) countdownUI.SetActive(false);
        
        isCountingDown = false;
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
        if (!playerInside || player == null || hasBeenUsed) return;

        isDriving = true;
        hasBeenUsed = true;
        
        RemoveWagonBlocker();
        StartHorseRunningSound();

        if (kartController != null)
        {
            kartController.SetControllable(true);
            UpdateDestinationUI();
        }
    }
    
    private void RemoveWagonBlocker()
    {
        if (wagonBlocker != null)
        {
            wagonBlocker.SetActive(false);
        }
    }

    private void ForceStopKart()
    {
        if (kartController == null) return;
        
        kartController.SetControllable(false);
        
        Rigidbody rb = kartController.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log("Kart force stopped");
    }

    private void StartHorseIdleSound()
    {
        if (horseIdleSource == null || horseIdleSound == null) return;
        
        horseIdleSource.Play();
        
        if (idleFadeCoroutine != null)
        {
            StopCoroutine(idleFadeCoroutine);
        }
        idleFadeCoroutine = StartCoroutine(FadeAudioSource(horseIdleSource, 0f, idleVolume, fadeInOutDuration));
    }

    private void FadeOutHorseIdleSound()
    {
        if (horseIdleSource == null || !horseIdleSource.isPlaying) return;
        
        if (idleFadeCoroutine != null)
        {
            StopCoroutine(idleFadeCoroutine);
        }
        idleFadeCoroutine = StartCoroutine(FadeAudioSource(horseIdleSource, horseIdleSource.volume, 0f, fadeInOutDuration, true));
    }

    private void StartHorseRunningSound()
    {
        if (horseRunningSource == null || horseRunningSound == null) return;
        
        horseRunningSource.Play();
        
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
        }
        runningFadeCoroutine = StartCoroutine(FadeAudioSource(horseRunningSource, 0f, runningVolume, fadeInOutDuration));
    }

    private void FadeOutHorseRunningSound()
    {
        if (horseRunningSource == null || !horseRunningSource.isPlaying) return;
        
        if (runningFadeCoroutine != null)
        {
            StopCoroutine(runningFadeCoroutine);
        }
        runningFadeCoroutine = StartCoroutine(FadeAudioSource(horseRunningSource, horseRunningSource.volume, 0f, fadeInOutDuration, true));
    }

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

    private void LateUpdate()
    {
        if (isDriving)
        {
            UpdateDestinationUI();

            if (kartController != null && kartController.HasArrived && !hasPlayedTimeline)
            {
                PlayDestinationTimeline();
            }
        }
    }

    void PlayDestinationTimeline()
    {
        hasPlayedTimeline = true;

        ForceStopKart();
        FadeOutHorseRunningSound();

        if (kartController != null)
            kartController.SetControllable(false);

        if (kartDrivingUI != null) kartDrivingUI.SetActive(false);

        if (destinationDirector != null)
        {
            destinationDirector.stopped += OnTimelineFinished;
            destinationDirector.Play();
        }
        else
        {
            AutoExitKart();
        }
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        director.stopped -= OnTimelineFinished;
        AutoExitKart();
    }

    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        isCountingDown = false;
        
        if (countdownUI != null) countdownUI.SetActive(false);
        if (kartDrivingUI != null) kartDrivingUI.SetActive(false);
        
        ResetPlayerAfterCancellation();
        
        if (playerInside)
        {
            StartHorseIdleSound();
        }
        
        hasTriggeredThisEntry = false;
    }

    private void ResetPlayerAfterCancellation()
    {
        if (player == null) return;
        
        player.transform.SetParent(playerOriginalParent);
        player.transform.position = playerOriginalPosition;
        player.transform.rotation = playerOriginalRotation;

        EnablePlayerMovementAndAnimation();
        ShowPlayerUIElements();
        
        if (!isDriving)
        {
            hasBeenUsed = false;
        }
    }

    private void EnablePlayerMovementAndAnimation()
    {
        if (characterController != null) characterController.enabled = true;
        if (thirdPersonController != null) thirdPersonController.enabled = true;
        if (starterAssetsInputs != null) starterAssetsInputs.enabled = true;
        if (playerAnimator != null) 
        {
            playerAnimator.enabled = true;
            playerAnimator.SetBool("IsDriving", false);
        }
        
        // Switch back to player camera
        SwitchToPlayerCamera();
        
        Debug.Log("Player movement and camera control restored");
    }

    private void ResetPlayerMovement()
    {
        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = false;
        }
        
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.move = Vector2.zero;
            starterAssetsInputs.jump = false;
            starterAssetsInputs.sprint = false;
        }
        
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }
    }

    public void ExitKart()
    {
        if (!isDriving) return;

        ForceStopKart();
        
        isDriving = false;
        playerInside = false;

        FadeOutHorseRunningSound();
        ShowPlayerUIElements();

        if (kartDrivingUI != null) kartDrivingUI.SetActive(false);

        player.transform.SetParent(null);
        if (kartController != null)
        {
            player.transform.position = kartController.transform.position + Vector3.right * 2f;
        }

        EnablePlayerMovementAndAnimation();

        if (kartController != null)
            kartController.SetControllable(false);
            
        hasPlayedTimeline = false;
    }

    public void AutoExitKart()
    {
        if (!isDriving) return;

        isDriving = false;
        if (kartDrivingUI != null) kartDrivingUI.SetActive(false);

        CompleteAutoExit();
    }

    void CompleteAutoExit()
    {
        playerInside = false;
        hasPlayedTimeline = false;

        ShowPlayerUIElements();

        player.transform.SetParent(null);
        
        if (kartController != null && kartController.CurrentDestination != null)
        {
            player.transform.position = kartController.CurrentDestination.position + Vector3.right * 2f;
        }

        StartCoroutine(DelayedEnablePlayerControl());
        GoToNextDestination();
    }

    private IEnumerator DelayedEnablePlayerControl()
    {
        yield return new WaitForSeconds(cameraRestoreDelay);
        EnablePlayerMovementAndAnimation();
        
        // Force hard reset after enabling
        HardResetCamera();
        
        Debug.Log("Delayed player control enabled after timeline");
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
        
        if (destinationDirector != null)
        {
            destinationDirector.stopped -= OnTimelineFinished;
        }
    }
}