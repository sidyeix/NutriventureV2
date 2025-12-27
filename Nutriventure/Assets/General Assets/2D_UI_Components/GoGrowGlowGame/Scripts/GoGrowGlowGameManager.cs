using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoGrowGlowGameManager : MonoBehaviour
{
    public static GoGrowGlowGameManager Instance { get; private set; }

    public enum FoodType { Go, Grow, Glow }

    [Header("Player Settings")]
    public ThirdPersonController playerController;
    public Transform playerTransform;
    public Transform playerArmature; // Reference to player's armature for scaling
    public int maxLives = 5;
    private int currentLives;
    private float currentLifeAmount;

    [Header("Initial Player Settings")]
    public float initialPlayerSpeed = 5f;    // Speed set in ThirdPersonController inspector
    public float initialPlayerSize = 1f;     // Normal scale of player (for Go zone)

    [Header("Slider/Energy Settings")]
    public Slider energySlider;
    public Image sliderFillImage;
    public Image sliderHandleImage;
    public float energyDecreaseRate = 2f;
    public float goFoodEnergyGain = 22f;
    public float growFoodEnergyGain = 22f;
    public float glowFoodEnergyGain = 22f;
    public float junkFoodEnergyDeduction = 20f;
    public float healingZoneEnergyGain = 5f;
    private float currentEnergy = 0f;

    [Header("Speed Settings - Go Mechanics")]
    public float minSpeed = 2f;
    public float maxSpeed = 7f;
    public float speedBoostAmount = 8f;
    public float speedBoostDuration = 3f;
    private float speedBoostTimer = 0f;
    private bool isSpeedBoosted = false;

    [Header("Size Settings - Grow Mechanics")]
    public float minSize = 0.56f;
    public float maxSize = 3f;
    private bool isSizeBoosted = false;
    private float sizeBoostTimer = 0f;
    public float sizeBoostDuration = 3f;

    [Header("UI Elements")]
    public List<GameObject> uiElementsToDisable = new List<GameObject>();
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public Button startButton;
    public Canvas gameCanvas; // Reference to the main game canvas

    [Header("Heart System")]
    public Transform heartContainer;
    public GameObject heartPrefab;
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;
    private List<Image> heartImages = new List<Image>();

    [Header("Boost UI Effects")]
    public Canvas speedLinesCanvas;
    public GameObject speedBoostIndicator;
    public GameObject sizeBoostIndicator;
    public GameObject glowBoostIndicator;

    [Header("Game State")]
    public bool gameIsActive = false;
    private float gameTimer = 0f;
    private int score = 0;
    private FoodType currentFoodZone = FoodType.Go;

    [Header("Respawn Points")]
    public Transform[] spawnPoints;

    [Header("Food Settings")]
    public int foodPoints = 100;
    public int junkFoodPointsDeduction = 120;

    [Header("Food Spawning")]
    public FoodSpawner foodSpawner;

    [Header("Character Animation")]
    public Animator characterAnimator;
    public string exciteTrigger = "isExcite";
    public string stomachAcheTrigger = "isStomachAche";
    public string strongTrigger = "isStrong";
    public string glowTrigger = "isGlow";

    [Header("Player Visual Effects")]
    public GameObject foodReactionEffect;
    public GameObject badEffect;
    public GameObject feedbackSpriteObject;
    public float spriteDisplayTime = 1f;

    [Header("Food Type Feedback Sprites")]
    public Sprite goFoodSprite;
    public Sprite growFoodSprite;
    public Sprite glowFoodSprite;
    public Sprite junkFoodSprite;

    [Header("Audio Integration")]
    public AudioClip[] goFoodSounds;
    public AudioClip[] growFoodSounds;
    public AudioClip[] glowFoodSounds;
    public AudioClip[] junkFoodSounds;
    public AudioClip speedBoostSound;
    public AudioClip sizeBoostSound;
    public AudioClip glowBoostSound;
    public AudioClip loseLifeSound;
    public AudioClip healingZoneSound;
    public AudioClip collectionSound;

    [Header("Boost Effects")]
    public GameObject speedBoostEffect;
    public GameObject sizeBoostEffect;
    public GameObject glowBoostEffect;

    // Healing zone tracking
    private bool inHealingZone = false;
    private AudioSource healingZoneAudioSource;

    // Animation coroutine tracking
    private Coroutine resetExciteCoroutine;
    private Coroutine resetStomachAcheCoroutine;
    private Coroutine resetStrongCoroutine;
    private Coroutine resetGlowCoroutine;
    private Coroutine spriteDisplayCoroutine;

    // Effect tracking coroutines
    private Coroutine stopFoodReactionCoroutine;
    private Coroutine stopBadEffectCoroutine;

    // Track if we've initialized player settings
    private bool playerSettingsInitialized = false;

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
        // Hide game canvas at start if assigned
        if (gameCanvas != null)
        {
            gameCanvas.gameObject.SetActive(false);
        }

        // Initialize UI
        if (energySlider != null)
        {
            energySlider.maxValue = 100f;
            energySlider.minValue = 0f;
            energySlider.value = currentEnergy;
        }

        // Initialize player settings from inspector
        InitializePlayerSettings();

        // Initialize heart system
        InitializeHeartSystem();

        // Hide visual effects at start
        HideAllVisualEffects();

        // Hide boost UI at start
        HideAllBoostUI();

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

    private void InitializePlayerSettings()
    {
        // Set player speed to initial value (from inspector)
        if (playerController != null)
        {
            playerController.MoveSpeed = initialPlayerSpeed;
        }

        // Set player size to initial value (normal size = 1)
        if (playerArmature != null)
        {
            playerArmature.localScale = Vector3.one * initialPlayerSize;
        }

        playerSettingsInitialized = true;
    }

    private void InitializeHeartSystem()
    {
        if (heartContainer == null || heartPrefab == null) return;

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
        UpdateHeartUI();
    }

    private void Update()
    {
        if (!gameIsActive) return;

        // Update timer
        gameTimer += Time.deltaTime;
        UpdateTimerDisplay();

        // Handle energy decrease (if not in boost)
        if (!isSpeedBoosted && !isSizeBoosted && !inHealingZone)
        {
            UpdateEnergy();
        }

        // Update player mechanics based on current zone
        switch (currentFoodZone)
        {
            case FoodType.Go:
                UpdatePlayerSpeed();
                break;
            case FoodType.Grow:
                UpdatePlayerSize();
                break;
            case FoodType.Glow:
                // We'll implement glow mechanics later
                break;
        }

        // Handle boost timers
        if (isSpeedBoosted)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                EndSpeedBoost();
            }
        }

        if (isSizeBoosted)
        {
            sizeBoostTimer -= Time.deltaTime;
            if (sizeBoostTimer <= 0f)
            {
                EndSizeBoost();
            }
        }

        // Handle healing zone
        if (inHealingZone)
        {
            HealPlayer();
        }
    }

    public void SetCurrentFoodZone(FoodType zoneType, Color fillColor, Sprite handleSprite)
    {
        if (!gameIsActive) return; // Only allow zone switching during active game

        currentFoodZone = zoneType;

        // Update UI colors
        if (sliderFillImage != null)
        {
            sliderFillImage.color = fillColor;
        }

        if (sliderHandleImage != null && handleSprite != null)
        {
            sliderHandleImage.sprite = handleSprite;
        }

        // If switching to Grow zone, initialize size based on current energy
        if (zoneType == FoodType.Grow && playerArmature != null)
        {
            UpdatePlayerSize();
        }
        // If switching to Go zone, set player back to initial size
        else if (zoneType == FoodType.Go && playerArmature != null)
        {
            playerArmature.localScale = Vector3.one * initialPlayerSize;
        }

        Debug.Log($"Switched to {zoneType} zone");
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

    private void UpdatePlayerSize()
    {
        if (playerArmature == null) return;

        if (isSizeBoosted)
        {
            playerArmature.localScale = Vector3.one * maxSize;
        }
        else
        {
            float energyPercentage = currentEnergy / 100f;
            float currentSize = Mathf.Lerp(minSize, maxSize, energyPercentage);
            playerArmature.localScale = Vector3.one * currentSize;
        }
    }

    public void StartGame()
    {
        // Show game canvas
        if (gameCanvas != null)
        {
            gameCanvas.gameObject.SetActive(true);
        }

        gameIsActive = true;
        currentLives = maxLives;
        currentLifeAmount = maxLives;
        currentEnergy = 0f; // Start with 0 energy
        score = 0;
        gameTimer = 0f;
        currentFoodZone = FoodType.Go; // Start with Go mechanics

        // Set initial player speed to minimum (when game starts in Go zone)
        if (playerController != null)
        {
            playerController.MoveSpeed = minSpeed;
        }

        // Player stays at initial size (1) for Go zone
        if (playerArmature != null)
        {
            playerArmature.localScale = Vector3.one * initialPlayerSize;
        }

        // Reset animation state
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
            characterAnimator.SetBool(stomachAcheTrigger, false);
            characterAnimator.SetBool(strongTrigger, false);
            characterAnimator.SetBool(glowTrigger, false);
        }

        // Update hearts
        UpdateHeartUI();

        // Hide all visual effects
        HideAllVisualEffects();

        // Hide all boost UI
        HideAllBoostUI();

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

        Debug.Log("Game Started! Timer started, Energy: 0, Player at normal scale: " + initialPlayerSize);
    }

    public void EndGame()
    {
        gameIsActive = false;

        // Hide game canvas
        if (gameCanvas != null)
        {
            gameCanvas.gameObject.SetActive(false);
        }

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
            characterAnimator.SetBool(stomachAcheTrigger, false);
            characterAnimator.SetBool(strongTrigger, false);
            characterAnimator.SetBool(glowTrigger, false);
        }

        // Reset player to initial settings
        if (playerSettingsInitialized)
        {
            if (playerController != null)
            {
                playerController.MoveSpeed = initialPlayerSpeed;
            }

            if (playerArmature != null)
            {
                playerArmature.localScale = Vector3.one * initialPlayerSize;
            }
        }

        // Hide all visual effects
        HideAllVisualEffects();

        // Hide all boost UI
        HideAllBoostUI();

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

        Debug.Log("Game Ended! Player returned to initial settings");
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

        // Hide boost effects
        if (speedBoostEffect != null)
        {
            speedBoostEffect.SetActive(false);
        }
        if (sizeBoostEffect != null)
        {
            sizeBoostEffect.SetActive(false);
        }
        if (glowBoostEffect != null)
        {
            glowBoostEffect.SetActive(false);
        }
    }

    private void HideAllBoostUI()
    {
        // Hide speed lines
        if (speedLinesCanvas != null)
        {
            speedLinesCanvas.gameObject.SetActive(false);
        }

        // Hide boost indicators
        if (speedBoostIndicator != null)
            speedBoostIndicator.SetActive(false);
        if (sizeBoostIndicator != null)
            sizeBoostIndicator.SetActive(false);
        if (glowBoostIndicator != null)
            glowBoostIndicator.SetActive(false);
    }

    private void ShowBoostUI(FoodType boostType)
    {
        HideAllBoostUI(); // Hide all first

        switch (boostType)
        {
            case FoodType.Go:
                if (speedLinesCanvas != null)
                    speedLinesCanvas.gameObject.SetActive(true);
                if (speedBoostIndicator != null)
                    speedBoostIndicator.SetActive(true);
                break;
            case FoodType.Grow:
                if (sizeBoostIndicator != null)
                    sizeBoostIndicator.SetActive(true);
                break;
            case FoodType.Glow:
                if (glowBoostIndicator != null)
                    glowBoostIndicator.SetActive(true);
                break;
        }
    }

    public void CollectGoFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return; // Only collect food during active game

        // Play generic collection sound
        PlayCollectionSound();

        currentEnergy += goFoodEnergyGain;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);
        score += foodPoints;

        // Trigger animation
        TriggerExciteAnimation();

        // Show player effect
        ShowFoodReactionEffect();

        // Show feedback sprite
        ShowFeedbackSprite(goFoodSprite);

        // Check for speed boost (only in Go zone)
        if (currentFoodZone == FoodType.Go)
        {
            if (currentEnergy >= 100f && !isSpeedBoosted)
            {
                StartSpeedBoost();
            }
            else if (isSpeedBoosted)
            {
                speedBoostTimer += 2f;
                Debug.Log("Speed boost extended by 2 seconds!");
            }
            else
            {
                PlayGoFoodSound();
            }
        }

        UpdateUI();
        Debug.Log($"Go Food Collected! Energy: {currentEnergy}, Score: {score}");
    }

    public void CollectGrowFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return;

        // Play generic collection sound
        PlayCollectionSound();

        currentEnergy += growFoodEnergyGain;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);
        score += foodPoints;

        // Trigger animation
        TriggerStrongAnimation();

        // Show player effect
        ShowFoodReactionEffect();

        // Show feedback sprite
        ShowFeedbackSprite(growFoodSprite);

        // Check for size boost (only in Grow zone)
        if (currentFoodZone == FoodType.Grow)
        {
            if (currentEnergy >= 100f && !isSizeBoosted)
            {
                StartSizeBoost();
            }
            else if (isSizeBoosted)
            {
                sizeBoostTimer += 2f;
                Debug.Log("Size boost extended by 2 seconds!");
            }
            else
            {
                PlayGrowFoodSound();
            }
        }

        UpdateUI();
        Debug.Log($"Grow Food Collected! Energy: {currentEnergy}, Score: {score}");
    }

    public void CollectGlowFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return;

        // Play generic collection sound
        PlayCollectionSound();

        currentEnergy += glowFoodEnergyGain;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);
        score += foodPoints;

        // Trigger animation
        TriggerGlowAnimation();

        // Show player effect
        ShowFoodReactionEffect();

        // Show feedback sprite
        ShowFeedbackSprite(glowFoodSprite);

        // We'll implement glow boost mechanics later
        PlayGlowFoodSound();

        UpdateUI();
        Debug.Log($"Glow Food Collected! Energy: {currentEnergy}, Score: {score}");
    }

    public void CollectJunkFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return;

        // Play generic collection sound
        PlayCollectionSound();

        currentEnergy -= junkFoodEnergyDeduction;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);
        score = Mathf.Max(0, score - junkFoodPointsDeduction);

        // Play sound
        PlayJunkFoodSound();

        // Trigger stomach ache animation
        TriggerStomachAcheAnimation();

        // Show bad effect
        ShowBadEffect();

        // Show feedback sprite
        ShowFeedbackSprite(junkFoodSprite);

        UpdateUI();
        Debug.Log($"Junk Food Collected! Energy: {currentEnergy}, Score: {score}");
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

        // Show speed UI
        ShowBoostUI(FoodType.Go);

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

        // Hide speed UI
        HideAllBoostUI();

        Debug.Log("Speed Boost Ended!");
    }

    private void StartSizeBoost()
    {
        isSizeBoosted = true;
        sizeBoostTimer = sizeBoostDuration;

        // Play size boost sound
        if (AudioHandler.Instance != null && sizeBoostSound != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(sizeBoostSound);
        }

        // Show size boost effect
        if (sizeBoostEffect != null)
        {
            sizeBoostEffect.SetActive(true);
        }

        // Show size UI
        ShowBoostUI(FoodType.Grow);

        Debug.Log("Size Boost Activated for " + sizeBoostDuration + " seconds!");
    }

    private void EndSizeBoost()
    {
        isSizeBoosted = false;
        sizeBoostTimer = 0f;

        // Hide size boost effect
        if (sizeBoostEffect != null)
        {
            sizeBoostEffect.SetActive(false);
        }

        // Hide size UI
        HideAllBoostUI();

        Debug.Log("Size Boost Ended!");
    }

    public void LoseLifeAmount(float amount, bool respawnAtCheckpoint = true)
    {
        if (!gameIsActive) return;

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
                characterAnimator.SetBool(stomachAcheTrigger, false);
                characterAnimator.SetBool(strongTrigger, false);
                characterAnimator.SetBool(glowTrigger, false);
            }

            // Hide all visual effects
            HideAllVisualEffects();

            // Hide all boost UI
            HideAllBoostUI();

            // Reset boosts
            isSpeedBoosted = false;
            isSizeBoosted = false;
            speedBoostTimer = 0f;
            sizeBoostTimer = 0f;

            // Stop any running coroutines
            StopAllCoroutines();

            Debug.Log($"Lost {amount} life! Current life: {currentLifeAmount}");
        }
    }

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
            resetExciteCoroutine = StartCoroutine(ResetAnimation(exciteTrigger));
        }
    }

    private void TriggerStomachAcheAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetStomachAcheCoroutine != null)
            {
                StopCoroutine(resetStomachAcheCoroutine);
            }

            characterAnimator.SetBool(stomachAcheTrigger, true);
            resetStomachAcheCoroutine = StartCoroutine(ResetAnimation(stomachAcheTrigger));
        }
    }

    private void TriggerStrongAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetStrongCoroutine != null)
            {
                StopCoroutine(resetStrongCoroutine);
            }

            characterAnimator.SetBool(strongTrigger, true);
            resetStrongCoroutine = StartCoroutine(ResetAnimation(strongTrigger));
        }
    }

    private void TriggerGlowAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetGlowCoroutine != null)
            {
                StopCoroutine(resetGlowCoroutine);
            }

            characterAnimator.SetBool(glowTrigger, true);
            resetGlowCoroutine = StartCoroutine(ResetAnimation(glowTrigger));
        }
    }

    private IEnumerator ResetAnimation(string triggerName)
    {
        yield return new WaitForSeconds(1f);

        if (characterAnimator != null)
        {
            characterAnimator.SetBool(triggerName, false);
        }

        // Reset the appropriate coroutine reference
        switch (triggerName)
        {
            case "isExcite":
                resetExciteCoroutine = null;
                break;
            case "isStomachAche":
                resetStomachAcheCoroutine = null;
                break;
            case "isStrong":
                resetStrongCoroutine = null;
                break;
            case "isGlow":
                resetGlowCoroutine = null;
                break;
        }
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
            stopFoodReactionCoroutine = StartCoroutine(HideEffectAfterTime(foodReactionEffect, 2f));
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
            stopBadEffectCoroutine = StartCoroutine(HideEffectAfterTime(badEffect, 2f));
        }
    }

    private IEnumerator HideEffectAfterTime(GameObject effect, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (effect != null)
        {
            effect.SetActive(false);
        }
    }

    private void ShowFeedbackSprite(Sprite sprite)
    {
        if (feedbackSpriteObject != null && sprite != null)
        {
            SpriteRenderer spriteRenderer = feedbackSpriteObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (spriteDisplayCoroutine != null)
                {
                    StopCoroutine(spriteDisplayCoroutine);
                }

                spriteRenderer.sprite = sprite;
                feedbackSpriteObject.SetActive(true);
                spriteDisplayCoroutine = StartCoroutine(HideFeedbackSprite());
            }
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

    private void PlayGrowFoodSound()
    {
        if (AudioHandler.Instance != null && growFoodSounds.Length > 0)
        {
            AudioClip randomClip = growFoodSounds[Random.Range(0, growFoodSounds.Length)];
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(randomClip);
        }
    }

    private void PlayGlowFoodSound()
    {
        if (AudioHandler.Instance != null && glowFoodSounds.Length > 0)
        {
            AudioClip randomClip = glowFoodSounds[Random.Range(0, glowFoodSounds.Length)];
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

    private void PlayCollectionSound()
    {
        if (AudioHandler.Instance != null && collectionSound != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(collectionSound);
        }
    }

    public void EnterHealingZone()
    {
        if (!gameIsActive) return;

        inHealingZone = true;

        if (healingZoneAudioSource != null && !healingZoneAudioSource.isPlaying)
        {
            healingZoneAudioSource.Play();
        }

        Debug.Log("Entered Healing Zone");
    }

    public void ExitHealingZone()
    {
        if (!gameIsActive) return;

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
    public bool IsSizeBoosted() => isSizeBoosted;
    public FoodType GetCurrentFoodZone() => currentFoodZone;
}