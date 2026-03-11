using UnityEngine;
using UnityEngine.UI;

public class HealingZone : MonoBehaviour
{
    [Header("Healing Settings")]
    public float healingRate = 10f; // Energy per second (adjustable in inspector)
    public AudioSource healingAudioSource; // Assign in inspector
    public Animator healingAnimator; // Assign the animator of the healing station

    [Header("Animation Settings")]
    public string healingBoolParameter = "isHealing";
    public float animationDuration = 10f; // Total animation duration in seconds
    public string animationStateName = "HealingAnimation"; // Name of your animation state

    [Header("References")]
    public Slider energySlider; // Assign the energy slider from GameManager

    [Header("Visual Effects")]
    public GameObject healingEffect; // Healing effect GameObject to enable/disable
    public bool enableEffectOnHealing = true; // Whether to show the effect

    [Header("Checkpoint Settings")]
    public bool isCheckpoint = true; // Whether this zone also acts as a checkpoint
    public Checkpoint checkpointComponent; // Optional: reference to separate checkpoint object
    public AudioClip checkpointActivatedSound; // Sound when checkpoint is activated

    private bool playerInZone = false;
    private bool checkpointActivated = false;
    private GoGrowGlowGameManager gameManager;
    private Coroutine healingCoroutine;
    private float animationNormalizedTime = 0f;
    private bool gameWasActive = false;

    private void Start()
    {
        // Get reference to GameManager
        gameManager = GoGrowGlowGameManager.Instance;

        // Ensure audio source is disabled at start
        if (healingAudioSource != null)
        {
            healingAudioSource.Stop();
        }

        // Ensure animation is stopped at start
        if (healingAnimator != null)
        {
            healingAnimator.SetBool(healingBoolParameter, false);
        }

        // Ensure healing effect is disabled at start
        if (healingEffect != null)
        {
            healingEffect.SetActive(false);
        }

        // If no specific checkpoint component is assigned but we want checkpoint functionality,
        // add a Checkpoint component
        if (isCheckpoint && checkpointComponent == null)
        {
            checkpointComponent = gameObject.AddComponent<Checkpoint>();
            checkpointComponent.spawnPoint = transform;
            checkpointComponent.activateOnTouch = true;
            checkpointComponent.isStartCheckpoint = false;

            // Try to find visual components on children
            Transform inactiveVisual = transform.Find("InactiveVisual");
            Transform activeVisual = transform.Find("ActiveVisual");

            if (inactiveVisual != null) checkpointComponent.inactiveVisual = inactiveVisual.gameObject;
            if (activeVisual != null) checkpointComponent.activeVisual = activeVisual.gameObject;
        }
    }

    private void Update()
    {
        // Check if game state changed
        if (gameManager != null)
        {
            bool gameIsActive = gameManager.IsGameActive();

            // If game was active but now isn't, stop healing
            if (gameWasActive && !gameIsActive && playerInZone)
            {
                StopAllHealingActivities();
            }

            gameWasActive = gameIsActive;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && gameManager != null)
        {
            playerInZone = true;

            // Activate as checkpoint if not already activated
            if (isCheckpoint && checkpointComponent != null && !checkpointComponent.IsActivated())
            {
                checkpointComponent.Activate();

                // Play checkpoint activated sound
                if (checkpointActivatedSound != null && AudioHandler.Instance != null)
                {
                    AudioHandler.Instance.soundEffectsSource.PlayOneShot(checkpointActivatedSound);
                }
            }

            // Only start healing if game is active
            if (gameManager.IsGameActive())
            {
                StartHealingActivities();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log($"Player entered Healing Zone (game not active). Healing disabled.");
                #endif
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;

            // Stop healing activities regardless of game state
            StopAllHealingActivities();
        }
    }

    // Start all healing activities (audio, effects, coroutine)
    private void StartHealingActivities()
    {
        if (gameManager == null || !gameManager.IsGameActive()) return;

        // Calculate animation start time based on current energy
        float currentEnergy = GetCurrentEnergyFromSlider();

        // Convert energy (0-100) to normalized time (0-1)
        animationNormalizedTime = currentEnergy / 100f;

        // Notify GameManager
        gameManager.EnterHealingZone();

        // Start animation
        if (healingAnimator != null)
        {
            // Set the boolean parameter to start animation
            healingAnimator.SetBool(healingBoolParameter, true);

            // Set the animation to start at the calculated time
            if (!string.IsNullOrEmpty(animationStateName))
            {
                healingAnimator.Play(animationStateName, 0, animationNormalizedTime);
            }
            else
            {
                healingAnimator.Play(0, 0, animationNormalizedTime);
            }

            #if UNITY_EDITOR
            Debug.Log($"Started healing animation at normalized time: {animationNormalizedTime:F2}");
            #endif
        }

        // Start audio
        if (healingAudioSource != null && !healingAudioSource.isPlaying)
        {
            healingAudioSource.Play();
        }

        // Enable healing effect
        if (enableEffectOnHealing && healingEffect != null)
        {
            healingEffect.SetActive(true);
        }

        // Start healing coroutine if not already running
        if (healingCoroutine == null)
        {
            healingCoroutine = StartCoroutine(HealingProcess());
        }

        #if UNITY_EDITOR
        Debug.Log($"Player entered Healing Zone. Energy: {currentEnergy}, Animation start time: {animationNormalizedTime:F2}");
        #endif
    }

    // Stop all healing activities
    private void StopAllHealingActivities()
    {
        // Notify GameManager
        if (gameManager != null && gameManager.IsGameActive())
        {
            gameManager.ExitHealingZone();
        }

        // Stop animation
        if (healingAnimator != null)
        {
            healingAnimator.SetBool(healingBoolParameter, false);
        }

        // Stop healing coroutine
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
        }

        // Stop audio
        if (healingAudioSource != null && healingAudioSource.isPlaying)
        {
            healingAudioSource.Stop();
        }

        // Disable healing effect
        if (healingEffect != null)
        {
            healingEffect.SetActive(false);
        }

        #if UNITY_EDITOR
        Debug.Log("Healing activities stopped");
        #endif
    }

    private System.Collections.IEnumerator HealingProcess()
    {
        while (playerInZone && gameManager != null && gameManager.IsGameActive())
        {
            // Apply healing based on healing rate
            float healingAmount = healingRate * Time.deltaTime;

            // Add healing to GameManager
            gameManager.CollectHealing(healingAmount);

            // Update animation time based on current energy
            UpdateAnimationTime();

            yield return null;
        }
    }

    private void UpdateAnimationTime()
    {
        if (healingAnimator == null || !gameManager.IsGameActive()) return;

        // Get current energy from slider or GameManager
        float currentEnergy = GetCurrentEnergyFromSlider();

        // Calculate new normalized time based on current energy
        float newNormalizedTime = currentEnergy / 100f;
        newNormalizedTime = Mathf.Clamp01(newNormalizedTime);

        // Update animation speed based on healing rate
        float animationSpeed = healingRate / 10f;
        healingAnimator.speed = animationSpeed;

        // If the normalized time has changed significantly, update it
        if (Mathf.Abs(newNormalizedTime - animationNormalizedTime) > 0.01f)
        {
            animationNormalizedTime = newNormalizedTime;

            if (!string.IsNullOrEmpty(animationStateName))
            {
                healingAnimator.Play(animationStateName, 0, animationNormalizedTime);
            }
            else
            {
                healingAnimator.Play(0, 0, animationNormalizedTime);
            }
        }
    }

    private float GetCurrentEnergyFromSlider()
    {
        if (energySlider != null)
        {
            return energySlider.value;
        }

        if (gameManager != null)
        {
            return gameManager.GetCurrentEnergy();
        }

        return 0f;
    }

    private void OnDisable()
    {
        StopAllHealingActivities();
    }

    private void OnDestroy()
    {
        StopAllHealingActivities();
    }

    // ====== Public Methods ======

    public void SetHealingEffectActive(bool active)
    {
        if (healingEffect != null)
        {
            healingEffect.SetActive(active);
        }
    }

    public bool IsPlayerInZone() => playerInZone;

    public bool IsCheckpointActivated() => checkpointComponent != null && checkpointComponent.IsActivated();

    public Transform GetCheckpointSpawnPoint() => transform;

    public float GetCurrentAnimationTime() => animationNormalizedTime * animationDuration;

    // Method to manually activate checkpoint (useful for scripted events)
    public void ActivateAsCheckpoint()
    {
        if (checkpointComponent != null)
        {
            checkpointComponent.Activate();
        }
    }

    // Method to reset checkpoint (for level reset)
    public void ResetCheckpoint()
    {
        if (checkpointComponent != null)
        {
            checkpointComponent.ResetCheckpoint();
        }
    }

    // New method to check if healing zone should be active
    public bool ShouldHealingBeActive()
    {
        return playerInZone && gameManager != null && gameManager.IsGameActive();
    }

    // New method to restart healing if game becomes active while player is in zone
    public void RestartHealingIfNeeded()
    {
        if (playerInZone && gameManager != null && gameManager.IsGameActive() && healingCoroutine == null)
        {
            StartHealingActivities();
        }
    }
}
