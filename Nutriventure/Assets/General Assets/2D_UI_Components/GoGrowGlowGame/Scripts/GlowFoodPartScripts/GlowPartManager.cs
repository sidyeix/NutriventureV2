using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumetricLines;
using Cinemachine;

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

    [Header("Character Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string transferEnergyParam = "transferEnergy";

    [Header("Objects to ENABLE When Holding Button")]
    [SerializeField] private List<GameObject> objectsToEnableWhenHolding = new List<GameObject>();

    [Header("Objects to DISABLE When Holding Button")]
    [SerializeField] private List<GameObject> objectsToDisableWhenHolding = new List<GameObject>();

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
    [SerializeField] private float warningBlinkSpeed = 2f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip panelSlideInSound;
    [SerializeField] private AudioClip panelSlideOutSound;
    [SerializeField] private AudioClip transferStartSound;
    [SerializeField] private AudioClip transferLoopSound;
    [SerializeField] private AudioClip transferStopSound;
    [SerializeField] private AudioClip towerLitSound;
    [SerializeField] private AudioClip warningSound;

    [Header("Energy Transfer Settings")]
    [SerializeField] private float transferRate = 5f;
    [SerializeField] private float maxTowerEnergy = 100f;
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private int pointReward = 250;

    [Header("Tracking Settings")]
    [SerializeField] private string trackerFormat = "{0}/{1} Towers Lit";

    [Header("References")]
    [SerializeField] private List<GlowTower> glowTowers = new List<GlowTower>();
    [SerializeField] private GameObject playerObject;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera towerFocusVirtualCamera;
    [SerializeField] private float playerRotationSpeed = 8f; // Increased speed for faster rotation
    [SerializeField] private float lookAtAngleThreshold = 10f; // Angle threshold for "looking at" tower

    // State
    private bool isGlowPartActive = false;
    private bool isTrackerVisible = false;
    private bool isTransferring = false;
    private GlowTower currentActiveTower = null;
    private int litTowersCount = 0;
    private Vector3 trackerPanelHiddenPosition;
    private Vector3 trackerPanelVisiblePosition;
    private Coroutine panelSlideCoroutine;
    private Coroutine transferCoroutine;
    private Coroutine warningBlinkCoroutine;
    private AudioSource audioSource;
    private AudioSource transferAudioSource;
    private Coroutine warningCheckCoroutine;
    private ButtonPressHandler buttonPressHandler;
    private List<TowerProximityDetector> proximityDetectors = new List<TowerProximityDetector>();
    private Transform playerTransform;
    private bool wasEnergyPaused = false;

    // NEW: Rotation state
    private bool isRotatingToTower = false;
    private bool isLookingAtTower = false;
    private Coroutine rotationCheckCoroutine;

    // Lightsaber
    private Vector3 lightsaberOriginalEndPos;
    private bool isLightsaberActive = false;
    private Vector3 targetEndPos;
    private Coroutine lightsaberCoroutine;

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
        DisableGlowPart();
        UpdateTrackerText();

        SetupTransferButton();
        SetupProximityDetectors();
    }

    private void Update()
    {
        if (!isGlowPartActive) return;

        CheckPlayerEnergy();

        // NEW: Handle rotation and check if looking at tower
        if (isRotatingToTower && currentActiveTower != null && playerTransform != null)
        {
            RotatePlayerToTower();
            CheckIfLookingAtTower();
        }
    }

    // NEW: Check if player is looking at the tower
    private void CheckIfLookingAtTower()
    {
        if (currentActiveTower == null || playerTransform == null) return;

        Vector3 towerPosition = currentActiveTower.GetCenterPointPosition();
        Vector3 directionToTower = towerPosition - playerTransform.position;
        directionToTower.y = 0;

        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0;

        // Calculate angle between player forward and direction to tower
        float angle = Vector3.Angle(playerForward, directionToTower.normalized);

        if (angle <= lookAtAngleThreshold && !isLookingAtTower)
        {
            isLookingAtTower = true;
            OnPlayerLookingAtTower();
        }
    }

    // NEW: Called when player is looking at the tower
    private void OnPlayerLookingAtTower()
    {
        Debug.Log("Player is now looking at the tower!");

        // Start lightsaber and other effects
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

            if (isTransferring)
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
        StartWarningCheck();

        isGlowPartActive = true;

        if (GoGrowGlowGameManager.Instance != null)
            GoGrowGlowGameManager.Instance.StartOneLifeCheck();
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

        StopWarningCheck();
        StopWarningBlink();

        currentActiveTower = null;

        if (transferButton != null)
            transferButton.gameObject.SetActive(false);

        if (transferProgressSlider != null)
            transferProgressSlider.gameObject.SetActive(false);

        isGlowPartActive = false;

        if (GoGrowGlowGameManager.Instance != null)
        {
            if (!wasEnergyPaused)
                GoGrowGlowGameManager.Instance.ResumeEnergyDecrease();
            GoGrowGlowGameManager.Instance.StopOneLifeCheck();
        }
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

        isGlowPartActive = false;
    }

    // BUTTON EVENT HANDLERS
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

    // NEW: Start rotation phase
    private void StartRotationToTower()
    {
        if (!isGlowPartActive || currentActiveTower == null || currentActiveTower.IsFullyLit())
        {
            Debug.Log($"Cannot start rotation: Active={isGlowPartActive}, Tower={currentActiveTower?.name}");
            return;
        }

        Debug.Log($"Starting rotation to tower: {currentActiveTower.gameObject.name}");

        // Reset states
        isRotatingToTower = true;
        isLookingAtTower = false;

        // 1. Character animation
        SetCharacterTransferAnimation(true);

        // 2. Manage objects (enable when holding / disable when holding)
        EnableObjectsWhenHolding();
        DisableObjectsWhenHolding();

        // 3. Set tower focus camera priority
        SetTowerFocusCameraPriority(50);

        // 4. Tower animation (start lighting animation)
        currentActiveTower.SetLightingAnimation(true);

        Debug.Log("Waiting for player to look at tower before starting lightsaber...");
    }

    // NEW: Start lightsaber and effects after rotation
    private void StartLightsaberAndEffects()
    {
        if (!isRotatingToTower || isTransferring) return;

        Debug.Log("Starting lightsaber and energy transfer");

        isTransferring = true;

        // Start lightsaber
        if (lightsaber != null && currentActiveTower != null)
        {
            StartLightsaberExtension();
        }

        // Start energy transfer routine
        if (transferCoroutine != null)
            StopCoroutine(transferCoroutine);
        transferCoroutine = StartCoroutine(TransferEnergyRoutine());

        PlaySound(transferStartSound);

        if (transferLoopSound != null && transferAudioSource != null)
        {
            transferAudioSource.clip = transferLoopSound;
            transferAudioSource.Play();
        }
    }

    private void StopTransfer()
    {
        if (!isRotatingToTower && !isTransferring) return;

        Debug.Log("Stopping energy transfer and rotation");

        // Reset all states
        isRotatingToTower = false;
        isLookingAtTower = false;
        isTransferring = false;

        // 1. Character animation
        SetCharacterTransferAnimation(false);

        // 2. Restore object states
        DisableObjectsWhenNotHolding();
        EnableObjectsWhenNotHolding();

        // 3. Stop lightsaber
        if (lightsaber != null)
        {
            StartLightsaberRetraction();
        }

        // 4. Tower animation
        if (currentActiveTower != null && !currentActiveTower.IsFullyLit())
        {
            currentActiveTower.SetLightingAnimation(false);
        }

        // 5. Reset tower focus camera priority
        SetTowerFocusCameraPriority(10);

        if (transferCoroutine != null)
        {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
        }

        if (transferAudioSource != null && transferAudioSource.isPlaying)
            transferAudioSource.Stop();

        PlaySound(transferStopSound);
    }

    private void SetTowerFocusCameraPriority(int priority)
    {
        if (towerFocusVirtualCamera != null)
        {
            towerFocusVirtualCamera.Priority = priority;
        }
    }

    // OBJECT MANAGEMENT METHODS
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

    // LIGHTSABER METHODS
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

    // CHARACTER ANIMATION
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
                StopTransfer();
                yield break;
            }

            CheckPlayerEnergyForWarning();
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

        // Reset rotation state
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
            AllTowersLit();
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

    private void CheckPlayerEnergyForWarning()
    {
        if (GoGrowGlowGameManager.Instance == null) return;

        float playerEnergy = GoGrowGlowGameManager.Instance.GetCurrentEnergy();
        UpdateWarningPanelState(playerEnergy);
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

    private void AllTowersLit()
    {
        Debug.Log("=== ALL TOWERS ARE LIT! ===");

        if (trackerText != null)
        {
            trackerText.text = "COMPLETE!";
            StartCoroutine(FlashCompleteText());
        }

        Invoke(nameof(EndGlowPart), 3f);
    }

    private void StartWarningCheck()
    {
        if (warningCheckCoroutine != null)
            StopCoroutine(warningCheckCoroutine);

        warningCheckCoroutine = StartCoroutine(WarningCheckRoutine());
    }

    private void StopWarningCheck()
    {
        if (warningCheckCoroutine != null)
        {
            StopCoroutine(warningCheckCoroutine);
            warningCheckCoroutine = null;
        }

        if (warningPanel != null)
            warningPanel.SetActive(false);

        StopWarningBlink();
    }

    private IEnumerator WarningCheckRoutine()
    {
        while (isGlowPartActive)
        {
            yield return new WaitForSeconds(0.2f);

            if (GoGrowGlowGameManager.Instance != null)
            {
                float playerEnergy = GoGrowGlowGameManager.Instance.GetCurrentEnergy();
                UpdateWarningPanelState(playerEnergy);
            }
        }
    }

    private void UpdateWarningPanelState(float playerEnergy)
    {
        if (warningPanel == null) return;

        bool shouldShowWarning = playerEnergy <= warningThreshold && playerEnergy > 0f;

        if (warningPanel.activeSelf != shouldShowWarning)
        {
            warningPanel.SetActive(shouldShowWarning);

            if (shouldShowWarning)
            {
                StartWarningBlink();
                PlaySound(warningSound);
            }
            else
            {
                StopWarningBlink();
            }
        }
    }

    private void StartWarningBlink()
    {
        if (warningBlinkCoroutine != null)
            StopCoroutine(warningBlinkCoroutine);

        warningBlinkCoroutine = StartCoroutine(WarningBlinkRoutine());
    }

    private void StopWarningBlink()
    {
        if (warningBlinkCoroutine != null)
        {
            StopCoroutine(warningBlinkCoroutine);
            warningBlinkCoroutine = null;
        }

        if (warningPanelImage != null)
        {
            Color color = warningPanelImage.color;
            color.a = 1f;
            warningPanelImage.color = color;
        }
    }

    private IEnumerator WarningBlinkRoutine()
    {
        if (warningPanelImage == null) yield break;

        while (true)
        {
            float alpha = Mathf.PingPong(Time.time * warningBlinkSpeed, 1f);
            Color color = warningPanelImage.color;
            color.a = 0.3f + alpha * 0.7f;
            warningPanelImage.color = color;

            yield return null;
        }
    }

    private void UpdateTrackerText()
    {
        if (trackerText != null)
            trackerText.text = string.Format(trackerFormat, litTowersCount, glowTowers.Count);
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

    public void RegisterTower(GlowTower tower)
    {
        if (!glowTowers.Contains(tower))
        {
            glowTowers.Add(tower);
        }
    }

    // Getters and setters
    public bool IsGlowPartActive() => isGlowPartActive;
    public int GetLitTowersCount() => litTowersCount;
    public int GetTotalTowers() => glowTowers.Count;
    public float GetTransferRate() => transferRate;
    public float GetWarningThreshold() => warningThreshold;
    public int GetPointReward() => pointReward;

    public void SetTransferRate(float rate) => transferRate = Mathf.Max(0.1f, rate);
    public void SetWarningThreshold(float threshold) => warningThreshold = Mathf.Max(1f, threshold);
    public void SetPointReward(int points) => pointReward = Mathf.Max(0, points);
}