using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumetricLines;
using Cinemachine;
using UnityEngine.Playables;

public class GlowPartManager : MonoBehaviour
{
    public static GlowPartManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject glowCanvas;
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TMP_Text trackerText;
    [SerializeField] private Button transferButton;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private Slider transferProgressSlider;
    [SerializeField] private TMP_Text transferProgressText;
    [SerializeField] private TMP_Text transferButtonText;
    [SerializeField] private Image warningPanelImage;

    [Header("Energy Slider Indicator")]
    [SerializeField] private GameObject energySliderIndicator;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text energySliderText;
    [SerializeField] private float energyPanelSlideDuration = 0.5f;
    [SerializeField] private float energyPanelSlideDistance = 300f;
    [SerializeField] private CanvasGroup energySliderCanvasGroup;

    [Header("Character Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string transferEnergyParam = "transferEnergy";

    [Header("Objects to ENABLE When Holding Button")]
    [SerializeField] private List<GameObject> objectsToEnableWhenHolding = new List<GameObject>();

    [Header("Objects to DISABLE When Holding Button")]
    [SerializeField] private List<GameObject> objectsToDisableWhenHolding = new List<GameObject>();

    [Header("Objects to ENABLE When Glow Part Starts")]
    [SerializeField] private List<GameObject> objectsToEnableOnStart = new List<GameObject>();

    [Header("Objects to DISABLE When Glow Part Ends")]
    [SerializeField] private List<GameObject> objectsToDisableOnEnd = new List<GameObject>();

    [Header("Lightsaber Settings")]
    [SerializeField] private VolumetricLineBehavior lightsaber;
    [SerializeField] private float lightsaberExtendSpeed = 5f;
    [SerializeField] private float lightsaberRetractSpeed = 8f;
    [SerializeField] private float maxLightsaberWidth = 1f;

    [Header("Animation Settings")]
    [SerializeField] private float panelSlideDuration = 0.8f;
    [SerializeField] private float panelSlideDistance = 400f;
    [SerializeField] private float panelShowDelay = 0.2f;
    [SerializeField] private float panelSlideSoundDelay = 0.1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip panelSlideInSound;
    [SerializeField] private AudioClip panelSlideOutSound;
    [SerializeField] private AudioClip transferStartSound;
    [SerializeField] private AudioClip transferLoopSound;
    [SerializeField] private AudioClip transferStopSound;
    [SerializeField] private AudioClip towerLitSound;
    [SerializeField] private AudioClip fillingUpSound;
    [SerializeField] private AudioClip completeSound;

    [Header("Energy Transfer Settings")]
    [SerializeField] private float transferRate = 5f;
    [SerializeField] private float maxTowerEnergy = 100f;
    [SerializeField] private int pointReward = 250;

    [Header("Tracking Settings")]
    [SerializeField] private string trackerFormat = "{0}/{1} Towers Lit";

    [Header("References")]
    [SerializeField] private List<GlowTower> glowTowers = new List<GlowTower>();
    [SerializeField] private GameObject playerObject;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera towerFocusVirtualCamera;
    [SerializeField] private float playerRotationSpeed = 8f;
    [SerializeField] private float lookAtAngleThreshold = 10f;

    [Header("Timeline Settings")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private PlayableAsset timelineToPlay;
    [SerializeField] private bool playTimelineOnCompletion = true;
    [SerializeField] private float timelineDelay = 2f;

    private bool isGlowPartActive = false;
    private bool isTrackerVisible = false;
    private bool isTransferring = false;
    private GlowTower currentActiveTower = null;
    private int litTowersCount = 0;
    private Vector3 trackerPanelHiddenPosition;
    private Vector3 trackerPanelVisiblePosition;
    private bool isWarningActive = false;
    private Vector3 energyPanelHiddenPosition;
    private Vector3 energyPanelVisiblePosition;
    private bool wasEnergyPaused = false;
    private bool wasTimerPaused = false;
    private bool isGameStatePaused = false;
    private Coroutine panelSlideCoroutine;
    private Coroutine energyPanelAnimationCoroutine;
    private Coroutine transferCoroutine;
    private AudioSource audioSource;
    private AudioSource transferAudioSource;
    private AudioSource fillingAudioSource;
    private ButtonPressHandler buttonPressHandler;
    private List<TowerProximityDetector> proximityDetectors = new List<TowerProximityDetector>();
    private Transform playerTransform;
    private bool isRotatingToTower = false;
    private bool isLookingAtTower = false;
    private Vector3 lightsaberOriginalEndPos;
    private bool isLightsaberActive = false;
    private Vector3 targetEndPos;
    private Coroutine lightsaberCoroutine;
    private bool hasCompleted = false;

    // NEW: Store initial tower states
    private Dictionary<GlowTower, float> initialTowerEnergies = new Dictionary<GlowTower, float>();
    private Dictionary<GlowTower, bool> initialTowerStates = new Dictionary<GlowTower, bool>();

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        transferAudioSource = gameObject.AddComponent<AudioSource>();
        transferAudioSource.loop = true;
        transferAudioSource.playOnAwake = false;

        fillingAudioSource = gameObject.AddComponent<AudioSource>();
        fillingAudioSource.loop = true;
        fillingAudioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;

            if (characterAnimator == null)
                characterAnimator = playerObject.GetComponentInChildren<Animator>();
        }
        else
        {
            Debug.LogError("GlowPartManager: No player object found!");
        }

        if (towerFocusVirtualCamera != null)
        {
            towerFocusVirtualCamera.Priority = 10;
        }

        InitializeLightsaber();
        InitializeObjectStates();
        InitializeTracker();
        InitializeEnergySliderIndicator();
        DisableGlowPart();
        UpdateTrackerText();

        SetupTransferButton();
        SetupProximityDetectors();

        // NEW: Store initial tower states
        StoreInitialTowerStates();

        DisableObjectsOnStart();
    }

    // NEW: Method to store initial tower states
    private void StoreInitialTowerStates()
    {
        initialTowerEnergies.Clear();
        initialTowerStates.Clear();

        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null)
            {
                initialTowerEnergies[tower] = 0f; // Towers start with 0 energy
                initialTowerStates[tower] = false; // Towers start as not active

                // Ensure tower is at initial state
                tower.SetEnergy(0f);
                tower.ResetTower();
            }
        }
    }

    private void InitializeLightsaber()
    {
        if (lightsaber != null)
        {
            lightsaberOriginalEndPos = lightsaber.EndPos;
            lightsaber.LineWidth = 0f;
        }
    }

    private void InitializeObjectStates()
    {
        foreach (GameObject obj in objectsToEnableWhenHolding)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToDisableWhenHolding)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    private void InitializeTracker()
    {
        if (trackerPanel != null)
        {
            trackerPanelHiddenPosition = trackerPanel.transform.localPosition - new Vector3(panelSlideDistance, 0, 0);
            trackerPanelVisiblePosition = trackerPanel.transform.localPosition;

            trackerPanel.transform.localPosition = trackerPanelHiddenPosition;
            trackerPanel.SetActive(false);
        }

        if (warningPanel != null)
            warningPanel.SetActive(false);

        if (transferProgressSlider != null)
        {
            transferProgressSlider.gameObject.SetActive(false);
            transferProgressSlider.minValue = 0f;
            transferProgressSlider.maxValue = 1f;
            transferProgressSlider.value = 0f;
        }
    }

    private void InitializeEnergySliderIndicator()
    {
        if (energySliderIndicator != null)
        {
            if (energySliderCanvasGroup == null)
            {
                energySliderCanvasGroup = energySliderIndicator.GetComponent<CanvasGroup>();
                if (energySliderCanvasGroup == null)
                {
                    energySliderCanvasGroup = energySliderIndicator.AddComponent<CanvasGroup>();
                }
            }

            RectTransform rectTransform = energySliderIndicator.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                energyPanelVisiblePosition = rectTransform.anchoredPosition;
                energyPanelHiddenPosition = energyPanelVisiblePosition - new Vector3(energyPanelSlideDistance, 0, 0);

                rectTransform.anchoredPosition = energyPanelHiddenPosition;
                energySliderCanvasGroup.alpha = 0f;
                energySliderCanvasGroup.interactable = false;
                energySliderCanvasGroup.blocksRaycasts = false;
                energySliderIndicator.SetActive(false);
            }

            if (energySlider != null)
            {
                energySlider.minValue = 0f;
                energySlider.maxValue = 1f;
                energySlider.value = 0f;
            }

            if (energySliderText != null)
            {
                energySliderText.text = "0%";
            }
        }
    }

    private void SetupTransferButton()
    {
        buttonPressHandler = transferButton.gameObject.GetComponent<ButtonPressHandler>();
        if (buttonPressHandler == null)
            buttonPressHandler = transferButton.gameObject.AddComponent<ButtonPressHandler>();

        buttonPressHandler.onButtonPressed.RemoveAllListeners();
        buttonPressHandler.onButtonReleased.RemoveAllListeners();
        buttonPressHandler.onButtonHeld.RemoveAllListeners();

        buttonPressHandler.onButtonPressed.AddListener(HandleButtonPressed);
        buttonPressHandler.onButtonReleased.AddListener(HandleButtonReleased);
        buttonPressHandler.onButtonHeld.AddListener(HandleButtonHeld);

        if (transferButtonText != null)
            transferButtonText.text = "HOLD TO TRANSFER";

        transferButton.gameObject.SetActive(false);
    }

    private void SetupProximityDetectors()
    {
        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null)
            {
                tower.SetEnergy(0f);

                TowerProximityDetector detector = tower.gameObject.GetComponent<TowerProximityDetector>();
                if (detector == null)
                    detector = tower.gameObject.AddComponent<TowerProximityDetector>();

                detector.OnPlayerEnterRange += OnPlayerEnterTowerRange;
                detector.OnPlayerExitRange += OnPlayerExitTowerRange;

                proximityDetectors.Add(detector);
            }
        }
    }

    private void Update()
    {
        if (!isGlowPartActive) return;

        CheckPlayerEnergy();

        if (isRotatingToTower && currentActiveTower != null && playerTransform != null)
        {
            RotatePlayerToTower();
            CheckIfLookingAtTower();
        }

        if (isTransferring && currentActiveTower != null)
        {
            UpdateEnergySliderIndicator();
        }
    }

    private void StartLoopAudio()
    {
        if (transferLoopSound != null && transferAudioSource != null)
        {
            transferAudioSource.clip = transferLoopSound;
            transferAudioSource.loop = true;
            transferAudioSource.Play();
        }

        if (fillingUpSound != null && fillingAudioSource != null)
        {
            fillingAudioSource.clip = fillingUpSound;
            fillingAudioSource.loop = true;
            fillingAudioSource.Play();
        }
    }

    private void StopLoopAudio()
    {
        if (transferAudioSource != null && transferAudioSource.isPlaying)
        {
            transferAudioSource.Stop();
        }

        if (fillingAudioSource != null && fillingAudioSource.isPlaying)
        {
            fillingAudioSource.Stop();
        }

        if (transferStopSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transferStopSound);
        }
    }

    private void UpdateEnergySliderIndicator()
    {
        if (energySlider == null || energySliderText == null || currentActiveTower == null) return;

        float towerEnergy = currentActiveTower.GetCurrentEnergy();
        float maxEnergy = currentActiveTower.GetMaxEnergy();
        float energyPercentage = (towerEnergy / maxEnergy) * 100f;

        energySlider.value = towerEnergy / maxEnergy;
        energySliderText.text = $"{energyPercentage:F0}%";

        if (energyPercentage <= 33f)
            energySliderText.color = Color.red;
        else if (energyPercentage <= 66f)
            energySliderText.color = Color.yellow;
        else
            energySliderText.color = Color.green;
    }

    private void ShowEnergySliderIndicator()
    {
        if (energySliderIndicator == null || energySliderCanvasGroup == null || currentActiveTower == null) return;

        energySliderIndicator.SetActive(true);

        if (energyPanelAnimationCoroutine != null)
            StopCoroutine(energyPanelAnimationCoroutine);

        energyPanelAnimationCoroutine = StartCoroutine(AnimateEnergyPanel(true));
    }

    private void HideEnergySliderIndicator()
    {
        if (energySliderIndicator == null || energySliderCanvasGroup == null) return;

        if (energyPanelAnimationCoroutine != null)
            StopCoroutine(energyPanelAnimationCoroutine);

        energyPanelAnimationCoroutine = StartCoroutine(AnimateEnergyPanel(false));
        StartCoroutine(DisableEnergyPanelAfterAnimation());
    }

    private IEnumerator AnimateEnergyPanel(bool showIn)
    {
        if (energySliderIndicator == null || energySliderCanvasGroup == null) yield break;

        RectTransform rectTransform = energySliderIndicator.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector3 startPos = rectTransform.anchoredPosition;
        Vector3 targetPos = showIn ? energyPanelVisiblePosition : energyPanelHiddenPosition;
        float startAlpha = energySliderCanvasGroup.alpha;
        float targetAlpha = showIn ? 1f : 0f;
        float elapsedTime = 0f;

        if (showIn)
        {
            energySliderCanvasGroup.interactable = true;
            energySliderCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            energySliderCanvasGroup.interactable = false;
            energySliderCanvasGroup.blocksRaycasts = false;
        }

        if (showIn && panelSlideInSound != null)
            PlaySound(panelSlideInSound);
        else if (!showIn && panelSlideOutSound != null)
            PlaySound(panelSlideOutSound);

        while (elapsedTime < energyPanelSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / energyPanelSlideDuration);

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, easedT);
            energySliderCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedT);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        energySliderCanvasGroup.alpha = targetAlpha;
        energyPanelAnimationCoroutine = null;
    }

    private IEnumerator DisableEnergyPanelAfterAnimation()
    {
        yield return new WaitForSeconds(energyPanelSlideDuration + 0.1f);
        if (energySliderIndicator != null)
            energySliderIndicator.SetActive(false);
    }

    private void CheckIfLookingAtTower()
    {
        if (currentActiveTower == null || playerTransform == null) return;

        Vector3 towerPosition = currentActiveTower.GetCenterPointPosition();
        Vector3 directionToTower = towerPosition - playerTransform.position;
        directionToTower.y = 0;

        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0;

        float angle = Vector3.Angle(playerForward, directionToTower.normalized);

        if (angle <= lookAtAngleThreshold && !isLookingAtTower)
        {
            isLookingAtTower = true;
            OnPlayerLookingAtTower();
        }
    }

    private void OnPlayerLookingAtTower()
    {
        Debug.Log("Player is now looking at the tower!");
        StartLightsaberAndEffects();
    }

    private void RotatePlayerToTower()
    {
        Vector3 towerPosition = currentActiveTower.GetCenterPointPosition();
        Vector3 directionToTower = towerPosition - playerTransform.position;
        directionToTower.y = 0;

        if (directionToTower != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTower);
            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation,
                targetRotation,
                playerRotationSpeed * Time.deltaTime
            );
        }
    }

    // NEW: Method to disable objects that should be inactive when glow part starts
    private void DisableObjectsOnStart()
    {
        foreach (GameObject obj in objectsToEnableOnStart)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    private void OnPlayerEnterTowerRange(GlowTower tower)
    {
        if (!isGlowPartActive || tower.IsFullyLit()) return;

        Debug.Log($"Player entered range of tower: {tower.gameObject.name}");
        currentActiveTower = tower;

        if (transferButton != null)
            transferButton.gameObject.SetActive(true);

        if (transferProgressSlider != null)
        {
            transferProgressSlider.gameObject.SetActive(true);
            UpdateTransferProgressUI();
        }
    }

    private void OnPlayerExitTowerRange(GlowTower tower)
    {
        if (!isGlowPartActive) return;

        if (currentActiveTower == tower)
        {
            Debug.Log($"Player exited range of current tower: {tower.gameObject.name}");

            if (isTransferring || isRotatingToTower)
                StopTransfer();

            if (transferButton != null)
                transferButton.gameObject.SetActive(false);

            if (transferProgressSlider != null)
                transferProgressSlider.gameObject.SetActive(false);

            currentActiveTower = null;
        }
    }

    public void StartGlowPart()
    {
        if (isGlowPartActive) return;

        Debug.Log("=== STARTING GLOW PART ===");

        if (GoGrowGlowGameManager.Instance != null)
        {
            wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
            GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
            Debug.Log("Energy decrease paused for glow part");
        }

        if (glowCanvas != null)
            glowCanvas.SetActive(true);

        ShowTrackerPanel();

        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null)
            {
                tower.ActivateTower();
                tower.SetEnergy(0f);
            }
        }

        UpdateLitTowersCount();
        isGlowPartActive = true;

        // NEW: Enable objects when glow part starts
        EnableObjectsOnGlowPartStart();

        if (GoGrowGlowGameManager.Instance != null)
            GoGrowGlowGameManager.Instance.StartOneLifeCheck();
    }

    // NEW: Method to enable objects when glow part starts
    private void EnableObjectsOnGlowPartStart()
    {
        Debug.Log($"Enabling {objectsToEnableOnStart.Count} objects on glow part start");
        foreach (GameObject obj in objectsToEnableOnStart)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Enabled object: {obj.name}");
            }
        }
    }

    public void EndGlowPart()
    {
        if (!isGlowPartActive) return;

        Debug.Log("=== ENDING GLOW PART ===");

        HideTrackerPanel();
        StopTransfer();
        StartCoroutine(DisableCanvasAfterDelay());

        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null)
                tower.DeactivateTower();
        }

        currentActiveTower = null;

        if (transferButton != null)
            transferButton.gameObject.SetActive(false);

        if (transferProgressSlider != null)
            transferProgressSlider.gameObject.SetActive(false);

        // NEW: Disable objects when glow part ends
        DisableObjectsOnGlowPartEnd();

        if (GoGrowGlowGameManager.Instance != null &&
            GoGrowGlowGameManager.Instance.IsGameActive() &&
            GoGrowGlowGameManager.Instance.foodSpawner != null)
        {
            GoGrowGlowGameManager.Instance.foodSpawner.HideAllFood();
            Debug.Log("All food hidden after glow part completion");
        }

        isGlowPartActive = false;

        if (GoGrowGlowGameManager.Instance != null)
        {
            if (!wasEnergyPaused && !hasCompleted)
            {
                GoGrowGlowGameManager.Instance.ResumeEnergyDecrease();
                Debug.Log("Energy decrease resumed (wasn't paused before)");
            }
            else if (hasCompleted)
            {
                Debug.Log("Glow part completed - energy decrease remains paused");
            }

            GoGrowGlowGameManager.Instance.StopOneLifeCheck();

            if (hasCompleted && playTimelineOnCompletion)
            {
                StartCoroutine(PlayTimelineAfterDelay());
            }
        }
    }

    // NEW: Method to disable objects when glow part ends
    private void DisableObjectsOnGlowPartEnd()
    {
        Debug.Log($"Disabling {objectsToDisableOnEnd.Count} objects on glow part end");
        foreach (GameObject obj in objectsToDisableOnEnd)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"Disabled object: {obj.name}");
            }
        }
    }

    private IEnumerator PlayTimelineAfterDelay()
    {
        Debug.Log($"Waiting {timelineDelay} seconds before playing timeline...");
        yield return new WaitForSeconds(timelineDelay);

        Debug.Log("Playing timeline after glow part completion...");

        bool isGameActive = GoGrowGlowGameManager.Instance != null && GoGrowGlowGameManager.Instance.IsGameActive();

        if (isGameActive)
        {
            wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
            wasTimerPaused = GoGrowGlowGameManager.Instance.IsGameTimerPaused();

            GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
            GoGrowGlowGameManager.Instance.PauseGameTimer();

            isGameStatePaused = true;
        }

        if (playableDirector != null && timelineToPlay != null)
        {
            playableDirector.stopped += OnTimelineStopped;
            playableDirector.playableAsset = timelineToPlay;
            playableDirector.Play();
        }
        else if (playableDirector == null)
        {
            Debug.LogWarning("Playable Director not assigned.");
        }
        else if (timelineToPlay == null)
        {
            Debug.LogWarning("Timeline asset not assigned.");
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != playableDirector) return;

        Debug.Log($"Glow Part: Timeline stopped.");

        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }

        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
        {
            ResumeGameState();
        }
    }

    private void ResumeGameState()
    {
        if (GoGrowGlowGameManager.Instance != null)
        {
            if (!wasTimerPaused)
            {
                GoGrowGlowGameManager.Instance.ResumeGameTimer();
            }

            if (!wasEnergyPaused && !hasCompleted)
            {
                GoGrowGlowGameManager.Instance.ResumeEnergyDecrease();
                Debug.Log("Resuming energy decrease after timeline");
            }
            else
            {
                Debug.Log("Keeping energy decrease paused (glow part completed)");
            }
        }

        isGameStatePaused = false;
    }

    private IEnumerator DisableCanvasAfterDelay()
    {
        yield return new WaitForSeconds(panelSlideDuration + 0.3f);

        if (glowCanvas != null)
            glowCanvas.SetActive(false);
    }

    private void DisableGlowPart()
    {
        if (glowCanvas != null) glowCanvas.SetActive(false);
        if (trackerPanel != null) trackerPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);
        if (transferButton != null) transferButton.gameObject.SetActive(false);
        if (transferProgressSlider != null) transferProgressSlider.gameObject.SetActive(false);
        if (energySliderIndicator != null) energySliderIndicator.SetActive(false);

        isGlowPartActive = false;

        // NEW: Also disable end objects when glow part is disabled
        DisableObjectsOnGlowPartEnd();
    }

    private void HandleButtonPressed()
    {
        Debug.Log("Transfer button PRESSED");
        StartRotationToTower();
    }

    private void HandleButtonReleased()
    {
        Debug.Log("Transfer button RELEASED");
        StopTransfer();
    }

    private void HandleButtonHeld()
    {
        // Continuously called while button is held
    }

    private void StartRotationToTower()
    {
        if (!isGlowPartActive || currentActiveTower == null || currentActiveTower.IsFullyLit())
        {
            return;
        }

        Debug.Log($"Starting rotation to tower: {currentActiveTower.gameObject.name}");

        isRotatingToTower = true;
        isLookingAtTower = false;

        SetCharacterTransferAnimation(true);
        EnableObjectsWhenHolding();
        DisableObjectsWhenHolding();
        SetTowerFocusCameraPriority(50);
        currentActiveTower.SetLightingAnimation(true);

        ShowEnergySliderIndicator();

        if (energySlider != null && energySliderText != null && currentActiveTower != null)
        {
            float towerEnergy = currentActiveTower.GetCurrentEnergy();
            float maxEnergy = currentActiveTower.GetMaxEnergy();
            energySlider.value = towerEnergy / maxEnergy;
            energySliderText.text = $"{(towerEnergy / maxEnergy * 100f):F0}%";
        }
    }

    private void StartLightsaberAndEffects()
    {
        if (!isRotatingToTower || isTransferring) return;

        Debug.Log("Starting lightsaber and energy transfer");

        isTransferring = true;

        StartLoopAudio();

        if (lightsaber != null && currentActiveTower != null)
        {
            StartLightsaberExtension();
        }

        if (transferCoroutine != null)
            StopCoroutine(transferCoroutine);
        transferCoroutine = StartCoroutine(TransferEnergyRoutine());

        PlaySound(transferStartSound);
    }

    private void StopTransfer()
    {
        if (!isRotatingToTower && !isTransferring) return;

        Debug.Log("Stopping energy transfer and rotation");

        StopLoopAudio();

        isRotatingToTower = false;
        isLookingAtTower = false;
        isTransferring = false;

        SetCharacterTransferAnimation(false);
        DisableObjectsWhenNotHolding();
        EnableObjectsWhenNotHolding();

        if (lightsaber != null)
        {
            StartLightsaberRetraction();
        }

        if (currentActiveTower != null && !currentActiveTower.IsFullyLit())
        {
            currentActiveTower.SetLightingAnimation(false);
        }

        SetTowerFocusCameraPriority(10);
        HideEnergySliderIndicator();

        if (transferCoroutine != null)
        {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
        }
    }

    private void SetTowerFocusCameraPriority(int priority)
    {
        if (towerFocusVirtualCamera != null)
        {
            towerFocusVirtualCamera.Priority = priority;
        }
    }

    private void EnableObjectsWhenHolding()
    {
        foreach (GameObject obj in objectsToEnableWhenHolding)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }

    private void DisableObjectsWhenHolding()
    {
        foreach (GameObject obj in objectsToDisableWhenHolding)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    private void DisableObjectsWhenNotHolding()
    {
        foreach (GameObject obj in objectsToEnableWhenHolding)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    private void EnableObjectsWhenNotHolding()
    {
        foreach (GameObject obj in objectsToDisableWhenHolding)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }

    private void StartLightsaberExtension()
    {
        if (lightsaber == null || currentActiveTower == null) return;

        Transform beamHitPoint = currentActiveTower.GetBeamHitPoint();
        if (beamHitPoint != null)
        {
            Vector3 worldTargetPos = beamHitPoint.position;
            Vector3 localTargetPos = lightsaber.transform.InverseTransformPoint(worldTargetPos);

            targetEndPos = localTargetPos;
            isLightsaberActive = true;

            if (lightsaberCoroutine != null)
                StopCoroutine(lightsaberCoroutine);

            lightsaberCoroutine = StartCoroutine(LightsaberExtensionRoutine());
        }
    }

    private void StartLightsaberRetraction()
    {
        if (lightsaber == null) return;

        isLightsaberActive = false;

        if (lightsaberCoroutine != null)
            StopCoroutine(lightsaberCoroutine);

        lightsaberCoroutine = StartCoroutine(LightsaberRetractionRoutine());
    }

    private IEnumerator LightsaberExtensionRoutine()
    {
        Vector3 startPos = lightsaber.EndPos;
        float startWidth = lightsaber.LineWidth;
        float elapsedTime = 0f;

        while (elapsedTime < 1f && isLightsaberActive)
        {
            elapsedTime += Time.deltaTime * lightsaberExtendSpeed;
            float t = Mathf.Clamp01(elapsedTime);

            lightsaber.EndPos = Vector3.Lerp(startPos, targetEndPos, t);
            lightsaber.LineWidth = Mathf.Lerp(startWidth, maxLightsaberWidth, t);

            yield return null;
        }

        if (isLightsaberActive)
        {
            lightsaber.EndPos = targetEndPos;
            lightsaber.LineWidth = maxLightsaberWidth;
        }
    }

    private IEnumerator LightsaberRetractionRoutine()
    {
        Vector3 startPos = lightsaber.EndPos;
        float startWidth = lightsaber.LineWidth;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * lightsaberRetractSpeed;
            float t = Mathf.Clamp01(elapsedTime);

            lightsaber.EndPos = Vector3.Lerp(startPos, Vector3.zero, t);
            lightsaber.LineWidth = Mathf.Lerp(startWidth, 0f, t);

            yield return null;
        }

        lightsaber.EndPos = Vector3.zero;
        lightsaber.LineWidth = 0f;
    }

    private void SetCharacterTransferAnimation(bool transferring)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(transferEnergyParam, transferring);
        }
    }

    private IEnumerator TransferEnergyRoutine()
    {
        Debug.Log("Transfer Energy Routine Started");

        while (isTransferring && currentActiveTower != null &&
               !currentActiveTower.IsFullyLit() &&
               GoGrowGlowGameManager.Instance != null)
        {
            float playerEnergy = GoGrowGlowGameManager.Instance.GetCurrentEnergy();

            if (playerEnergy <= 0f)
            {
                Debug.Log("Player has no energy to transfer");
                StopTransfer();
                yield break;
            }

            float transferAmount = transferRate * Time.deltaTime;
            transferAmount = Mathf.Min(transferAmount, playerEnergy);

            float towerEnergyNeeded = currentActiveTower.GetMaxEnergy() - currentActiveTower.GetCurrentEnergy();
            transferAmount = Mathf.Min(transferAmount, towerEnergyNeeded);

            GoGrowGlowGameManager.Instance.RemoveEnergy(transferAmount);
            currentActiveTower.AddEnergy(transferAmount);

            if (currentActiveTower != null && !currentActiveTower.IsFullyLit())
            {
                currentActiveTower.SetLightingAnimation(true);
            }

            UpdateTransferProgressUI();

            if (currentActiveTower.IsFullyLit())
            {
                OnTowerFullyLit(currentActiveTower);
                yield break;
            }

            yield return null;
        }

        Debug.Log("Transfer Energy Routine Ended");
    }

    private void OnTowerFullyLit(GlowTower tower)
    {
        Debug.Log($"Tower {tower.gameObject.name} is fully lit!");

        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(pointReward);
        }

        litTowersCount++;
        UpdateTrackerText();

        PlaySound(towerLitSound);

        SetCharacterTransferAnimation(false);
        DisableObjectsWhenNotHolding();
        EnableObjectsWhenNotHolding();
        StartLightsaberRetraction();
        SetTowerFocusCameraPriority(10);
        HideEnergySliderIndicator();

        StopLoopAudio();

        isRotatingToTower = false;
        isLookingAtTower = false;
        isTransferring = false;

        if (currentActiveTower == tower)
        {
            if (transferButton != null)
                transferButton.gameObject.SetActive(false);
            if (transferProgressSlider != null)
                transferProgressSlider.gameObject.SetActive(false);
        }

        if (AreAllTowersLit())
        {
            AllTowersLit();
        }
    }

    private void AllTowersLit()
    {
        if (hasCompleted) return;

        Debug.Log("=== ALL TOWERS ARE LIT! ===");

        hasCompleted = true;

        PlaySound(completeSound);

        if (trackerText != null)
        {
            trackerText.text = "COMPLETE!";
            StartCoroutine(FlashCompleteText());
        }

        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(500);
        }

        Invoke(nameof(EndGlowPart), 3f);
    }

    private IEnumerator FlashCompleteText()
    {
        if (trackerText == null) yield break;

        Color originalColor = trackerText.color;
        float flashDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.PingPong(elapsedTime * 3f, 1f);
            trackerText.color = Color.Lerp(originalColor, Color.yellow, t);

            float scale = 1 + Mathf.Sin(elapsedTime * 5f) * 0.05f;
            trackerText.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        trackerText.color = originalColor;
        trackerText.transform.localScale = Vector3.one;
    }

    private void UpdateTransferProgressUI()
    {
        if (currentActiveTower == null || transferProgressSlider == null) return;

        float towerEnergy = currentActiveTower.GetCurrentEnergy();
        float progress = towerEnergy / maxTowerEnergy;

        transferProgressSlider.value = progress;

        if (transferProgressText != null)
        {
            transferProgressText.text = $"{towerEnergy:F0}/{maxTowerEnergy}";

            if (progress <= 0.33f)
                transferProgressText.color = Color.red;
            else if (progress <= 0.66f)
                transferProgressText.color = Color.yellow;
            else
                transferProgressText.color = Color.green;
        }
    }

    private void CheckPlayerEnergy()
    {
        if (GoGrowGlowGameManager.Instance == null) return;

        float playerEnergy = GoGrowGlowGameManager.Instance.GetCurrentEnergy();

        if (playerEnergy <= 0f && !GoGrowGlowGameManager.Instance.IsRespawning())
        {
            GoGrowGlowGameManager.Instance.LoseLife();
        }
    }

    private void UpdateLitTowersCount()
    {
        litTowersCount = 0;
        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null && tower.IsFullyLit())
                litTowersCount++;
        }
        UpdateTrackerText();
    }

    private bool AreAllTowersLit()
    {
        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null && !tower.IsFullyLit())
                return false;
        }
        return true;
    }

    private void UpdateTrackerText()
    {
        if (trackerText != null)
            trackerText.text = string.Format(trackerFormat, litTowersCount, glowTowers.Count);
    }

    public void ShowTrackerPanel()
    {
        if (isTrackerVisible || trackerPanel == null) return;

        isTrackerVisible = true;
        trackerPanel.SetActive(true);

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(true));
    }

    public void HideTrackerPanel()
    {
        if (!isTrackerVisible || trackerPanel == null) return;

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(false));
        StartCoroutine(DisablePanelAfterSlide());
    }

    private IEnumerator SlidePanel(bool slideIn)
    {
        if (trackerPanel == null) yield break;

        Vector3 startPos = trackerPanel.transform.localPosition;
        Vector3 targetPos = slideIn ? trackerPanelVisiblePosition : trackerPanelHiddenPosition;
        float elapsedTime = 0f;

        if (slideIn && panelSlideInSound != null)
            StartCoroutine(PlaySoundDelayed(panelSlideInSound, panelSlideSoundDelay));
        else if (!slideIn && panelSlideOutSound != null)
            StartCoroutine(PlaySoundDelayed(panelSlideOutSound, panelSlideSoundDelay));

        if (slideIn)
            yield return new WaitForSeconds(panelShowDelay);

        while (elapsedTime < panelSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / panelSlideDuration;

            if (slideIn)
                t = 1 - Mathf.Pow(1 - t, 3);
            else
                t = Mathf.Pow(t, 3);

            trackerPanel.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        trackerPanel.transform.localPosition = targetPos;
        panelSlideCoroutine = null;
    }

    private IEnumerator DisablePanelAfterSlide()
    {
        yield return new WaitForSeconds(panelSlideDuration + 0.1f);
        trackerPanel.SetActive(false);
        isTrackerVisible = false;
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(clip);
    }

    private void OnDestroy()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }

        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
        {
            ResumeGameState();
        }
    }

    public void RegisterTower(GlowTower tower)
    {
        if (!glowTowers.Contains(tower))
        {
            glowTowers.Add(tower);
        }
    }

    // NEW: COMPLETE RESET METHOD
    public void CompleteReset()
    {
        Debug.Log("=== COMPLETE RESET OF GLOW PART MANAGER ===");

        // Stop all ongoing processes
        StopAllCoroutines();

        // Reset lightsaber
        if (lightsaber != null)
        {
            lightsaber.EndPos = Vector3.zero;
            lightsaber.LineWidth = 0f;
        }

        // Reset all towers to initial state
        ResetAllTowers();

        // Reset manager state
        hasCompleted = false;
        litTowersCount = 0;
        isGlowPartActive = false;
        isTransferring = false;
        isRotatingToTower = false;
        isLookingAtTower = false;
        isLightsaberActive = false;
        currentActiveTower = null;

        // Stop audio
        StopLoopAudio();

        // Hide all UI
        DisableGlowPart();

        // Reset tracker text
        UpdateTrackerText();

        // Reset button states
        if (transferButton != null)
            transferButton.gameObject.SetActive(false);

        if (transferProgressSlider != null)
            transferProgressSlider.gameObject.SetActive(false);

        if (energySliderIndicator != null)
            energySliderIndicator.SetActive(false);

        // Reset character animation
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(transferEnergyParam, false);
        }

        // Resume game state if paused
        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
        {
            ResumeGameState();
        }

        Debug.Log("Glow Part Manager completely reset - All towers back to default state");
    }

    // NEW: Method to reset all towers
    public void ResetAllTowers()
    {
        Debug.Log($"Resetting {glowTowers.Count} glow towers...");

        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null)
            {
                // Reset tower to initial state
                tower.SetEnergy(0f);
                tower.ResetTower();

                // Deactivate the tower
                tower.DeactivateTower();

                Debug.Log($"Reset tower: {tower.gameObject.name} - Energy: {tower.GetCurrentEnergy()}");
            }
        }

        // Update lit towers count
        UpdateLitTowersCount();
    }

    // NEW: Method to reset a specific tower
    public void ResetTower(GlowTower tower)
    {
        if (tower != null)
        {
            tower.SetEnergy(0f);
            tower.ResetTower();
            tower.DeactivateTower();

            Debug.Log($"Individual tower reset: {tower.gameObject.name}");
        }
    }

    // NEW: Method to check if all towers are at initial state
    public bool AreAllTowersReset()
    {
        foreach (GlowTower tower in glowTowers)
        {
            if (tower != null)
            {
                if (tower.GetCurrentEnergy() > 0.01f ||
                    tower.IsFullyLit() ||
                    tower.IsLighting() ||
                    tower.IsActive())
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsGlowPartActive() => isGlowPartActive;
    public int GetLitTowersCount() => litTowersCount;
    public int GetTotalTowers() => glowTowers.Count;
    public float GetTransferRate() => transferRate;
    public int GetPointReward() => pointReward;

    public void SetTransferRate(float rate) => transferRate = Mathf.Max(0.1f, rate);
    public void SetPointReward(int points) => pointReward = Mathf.Max(0, points);
    public void SetPlayTimelineOnCompletion(bool enabled) => playTimelineOnCompletion = enabled;
    public void SetTimelineDelay(float delay) => timelineDelay = Mathf.Max(0f, delay);
    public void SetTimelineToPlay(PlayableAsset timeline) => timelineToPlay = timeline;
    public void SetPlayableDirector(PlayableDirector director) => playableDirector = director;
}