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
    private bool isEnergyDecreasePaused = false; // New flag for pausing energy decrease

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
    public float respawnEffectDuration = 1f;
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

    [Header("Audio Integration")]
    public AudioClip[] goFoodSounds;
    public AudioClip[] growFoodSounds;
    public AudioClip[] glowFoodSounds;
    public AudioClip[] junkFoodSounds;
    public AudioClip speedBoostSound;
    public AudioClip sizeBoostSound;
    public AudioClip glowBoostSound;
    public AudioClip loseLifeSound;
    public AudioClip collectionSound;
    public AudioClip respawnSound;
    public AudioClip knockbackSound;
    public AudioClip damageSound;
    public AudioClip deathSound;

    [Header("Boost Effects")]
    public GameObject speedBoostEffect;
    public GameObject sizeBoostEffect;
    public GameObject glowBoostEffect;

    [Header("Food Feedback UI")]
    public FoodFeedbackUI foodFeedbackUI;

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
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(false);
        if (energySlider != null)
        {
            energySlider.maxValue = 100f;
            energySlider.minValue = 0f;
            energySlider.value = currentEnergy;
        }

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

        targetEnergy = currentEnergy;
        targetSpeed = initialPlayerSpeed;
        targetSize = initialPlayerSize;

        if (startCheckpoint != null)
        {
            currentCheckpoint = startCheckpoint;
            startCheckpoint.Activate();
        }

        UpdateUI();
        SetGameActive(false);
    }

    private void Update()
    {
        if (!gameIsActive) return;

        gameTimer += Time.deltaTime;
        UpdateTimerDisplay();

        // Only decrease energy if not paused and not in healing zone/boosted states
        if (!isEnergyDecreasePaused && !inHealingZone && !isSpeedBoosted && !isSizeBoosted)
        {
            targetEnergy -= energyDecreaseRate * Time.deltaTime;
            targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        }

        if (Mathf.Abs(currentEnergy - targetEnergy) > 0.01f)
        {
            currentEnergy = Mathf.Lerp(currentEnergy, targetEnergy, energyTransitionSpeed * Time.deltaTime);
            if (energySlider != null) energySlider.value = currentEnergy;
        }
        else currentEnergy = targetEnergy;

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

    // ====== ENERGY SLIDER PAUSE/RESUME LOGIC ======
    public void PauseEnergyDecrease()
    {
        isEnergyDecreasePaused = true;
        Debug.Log("Energy decrease paused");
    }

    public void ResumeEnergyDecrease()
    {
        isEnergyDecreasePaused = false;
        Debug.Log("Energy decrease resumed");
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
            yield return new WaitForSeconds(oneLifeCheckInterval);
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
        yield return new WaitForSeconds(duration);
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
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(true);
        gameIsActive = true;
        isEnergyDecreasePaused = false; // Reset pause state
        currentLives = maxLives;
        currentLifeAmount = maxLives;
        currentEnergy = 0f;
        targetEnergy = 0f;
        score = 0;
        gameTimer = 0f;
        currentFoodZone = FoodType.Go;

        Checkpoint[] allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        foreach (Checkpoint checkpoint in allCheckpoints) checkpoint.ResetCheckpoint();

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

        if (startButton != null) startButton.gameObject.SetActive(false);
        if (foodSpawner != null) foodSpawner.StartSpawning();
        else Debug.LogError("FoodSpawner not assigned to GameManager!");

        UpdateUI();
        RespawnPlayer();
        Debug.Log("Game Started!");
    }

    public void EndGame()
    {
        gameIsActive = false;
        isEnergyDecreasePaused = false; // Reset pause state
        StopOneLifeCheck();
        StopKnockback();

        if (gameCanvas != null) gameCanvas.gameObject.SetActive(false);
        if (foodSpawner != null) foodSpawner.StopSpawning();

        StopAllCoroutines();
        if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
        if (respawnEffectCoroutine != null) StopCoroutine(respawnEffectCoroutine);

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

        if (startButton != null) startButton.gameObject.SetActive(true);
        Debug.Log("Game Ended!");
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

    public void SetCurrentFoodZone(FoodType zoneType, Color fillColor, Sprite handleSprite)
    {
        if (!gameIsActive) return;

        currentFoodZone = zoneType;
        if (sliderFillImage != null) sliderFillImage.color = fillColor;
        if (sliderHandleImage != null && handleSprite != null) sliderHandleImage.sprite = handleSprite;

        if (zoneType == FoodType.Grow && playerArmature != null) UpdatePlayerSize();
        else if (zoneType == FoodType.Go && playerArmature != null) targetSize = initialPlayerSize;

        Debug.Log($"Switched to {zoneType} zone");
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

        if (currentFoodZone == FoodType.Grow)
        {
            if (targetEnergy >= 100f && !isSizeBoosted) StartSizeBoost();
            else if (isSizeBoosted) sizeBoostTimer += 2f;
            else PlayGrowFoodSound();
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

        if (AudioHandler.Instance != null && speedBoostSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(speedBoostSound);

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

        if (AudioHandler.Instance != null && sizeBoostSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(sizeBoostSound);

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
        yield return new WaitForSeconds(deathAnimationDuration);
        ResetDeathAnimation();
        yield return new WaitForSeconds(respawnDelay);

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
        Debug.Log("Respawn complete!");
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

    private void ShowRespawnEffect()
    {
        if (respawnEffect != null)
        {
            respawnEffect.transform.position = playerTransform.position;
            respawnEffect.SetActive(true);

            if (respawnEffectCoroutine != null) StopCoroutine(respawnEffectCoroutine);
            respawnEffectCoroutine = StartCoroutine(HideRespawnEffectAfterDelay());
        }
    }

    private IEnumerator HideRespawnEffectAfterDelay()
    {
        yield return new WaitForSeconds(respawnEffectDuration);
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
        yield return new WaitForSeconds(1f);
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
        yield return new WaitForSeconds(duration);
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
        }
    }

    private IEnumerator HideFeedbackSprite()
    {
        yield return new WaitForSeconds(spriteDisplayTime);
        if (feedbackSpriteObject != null) feedbackSpriteObject.SetActive(false);
        spriteDisplayCoroutine = null;
    }

    // ====== AUDIO ======
    private void PlayGoFoodSound()
    {
        if (AudioHandler.Instance != null && goFoodSounds.Length > 0)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(goFoodSounds[Random.Range(0, goFoodSounds.Length)]);
    }

    private void PlayGrowFoodSound()
    {
        if (AudioHandler.Instance != null && growFoodSounds.Length > 0)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(growFoodSounds[Random.Range(0, growFoodSounds.Length)]);
    }

    private void PlayGlowFoodSound()
    {
        if (AudioHandler.Instance != null && glowFoodSounds.Length > 0)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(glowFoodSounds[Random.Range(0, glowFoodSounds.Length)]);
    }

    private void PlayJunkFoodSound()
    {
        if (AudioHandler.Instance != null && junkFoodSounds.Length > 0)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(junkFoodSounds[Random.Range(0, junkFoodSounds.Length)]);
    }

    private void PlayCollectionSound()
    {
        if (AudioHandler.Instance != null && collectionSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(collectionSound);
    }

    // ====== UI ======
    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
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
            int minutes = Mathf.FloorToInt(gameTimer / 60f);
            int seconds = Mathf.FloorToInt(gameTimer % 60f);
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
        Debug.Log($"Checkpoint set to: {checkpoint.gameObject.name}");
    }

    private void RespawnPlayer()
    {
        if (playerTransform == null || currentCheckpoint == null) return;
        playerTransform.position = currentCheckpoint.GetSpawnPosition();
        playerTransform.rotation = currentCheckpoint.GetSpawnRotation();
        Debug.Log($"Respawned at: {currentCheckpoint.gameObject.name}");
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
        score = Mathf.Max(0, score); // Ensure score doesn't go negative
        UpdateUI();
    }

    public void AddEnergy(float amount)
    {
        if (!gameIsActive) return;

        targetEnergy += amount;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        Debug.Log($"Energy added: {amount}. New target: {targetEnergy}");
    }

    public void RemoveEnergy(float amount)
    {
        if (!gameIsActive) return;

        targetEnergy -= amount;
        targetEnergy = Mathf.Clamp(targetEnergy, 0f, 100f);
        Debug.Log($"Energy removed: {amount}. New target: {targetEnergy}");
    }

    public void SetEnergy(float amount)
    {
        if (!gameIsActive) return;

        targetEnergy = Mathf.Clamp(amount, 0f, 100f);
        currentEnergy = targetEnergy;

        if (energySlider != null)
            energySlider.value = currentEnergy;

        Debug.Log($"Energy set to: {targetEnergy}");
    }

    // ====== BOOST METHODS ======
    public void TriggerSpeedBoost(float duration = 10f)
    {
        if (!gameIsActive || isRespawning) return;

        isSpeedBoosted = true;
        speedBoostTimer = duration;

        if (AudioHandler.Instance != null && speedBoostSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(speedBoostSound);

        if (speedBoostEffect != null) speedBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Go);

        Debug.Log($"Speed boost activated for {duration} seconds");
    }

    public void TriggerSizeBoost(float duration = 10f)
    {
        if (!gameIsActive || isRespawning) return;

        isSizeBoosted = true;
        sizeBoostTimer = duration;

        if (AudioHandler.Instance != null && sizeBoostSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(sizeBoostSound);

        if (sizeBoostEffect != null) sizeBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Grow);

        Debug.Log($"Size boost activated for {duration} seconds");
    }

    public void TriggerGlowBoost(float duration = 10f)
    {
        if (!gameIsActive || isRespawning) return;

        if (AudioHandler.Instance != null && glowBoostSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(glowBoostSound);

        if (glowBoostEffect != null) glowBoostEffect.SetActive(true);
        ShowBoostUI(FoodType.Glow);

        Debug.Log($"Glow boost activated for {duration} seconds");
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
}