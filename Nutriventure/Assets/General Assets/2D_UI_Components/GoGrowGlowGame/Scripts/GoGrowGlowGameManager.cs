using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoGrowGlowGameManager : MonoBehaviour
{
    public static GoGrowGlowGameManager Instance { get; private set; }

    [Header("Player Settings")]
    public ThirdPersonController playerController;
    public Transform playerTransform;
    public int maxLives = 5;
    private int currentLives;
    private float currentLifeAmount; // For half hearts (e.g., 4.5 lives)

    [Header("Slider/Energy Settings")]
    public Slider energySlider;
    public float energyDecreaseRate = 2f;
    public float goFoodEnergyGain = 22f;
    public float healingZoneEnergyGain = 5f;
    public float junkFoodEnergyDeduction = 20f;
    private float currentEnergy = 0f;

    [Header("Speed Settings")]
    public float minSpeed = 2f;
    public float maxSpeed = 7f;
    public float speedBoostAmount = 8f;
    public float speedBoostDuration = 3f;
    private float speedBoostTimer = 0f;
    private bool isSpeedBoosted = false;

    [Header("UI Elements")]
    public List<GameObject> uiElementsToDisable = new List<GameObject>();
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public Button startButton;

    [Header("Heart System")]
    public Transform heartContainer;        // Parent object for heart images
    public GameObject heartPrefab;          // Prefab for heart UI
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;
    private List<Image> heartImages = new List<Image>();

    [Header("Speed Boost UI")]
    public Canvas speedLinesCanvas;         // Canvas with speed lines effect
    public GameObject speedBoostIndicator;  // Optional UI indicator for speed boost

    [Header("Game State")]
    public bool gameIsActive = false;
    private float gameTimer = 0f;
    private int score = 0;

    [Header("Respawn Points")]
    public Transform[] spawnPoints;

    [Header("Food Settings")]
    public int foodPoints = 100;
    public int junkFoodPointsDeduction = 120;

    [Header("Food Spawning")]
    public FoodSpawner foodSpawner;
    public bool respawnFoodOnStart = true;

    [Header("Character Animation")]
    public Animator characterAnimator;
    public string exciteTrigger = "isExcite";

    [Header("Player Visual Effects - GameObject Approach")]
    public GameObject foodReactionEffect;    // Good effect GameObject on player
    public GameObject badEffect;             // Bad effect GameObject on player
    public GameObject feedbackSpriteObject;  // GameObject with SpriteRenderer
    public float spriteDisplayTime = 1f;

    [Header("Audio Integration")]
    public AudioClip[] goFoodSounds;
    public AudioClip[] junkFoodSounds;
    public AudioClip speedBoostSound;
    public AudioClip loseLifeSound;
    public AudioClip healingZoneSound;

    [Header("World Effects - GameObject Approach")]
    public GameObject speedBoostEffect;
    public GameObject goFoodCollectionEffect;
    public GameObject junkFoodCollectionEffect;

    // Healing zone tracking
    private bool inHealingZone = false;
    private AudioSource healingZoneAudioSource;

    // Animation coroutine tracking
    private Coroutine resetExciteCoroutine;
    private Coroutine spriteDisplayCoroutine;

    // Effect tracking coroutines
    private Coroutine stopFoodReactionCoroutine;
    private Coroutine stopBadEffectCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize UI
        if (energySlider != null)
        {
            energySlider.maxValue = 100f;
            energySlider.minValue = 0f;
            energySlider.value = currentEnergy;
        }

        // Initialize player speed to minimum
        if (playerController != null)
        {
            playerController.MoveSpeed = minSpeed;
        }

        // Initialize heart system
        InitializeHeartSystem();

        // Hide visual effects at start
        HideAllVisualEffects();

        // Hide speed lines at start
        HideSpeedLines();

        // Create healing zone audio source
        if (healingZoneSound != null)
        {
            healingZoneAudioSource = gameObject.AddComponent<AudioSource>();
            healingZoneAudioSource.clip = healingZoneSound;
            healingZoneAudioSource.loop = true;
            healingZoneAudioSource.volume = 0.5f;
        }

        UpdateUI();
        SetGameActive(false);
    }

    private void InitializeHeartSystem()
    {
        // Clear existing hearts
        foreach (Transform child in heartContainer)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();

        // Create hearts based on maxLives
        for (int i = 0; i < maxLives; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, heartContainer);
            Image heartImage = heartObj.GetComponent<Image>();
            if (heartImage != null)
            {
                heartImages.Add(heartImage);
                heartImage.sprite = emptyHeart;
            }
        }

        currentLifeAmount = maxLives;
        currentLives = maxLives;
    }

    private void Update()
    {
        if (!gameIsActive) return;

        // Update timer
        gameTimer += Time.deltaTime;
        UpdateTimerDisplay();

        // Handle energy decrease (if not in speed boost)
        if (!isSpeedBoosted && !inHealingZone)
        {
            UpdateEnergy();
        }

        // Update player speed based on energy
        UpdatePlayerSpeed();

        // Handle speed boost timer
        if (isSpeedBoosted)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                EndSpeedBoost();
            }
        }

        // Handle healing zone
        if (inHealingZone)
        {
            HealPlayer();
        }
    }

    public void StartGame()
    {
        gameIsActive = true;
        currentLives = maxLives;
        currentLifeAmount = maxLives;
        currentEnergy = 0f;
        score = 0;
        gameTimer = 0f;

        // Set initial player speed
        if (playerController != null)
        {
            playerController.MoveSpeed = minSpeed;
        }

        // Reset animation state
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
        }

        // Update hearts
        UpdateHeartUI();

        // Hide all visual effects
        HideAllVisualEffects();

        // Hide speed lines
        HideSpeedLines();

        // Disable UI elements
        foreach (GameObject uiElement in uiElementsToDisable)
        {
            if (uiElement != null)
                uiElement.SetActive(false);
        }

        // Disable start button
        if (startButton != null)
            startButton.gameObject.SetActive(false);

        // START FOOD SPAWNING
        if (foodSpawner != null)
        {
            foodSpawner.StartSpawning();
        }
        else
        {
            Debug.LogError("FoodSpawner not assigned to GameManager!");
        }

        UpdateUI();
        RespawnPlayer();

        Debug.Log("Game Started! Timer started, Energy: 0, Speed: " + minSpeed);
    }

    public void EndGame()
    {
        gameIsActive = false;

        // STOP FOOD SPAWNING
        if (foodSpawner != null)
        {
            foodSpawner.StopSpawning();
        }

        // Stop all running coroutines
        StopAllCoroutines();

        // Reset animation state
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
        }

        // Hide all visual effects
        HideAllVisualEffects();

        // Hide speed lines
        HideSpeedLines();

        // Stop healing zone audio
        if (healingZoneAudioSource != null && healingZoneAudioSource.isPlaying)
        {
            healingZoneAudioSource.Stop();
        }

        // Re-enable UI elements
        foreach (GameObject uiElement in uiElementsToDisable)
        {
            if (uiElement != null)
                uiElement.SetActive(true);
        }

        // Re-enable start button
        if (startButton != null)
            startButton.gameObject.SetActive(true);

        Debug.Log("Game Ended! All effects stopped.");
    }

    private void HideAllVisualEffects()
    {
        // Hide feedback sprite
        if (feedbackSpriteObject != null)
        {
            feedbackSpriteObject.SetActive(false);
        }

        // Hide player effects
        if (foodReactionEffect != null)
        {
            foodReactionEffect.SetActive(false);
        }
        if (badEffect != null)
        {
            badEffect.SetActive(false);
        }

        // Hide world effects
        if (speedBoostEffect != null)
        {
            speedBoostEffect.SetActive(false);
        }
    }

    private void ShowSpeedLines()
    {
        if (speedLinesCanvas != null)
        {
            speedLinesCanvas.gameObject.SetActive(true);
        }

        if (speedBoostIndicator != null)
        {
            speedBoostIndicator.SetActive(true);
        }
    }

    private void HideSpeedLines()
    {
        if (speedLinesCanvas != null)
        {
            speedLinesCanvas.gameObject.SetActive(false);
        }

        if (speedBoostIndicator != null)
        {
            speedBoostIndicator.SetActive(false);
        }
    }

    private void UpdateEnergy()
    {
        currentEnergy -= energyDecreaseRate * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);

        if (energySlider != null)
            energySlider.value = currentEnergy;

        if (currentEnergy <= 0f)
        {
            LoseLife();
        }
    }

    private void UpdatePlayerSpeed()
    {
        if (playerController == null) return;

        if (isSpeedBoosted)
        {
            playerController.MoveSpeed = speedBoostAmount;
        }
        else
        {
            float energyPercentage = currentEnergy / 100f;
            playerController.MoveSpeed = Mathf.Lerp(minSpeed, maxSpeed, energyPercentage);
        }
    }

    public void CollectGoFood(GameObject foodObject = null)
    {
        currentEnergy += goFoodEnergyGain;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);

        score += foodPoints;

        // Trigger animation
        TriggerExciteAnimation();

        // Show player effect (good reaction)
        ShowFoodReactionEffect();

        // Show feedback sprite
        ShowFeedbackSprite();

        // Play world collection effect
        PlayGoFoodCollectionEffect(foodObject);

        // Check for speed boost
        if (currentEnergy >= 100f && !isSpeedBoosted)
        {
            // Don't play go food sound when reaching 100, only speed boost sound
            StartSpeedBoost();
        }
        else if (isSpeedBoosted)
        {
            speedBoostTimer += 2f;
            Debug.Log("Speed boost extended by 2 seconds!");
        }
        else
        {
            // Only play go food sound if NOT reaching 100
            PlayGoFoodSound();
        }

        UpdateUI();
        Debug.Log($"Go Food Collected! Energy: {currentEnergy}, Score: {score}, Speed: {playerController.MoveSpeed}");
    }

    public void CollectJunkFood(GameObject foodObject = null)
    {
        currentEnergy -= junkFoodEnergyDeduction;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);

        score = Mathf.Max(0, score - junkFoodPointsDeduction);

        // Play sound
        PlayJunkFoodSound();

        // Show player effect (bad reaction)
        ShowBadEffect();

        // Play world collection effect
        PlayJunkFoodCollectionEffect(foodObject);

        UpdateUI();
        Debug.Log($"Junk Food Collected! Energy: {currentEnergy}, Score: {score}, Speed: {playerController.MoveSpeed}");
    }

    private void StartSpeedBoost()
    {
        isSpeedBoosted = true;
        speedBoostTimer = speedBoostDuration;

        // Play speed boost sound
        if (AudioHandler.Instance != null && speedBoostSound != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(speedBoostSound);
        }

        // Show speed boost effect
        if (speedBoostEffect != null)
        {
            speedBoostEffect.SetActive(true);
        }

        // Show speed lines canvas
        ShowSpeedLines();

        Debug.Log("Speed Boost Activated for " + speedBoostDuration + " seconds!");
    }

    private void EndSpeedBoost()
    {
        isSpeedBoosted = false;
        speedBoostTimer = 0f;

        // Hide speed boost effect
        if (speedBoostEffect != null)
        {
            speedBoostEffect.SetActive(false);
        }

        // Hide speed lines canvas
        HideSpeedLines();

        Debug.Log("Speed Boost Ended!");
    }

    // Public method to lose specific amount of life
    public void LoseLifeAmount(float amount, bool respawnAtCheckpoint = true)
    {
        currentLifeAmount -= amount;
        currentLifeAmount = Mathf.Max(0f, currentLifeAmount);

        // Update currentLives (integer part)
        currentLives = Mathf.CeilToInt(currentLifeAmount);

        // Play lose life sound
        if (AudioHandler.Instance != null && loseLifeSound != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(loseLifeSound);
        }

        UpdateHeartUI();

        if (currentLifeAmount <= 0f)
        {
            EndGame();
            Debug.Log("Game Over! No lives left.");
        }
        else
        {
            if (respawnAtCheckpoint)
            {
                RespawnPlayer();
                currentEnergy = 50f;
            }

            // Reset animation state
            if (characterAnimator != null)
            {
                characterAnimator.SetBool(exciteTrigger, false);
            }

            // Hide all visual effects
            HideAllVisualEffects();

            // Hide speed lines if boost was active
            if (isSpeedBoosted)
            {
                HideSpeedLines();
                isSpeedBoosted = false;
                speedBoostTimer = 0f;
            }

            // Stop any running coroutines
            if (resetExciteCoroutine != null)
            {
                StopCoroutine(resetExciteCoroutine);
                resetExciteCoroutine = null;
            }

            if (spriteDisplayCoroutine != null)
            {
                StopCoroutine(spriteDisplayCoroutine);
                spriteDisplayCoroutine = null;
            }

            if (stopFoodReactionCoroutine != null)
            {
                StopCoroutine(stopFoodReactionCoroutine);
                stopFoodReactionCoroutine = null;
            }

            if (stopBadEffectCoroutine != null)
            {
                StopCoroutine(stopBadEffectCoroutine);
                stopBadEffectCoroutine = null;
            }

            Debug.Log($"Lost {amount} life! Current life: {currentLifeAmount}");
        }
    }

    // Original LoseLife method (for backward compatibility)
    public void LoseLife()
    {
        LoseLifeAmount(1f, true);
    }

    private void TriggerExciteAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetExciteCoroutine != null)
            {
                StopCoroutine(resetExciteCoroutine);
            }

            characterAnimator.SetBool(exciteTrigger, true);
            resetExciteCoroutine = StartCoroutine(ResetExciteAnimation());
        }
    }

    private IEnumerator ResetExciteAnimation()
    {
        yield return new WaitForSeconds(1f);

        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
        }
        resetExciteCoroutine = null;
    }

    private void ShowFoodReactionEffect()
    {
        if (foodReactionEffect != null)
        {
            if (stopFoodReactionCoroutine != null)
            {
                StopCoroutine(stopFoodReactionCoroutine);
            }

            foodReactionEffect.SetActive(true);
            stopFoodReactionCoroutine = StartCoroutine(HideEffectAfterTime(foodReactionEffect, 2f, stopFoodReactionCoroutine));
        }
    }

    private void ShowBadEffect()
    {
        if (badEffect != null)
        {
            if (stopBadEffectCoroutine != null)
            {
                StopCoroutine(stopBadEffectCoroutine);
            }

            badEffect.SetActive(true);
            stopBadEffectCoroutine = StartCoroutine(HideEffectAfterTime(badEffect, 2f, stopBadEffectCoroutine));
        }
    }

    private IEnumerator HideEffectAfterTime(GameObject effect, float duration, Coroutine coroutineRef)
    {
        yield return new WaitForSeconds(duration);

        if (effect != null)
        {
            effect.SetActive(false);
        }

        if (coroutineRef == stopFoodReactionCoroutine)
        {
            stopFoodReactionCoroutine = null;
        }
        else if (coroutineRef == stopBadEffectCoroutine)
        {
            stopBadEffectCoroutine = null;
        }
    }

    private void ShowFeedbackSprite()
    {
        if (feedbackSpriteObject != null)
        {
            if (spriteDisplayCoroutine != null)
            {
                StopCoroutine(spriteDisplayCoroutine);
            }

            feedbackSpriteObject.SetActive(true);
            spriteDisplayCoroutine = StartCoroutine(HideFeedbackSprite());
        }
    }

    private IEnumerator HideFeedbackSprite()
    {
        yield return new WaitForSeconds(spriteDisplayTime);

        if (feedbackSpriteObject != null)
        {
            feedbackSpriteObject.SetActive(false);
        }
        spriteDisplayCoroutine = null;
    }

    private void PlayGoFoodSound()
    {
        if (AudioHandler.Instance != null && goFoodSounds.Length > 0)
        {
            AudioClip randomClip = goFoodSounds[Random.Range(0, goFoodSounds.Length)];
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(randomClip);
        }
    }

    private void PlayJunkFoodSound()
    {
        if (AudioHandler.Instance != null && junkFoodSounds.Length > 0)
        {
            AudioClip randomClip = junkFoodSounds[Random.Range(0, junkFoodSounds.Length)];
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(randomClip);
        }
    }

    private void PlayGoFoodCollectionEffect(GameObject foodObject)
    {
        if (goFoodCollectionEffect != null)
        {
            Vector3 position = foodObject != null ? foodObject.transform.position : playerTransform.position;
            GameObject effect = Instantiate(goFoodCollectionEffect, position, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, 3f);
        }
    }

    private void PlayJunkFoodCollectionEffect(GameObject foodObject)
    {
        if (junkFoodCollectionEffect != null)
        {
            Vector3 position = foodObject != null ? foodObject.transform.position : playerTransform.position;
            GameObject effect = Instantiate(junkFoodCollectionEffect, position, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, 3f);
        }
    }

    public void EnterHealingZone()
    {
        inHealingZone = true;

        if (healingZoneAudioSource != null && !healingZoneAudioSource.isPlaying)
        {
            healingZoneAudioSource.Play();
        }

        Debug.Log("Entered Healing Zone");
    }

    public void ExitHealingZone()
    {
        inHealingZone = false;

        if (healingZoneAudioSource != null && healingZoneAudioSource.isPlaying)
        {
            healingZoneAudioSource.Stop();
        }

        Debug.Log("Exited Healing Zone");
    }

    private void HealPlayer()
    {
        currentEnergy += healingZoneEnergyGain * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);

        if (energySlider != null)
            energySlider.value = currentEnergy;
    }

    private void RespawnPlayer()
    {
        if (playerTransform == null || spawnPoints.Length == 0) return;

        Transform nearestSpawn = spawnPoints[0];
        float nearestDistance = Vector3.Distance(playerTransform.position, nearestSpawn.position);

        for (int i = 1; i < spawnPoints.Length; i++)
        {
            float distance = Vector3.Distance(playerTransform.position, spawnPoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSpawn = spawnPoints[i];
            }
        }

        playerTransform.position = nearestSpawn.position;
        Debug.Log($"Respawned at {nearestSpawn.name}");
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (livesText != null)
            livesText.text = $"Lives: {Mathf.CeilToInt(currentLifeAmount)}";

        if (energySlider != null)
            energySlider.value = currentEnergy;
    }

    private void UpdateHeartUI()
    {
        if (heartImages.Count == 0) return;

        float remainingLife = currentLifeAmount;

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] != null)
            {
                if (remainingLife >= 1f)
                {
                    // Full heart
                    heartImages[i].sprite = fullHeart;
                    remainingLife -= 1f;
                }
                else if (remainingLife >= 0.5f)
                {
                    // Half heart
                    heartImages[i].sprite = halfHeart;
                    remainingLife -= 0.5f;
                }
                else
                {
                    // Empty heart
                    heartImages[i].sprite = emptyHeart;
                }
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTimer / 60f);
            int seconds = Mathf.FloorToInt(gameTimer % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void SetGameActive(bool active)
    {
        gameIsActive = active;

        if (playerController != null)
        {
            playerController.enabled = active;
        }
    }

    public void RespawnAllFood()
    {
        if (foodSpawner != null && foodSpawner.IsSpawningEnabled())
        {
            foodSpawner.RespawnAllFood();
        }
    }

    // Public getters
    public float GetCurrentLifeAmount() => currentLifeAmount;
    public int GetCurrentLives() => currentLives;
    public float GetCurrentEnergy() => currentEnergy;
    public int GetCurrentScore() => score;
    public bool IsGameActive() => gameIsActive;
    public bool IsSpeedBoosted() => isSpeedBoosted;
}