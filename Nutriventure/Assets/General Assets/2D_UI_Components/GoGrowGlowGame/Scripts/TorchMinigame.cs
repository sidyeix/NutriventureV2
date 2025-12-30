using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

[System.Serializable]
public class FoodOption
{
    public Sprite foodSprite;
    public GameObject foodPrefab;
    public bool isGoFood;
    public string foodName;
}

public class TorchMinigame : MonoBehaviour
{
    [Header("Torch Components")]
    [SerializeField] private GameObject torchCanvas;
    [SerializeField] private Button litButton;
    [SerializeField] private CinemachineVirtualCamera torchVirtualCamera;
    [SerializeField] private int activeCameraPriority = 30;
    [SerializeField] private int inactiveCameraPriority = 10;
    [SerializeField] private GameObject fireObject;
    [SerializeField] private GameObject wrongFlameObject;
    [SerializeField] private Transform flameFoodSpawn;

    [Header("Button System")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonPanel;
    [SerializeField] private List<GameObject> uiElementsToDisable = new List<GameObject>();

    [Header("Game Settings")]
    [SerializeField] private int numberOfChoices = 3;
    [SerializeField] private int requiredCorrectAnswers = 2;
    [SerializeField] private int currentCorrectAnswers = 0;
    [SerializeField] private bool isInTorchMode = false;

    [Header("Energy Settings")]
    [SerializeField] private float energyLossOnWrongAnswer = 30f;
    [SerializeField] private float energyGainOnCorrectAnswer = 10f;
    [SerializeField] private float energyBonusOnComplete = 100f;
    private float startingEnergy; // Store energy at start of minigame

    [Header("Point Settings")]
    [SerializeField] private int pointsGainOnCorrectAnswer = 10;
    [SerializeField] private int pointsLossOnWrongAnswer = 5;

    [Header("Torch Status")]
    [SerializeField] private bool isLit = false;
    [SerializeField] private string torchID = "torch_1"; // Unique ID for each torch

    [Header("Coin Explosion")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int numberOfCoins = 10;
    [SerializeField] private float coinExplosionForce = 5f;
    [SerializeField] private float coinUpwardForce = 8f;
    [SerializeField] private float coinExplosionRadius = 2f;
    [Header("Coin Setup Override")]
    [SerializeField] private bool setupCoinComponents = true;
    [SerializeField] private bool coinIsTrigger = true;
    [SerializeField] private float coinMass = 1f;
    [SerializeField] private float coinDrag = 0.5f;
    [SerializeField] private float coinAngularDrag = 0.05f;
    [SerializeField] private string coinLayer = "Default";

    [Header("Food Options")]
    [SerializeField] private List<FoodOption> goFoodOptions = new List<FoodOption>();
    [SerializeField] private List<FoodOption> otherFoodOptions = new List<FoodOption>();

    [Header("Animation Settings")]
    [SerializeField] private float buttonSlideDuration = 0.5f;
    [SerializeField] private float flameScaleDuration = 1f;
    [SerializeField] private float foodAnimationDuration = 1f;
    [SerializeField] private float wrongFlameDuration = 2f;
    [SerializeField] private float delayBeforeFlameScale = 0.5f;

    [Header("Button Feedback")]
    [SerializeField] private float buttonShakeDuration = 0.5f;
    [SerializeField] private float buttonShakeMagnitude = 10f;
    [SerializeField] private Color wrongButtonColor = Color.red;
    [SerializeField] private float buttonColorFlashDuration = 0.3f;

    [Header("Camera Shake")]
    [SerializeField] private float cameraShakeDuration = 0.5f;
    [SerializeField] private float cameraShakeIntensity = 0.5f;
    [SerializeField] private float cameraShakeFrequency = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip flameScaleUpSound;
    [SerializeField] private AudioClip flameScaleDownSound;
    [SerializeField] private AudioClip shrivelingSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private AudioClip startButtonSound;
    [SerializeField] private AudioClip coinExplosionSound;

    [Header("Damage Panel")]
    [SerializeField] private GameObject damagePanel;
    [SerializeField] private float damagePanelDuration = 1f;

    private List<GameObject> currentButtons = new List<GameObject>();
    private List<FoodOption> currentRoundFoods = new List<FoodOption>();
    private int goFoodIndex;
    private float currentFlameScale = 0f;
    private float scaleStep;
    private Vector3 buttonPanelHiddenPosition;
    private Vector3 buttonPanelVisiblePosition;
    private Coroutine scaleCoroutine;
    private Coroutine wrongFlameCoroutine;
    private Coroutine damagePanelCoroutine;
    private Coroutine buttonFeedbackCoroutine;
    private Coroutine cameraShakeCoroutine;

    private GoGrowGlowGameManager gameManager;
    private TorchMinigameManager torchManager;
    private AudioSource audioSource;
    private BoxCollider triggerCollider;

    private void Awake()
    {
        Debug.Log($"=== TORCH MINIGAME AWAKE [{torchID}] ===");

        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Get box collider
        triggerCollider = GetComponent<BoxCollider>();
        Debug.Log("BoxCollider found: " + (triggerCollider != null));

        if (triggerCollider == null)
        {
            Debug.LogError("NO BOXCOLLIDER FOUND ON TORCH MINIGAME!");
        }
        else
        {
            Debug.Log("BoxCollider isTrigger: " + triggerCollider.isTrigger);
            Debug.Log("BoxCollider enabled: " + triggerCollider.enabled);
            Debug.Log("BoxCollider size: " + triggerCollider.size);
        }
    }

    private void Start()
    {
        Debug.Log($"=== TORCH MINIGAME START [{torchID}] ===");

        // FORCE reset state
        isInTorchMode = false;
        isLit = false; // Reset lit status
        Debug.Log($"isInTorchMode initialized to: {isInTorchMode}");
        Debug.Log($"isLit initialized to: {isLit}");

        gameManager = GoGrowGlowGameManager.Instance;
        torchManager = TorchMinigameManager.Instance;
        Debug.Log("GameManager found: " + (gameManager != null));
        Debug.Log("TorchManager found: " + (torchManager != null));

        // Check if canvas and button are assigned
        Debug.Log("Torch Canvas assigned: " + (torchCanvas != null));
        Debug.Log("Lit Button assigned: " + (litButton != null));

        if (torchCanvas == null)
            Debug.LogError("TORCH CANVAS IS NULL - CHECK INSPECTOR!");
        if (litButton == null)
            Debug.LogError("LIT BUTTON IS NULL - CHECK INSPECTOR!");

        // Initialize button panel positions
        if (buttonPanel != null)
        {
            buttonPanelHiddenPosition = buttonPanel.localPosition - new Vector3(0, 300, 0);
            buttonPanelVisiblePosition = buttonPanel.localPosition;
            buttonPanel.localPosition = buttonPanelHiddenPosition;
        }

        // Initialize flame
        if (fireObject != null)
            fireObject.transform.localScale = Vector3.zero;

        if (wrongFlameObject != null)
            wrongFlameObject.SetActive(false);

        if (damagePanel != null)
            damagePanel.SetActive(false);

        // Hide UI initially
        if (torchCanvas != null)
        {
            torchCanvas.SetActive(false);
            Debug.Log("Canvas hidden at start");
        }

        if (flameFoodSpawn != null)
            flameFoodSpawn.gameObject.SetActive(false);

        if (litButton != null)
        {
            litButton.gameObject.SetActive(false);
            Debug.Log("Button hidden at start");
        }

        // Calculate scale step
        scaleStep = 1f / requiredCorrectAnswers;

        // Validate food options
        ValidateFoodOptions();

        // Register with torch manager
        if (torchManager != null)
        {
            torchManager.RegisterTorch(this);
        }

        Debug.Log($"=== TORCH MINIGAME READY [{torchID}] ===");
    }

    private void ValidateFoodOptions()
    {
        goFoodOptions.RemoveAll(food => food.foodPrefab == null || food.foodSprite == null);
        otherFoodOptions.RemoveAll(food => food.foodPrefab == null || food.foodSprite == null);

        if (goFoodOptions.Count == 0)
            Debug.LogWarning("No Go Food options assigned");
        if (otherFoodOptions.Count == 0)
            Debug.LogWarning("No Other Food options assigned");
    }

    // CORRECTED TRIGGER LOGIC - Only show UI if torch is NOT lit
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"=== ON TRIGGER ENTER CALLED [{torchID}] ===");
        Debug.Log("Collider tag: " + other.tag);
        Debug.Log("Is Player? " + other.CompareTag("Player"));
        Debug.Log("Current isInTorchMode: " + isInTorchMode);
        Debug.Log("Is torch already lit? " + isLit);

        // Check if this is the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered trigger!");

            // Only show UI if torch is NOT lit and not in minigame mode
            if (!isLit && !isInTorchMode)
            {
                ShowTorchUI();
            }
            else if (isLit)
            {
                Debug.Log("Torch already lit - not showing UI");
            }
            else
            {
                Debug.Log("Minigame already active, not showing UI");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"=== ON TRIGGER EXIT CALLED [{torchID}] ===");
        Debug.Log("Collider tag: " + other.tag);
        Debug.Log("Is Player? " + other.CompareTag("Player"));

        // Check if this is the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited trigger!");

            // Only hide UI if not in torch mode
            if (!isInTorchMode)
            {
                HideTorchUI();
            }
        }
    }

    // IMPROVED ShowTorchUI with better debugging
    public void ShowTorchUI()
    {
        Debug.Log($"=== SHOW TORCH UI [{torchID}] ===");
        Debug.Log("isInTorchMode: " + isInTorchMode);
        Debug.Log("isLit: " + isLit);
        Debug.Log("Torch Canvas: " + (torchCanvas != null ? torchCanvas.name : "NULL"));
        Debug.Log("Lit Button: " + (litButton != null ? litButton.name : "NULL"));

        if (torchCanvas == null)
        {
            Debug.LogError("CANNOT SHOW UI - TORCH CANVAS IS NULL!");

            // Try to find it
            torchCanvas = GameObject.Find("TorchCanvas");
            if (torchCanvas == null)
            {
                Debug.LogError("STILL CAN'T FIND TORCH CANVAS!");
                return;
            }
        }

        if (litButton == null)
        {
            Debug.LogError("CANNOT SHOW UI - LIT BUTTON IS NULL!");

            // Try to find it
            GameObject buttonObj = GameObject.Find("LitButton");
            if (buttonObj != null)
            {
                litButton = buttonObj.GetComponent<Button>();
            }

            if (litButton == null)
            {
                Debug.LogError("STILL CAN'T FIND LIT BUTTON!");
                return;
            }
        }

        // Show canvas
        torchCanvas.SetActive(true);
        Debug.Log("Canvas set to active: " + torchCanvas.activeSelf);
        Debug.Log("Canvas activeInHierarchy: " + torchCanvas.activeInHierarchy);

        // Show button
        litButton.gameObject.SetActive(true);
        Debug.Log("Button set to active: " + litButton.gameObject.activeSelf);
        Debug.Log("Button activeInHierarchy: " + litButton.gameObject.activeInHierarchy);

        Debug.Log("=== UI SHOULD NOW BE VISIBLE ===");
    }

    public void HideTorchUI()
    {
        Debug.Log($"=== HIDE TORCH UI [{torchID}] ===");

        if (torchCanvas != null)
        {
            torchCanvas.SetActive(false);
            Debug.Log("Canvas hidden");
        }

        if (litButton != null)
        {
            litButton.gameObject.SetActive(false);
            Debug.Log("Button hidden");
        }
    }

    public void StartTorchMinigame()
    {
        Debug.Log($"StartTorchMinigame called [{torchID}]. isInTorchMode: " + isInTorchMode);

        if (isInTorchMode || isLit) return; // Don't start if already lit

        // NO LONGER storing starting energy - we won't reset it
        // NO LONGER pausing energy decrease

        // Play sound
        if (startButtonSound != null && audioSource != null)
            audioSource.PlayOneShot(startButtonSound);

        isInTorchMode = true;
        currentCorrectAnswers = 0;
        currentFlameScale = 0f;

        // Switch camera
        if (torchVirtualCamera != null)
            torchVirtualCamera.Priority = activeCameraPriority;

        // Hide UI elements
        foreach (GameObject uiElement in uiElementsToDisable)
            if (uiElement != null) uiElement.SetActive(false);

        // Hide lit button
        if (litButton != null)
            litButton.gameObject.SetActive(false);

        // Start minigame
        InitializeButtons();
        StartCoroutine(SlideButtonPanel(true));
    }

    private void InitializeButtons()
    {
        // Clear old buttons
        foreach (GameObject button in currentButtons)
            Destroy(button);
        currentButtons.Clear();
        currentRoundFoods.Clear();

        // Randomly choose which button is correct
        goFoodIndex = Random.Range(0, numberOfChoices);

        List<FoodOption> availableGoFoods = new List<FoodOption>(goFoodOptions);
        List<FoodOption> availableOtherFoods = new List<FoodOption>(otherFoodOptions);

        // Create buttons
        for (int i = 0; i < numberOfChoices; i++)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonPanel);
            Button button = buttonObj.GetComponent<Button>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            FoodOption foodOption;

            if (i == goFoodIndex) // Correct answer
            {
                if (availableGoFoods.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableGoFoods.Count);
                    foodOption = availableGoFoods[randomIndex];
                    availableGoFoods.RemoveAt(randomIndex);
                }
                else
                {
                    Debug.LogError("No Go Food options!");
                    return;
                }
            }
            else // Wrong answer
            {
                if (availableOtherFoods.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableOtherFoods.Count);
                    foodOption = availableOtherFoods[randomIndex];
                    availableOtherFoods.RemoveAt(randomIndex);
                }
                else if (otherFoodOptions.Count > 0)
                {
                    int randomIndex = Random.Range(0, otherFoodOptions.Count);
                    foodOption = otherFoodOptions[randomIndex];
                }
                else
                {
                    Debug.LogError("No Other Food options!");
                    return;
                }
            }

            // Set button image
            if (buttonImage != null && foodOption.foodSprite != null)
                buttonImage.sprite = foodOption.foodSprite;

            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && !string.IsNullOrEmpty(foodOption.foodName))
                buttonText.text = foodOption.foodName;

            currentRoundFoods.Add(foodOption);

            // Add click listener
            int index = i;
            button.onClick.AddListener(() => OnFoodSelected(index));

            currentButtons.Add(buttonObj);
        }
    }

    private void OnFoodSelected(int buttonIndex)
    {
        if (!isInTorchMode) return;

        // Disable all buttons
        foreach (GameObject button in currentButtons)
            if (button != null) button.GetComponent<Button>().interactable = false;

        if (buttonIndex == goFoodIndex)
        {
            StartCoroutine(HandleCorrectAnswerSequence(buttonIndex));
        }
        else
        {
            StartCoroutine(HandleWrongAnswerSequence(buttonIndex));
        }
    }

    private IEnumerator HandleCorrectAnswerSequence(int foodIndex)
    {
        // Play food animation
        yield return StartCoroutine(PlayFoodAnimation(foodIndex, false));

        // Wait then scale flame
        yield return new WaitForSeconds(delayBeforeFlameScale);
        HandleCorrectAnswerResult();

        // Re-enable buttons
        ReEnableButtons();
    }

    private IEnumerator HandleWrongAnswerSequence(int foodIndex)
    {
        // Button feedback
        GameObject selectedButton = currentButtons[foodIndex];
        yield return StartCoroutine(ButtonFeedbackSequence(selectedButton));

        // Camera shake
        yield return StartCoroutine(ShakeCamera());

        // Play sound
        if (shrivelingSound != null && audioSource != null)
            audioSource.PlayOneShot(shrivelingSound);

        // Show wrong flame and food animation together
        yield return StartCoroutine(PlayFoodAnimationAndWrongFlame(foodIndex));

        // Handle result - THIS IS NOW A COROUTINE TOO
        yield return StartCoroutine(HandleWrongAnswerResult());

        // Re-enable buttons
        ReEnableButtons();
    }

    private IEnumerator PlayFoodAnimationAndWrongFlame(int foodIndex)
    {
        if (flameFoodSpawn == null || currentRoundFoods.Count <= foodIndex)
            yield break;

        // Show wrong flame
        if (wrongFlameObject != null)
            wrongFlameObject.SetActive(true);

        // Spawn food
        FoodOption selectedFood = currentRoundFoods[foodIndex];
        if (selectedFood.foodPrefab != null)
        {
            GameObject spawnedFood = Instantiate(selectedFood.foodPrefab, flameFoodSpawn);
            spawnedFood.transform.localPosition = Vector3.zero;
            flameFoodSpawn.gameObject.SetActive(true);

            // Play animation
            Animator foodAnimator = spawnedFood.GetComponent<Animator>();
            if (foodAnimator != null)
                foodAnimator.SetTrigger("Play");

            // Wait
            yield return new WaitForSeconds(wrongFlameDuration);

            // Clean up
            flameFoodSpawn.gameObject.SetActive(false);
            Destroy(spawnedFood);
        }
        else
        {
            yield return new WaitForSeconds(wrongFlameDuration);
        }

        // Hide wrong flame
        if (wrongFlameObject != null)
            wrongFlameObject.SetActive(false);
    }

    private IEnumerator ButtonFeedbackSequence(GameObject button)
    {
        if (button == null) yield break;

        // Flash red
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = buttonImage.color;
            buttonImage.color = wrongButtonColor;
            yield return new WaitForSeconds(buttonColorFlashDuration);
            buttonImage.color = originalColor;
        }

        // Shake
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            Vector3 originalPosition = buttonRect.localPosition;
            float elapsedTime = 0f;

            while (elapsedTime < buttonShakeDuration)
            {
                elapsedTime += Time.deltaTime;
                float x = Random.Range(-1f, 1f) * buttonShakeMagnitude;
                float y = Random.Range(-1f, 1f) * buttonShakeMagnitude;
                buttonRect.localPosition = originalPosition + new Vector3(x, y, 0);
                yield return null;
            }

            buttonRect.localPosition = originalPosition;
        }
    }

    private IEnumerator ShakeCamera()
    {
        // Find main camera
        CinemachineVirtualCamera mainCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (mainCamera == null) yield break;

        CinemachineBasicMultiChannelPerlin noise = mainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) yield break;

        float originalAmplitude = noise.m_AmplitudeGain;
        float originalFrequency = noise.m_FrequencyGain;

        float elapsedTime = 0f;
        while (elapsedTime < cameraShakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / cameraShakeDuration;
            float intensity = cameraShakeIntensity * (1f - progress);
            noise.m_AmplitudeGain = intensity;
            noise.m_FrequencyGain = cameraShakeFrequency;
            yield return null;
        }

        // Reset
        noise.m_AmplitudeGain = originalAmplitude;
        noise.m_FrequencyGain = originalFrequency;
    }

    private IEnumerator PlayFoodAnimation(int foodIndex, bool isWrongAnswer = false)
    {
        if (flameFoodSpawn == null || currentRoundFoods.Count <= foodIndex)
            yield break;

        FoodOption selectedFood = currentRoundFoods[foodIndex];
        if (selectedFood.foodPrefab != null)
        {
            GameObject spawnedFood = Instantiate(selectedFood.foodPrefab, flameFoodSpawn);
            spawnedFood.transform.localPosition = Vector3.zero;
            flameFoodSpawn.gameObject.SetActive(true);

            Animator foodAnimator = spawnedFood.GetComponent<Animator>();
            if (foodAnimator != null)
                foodAnimator.SetTrigger("Play");

            float duration = isWrongAnswer ? wrongFlameDuration : foodAnimationDuration;
            yield return new WaitForSeconds(duration);

            flameFoodSpawn.gameObject.SetActive(false);
            Destroy(spawnedFood);
        }
        else
        {
            yield return new WaitForSeconds(foodAnimationDuration);
        }
    }

    private void HandleCorrectAnswerResult()
    {
        // Play sound
        if (correctSound != null && audioSource != null)
            audioSource.PlayOneShot(correctSound);

        // Scale flame up
        float targetScale = currentFlameScale + scaleStep;
        StartCoroutine(ScaleFlame(targetScale, true));

        // Add energy
        if (gameManager != null)
        {
            gameManager.AddEnergy(energyGainOnCorrectAnswer);
        }

        // Add points
        if (gameManager != null && pointsGainOnCorrectAnswer > 0)
        {
            gameManager.AddPoints(pointsGainOnCorrectAnswer);
            Debug.Log($"Added {pointsGainOnCorrectAnswer} points for correct answer");
        }

        currentCorrectAnswers++;

        if (currentCorrectAnswers >= requiredCorrectAnswers)
        {
            CompleteMinigame();
        }
        else
        {
            StartCoroutine(ResetButtonsForNextRound());
        }
    }

    private IEnumerator HandleWrongAnswerResult()
    {
        // Play sound
        if (wrongSound != null && audioSource != null)
            audioSource.PlayOneShot(wrongSound);

        // Scale flame down
        float targetScale = Mathf.Max(0f, currentFlameScale - scaleStep);
        yield return StartCoroutine(ScaleFlame(targetScale, false));

        // Deduct energy and points
        if (gameManager != null)
        {
            // Deduct points immediately
            if (pointsLossOnWrongAnswer > 0)
            {
                gameManager.AddPoints(-pointsLossOnWrongAnswer);
                Debug.Log($"Deducted {pointsLossOnWrongAnswer} points for wrong answer");
            }

            // Deduct energy
            gameManager.RemoveEnergy(energyLossOnWrongAnswer);

            // Check if energy reached 0
            float currentEnergy = gameManager.GetCurrentEnergy();
            if (currentEnergy <= 0)
            {
                Debug.Log("Energy depleted to 0! Ending minigame with life loss");

                // Show damage panel immediately
                if (damagePanel != null)
                {
                    damagePanel.SetActive(true);
                    Debug.Log("Damage panel activated");
                }

                // Wait so player sees the damage panel
                yield return new WaitForSeconds(0.5f);

                // End minigame
                EndTorchMinigame(false);
                yield break;
            }
        }

        if (targetScale <= 0f && currentFlameScale <= 0f)
        {
            // If flame goes out completely, end the minigame
            EndTorchMinigame(false);
        }
        else
        {
            yield return StartCoroutine(ResetButtonsForNextRound());
        }
    }

    private IEnumerator HideDamagePanelAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (damagePanel != null)
            damagePanel.SetActive(false);
    }

    private IEnumerator ShowDamagePanel()
    {
        if (damagePanel != null)
        {
            damagePanel.SetActive(true);
            yield return new WaitForSeconds(damagePanelDuration);
            damagePanel.SetActive(false);
        }
    }

    private IEnumerator ScaleFlame(float targetScale, bool isScalingUp)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleFlameCoroutine(targetScale, isScalingUp));
        yield return scaleCoroutine;
    }

    private IEnumerator ScaleFlameCoroutine(float targetScale, bool isScalingUp)
    {
        if (fireObject == null) yield break;

        // Play sound
        if (isScalingUp && flameScaleUpSound != null && audioSource != null)
            audioSource.PlayOneShot(flameScaleUpSound);
        else if (!isScalingUp && flameScaleDownSound != null && audioSource != null)
            audioSource.PlayOneShot(flameScaleDownSound);

        Vector3 startScale = fireObject.transform.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsedTime = 0f;

        while (elapsedTime < flameScaleDuration)
        {
            float t = elapsedTime / flameScaleDuration;
            fireObject.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fireObject.transform.localScale = endScale;
        currentFlameScale = targetScale;
        scaleCoroutine = null;
    }

    private void ReEnableButtons()
    {
        foreach (GameObject button in currentButtons)
            if (button != null) button.GetComponent<Button>().interactable = true;
    }

    private void CompleteMinigame()
    {
        if (completeSound != null && audioSource != null)
            audioSource.PlayOneShot(completeSound);

        if (gameManager != null && gameManager.characterAnimator != null)
            gameManager.characterAnimator.SetBool("isCorrect", true);

        StartCoroutine(CompleteMinigameSequence());
    }

    private IEnumerator CompleteMinigameSequence()
    {
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(SlideButtonPanel(false));
        yield return new WaitForSeconds(0.5f);

        // Give energy bonus on completion
        if (gameManager != null)
        {
            gameManager.AddEnergy(energyBonusOnComplete);
            // NO LONGER resuming energy decrease (it was never paused)

            // Trigger boost on completion
            gameManager.TriggerSpeedBoost(10f); // 10 second speed boost

            // Spawn coin explosion
            SpawnCoinExplosion();
        }

        // Mark torch as lit
        isLit = true;

        // Notify torch manager
        if (torchManager != null)
        {
            torchManager.TorchLit(this);
        }

        EndTorchMinigame(true);
    }

    private void SetupCoinComponents(GameObject coin)
    {
        if (!setupCoinComponents) return;

        // Add Rigidbody if missing
        Rigidbody rb = coin.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = coin.AddComponent<Rigidbody>();
        }

        rb.mass = coinMass;
        rb.linearDamping = coinDrag;
        rb.angularDamping = coinAngularDrag;
        rb.useGravity = true;
        rb.isKinematic = false;

        // Setup BoxCollider
        BoxCollider collider = coin.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = coinIsTrigger;
            // Don't change size since it's already set in your prefab
        }
        else
        {
            // Add if missing
            collider = coin.AddComponent<BoxCollider>();
            collider.isTrigger = coinIsTrigger;
            collider.size = Vector3.one * 0.5f; // Default coin size
        }

        // Set layer
        if (!string.IsNullOrEmpty(coinLayer))
        {
            int layer = LayerMask.NameToLayer(coinLayer);
            if (layer != -1)
                coin.layer = layer;
        }

        // Add tag
        coin.tag = "Coin";

        Debug.Log("Coin components setup complete: " + coin.name);
    }

    private void SpawnCoinExplosion()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("Coin prefab not assigned!");
            return;
        }

        // Play coin explosion sound
        if (coinExplosionSound != null && audioSource != null)
            audioSource.PlayOneShot(coinExplosionSound);

        Vector3 spawnPosition = transform.position;

        for (int i = 0; i < numberOfCoins; i++)
        {
            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            // Setup coin components (override)
            SetupCoinComponents(coin);

            // Add Rigidbody if missing (should be added by SetupCoinComponents, but just in case)
            Rigidbody rb = coin.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = coin.AddComponent<Rigidbody>();
                rb.useGravity = true;
            }

            // Random direction for explosion
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f), // More upward
                Random.Range(-1f, 1f)
            ).normalized;

            // Apply explosion force
            float force = Random.Range(coinExplosionForce * 0.7f, coinExplosionForce * 1.3f);
            rb.AddForce(randomDirection * force, ForceMode.Impulse);

            // Add some upward force
            rb.AddForce(Vector3.up * coinUpwardForce, ForceMode.Impulse);

            // Add random rotation
            Vector3 randomTorque = new Vector3(
                Random.Range(-100f, 100f),
                Random.Range(-100f, 100f),
                Random.Range(-100f, 100f)
            );
            rb.AddTorque(randomTorque);

            // Auto-destroy after some time (optional)
            Destroy(coin, 10f);
        }

        Debug.Log($"Spawned {numberOfCoins} coins in explosion!");
    }

    private IEnumerator ResetButtonsForNextRound()
    {
        yield return StartCoroutine(SlideButtonPanel(false));
        InitializeButtons();
        yield return StartCoroutine(SlideButtonPanel(true));
    }

    private IEnumerator SlideButtonPanel(bool slideIn)
    {
        if (buttonPanel == null) yield break;

        Vector3 startPos = buttonPanel.localPosition;
        Vector3 targetPos = slideIn ? buttonPanelVisiblePosition : buttonPanelHiddenPosition;
        float elapsedTime = 0f;

        while (elapsedTime < buttonSlideDuration)
        {
            float t = elapsedTime / buttonSlideDuration;
            t = slideIn ? Mathf.Sin(t * Mathf.PI * 0.5f) : 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            buttonPanel.localPosition = Vector3.Lerp(startPos, targetPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        buttonPanel.localPosition = targetPos;
    }

    private void EndTorchMinigame(bool success)
    {
        isInTorchMode = false;

        // Stop coroutines
        if (wrongFlameCoroutine != null) StopCoroutine(wrongFlameCoroutine);
        if (damagePanelCoroutine != null) StopCoroutine(damagePanelCoroutine);
        if (buttonFeedbackCoroutine != null) StopCoroutine(buttonFeedbackCoroutine);
        if (cameraShakeCoroutine != null) StopCoroutine(cameraShakeCoroutine);

        // Reset objects
        if (wrongFlameObject != null) wrongFlameObject.SetActive(false);
        if (damagePanel != null) damagePanel.SetActive(false);

        // Show UI elements
        foreach (GameObject uiElement in uiElementsToDisable)
            if (uiElement != null) uiElement.SetActive(true);

        // Reset camera
        if (torchVirtualCamera != null)
            torchVirtualCamera.Priority = inactiveCameraPriority;

        // Hide canvas
        if (torchCanvas != null)
            torchCanvas.SetActive(false);

        if (flameFoodSpawn != null)
            flameFoodSpawn.gameObject.SetActive(false);

        // Clear buttons
        foreach (GameObject button in currentButtons)
            Destroy(button);
        currentButtons.Clear();
        currentRoundFoods.Clear();

        if (buttonPanel != null)
            buttonPanel.localPosition = buttonPanelHiddenPosition;

        // Check if player is still in trigger
        if (triggerCollider != null)
        {
            Collider[] colliders = Physics.OverlapBox(transform.position, triggerCollider.size / 2, transform.rotation);
            bool playerInTrigger = false;

            foreach (Collider col in colliders)
                if (col.CompareTag("Player")) playerInTrigger = true;

            // Only show UI if torch is NOT lit and player is still there
            if (playerInTrigger && !isLit && litButton != null)
            {
                litButton.gameObject.SetActive(true);
                torchCanvas.SetActive(true);
            }
        }

        if (gameManager != null && gameManager.characterAnimator != null)
            gameManager.characterAnimator.SetBool("isCorrect", false);
    }

    // Public getters
    public bool IsInTorchMode() => isInTorchMode;
    public float GetCurrentFlameScale() => currentFlameScale;
    public int GetCurrentCorrectAnswers() => currentCorrectAnswers;
    public bool IsLit() => isLit;
    public string GetTorchID() => torchID;

    // For saving/loading
    public void SetLit(bool lit)
    {
        isLit = lit;
        if (fireObject != null && isLit)
        {
            fireObject.transform.localScale = Vector3.one; // Show flame if lit
        }
    }
}