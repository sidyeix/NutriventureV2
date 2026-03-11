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

    [Header("Initial Slider Settings")]
    [SerializeField] private FoodType initialZoneType = FoodType.Go;
    [SerializeField] private Color initialSliderFillColor = Color.red;
    [SerializeField] private Sprite initialSliderHandleSprite;

    [Header("Zone Slider Appearances (for Resume)")]
    [SerializeField] private Color goSliderFillColor = Color.red;
    [SerializeField] private Sprite goSliderHandleSprite;
    [SerializeField] private Color growSliderFillColor = Color.green;
    [SerializeField] private Sprite growSliderHandleSprite;
    [SerializeField] private Color glowSliderFillColor = new Color(0.5f, 0f, 1f);
    [SerializeField] private Sprite glowSliderHandleSprite;

    [Header("Player Settings")]
    public ThirdPersonController playerController;
    public Transform playerTransform;
    public Transform playerArmature;
    public int maxLives = 5;
    private int currentLives;
    private float currentLifeAmount;

    [Header("Initial Player Settings")]
    public float initialPlayerSpeed = 5f;
    public float initialPlayerSize = 1f;

    [Header("Knockback Settings")]
    public bool enableKnockback = true;
    public float defaultKnockbackForce = 3f;
    public float knockbackDuration = 0.3f;
    private bool isKnockbackActive = false;
    private Vector3 knockbackDirection;
    private float knockbackForce;
    private Coroutine knockbackCoroutine;
    private Vector3 currentKnockbackVelocity;

    [Header("Global Damage Cooldown")]
    public bool enableGlobalDamageCooldown = true;
    public float globalDamageCooldown = 1f;
    private float lastDamageTime = 0f;
    private bool isDamageOnCooldown = false;

    [Header("Slider/Energy Settings")]
    public Slider energySlider;
    public Image sliderFillImage;
    public Image sliderHandleImage;
    public float energyDecreaseRate = 2f;
    public float goFoodEnergyGain = 22f;
    public float growFoodEnergyGain = 22f;
    public float glowFoodEnergyGain = 22f;
    public float junkFoodEnergyDeduction = 20f;
    private float currentEnergy = 0f;
    private float targetEnergy = 0f;
    private bool isEnergyDecreasePaused = false;

    [Header("Smooth Transition Settings")]
    public float energyTransitionSpeed = 5f;
    public float sizeTransitionSpeed = 5f;
    public float speedTransitionSpeed = 5f;

    [Header("Speed Settings - Go Mechanics")]
    public float minSpeed = 2f;
    public float maxSpeed = 7f;
    public float speedBoostAmount = 8f;
    public float speedBoostDuration = 3f;
    private float speedBoostTimer = 0f;
    private bool isSpeedBoosted = false;
    private float targetSpeed = 0f;

    [Header("Size Settings - Grow Mechanics")]
    public float minSize = 0.56f;
    public float maxSize = 3f;
    private bool isSizeBoosted = false;
    private float sizeBoostTimer = 0f;
    public float sizeBoostDuration = 3f;
    private float targetSize = 1f;

    [Header("UI Elements")]
    public List<GameObject> uiElementsToDisable = new List<GameObject>();
    public List<GameObject> uiElementsToEnable = new List<GameObject>();
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public Button startButton;
    public Canvas gameCanvas;

    [Header("One Life UI")]
    public GameObject oneLifePanel;
    public float oneLifeCheckInterval = 0.5f;
    private Coroutine oneLifeCheckCoroutine;
    private bool wasOneLifeLastCheck = false;

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

    [Header("Checkpoint System")]
    public Checkpoint startCheckpoint;
    private Checkpoint currentCheckpoint;
    private bool isRespawning = false;

    [Header("Respawn Settings")]
    public float deathAnimationDuration = 1f;
    public float respawnDelay = 2f;
    private Coroutine respawnCoroutine;

    [Header("Respawn Effect")]
    public GameObject respawnEffect;
    public float respawnEffectDuration = 2f;
    private Coroutine respawnEffectCoroutine;

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
    public string damageTrigger = "isDamaged";
    public string deathTrigger = "isDead";
    private Coroutine resetDamageCoroutine;

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

    [Header("Audio Integration - Male")]
    public AudioClip[] goFoodSoundsMale;
    public AudioClip[] growFoodSoundsMale;
    public AudioClip[] glowFoodSoundsMale;
    public AudioClip[] junkFoodSoundsMale;
    public AudioClip speedBoostSoundMale;

    [Header("Audio Integration - Female")]
    public AudioClip[] goFoodSoundsFemale;
    public AudioClip[] growFoodSoundsFemale;
    public AudioClip[] glowFoodSoundsFemale;
    public AudioClip[] junkFoodSoundsFemale;
    public AudioClip speedBoostSoundFemale;

    [Header("Gender-Neutral Sounds")]
    public AudioClip loseLifeSound;
    public AudioClip collectionSound;
    public AudioClip respawnSound;
    public AudioClip knockbackSound;
    public AudioClip damageSound;
    public AudioClip deathSound;

    [Header("Background Music Settings")]
    public AudioSource backgroundMusicSource;
    public AudioClip gameStartBGM;
    public AudioClip gameEndBGM;

    [Header("Boost Effects")]
    public GameObject speedBoostEffect;
    public GameObject sizeBoostEffect;
    public GameObject glowBoostEffect;

    [Header("Food Feedback UI")]
    public FoodFeedbackUI foodFeedbackUI;

    [Header("Start Game Settings")]
    public float startDelay = 2f;
    public float startEnergy = 100f;
    private bool isStartingGame = false;

    [Header("Low Energy Warning")]
    public GameObject lowEnergyCanvas;
    public float lowEnergyThreshold = 20f;
    public AudioSource lowEnergyAudioSource;
    public AudioClip lowEnergySound;

    private bool inHealingZone = false;
    private Coroutine resetExciteCoroutine;
    private Coroutine resetStomachAcheCoroutine;
    private Coroutine resetStrongCoroutine;
    private Coroutine resetGlowCoroutine;
    private Coroutine spriteDisplayCoroutine;
    private Coroutine stopFoodReactionCoroutine;
    private Coroutine stopBadEffectCoroutine;
    private bool playerSettingsInitialized = false;
    private CharacterController characterController;
    private float originalSpeed;
    private bool isGameTimerPaused = false;
    private float pausedTimerValue = 0f;
    private bool wasLowEnergyLastFrame = false;

    // NEW: Variables for power-up tracking
    private int baseMaxLives; // Store the original max lives without power-ups
    private float timeReductionSeconds = 0f; // Time reduction from equipped pets

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

        // Store the base max lives
        baseMaxLives = maxLives;
    }

    private void Start()
    {
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(false);
        if (energySlider != null)
        {
            energySlider.maxValue = 100f;
            energySlider.minValue = 0f;
            energySlider.value = 0f;
        }

        // Set initial slider appearance
        SetSliderAppearance(initialZoneType, initialSliderFillColor, initialSliderHandleSprite);

        if (playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
            originalSpeed = playerController.MoveSpeed;
        }

        InitializePlayerSettings();
        InitializeHeartSystem();
        HideAllVisualEffects();
        HideAllBoostUI();
        if (oneLifePanel != null) oneLifePanel.SetActive(false);

        targetEnergy = 0f;
        currentEnergy = 0f;
        targetSpeed = initialPlayerSpeed;
        targetSize = initialPlayerSize;

        // Hook up start button programmatically
        // DelayedButtonEnable component on the button handles the 1.5s interactable delay via OnEnable
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (startCheckpoint != null)
        {
            currentCheckpoint = startCheckpoint;
            startCheckpoint.Activate();
        }

        // Initialize with initial zone type
        currentFoodZone = initialZoneType;

        UpdateUI();
        SetGameActive(false);

        // Set initial BGM to gameEndBGM (looping)
        if (backgroundMusicSource != null && gameEndBGM != null)
        {
            backgroundMusicSource.clip = gameEndBGM;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }

        // Initialize low energy canvas
        if (lowEnergyCanvas != null)
        {
            lowEnergyCanvas.SetActive(false);
        }

        // Initialize low energy audio source
        if (lowEnergyAudioSource == null)
        {
            lowEnergyAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (lowEnergyAudioSource != null)
        {
            lowEnergyAudioSource.loop = true;
            lowEnergyAudioSource.playOnAwake = false;
        }

        // Set initial feedback sprite based on starting zone
        UpdateFeedbackSpriteForZone(initialZoneType);

        // Apply power-up bonuses from equipped pets
        ApplyEquippedPetPowerUps();
    }

    // NEW: Apply heart and time power-ups from equipped pets
    private void ApplyEquippedPetPowerUps()
    {
        if (PowerUpManager.Instance == null) return;

        // Apply heart bonus - increase max lives
        int heartBonus = PowerUpManager.Instance.GetStartGameHeartBonus();
        if (heartBonus > 0)
        {
            maxLives = baseMaxLives + heartBonus;
            currentLifeAmount = maxLives; // Start with full lives
            currentLives = maxLives;
#if UNITY_EDITOR
            Debug.Log($"Heart power-up applied: +{heartBonus} lives. Total max lives: {maxLives}");
#endif
        }

        // Apply time reduction
        timeReductionSeconds = PowerUpManager.Instance.GetStartGameTimeReduction();
        if (timeReductionSeconds > 0)
        {
#if UNITY_EDITOR
            Debug.Log($"Time reduction power-up applied: -{timeReductionSeconds} seconds from timer");
#endif
        }

        // Reinitialize heart UI with new max lives
        InitializeHeartSystem();
    }

    // Method to set slider appearance
    private void SetSliderAppearance(FoodType zoneType, Color fillColor, Sprite handleSprite)
    {
        if (sliderFillImage != null)
        {
            sliderFillImage.color = fillColor;
        }

        if (sliderHandleImage != null && handleSprite != null)
        {
            sliderHandleImage.sprite = handleSprite;
        }

#if UNITY_EDITOR
        Debug.Log($"Set initial slider appearance: {zoneType} zone, Color: {fillColor}");
#endif
    }

    /// <summary>
    /// Returns the correct slider fill color and handle sprite for the given zone.
    /// Used by ResumeFromSavedState to restore the correct zone appearance.
    /// </summary>
    private void GetSliderAppearanceForZone(FoodType zone, out Color fillColor, out Sprite handleSprite)
    {
        switch (zone)
        {
            case FoodType.Grow:
                fillColor = growSliderFillColor;
                handleSprite = growSliderHandleSprite;
                break;
            case FoodType.Glow:
                fillColor = glowSliderFillColor;
                handleSprite = glowSliderHandleSprite;
                break;
            default: // FoodType.Go
                fillColor = goSliderFillColor;
                handleSprite = goSliderHandleSprite;
                break;
        }
    }

    private void Update()
    {
        if (!gameIsActive) return;

        // Check energy for low energy warning
        CheckLowEnergyWarning();

        // Only update timer if not paused
        if (!isGameTimerPaused)
        {
            gameTimer += Time.deltaTime;
            UpdateTimerDisplay();
        }

        // Only decrease energy if not paused and not in healing zone/boosted states
        if (!isEnergyDecreasePaused && !inHealingZone && !isSpeedBoosted && !isSizeBoosted)
        {
            targetEnergy -= energyDecreaseRate * Time.deltaTime;
            targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        }

        if (Mathf.Abs(currentEnergy - targetEnergy) > 0.01f)
        {
            currentEnergy = Mathf.Lerp(currentEnergy, targetEnergy, energyTransitionSpeed * Time.deltaTime);
        }
        else
        {
            currentEnergy = targetEnergy;
        }

        if (energySlider != null) energySlider.value = currentEnergy;

        if (currentEnergy <= 0f) LoseLife();

        if (isKnockbackActive && characterController != null)
        {
            characterController.Move(currentKnockbackVelocity * Time.deltaTime);
        }

        switch (currentFoodZone)
        {
            case FoodType.Go: UpdatePlayerSpeed(); break;
            case FoodType.Grow: UpdatePlayerSize(); break;
        }

        if (isSpeedBoosted)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f) EndSpeedBoost();
        }

        if (isSizeBoosted)
        {
            sizeBoostTimer -= Time.deltaTime;
            if (sizeBoostTimer <= 0f) EndSizeBoost();
        }
    }

    // ====== GENDER DETERMINATION ======
    private bool IsCharacterMale()
    {
        // Get the current character ID from GameDataManager
        int characterID = 0; // Default to Eyron (male)

        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            characterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
#if UNITY_EDITOR
            Debug.Log($"Character gender check - ID: {characterID}, Is Male: {characterID == 0 || characterID == 4 || characterID == 6}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("GameDataManager not found, using default male character");
#endif
        }

        // Male characters: Eyron (0), Kaya (4), Albert (6)
        if (characterID == 0 || characterID == 4 || characterID == 6)
        {
            return true;
        }

        // Female characters: Claire (1), Amy (2), Jackie (3), Michelle (5)
        return false;
    }

    // ====== RESET METHODS ======
    public void ResetGameState()
    {
#if UNITY_EDITOR
        Debug.Log("=== RESETTING GAME STATE ===");
#endif

        // Reset all tracking variables
        currentEnergy = 0f;
        targetEnergy = 0f;
        currentLifeAmount = maxLives;
        currentLives = maxLives;
        score = 0;
        gameTimer = 0f;

        // Reset boost states
        isSpeedBoosted = false;
        isSizeBoosted = false;
        speedBoostTimer = 0f;
        sizeBoostTimer = 0f;

        // Reset animation states
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
            characterAnimator.SetBool(stomachAcheTrigger, false);
            characterAnimator.SetBool(strongTrigger, false);
            characterAnimator.SetBool(glowTrigger, false);
            characterAnimator.SetBool(damageTrigger, false);
            characterAnimator.SetBool(deathTrigger, false);
        }

        // Reset flags
        isRespawning = false;
        inHealingZone = false;
        wasLowEnergyLastFrame = false;

        // Reset UI
        if (energySlider != null) energySlider.value = 0f;
        UpdateHeartUI();
        UpdateUI();
        UpdateTimerDisplay();

        // Reset slider to initial appearance
        SetSliderAppearance(initialZoneType, initialSliderFillColor, initialSliderHandleSprite);

        // Reset feedback sprite to initial zone
        UpdateFeedbackSpriteForZone(initialZoneType);

        // Hide all effects
        HideAllVisualEffects();
        HideAllBoostUI();

        // Reset low energy warning
        if (lowEnergyCanvas != null) lowEnergyCanvas.SetActive(false);
        StopLowEnergySound();

        // Reset food spawner
        if (foodSpawner != null)
        {
            foodSpawner.HideAllFood();
        }

#if UNITY_EDITOR
        Debug.Log("Game state reset complete");
#endif
    }

    public void FullGameReset()
    {
#if UNITY_EDITOR
        Debug.Log("=== FULL GAME RESET ===");
#endif

        // IMPORTANT: Reset these BEFORE EndGame()
        gameIsActive = false;
        gameTimer = 0f;
        score = 0;
        currentEnergy = 0f;
        targetEnergy = 0f;

        // Reset timer UI immediately
        UpdateTimerDisplay();
        UpdateUI();

        // Stop all coroutines first
        StopAllCoroutines();

        // Explicitly hide respawn effect since StopAllCoroutines kills the timed hide coroutine
        HideRespawnEffect();

        // Reset one life check
        StopOneLifeCheck();

        // Stop knockback
        StopKnockback();

        // End the current game
        EndGame();

        // Reset all game state
        ResetGameState();

        // Reset all checkpoints
        Checkpoint[] allCheckpoints = FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint checkpoint in allCheckpoints)
        {
            if (checkpoint != null)
                checkpoint.ResetCheckpoint();
        }

        // Re-enable all triggers and colliders
        ResetAllTriggersAndColliders();

        // IMPORTANT: Reset GameEndManager state
        GameEndManager gameEndManager = FindObjectOfType<GameEndManager>();
        if (gameEndManager != null)
        {
            gameEndManager.ResetGameEndState();

            // FIXED: DO NOT call ResetMinigames() here - it's already called by the button handlers
            // This was causing the PlayableDirector to be re-enabled during Home button click
            // gameEndManager.ResetMinigames(); // <-- REMOVED THIS LINE
        }

        // REMOVED: Player position reset - GameEndManager handles this

        // Reset the player controller
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.MoveSpeed = initialPlayerSpeed;
        }

        // Reset player size
        if (playerArmature != null)
        {
            playerArmature.localScale = Vector3.one * initialPlayerSize;
        }

        // Reset all UI
        if (energySlider != null) energySlider.value = 0f;
        UpdateHeartUI();
        UpdateUI();
        UpdateTimerDisplay();

        // Hide all effects
        HideAllVisualEffects();
        HideAllBoostUI();

        // Reset feedback sprite to initial zone
        UpdateFeedbackSpriteForZone(initialZoneType);

        // Reset low energy warning
        if (lowEnergyCanvas != null) lowEnergyCanvas.SetActive(false);
        StopLowEnergySound();

        // Reset audio (looping)
        if (backgroundMusicSource != null && gameEndBGM != null)
        {
            backgroundMusicSource.clip = gameEndBGM;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }

#if UNITY_EDITOR
        Debug.Log("Full game reset complete");
#endif
    }

    // ====== ENERGY SLIDER PAUSE/RESUME LOGIC ======
    public void PauseEnergyDecrease()
    {
        isEnergyDecreasePaused = true;
#if UNITY_EDITOR
        Debug.Log("Energy decrease paused");
#endif
    }

    public void ResumeEnergyDecrease()
    {
        isEnergyDecreasePaused = false;
#if UNITY_EDITOR
        Debug.Log("Energy decrease resumed");
#endif
    }

    public bool IsEnergyDecreasePaused()
    {
        return isEnergyDecreasePaused;
    }

    // ====== GLOBAL DAMAGE COOLDOWN ======
    public bool CanTakeDamage()
    {
        if (!enableGlobalDamageCooldown || !gameIsActive) return true;

        if (isDamageOnCooldown || Time.time - lastDamageTime < globalDamageCooldown)
        {
            return false;
        }

        return true;
    }

    public void StartDamageCooldown()
    {
        if (!enableGlobalDamageCooldown) return;

        lastDamageTime = Time.time;
        isDamageOnCooldown = true;
        Invoke(nameof(ResetDamageCooldown), globalDamageCooldown);
    }

    private void ResetDamageCooldown()
    {
        isDamageOnCooldown = false;
    }

    // ====== KNOCKBACK SYSTEM ======
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (!enableKnockback || isRespawning || !gameIsActive || playerTransform == null) return;

        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(PerformKnockback(direction, force, duration));
    }

    private IEnumerator PerformKnockback(Vector3 direction, float force, float duration)
    {
        isKnockbackActive = true;
        knockbackDirection = direction.normalized;
        knockbackForce = force;

        currentKnockbackVelocity = knockbackDirection * force;

        if (knockbackSound != null && AudioHandler.Instance != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(knockbackSound);

        float elapsedTime = 0f;
        while (elapsedTime < duration && playerTransform != null)
        {
            float t = elapsedTime / duration;
            float currentForce = Mathf.Lerp(force, 0f, t);
            currentKnockbackVelocity = knockbackDirection * currentForce;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentKnockbackVelocity = Vector3.zero;
        isKnockbackActive = false;
        knockbackCoroutine = null;
    }

    public void StopKnockback()
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
        currentKnockbackVelocity = Vector3.zero;
        isKnockbackActive = false;
    }

    // ====== ONE LIFE UI SYSTEM ======
    public void StartOneLifeCheck()
    {
        if (oneLifeCheckCoroutine != null) StopCoroutine(oneLifeCheckCoroutine);
        oneLifeCheckCoroutine = StartCoroutine(CheckOneLifeStatus());
    }

    public void StopOneLifeCheck()
    {
        if (oneLifeCheckCoroutine != null)
        {
            StopCoroutine(oneLifeCheckCoroutine);
            oneLifeCheckCoroutine = null;
        }
        if (oneLifePanel != null) oneLifePanel.SetActive(false);
        wasOneLifeLastCheck = false;
    }

    private IEnumerator CheckOneLifeStatus()
    {
        while (gameIsActive)
        {
            CheckAndUpdateOneLifeUI();
            yield return CoroutineYieldCache.WaitForSeconds(oneLifeCheckInterval);
        }
    }

    private void CheckAndUpdateOneLifeUI()
    {
        if (oneLifePanel == null) return;
        bool isOneLifeNow = currentLifeAmount <= 1f && currentLifeAmount > 0f;

        if (isOneLifeNow != wasOneLifeLastCheck)
        {
            oneLifePanel.SetActive(isOneLifeNow);
            wasOneLifeLastCheck = isOneLifeNow;
        }
    }

    // ====== DAMAGE ANIMATION SYSTEM ======
    public void TriggerDamageAnimation(string triggerName, float duration = 1f)
    {
        if (characterAnimator != null && !string.IsNullOrEmpty(triggerName))
        {
            if (resetDamageCoroutine != null) StopCoroutine(resetDamageCoroutine);
            characterAnimator.SetBool(triggerName, true);
            resetDamageCoroutine = StartCoroutine(ResetDamageAnimation(triggerName, duration));
        }
    }

    private IEnumerator ResetDamageAnimation(string triggerName, float duration)
    {
        yield return CoroutineYieldCache.WaitForSeconds(duration);
        if (characterAnimator != null) characterAnimator.SetBool(triggerName, false);
        resetDamageCoroutine = null;
    }

    // ====== DEATH ANIMATION SYSTEM ======
    private void TriggerDeathAnimation()
    {
        if (characterAnimator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            characterAnimator.SetBool(deathTrigger, true);
            if (deathSound != null && AudioHandler.Instance != null)
                AudioHandler.Instance.soundEffectsSource.PlayOneShot(deathSound);
        }
    }

    private void ResetDeathAnimation()
    {
        if (characterAnimator != null && !string.IsNullOrEmpty(deathTrigger))
            characterAnimator.SetBool(deathTrigger, false);
    }

    // ====== INITIALIZATION ======
    private void InitializePlayerSettings()
    {
        if (playerController != null)
        {
            playerController.MoveSpeed = initialPlayerSpeed;
            targetSpeed = initialPlayerSpeed;
        }

        if (playerArmature != null)
        {
            playerArmature.localScale = Vector3.one * initialPlayerSize;
            targetSize = initialPlayerSize;
        }

        playerSettingsInitialized = true;
    }

    private void InitializeHeartSystem()
    {
        if (heartContainer == null || heartPrefab == null) return;

        foreach (Transform child in heartContainer) Destroy(child.gameObject);
        heartImages.Clear();

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

    // ====== GAME FLOW ======
    public void StartGame()
    {
#if UNITY_EDITOR
        Debug.Log($"StartGame called! isStartingGame={isStartingGame}, gameIsActive={gameIsActive}, timeScale={Time.timeScale}");
#endif

        if (isStartingGame) return;

        isStartingGame = true;
        if (startButton != null) startButton.interactable = false;

        StartCoroutine(DelayedGameStart());
    }

    private IEnumerator DelayedGameStart()
    {
#if UNITY_EDITOR
        Debug.Log($"Game starting in {startDelay} seconds...");
#endif
        yield return CoroutineYieldCache.WaitForSecondsRealtime(startDelay);
        ActualGameStart();
    }

    private void ActualGameStart()
    {
        // Ensure time is running (safeguard against stuck timeScale from settings/pause)
        Time.timeScale = 1f;

        // Clear the starting flag and activate game FIRST so these always execute
        isStartingGame = false;
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(true);
        SetGameActive(true);
        isEnergyDecreasePaused = false;
        isGameTimerPaused = false;

        // Apply power-up bonuses before starting
        ApplyEquippedPetPowerUps();

        // Reset game state before starting
        ResetGameState();

        // Reset assessment system before starting
        ResetAssessmentSystem();

        // Ensure low energy canvas is hidden at game start
        if (lowEnergyCanvas != null)
        {
            lowEnergyCanvas.SetActive(false);
            wasLowEnergyLastFrame = false;
        }

        // Ensure low energy sound is stopped at game start
        StopLowEnergySound();

        // Set starting energy to 100
        currentEnergy = startEnergy;
        targetEnergy = startEnergy;
        if (energySlider != null) energySlider.value = currentEnergy;

        currentLives = maxLives;
        currentLifeAmount = maxLives;
        score = 0;
        gameTimer = 0f;

        // Always start in initial zone
        currentFoodZone = initialZoneType;
        SetSliderAppearance(initialZoneType, initialSliderFillColor, initialSliderHandleSprite);

        // Set initial feedback sprite
        UpdateFeedbackSpriteForZone(initialZoneType);

        // Reset all checkpoints
        Checkpoint[] allCheckpoints = FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint checkpoint in allCheckpoints)
        {
            if (checkpoint != null)
                checkpoint.ResetCheckpoint();
        }

        if (startCheckpoint != null)
        {
            currentCheckpoint = startCheckpoint;
            startCheckpoint.Activate();
        }

        if (playerController != null)
        {
            playerController.MoveSpeed = minSpeed;
            targetSpeed = minSpeed;
        }

        if (playerArmature != null)
        {
            playerArmature.localScale = Vector3.one * initialPlayerSize;
            targetSize = initialPlayerSize;
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
            characterAnimator.SetBool(stomachAcheTrigger, false);
            characterAnimator.SetBool(strongTrigger, false);
            characterAnimator.SetBool(glowTrigger, false);
            characterAnimator.SetBool(damageTrigger, false);
            characterAnimator.SetBool(deathTrigger, false);
        }

        isRespawning = false;
        StopKnockback();
        UpdateHeartUI();
        HideAllVisualEffects();
        HideAllBoostUI();
        StartOneLifeCheck();

        foreach (GameObject uiElement in uiElementsToDisable)
            if (uiElement != null) uiElement.SetActive(false);

        foreach (GameObject uiElement in uiElementsToEnable)
            if (uiElement != null) uiElement.SetActive(true);

        if (startButton != null) startButton.gameObject.SetActive(false);

        // Reset food spawner before starting
        if (foodSpawner != null)
        {
            foodSpawner.StopSpawning();
            foodSpawner.StartSpawning();
        }
        else Debug.LogError("FoodSpawner not assigned to GameManager!");

        // Change background music (looping)
        if (backgroundMusicSource != null && gameStartBGM != null)
        {
            backgroundMusicSource.clip = gameStartBGM;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }

        UpdateUI();
        RespawnPlayer();
#if UNITY_EDITOR
        Debug.Log($"Game Started! Starting energy: {currentEnergy}, Max lives: {maxLives}, Time reduction: {timeReductionSeconds}s");
#endif
    }

    public void EndGame()
    {
        SetGameActive(false);
        isEnergyDecreasePaused = false;
        isGameTimerPaused = false;
        isStartingGame = false;
        StopOneLifeCheck();
        StopKnockback();

        // Hide low energy canvas when game ends
        if (lowEnergyCanvas != null)
        {
            lowEnergyCanvas.SetActive(false);
            wasLowEnergyLastFrame = false;
        }

        // Stop low energy sound when game ends
        StopLowEnergySound();

        if (gameCanvas != null) gameCanvas.gameObject.SetActive(false);
        if (foodSpawner != null) foodSpawner.StopSpawning();

        StopAllCoroutines();
        if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
        if (respawnEffectCoroutine != null) StopCoroutine(respawnEffectCoroutine);

        // Explicitly hide respawn effect since StopAllCoroutines kills the timed hide coroutine
        HideRespawnEffect();

        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
            characterAnimator.SetBool(stomachAcheTrigger, false);
            characterAnimator.SetBool(strongTrigger, false);
            characterAnimator.SetBool(glowTrigger, false);
            characterAnimator.SetBool(damageTrigger, false);
            characterAnimator.SetBool(deathTrigger, false);
        }

        isRespawning = false;

        if (playerSettingsInitialized)
        {
            if (playerController != null)
            {
                playerController.MoveSpeed = initialPlayerSpeed;
                targetSpeed = initialPlayerSpeed;
            }

            if (playerArmature != null)
            {
                playerArmature.localScale = Vector3.one * initialPlayerSize;
                targetSize = initialPlayerSize;
            }
        }

        currentEnergy = 0f;
        targetEnergy = 0f;
        inHealingZone = false;
        HideAllVisualEffects();
        HideAllBoostUI();

        foreach (GameObject uiElement in uiElementsToDisable)
            if (uiElement != null) uiElement.SetActive(true);

        foreach (GameObject uiElement in uiElementsToEnable)
            if (uiElement != null) uiElement.SetActive(false);

        if (startButton != null)
            startButton.gameObject.SetActive(true);

#if UNITY_EDITOR
        Debug.Log("Game Ended!");
#endif
    }

    // ====== PLAYER MECHANICS ======
    private void UpdatePlayerSpeed()
    {
        if (playerController == null) return;

        if (isSpeedBoosted) targetSpeed = speedBoostAmount;
        else targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, currentEnergy / 100f);

        if (Mathf.Abs(playerController.MoveSpeed - targetSpeed) > 0.01f)
            playerController.MoveSpeed = Mathf.Lerp(playerController.MoveSpeed, targetSpeed, speedTransitionSpeed * Time.deltaTime);
        else playerController.MoveSpeed = targetSpeed;
    }

    private void UpdatePlayerSize()
    {
        if (playerArmature == null) return;

        if (isSizeBoosted) targetSize = maxSize;
        else targetSize = Mathf.Lerp(minSize, maxSize, currentEnergy / 100f);

        if (Mathf.Abs(playerArmature.localScale.x - targetSize) > 0.01f)
        {
            float newSize = Mathf.Lerp(playerArmature.localScale.x, targetSize, sizeTransitionSpeed * Time.deltaTime);
            playerArmature.localScale = Vector3.one * newSize;
        }
        else playerArmature.localScale = Vector3.one * targetSize;
    }

    // ====== ZONE SWITCHING WITH FEEDBACK SPRITE UPDATE ======
    public void SetCurrentFoodZone(FoodType zoneType, Color fillColor, Sprite handleSprite)
    {
        if (!gameIsActive) return;

        currentFoodZone = zoneType;

        if (sliderFillImage != null)
        {
            sliderFillImage.color = fillColor;
        }

        if (sliderHandleImage != null && handleSprite != null)
        {
            sliderHandleImage.sprite = handleSprite;
        }

        // Update feedback sprite based on zone
        UpdateFeedbackSpriteForZone(zoneType);

        if (zoneType == FoodType.Grow && playerArmature != null)
        {
            UpdatePlayerSize();
        }
        else if (zoneType == FoodType.Go && playerArmature != null)
        {
            targetSize = initialPlayerSize;
        }

#if UNITY_EDITOR
        Debug.Log($"Switched to {zoneType} zone, feedback sprite updated");
#endif
    }

    // Helper method to update feedback sprite for zone
    private void UpdateFeedbackSpriteForZone(FoodType zoneType)
    {
        if (feedbackSpriteObject == null) return;

        Sprite feedbackSprite = null;
        switch (zoneType)
        {
            case FoodType.Go:
                feedbackSprite = goFoodSprite;
                break;
            case FoodType.Grow:
                feedbackSprite = growFoodSprite;
                break;
            case FoodType.Glow:
                feedbackSprite = glowFoodSprite;
                break;
        }

        // Update the feedback sprite object
        if (feedbackSprite != null)
        {
            SpriteRenderer spriteRenderer = feedbackSpriteObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = feedbackSprite;
#if UNITY_EDITOR
                Debug.Log($"Feedback sprite updated to {zoneType} sprite");
#endif
            }
            else
            {
                Image image = feedbackSpriteObject.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = feedbackSprite;
#if UNITY_EDITOR
                    Debug.Log($"Feedback image updated to {zoneType} sprite");
#endif
                }
            }
        }
    }

    // ====== FOOD COLLECTION ======
    public void CollectHealing(float amount)
    {
        if (!gameIsActive || !inHealingZone) return;
        targetEnergy += amount;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
    }

    public void CollectGoFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return;

        PlayCollectionSound();
        targetEnergy += goFoodEnergyGain;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        score += foodPoints;

        TriggerExciteAnimation();
        ShowFoodReactionEffect();
        ShowFeedbackSprite(goFoodSprite);

        if (currentFoodZone == FoodType.Go)
        {
            if (targetEnergy >= 100f && !isSpeedBoosted) StartSpeedBoost();
            else if (isSpeedBoosted) speedBoostTimer += 2f;
            else PlayGoFoodSound();
        }

        UpdateUI();
    }

    public void CollectGrowFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return;

        PlayCollectionSound();
        targetEnergy += growFoodEnergyGain;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        score += foodPoints;

        TriggerStrongAnimation();
        ShowFoodReactionEffect();
        ShowFeedbackSprite(growFoodSprite);

        PlayGrowFoodSound();  // <-- MOVE THIS OUTSIDE THE ZONE CHECK

        if (currentFoodZone == FoodType.Grow)
        {
            if (targetEnergy >= 100f && !isSizeBoosted) StartSizeBoost();
            else if (isSizeBoosted) sizeBoostTimer += 2f;
        }

        UpdateUI();
    }

    public void CollectGlowFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return;

        PlayCollectionSound();
        targetEnergy += glowFoodEnergyGain;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        score += foodPoints;

        TriggerGlowAnimation();
        ShowFoodReactionEffect();
        ShowFeedbackSprite(glowFoodSprite);
        PlayGlowFoodSound();

        UpdateUI();
    }

    public void CollectJunkFood(GameObject foodObject = null)
    {
        if (!gameIsActive) return;

        PlayCollectionSound();
        targetEnergy -= junkFoodEnergyDeduction;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        score = Mathf.Max(0, score - junkFoodPointsDeduction);

        PlayJunkFoodSound();
        TriggerStomachAcheAnimation();
        ShowBadEffect();
        ShowFeedbackSprite(junkFoodSprite);

        UpdateUI();
    }

    // ====== BOOSTS ======
    private void StartSpeedBoost()
    {
        isSpeedBoosted = true;
        speedBoostTimer = speedBoostDuration;

        PlaySpeedBoostSound();

        if (speedBoostEffect != null) speedBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Go);
    }

    private void EndSpeedBoost()
    {
        isSpeedBoosted = false;
        speedBoostTimer = 0f;
        if (speedBoostEffect != null) speedBoostEffect.SetActive(false);
        HideAllBoostUI();
    }

    private void StartSizeBoost()
    {
        isSizeBoosted = true;
        sizeBoostTimer = sizeBoostDuration;

        // Note: You mentioned we don't need size boost sound
        // if (AudioHandler.Instance != null && sizeBoostSound != null)
        //     AudioHandler.Instance.soundEffectsSource.PlayOneShot(sizeBoostSound);

        if (sizeBoostEffect != null) sizeBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Grow);
    }

    private void EndSizeBoost()
    {
        isSizeBoosted = false;
        sizeBoostTimer = 0f;
        if (sizeBoostEffect != null) sizeBoostEffect.SetActive(false);
        HideAllBoostUI();
    }

    // ====== DAMAGE & LIFE LOSS ======
    public void LoseLifeAmount(float amount, bool respawnAtCheckpoint = true)
    {
        if (!gameIsActive || isRespawning) return;

        StartDamageCooldown();

        if (respawnAtCheckpoint)
        {
            LoseLife();
        }
        else
        {
            currentLifeAmount -= amount;
            currentLifeAmount = Mathf.Max(0f, currentLifeAmount);
            currentLives = Mathf.CeilToInt(currentLifeAmount);

            CheckAndUpdateOneLifeUI();

            if (damageSound != null && AudioHandler.Instance != null)
                AudioHandler.Instance.soundEffectsSource.PlayOneShot(damageSound);

            UpdateHeartUI();

            if (currentLifeAmount <= 0f)
            {
                EndGame();
            }
            else
            {
                if (characterAnimator != null)
                {
                    characterAnimator.SetBool(exciteTrigger, false);
                    characterAnimator.SetBool(stomachAcheTrigger, false);
                    characterAnimator.SetBool(strongTrigger, false);
                    characterAnimator.SetBool(glowTrigger, false);
                }

                HideAllVisualEffects();
                HideAllBoostUI();
                isSpeedBoosted = false;
                isSizeBoosted = false;
                speedBoostTimer = 0f;
                sizeBoostTimer = 0f;
            }
        }
    }

    public void LoseLife()
    {
        if (!gameIsActive || isRespawning) return;

        StartDamageCooldown();

        if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
        respawnCoroutine = StartCoroutine(RespawnProcess());
    }

    private IEnumerator RespawnProcess()
    {
        isRespawning = true;
        StopKnockback();

        if (playerController != null) playerController.enabled = false;

        if (AudioHandler.Instance != null && loseLifeSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(loseLifeSound);

        TriggerDeathAnimation();
        yield return CoroutineYieldCache.WaitForSeconds(deathAnimationDuration);
        ResetDeathAnimation();
        yield return CoroutineYieldCache.WaitForSeconds(respawnDelay);

        RespawnPlayer();
        currentEnergy = 50f;
        targetEnergy = 50f;
        currentLifeAmount -= 1f;
        currentLifeAmount = Mathf.Max(0f, currentLifeAmount);
        currentLives = Mathf.CeilToInt(currentLifeAmount);

        UpdateHeartUI();
        CheckAndUpdateOneLifeUI();

        if (currentLifeAmount <= 0f)
        {
            EndGame();

            // Trigger Game Over screen
            GameEndManager gameEndManager = FindObjectOfType<GameEndManager>();
            if (gameEndManager != null)
            {
                gameEndManager.TriggerGameOver();
            }

            yield break;
        }

        ShowRespawnEffect();

        if (respawnSound != null && AudioHandler.Instance != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(respawnSound);

        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
            characterAnimator.SetBool(stomachAcheTrigger, false);
            characterAnimator.SetBool(strongTrigger, false);
            characterAnimator.SetBool(glowTrigger, false);
            characterAnimator.SetBool(damageTrigger, false);
        }

        if (playerController != null) playerController.enabled = true;

        HideAllVisualEffects();
        HideAllBoostUI();
        isSpeedBoosted = false;
        isSizeBoosted = false;
        speedBoostTimer = 0f;
        sizeBoostTimer = 0f;

        isRespawning = false;
#if UNITY_EDITOR
        Debug.Log("Respawn complete!");
#endif
    }

    // ====== VISUAL EFFECTS ======
    private void HideAllVisualEffects()
    {
        if (feedbackSpriteObject != null) feedbackSpriteObject.SetActive(false);
        if (foodReactionEffect != null) foodReactionEffect.SetActive(false);
        if (badEffect != null) badEffect.SetActive(false);
        if (speedBoostEffect != null) speedBoostEffect.SetActive(false);
        if (sizeBoostEffect != null) sizeBoostEffect.SetActive(false);
        if (glowBoostEffect != null) glowBoostEffect.SetActive(false);
    }

    private void HideRespawnEffect()
    {
        if (respawnEffect != null) respawnEffect.SetActive(false);
    }

    private void HideAllBoostUI()
    {
        if (speedLinesCanvas != null) speedLinesCanvas.gameObject.SetActive(false);
        if (speedBoostIndicator != null) speedBoostIndicator.SetActive(false);
        if (sizeBoostIndicator != null) sizeBoostIndicator.SetActive(false);
        if (glowBoostIndicator != null) glowBoostIndicator.SetActive(false);
    }

    private void ShowBoostUI(FoodType boostType)
    {
        HideAllBoostUI();

        switch (boostType)
        {
            case FoodType.Go:
                if (speedLinesCanvas != null) speedLinesCanvas.gameObject.SetActive(true);
                if (speedBoostIndicator != null) speedBoostIndicator.SetActive(true);
                break;
            case FoodType.Grow:
                if (sizeBoostIndicator != null) sizeBoostIndicator.SetActive(true);
                break;
            case FoodType.Glow:
                if (glowBoostIndicator != null) glowBoostIndicator.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Shows the respawn VFX at its scene position and auto-hides it after respawnEffectDuration.
    /// Called internally on checkpoint respawn, and externally by GameEndManager on lobby teleport.
    /// </summary>
    public void ShowRespawnEffect()
    {
        if (respawnEffect != null)
        {
            respawnEffect.SetActive(true);

            if (respawnEffectCoroutine != null) StopCoroutine(respawnEffectCoroutine);
            respawnEffectCoroutine = StartCoroutine(HideRespawnEffectAfterDelay());
        }
    }

    /// <summary>
    /// Plays the respawn sound effect. Called externally by GameEndManager on lobby teleport.
    /// </summary>
    public void PlayRespawnSound()
    {
        if (respawnSound != null && AudioHandler.Instance != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(respawnSound);
    }

    private IEnumerator HideRespawnEffectAfterDelay()
    {
        yield return CoroutineYieldCache.WaitForSeconds(respawnEffectDuration);
        HideRespawnEffect();
        respawnEffectCoroutine = null;
    }

    // ====== ANIMATIONS ======
    private void TriggerExciteAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetExciteCoroutine != null) StopCoroutine(resetExciteCoroutine);
            characterAnimator.SetBool(exciteTrigger, true);
            resetExciteCoroutine = StartCoroutine(ResetAnimation(exciteTrigger));
        }
    }

    private void TriggerStomachAcheAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetStomachAcheCoroutine != null) StopCoroutine(resetStomachAcheCoroutine);
            characterAnimator.SetBool(stomachAcheTrigger, true);
            resetStomachAcheCoroutine = StartCoroutine(ResetAnimation(stomachAcheTrigger));
        }
    }

    private void TriggerStrongAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetStrongCoroutine != null) StopCoroutine(resetStrongCoroutine);
            characterAnimator.SetBool(strongTrigger, true);
            resetStrongCoroutine = StartCoroutine(ResetAnimation(strongTrigger));
        }
    }

    private void TriggerGlowAnimation()
    {
        if (characterAnimator != null)
        {
            if (resetGlowCoroutine != null) StopCoroutine(resetGlowCoroutine);
            characterAnimator.SetBool(glowTrigger, true);
            resetGlowCoroutine = StartCoroutine(ResetAnimation(glowTrigger));
        }
    }

    private IEnumerator ResetAnimation(string triggerName)
    {
        yield return CoroutineYieldCache.WaitForSeconds(1f);
        if (characterAnimator != null) characterAnimator.SetBool(triggerName, false);

        switch (triggerName)
        {
            case "isExcite": resetExciteCoroutine = null; break;
            case "isStomachAche": resetStomachAcheCoroutine = null; break;
            case "isStrong": resetStrongCoroutine = null; break;
            case "isGlow": resetGlowCoroutine = null; break;
        }
    }

    private void ShowFoodReactionEffect()
    {
        if (foodReactionEffect != null)
        {
            if (stopFoodReactionCoroutine != null) StopCoroutine(stopFoodReactionCoroutine);
            foodReactionEffect.SetActive(true);
            stopFoodReactionCoroutine = StartCoroutine(HideEffectAfterTime(foodReactionEffect, 2f));
        }
    }

    private void ShowBadEffect()
    {
        if (badEffect != null)
        {
            if (stopBadEffectCoroutine != null) StopCoroutine(stopBadEffectCoroutine);
            badEffect.SetActive(true);
            stopBadEffectCoroutine = StartCoroutine(HideEffectAfterTime(badEffect, 2f));
        }
    }

    private IEnumerator HideEffectAfterTime(GameObject effect, float duration)
    {
        yield return CoroutineYieldCache.WaitForSeconds(duration);
        if (effect != null) effect.SetActive(false);
    }

    private void ShowFeedbackSprite(Sprite sprite)
    {
        if (feedbackSpriteObject != null && sprite != null)
        {
            SpriteRenderer spriteRenderer = feedbackSpriteObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (spriteDisplayCoroutine != null) StopCoroutine(spriteDisplayCoroutine);
                spriteRenderer.sprite = sprite;
                feedbackSpriteObject.SetActive(true);
                spriteDisplayCoroutine = StartCoroutine(HideFeedbackSprite());
            }
            else
            {
                Image image = feedbackSpriteObject.GetComponent<Image>();
                if (image != null)
                {
                    if (spriteDisplayCoroutine != null) StopCoroutine(spriteDisplayCoroutine);
                    image.sprite = sprite;
                    feedbackSpriteObject.SetActive(true);
                    spriteDisplayCoroutine = StartCoroutine(HideFeedbackSprite());
                }
            }
        }
    }

    private IEnumerator HideFeedbackSprite()
    {
        yield return CoroutineYieldCache.WaitForSeconds(spriteDisplayTime);
        if (feedbackSpriteObject != null) feedbackSpriteObject.SetActive(false);
        spriteDisplayCoroutine = null;
    }

    // ====== GENDER-BASED AUDIO ======
    private void PlayGoFoodSound()
    {
        bool isMale = IsCharacterMale();
        AudioClip[] soundArray = isMale ? goFoodSoundsMale : goFoodSoundsFemale;

        if (AudioHandler.Instance != null && soundArray != null && soundArray.Length > 0)
        {
            AudioClip sound = soundArray[Random.Range(0, soundArray.Length)];
            if (sound != null)
                AudioHandler.Instance.soundEffectsSource.PlayOneShot(sound);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No {(isMale ? "male" : "female")} Go food sounds found or AudioHandler not available");
#endif
        }
    }

    private void PlayGrowFoodSound()
    {
        bool isMale = IsCharacterMale();
        AudioClip[] soundArray = isMale ? growFoodSoundsMale : growFoodSoundsFemale;

        if (AudioHandler.Instance != null && soundArray != null && soundArray.Length > 0)
        {
            AudioClip sound = soundArray[Random.Range(0, soundArray.Length)];
            if (sound != null)
                AudioHandler.Instance.soundEffectsSource.PlayOneShot(sound);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No {(isMale ? "male" : "female")} Grow food sounds found or AudioHandler not available");
#endif
        }
    }

    private void PlayGlowFoodSound()
    {
        bool isMale = IsCharacterMale();
        AudioClip[] soundArray = isMale ? glowFoodSoundsMale : glowFoodSoundsFemale;

        if (AudioHandler.Instance != null && soundArray != null && soundArray.Length > 0)
        {
            AudioClip sound = soundArray[Random.Range(0, soundArray.Length)];
            if (sound != null)
                AudioHandler.Instance.soundEffectsSource.PlayOneShot(sound);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No {(isMale ? "male" : "female")} Glow food sounds found or AudioHandler not available");
#endif
        }
    }

    private void PlayJunkFoodSound()
    {
        bool isMale = IsCharacterMale();
        AudioClip[] soundArray = isMale ? junkFoodSoundsMale : junkFoodSoundsFemale;

        if (AudioHandler.Instance != null && soundArray != null && soundArray.Length > 0)
        {
            AudioClip sound = soundArray[Random.Range(0, soundArray.Length)];
            if (sound != null)
                AudioHandler.Instance.soundEffectsSource.PlayOneShot(sound);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No {(isMale ? "male" : "female")} Junk food sounds found or AudioHandler not available");
#endif
        }
    }

    private void PlaySpeedBoostSound()
    {
        bool isMale = IsCharacterMale();
        AudioClip sound = isMale ? speedBoostSoundMale : speedBoostSoundFemale;

        if (AudioHandler.Instance != null && sound != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(sound);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No {(isMale ? "male" : "female")} speed boost sound found or AudioHandler not available");
#endif
        }
    }

    private void PlayCollectionSound()
    {
        if (AudioHandler.Instance != null && collectionSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(collectionSound);
    }

    // ====== UI ======
    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"{score}";
        if (livesText != null) livesText.text = $"Lives: {Mathf.CeilToInt(currentLifeAmount)}";
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
                    heartImages[i].sprite = fullHeart;
                    remainingLife -= 1f;
                }
                else if (remainingLife >= 0.5f)
                {
                    heartImages[i].sprite = halfHeart;
                    remainingLife -= 0.5f;
                }
                else heartImages[i].sprite = emptyHeart;
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            // Apply time reduction if any
            float adjustedTime = Mathf.Max(0, gameTimer - timeReductionSeconds);
            int minutes = Mathf.FloorToInt(adjustedTime / 60f);
            int seconds = Mathf.FloorToInt(adjustedTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void SetGameActive(bool active)
    {
        gameIsActive = active;
        if (playerController != null) playerController.enabled = active;
    }

    // ====== PUBLIC METHODS ======
    public void EnterHealingZone() { if (!gameIsActive) return; inHealingZone = true; }
    public void ExitHealingZone() { if (!gameIsActive) return; inHealingZone = false; }

    public void SetCurrentCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null || !checkpoint.IsActivated()) return;
        currentCheckpoint = checkpoint;
#if UNITY_EDITOR
        Debug.Log($"Checkpoint set to: {checkpoint.gameObject.name}");
#endif
    }

    private void RespawnPlayer()
    {
        if (playerTransform == null || currentCheckpoint == null) return;
        playerTransform.position = currentCheckpoint.GetSpawnPosition();

        // Apply rotation to both player transform and armature
        Quaternion spawnRotation = currentCheckpoint.GetSpawnRotation();
        playerTransform.rotation = spawnRotation;

        if (playerArmature != null)
        {
            playerArmature.rotation = spawnRotation;
        }

#if UNITY_EDITOR
        Debug.Log($"Respawned at: {currentCheckpoint.gameObject.name}");
#endif
    }

    public void RespawnAllFood()
    {
        if (foodSpawner != null && foodSpawner.IsSpawningEnabled())
            foodSpawner.RespawnAllFood();
    }

    public void ShowFoodFeedback(Sprite foodSprite)
    {
        if (foodFeedbackUI != null && foodSprite != null)
            foodFeedbackUI.ShowFoodFeedback(foodSprite);
    }

    public void AddPoints(int points)
    {
        if (!gameIsActive) return;

        score += points;
        score = Mathf.Max(0, score);
        UpdateUI();
    }

    public void AddEnergy(float amount)
    {
        if (!gameIsActive)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"AddEnergy called but game is not active! Amount: {amount}");
#endif
            return;
        }

        targetEnergy += amount;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
#if UNITY_EDITOR
        Debug.Log($"Energy added: {amount}. New target: {targetEnergy}. Current: {currentEnergy}");
#endif
    }

    public void RemoveEnergy(float amount)
    {
        if (!gameIsActive)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"RemoveEnergy called but game is not active! Amount: {amount}");
#endif
            return;
        }

        targetEnergy -= amount;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
#if UNITY_EDITOR
        Debug.Log($"Energy removed: {amount}. New target: {targetEnergy}. Current: {currentEnergy}");
#endif
    }

    public void SetEnergy(float amount)
    {
        if (!gameIsActive) return;

        targetEnergy = Mathf.Clamp(amount, 0f, 100f);
        currentEnergy = targetEnergy;

        if (energySlider != null)
            energySlider.value = currentEnergy;

#if UNITY_EDITOR
        Debug.Log($"Energy set to: {targetEnergy}");
#endif
    }

    // ====== BOOST METHODS ======
    public void TriggerSpeedBoost(float duration = 10f)
    {
        if (!gameIsActive || isRespawning) return;

        isSpeedBoosted = true;
        speedBoostTimer = duration;

        PlaySpeedBoostSound();

        if (speedBoostEffect != null) speedBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Go);

#if UNITY_EDITOR
        Debug.Log($"Speed boost activated for {duration} seconds");
#endif
    }

    public void TriggerSizeBoost(float duration = 10f)
    {
        if (!gameIsActive || isRespawning) return;

        isSizeBoosted = true;
        sizeBoostTimer = duration;

        // Note: You mentioned we don't need size boost sound
        // if (AudioHandler.Instance != null && sizeBoostSound != null)
        //     AudioHandler.Instance.soundEffectsSource.PlayOneShot(sizeBoostSound);

        if (sizeBoostEffect != null) sizeBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Grow);

#if UNITY_EDITOR
        Debug.Log($"Size boost activated for {duration} seconds");
#endif
    }

    public void TriggerGlowBoost(float duration = 10f)
    {
        if (!gameIsActive || isRespawning) return;

        // Note: You mentioned we don't need glow foods sound
        // if (AudioHandler.Instance != null && glowBoostSound != null)
        //     AudioHandler.Instance.soundEffectsSource.PlayOneShot(glowBoostSound);

        if (glowBoostEffect != null) glowBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Glow);

#if UNITY_EDITOR
        Debug.Log($"Glow boost activated for {duration} seconds");
#endif
    }

    public void PauseGameTimer()
    {
        if (!gameIsActive) return;

        isGameTimerPaused = true;
        pausedTimerValue = gameTimer;
#if UNITY_EDITOR
        Debug.Log("Game timer paused");
#endif
    }

    public void ResumeGameTimer()
    {
        if (!gameIsActive) return;

        isGameTimerPaused = false;
#if UNITY_EDITOR
        Debug.Log("Game timer resumed");
#endif
    }

    public bool IsGameTimerPaused()
    {
        return isGameTimerPaused;
    }

    // Method to check and update low energy warning
    private void CheckLowEnergyWarning()
    {
        if (lowEnergyCanvas == null) return;

        bool isLowEnergyNow = currentEnergy <= lowEnergyThreshold && currentEnergy > 0f;

        if (isLowEnergyNow != wasLowEnergyLastFrame)
        {
            lowEnergyCanvas.SetActive(isLowEnergyNow);

            // Handle low energy audio
            if (isLowEnergyNow)
            {
#if UNITY_EDITOR
                Debug.Log($"Low energy warning shown! Energy: {currentEnergy}");
#endif
                PlayLowEnergySound();
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"Low energy warning hidden. Energy: {currentEnergy}");
#endif
                StopLowEnergySound();
            }
        }

        wasLowEnergyLastFrame = isLowEnergyNow;
    }

    // Method to play low energy sound
    private void PlayLowEnergySound()
    {
        if (lowEnergyAudioSource != null && lowEnergySound != null)
        {
            lowEnergyAudioSource.clip = lowEnergySound;
            lowEnergyAudioSource.loop = true;
            if (!lowEnergyAudioSource.isPlaying)
            {
                lowEnergyAudioSource.Play();
#if UNITY_EDITOR
                Debug.Log("Started low energy warning sound");
#endif
            }
        }
        else if (lowEnergyAudioSource == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Low energy audio source not assigned!");
#endif
        }
        else if (lowEnergySound == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Low energy sound clip not assigned!");
#endif
        }
    }

    // Method to stop low energy sound
    private void StopLowEnergySound()
    {
        if (lowEnergyAudioSource != null && lowEnergyAudioSource.isPlaying)
        {
            lowEnergyAudioSource.Stop();
#if UNITY_EDITOR
            Debug.Log("Stopped low energy warning sound");
#endif
        }
    }

    private void ResetAssessmentSystem()
    {
#if UNITY_EDITOR
        Debug.Log("=== RESETTING ASSESSMENT SYSTEM ===");
#endif

        // Reset Grow Assessment Manager
        GrowAssessmentManager assessmentManager = FindObjectOfType<GrowAssessmentManager>();
        if (assessmentManager != null)
        {
            // DON'T call CompleteResetForNewGame at game start
            // Only call EndGrowAssessment if it's active
            if (assessmentManager.IsAssessmentActive() || assessmentManager.IsWaitingForEndTrigger())
            {
                assessmentManager.EndGrowAssessment();
#if UNITY_EDITOR
                Debug.Log("Ended active assessment at game start");
#endif
            }
            else
            {
                // Just reset the state without moving the panel
                assessmentManager.ResetForNewAssessmentWithoutMovingPanel();
#if UNITY_EDITOR
                Debug.Log("Reset assessment state without moving panel");
#endif
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("No GrowAssessmentManager found in scene");
#endif
        }

        // Reset all Object Group Managers
        ObjectGroupManager[] groupManagers = FindObjectsOfType<ObjectGroupManager>();
        foreach (ObjectGroupManager manager in groupManagers)
        {
            if (manager != null)
            {
                manager.ResetGroupForNewGame();
            }
        }

        // Reset all Assessment Triggers
        AssessmentTrigger[] triggers = FindObjectsOfType<AssessmentTrigger>();
        foreach (AssessmentTrigger trigger in triggers)
        {
            if (trigger != null)
            {
                trigger.ResetTrigger();
            }
        }

#if UNITY_EDITOR
        Debug.Log($"Assessment System reset: {groupManagers.Length} groups, {triggers.Length} triggers");
#endif
    }

    private void ResetAllTriggersAndColliders()
    {
        // Find all trigger colliders and re-enable them
        AssessmentTrigger[] assessmentTriggers = FindObjectsOfType<AssessmentTrigger>();
        foreach (AssessmentTrigger trigger in assessmentTriggers)
        {
            if (trigger != null)
            {
                Collider collider = trigger.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
        }

        // Reset glow part triggers
        GlowPartTrigger[] glowTriggers = FindObjectsOfType<GlowPartTrigger>();
        foreach (GlowPartTrigger trigger in glowTriggers)
        {
            if (trigger != null)
            {
                Collider collider = trigger.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
        }
    }

    // ====== PUBLIC GETTERS ======
    public float GetCurrentLifeAmount() => currentLifeAmount;
    public int GetCurrentLives() => currentLives;
    public float GetCurrentEnergy() => currentEnergy;
    public int GetCurrentScore() => score;
    public bool IsGameActive() => gameIsActive;
    public bool IsSpeedBoosted() => isSpeedBoosted;
    public bool IsSizeBoosted() => isSizeBoosted;
    public FoodType GetCurrentFoodZone() => currentFoodZone;
    public bool IsInHealingZone() => inHealingZone;
    public Slider GetEnergySlider() => energySlider;
    public bool IsRespawning() => isRespawning;
    public bool IsKnockbackActive() => isKnockbackActive;
    public bool IsDamageOnCooldown() => isDamageOnCooldown;
    public float GetStartDelay() => startDelay;
    public bool IsStartingGame() => isStartingGame;
    public float GetLowEnergyThreshold() => lowEnergyThreshold;
    public float GetGameTimer() => gameTimer;
    public float GetRemainingHearts() => currentLifeAmount;

    // NEW: Get time reduction amount
    public float GetTimeReduction() => timeReductionSeconds;

    // ====== RESUME FROM SAVED STATE ======

    /// <summary>
    /// Called by GameStateManager to restore the game from a previously saved state.
    /// Sets all internal fields to match the snapshot, enables the game canvas,
    /// starts the food spawner and UI, then un-pauses so gameplay continues exactly
    /// where the player left off.
    /// </summary>
    public void ResumeFromSavedState(GameStateSaveData saveData)
    {
        if (saveData == null)
        {
#if UNITY_EDITOR
            Debug.LogError("ResumeFromSavedState: saveData is null!");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log("=== RESUMING FROM SAVED STATE ===");
#endif

        // --- Stop any ongoing processes ---
        StopAllCoroutines();
        HideRespawnEffect(); // Clean up in case respawn effect was active
        StopKnockback();
        isRespawning = false;
        isStartingGame = false;

        // --- Apply power-up bonuses ---
        ApplyEquippedPetPowerUps();

        // --- Core state ---
        currentEnergy = saveData.currentEnergy;
        targetEnergy = saveData.targetEnergy;
        currentLifeAmount = saveData.currentLifeAmount;
        currentLives = saveData.currentLives;
        score = saveData.currentScore;
        gameTimer = saveData.gameTimer;
        currentFoodZone = saveData.currentFoodZone;
        isEnergyDecreasePaused = saveData.isEnergyDecreasePaused;
        isGameTimerPaused = saveData.isGameTimerPaused;

        // --- Boost state ---
        isSpeedBoosted = saveData.isSpeedBoosted;
        speedBoostTimer = saveData.speedBoostTimer;
        isSizeBoosted = saveData.isSizeBoosted;
        sizeBoostTimer = saveData.sizeBoostTimer;

        // --- Player speed / size ---
        if (playerController != null)
        {
            playerController.MoveSpeed = saveData.playerSpeed > 0 ? saveData.playerSpeed : initialPlayerSpeed;
            targetSpeed = playerController.MoveSpeed;
            playerController.enabled = true;
        }

        if (playerArmature != null)
        {
            float size = saveData.playerSize > 0 ? saveData.playerSize : initialPlayerSize;
            playerArmature.localScale = Vector3.one * size;
            targetSize = size;
        }

        // --- Activate game canvas FIRST so UI elements rebuild properly ---
        gameIsActive = true;

        if (gameCanvas != null) gameCanvas.gameObject.SetActive(true);

        // Hide start button
        if (startButton != null) startButton.gameObject.SetActive(false);

        // Disable lobby UI elements
        foreach (GameObject uiElement in uiElementsToDisable)
            if (uiElement != null) uiElement.SetActive(false);

        // Enable gameplay UI elements
        foreach (GameObject uiElement in uiElementsToEnable)
            if (uiElement != null) uiElement.SetActive(true);

        // --- Slider / UI (must be set AFTER canvas is active for proper layout rebuild) ---
        if (energySlider != null)
        {
            energySlider.maxValue = 100f;
            energySlider.minValue = 0f;
            energySlider.value = currentEnergy;
        }

        // Re-apply slider appearance for the SAVED zone (not initial)
        GetSliderAppearanceForZone(currentFoodZone, out Color resumeFillColor, out Sprite resumeHandleSprite);
        SetSliderAppearance(currentFoodZone, resumeFillColor, resumeHandleSprite);
        UpdateFeedbackSpriteForZone(currentFoodZone);

        // --- Heart UI ---
        InitializeHeartSystem(); // rebuilds heart images for maxLives
        currentLifeAmount = saveData.currentLifeAmount;
        currentLives = saveData.currentLives;
        UpdateHeartUI();

        // --- Boost visual indicators ---
        HideAllBoostUI();
        HideAllVisualEffects();

        if (isSpeedBoosted)
        {
            if (speedBoostEffect != null) speedBoostEffect.SetActive(true);
            ShowBoostUI(FoodType.Go);
        }
        if (isSizeBoosted)
        {
            if (sizeBoostEffect != null) sizeBoostEffect.SetActive(true);
            ShowBoostUI(FoodType.Grow);
        }

        // --- Low-energy canvas ---
        if (lowEnergyCanvas != null) lowEnergyCanvas.SetActive(false);
        wasLowEnergyLastFrame = false;

        // --- Timer display ---
        UpdateTimerDisplay();
        UpdateUI();

        // --- Food spawner ---
        if (foodSpawner != null)
        {
            foodSpawner.StopSpawning();
            foodSpawner.StartSpawning();
        }

        // --- BGM (looping) ---
        if (backgroundMusicSource != null && gameStartBGM != null)
        {
            backgroundMusicSource.clip = gameStartBGM;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }

        // --- Pause timer initially; InGameSettingsButton will resume after countdown ---
        isGameTimerPaused = true;

        // --- One-life check ---
        StartOneLifeCheck();

        // --- Reset animations to clean state ---
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(exciteTrigger, false);
            characterAnimator.SetBool(stomachAcheTrigger, false);
            characterAnimator.SetBool(strongTrigger, false);
            characterAnimator.SetBool(glowTrigger, false);
            characterAnimator.SetBool(damageTrigger, false);
            characterAnimator.SetBool(deathTrigger, false);
        }

#if UNITY_EDITOR
        Debug.Log($"Resume complete. Energy:{currentEnergy} Score:{score} Lives:{currentLifeAmount} " +
                  $"Timer:{gameTimer}s Zone:{currentFoodZone} SpeedBoosted:{isSpeedBoosted} SizeBoosted:{isSizeBoosted}");
#endif
    }

    // ====== ADDITIONAL SETTERS (for save/restore) ======

    /// <summary>Sets the life amount + count and refreshes the heart UI.</summary>
    public void SetLives(float lifeAmount)
    {
        currentLifeAmount = Mathf.Max(0f, lifeAmount);
        currentLives = Mathf.CeilToInt(currentLifeAmount);
        UpdateHeartUI();
    }

    /// <summary>Sets the game timer to an exact value.</summary>
    public void SetGameTimer(float time)
    {
        gameTimer = Mathf.Max(0f, time);
        UpdateTimerDisplay();
    }

    /// <summary>Sets the score to an exact value.</summary>
    public void SetScore(int newScore)
    {
        score = Mathf.Max(0, newScore);
        UpdateUI();
    }
}
